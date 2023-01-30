using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Precision.Core.Models.ChatService;

namespace Precision.Data.Configurations
{
    public class ChatGroupConfigurations : IEntityTypeConfiguration<ChatGroup>
    {
        public void Configure(EntityTypeBuilder<ChatGroup> builder)
        {
            builder.HasData(
                new ChatGroup()
                {
                    Id = 1,
                    ShiftId=1,
                    AdminUserId=1,
                    MobileUserId=2,
                    WebUserId=3,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                },
                new ChatGroup()
                {
                    Id = 2,
                    ShiftId=2,
                    AdminUserId=1,
                    MobileUserId=2,
                    WebUserId=3,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                },
                new ChatGroup()
                {
                    Id = 3,
                    ShiftId=3,
                    AdminUserId=1,
                    MobileUserId=2,
                    WebUserId=3,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsDeleted = false
                }
                );
        }
    }
}

