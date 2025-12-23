using Server.Game.HFSM;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIAttackState : HState
    {
        public readonly AIFsmContext ctx;
        public AIAttackState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            var target = ctx.Agent.Target;
            var ai = ctx.Agent.Entity;
            if (target != null)
            {
                Console.WriteLine("给你一刀");
                var dir = Vector3.Normalize(target.Kinematics.Position - ai.Kinematics.Position);
                float yaw = HelperUtility.GetYawFromDirection(dir);
                ctx.AddIntent(new AIMoveIntent(ai.Identity.EntityId, ai.Kinematics.Position, yaw, Vector3.Zero, 0));
                ctx.AddIntent(new AIAttackIntent(ai.Identity.EntityId, target.EntityId, 0));
            }
        }
    }
}
