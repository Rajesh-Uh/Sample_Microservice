using Precision.WebApi.Interface;
using FluentValidation;
using System.Linq.Expressions;

namespace Precision.WebApi.Implementation
{
    public abstract class AbstractPatchValidator<T> : AbstractValidator<T>
    where T : IEasyPatchModel<T>
    {
        public void WhenBound<TProperty>(
            Expression<Func<T, TProperty>> propertyExpression,
            Action<IRuleBuilderInitial<T, TProperty>> action)
        {
            When(x => x.IsBound(propertyExpression), () => action(RuleFor(propertyExpression)));
        }
    }
}
