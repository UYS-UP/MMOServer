using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data.Game.Json.ItemJson
{
    public class ItemConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType Type { get; set; }
        public QualityType Quality { get; set; }
        public int Price { get; set; }

        public ItemEquipConfig EquipConfig { get; set; }
        public ItemConsumableConfig ConsumableConfig { get; set; }
    }

    public class ItemEquipConfig
    {
        public EquipType EquipType { get; set; }
        public int ReqLevel { get; set; }
        public WeaponType WeaponType { get; set; }

        public Dictionary<AttributeType, float> BaseAttributes { get; set; }
    }

    public class ItemConsumableConfig
    {
        public EffectType EffectType { get; set; }
        public float EffectValue { get; set; }
        public float Cooldown {  get; set; }
    }
}
