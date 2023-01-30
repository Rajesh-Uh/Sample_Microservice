using System.Linq.Expressions;
using Precision.WebApi.Interface;
using Precision.WebApi.Util;

namespace Precision.WebApi.Implementation
{
    public abstract class EasyPatchModelMapBase<TRequest> : IEasyPatchModel<TRequest>
        where TRequest : class, IEasyPatchModel<TRequest>, new()
    {
        public EasyPatchModelMapBase() { }
        public EasyPatchModelMapBase(AbstractPatchValidator<TRequest> validator)
        {
            _validator = validator;
        }

        protected AbstractPatchValidator<TRequest> _validator;
        protected readonly IList<string> boundProperties = new List<string>();
        protected IPatchMap requestModelMap;

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

        public bool IsBound<TProperty>(Expression<Func<TRequest, TProperty>> propertyExpression)
        {
            var propertyName = PatchUtil.GetPropertyName(propertyExpression);

            return boundProperties.Contains(propertyName, StringComparer.InvariantCultureIgnoreCase);
        }

        public void Patch<TModel>(TModel model)
        {
            var boundPropertyMappings = requestModelMap.GetMap()
                .Where(x => boundProperties.Contains(x.Key, StringComparer.InvariantCultureIgnoreCase))
                .Select(x => x.Value);
            foreach (var assignProperty in boundPropertyMappings)
            {
                assignProperty(this, model);
            }
        }

        protected virtual IEnumerable<KeyValuePair<string, string>> GetValidationErrors(TRequest request)
        {
            return _validator != null ? _validator.Validate(request).Errors.Select(z => new KeyValuePair<string, string>(z.PropertyName, z.ErrorMessage)) : Enumerable.Empty<KeyValuePair<string, string>>();
        }

        public abstract IEnumerable<KeyValuePair<string, string>> Validate();

        public void AddMap(IPatchMap map)
        {
            requestModelMap = map;
        }
    }
}
