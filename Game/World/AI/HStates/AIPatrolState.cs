using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIPatrolState : HState
    {

        private readonly AIFsmContext ctx;
        public AIPatrolState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            Vector3 target = ctx.GetRandomPatrolPoint();
            ctx.PatrolTargetPos = target;
            if (!ctx.EnsurePath(target))
            {
                ctx.HasReachedTarget = true;
            }
            Console.WriteLine("Enter AI Patrol");
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (ctx.HasReachedTarget || ctx.PatrolTargetPos == null) return;

            ctx.FollowPath(deltaTime);
        }
    }
}
