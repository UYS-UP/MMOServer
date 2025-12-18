using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class MoveState : HState
    {
        private readonly EntityFsmContext ctx;
        public MoveState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.LockMove = false;
            ctx.LockTurn = false;
        }

        protected override HState GetTransition()
            => !ctx.HasMoveInput ? Parent.AsTo<LocomotionState>().Idle : null;
    }
}
