using Server.Game.Actor.Domain.Region.AI;
using Server.Game.Actor.Domain.Region.FSM;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public class EntityContext
    {
        public int Tick;
        public string Id;

        public BatchGatewaySend Gateway;
        public BatchActorSend Actor;
        public List<string> WaitDestory;

        // 实体管理
        private readonly Dictionary<string, EntityRuntime> entities = new();
        private readonly Dictionary<string, AIAgent> aiAgents = new();

        private readonly Dictionary<string, string> playerToEntity = new();
        private readonly Dictionary<string, string> entityToPlayer = new();
        private readonly HashSet<string> players = new();

        private readonly Dictionary<string, (ActionStateType Action, MotionStateType Motion)> lastBroadcast = new();

        public IReadOnlyDictionary<string, EntityRuntime> Entities => entities;
        public IReadOnlySet<string> Players => players;

        public IReadOnlyDictionary<string, AIAgent> AIAgents => aiAgents;

        public void AddEntity(EntityRuntime entity)
        {
            entities[entity.Identity.EntityId] = entity;

            if (entity.Identity.Type == EntityType.Character)
            {
                playerToEntity[entity.Profile.PlayerId] = entity.Identity.EntityId;
                entityToPlayer[entity.Identity.EntityId] = entity.Profile.PlayerId;
                players.Add(entity.Profile.PlayerId);
            }
        }

        public bool TryGetEntity(string entityId, out EntityRuntime entity)
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

        public string GetPlayerIdByEntityId(string entityId)
            => entityToPlayer.TryGetValue(entityId, out var pid) ? pid : "";

        public string GetEntityIdByPlayerId(string playerId)
            => playerToEntity.TryGetValue(playerId, out var eid) ? eid : "";

        public List<string> GetPlayerIdsByEntityIds(IEnumerable<string> entityIds)
        {
            var set = new List<string>(capacity: entityIds.Count());
            foreach (var eid in entityIds)
                if (entityToPlayer.TryGetValue(eid, out var pid))
                    set.Add(pid);
            return set;
        }

        public List<EntityRuntime> GetEntitisByEntityIds(IEnumerable<string> entityIds)
        {
            var set = new List<EntityRuntime>(capacity: entityIds.Count());
            foreach (var eid in entityIds)
                if (entities.TryGetValue(eid, out var e))
                    set.Add(e);
            return set;
        }

        public void RemoveEntity(string entityId)
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

        public (ActionStateType Action, MotionStateType Motion) GetEntityLastBroadcast(string entityId)
            => lastBroadcast.TryGetValue(entityId, out var v) ? v : (ActionStateType.None, MotionStateType.Idle);

        public void UpdateEntityLastBroadcast(string entityId, ActionStateType a, MotionStateType m)
            => lastBroadcast[entityId] = (a, m);

        public void ClearEntityLastBroadcast(string entityId)
            => lastBroadcast.Remove(entityId);



        public NetworkEntity GetNetworkEntityByEntityId(string entityId)
        {
            if (!entities.TryGetValue(entityId, out var entity)) return null;

  
            return entity.Identity.Type switch
            {
                EntityType.Character => new NetworkCharacter
                {
                    EntityId = entity.Identity.EntityId,
                    RegionId = entity.WorldRef.RegionId,
                    DungeonId = entity.WorldRef.DungeonId,
                    EntityType = entity.Identity.Type,
                    Action = entity.FSM.Action.CurrentStateType,
                    Motion = entity.FSM.Motion.CurrentStateType,
                    Position = entity.Kinematics.Position,
                    Yaw = entity.Kinematics.Yaw,
                    Direction = entity.Kinematics.Direction,
                    Speed = entity.Kinematics.Speed,
                    PlayerId = entity.Profile.PlayerId,
                    CharacterId = entity.Profile.CharacterId,
                    Name = entity.Identity.Name,
                    Level = entity.Combat.Level,
                    MaxHp = entity.Combat.Maxhp,
                    MaxMp = entity.Combat.MaxMp,
                    MaxEx = entity.Combat.MaxEx,
                    Hp = entity.Combat.Hp,
                    Mp = entity.Combat.Mp,
                    Ex = entity.Combat.Ex,
                    Gold = 0,
                    Profession = entity.Profile.Profession,
                },
                EntityType.Monster => new NetworkMonster
                {
                    EntityId = entity.Identity.EntityId,
                    RegionId = entity.WorldRef.RegionId,
                    DungeonId = entity.WorldRef.DungeonId,
                    EntityType = entity.Identity.Type,
                    Action = entity.FSM.Action.CurrentStateType,
                    Motion = entity.FSM.Motion.CurrentStateType,
                    Position = entity.Kinematics.Position,
                    Yaw = entity.Kinematics.Yaw,
                    Direction = entity.Kinematics.Direction,
                    Speed = entity.Kinematics.Speed,
                    MonsterTemplateId = entity.Identity.TemplateId,
                    Level = entity.Combat.Level,
                    MaxHp = entity.Combat.Maxhp,
                    Hp = entity.Combat.Hp,
                },
                EntityType.Npc => new NetworkNpc
                {
                    EntityId = entity.Identity.EntityId,
                    RegionId = entity.WorldRef.RegionId,
                    DungeonId = entity.WorldRef.DungeonId,
                    EntityType = entity.Identity.Type,
                    Action = entity.FSM.Action.CurrentStateType,
                    Motion = entity.FSM.Motion.CurrentStateType,
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
