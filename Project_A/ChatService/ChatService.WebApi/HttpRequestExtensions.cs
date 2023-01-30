using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Precision.Core.Exceptions;
using Precision.WebApi.PagingHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Precision.WebApi
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Gets the list of user permissions
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTokenInformationException"></exception>
        public static IEnumerable<string> GetPermissions(this HttpRequest request)
        {
            string permissionsHeader = request.Headers["Permissions"];
            if (permissionsHeader is null)
            {
                return null;
                //throw new InvalidTokenInformationException("Essential permissions header information is missing");
            }
            permissionsHeader = permissionsHeader.Trim();

            if (permissionsHeader.Equals(string.Empty))
            {
                return new List<string>();
            }
            return permissionsHeader.Split(',').Select(x => x.Trim());
        }

        /// <summary>
        /// Gets the Application Tenant Id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTokenInformationException"></exception>
        public static long GetTenantId(this HttpRequest request)
        {
            var tenantHeader = request.Headers["TenantId"];
            if (string.IsNullOrWhiteSpace(tenantHeader))
            {
                return long.MaxValue;
                //throw new InvalidTokenInformationException("Essential tenant header information is missing");
            }

            if (!long.TryParse(tenantHeader, out var tenantId))
            {
                throw new InvalidTokenInformationException($"Invalid TenantId {tenantHeader}");
            }
            return tenantId;

        }

        /// <summary>
        /// Gets the logged in User Id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTokenInformationException"></exception>
        public static long GetUserId(this HttpRequest request)
        {
            var userHeader = request.Headers["UserId"];
            if (string.IsNullOrWhiteSpace(userHeader))
            {
                return long.MinValue;
                //throw new InvalidTokenInformationException("Essential user information missing");
            }
            if (!long.TryParse(userHeader, out var userId))
            {
                throw new InvalidTokenInformationException($"Invalid UserId {userHeader}");
            }
            return userId;

        }
        /// <summary>
        /// Gets the Impersonator User Id
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidTokenInformationException"></exception>
        public static Guid? GetImpersonatorUserId(this HttpRequest request)
        {
            var impersonatorUserHeader = request.Headers["ImpersonatorUserId"];
            if (string.IsNullOrWhiteSpace(impersonatorUserHeader))
            {
                return null;
            }
            if (!Guid.TryParse(impersonatorUserHeader, out var impersonatorUserId))
            {
                throw new InvalidTokenInformationException($"Invalid Impersonator UserId {impersonatorUserHeader}");
            }
            return impersonatorUserId;
        }

        /// <summary>
        /// Gets the base url of the request
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static string GetBaseUrl(this HttpRequest req)
        {
            return $"{req.Scheme}://{req.Host.Value}/api";
        }

        /// <summary>
        /// Gets pagination data from the request query parameters
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static Pagination GetPagination(this HttpRequest req)
        {
            var pagination = new Pagination();
            if (req.Query.TryGetValue("PageSize", out var pageSizeString))
            {
                pagination.PageSize = int.Parse(pageSizeString);
            }
            if (req.Query.TryGetValue("PageOffset", out var pageOffsetString))
            {
                pagination.PageOffset = int.Parse(pageOffsetString);
            }
            if (req.Query.TryGetValue("OrderBy", out var orderByString))
            {
                pagination.OrderBy = orderByString.ToString();
            }
            if (req.Query.TryGetValue("Descending", out var descendingString))
            {
                pagination.Descending = bool.Parse(descendingString);
            }
            if (req.Query.TryGetValue("IncludeTotal", out var includeTotalString))
            {
                pagination.IncludeTotal = bool.Parse(includeTotalString);
            }
            if (req.Query.TryGetValue("Query", out var queryString))
            {
                pagination.Query = queryString.ToString();
            }

            pagination.BaseUrl = GetBaseUrl(req);
            return pagination;
        }

        /// <summary>
        /// Unpacks a <see cref="PagingResult{T}"/> and sends the total count and total pages as header values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="req"></param>
        /// <param name="pagingResult"></param>
        /// <returns></returns>
        public static OkObjectResult ToPagingObjectResult<T>(this HttpRequest req, PagingResult<T> pagingResult)
        {
            req.HttpContext.Response.Headers.Add("TotalCount", new Microsoft.Extensions.Primitives.StringValues(pagingResult.Metadata.TotalCount.ToString()));
            req.HttpContext.Response.Headers.Add("TotalPages", new Microsoft.Extensions.Primitives.StringValues(pagingResult.Metadata.TotalPages.ToString()));

            return new OkObjectResult(pagingResult.Result);
        }

        /// <summary>
        /// Creates a standardized error response object from a thrown exception and logs the exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns>ObjectResult</returns>
        public static ObjectResult CreateErrorResponseObject(this Exception e, ILogger log)
        {
            log?.LogError(e, e.Message);
            return CreateErrorResponseObject(e);
        }

        /// <summary>
        /// Creates a standardized error response object from a thrown exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns>ObjectResult</returns>
        public static ObjectResult CreateErrorResponseObject(this Exception e)
        {
            switch (e.GetType().Name)
            {
                case nameof(ConcurrencyException):
                case nameof(UniqueKeyException):
                    return new ConflictObjectResult(CreateErrorResponse(e));

                case nameof(DatabaseObjectNotFoundException):
                    return new NotFoundObjectResult(CreateErrorResponse(e));

                case nameof(InsufficientPermissionException):
                case nameof(InvalidTokenInformationException):
                case nameof(PasswordActionException):
                case nameof(PasswordMismatchException):
                case nameof(PasswordRestrictionException):
                case nameof(PasswordUnknownException):
                case nameof(ResetCodeMismatchException):
                case nameof(UserDeactivatedException):
                
                case nameof(InvalidEnumArgumentException):
                case nameof(InvalidDataException):
                case nameof(InvalidImageDataException):
                    return new BadRequestObjectResult(CreateErrorResponse(e));

                case nameof(InvalidStateException):
                    return new UnprocessableEntityObjectResult(CreateErrorResponse(e));

                default: throw new Exception("Unexpected Error", e);
            }
        }

        /// <summary>
        /// Creates a standardized error response from a thrown exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static ErrorDetails CreateErrorResponse(Exception e)
        {
            return CreateErrorResponse(e.Message);
        }
        /// <summary>
        /// Creates a standardized error response from a string
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static ErrorDetails CreateErrorResponse(string e)
        {
            return new ErrorDetails { ErrorBody = e };

        }
    }
}
