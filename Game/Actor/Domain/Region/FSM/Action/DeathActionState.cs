using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM.Action
{
    public class DeathActionState : StateBase<ActionStateType>, IActionControl
    {
        public bool LockMove => true;
        public bool LockTurn => true;
        public bool OverlayMotion => true;

        public override int Priority => 100;

        public override bool CanExit(ActionStateType to)
        {
            return false;
        }
    }
}
