using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM.Action
{
    public class CastSkillActionState : StateBase<ActionStateType>, IActionControl
    {
        public bool LockMove => true;
        public bool LockTurn => false;
        public bool OverlayMotion => true;

        public override int Priority => 20;


        public override bool CanExit(ActionStateType to)
        {
            if (to == ActionStateType.Hit || to == ActionStateType.Death) return true;
            return base.CanExit(to);
        }
    }
}
