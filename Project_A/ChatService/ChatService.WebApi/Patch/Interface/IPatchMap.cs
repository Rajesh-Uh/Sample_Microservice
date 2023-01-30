using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Precision.WebApi.Interface
{
    public interface IPatchMap<TRequest, TModel> : IPatchMap
    {
        void AddPatchStateMapping<TProperty, TModelProperty>(
           Expression<Func<TRequest, TProperty>> propertyExpression,
           Expression<Func<TModel, TModelProperty>> modelMapping);
    }

    public interface IPatchMap
    {
        IDictionary<string, Action<object, object>> GetMap();
    }
}
