using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIManeuverState : HState
    {

        public readonly AIFsmContext ctx;
        public AIManeuverState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
        }

        protected override void OnUpdate(float deltaTime)
        {
        }
    }
}
