using Newtonsoft.Json;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.Entities
{
    [Table("characters")]
    public class Character
    {
        /// <summary>
        /// 角色唯一ID (UUID/GUID) - 永不改变
        /// </summary>
        [Key]
        [Column("characterId")]
        [MaxLength(64)]
        public string CharacterId { get; set; }

        [Column("playerId")]
        [Required]
        [MaxLength(64)]
        public string PlayerId { get; set; }

        [Column("serverId")]
        [Required]
        public int ServerId { get; set; }

        [Column("name")]
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        // --- 基础成长数据 ---

        [Column("level")]
        public int Level { get; set; }

        [Column("exp")]
        public long Exp { get; set; }

        [Column("gold")]
        public long Gold { get; set; }

        [Column("mapId")]
        public int MapId { get; set; }

        [Column("x")]
        public float X { get; set; }

        [Column("y")]
        public float Y { get; set; }

        [Column("z")]
        public float Z { get; set; }

        [Column("yaw")] // 朝向
        public float Yaw { get; set; }

        public Dictionary<AttributeType, float> Attributes { get; set; } = new Dictionary<AttributeType, float>();

        [Column("lastLoginTime")]
        public DateTime LastLoginTime { get; set; }

        [Column("createTime")]
        public DateTime CreateTime { get; set; }



        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; }

        // 级联加载背包和武器熟练度
        public virtual ICollection<InventoryItem> InventoryItems { get; set; }
        public virtual ICollection<WeaponMastery> WeaponMasteries { get; set; }

    }
}
