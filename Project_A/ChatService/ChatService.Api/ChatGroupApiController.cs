using Microsoft.AspNetCore.Mvc;
using Precision.Providers.Interfaces.ChatService;
using Precision.WebApi;
using System.Net;
using Precision.Contracts.Response;
using Precision.Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Precision.Contracts.Request;

namespace Precision.Api.ChatService
{
    ///<Summary>
    /// ChatGroup controller layer
    ///</Summary>
    [Route("api/chatService")]
    [ApiController]
    public class ChatGroupApiController : ControllerBase
    {
        private readonly ILogger<ChatGroupApiController> _logger;
        private readonly IChatGroupService _chatGroupService;
        private readonly IWebApiHelper _webApiHelper;

        ///<Summary>
        /// ChatGroup controller layer contructor
        ///</Summary>

        public ChatGroupApiController(ILogger<ChatGroupApiController> log,
            IChatGroupService chatGroupService,
            IWebApiHelper webApiHelper)
        {
            _logger = log;
            _chatGroupService = chatGroupService;
            _webApiHelper = webApiHelper;
        }

        ///<Summary>
        /// Delete ChatGroup Based on ID API
        ///</Summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("chatGroups/{chatGroupId}")]
        public async Task<IActionResult> DeleteChatGroupById(long chatGroupId)
        {
            _logger.LogInformation($"received {nameof(DeleteChatGroupById)} request");

            try
            {
                await _chatGroupService.DeleteChatGroup(chatGroupId);
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
        /// Get All ChatGroups API
        ///</Summary>

        [Authorize]
        [HttpGet("chatGroups")]
        public async Task<IActionResult> GetAllChatGroups()
        {
            _logger.LogInformation($"received {nameof(GetAllChatGroups)} request");

            var role = Auth.JwtHelper.JwtUtils.decodeJwt(HttpContext.Request.Headers["Authorization"]).role;

            var userId = Auth.JwtHelper.JwtUtils.decodeJwt(HttpContext.Request.Headers["Authorization"]).userData;

            GetRequest getRequest = _webApiHelper.ReadRequestQuery<GetRequest>(HttpContext.Request);

            var pagedResult = await _chatGroupService.GetChatGroups(role, Convert.ToInt64(userId), getRequest);

            return HttpContext.Request.ToPagingObjectResult(pagedResult);
        }
    }
}