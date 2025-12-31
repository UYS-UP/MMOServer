using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
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
        public DbSet<WeaponMastery> WeaponMasteries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            base.OnModelCreating(modelBuilder);

            // --- Player 配置 ---
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.PlayerId);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.CreatedTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // --- Character 配置 ---
            modelBuilder.Entity<Character>(entity =>
            {
                entity.HasKey(e => e.CharacterId);
                entity.HasIndex(e => e.PlayerId);
                entity.HasIndex(e => e.Name).IsUnique();

                // 默认值
                entity.Property(e => e.Level).HasDefaultValue(1);
                entity.Property(e => e.Gold).HasDefaultValue(0);

                // *** JSON 映射核心 ***
                // 数据库中是 "attributesJson" (string/text)
                // 实体中是 "Attributes" (Dictionary)
                entity.Property(e => e.Attributes)
                    .HasColumnName("attributesJson")
                    .HasColumnType("TEXT") // 或 varchar(4000)
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v), // 写入: 对象 -> JSON字符串
                        v => JsonConvert.DeserializeObject<Dictionary<AttributeType, float>>(v) ?? new Dictionary<AttributeType, float>() // 读取: JSON字符串 -> 对象
                    );

                // 级联删除
                entity.HasOne(e => e.Player)
                    .WithMany()
                    .HasForeignKey(e => e.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // --- InventoryItem 配置 ---
            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.DbId);
                entity.HasIndex(e => e.CharacterId);
                entity.Property(e => e.ForgeLevel).HasDefaultValue(0);

                // *** JSON 映射核心 ***
                entity.Property(e => e.DynamicData)
                    .HasColumnName("dynamicDataJson")
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<EquipDynamicData>(v) ?? new EquipDynamicData()
                    );

                entity.HasOne(e => e.Character)
                    .WithMany(c => c.InventoryItems)
                    .HasForeignKey(e => e.CharacterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // --- WeaponMastery 配置 ---
            modelBuilder.Entity<WeaponMastery>(entity =>
            {
                // 联合主键：角色ID + 武器类型
                entity.HasKey(e => new { e.CharacterId, e.WeaponType });


                // 1. 已解锁节点 List<int>
                entity.Property(e => e.UnlockedNodes)
                    .HasColumnName("unlockedNodesJson")
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<List<int>>(v) ?? new List<int>()
                    );

                // 2. 已装备技能 int[]
                entity.Property(e => e.EquippedSkills)
                    .HasColumnName("equippedSkillsJson")
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => JsonConvert.SerializeObject(v),
                        v => JsonConvert.DeserializeObject<int[]>(v) ?? new int[3]
                    );

                entity.HasOne(e => e.Character)
                    .WithMany(c => c.WeaponMasteries)
                    .HasForeignKey(e => e.CharacterId)
                    .OnDelete(DeleteBehavior.Cascade);
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

