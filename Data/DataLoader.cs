using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data
{
    public static class DataLoader
    {
        private const string path = "D:\\Project\\UnityDemo\\MMORPGServer\\Data\\Game\\";
        public static Dictionary<string, RoleBaseValue> RoleBaseTable { get; private set; }
        public static Dictionary<string, RoleGrowthValue> RoleGrowthTable { get; private set; }

        public static void LoadAllExcel()
        {
            RoleBaseTable = ExcelDataReaderConverter.ConvertToDictionary<RoleBaseValue>(path);
            RoleGrowthTable = ExcelDataReaderConverter.ConvertToDictionary<RoleGrowthValue>(path);
           
        }

        public static List<EntityMapData> EntityMapData_01 { get; private set; }

        public static void LoadAllJson()
        {
            string jsonContent = File.ReadAllText(path + "entityMapData_01.json");
            RootData root = JsonConvert.DeserializeObject<RootData>(jsonContent);
            EntityMapData_01 = root.data.ToList<EntityMapData>();

        }


    }

    [System.Serializable]
    public class EntityMapData
    {
        public string entityId;
        public int entityType;
        public Position position;
        public int spawnPattern;
        public int count;
        public float radius;
        public RectangleSize rectangleSize;
        public float lineLength;
        public Rotation rotation;

        [System.Serializable]
        public class Position
        {
            public float x;
            public float y;
            public float z;
        }

        [System.Serializable]
        public class RectangleSize
        {
            public float x;
            public float y;
        }

        [System.Serializable]
        public class Rotation
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }
    }

    [System.Serializable]
    public class RootData
    {
        public List<EntityMapData> data;
    }
}
