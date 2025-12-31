using Newtonsoft.Json;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.Entities
{
    [Table("weapon_masteries")]
    public class WeaponMastery
    {
        [Column("characterId")]
        public string CharacterId { get; set; }

        [Column("weaponType")]
        public WeaponType WeaponType { get; set; }

        [Column("Level")]
        public int Level { get; set; }

        [Column("exp")]
        public int Exp { get; set; }

        [Column("skillPoints")]
        public int SkillPoints { get; set; }

        public List<int> UnlockedNodes { get; set; } = new List<int>();
        public int[] EquippedSkills { get; set; } = new int[3];

        [ForeignKey("CharacterId")]
        public virtual Character Character { get; set; }

    }
}
