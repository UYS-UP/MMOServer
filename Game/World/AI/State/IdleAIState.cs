using Server.Game.Contracts.Actor;
using Server.Game.World.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.State
{
    public class IdleAIState : AIStateBase
    {
        private const float HomeArriveTolerance = 0.5f;
        private const float PatrolArriveTolerance = 0.5f;

        public IdleAIState(AIAgent agent, AIStateMachine fsm) : base(agent, fsm) { }

        public override void Tick(float deltaTime)
        {
            var pos = agent.Entity.Kinematics.Position;

            if (agent.ReturningHome)
            {
                float distHome = Vector3.Distance(pos, agent.HomePos);
                if (distHome <= HomeArriveTolerance)
                {
                    // 回家完成，开始正常巡逻
                    agent.ReturningHome = false;
                    agent.PatrolTarget = null;
                    return;
                }

                if (!EnsurePath(agent.HomePos))
                {
                    // 寻路失败，等下次再试
                    return;
                }

                FollowPath(deltaTime);
                return;
            }


            if (agent.Target != null)
            {
                Console.WriteLine("追击");
                agent.ReturningHome = false;
                fsm.ChangeState(AIStateType.Chase);
                return;
            }

            if (agent.PatrolTarget == null)
            {
                agent.PatrolTarget = GetRandomPatrolPoint();
            }

            float distToPatrol = Vector3.Distance(pos, agent.PatrolTarget.Value);
            if (distToPatrol <= PatrolArriveTolerance)
            {
                agent.PatrolTarget = GetRandomPatrolPoint();
            }

            if (!EnsurePath(agent.PatrolTarget.Value))
            {
                // 寻路失败，等下次再试
                return;
            }

            FollowPath(deltaTime);
        }
    }
}
