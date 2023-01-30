using System.ComponentModel.DataAnnotations;

namespace Precision.Contracts.Request.ChatService
{
    public class ChatCreateRequest
    {
        [Required]
        public string Message { get; set; }
    }
}