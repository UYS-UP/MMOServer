using MessagePack;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Server
{
    public enum TeamType
    {
        Dungeon, // 副本队伍
        Pvp,    // PVP 竞技场队伍
        World   // 野外组队
    }

    [MessagePackObject]
    [Union(0, typeof(DungeonTeamData))]
    public abstract class TeamBaseData
    {
        [Key(0)] public TeamMember Leader;
        [Key(1)] public List<TeamMember> TeamMembers;
        [Key(2)] public TeamType TeamType;
        [Key(3)] public string TeamName;
        [Key(4)] public int TeamId;
        [Key(5)] public int MaxPlayers;
        [Key(6)] public int MinPlayers;

        public bool TryAddMemeber(string name, string playerId, string characterId, int level, out TeamMember member)
        {
            if (TeamMembers.Count >= MaxPlayers)
            {
                member = null;
                return false;
            }
            member = new TeamMember
            {
                Name = name,
                PlayerId = playerId,
                Level = level,
                CharacterId = characterId

            };
            TeamMembers.Add(member);
            return true;
        }

        public List<string> GetTeamPlayers()
        {
            List<string> result = new List<string>();
            foreach (var member in TeamMembers)
            {
                result.Add(member.PlayerId);
            }
            return result;
        }

        public bool CheckMemberCount()
        {
            return TeamMembers.Count >= MinPlayers && TeamMembers.Count <= MaxPlayers;
        }
    }

    [MessagePackObject]
    public class TeamMember
    {
        [Key(0)] public string PlayerId;
        [Key(1)] public string CharacterId;
        [Key(2)] public string Name;
        [Key(3)] public int Level;
    }

    [MessagePackObject]
    public class DungeonTeamData : TeamBaseData
    {
        [Key(7)] public string DungeonTemplateId;
        [Key(8)] public HashSet<string> LoadedPlayers;
    }
}
