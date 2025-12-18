using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.Util.HSSFColor;

namespace Server.Game.HFSM.HStates
{
    public class HitState : HState
    {
        private readonly EntityFsmContext ctx;
        public HitState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }
    }
}
