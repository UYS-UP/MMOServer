using MessagePack;
using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
using System.Numerics;

namespace Server.Game.Contracts.Network
{
    [MessagePackObject]
    public class NetworkPlayer
    {
        [Key(0)] public string PlayerId { get; set; }
        [Key(1)] public string Username { get; set; }
        [Key(2)] public string Password { get; set; }

        public static NetworkPlayer CreatePlayerData(Player player)
        {
            return new NetworkPlayer
            {
                PlayerId = player.PlayerId,
                Username = player.Username,
                Password = player.Password,
            };
        }
    }

    [MessagePackObject]
    [Union(0, typeof(NetworkCharacter))]
    [Union(1, typeof(NetworkMonster))]
    [Union(2, typeof(NetworkNpc))]
    public abstract class NetworkEntity
    {
        [Key(0)] public int EntityId { get; set; }
        [Key(1)] public int MapId { get; set; }
        [Key(2)] public int DungeonId { get; set; }
        [Key(3)] public EntityType EntityType { get; set; }
        [Key(4)] public EntityState State { get; set; }
        [Key(5)] public Vector3 Position { get; set; }
        [Key(6)] public float Yaw { get; set; }
        [Key(7)] public Vector3 Direction { get; set; }
        [Key(8)] public float Speed { get; set; }
    }



    [MessagePackObject]
    public class NetworkCharacter : NetworkEntity
    {
        [Key(20)] public string PlayerId { get; set; }
        [Key(21)] public string CharacterId { get; set; }
        [Key(22)] public string Name { get; set; }
        [Key(23)] public int Level { get; set; }
        [Key(24)] public float MaxHp { get; set; }
        [Key(25)] public float Hp { get; set; }
        [Key(26)] public float MaxEx { get; set; }
        [Key(27)] public float Ex { get; set; }
        [Key(28)] public int Gold { get; set; }
        [Key(29)] public List<int> Skills { get; set; }
        

    }

    [MessagePackObject]
    public class NetworkMonster : NetworkEntity
    {
        [Key(20)] public string MonsterTemplateId { get; set; }
        [Key(21)] public int Level { get; set; }
        [Key(22)] public float MaxHp { get; set; }
        [Key(23)] public float Hp { get; set; }
    }

    [MessagePackObject]
    public class NetworkNpc : NetworkEntity
    {
        [Key(20)] public string NpcTemplateId { get; set; }
    }

    [MessagePackObject]
    public class NetworkFriendData
    {
        [Key(0)] public string CharacterId;
        [Key(1)] public string CharacterName;
        [Key(2)] public ProfessionType Type;
        [Key(3)] public string Avatar;
        [Key(4)] public int Level;
        [Key(5)] public string GroupId;
    }

    [MessagePackObject]
    public class NetworkFriendRequestData
    {
        [Key(0)] public string RequestId;
        [Key(1)] public string SenderName;
        [Key(2)] public string Remark;
    }

    [MessagePackObject]
    public class NetworkFriendGroupData
    {
        [Key(0)] public string GroupId;
        [Key(1)] public string GroupName;
    }
}
