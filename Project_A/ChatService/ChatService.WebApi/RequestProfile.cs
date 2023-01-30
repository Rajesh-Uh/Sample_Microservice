using Precision.WebApi.PagingHelper;

namespace Precision.WebApi
{
    /// <inheritdoc/>
    public class RequestProfile : IRequestProfile
    {
        public long TenantId { get; internal set; }
        public long UserId { get; internal set; }
        public IEnumerable<string> Permissions { get; internal set; }
        public Guid? ImpersonatorUserId { get; internal set; }
        public string BaseUrl { get; internal set; }
        public Pagination Pagination { get; internal set; }
    }
}
