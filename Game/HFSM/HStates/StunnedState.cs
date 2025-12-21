using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.Util.HSSFColor;

namespace Server.Game.HFSM.HStates
{
    public class StunnedState : HState
    {
        private readonly EntityFsmContext ctx;
        public StunnedState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.Entity.Kinematics.State = EntityState.Stunned;
        }
    }
}
