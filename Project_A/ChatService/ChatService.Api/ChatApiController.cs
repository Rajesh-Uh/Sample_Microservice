using Microsoft.AspNetCore.Mvc;
using Precision.Contracts.Request.ChatService;
using Precision.Providers.Interfaces.ChatService;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Precision.Api.ChatService
{
    ///<Summary>
    /// Chat controller layer
    ///</Summary>
    [Authorize]
    [Route("api/chatService")]
    [ApiController]
    public class ChatApiController : ControllerBase
    {
        private readonly ILogger<ChatApiController> _logger;
        private readonly IChatService _chatService;
        long userId = 1;

        public ChatApiController(ILogger<ChatApiController> log,
            IChatService chatService)
        {
            _logger = log;
            _chatService = chatService;
        }

        ///<Summary>
        /// Create New Chat API
        ///</Summary>

        [HttpPost("chatGroups/{chatGroupId}/chats")]
        public async Task<IActionResult> CreateNewChat(long chatGroupId, ChatCreateRequest chatCreateRequest)
        {
            _logger.LogInformation($"received {nameof(CreateNewChat)} request");

            try
            {
                //var userId = Auth.JwtHelper.JwtUtils.decodeJwt(HttpContext.Request.Headers["Authorization"]).userData;

                var chat = await _chatService.CreateChat(chatGroupId, userId, chatCreateRequest);

                return new OkObjectResult(chat);
            }
            catch (DatabaseObjectNotFoundException e)
            {
                return StatusCode((int)HttpStatusCode.NotFound,
                    new GenericResponse()
                    {
                        statusCode = HttpStatusCode.NotFound,
                        success = false,
                        message = e.Message
                    }
                );
            }
        }

        ///<Summary>
        /// Delete Chat Based on ID API
        ///</Summary>

        [HttpDelete("chatGroups/{chatGroupId}/chats/{chatId}")]
        public async Task<ActionResult> DeleteChatById(long chatGroupId, long chatId)
        {
            _logger.LogInformation($"received {nameof(DeleteChatById)} request");

            try
            {
                await _chatService.DeleteChat(chatGroupId, chatId);
                return new NoContentResult();
            }
            catch (DatabaseObjectNotFoundException e)
            {
                return StatusCode((int)HttpStatusCode.NotFound,
                    new GenericResponse()
                    {
                        statusCode = HttpStatusCode.NotFound,
                        success = false,
                        message = e.Message
                    }
                );
            }
        }

        ///<Summary>
        /// Get All Chats Based on ChatGroupID API
        ///</Summary>

        [HttpGet("chatGroups/{chatGroupId}/chats")]
        public async Task<IActionResult> GetChats(long chatGroupId)
        {
            _logger.LogInformation($"received {nameof(GetChats)} request");

            try
            {
                var chats = await _chatService.GetAllChats(chatGroupId);

                return new OkObjectResult(chats);
            }
            catch (DatabaseObjectNotFoundException e)
            {
                return StatusCode((int)HttpStatusCode.NotFound,
                    new GenericResponse()
                    {
                        statusCode = HttpStatusCode.NotFound,
                        success = false,
                        message = e.Message
                    }
                );
            }
        }

        ///<Summary>
        ///Update the Read flag of the Chat
        ///</Summary>

        [HttpPatch("chatGroups/{chatGroupId}/chats/{chatId}")]
        public async Task<IActionResult> UpdateChat(long chatGroupId, long chatId)
        {
            _logger.LogInformation($"received {nameof(UpdateChat)} request");

            try
            {
                var chatPatchRequest = await _webApiHelper.ReadRequestBody<ChatPatchRequest>(HttpContext.Request);

                var updateChat = await _chatService.UpdateChat(chatGroupId, chatId, chatPatchRequest);

                return new OkObjectResult(updateChat);
            }
            catch (DatabaseObjectNotFoundException e)
            {
                return StatusCode((int)HttpStatusCode.NotFound,
                    new GenericResponse()
                    {
                        statusCode = HttpStatusCode.NotFound,
                        success = false,
                        message = e.Message
                    }
                );
            }
        }
    }
}