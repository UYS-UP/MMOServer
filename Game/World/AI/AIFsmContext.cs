using Server.Game.Contracts.Actor;
using Server.Game.World;
using Server.Game.World.AI;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Game.HFSM
{
    public class AIFsmContext
    {
        public readonly AIAgent Agent;
        public readonly ICombatContext Combat;
        public readonly List<AIBaseIntent> Intents = new List<AIBaseIntent>();
        public readonly Random Random = new Random();

        // --- 从 AIAgent 搬过来的动态状态数据 ---
        public bool ReturningHome;
        public List<Vector3> CurrentPath;
        public int WaypointIndex;
        public float RepathCdLeft;
        public HashSet<string> LastVisibleEntities = new HashSet<string>();

        // --- HFSM 专用状态数据 ---
        // 巡逻/待机
        public float PatrolWaitTimer;
        public Vector3? PatrolTargetPos;
        public bool HasReachedTarget;

        // 战斗
        public float LastAttackTime = -999f;
        public float ManeuverTimer;
        public Vector3? ManeuverTarget;

        public AIFsmContext(AIAgent agent, ICombatContext combat)
        {
            Agent = agent;
            Combat = combat;
        }

        public void AddIntent(AIBaseIntent intent) => Intents.Add(intent);
        public void ClearIntents() => Intents.Clear();
        public bool IsAttackReady => (DateTime.UtcNow.Ticks / 10000000f - LastAttackTime) >= Agent.AttackCooldown; // 简易时间

        // --- 行为逻辑方法 (直接操作 Context 里的数据) ---

        public Vector3 GetRandomPatrolPoint()
        {
            var home = Agent.HomePos;
            float radius = Agent.PatrolRadius;

            for (int i = 0; i < 10; i++)
            {
                float r = (float)Math.Sqrt(Random.NextDouble()) * radius; // Sqrt 保证分布均匀
                float angle = (float)Random.NextDouble() * MathF.PI * 2f;

                var candidate = home + new Vector3(MathF.Cos(angle) * r, 0, MathF.Sin(angle) * r);

                // 简单的防卡死检查 (距离判断)
                if (Vector3.Distance(candidate, Agent.Entity.Kinematics.Position) < 0.5f) continue;

                // 实际项目中这里应该 Check NavMesh
                var path = Agent.AStarPathfind.FindPath(goal: candidate, start: home);
                if (path != null) return candidate;

                return candidate; // 暂时直接返回
            }
            return home;
        }

        public void FollowPath(float deltaTime)
        {
            if (CurrentPath == null || CurrentPath.Count == 0) return;

            var currentPos = Agent.Entity.Kinematics.Position;
            int idx = WaypointIndex;

            if (idx >= CurrentPath.Count)
            {
                CurrentPath = null;
                WaypointIndex = 0;
                HasReachedTarget = true; // 标记到达
                return;
            }

            var targetWp = CurrentPath[idx];
            float moveSpeed = 4f; // 可以从 Agent 配置读取
            float arrivalThreshold = 0.5f;

            // 1. 计算移动位置
            float maxMoveDelta = moveSpeed * deltaTime;
            var nextPos = HelperUtility.MoveTowards(currentPos, targetWp, maxMoveDelta);

            // 2. 计算朝向
            var dirVec = targetWp - currentPos;
            dirVec.Y = 0;
            var dir = Vector3.Normalize(dirVec);
            float targetYaw = Agent.Entity.Kinematics.Yaw;

            if (dirVec.LengthSquared() > 0.001f)
            {
                targetYaw = HelperUtility.GetYawFromDirection(dir);
            }

            // 3. 发送意图
            AddIntent(new AIMoveIntent(
                Agent.Entity.Identity.EntityId,
                nextPos,
                targetYaw,
                dir,
                moveSpeed
            ));

            // 4. 判断路点到达
            if (Vector3.Distance(nextPos, targetWp) <= arrivalThreshold)
            {
                WaypointIndex++;
                if (WaypointIndex >= CurrentPath.Count)
                {
                    CurrentPath = null;
                    WaypointIndex = 0;
                    HasReachedTarget = true; // 路径跑完，视为到达
                }
            }
        }

        public bool EnsurePath(Vector3 targetPos)
        {
            var currentPos = Agent.Entity.Kinematics.Position;

            // 如果已有路径且终点差不多，继续用
            if (CurrentPath != null && CurrentPath.Count > 0)
            {
                if (Vector3.Distance(CurrentPath[^1], targetPos) < 2.0f) return true;
            }

            // 寻路
            var path = Agent.AStarPathfind.FindPath(goal: targetPos, start: currentPos);
            if (path == null || path.Count == 0)
            {
                CurrentPath = null;
                return false;
            }

            // 简单平滑：去掉第一个点如果它离我很近
            if (path.Count > 1 && Vector3.Distance(path[0], currentPos) < 0.5f)
            {
                path.RemoveAt(0);
            }

            CurrentPath = path;
            WaypointIndex = 0;
            HasReachedTarget = false;
            return true;
        }
    }
}