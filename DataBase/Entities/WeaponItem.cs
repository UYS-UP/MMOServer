using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.Entities
{
    [Table("weapon_items")]
    public class WeaponItem
    {
        [Key]
        [Column("weaponDbId")]
        [MaxLength(64)]
        public string WeaponDbId { get; set; } // 武器的实例ID

        [Column("characterId")]
        [Required]
        public string CharacterId { get; set; }

        [Column("templateId")] // 武器配置ID (如: "W_Fire_SSR_01")
        public int TemplateId { get; set; }

        [Column("level")]
        public int Level { get; set; }

        [Column("exp")]
        public int Exp { get; set; }

        [Column("starLevel")] // 突破/精炼等级 (0-6星)
        public int StarLevel { get; set; }

        [Column("isLocked")]
        public bool IsLocked { get; set; }

        [ForeignKey("CharacterId")]
        public virtual Character Character { get; set; }
    }
}
