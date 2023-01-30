using Precision.WebApi.PagingHelper;

namespace Precision.WebApi
{
    /// <summary>
    /// Interface to read Request Profile 
    /// </summary>
    public interface IRequestProfile
    {
        /// <summary>
        /// Application Tenant Id
        /// </summary>
        public long TenantId { get; }
        /// <summary>
        /// Loggedin User Id
        /// </summary>
        public long UserId { get; }
        /// <summary>
        /// List of User Permissions
        /// </summary>
        public IEnumerable<string> Permissions { get; }
        /// <summary>
        /// Impersonator User Id
        /// </summary>
        public Guid? ImpersonatorUserId { get; }
        /// <summary>
        /// Base Url
        /// </summary>
        public string BaseUrl { get; }
        /// <summary>
        /// Pagination
        /// </summary>
        public Pagination Pagination { get; }

    }
}
