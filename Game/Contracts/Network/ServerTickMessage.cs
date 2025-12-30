using MessagePack;
using Server.Game.Contracts.Server;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Network
{
    [MessagePackObject]
    public class TickMessage
    {
        [Key(0)] public int Tick;


    }


    [MessagePackObject]
    public class ServerEntityCastSkill : TickMessage
    {
        [Key(1)] public int Caster;
        [Key(2)] public int SkillId;
        [Key(3)] public short[] Position;
        [Key(4)] public short Yaw;
        [Key(5)] public EntityState State;

        public ServerEntityCastSkill(int tick, int caster, int skillId, short[] position, short yaw, EntityState state)
        {
            Tick = tick;
            Caster = caster;
            SkillId = skillId;
            Position = position;
            Yaw = yaw;
            State = state;
        }

        public ServerEntityCastSkill(int tick, int caster, int skillId, Vector3 position, float yaw, EntityState state)
        {
            Tick = tick;
            Caster = caster;
            SkillId = skillId;
            Position = HelperUtility.Vector3ToShortArray(position);
            Yaw = HelperUtility.YawToShort(yaw);
            State = state;
        }

        public ServerEntityCastSkill() { }
    }

    [MessagePackObject]
    public class ServerPlayerReleaseSkill : TickMessage
    {
        //[Key(1)] public int ClientTick;
        //[Key(2)] public int SkillId;
        //// [Key(3)] public EntityStateType State;
        //[Key(4)] public bool Success;
        //[Key(5)] public string Message;

        //public ServerPlayerReleaseSkill()
        //{
        //}

        //public ServerPlayerReleaseSkill(int tick, int clientTick, int skillId, EntityStateType state, bool success, string message)
        //{
        //    Tick = tick;
        //    ClientTick = clientTick;
        //    SkillId = skillId;
        //    State = state;
        //    Success = success;
        //    Message = message;
        //}
    }


    [MessagePackObject]
    public class ServerEntityMoveSync : TickMessage
    {
        [Key(1)] public int EntityId;
        [Key(2)] public EntityType Type;
        [Key(3)] public short[] Position;
        [Key(4)] public short Yaw;
        [Key(5)] public sbyte[] Direction;
        [Key(6)] public EntityState State;
        [Key(8)] public float Speed;

        public ServerEntityMoveSync() { }

        public ServerEntityMoveSync(int tick, int entityId, EntityType type, short[] position, short yaw, sbyte[] direction, EntityState state, float moveSpeed)
        {
            Tick = tick;
            EntityId = entityId;
            Type = type;
            Position = position;
            Yaw = yaw;
            Direction = direction;
            Speed = moveSpeed;
            State = state;
        }

        public ServerEntityMoveSync(int tick, int entityId, EntityType type, Vector3 position, float yaw, Vector3 direction, EntityState state, float moveSpeed)
        {
            Tick = tick;
            EntityId = entityId;
            Type = type;
            Position = HelperUtility.Vector3ToShortArray(position);
            Yaw = HelperUtility.YawToShort(yaw);
            Direction = HelperUtility.Vector3ToSbyteArray(direction);
            Speed = moveSpeed;
            State = state;
        }
    }


    [MessagePackObject]
    public class ServerPlayerMoveSync : TickMessage
    {
        [Key(1)] public int ClientTick;
        [Key(2)] public int ServerTick;
        [Key(3)] public int EntityId;
        [Key(4)] public short[] Position;
        [Key(5)] public short Yaw;
        [Key(6)] public sbyte[] Direction;
        [Key(7)] public float Speed;
        [Key(8)] public bool IsValid;

        public ServerPlayerMoveSync() { }

        public ServerPlayerMoveSync(int clientTick, int tick, int entityId, EntityType type, short[] position, short yaw, sbyte[] direction, float speed, bool isValid)
        {
            Tick = tick;
            ClientTick = clientTick;
            EntityId = entityId;
            Position = position;
            Yaw = yaw;
            Direction = direction;
            Speed = speed;
            IsValid = isValid;
        }

        public ServerPlayerMoveSync(int clientTick, int tick, int entityId, Vector3 position, float yaw, Vector3 direction, float speed, bool isValid)
        {
            ClientTick = clientTick;
            EntityId = entityId;
            Position = HelperUtility.Vector3ToShortArray(position);
            Yaw = HelperUtility.YawToShort(yaw);
            Direction = HelperUtility.Vector3ToSbyteArray(direction);
            Speed = speed;
            IsValid = isValid;
            Tick = tick;
        }
    }


    [MessagePackObject]
    public class ServerEntitySpawn : TickMessage
    {
        [Key(1)] public NetworkEntity SpawnEntity;
        public ServerEntitySpawn() { }

        public ServerEntitySpawn(int tick, NetworkEntity spawnEntity)
        {
            Tick = tick;
            SpawnEntity = spawnEntity;
        }
    }


    [MessagePackObject]
    public class ServerEntityDespawn : TickMessage
    {
        [Key(1)] public HashSet<int> DespawnEntities;
        public ServerEntityDespawn() { }
        public ServerEntityDespawn(int tick, HashSet<int> despawnEntities)
        {
            Tick = tick;
            DespawnEntities = despawnEntities;
        }
    }




    [MessagePackObject]
    public class ServerEntityDamage : TickMessage
    {
        [Key(1)] public int Source;
        [Key(2)] public List<EntityWound> Wounds;
        [Key(3)] public List<EntityDeath> Deaths;

        public ServerEntityDamage(int tick, int source, List<EntityWound> wounds, List<EntityDeath> deaths)
        {
            Tick = tick;
            Source = source;
            Wounds = wounds;
            Deaths = deaths;
        }

        public ServerEntityDamage()
        {
        }
    }

    [MessagePackObject]
    public class EntityWound
    {
        [Key(0)] public float Wound;
        [Key(1)] public int Target;
        [Key(2)] public float CurrentHp;

        public EntityWound(float wound, int target, float currentHp)
        {
            Wound = wound;
            Target = target;
            CurrentHp = currentHp;
        }

        public EntityWound()
        {
        }
    }

    [MessagePackObject]
    public class EntityDeath
    {
        [Key(0)] public float Wound;
        [Key(1)] public int Target;
        [Key(2)] public List<ItemData> DroppedItems;

        public EntityDeath(float wound, int target, List<ItemData> droppedItems)
        {
            Wound = wound;
            Target = target;
            DroppedItems = droppedItems;
        }

        public EntityDeath()
        {
        }
    }


    [MessagePackObject]
    public class ServerMonsterDeath : TickMessage
    {
        [Key(1)] public string EntityId;

        public ServerMonsterDeath() { }

        public ServerMonsterDeath(int tick, string entityId)
        {
            Tick = tick;
            EntityId = entityId;
        }
    }
}
