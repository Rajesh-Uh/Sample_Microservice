using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Primitives;
using Precision.WebApi.PagingHelper;
using Precision.WebApi.Interface;
using FluentValidation;
using FluentValidation.Results;

namespace Precision.WebApi
{
    public class WebApiHelper : IWebApiHelper
    {

        
        private readonly IRequestProfile _requestProfile;
        public WebApiHelper(IRequestProfile requestProfile)
        {
            _requestProfile = requestProfile;
        }
        /// <summary>
        /// Read and deserialize the request body, also reads the request headers and populates request profile(TenantId,UserId,Permissions,ImpersonatorUserId,BaseUrl and Pagination) as an optimization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="req">The HttpRequest</param>
        /// <returns></returns>
        public async Task<T> ReadRequestBody<T>(HttpRequest req)
        {
            PopulateRequestProfile(req);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<T>(requestBody);

            if (IsPatchRequest(model))
            {
                ExecutePatchMiddleware(requestBody, (IEasyPatchModel)model);
            }
            else
            {
                ExecuteMiddleware(model);
            }

            return model;
        }

        private void ExecutePatchMiddleware<T>(string requestBody, T patchModel) where T : IEasyPatchModel
        {
            var dictionary = JObject.Parse(requestBody);

            foreach (var kvp in dictionary)
            {
                patchModel.AddBoundProperty(kvp.Key);
            }

            var map = GetPatchMappers(patchModel.GetType());
            patchModel.AddMap(map);

            var validationResult = patchModel.Validate();
            if (validationResult.Any())
            {
                throw new InvalidDataException(string.Join(Environment.NewLine, validationResult.Select(x => $"{x.Key} : {x.Value}")));
            }
        }

        private bool IsPatchRequest<T>(T model)
        {
            return typeof(IEasyPatchModel).IsAssignableFrom(model.GetType());
        }


        private void ExecuteMiddleware<T>(T model)
        {
            var modelType = model.GetType();

            var validators = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => GetTypesForAssembly(x))
                .Where(x => typeof(IValidator).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Where(x => x.BaseType.GenericTypeArguments[0] == modelType).ToList();

            if (!validators.Any()) // if validators are not implemented, nothing to validate  
            {
                return;
            }

            if (validators.Count() > 1) // if multiple validators are implemented, throw exception 
            {
                throw new InvalidOperationException($"For '{modelType}', only 1 validator is expected, however found {validators.Count()} validators");
            }

            var validator = (IValidator<T>)Activator.CreateInstance(validators.Single());
            ValidationResult validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
            {
                throw new InvalidDataException(validationResult.ToString(Environment.NewLine));
            }
        }



        private IPatchMap GetPatchMappers(Type type)
        {
            var patchMapperType = typeof(IPatchMap);
            var map = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => GetTypesForAssembly(x))
                .Where(x => patchMapperType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Where(x => x.BaseType.GenericTypeArguments[0] == type)
                .Select(x => (IPatchMap)Activator.CreateInstance(x))
                .Single();
            return map;
        }
        private IEnumerable<Type> GetTypesForAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        /// <summary>
        /// Read and deserialize the request query parameters for GET requests, also reads the request headers and populates request profile(TenantId,UserId,Permissions,ImpersonatorUserId,BaseUrl and Pagination) as an optimization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="req">The HttpRequest</param>
        /// <returns></returns>
        public T ReadRequestQuery<T>(HttpRequest req) where T : new()
        {
            PopulateRequestProfile(req);

            var query = req.Query;
            var classType = typeof(T);
            var response = (T)Activator.CreateInstance(classType);

            foreach (var q in query)
            {
                var prop = classType.GetProperty(q.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var stringVal = q.Value.ToString();
                if (prop != null && !string.IsNullOrWhiteSpace(stringVal))
                {
                    var propType = prop.PropertyType;
                    if (propType == typeof(string) || propType.IsPrimitive)
                    {
                        var val = Convert.ChangeType(stringVal, propType);
                        prop.SetValue(response, val);
                    }
                    else if (propType.IsValueType)
                    {
                        var elementType = propType.GenericTypeArguments[0];
                        var val = Convert.ChangeType(stringVal, elementType);
                        prop.SetValue(response, val);
                    }
                    else if (propType.IsSerializable || propType.Name.StartsWith("IEnumerable") || propType.Name.StartsWith("ICollection"))
                    {
                        Type elementType = propType.GetElementType();
                        if (elementType == null)
                        {
                            elementType = propType.GenericTypeArguments[0];
                        }
                        if (elementType == typeof(string) || elementType.IsPrimitive)
                        {
                            MethodInfo methodDefinition;
                            if (propType.Name.StartsWith("List"))
                            {
                                methodDefinition = typeof(ParseHelper).GetMethod("ParseToList");
                            }
                            else
                            {
                                methodDefinition = typeof(ParseHelper).GetMethod("ParseToArray");
                            }
                            MethodInfo method = methodDefinition.MakeGenericMethod(elementType);
                            var val = method.Invoke(null, new object[] { stringVal });
                            prop.SetValue(response, val);
                        }
                    }
                }
            }
            return response;
        }

        /// <summary>
        /// Read pagination data from the request query parameters
        /// </summary>
        /// <param name="req">The HttpRequest</param>
        /// <returns></returns>
        public Pagination ReadPagination(HttpRequest req)
        {
            return req.GetPagination();
        }

        /// <summary>
        /// Adds pagination result metadata to the response headers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="req">The HttpRequest</param>
        /// <param name="pagingResult"></param>
        public void AddPagingHeaders<T>(HttpRequest req, PagingResult<T> pagingResult)
        {
            req.HttpContext.Response.Headers.Add("TotalCount", new StringValues(pagingResult.Metadata.TotalCount.ToString()));
            req.HttpContext.Response.Headers.Add("TotalPages", new StringValues(pagingResult.Metadata.TotalPages.ToString()));
        }

        /// <summary>
        /// Populates Request Profile with TenantId,UserId,Permissions,ImpersonatorUserId,BaseUrl and Pagination
        /// </summary>
        /// <param name="req"></param>
        public void PopulateRequestProfile(HttpRequest req)
        {
            var _requestSetProfile = (RequestProfile)_requestProfile;
            _requestSetProfile.TenantId = req.GetTenantId();
            _requestSetProfile.UserId = req.GetUserId();
            _requestSetProfile.Permissions = req.GetPermissions();
            _requestSetProfile.ImpersonatorUserId = req.GetImpersonatorUserId();
            _requestSetProfile.BaseUrl = req.GetBaseUrl();
            _requestSetProfile.Pagination = req.GetPagination();
        }
        
    }

}