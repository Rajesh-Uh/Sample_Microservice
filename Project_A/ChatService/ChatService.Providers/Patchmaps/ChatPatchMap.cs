using Precision.WebApi.Implementation;
using Precision.Contracts.Request.ChatService;
using Precision.Core.Models.ChatService;

namespace Precision.Providers.PatchMaps
{
    public class ChatPatchMap : PatchMapBase<ChatPatchRequest, Chat>
    {
        public ChatPatchMap()
        {
            AddPatchStateMapping(x => x.IsRead, x => x.IsRead);
        }
    }
}
