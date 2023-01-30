using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Precision.Core.Models.UserService;

namespace Precision.Core.Models.ChatService
{
    public class Chat
    {
        [Key]
        [Required]
        public long Id { get; set; }
        [Required,ForeignKey(nameof(ChatGroupId))]
        public long ChatGroupId { get; set;}
        [Required,ForeignKey(nameof(UserId))]
        public long UserId{ get; set; }
        [Required,MaxLength(256)]
        public string Message { get; set; }
        public DateTime CreatedAt{ get; set; }
        public DateTime UpdatedAt{ get; set; }
        public bool IsDeleted { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        public bool IsRead { get; set;}

    }
}