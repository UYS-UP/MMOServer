using NPOI.SS.Formula.Functions;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Region;
using Server.Game.Actor.Domain.Region.AStar;
using Server.Game.Actor.Domain.Region.Services;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.AI
{
    public class AIAgent
    {
        public EntityRuntime Entity { get; }
        public AStarPathfind AStarPathfind { get; }
        public PerceptionSystem Perception { get; }
        public AggroSystem Aggro { get; }
        public EntityRuntime Target { get; set; }
        public List<Vector3> CurrentPath { get; set; }
        public int WaypointIndex { get; set; }
        public HashSet<string> LastVisibleEntities { get; set; }
        public float PerceptionRange { get; set; }
        public float FovAngle { get; set; }
        public float RepathCdLeft { get; set; }
        public Vector3 HomePos { get; set; }
        public float PatrolRadius { get; set; }
        public float LeashDistance { get; set; }

        public bool ReturningHome { get; set; }
        public Vector3? PatrolTarget { get; set; }

        public AIStateMachine StateMachine;    // 新增：状态机


        public AIAgent(
            EntityRuntime entity,
            AStarPathfind aStarPathfind,
            PerceptionSystem perception,
            AggroSystem aggro,
            float perceptionRange,
            float fovAngle,
            float repathCdLeft,
            float patrolRadious,
            float leashDistance,
            Vector3 homePos)
        {
            Entity = entity;
            Perception = perception;
            Aggro = aggro;
            AStarPathfind = aStarPathfind;
            Target = null;
            CurrentPath = new List<Vector3>();
            WaypointIndex = 0;
            LastVisibleEntities = new HashSet<string>();

            ReturningHome = false;
            PatrolTarget = null;

            StateMachine = new AIStateMachine(this);
            StateMachine.Init(AIStateType.Idle);
            PerceptionRange = perceptionRange;
            FovAngle = fovAngle;
            RepathCdLeft = repathCdLeft;
            HomePos = homePos;
            PatrolRadius = patrolRadious;
            LeashDistance = leashDistance;
        }
    }


    public abstract record AIBaseIntent(string EntityId) : IActorMessage;
    public sealed record AIBatchIntents(IReadOnlyList<AIBaseIntent> Intents) : IActorMessage;
    public sealed record AIMoveIntent(
        string EntityId,
        Vector3 TargetPos,
        float TargetYaw,
        Vector3 Direction,
        float Speed
    ) : AIBaseIntent(EntityId);


    public sealed record AIRotateIntent(
        string EntityId,
        float Yaw
    ) : AIBaseIntent(EntityId);

    public sealed record AIAttackIntent(
        string EntityId,
        string TargetId,
        int SkillId
    ) : AIBaseIntent(EntityId);
}
