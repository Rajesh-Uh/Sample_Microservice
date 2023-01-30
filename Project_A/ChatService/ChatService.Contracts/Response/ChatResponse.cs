namespace Precision.Contracts.Response.ChatService
{
    public class ChatResponse
    {
        public long Id { get; set; }
        public long ChatGroupId { get; set; }
        public long UserId { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
    }
}