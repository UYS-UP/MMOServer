using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using Server.Game.World.AI;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.State
{
    public class AttackAIState : AIStateBase
    {
        public AttackAIState(AIAgent agent, AIStateMachine fsm) : base(agent, fsm) { }

        public override void Tick(float deltaTime)
        {
            if (agent.Target == null)
            {
                fsm.ChangeState(AIStateType.Idle);
                return;
            }

            float dist = Vector3.Distance(agent.Entity.Kinematics.Position, agent.Target.Kinematics.Position);
            float attackRange = 1.5f;

            if (dist > attackRange * 1.2f)
            {
                // 目标跑远了，回 Chase
                fsm.ChangeState(AIStateType.Chase);
                return;
            }

            var toTarget = agent.Target.Kinematics.Position - agent.Entity.Kinematics.Position;
            toTarget.Y = 0;

            if (toTarget.LengthSquared() > float.Epsilon)
            {
                var dir = Vector3.Normalize(toTarget);
                float currentYaw = agent.Entity.Kinematics.Yaw;
                float desiredYaw = HelperUtility.GetYawFromDirection(dir);
                float deltaYaw = Math.Abs(HelperUtility.DeltaAngle(currentYaw, desiredYaw));

                if (deltaYaw > 15)
                {
                    float rotationSpeed = 180f * HelperUtility.Deg2Rad; // 旋转速度（弧度/秒）
                    float maxRotationDelta = rotationSpeed * deltaTime;
                    float newYaw = HelperUtility.MoveTowardsYaw(currentYaw, desiredYaw, maxRotationDelta);

                    AddIntent(new AIMoveIntent(
                        agent.Entity.Identity.EntityId,
                        agent.Entity.Kinematics.Position,
                        newYaw,
                        Vector3.Zero,
                        4
                    ));

    
                    return;
                }
            }



            // 产出一次普攻意图
            AddIntent(new AIAttackIntent(
                agent.Entity.Identity.EntityId,
                agent.Target.Identity.EntityId,
                0
            ));
        }
    }
}
