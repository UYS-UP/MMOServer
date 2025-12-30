using NPOI.SS.Formula.Functions;
using Server.Game.Actor.Core;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using Server.Game.World.AStar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI
{
    public class AIAgent
    {
        // --- 基础组件 ---
        public EntityRuntime Entity { get; }
        public AStarPathfind AStarPathfind { get; }
        public PerceptionSystem Perception { get; }
        public AggroSystem Aggro { get; }

        // --- 核心状态 (由外部系统如 Aggro 更新) ---
        public EntityRuntime Target { get; set; }

        // --- 全局配置参数 (只读或初始化后不变) ---
        public Vector3 HomePos { get; }
        public float PerceptionRange { get; }
        public float FovAngle { get; }
        public float PatrolRadius { get; }
        public float LeashDistance { get; }

        // 战斗配置
        public float AttackRange { get; set; } = 3f;
        public float AttackCooldown { get; set; } = 2.0f;

        // --- HFSM 入口 ---
        public AIHFSM AiFsm { get; private set; }

        public AIAgent(
            EntityRuntime entity,
            AStarPathfind aStarPathfind,
            PerceptionSystem perception,
            AggroSystem aggro,
            float perceptionRange,
            float fovAngle,
            float patrolRadious,
            float leashDistance,
            Vector3 homePos,
            ICombatContext combat)
        {
            Entity = entity;
            AStarPathfind = aStarPathfind;
            Perception = perception;
            Aggro = aggro;

            PerceptionRange = perceptionRange;
            FovAngle = fovAngle;
            PatrolRadius = patrolRadious;
            LeashDistance = leashDistance;
            HomePos = homePos;

            // 初始化 HFSM
            AiFsm = new AIHFSM(this, combat);
        }
    }


    public abstract record AIBaseIntent(int EntityId) : IActorMessage;
    public sealed record AIBatchIntents(IReadOnlyList<AIBaseIntent> Intents) : IActorMessage;
    public sealed record AIMoveIntent(
        int EntityId,
        Vector3 TargetPos,
        float TargetYaw,
        Vector3 Direction,
        float Speed
    ) : AIBaseIntent(EntityId);


    public sealed record AIRotateIntent(
        int EntityId,
        float Yaw
    ) : AIBaseIntent(EntityId);

    public sealed record AIAttackIntent(
        int EntityId,
        int TargetId,
        int SkillId
    ) : AIBaseIntent(EntityId);
}
