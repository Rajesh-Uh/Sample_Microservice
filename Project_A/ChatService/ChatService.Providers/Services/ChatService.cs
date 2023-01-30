using Precision.Core.Models.ChatService;
using Precision.Data.Context;
using Precision.Contracts.Response.ChatService;
using Precision.Contracts.Request.ChatService;
using Precision.Providers.Interfaces.ChatService;
using Precision.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Precision.Providers.Interfaces.NotificationService;
using Precision.Providers.Helpers;
using Precision.Core.Enums.UserService;

namespace Precision.Providers.Services.ChatService
{
    public class ChatProvider : IChatService
    {
        private readonly PrecisionDbContext _context;
        private readonly IMailService _mailService;
        private readonly IConfiguration _config;
        private readonly IPushNotificationService _pushNotificationService;
        public ChatProvider(PrecisionDbContext context, IMailService mailService, IConfiguration config, IPushNotificationService pushNotificationService)
        {
            _context = context;
            _mailService = mailService;
            _config = config;
            _pushNotificationService = pushNotificationService;
        }

        public async Task<ChatResponse> CreateChat(long chatGroupId, long userId, ChatCreateRequest chatCreateRequest)
        {
            var message = string.Empty;
            var email = string.Empty;

            var chatGroupExist = await _context.ChatGroups.FirstOrDefaultAsync(x => x.Id == chatGroupId);

            if (chatGroupExist == null)
            {
                throw new DatabaseObjectNotFoundException("ChatGroups", "Id", chatGroupId.ToString());
            }

            var createChat = new Chat()
            {
                UserId = userId,
                Message = chatCreateRequest.Message,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ChatGroupId = chatGroupId,
                IsRead = false,
            };

            _context.Add(createChat);

            await _context.SaveChangesAsync();

            var shiftDetail = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == chatGroupExist.ShiftId);

            message = $" You obtained a new Chat message for the Shift with Hospital name  : {shiftDetail.HospitalName}";

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);

            var userType = await _context.UserTypes.Where(x => x.Id == user.UserTypeId).Select(x => x.Type).FirstOrDefaultAsync();
            if (user.PlatformId == (int)UserPlatform.Mobile)
            {
                var userTypeId = await _context.MobileUserTypes.Where(x => x.Id == user.UserTypeId).Select(x => x.UserTypeId).FirstOrDefaultAsync();
                userType = await _context.UserTypes.Where(x => x.Id == userTypeId).Select(x => x.Type).FirstOrDefaultAsync();
            }
            else
            {
                var mobileUser = await _context.Users.FirstOrDefaultAsync(x => x.Id == chatGroupExist.MobileUserId);
                var deviceId = mobileUser.DeviceId;
                var devicePlatform = mobileUser.DevicePlatform;
                var title = _config["NewChatCreation:Title"];
                var body = _config["NewChatCreation:Body1"];

                await _pushNotificationService.SendPushNotification(deviceId, devicePlatform, title, body);
            }

            if (userType != Constants.HOSPITAL && userType != Constants.SURGICAL_ROOM && chatGroupExist.WebUserId != byte.MinValue)
            {
                //Sending Email Notification to Web User(Hospital or Surgical Room)
                email = await _context.Users.Where(x => x.Id == chatGroupExist.WebUserId).Select(x => x.Email).FirstOrDefaultAsync();
                await _mailService.SendMailAsHtml(email, message, _config["NewChatCreation:Subject"]);
            }

            if (userType != Constants.ADMIN)
            {
                //Sending Email Notification to Admin
                email = await _context.Users.Where(x => x.Id == Constants.ADMIN_ID).Select(x => x.Email).FirstOrDefaultAsync();
                await _mailService.SendMailAsHtml(email, message, _config["NewChatCreation:Subject"]);
            }

            return await GetChatById(createChat.ChatGroupId, createChat.Id);
        }

        public async Task<IEnumerable<ChatResponse>> GetAllChats(long chatGroupId)
        {
            var chatGroupExist = await _context.ChatGroups.FirstOrDefaultAsync(x => x.Id == chatGroupId);

            if (chatGroupExist == null)
            {
                throw new DatabaseObjectNotFoundException("ChatGroups", "Id", chatGroupId.ToString());
            }

            return await _context.Chats
                .Where(x => x.ChatGroupId == chatGroupId)
                .Select(chats => new ChatResponse()
                {
                    Id = chats.Id,
                    ChatGroupId = chats.ChatGroupId,
                    UserId = chats.UserId,
                    SenderName = chats.User.UserProfiles.FirstOrDefault().FullName,
                    Message = chats.Message,
                    IsRead = chats.IsRead,
                }
            ).ToListAsync();
        }

        public async Task DeleteChat(long chatGroupId, long chatId)
        {
            var deleteChat = await _context.Chats.FirstOrDefaultAsync(x => x.ChatGroupId == chatGroupId && x.Id == chatId);

            if (deleteChat == null)
            {
                throw new DatabaseObjectNotFoundException("Chat", "Id", chatId.ToString());
            }

            deleteChat.IsDeleted = true;
            deleteChat.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task<ChatResponse> GetChatById(long chatGroupId, long chatId)
        {
            var getChat = await _context.Chats.FirstOrDefaultAsync(x => x.ChatGroupId == chatGroupId && x.Id == chatId);

            if (getChat == null)
            {
                throw new DatabaseObjectNotFoundException("Chat", "Id", chatId.ToString());
            }

            return await _context.Chats
                .Select(chat => new ChatResponse()
                {
                    Id = chat.Id,
                    ChatGroupId = chat.ChatGroupId,
                    UserId = chat.UserId,
                    SenderName = chat.User.UserProfiles.FirstOrDefault().FullName,
                    Message = chat.Message,
                    IsRead = chat.IsRead,
                }
            ).FirstOrDefaultAsync(x => x.ChatGroupId == chatGroupId && x.Id == chatId);
        }
        public async Task<ChatResponse> UpdateChat(long chatGroupId, long chatId, ChatPatchRequest chatPatchRequest)
        {
            var updateChat = await _context.Chats.Where(x => x.ChatGroupId == chatGroupId).ToListAsync();

            if (updateChat == null)
            {
                throw new DatabaseObjectNotFoundException("Chats", "Id", chatId.ToString());
            }

            foreach (var chat in updateChat)
            {
                chatPatchRequest.Patch(chat);
            }

            await _context.SaveChangesAsync();

            return await GetChatById(chatGroupId, chatId);
        }
    }
}