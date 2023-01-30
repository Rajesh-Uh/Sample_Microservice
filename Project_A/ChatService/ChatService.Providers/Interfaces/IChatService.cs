using Precision.Contracts.Response.ChatService;
using Precision.Contracts.Request.ChatService;

namespace Precision.Providers.Interfaces.ChatService
{
    public interface IChatService
    {
        Task<IEnumerable<ChatResponse>> GetAllChats(long chatGroupId);
        Task<ChatResponse> CreateChat(long chatGroupId, long userId, ChatCreateRequest chatCreateRequest);
        Task DeleteChat(long chatGroupId, long chatId);
        Task<ChatResponse> UpdateChat(long chatGroupId, long chatId, ChatPatchRequest chatPatchRequest);
        Task<ChatResponse> GetChatById(long chatGroupId, long chatId);
    }
}