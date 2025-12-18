using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.DataBase.Entities
{
    /// <summary>
    /// 实体位置信息 - 使用EF Core数据注解
    /// </summary>
    [Table("entities")]
    public class Entity
    {
        /// <summary>
        /// 实体ID (主键)
        /// </summary>
        [Key]
        [Column("entityId")]
        [MaxLength(64)]
        public string EntityId { get; set; }

        /// <summary>
        /// 实体类型 (玩家/怪物/NPC等)
        /// </summary>
        [Column("entityType")]
        public EntityType EntityType { get; set; }

        /// <summary>
        /// X坐标
        /// </summary>
        [Column("x")]
        public float X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        [Column("y")]
        public float Y { get; set; }

        /// <summary>
        /// Z坐标
        /// </summary>
        [Column("z")]
        public float Z { get; set; }

        /// <summary>
        /// 朝向角度
        /// </summary>
        [Column("yaw")]
        public float Yaw { get; set; }

        /// <summary>
        /// 所在区域ID
        /// </summary>
        [Column("regionId")]
        [Required]
        [MaxLength(64)]
        public string RegionId { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [Column("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// 导航属性: 关联的角色 (一对一关系)
        /// </summary>
        public virtual Character Character { get; set; }
    }
}

