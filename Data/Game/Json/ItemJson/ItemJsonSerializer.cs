using Newtonsoft.Json;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data.Game.Json.ItemJson
{
    public static class ItemJsonSerializer
    {
        private static Dictionary<string, ItemConfig> itemConfigs = new Dictionary<string, ItemConfig>();
        public static IReadOnlyDictionary<string, ItemConfig> ItemConfigs => itemConfigs;

        public static void Deserializer(string filePath)
        {
            var jsonText = File.ReadAllText(filePath);
            foreach(var cfg in JsonConvert.DeserializeObject<List<ItemConfig>>(jsonText))
            {
                itemConfigs[cfg.Id] = cfg;
            }
    
        }

        
    }
}
