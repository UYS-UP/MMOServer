using MessagePack;
using System.Collections.Generic;

namespace Server.Game.Contracts.Server
{
    [MessagePackObject]
    public class TeamData
    {
        [Key(0)] public int TeamId;
        [Key(1)] public string LeaderCharacter; // 队长ID
        [Key(2)] public int MaxPlayers;

        // 成员列表 (Key: CharacterId)
        [Key(4)] public Dictionary<string, TeamMember> Members = new Dictionary<string, TeamMember>();

        public TeamData() { }
        public TeamData(int id, TeamMember leader, int maxPlayers)
        {
            TeamId = id;
            LeaderCharacter = leader.CharacterId;
            Members[leader.CharacterId] = leader;
            MaxPlayers = maxPlayers;
        }

        public bool IsLeader(string characterId) => LeaderCharacter == characterId;
        public List<string> GetMemberPlayerIds() => new List<string>(Members.Keys);

    }

    [MessagePackObject]
    public class TeamMember
    {
        [Key(0)] public string CharacterId;
        [Key(1)] public string Name;
        [Key(2)] public int Level;
    }
}