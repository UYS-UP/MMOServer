using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM.Action
{
    public class HitActionState : StateBase<ActionStateType>, IActionControl
    {
        public bool LockMove => true;
        public bool LockTurn => false;
        public bool OverlayMotion => true;

        private float Duration = 0.8f;
        private float Current;
        public override void Update(float deltaTime)
        {
            Current += deltaTime;
            if (Current >= Duration) FSM.RequestChange(ActionStateType.None);
        }

        public override bool CanExit(ActionStateType to)
        {
            if(to == ActionStateType.Death) return true;
            return base.CanExit(to);
        }

    }
}
