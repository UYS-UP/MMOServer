using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.Formula.Functions;
using Server.Game.HFSM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIEngageState : HState
    {
        private readonly AIFsmContext ctx;
        public AIAttackState Attack;
        public AIManeuverState Maneuver;

        public AIEngageState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
            Attack = new AIAttackState(ctx, machine, this);
            Maneuver = new AIManeuverState(ctx, machine, this);
        }

        protected override HState GetInitialState()
        {
            if (!ctx.Combat.IsSkillCooldown(ctx.Agent.Entity.EntityId, 0)) return Attack;
            return Maneuver;
        }

        protected override HState GetTransition()
        {
            if (ActiveChild == Maneuver && !ctx.Combat.IsSkillCooldown(ctx.Agent.Entity.EntityId, 0)) return Attack;
            if (ActiveChild == Attack && !ctx.Combat.IsSkillRunning(ctx.Agent.Entity.EntityId))
            {
                return Maneuver;
            }
            return null;
        }
    }
}
