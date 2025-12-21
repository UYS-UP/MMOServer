using ICSharpCode.SharpZipLib.Core;
using Server.Game.Actor.Core;
using Server.Game.Contracts.Actor;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI
{
    public abstract class AIStateBase
    {
        protected readonly Random random = new Random();
        protected readonly AIAgent agent;
        protected readonly AIStateMachine fsm;

        protected AIStateBase(AIAgent agent, AIStateMachine fsm)
        {
            this.agent = agent;
            this.fsm = fsm;
        }

        public abstract void Tick(float deltaTime);

        public virtual void Enter() { }
        public virtual void Exit() { }

        protected void AddIntent(AIBaseIntent intent)
        {
            fsm.Intents.Add(intent);
        }


        /// <summary>
        /// 在 HomePos 周围随机选一个巡逻点，半径在 [0, PatrolRadius]，并保证可寻路。
        /// </summary>
        protected Vector3 GetRandomPatrolPoint()
        {
            var home = agent.HomePos;
            float radius = agent.PatrolRadius;

            for (int i = 0; i < 20; i++)  // 尝试次数加大，防止卡死
            {
                float r = (float)random.NextDouble() * radius;
                float angle = (float)random.NextDouble() * MathF.PI * 2f;

                var candidate = home + new Vector3(MathF.Cos(angle) * r, 0, MathF.Sin(angle) * r);

                if (Vector3.Distance(candidate, home) > radius + 1f)
                    continue;

                var path = agent.AStarPathfind.FindPath(goal: candidate, start: home);
                if (path != null && path.Count > 0)
                    return candidate;
            }

            return home;
        }


        protected void FollowPath(float deltaTime)
        {
            if (agent.CurrentPath == null || agent.CurrentPath.Count == 0)
                return;

            var currentPos = agent.Entity.Kinematics.Position;
            float currentYaw = agent.Entity.Kinematics.Yaw; // 重命名避免混淆
            int idx = agent.WaypointIndex;

            if (idx >= agent.CurrentPath.Count)
            {
                agent.CurrentPath = null;
                agent.WaypointIndex = 0;
                return;
            }

            var targetWp = agent.CurrentPath[idx];

            // 移动参数
            float moveSpeed = 4f;
            float arrivalThreshold = 0.8f; // 到达阈值

            // 计算移动
            float maxMoveDelta = moveSpeed * deltaTime;
            var targetPos = HelperUtility.MoveTowards(currentPos, targetWp, maxMoveDelta);

            // 计算朝向目标的方向
            var toNextWp = targetWp - targetPos;
            toNextWp.Y = 0; // 忽略Y轴差异（假设在水平面上移动）

            float targetYaw = currentYaw; // 默认保持当前朝向
            var desiredDir = Vector3.Zero;
            if (toNextWp.LengthSquared() > 0.01f)
            {
                // 计算目标朝向
                desiredDir = Vector3.Normalize(toNextWp);
                targetYaw = HelperUtility.GetYawFromDirection(desiredDir);
            }

            // 到达当前waypoint的判定
            float distanceToTarget = Vector3.Distance(targetPos, targetWp);
            if (distanceToTarget <= arrivalThreshold)
            {
                idx++;
                if (idx >= agent.CurrentPath.Count)
                {
                    // 路径完成
                    agent.CurrentPath = null;
                    agent.WaypointIndex = 0;
                }
                else
                {
                    // 移动到下一个waypoint
                    agent.WaypointIndex = idx;
                }
            }

            AddIntent(new AIMoveIntent(
                agent.Entity.Identity.EntityId,
                targetPos,
                targetYaw,
                desiredDir,
                moveSpeed
            ));
        }


        protected bool EnsurePath(Vector3 targetPos)
        {
            var currentPos = agent.Entity.Kinematics.Position;

            if (agent.CurrentPath != null && agent.CurrentPath.Count > 0)
            {
                var lastWp = agent.CurrentPath[^1];
                if (Vector3.Distance(lastWp, targetPos) > 5f)
                {
                    agent.CurrentPath = null;  // 强制重寻
                }
                else
                {
                    return true;
                }
            }

            var path = agent.AStarPathfind.FindPath(goal: targetPos, start: currentPos);
            if (path == null || path.Count <= 1)
            {
                agent.CurrentPath = null;
                agent.WaypointIndex = 0;
                return false;
            }

            path.RemoveAt(0);
            agent.CurrentPath = path;
            agent.WaypointIndex = 0;
            return true;
        }


    }




}
