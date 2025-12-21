using MessagePack;
using Server.DataBase.Entities;
using Server.Game.Actor.Domain.Player;
using Server.Game.Contracts.Server;
using Server.Game.World;
using Server.Utility;
using System.Numerics;

namespace Server.Game.Contracts.Network
{
    [MessagePackObject]
    public class TickMessage
    {
        [Key(0)] public int Tick;


    }

    [MessagePackObject]
    public class ServerHeartPong
    {
        [Key(0)] public long ServerUtcMs;
        [Key(1)] public long EchoClientUtcMs;
        [Key(2)] public int Tick;

        public ServerHeartPong(long serverUtcMs, int serverTick, long echoClientUtcMs)
        {
            ServerUtcMs = serverUtcMs;
            Tick = serverTick;
            EchoClientUtcMs = echoClientUtcMs;
        }

        public ServerHeartPong()
        {
        }
    }


    [MessagePackObject]
    public class ServerEntityReleaseSkill : TickMessage
    {
        [Key(1)] public string ReleaserId;
        [Key(2)] public int SkillId;
        [Key(3)] public short[] Position;
        [Key(4)] public short Yaw;
        [Key(5)] public EntityState State;

        public ServerEntityReleaseSkill(int tick, string releaserId, int skillId, short[] position, short yaw, EntityState state)
        {
            Tick = tick;
            ReleaserId = releaserId;
            SkillId = skillId;
            Position = position;
            Yaw = yaw;
            State = state;
        }

        public ServerEntityReleaseSkill(int tick, string releaserId, int skillId, Vector3 position, float yaw, EntityState state)
        {
            Tick = tick;
            ReleaserId = releaserId;
            SkillId = skillId;
            Position = HelperUtility.Vector3ToShortArray(position);
            Yaw = HelperUtility.YawToShort(yaw);
            State = state;
        }

        public ServerEntityReleaseSkill() { }
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
        [Key(1)] public string EntityId;
        [Key(2)] public EntityType Type;
        [Key(3)] public short[] Position;
        [Key(4)] public short Yaw;
        [Key(5)] public sbyte[] Direction;
        [Key(6)] public EntityState State;
        [Key(8)] public float Speed;

        public ServerEntityMoveSync() { }

        public ServerEntityMoveSync(int tick, string entityId, EntityType type, short[] position, short yaw, sbyte[] direction, EntityState state, float moveSpeed)
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

        public ServerEntityMoveSync(int tick, string entityId, EntityType type, Vector3 position, float yaw, Vector3 direction, EntityState state, float moveSpeed)
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
    public class ServerLevelDungeon
    {
        [Key(0)] public string Cause;
        [Key(1)] public string RegionId;
    }


    [MessagePackObject]
    public class ServerApplyBuff
    {
        [Key(0)] public int BuffId;
        [Key(1)] public float Duration;
    }

    [MessagePackObject]
    public class ServerLevelRegion
    {
        [Key(0)] public string RegionId;
    }

    [MessagePackObject]
    public class ServerPlayerMoveSync : TickMessage
    {
        [Key(1)] public int ClientTick;
        [Key(2)] public int ServerTick;
        [Key(3)] public string EntityId;
        [Key(4)] public short[] Position;
        [Key(5)] public short Yaw;
        [Key(6)] public sbyte[] Direction;
        [Key(7)] public float Speed;
        [Key(8)] public bool IsValid;

        public ServerPlayerMoveSync() { }

        public ServerPlayerMoveSync(int clientTick, int tick, string entityId, EntityType type, short[] position, short yaw, sbyte[] direction, float speed, bool isValid)
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

        public ServerPlayerMoveSync(int clientTick, int tick, string entityId, Vector3 position, float yaw, Vector3 direction, float speed, bool isValid)
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
        [Key(1)] public HashSet<string> DespawnEntities;
        public ServerEntityDespawn() { }
        public ServerEntityDespawn(int tick, HashSet<string> despawnEntities)
        {
            Tick = tick;
            DespawnEntities = despawnEntities;
        }
    }


  

    [MessagePackObject]
    public class ServerEntityDamage : TickMessage
    {
        [Key(1)] public string Source;
        [Key(2)] public List<EntityWound> Wounds;
        [Key(3)] public List<EntityDeath> Deaths;

        public ServerEntityDamage(int tick, string source, List<EntityWound> wounds, List<EntityDeath> deaths)
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
        [Key(1)] public string Target;
        [Key(2)] public int CurrentHp;

        public EntityWound(float wound, string target, int currentHp)
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
        [Key(1)] public string Target;
        [Key(2)] public List<ItemData> DroppedItems;

        public EntityDeath(float wound, string target, List<ItemData> droppedItems)
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
    public class ServerQueryInventory
    {
        [Key(0)] public int MaxSize;
        [Key(1)] public Dictionary<SlotKey, ItemData> Data;
        [Key(2)] public int MaxOccupiedSlot;

        public ServerQueryInventory() { }

        public ServerQueryInventory(int maxSize, Dictionary<SlotKey, ItemData> data, int maxOccupiedSlot)
        {
            MaxSize = maxSize;
            Data = data;
            MaxOccupiedSlot = maxOccupiedSlot;
        }
    }

    [MessagePackObject]
    public class ServerSwapStorageSlotResponse
    {
        [Key(0)] public int ReqId;
        [Key(1)] public bool Success;
        [Key(2)] public ItemData Item1;
        [Key(3)] public ItemData Item2;

        public ServerSwapStorageSlotResponse()
        {
        }

        public ServerSwapStorageSlotResponse(int reqId, bool success, ItemData item1, ItemData item2)
        {
            ReqId = reqId;
            Success = success;
            Item1 = item1;
            Item2 = item2;
        }
    }

    [MessagePackObject]
    public class ServerAddItem
    {
        [Key(0)] public Dictionary<SlotKey, ItemData> Items;
        [Key(1)] public int MaxSize;

    }

    [MessagePackObject]
    public class ServerQuestProgressUpdate
    {
        [Key(0)] public List<QuestProgressUpdate> QuestUpdates;
    }

    [MessagePackObject]
    public class ServerDungeonFailed
    {

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

    [MessagePackObject]
    public class ServerPlayerPickupItem
    {
        [Key(0)] public ItemData Data;
        [Key(1)] public int InventorySlot;

        public ServerPlayerPickupItem(ItemData data, int inventorySlot)
        {
            Data = data;
            InventorySlot = inventorySlot;
        }

        public ServerPlayerPickupItem()
        {
        }
    }


    [MessagePackObject]
    public class ServerDroppedItemDespawn : TickMessage
    {
        [Key(1)] public HashSet<string> DroppedItemIds;

        public ServerDroppedItemDespawn(int tick, HashSet<string> droppedItemIds)
        {
            Tick = tick;
            DroppedItemIds = droppedItemIds;
        }

        public ServerDroppedItemDespawn()
        {
        }
    }

    [MessagePackObject]
    public class ServerCreateDungeonTeam
    {
        [Key(0)] public bool Success;
        [Key(1)] public string Message;
        [Key(2)] public TeamBaseData Team;

        public ServerCreateDungeonTeam()
        {
        }

        public ServerCreateDungeonTeam(bool success, string message, TeamBaseData team)
        {
            Success = success;
            Message = message;
            Team = team;
        }
    }

    [MessagePackObject]
    public class ServerLoadDungeon
    {
        [Key(0)] public string TemplateId;
    }

    [MessagePackObject]
    public class ServerDungeonTeamInvite
    {
        [Key(0)] public int TeamId;
        [Key(1)] public string Message;

        public ServerDungeonTeamInvite(int teamId, string message)
        {
            TeamId = teamId;
            Message = message;
        }

        public ServerDungeonTeamInvite()
        {
        }
    }



    [MessagePackObject]
    public class ServerPlayerEnterGame
    {
        [Key(0)] public NetworkEntity PlayerEntity;

        public ServerPlayerEnterGame()
        {
        }

        public ServerPlayerEnterGame(NetworkEntity playerEntity)
        {
            PlayerEntity = playerEntity;
        }
    }

    [MessagePackObject]
    public class ServerPlayerChangeDungeon
    {
        [Key(0)] public string RegionId;
        [Key(1)] public string DungeonTemplateId;

        public ServerPlayerChangeDungeon()
        {
        }

        public ServerPlayerChangeDungeon(string regionId, string dungeonTemplateId)
        {
            RegionId = regionId;
            DungeonTemplateId = dungeonTemplateId;
        }
    }

    [MessagePackObject]
    public class ServerDungeonCompleted
    {
      
    }

    [MessagePackObject]
    public class ServerDungeonLootChoice
    {
        [Key(0)] public string EntityName;
        [Key(1)] public string ItemId;
        [Key(2)] public LootChoiceType LootChoiceType;
        [Key(3)] public int RollValue;
    }

    [MessagePackObject]
    public class ServerPlayerEnterDungeon
    {
        [Key(0)] public NetworkEntity PlayerEntity;
        [Key(1)] public float LimitTime;

        public ServerPlayerEnterDungeon()
        {
        }

        public ServerPlayerEnterDungeon(NetworkEntity playerEntity, float limitTime)
        {
            PlayerEntity = playerEntity;
            LimitTime = limitTime;
        }
    }

    [MessagePackObject]
    public class ServerPlayerEnterTeam
    {
        [Key(0)] public bool Success;
        [Key(1)] public string Message;
        [Key(2)] public TeamBaseData Team;
        [Key(3)] public string Player;

        public ServerPlayerEnterTeam(bool success, string message, TeamBaseData team, string player)
        {
            Success = success;
            Message = message;
            Team = team;
            Player = player;
        }

        public ServerPlayerEnterTeam()
        {
        }
    }

    [MessagePackObject]
    public class ServerSkillTimelineEventMove : TickMessage
    {
        [Key(1)] public string EntityId;
        [Key(2)] public int SkillId;
        [Key(3)] public Vector3 EndPos;
        [Key(4)] public float Time;

        public ServerSkillTimelineEventMove()
        {
        }

        public ServerSkillTimelineEventMove(int tick, string entityId, int skillId, Vector3 endPos, float time)
        {
            Tick = tick;
            EntityId = entityId;
            SkillId = skillId;
            EndPos = endPos;
            Time = time;
        }
    }


    [MessagePackObject]
    public class ServerQuestListSync
    {
        [Key(0)] public List<QuestNode> Quests;

        public ServerQuestListSync()
        {

        }
        public ServerQuestListSync(List<QuestNode> quests)
        {
            Quests = quests;
        }
    }

    [MessagePackObject]
    public class ServerAddFriend
    {
        [Key(0)] public bool Success;
        [Key(1)] public string Message;

        public ServerAddFriend()
        {
        }

        public ServerAddFriend(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    [MessagePackObject]
    public class ServerFriendListSync
    {
        [Key(0)] public List<NetworkFriendGroupData> Groups;
        [Key(1)] public List<NetworkFriendRequestData> Requests;
        [Key(2)] public List<NetworkFriendData> Friends;

        public ServerFriendListSync()
        {
        }

        public ServerFriendListSync(List<NetworkFriendGroupData> groups, List<NetworkFriendRequestData> requests, List<NetworkFriendData> friends)
        {
            Groups = groups;
            Requests = requests;
            Friends = friends;
        }
    }



    [MessagePackObject]
    public class ServerAddFriendGroup
    {
        [Key(0)] public bool Success;

    }

}
