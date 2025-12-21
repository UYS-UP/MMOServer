using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using Server.Game.World.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.State
{
    public class ChaseAIState : AIStateBase
    {
        public ChaseAIState(AIAgent agent, AIStateMachine fsm) : base(agent, fsm) { }

        public override void Tick(float deltaTime)
        {

            if (agent.Target == null)
            {
                fsm.ChangeState(AIStateType.Idle);
                return;
            }

            var targetPos = agent.Target.Kinematics.Position;
            var agentPos = agent.Entity.Kinematics.Position;

            float distFromHome = Vector3.Distance(agent.HomePos, agentPos);
            if (distFromHome > agent.LeashDistance)
            {
                agent.Target = null;
                agent.ReturningHome = true;
                agent.PatrolTarget = null;
                fsm.ChangeState(AIStateType.Idle);
                return;
            }

            float distToTarget = Vector3.Distance(agentPos, targetPos);
            float attackRange = 1.5f; // 需要你在 AISnapShot 里加上

            if (distToTarget <= attackRange * 0.9f)
            {

                fsm.ChangeState(AIStateType.Attack);
                return;
            }
            if (!EnsurePath(targetPos))
            {
                // 寻路失败，等下次再试
                return;
            }

            FollowPath(deltaTime);
        }
    }
}
