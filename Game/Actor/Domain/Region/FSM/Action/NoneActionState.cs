using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM.Action
{
    public class NoneActionState : StateBase<ActionStateType>, IActionControl
    {
        public bool LockMove => false;
        public bool LockTurn => false;
        public bool OverlayMotion => false;

    }
}
