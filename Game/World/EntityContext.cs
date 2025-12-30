using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Gateway;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.HFSM;
using Server.Game.World.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public struct BroadcastSnapshot
    {
        public EntityState State;
        public Vector3 Position;
        public float Yaw;
        public Vector3 Dir;


        public bool Equals(BroadcastSnapshot other) =>
            State == other.State && Position == other.Position
            && Yaw == other.Yaw && Dir == other.Dir;
    }


    public class BatchActorSend
    {
        public List<(string TargetActorId, IActorMessage Message)> Commnads { get; }

        public BatchActorSend()
        {
            Commnads = new List<(string TargetActorId, IActorMessage Message)>();
        }

        public void AddTell(string targetActorId, IActorMessage msg)
        {
            Commnads.Add((targetActorId, msg));
        }

        public void ClearSend()
        {
            Commnads.Clear();
        }
    }


    public class EntityContext
    {
        public int Tick;
        public int Id;

        public BatchGatewaySend Gateway;
        public BatchActorSend Actor;
        public List<int> WaitDestory;

        // 实体管理
        private readonly Dictionary<int, EntityRuntime> entities = new();
        private readonly Dictionary<int, AIAgent> aiAgents = new();

        private readonly Dictionary<string, int> playerToEntity = new();
        private readonly Dictionary<int, string> entityToPlayer = new();
        private readonly HashSet<string> players = new();

        private readonly Dictionary<int, BroadcastSnapshot> lastBroadcast = new();

        public IReadOnlySet<string> Players => players;
        public IReadOnlyDictionary<int, EntityRuntime> Entities => entities; 

        public IReadOnlyDictionary<int, AIAgent> AIAgents => aiAgents;

        public void AddEntity(EntityRuntime entity)
        {
            entities[entity.EntityId] = entity;
            if (entity.Identity.Type == EntityType.Character)
            {
                playerToEntity[entity.Profile.PlayerId] = entity.EntityId;
                entityToPlayer[entity.EntityId] = entity.Profile.PlayerId;
                players.Add(entity.Profile.PlayerId);
            }
        }

        public void AddAgent(AIAgent agent)
        {
            aiAgents[agent.Entity.EntityId] = agent;
        }

        public bool TryGetEntity(int entityId, out EntityRuntime entity)
            => entities.TryGetValue(entityId, out entity);


        public bool TryGetEntityByPlayerId(string playerId, out EntityRuntime? entity)
        {
            entity = null;
            if (playerToEntity.TryGetValue(playerId, out var entityId) &&
                entities.TryGetValue(entityId, out entity))
            {
                return true;
            }
            return false;
        }

        public string GetPlayerIdByEntityId(int entityId)
            => entityToPlayer.TryGetValue(entityId, out var pid) ? pid : "";

        public int GetEntityIdByPlayerId(string playerId)
            => playerToEntity.TryGetValue(playerId, out var eid) ? eid : -1;

        public List<string> GetPlayerIdsByEntityIds(IEnumerable<int> entityIds)
        {
            var set = new List<string>(capacity: entityIds.Count());
            foreach (var eid in entityIds)
                if (entityToPlayer.TryGetValue(eid, out var pid))
                    set.Add(pid);
            return set;
        }

        public List<EntityRuntime> GetEntitisByEntityIds(IEnumerable<int> entityIds)
        {
            var set = new List<EntityRuntime>(capacity: entityIds.Count());
            foreach (var eid in entityIds)
                if (entities.TryGetValue(eid, out var e))
                    set.Add(e);
            return set;
        }

        public void RemoveEntity(int entityId)
        {
            if (!entities.TryGetValue(entityId, out var entity)) return;

            entities.Remove(entityId);
            aiAgents.Remove(entityId);

            if (entity.Identity.Type == EntityType.Character)
            {
                playerToEntity.Remove(entity.Profile.PlayerId);
                entityToPlayer.Remove(entityId);
                players.Remove(entity.Profile.PlayerId);
            }

        }

        public BroadcastSnapshot? GetEntityLastBroadcast(int entityId)
        {
            if(lastBroadcast.TryGetValue(entityId, out var snap))
            {
                return snap;
            }
            return null;
        }


        public void UpdateEntityLastBroadcast(int entityId, BroadcastSnapshot snap)
            => lastBroadcast[entityId] = snap;

        public void ClearEntityLastBroadcast(int entityId)
            => lastBroadcast.Remove(entityId);



        public NetworkEntity GetNetworkEntityByEntityId(int entityId)
        {
            if (!entities.TryGetValue(entityId, out var entity)) return null;

  
            return entity.Identity.Type switch
            {
                EntityType.Character => new NetworkCharacter
                {
                    EntityId = entity.Identity.EntityId,
                    MapId = entity.World.MapId,
                    DungeonId = entity.World.DungeonId,
                    EntityType = entity.Identity.Type,
                    Position = entity.Kinematics.Position,
                    Yaw = entity.Kinematics.Yaw,
                    Direction = entity.Kinematics.Direction,
                    Speed = entity.Kinematics.Speed,
                    PlayerId = entity.Profile.PlayerId,
                    CharacterId = entity.Profile.CharacterId,
                    Name = entity.Identity.Name,
                    Level = entity.Stats.Level,
                    MaxHp = entity.Stats.BaseStats[AttributeType.MaxHp],
                    MaxEx = entity.Stats.BaseStats[AttributeType.MaxEx],
                    Hp = entity.Stats.CurrentHp,
                    Ex = entity.Stats.CurrentEx,
                    Gold = 0
                },
                EntityType.Monster => new NetworkMonster
                {
                    EntityId = entity.Identity.EntityId,
                    MapId = entity.World.MapId,
                    DungeonId = entity.World.DungeonId,
                    EntityType = entity.Identity.Type,
                    Position = entity.Kinematics.Position,
                    Yaw = entity.Kinematics.Yaw,
                    Direction = entity.Kinematics.Direction,
                    Speed = entity.Kinematics.Speed,
                    MonsterTemplateId = entity.Identity.TemplateId,
                    Level = entity.Stats.Level,
                    MaxHp = entity.Stats.BaseStats[AttributeType.MaxHp],
                    Hp = entity.Stats.CurrentHp,
                },
                EntityType.Npc => new NetworkNpc
                {
                    EntityId = entity.Identity.EntityId,
                    MapId = entity.World.MapId,
                    DungeonId = entity.World.DungeonId,
                    EntityType = entity.Identity.Type,
                    Position = entity.Kinematics.Position,
                    Yaw = entity.Kinematics.Yaw,
                    Direction = entity.Kinematics.Direction,
                    Speed = entity.Kinematics.Speed,
                    NpcTemplateId = entity.Identity.TemplateId,
                },
                _ => null
            };
        }
    }
}
