using System.Collections.Generic;
using System.Numerics;

public static class RegionTemplateConfig
{
    public static readonly Dictionary<string, RegionTemplate> regionTemplates = new Dictionary<string, RegionTemplate> 
    {
        {
            "001", new RegionTemplate(
                regionId: "001",
                regionName: "暴风城",
                regionDescription: "主城区域",
                navMeshPath: "D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Scenes\\GameScene_001.bin",
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


    public static readonly Dictionary<string, List<string>> regionDungeons = new Dictionary<string, List<string>>
    {
        {
            "001", new List<string> {"fuben01"}
        }
    };


    public static readonly Dictionary<string, DungeonTemplate> dungeonTemplates = new Dictionary<string, DungeonTemplate>
    {
   
        {
            "fuben01", new DungeonTemplate(
                id: "fuben01",
                name : "嚎哭深渊",
                minPlayers: 1,
                maxPlayers: 5,
                minLevel: 0,
                navMeshPath: "D:\\Project\\UnityDemo\\MMORPGServer\\Data\\GameScene_0_fuben01.bin",
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



    public static bool TryGetDungeonTemplateById(string id, out DungeonTemplate dungeonTemplate)
    {
        if (dungeonTemplates.TryGetValue(id, out dungeonTemplate)) return true;
        return false;
    }

    public static List<DungeonTemplate> GetDungeonTemplates(string regionId)
    {
        List<DungeonTemplate> templates = new List<DungeonTemplate>();
        if (!regionDungeons.TryGetValue(regionId, out var templateIds)) return templates;
        foreach (var id in templateIds)
        {
            if (!dungeonTemplates.TryGetValue(id, out var template)) continue;
            templates.Add(template);
        }

        return templates;
    }

    public static bool TryGetRegionTemplateById(string id, out RegionTemplate regionTemplate)
    {
        if (regionTemplates.TryGetValue(id, out regionTemplate)) return true;
        return false;
    }
}

public class RegionTemplate
{
    public string RegionId { get; set; }
    public string RegionName { get; set; }
    public string RegionDescription { get; set; }
    public string NavMeshPath { get; set; }
    public Vector3 EntryPosition { get; set; }
    public List<string> Dungeons { get; set; }
    public Dictionary<Vector3, string> MonsterPositions { get; set; }
    public Dictionary<Vector3, string> NpcPositions { get; set; }

    public RegionTemplate(string regionId, string regionName, string regionDescription, string navMeshPath, Vector3 entryPosition,List<string> dungeons, Dictionary<Vector3, string> monsterPositions, Dictionary<Vector3, string> npcPositions)
    {
        RegionId = regionId;
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
    public string Id { get; set; }
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


    public DungeonTemplate(string id, string name, int minPlayers, int maxPlayers, int minLevel, string navMeshPath, Vector3 entryPosition, string regionId, string bossTemplateId, Vector3 bossPostion, Dictionary<Vector3, string> monsterPostions, float limitTime)
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
