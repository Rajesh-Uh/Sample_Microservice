using Precision.WebApi.Interface;
using System.Linq.Expressions;
using System.Reflection;
using Precision.WebApi.Util;

namespace Precision.WebApi.Implementation
{
    public abstract class EasyPatchModelBase<TRequest, TModel> : IEasyPatchModel<TRequest, TModel>
        where TRequest : class, IEasyPatchModel<TRequest, TModel>, new()
    {

        public EasyPatchModelBase() { }
        public EasyPatchModelBase(AbstractPatchValidator<TRequest> validator)
        {
            _validator = validator;
        }

        protected AbstractPatchValidator<TRequest> _validator;
        protected readonly IList<string> boundProperties = new List<string>();
        private readonly BindingFlags bindingFlags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;
        protected readonly IDictionary<string, Action<TModel>> patchStateMapping = new Dictionary<string, Action<TModel>>(StringComparer.InvariantCultureIgnoreCase);

        public void AddBoundProperty(string propertyName)
        {
            if (!boundProperties.Contains(propertyName, StringComparer.InvariantCultureIgnoreCase))
            {
                boundProperties.Add(propertyName);
            }
        }

        protected void AddBoundProperty<TProperty>(Expression<Func<TRequest, TProperty>> propertyExpression)
        {
            AddBoundProperty(PatchUtil.GetPropertyName(propertyExpression));
        }

        public void AddPatchStateMapping<TProperty, TModelProperty>(
            Expression<Func<TRequest, TProperty>> propertyExpression,
            Expression<Func<TModel, TModelProperty>> modelMapping)
        {
            var propertyName = PatchUtil.GetPropertyName(propertyExpression);

            var instanceProperty = GetType().GetProperty(propertyName, bindingFlags);

            void mappingAction(TModel model)
            {
                BuildActionFromExpression(modelMapping)(model, (TModelProperty)instanceProperty.GetValue(this, null));
            }

            AddPatchStateMapping(propertyExpression, mappingAction);
        }

        public void AddPatchStateMapping<TProperty>(
            Expression<Func<TRequest, TProperty>> propertyExpression,
            Action<TModel> propertyToModelMapping = null)
        {
            var propertyName = PatchUtil.GetPropertyName(propertyExpression);

            if (propertyToModelMapping == null)
            {
                propertyToModelMapping = (model) =>
                {
                    var modelProperty = model.GetType().GetProperty(propertyName, bindingFlags);

                    var instanceProperty = GetType().GetProperty(propertyName, bindingFlags);

                    if (modelProperty != null && instanceProperty != null)
                    {
                        modelProperty.SetValue(model, instanceProperty.GetValue(this, null), null);
                    }
                };
            }

            if (patchStateMapping.ContainsKey(propertyName))
            {
                patchStateMapping[propertyName] = propertyToModelMapping;
            }
            else
            {
                patchStateMapping.Add(propertyName, propertyToModelMapping);
            }
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

        public bool IsBound<TProperty>(Expression<Func<TRequest, TProperty>> propertyExpression)
        {
            var propertyName = PatchUtil.GetPropertyName(propertyExpression);

            return boundProperties.Contains(propertyName, StringComparer.InvariantCultureIgnoreCase);
        }

        public void Patch(TModel model)
        {
            foreach (var kvp in patchStateMapping)
            {
                if (boundProperties.Contains(kvp.Key, StringComparer.InvariantCultureIgnoreCase))
                {
                    kvp.Value(model);
                }
            }
        }

        protected virtual IEnumerable<KeyValuePair<string, string>> GetValidationErrors(TRequest request)
        {
            return _validator != null ? _validator.Validate(request).Errors.Select(z => new KeyValuePair<string, string>(z.PropertyName, z.ErrorMessage)) : Enumerable.Empty<KeyValuePair<string, string>>();
        }

        public abstract IEnumerable<KeyValuePair<string, string>> Validate();

        public void AddMap(IPatchMap map)
        {
            throw new NotImplementedException($"If a map is used, derive from {nameof(EasyPatchModelMapBase<TRequest>)} instead");
        }
    }
}
