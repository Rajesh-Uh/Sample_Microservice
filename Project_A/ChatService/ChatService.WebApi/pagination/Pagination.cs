using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Precision.WebApi.PagingHelper
{
    public class Pagination
    {
        public int PageSize { get; set; } = 50;
        public int PageOffset { get; set; } = 0;
        public string OrderBy { get; set; }
        public bool Descending { get; set; }
        public bool IncludeTotal { get; set; }
        public string Query { get; set; }
        public string BaseUrl { get; set; }
    }

    public static class Extensions
    {
        private const string FIELD_NAME_KEY = "fieldName";
        private const string FIELD_VALUE_KEY = "fieldValues";
        private const string NEXT_OPERATOR_KEY = "nextOperator";
        private const string REMAINING_KEY = "remaining";

        private const string NULL = "null";
        private const char ASSIGNMENT_SEPARATOR = '=';
        private const char FIELD_SEPARATOR = ';';
        private const char VALUE_SEPARATOR = ',';
        private const char RANGE_SEPARATOR = '~';
        private const char STRING_LIKE_INDICATOR = '*';

        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, Pagination paging, out PagingMetadata metadata)
        {
            if (!string.IsNullOrWhiteSpace(paging.Query))
            {
                query = query.Where(CreateWhere<T>(paging.Query));
            }

            metadata = GetPagingMetadata(query, paging);

            if (!string.IsNullOrWhiteSpace(paging.OrderBy))
            {
                var orderByFields = paging.OrderBy.Split(',');
                var firstOrderBy = orderByFields.First();
                if (paging.Descending)
                {
                    query = query.OrderByDescending(firstOrderBy);
                    foreach (var orderBy in orderByFields.Skip(1))
                    {
                        query = query.ThenByDescending(orderBy);
                    }
                }
                else
                {
                    query = query.OrderBy(firstOrderBy);
                    foreach (var orderBy in orderByFields.Skip(1))
                    {
                        query = query.ThenBy(orderBy);
                    }
                }
            }
            query = query.Skip(paging.PageOffset * paging.PageSize);
            query = query.Take(paging.PageSize);
            return query;
        }

        private static Expression<Func<T, bool>> CreateWhere<T>(string query)
        {
            var regex = new Regex($@"(?<{FIELD_NAME_KEY}>[^{ASSIGNMENT_SEPARATOR}]+){ASSIGNMENT_SEPARATOR}" +
                                    $@"(?<{FIELD_VALUE_KEY}>[^{ASSIGNMENT_SEPARATOR}]+)" +
                                    $@"($|((?<{NEXT_OPERATOR_KEY}>[{FIELD_SEPARATOR}{VALUE_SEPARATOR}])(?<{REMAINING_KEY}>.*)))");
            var objectType = typeof(T);
            var input = Expression.Parameter(objectType, "x");
            var remainingQuery = query;
            var expressions = new Queue<Expression>();
            var operators = new List<char>();
            while (remainingQuery.Length > 0)
            {
                var chunk = regex.Match(remainingQuery);
                if (!chunk.Success)
                {
                    break;
                }
                var fieldName = chunk.Groups[FIELD_NAME_KEY].Value;
                var fieldValue = chunk.Groups[FIELD_VALUE_KEY].Value;
                var nextOperator = chunk.Groups[NEXT_OPERATOR_KEY];
                if (nextOperator.Success)
                {
                    operators.Add(nextOperator.Value[0]);
                }
                remainingQuery = chunk.Groups[REMAINING_KEY].Value;

                var property = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .SingleOrDefault(x => string.Equals(x.Name, fieldName, StringComparison.InvariantCultureIgnoreCase) && x.CanWrite);
                //Ignore improper fields from the request
                if (property == null)
                {
                    continue;
                }

                var values = fieldValue.Split(VALUE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    var propertyExpression = Expression.Property(input, property);
                    var propertyType = property.PropertyType;
                    expressions.Enqueue(GetOrExpression(values, propertyExpression, propertyType));
                }
                //Ignore fields which can't be converted from the request
                catch (TargetInvocationException e)
                {
                    if (e.InnerException.GetType() == typeof(FormatException))
                    {
                        continue;
                    }
                    throw;
                }
            }

            if (expressions.Count == 0)
            {
                return (Expression<Func<T, bool>>)Expression.Lambda(Expression.Constant(true), input);
            }

            var combinedExpressions = expressions.Dequeue();
            foreach (var x in operators)
            {
                if (expressions.Count == 0)
                {
                    break;
                }
                var expression = expressions.Dequeue();
                switch (x)
                {
                    case FIELD_SEPARATOR:
                        combinedExpressions = Expression.AndAlso(combinedExpressions, expression);
                        break;
                    case VALUE_SEPARATOR:
                        combinedExpressions = Expression.OrElse(combinedExpressions, expression);
                        break;
                }
            }
            return (Expression<Func<T, bool>>)Expression.Lambda(combinedExpressions, input);
        }

        private static BinaryExpression GetOrExpression(string[] values, MemberExpression propertyExpression, Type propertyType)
        {
            if (propertyType == typeof(string))
            {
                var likeValues = values.Where(x => x.StartsWith(STRING_LIKE_INDICATOR) || x.EndsWith(STRING_LIKE_INDICATOR));
                var exactValues = values.Where(x => !x.StartsWith(STRING_LIKE_INDICATOR) && !x.EndsWith(STRING_LIKE_INDICATOR));
                var stringLikeExpressions = GetLikeExpressions(likeValues, propertyExpression);
                var stringExactExpressions = GetEqualityExpressions(exactValues, propertyType, propertyExpression);
                return Expression.OrElse(stringLikeExpressions, stringExactExpressions);
            }

            var rangeExpressions = GetRangeExpressions(values.Where(x => x.Contains(RANGE_SEPARATOR)), propertyType, propertyExpression);
            var equalityExpressions = GetEqualityExpressions(values.Where(x => !x.Contains(RANGE_SEPARATOR)), propertyType, propertyExpression);
            return Expression.OrElse(rangeExpressions, equalityExpressions);
        }

        private static Expression GetLikeExpressions(IEnumerable<string> values, MemberExpression propertyExpression)
        {
            var containsExpression = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var startsWithExpression = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            var endsWithExpression = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });

            Expression orExpression = Expression.Constant(false);
            foreach (var x in values)
            {
                var positions = GetCharIndexes(x);

                var useStartsWith = positions.Contains(x.Length - 1);
                var useEndsWith = positions.Contains(0);
                var useContains = useStartsWith && useEndsWith;

                var argument = Expression.Constant(x.Replace(STRING_LIKE_INDICATOR.ToString(), string.Empty));
                if (useContains)
                {
                    orExpression = Expression.OrElse(orExpression, Expression.Call(propertyExpression, containsExpression, argument));
                }
                else if (useStartsWith)
                {
                    orExpression = Expression.OrElse(orExpression, Expression.Call(propertyExpression, startsWithExpression, argument));
                }
                else if (useEndsWith)
                {
                    orExpression = Expression.OrElse(orExpression, Expression.Call(propertyExpression, endsWithExpression, argument));
                }
                else
                {
                    orExpression = Expression.OrElse(orExpression, Expression.Equal(propertyExpression, argument));
                }
            }
            return orExpression;
        }

        private static IEnumerable<int> GetCharIndexes(string s)
        {
            var positions = Enumerable.Empty<int>();
            for (int index = s.IndexOf(STRING_LIKE_INDICATOR); index > -1; index = s.IndexOf(STRING_LIKE_INDICATOR, index + 1))
            {
                positions = positions.Append(index);
            }
            return positions;
        }

        private static Expression GetEqualityExpressions(IEnumerable<string> values, Type propertyType, MemberExpression propertyExpression)
        {
            if (!values.Any())
            {
                return Expression.Constant(false);
            }

            var equalityValues = ParseEqualityValues(values, propertyType);
            var valuesExpression = Expression.Constant(equalityValues);
            var contains = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Single(x => x.Name == "Contains" && x.GetParameters().Length == 2)
                .MakeGenericMethod(propertyType);
            return Expression.Call(contains, valuesExpression, propertyExpression);
        }

        private static object ParseEqualityValues(IEnumerable<string> values, Type propertyType)
        {
            if (propertyType == typeof(string))
            {
                return values;
            }
            var convertTarget = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var equalValues = GetParsedValues(values, convertTarget);
            var castCall = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(new[] { propertyType });
            return castCall.Invoke(null, new[] { equalValues });
        }

        private static IEnumerable<object> GetParsedValues(IEnumerable<string> values, Type convertTarget)
        {
            if (convertTarget.IsAssignableFrom(typeof(IConvertible)))
            {
                return values.Select(x => Convert.ChangeType(x, convertTarget));
            }

            //fall back to finding parse
            var parse = convertTarget.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Single(x => x.Name == "Parse" && x.GetParameters().Length == 1 && x.GetParameters().First().ParameterType == typeof(string));
            //Can't use Select as the TargetInvocationException catch won't trigger
            var parsedValues = Enumerable.Empty<object>();
            foreach (var value in values)
            {
                if (string.Equals(value, NULL, StringComparison.InvariantCultureIgnoreCase))
                {
                    parsedValues = parsedValues.Append(null);
                }
                else
                {
                    parsedValues = parsedValues.Append(parse.Invoke(null, new[] { value }));
                }
            }
            return parsedValues;
        }

        private static Expression GetRangeExpressions(IEnumerable<string> values, Type propertyType, MemberExpression propertyExpression)
        {
            var convertTarget = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (!values.Any() || !convertTarget.IsValueType)
            {
                return Expression.Constant(false);
            }

            var orExpression = (Expression)Expression.Constant(false);
            foreach (var range in values)
            {
                var split = range.Split(RANGE_SEPARATOR);
                if (split.Length != 2)
                {
                    continue;
                }

                var startValue = split[0];
                var endValue = split[1];
                var andExpression = (Expression)Expression.Constant(true);
                if (!string.IsNullOrWhiteSpace(startValue) && !string.Equals(startValue, NULL, StringComparison.InvariantCultureIgnoreCase))
                {
                    var parsedValue = GetParsedValues(new[] { startValue }, convertTarget).First();
                    var greaterThanEqual = Expression.GreaterThanOrEqual(propertyExpression, Expression.Convert(Expression.Constant(parsedValue), propertyType));
                    andExpression = Expression.AndAlso(andExpression, greaterThanEqual);
                }
                if (!string.IsNullOrWhiteSpace(endValue) && !string.Equals(endValue, NULL, StringComparison.InvariantCultureIgnoreCase))
                {
                    var parsedValue = GetParsedValues(new[] { endValue }, convertTarget).First();
                    var lessThanEqual = Expression.LessThanOrEqual(propertyExpression, Expression.Convert(Expression.Constant(parsedValue), propertyType));
                    andExpression = Expression.AndAlso(andExpression, lessThanEqual);
                }
                orExpression = Expression.OrElse(orExpression, andExpression);
            }

            return orExpression;
        }

        //https://stackoverflow.com/questions/307512/how-do-i-apply-orderby-on-an-iqueryable-using-a-string-column-name-within-a-gene
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = source.GetType().GetGenericArguments()[0];
            var property = type.GetProperty(ordering, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderBy", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = source.GetType().GetGenericArguments()[0];
            var property = type.GetProperty(ordering, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "OrderByDescending", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ThenBy<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = source.GetType().GetGenericArguments()[0];
            var property = type.GetProperty(ordering, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "ThenBy", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ThenByDescending<T>(this IQueryable<T> source, string ordering, params object[] values)
        {
            var type = source.GetType().GetGenericArguments()[0];
            var property = type.GetProperty(ordering, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);
            MethodCallExpression resultExp = Expression.Call(typeof(Queryable), "ThenByDescending", new Type[] { type, property.PropertyType }, source.Expression, Expression.Quote(orderByExp));
            return source.Provider.CreateQuery<T>(resultExp);
        }

        private static PagingMetadata GetPagingMetadata<T>(IQueryable<T> query, Pagination paging)
        {
            if (!paging.IncludeTotal)
            {
                return new PagingMetadata();
            }
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((float)totalCount / paging.PageSize);
            return new PagingMetadata
            {
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}
