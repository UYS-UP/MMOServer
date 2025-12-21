using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class DeadState : HState
    {
        private readonly EntityFsmContext ctx;
        public DeadState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.LockMove = true;
            ctx.LockTurn = true;
            ctx.Entity.Kinematics.State = EntityState.Dead;
        }

        protected override HState GetTransition() => null;
    }
}
