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

        [Column("name")]
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        // --- 基础成长数据 ---

        [Column("level")]
        public int Level { get; set; }

        [Column("exp")]
        public long Exp { get; set; }

        // 金币/代币 (可以单独拆分表，也可以放这里)
        [Column("gold")]
        public long Gold { get; set; }

        [Column("mapId")] // 所在的地图/场景配置ID
        public int MapId { get; set; }

        [Column("x")]
        public float X { get; set; }

        [Column("y")]
        public float Y { get; set; }

        [Column("z")]
        public float Z { get; set; }

        [Column("yaw")] // 朝向
        public float Yaw { get; set; }

        [Column("hp")]
        public int Hp { get; set; }     

        [Column("lastLoginTime")]
        public DateTime LastLoginTime { get; set; }

        [Column("createTime")]
        public DateTime CreateTime { get; set; }

        [Column("serverId")]
        public int ServerId { get; set; }

        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; }

        // 级联加载背包和武器
        public virtual ICollection<InventoryItem> InventoryItems { get; set; }
        public virtual ICollection<WeaponItem> WeaponItems { get; set; }
    }
}
