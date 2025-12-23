using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIChaseState : HState
    {
        public readonly AIFsmContext Ctx;
        public AIChaseState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override void OnEnter()
        {
            Console.WriteLine("进入追击状态");
            Ctx.CurrentPath = null;
        }

        protected override void OnUpdate(float deltaTime)
        {
            var target = Ctx.Agent.Target;
            if (target == null) return;

            if (Ctx.EnsurePath(target.Kinematics.Position))
            {

                Ctx.FollowPath(deltaTime);
            }
        }
    }
}
