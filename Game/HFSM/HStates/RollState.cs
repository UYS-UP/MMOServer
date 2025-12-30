using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Game.Contracts.Server;
using Server.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class RollState : HState
    {
        private readonly EntityFsmContext ctx;

        public RollState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.RollRequested = false;
            ctx.Entity.Kinematics.State = EntityState.Roll;
            bool success = ctx.Combat.TryCastSkill(ctx.Entity, ctx.SkillRequestData, out _);
            if (success)
            {
                ctx.Combat.EmitEvent(new ExecuteSkillWorldEvent { Caster = ctx.Entity, SkillId = ctx.SkillRequestData.SkillId });
            }
            ctx.SkillRequestData = null;
        }
    }
}
