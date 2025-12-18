using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class RootState : HState
    {
        private readonly EntityFsmContext ctx;
        public AliveState Alive;
        public DeadState Dead;

        public RootState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
            Alive = new AliveState(ctx, m, this);
            Dead = new DeadState(ctx, m, this);
        }

        protected override HState GetInitialState() => Alive;

        protected override HState GetTransition()
        {
            if (ctx.DeathRequested) return Dead;
            return null;
        }


    }
}
