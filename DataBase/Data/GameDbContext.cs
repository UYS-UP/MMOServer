using Microsoft.EntityFrameworkCore;
using Server.DataBase.Entities;
using System;

namespace Server.DataBase.Data
{
    /// <summary>
    /// 游戏数据库上下文
    /// </summary>
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options)
            : base(options)
        {


        }

        /// <summary>
        /// 玩家表
        /// </summary>
        public DbSet<Player> Players { get; set; }

        /// <summary>
        /// 角色表
        /// </summary>
        public DbSet<Character> Characters { get; set; }

        /// <summary>
        /// 实体表
        /// </summary>
        public DbSet<Entity> Entities { get; set; }


        /// <summary>
        /// 好友表
        /// </summary>
        public DbSet<Friend> Friends { get; set; }

        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<FriendGroup> FriendGroups { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }

        public DbSet<Mail> Mails { get; set; }
        public DbSet<MailAttachment> MailAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            

            base.OnModelCreating(modelBuilder);
            OnModelCraetingPlayer(modelBuilder);
            OnModelCreatingEntity(modelBuilder);
            OnModelCreatingFriend(modelBuilder);
            OnModelCreatingMail(modelBuilder);




        }

        private void OnModelCreatingEntity(ModelBuilder modelBuilder)
        {
            // 配置Role实体
            modelBuilder.Entity<Character>(entity =>
            {
                // 主键
                entity.HasKey(e => e.CharacterId);

                // 索引
                entity.HasIndex(e => e.PlayerId)
                    .HasDatabaseName("idx_playerId");

                entity.HasIndex(e => e.CharacterName)
                    .IsUnique()
                    .HasDatabaseName("idx_roleName");

                entity.HasIndex(e => e.EntityId)
                    .IsUnique()
                    .HasDatabaseName("idx_entityId");

                // 一对一关系: Role -> Entity
                entity.HasOne(r => r.Entity)
                    .WithOne(e => e.Character)
                    .HasForeignKey<Character>(r => r.EntityId)
                    .OnDelete(DeleteBehavior.Cascade);


                // 枚举转换
                entity.Property(e => e.Profession)
                    .HasConversion<int>();

                // 默认值
                entity.Property(e => e.Level)
                    .HasDefaultValue(1);

                entity.Property(e => e.Gold)
                    .HasDefaultValue(0);
            });

            // 配置Entity实体
            modelBuilder.Entity<Entity>(entity =>
            {
                // 主键
                entity.HasKey(e => e.EntityId);

                // 索引
                entity.HasIndex(e => e.RegionId)
                    .HasDatabaseName("idx_regionId");

                entity.HasIndex(e => new { e.RegionId, e.EntityType })
                    .HasDatabaseName("idx_region_type");

                // 枚举转换
                entity.Property(e => e.EntityType)
                    .HasConversion<int>();

                // 默认值
                entity.Property(e => e.LastUpdated)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private void OnModelCraetingPlayer(ModelBuilder modelBuilder)
        {

            // 配置Player实体
            modelBuilder.Entity<Player>(entity =>
            {
                // 主键
                entity.HasKey(e => e.PlayerId);

                // 索引
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasDatabaseName("idx_username");

                // 默认值
                entity.Property(e => e.CreatedTime)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }


        private void OnModelCreatingFriend(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Friend>(entity =>
            {
                entity.HasKey(e => e.FriendshipId);

                entity.HasIndex(e => e.CharacterId).HasDatabaseName("idx_role");
                entity.HasIndex(e => e.FriendCharacterId).HasDatabaseName("idx_friend_role");
                entity.HasIndex(e => new { e.CharacterId, e.FriendCharacterId })
                      .IsUnique().HasDatabaseName("idx_role_friend");
                entity.HasIndex(e => e.PlayerId).HasDatabaseName("idx_owner_player");
                entity.HasIndex(e => e.FriendPlayerId).HasDatabaseName("idx_friend_player");

                entity.HasIndex(e => e.FriendGroupId).HasDatabaseName("ix_friends_friendgroupid");
                entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Intimacy).HasDefaultValue(0);
            });

            modelBuilder.Entity<FriendGroup>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.OwnerCharacterId).HasDatabaseName("ix_friendgroups_owner");
                e.HasIndex(x => new { x.OwnerCharacterId, x.Name })
                 .IsUnique().HasDatabaseName("ux_friendgroups_owner_name");

            });


            modelBuilder.Entity<FriendRequest>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.ToCharacterId, x.Status })
                 .HasDatabaseName("ix_friendrequests_to_status");
                e.HasIndex(x => new { x.FromCharacterId, x.ToCharacterId, x.Status })
                 .HasDatabaseName("ix_friendrequests_from_to_status");
                // 生成列 isPending 只在 DB 端，不在 EF 中配置唯一索引，避免迁移冲突
            });

            modelBuilder.Entity<PrivateMessage>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.RecipientCharacterId, x.Status, x.Id })
                 .HasDatabaseName("ix_pm_recipient_status_id");
                e.HasIndex(x => new { x.ConversationKey, x.Id })
                 .HasDatabaseName("ix_pm_conversation_id");
            });
        }


        private void OnModelCreatingMail(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Mail>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ReceiverCharacterId)
                      .HasDatabaseName("idx_receiver");

                // tinyint bool 修正
                entity.Property(e => e.IsRead)
                      .HasColumnType("tinyint(1)");

                entity.Property(e => e.IsAttachmentClaimed)
                      .HasColumnType("tinyint(1)");

                entity.Property(e => e.CreateTime)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 一对多关系 (Mail -> MailAttachments)
                entity.HasMany(e => e.Attachments)
                      .WithOne(a => a.Mail)
                      .HasForeignKey(a => a.MailId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Character>()
                  .WithMany()
                  .HasForeignKey(e => e.ReceiverCharacterId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MailAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Count)
                      .HasDefaultValue(1);
            });
        }
        /// <summary>
        /// 保存更改前的处理
        /// </summary>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// 异步保存更改前的处理
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 自动更新时间戳
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Entity && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Entity entity)
                {
                    entity.LastUpdated = DateTime.UtcNow;
                }
            }
        }
    }
}

