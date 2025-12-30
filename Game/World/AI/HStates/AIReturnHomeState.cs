using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIReturnHomeState : HState
    {
        public readonly AIFsmContext Ctx;
        public AIReturnHomeState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            Ctx = ctx;
        }

        protected override void OnEnter()
        {
            Ctx.ReturningHome = true;
            Ctx.EnsurePath(Ctx.Agent.HomePos);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if(Ctx.ReturningHome)
            {
                Ctx.FollowPath(deltaTime);
                if(Ctx.HasReachedTarget) Ctx.ReturningHome = false;
            }
        }
    }
}
