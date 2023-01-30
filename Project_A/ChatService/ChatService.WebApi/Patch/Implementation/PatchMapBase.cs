using System.Linq.Expressions;
using System.Reflection;
using Precision.WebApi.Interface;

namespace Precision.WebApi.Implementation
{
    public abstract class PatchMapBase<TRequest, TModel> : IPatchMap<TRequest, TModel>
    {
        private readonly BindingFlags bindingFlags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;
        private readonly IDictionary<string, Action<TRequest, TModel>> patchStateMapping = new Dictionary<string, Action<TRequest, TModel>>(StringComparer.InvariantCultureIgnoreCase);

        public IDictionary<string, Action<object, object>> GetMap()
        {
            return patchStateMapping.ToDictionary(x => x.Key, x => new Action<object, object>((y, z) => x.Value((TRequest)y, (TModel)z)));
        }

        public void AddPatchStateMapping<TProperty, TModelProperty>(
           Expression<Func<TRequest, TProperty>> propertyExpression,
           Expression<Func<TModel, TModelProperty>> modelMapping)
        {
            var propertyName = GetPropertyName(propertyExpression);

            var instanceProperty = typeof(TRequest).GetProperty(propertyName, bindingFlags);

            void mappingAction(TRequest request, TModel model)
            {
                BuildActionFromExpression(modelMapping)(model, (TModelProperty)instanceProperty.GetValue(request, null));
            }

            AddPatchStateMapping(propertyExpression, mappingAction);
        }

        public void AddPatchStateMapping<TProperty>(
            Expression<Func<TRequest, TProperty>> propertyExpression,
            Action<TRequest, TModel> propertyToModelMapping)
        {
            var propertyName = GetPropertyName(propertyExpression);

            if (patchStateMapping.ContainsKey(propertyName))
            {
                patchStateMapping[propertyName] = propertyToModelMapping;
            }
            else
            {
                patchStateMapping.Add(propertyName, propertyToModelMapping);
            }
        }

        private string GetPropertyName<TProperty>(Expression<Func<TRequest, TProperty>> propertyExpression)
        {
            if (!(propertyExpression.Body is MemberExpression propertyBody))
            {
                throw new InvalidCastException($"Cannot get property name from {nameof(propertyExpression)}.");
            }

            var fullPropertyName = propertyBody.ToString();

            return fullPropertyName.Substring(fullPropertyName.IndexOf('.') + 1);
        }

        private Action<TObject, TProperty> BuildActionFromExpression<TObject, TProperty>(
            Expression<Func<TObject, TProperty>> accessor)
        {
            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }

            var memberExpression = accessor.Body as MemberExpression;

            var memberInfo = memberExpression?.Member;
            if (!(memberInfo is PropertyInfo) && !(memberInfo is FieldInfo))
            {
                throw new InvalidOperationException("Member is not a property or field");
            }

            var valueParameter = Expression.Parameter(typeof(TProperty), "val");
            var assignmentExpression = Expression.Assign(memberExpression, valueParameter);
            var lambdaExpression =
                Expression.Lambda<Action<TObject, TProperty>>(
                    assignmentExpression,
                    accessor.Parameters[0],
                    valueParameter);

            return lambdaExpression.Compile();
        }
    }
}
