using Microsoft.EntityFrameworkCore;
using Server.DataBase.Entities;
using System;

namespace Server.DataBase
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


        public DbSet<Player> Players { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<WeaponItem> WeaponItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            

            base.OnModelCreating(modelBuilder);
            OnModelCraetingPlayer(modelBuilder);
            OnModelCreatingCharacter(modelBuilder);
            OnModelCreatingItems(modelBuilder);



        }

        private void OnModelCreatingCharacter(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Character>(entity =>
            {
                entity.HasKey(e => e.CharacterId);

                // 索引优化
                entity.HasIndex(e => e.PlayerId).HasDatabaseName("idx_playerId");
                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("idx_characterName");

                // 默认值
                entity.Property(e => e.Level).HasDefaultValue(1);
                entity.Property(e => e.Gold).HasDefaultValue(0);
                entity.Property(e => e.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne<Character>()
                    .WithMany()  // 如果Player有Characters集合，用 .WithMany(p => p.Characters)
                    .HasForeignKey(e => e.PlayerId)
                    .HasConstraintName("fk_chars_player")
                    .OnDelete(DeleteBehavior.Cascade);  // 这里匹配你的SQL约束
            });
        }


        private void OnModelCreatingItems(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.DbId);
                entity.HasIndex(e => e.CharacterId).HasDatabaseName("idx_inv_charId");

                entity.HasOne(e => e.Character)
                      .WithMany(c => c.InventoryItems)
                      .HasForeignKey(e => e.CharacterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WeaponItem>(entity =>
            {
                entity.HasKey(e => e.WeaponDbId);
                entity.HasIndex(e => e.CharacterId).HasDatabaseName("idx_wpn_charId");

                entity.HasOne(e => e.Character)
                      .WithMany(c => c.WeaponItems)
                      .HasForeignKey(e => e.CharacterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void OnModelCraetingPlayer(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.PlayerId);
                entity.HasIndex(e => e.Username)
                    .IsUnique()
                    .HasDatabaseName("idx_username");
                entity.Property(e => e.CreatedTime)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }


      
        /// <summary>
        /// 保存更改前的处理
        /// </summary>
        public override int SaveChanges()
        {
            return base.SaveChanges();
        }

        /// <summary>
        /// 异步保存更改前的处理
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}

