using Precision.WebApi.PagingHelper;

namespace Precision.WebApi
{
    public interface IWebApiHelper
    {
        Task<T> ReadRequestBody<T>(HttpRequest req);
        T ReadRequestQuery<T>(HttpRequest req) where T : new();
        Pagination ReadPagination(HttpRequest req);
        void AddPagingHeaders<T>(HttpRequest req, PagingResult<T> pagingResult);
        void PopulateRequestProfile(HttpRequest req);
    }

}