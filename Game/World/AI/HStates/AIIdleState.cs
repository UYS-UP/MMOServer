using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIIdleState : HState
    {
        private readonly AIFsmContext ctx;
        public AIIdleState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.PatrolWaitTimer = 2.0f + (float)(ctx.Random.NextDouble() * 3.0);
            ctx.CurrentPath = null;
            ctx.PatrolTargetPos = null;
            Console.WriteLine("Enter AI Idle");
            ctx.AddIntent(new AIMoveIntent(
                ctx.Agent.Entity.EntityId,
                ctx.Agent.Entity.Kinematics.Position,
                ctx.Agent.Entity.Kinematics.Yaw,
                Vector3.Zero,
                4
            ));
        }

        protected override void OnUpdate(float deltaTime)
        {
            ctx.PatrolWaitTimer -= deltaTime;
        }
    }
}
