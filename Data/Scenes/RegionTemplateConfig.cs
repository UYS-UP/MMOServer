using System.Collections.Generic;
using System.Numerics;

public static class RegionTemplateConfig
{
    public static readonly Dictionary<int, RegionTemplate> regionTemplates = new Dictionary<int, RegionTemplate> 
    {
        {
            0, new RegionTemplate(
                mapId: 0,
                regionName: "暴风城",
                regionDescription: "主城区域",
                navMeshPath: "D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Scenes\\GameScene001.bin",
                entryPosition: Vector3.Zero,
                dungeons: ["fuben01"],
                monsterPositions: new Dictionary<Vector3, string>
                {
                    {
                        new Vector3(10, 0, 10), "monster_001"
                    },
                },
                npcPositions: new Dictionary<Vector3, string>
                {

                }

            )
        }
    
    };


    public static readonly Dictionary<int, DungeonTemplate> dungeonTemplates = new Dictionary<int, DungeonTemplate>
    {
   
        {
            0, new DungeonTemplate(
                id: 0,
                name : "嚎哭深渊",
                minPlayers: 1,
                maxPlayers: 5,
                minLevel: 0,
                navMeshPath: "D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Scenes\\GameScene0_0.bin",
                entryPosition: Vector3.Zero,
                regionId: "001",
                monsterPostions: new Dictionary<Vector3, string>
                {
                    {
                        new Vector3(30, 0, 30), "monster_0001"
                    },
                    {
                        new Vector3(35, 0, 35), "monster_0001"
                    }
                },
                bossPostion: new Vector3(40, 0 ,40),
                bossTemplateId: "monster_0001",
                limitTime: 3000f
            )
        }
    };



    public static bool TryGetDungeonTemplateById(int id, out DungeonTemplate dungeonTemplate)
    {
        if (dungeonTemplates.TryGetValue(id, out dungeonTemplate)) return true;
        return false;
    }

    public static bool TryGetRegionTemplateById(int id, out RegionTemplate regionTemplate)
    {
        if (regionTemplates.TryGetValue(id, out regionTemplate)) return true;
        return false;
    }
}

public class RegionTemplate
{
    public int MapId { get; set; }
    public string RegionName { get; set; }
    public string RegionDescription { get; set; }
    public string NavMeshPath { get; set; }
    public Vector3 EntryPosition { get; set; }
    public List<string> Dungeons { get; set; }
    public Dictionary<Vector3, string> MonsterPositions { get; set; }
    public Dictionary<Vector3, string> NpcPositions { get; set; }

    public RegionTemplate(int mapId, string regionName, string regionDescription, string navMeshPath, Vector3 entryPosition,List<string> dungeons, Dictionary<Vector3, string> monsterPositions, Dictionary<Vector3, string> npcPositions)
    {
        MapId = mapId;
        RegionName = regionName;
        RegionDescription = regionDescription;
        NavMeshPath = navMeshPath;
        EntryPosition = entryPosition;
        Dungeons = dungeons;
        MonsterPositions = monsterPositions;
        NpcPositions = npcPositions;
    }
}

public class DungeonTemplate
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int MinLevel { get; set; }
    public string NavMeshPath { get; set; }
    public Vector3 EntryPosition { get; set; }
    public string RegionId { get; set; }
    public Dictionary<Vector3, string> MonsterPositions { get; set; }
    public Vector3 BossPosition { get; set; }
    public string BossTemplateId { get; set; }
    public float LimitTime { get; set; }


    public DungeonTemplate(int id, string name, int minPlayers, int maxPlayers, int minLevel, string navMeshPath, Vector3 entryPosition, string regionId, string bossTemplateId, Vector3 bossPostion, Dictionary<Vector3, string> monsterPostions, float limitTime)
    {
        Id = id;
        Name = name;
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        MinLevel = minLevel;
        NavMeshPath = navMeshPath;
        EntryPosition = entryPosition;
        RegionId = regionId;
        BossTemplateId = bossTemplateId;
        BossPosition = bossPostion;
        MonsterPositions = monsterPostions;
        LimitTime = limitTime;
    }
}
