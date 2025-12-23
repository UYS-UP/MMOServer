using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIPeaceState : HState
    {
        private readonly AIFsmContext ctx;
        public readonly AIIdleState Idle;
        public readonly AIPatrolState Patrol;
        public AIPeaceState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
            Idle = new AIIdleState(ctx, machine, this);
            Patrol = new AIPatrolState(ctx, machine, this);
        }

        protected override HState GetInitialState() => Idle;

        protected override HState GetTransition()
        {
            if(ActiveChild == Idle && ctx.PatrolWaitTimer <= 0)
            {
                return Patrol;
            }

            if(ActiveChild == Patrol && ctx.HasReachedTarget)
            {
                return Idle;
            }

            return null;
        }


    }
}
