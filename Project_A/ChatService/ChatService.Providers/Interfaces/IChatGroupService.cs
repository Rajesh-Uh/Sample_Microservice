using Precision.Contracts.Response.ChatService;
using Precision.WebApi.PagingHelper;
using Precision.Contracts.Request;

namespace Precision.Providers.Interfaces.ChatService
{
    public interface IChatGroupService
    {
        Task<PagingResult<ChatGroupResponse>> GetChatGroups(string role, long userId,GetRequest getRequest);
        Task CreateChatGroup(long userId, long adminId, long webUserId,long shiftId);
        Task DeleteChatGroup(long chatGroupId);
    }
}