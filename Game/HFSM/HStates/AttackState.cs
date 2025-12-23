using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Game.Contracts.Server;
using Server.Game.World;
using Server.Game.World.Skill;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class AttackState : HState
    {
        private readonly EntityFsmContext ctx;

        public AttackState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.Entity.Kinematics.State = EntityState.Attack;
            CastCurrentSkill();
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (ctx.AttackRequested)
            {
                CastCurrentSkill();
            }
        }

        private void CastCurrentSkill()
        {
            ctx.AttackRequested = false;
            if (ctx.Combat.IsSkillRunning(ctx.Entity.EntityId))
            {
                ctx.Combat.InterruptSkill(ctx.Entity.EntityId);
            }
            bool success = ctx.Combat.TryCastSkill(ctx.Entity, ctx.SkillRequestData, out var _);

            if (success)
            {
                Console.WriteLine("Attack:" + ctx.SkillRequestData.SkillId);
                ctx.Combat.EmitEvent(new ExecuteSkillWorldEvent { Caster = ctx.Entity , SkillId = ctx.SkillRequestData.SkillId});
            }
            ctx.SkillRequestData = null;
        }
    }
}
