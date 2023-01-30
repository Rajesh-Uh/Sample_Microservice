using Microsoft.EntityFrameworkCore;
using Precision.Core.Enums.ShiftService;
using Precision.Core.Models.AppService;
using Precision.Core.Models.ChatService;
using Precision.Core.Models.NotificationService;
using Precision.Core.Models.ShiftService;
using Precision.Core.Models.UserService;
using Precision.Data.Configurations;

namespace Precision.Data.Context
{
    public class PrecisionDbContext : DbContext
    {
        public PrecisionDbContext(DbContextOptions<PrecisionDbContext> opt) : base(opt)
        {

        }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftDetail> ShiftDetails { get; set; }
        public DbSet<JobFor> JobFors { get; set; }
        public DbSet<ShiftUserRequest> ShiftUserRequests { get; set; }
        public DbSet<ShiftFeedback> ShiftFeedbacks { get; set; }
        public DbSet<UserTotalHours> UserTotalHours { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserProfile> UserProfiles { get; set; }
        public virtual DbSet<UserType> UserTypes { get; set; }
        public virtual DbSet<CoverType> CoverTypes { get; set; }
        public virtual DbSet<MobileUserType> MobileUserTypes { get; set; }
        public virtual DbSet<Group> Groups { get; set; }
        public virtual DbSet<GroupMember> GroupMembers { get; set; }
        public virtual DbSet<Notification> Notifications { get; set; }
        public virtual DbSet<AppVersion> AppVersions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Chat>().HasBaseType((Type)null).HasQueryFilter(ct => !ct.IsDeleted);
            modelBuilder.Entity<ChatGroup>().HasBaseType((Type)null).HasQueryFilter(ct => !ct.IsDeleted);
            modelBuilder.Entity<Shift>().HasBaseType((Type)null).HasQueryFilter(ct => !ct.IsDeleted && ct.StatusId != (int)ShiftStatus.Reject);
            modelBuilder.Entity<ShiftDetail>().HasBaseType((Type)null).HasQueryFilter(sd => !sd.IsDeleted);
            modelBuilder.Entity<JobFor>().HasBaseType((Type)null).HasQueryFilter(sd => !sd.IsDeleted);
            modelBuilder.Entity<ShiftFeedback>().HasBaseType((Type)null).HasQueryFilter(sd => !sd.IsDeleted);
            modelBuilder.Entity<ShiftUserRequest>().HasBaseType((Type)null).HasQueryFilter(sd => !sd.IsDeleted && sd.AdminStatusId != (int)ShiftUserRequestStatus.Reject && sd.UserStatusId != (int)ShiftUserRequestStatus.Reject);
            modelBuilder.Entity<User>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsDeleted);
            modelBuilder.Entity<UserProfile>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsDeleted);
            modelBuilder.Entity<CoverType>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsDeleted);
            modelBuilder.Entity<Group>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsDeleted);
            modelBuilder.Entity<AppVersion>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsDeleted);
            modelBuilder.Entity<GroupMember>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsDeleted);
            modelBuilder.Entity<Notification>().HasBaseType((Type)null).HasQueryFilter(ai => !ai.IsRead);

            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                    .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.ApplyConfiguration(new ChatGroupConfigurations());
            modelBuilder.ApplyConfiguration(new ChatConfigurations());
            modelBuilder.ApplyConfiguration(new ShiftConfigurations());
            modelBuilder.ApplyConfiguration(new ShiftDetailConfigurations());
            modelBuilder.ApplyConfiguration(new ShiftUserRequestConfigurations());
            modelBuilder.ApplyConfiguration(new ShiftFeedbackConfigurations());
            modelBuilder.ApplyConfiguration(new UserConfigurations());
            modelBuilder.ApplyConfiguration(new UserProfileConfigurations());
            modelBuilder.ApplyConfiguration(new UserTypeConfigurations());
            modelBuilder.ApplyConfiguration(new CoverTypeConfigurations());
            modelBuilder.ApplyConfiguration(new JobForConfigurations());
            modelBuilder.ApplyConfiguration(new GroupConfigurations());
            modelBuilder.ApplyConfiguration(new GroupMemberConfigurations());
            modelBuilder.ApplyConfiguration(new MobileUserTypeConfiguration());
            modelBuilder.ApplyConfiguration(new AppVersionConfigurations());
        }
    }
}