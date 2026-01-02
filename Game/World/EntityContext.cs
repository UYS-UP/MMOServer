using Server.Game.Actor.Core;
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


    public class EntityContext
    {
        public int Tick;
        public int Id;
        public List<int> WaitDestory;

        // 实体管理
        private readonly Dictionary<int, EntityRuntime> entities = new();
        private readonly Dictionary<int, AIAgent> aiAgents = new();

        private readonly Dictionary<string, int> characterToEntity = new();
        private readonly Dictionary<int, string> entityToCharacter = new();
        private readonly HashSet<string> characters = new();

        private readonly Dictionary<int, BroadcastSnapshot> lastBroadcast = new();

        public IReadOnlySet<string> Characters => characters;
        public IReadOnlyDictionary<int, EntityRuntime> Entities => entities; 

        public IReadOnlyDictionary<int, AIAgent> AIAgents => aiAgents;

        public void AddEntity(EntityRuntime entity)
        {
            entities[entity.EntityId] = entity;
            if (entity.Identity.Type == EntityType.Character)
            {
                characterToEntity[entity.Identity.CharacterId] = entity.EntityId;
                entityToCharacter[entity.EntityId] = entity.Identity.CharacterId;
                characters.Add(entity.Identity.CharacterId);
            }
        }

        public void AddAgent(AIAgent agent)
        {
            aiAgents[agent.Entity.EntityId] = agent;
        }

        public bool TryGetEntity(int entityId, out EntityRuntime entity)
            => entities.TryGetValue(entityId, out entity);


        public bool TryGetEntityByCharacterId(string characterId, out EntityRuntime? entity)
        {
            entity = null;
            if (characterToEntity.TryGetValue(characterId, out var entityId) &&
                entities.TryGetValue(entityId, out entity))
            {
                return true;
            }
            return false;
        }

        public string GetCharacterIdByEntityId(int entityId)
            => entityToCharacter.TryGetValue(entityId, out var pid) ? pid : "";

        public int GetEntityIdByCharacterId(string characterId)
            => characterToEntity.TryGetValue(characterId, out var eid) ? eid : -1;

        public List<string> GetCharacterIdsByEntityIds(IEnumerable<int> entityIds)
        {
            var set = new List<string>(capacity: entityIds.Count());
            foreach (var eid in entityIds)
                if (entityToCharacter.TryGetValue(eid, out var pid))
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
                characterToEntity.Remove(entity.Identity.CharacterId);
                entityToCharacter.Remove(entityId);
                characters.Remove(entity.Identity.CharacterId);
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
                    CharacterId = entity.Identity.CharacterId,
                    Name = entity.Identity.Name,
                    Level = entity.Stats.Level,
                    MaxHp = entity.Stats.BaseStats[AttributeType.MaxHp],
                    MaxEx = entity.Stats.BaseStats[AttributeType.MaxExp],
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
