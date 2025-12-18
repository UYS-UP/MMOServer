using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.Skill
{
    [Serializable]
    public abstract class SkillEvent
    {
        public string Type { get; set; }
        public float Time { get; set; }
        public abstract void Execute(SkillInstance inst);
    }

    [Serializable]
    public class SkillTimelineConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Duration { get; set; }
        public List<SkillEvent> Events { get; set; }
    }

}
