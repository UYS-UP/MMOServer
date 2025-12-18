using Server.DataBase.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.DataBase.Entities
{
    /// <summary>
    /// 玩家实体 - 使用EF Core数据注解
    /// </summary>
    [Table("players")]
    public class Player
    {
        /// <summary>
        /// 玩家ID (主键)
        /// </summary>
        [Key]
        [Column("playerId")]
        [MaxLength(64)]
        public string PlayerId { get; set; }

        /// <summary>
        /// 用户名 (邮箱)
        /// </summary>
        [Column("username")]
        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Column("password")]
        [Required]
        [MaxLength(255)]
        public string Password { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("createdTime")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [Column("lastLoginTime")]
        public DateTime LastLoginTime { get; set; }

    }
}

