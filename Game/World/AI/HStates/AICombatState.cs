using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AICombatState : HState
    {
        public readonly AIFsmContext Ctx;
        public AIChaseState Chase;
        public AIEngageState Engage;


        public AICombatState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
            Chase = new AIChaseState(ctx, machine, this);
            Engage = new AIEngageState(ctx, machine, this);
        }

        protected override HState GetInitialState()
        {
            return CheckDistance();
        }

        protected override HState GetTransition()
        {
            return CheckDistance();
        }

        private HState CheckDistance()
        {
            if(Ctx.Agent.Target == null) return null;
            float dist = Vector3.Distance(Ctx.Agent.Entity.Kinematics.Position, Ctx.Agent.Target.Kinematics.Position);
            if (ActiveChild == Engage && dist > Ctx.Agent.AttackRange * 1.2f && !Ctx.Combat.IsSkillRunning(Ctx.Agent.Entity.EntityId))
            {
                return Chase;
            }

            if (ActiveChild == Chase && dist <= Ctx.Agent.AttackRange)
            {
                return Engage;
            }

            if (ActiveChild == null)
            {
                return dist > Ctx.Agent.AttackRange ? Chase : Engage;
            }

            return null;
        }
    }
}
