using Server.Game.Contracts.Server;
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
            ctx.Entity.Kinematics.State = EntityState.Move;
        }

      
    }
}
