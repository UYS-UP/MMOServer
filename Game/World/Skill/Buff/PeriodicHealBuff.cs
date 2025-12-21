using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill.Buff
{
    public class PeriodicHealBuff : BuffInstance
    {
        public PeriodicHealBuff(ICombatContext combatContext, BuffConfig cfg, EntityRuntime owner) : base(combatContext, cfg, owner) {}



        public override void OnApply()
        {
            base.OnApply();
            //CombatContext.BroadcastEntities(Owner.EntityId, Protocol.ApplyBuff, new ServerApplyBuff
            //{
            //    BuffId = Config.Id,
            //    Duration = Config.Duration
            //});
        }

        public override void OnTick()
        {
            Console.WriteLine("持续回血中....");
        }
    }
}
