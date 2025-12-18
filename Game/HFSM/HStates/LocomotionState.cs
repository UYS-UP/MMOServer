using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class LocomotionState : HState
    {
        private readonly EntityFsmContext ctx;
        public IdleState Idle;
        public MoveState Move;

        public LocomotionState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
            Idle = new IdleState(ctx, m, this);
            Move = new MoveState(ctx, m, this);
        }

        protected override HState GetInitialState() => ctx.HasMoveInput ? Move : Idle;
    }
}
