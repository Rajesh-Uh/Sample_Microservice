using System.Linq.Expressions;

namespace Precision.WebApi.Util
{
    internal static class PatchUtil
    {
        internal static string GetPropertyName<TRequest, TProperty>(Expression<Func<TRequest, TProperty>> propertyExpression)
        {
            if (!(propertyExpression.Body is MemberExpression propertyBody))
            {
                throw new InvalidCastException($"Cannot get property name from {nameof(propertyExpression)}.");
            }

            var fullPropertyName = propertyBody.ToString();
            return fullPropertyName.Substring(fullPropertyName.IndexOf('.') + 1);
        }
    }
}
