using Server.Game.Contracts.Server;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM.Motion
{
    public class IdleState : StateBase<MotionStateType>
    {
        public override void Update(float dt)
        {
            var dir = FSM.Entity.Kinematics.Direction;
            if(dir.SqrMagnitude() > 0.01f)
            {
                FSM.RequestChange(MotionStateType.Move);
            }
        }
    }
}
