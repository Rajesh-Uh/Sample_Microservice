using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Precision.Core.Models.ShiftService;
using Precision.Core.Models.UserService;

namespace Precision.Core.Models.ChatService
{
    public class ChatGroup
    {
        [Key]
        public long Id { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public long ShiftId { get; set; }
        public long AdminUserId { get; set; }
        public long MobileUserId { get; set; }
        public long WebUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        [ForeignKey(nameof(MobileUserId))]
        public virtual User User { get; set; }
        [ForeignKey(nameof(ShiftId))]
        public virtual Shift Shift { get; set; }

    }
}