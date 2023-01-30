using System.Text.Json.Serialization;

namespace Precision.Contracts.Response.ChatService
{
    public class ChatGroupResponse
    {
        public long Id { get; set; }
        public long ShiftId { get; set; }
        public string Shift_HospitalName { get; set; }
        public string Shift_Date { get; set; }
        public string Shift_FromTime { get; set; }
        public string Shift_ToTime { get; set; }
        public long AdminUserId { get; set; }
         [Newtonsoft.Json.JsonProperty("0")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long WebUserId { get; set; }
        public long MobileUserId { get; set; }
        public string MobileUserName { get; set; }
        public string CoverType { get; set; }
    }
}