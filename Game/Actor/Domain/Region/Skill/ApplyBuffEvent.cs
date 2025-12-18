using Server.Game.Actor.Domain.Region.Skill.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.Skill
{
    public class ApplyBuffEvent : SkillEvent
    {
        public int BuffId { get; set; }

        public override void Execute(SkillInstance inst)
        {
            var config = new BuffConfig
            {
                Id = 1,
                Name = "Regeneration",
                Duration = 5.0f,
                TickInterval = 1,
                Type = BuffType.PeriodicHeal,
                AmountPertTick = 20,
                MaxStack = 1,
                Dispellable = true,
            };

            inst.Buff.ApplyBuff(inst.CombatContext, inst.Caster, config);
        }
    }
}
