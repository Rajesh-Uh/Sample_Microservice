using Precision.Core.Models.ChatService;
using Precision.Data.Context;
using Precision.Contracts.Response.ChatService;
using Precision.Providers.Interfaces.ChatService;
using Microsoft.EntityFrameworkCore;
using Precision.Core.Exceptions;
using Precision.WebApi.PagingHelper;
using Precision.WebApi;
using Precision.Contracts.Request;
using Precision.Providers.Helpers;

namespace Precision.Providers.Services.ChatService
{
    public class ChatGroupProvider : IChatGroupService
    {
        private readonly PrecisionDbContext _context;
        private readonly IRequestProfile _requestProfile;

        public ChatGroupProvider(PrecisionDbContext context, IRequestProfile requestProfile)
        {
            _context = context;
            _requestProfile = requestProfile;
        }

        public async Task CreateChatGroup(long adminId, long userId, long webUserId, long shiftId)
        {
            var createChatGroup = new ChatGroup()
            {
                ShiftId = shiftId,
                AdminUserId = adminId,
                MobileUserId = userId,
                WebUserId = webUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Add(createChatGroup);
            await _context.SaveChangesAsync();
        }
        public async Task<PagingResult<ChatGroupResponse>> GetChatGroups(string role, long userId, GetRequest getRequest)
        {
            var result = await _context.ChatGroups
                .Where(c => (role.Equals(Constants.HOSPITAL) || role.Equals(Constants.SURGICAL_ROOM))
                    ? c.WebUserId == userId
                    : (role.Equals("Admin")
                        ? c.AdminUserId == userId
                        : c.MobileUserId == userId))
                .Select(c => new ChatGroupResponse()
                {
                    Id = c.Id,
                    ShiftId = c.ShiftId,
                    Shift_HospitalName = c.Shift.HospitalName,
                    Shift_Date = c.Shift.Date.ToString("dd/MM/yyyy"),
                    Shift_FromTime = c.Shift.FromTime.ToString("hh:mm:ss tt"),
                    Shift_ToTime = c.Shift.ToTime.ToString("hh:mm:ss tt"),
                    AdminUserId = c.AdminUserId,
                    MobileUserId = c.MobileUserId,
                    MobileUserName = c.User.UserProfiles.FirstOrDefault().FullName.ToString(),
                    CoverType = _context.CoverTypes.Where(x => x.Id == _context.UserProfiles.Where(x => x.Id == c.MobileUserId).Select(x => x.CoverTypeId).FirstOrDefault()).Select(x => x.Type).FirstOrDefault(),
                    WebUserId = c.WebUserId,
                }
            ).ApplyPaging(_requestProfile.Pagination, out var pagingMetadata)
                .ToListAsync();

            return new PagingResult<ChatGroupResponse>(pagingMetadata, result);
        }
        public async Task DeleteChatGroup(long chatGroupId)
        {
            var deleteChatGroup = await _context.ChatGroups.FirstOrDefaultAsync(x => x.Id == chatGroupId);

            if (deleteChatGroup == null)
            {
                throw new DatabaseObjectNotFoundException("ChatGroups", "Id", chatGroupId.ToString());
            }

            var chats = await _context.Chats.Where(x => x.ChatGroupId == chatGroupId).ToListAsync();

            foreach (var chat in chats)
            {
                chat.IsDeleted = true;
                chat.UpdatedAt = DateTime.Now;
            }
            deleteChatGroup.IsDeleted = true;
            deleteChatGroup.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}