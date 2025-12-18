using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Server.Game.Contracts.Server;

namespace Server.DataBase.Entities
{
    /// <summary>
    /// 角色实体 - 使用EF Core数据注解
    /// </summary>
    [Table("characters")]
    public class Character
    {
        /// <summary>
        /// 角色ID (主键)
        /// </summary>
        [Key]
        [Column("characterId")]
        [MaxLength(64)]
        public string CharacterId { get; set; }

        /// <summary>
        /// 玩家ID (外键)
        /// </summary>
        [Column("playerId")]
        [Required]
        [MaxLength(64)]
        public string PlayerId { get; set; }

        /// <summary>
        /// 实体ID (外键)
        /// </summary>
        [Column("entityId")]
        [Required]
        [MaxLength(64)]
        public string EntityId { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        [Column("characterName")]
        [Required]
        [MaxLength(50)]
        public string CharacterName { get; set; }

        /// <summary>
        /// 等级
        /// </summary>
        [Column("level")]
        public int Level { get; set; }

        /// <summary>
        /// 当前生命值
        /// </summary>
        [Column("hp")]
        public int HP { get; set; }

        /// <summary>
        /// 当前魔法值
        /// </summary>
        [Column("mp")]
        public int MP { get; set; }

        /// <summary>
        /// 金币
        /// </summary>
        [Column("gold")]
        public int Gold { get; set; }

        /// <summary>
        /// 角色类型 (职业)
        /// </summary>
        [Column("profession")]
        public ProfessionType Profession { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [Column("lastLoginTime")]
        public DateTime LastLoginTime { get; set; }

        /// <summary>
        /// 当前经验值
        /// </summary>
        [Column("ex")]
        public int EX { get; set; }

        /// <summary>
        /// 最大生命值
        /// </summary>
        [Column("maxHp")]
        public int MaxHp { get; set; }

        /// <summary>
        /// 最大魔法值
        /// </summary>
        [Column("maxMp")]
        public int MaxMp { get; set; }

        /// <summary>
        /// 升级所需经验值
        /// </summary>
        [Column("maxEx")]
        public int MaxEx { get; set; }

        /// <summary>
        /// 导航属性: 所属玩家
        /// </summary>
        [ForeignKey("PlayerId")]
        public virtual Player Player { get; set; }

        /// <summary>
        /// 导航属性: 关联的实体位置信息
        /// </summary>
        [ForeignKey("EntityId")]
        public virtual Entity Entity { get; set; }
    }
}

