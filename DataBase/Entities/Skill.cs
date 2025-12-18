using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.Entities
{
    public class Skill
    {

    }


    [MessagePackObject]
    public class SkillData
    {
        [Key(0)] public string SkillId { get; set; }
        [Key(1)] public string SkillName { get; set; }
        [Key(2)] public string SkillDescription { get; set; }
        [Key(3)] public int Level { get; set; }
        [Key(4)] public double CooldownDuration { get; set; }
        [Key(5)] public bool IsUnlocked { get; set; }
        [Key(6)] public int ManaCost { get; set; }
        [Key(7)] public int Attack { get; set; }

    }
}
