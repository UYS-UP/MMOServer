using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill
{
    [Serializable]
    public abstract class SkillEvent
    {
        public float Time { get; set; }
        public virtual void Execute(SkillInstance instance) { }
    }

    [Serializable]
    public abstract class SkillPhase
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public virtual void OnStart(SkillInstance instance) { }
        public virtual void OnExit(SkillInstance instance) { }
        public virtual void OnUpdate(SkillInstance instance, float dt) { }
    }

    [Serializable]
    public class SkillTimelineConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Duration { get; set; }

        public List<SkillEvent> ClientEvents { get; set; }
        public List<SkillEvent> ServerEvents { get; set; }
        public List<SkillPhase> ClientPhases { get; set; }
        public List<SkillPhase> ServerPhases { get; set; }
    }

}
