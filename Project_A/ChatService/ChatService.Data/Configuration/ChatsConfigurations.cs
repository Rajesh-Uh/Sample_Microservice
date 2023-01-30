using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precision.Core.Models.ChatService;

namespace Precision.Data.Configurations
{
    public class ChatConfigurations : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            builder.HasData(
                new Chat()
                {
                    Id = 1,
                    ChatGroupId = 1,
                    UserId = 1,
                    Message = "hii hospital and nurse",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false,
                    IsRead = false
                },
                    new Chat()
                    {
                        Id = 2,
                        ChatGroupId = 1,
                        UserId = 2,
                        Message = "hii admin & nurse",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsDeleted = false,
                        IsRead = false
                    },
                    new Chat()
                    {
                        Id = 3,
                        ChatGroupId = 1,
                        UserId = 3,
                        Message = "hii admin & Hospital",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsDeleted = false,
                        IsRead = false
                    }
                );
        }
    }
}


















