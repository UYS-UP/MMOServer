using Org.BouncyCastle.Math.EC.Multiplier;
using Server.Data.Game.Json.ItemJson;
using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public class AttributeManager
    {
        
        public Dictionary<AttributeType, float> CalculateAttributeChange(EquipData newItem, EquipData oldItem)
        {
            var delta = new Dictionary<AttributeType, float>();
            if (newItem != null)
            {
                var newStats = GetItemTotalStats(newItem);
                foreach (var kvp in newStats)
                {
                    AddStat(delta, kvp.Key, kvp.Value);
                }
            }

            if (oldItem != null)
            {
                var oldStats = GetItemTotalStats(oldItem);
                foreach (var kvp in oldStats)
                {
                    // 减去旧属性值
                    AddStat(delta, kvp.Key, -kvp.Value);
                }
            }
            return delta;
        }

        public Dictionary<AttributeType, float> GetItemTotalStats(EquipData equip)
        {
            var total = new Dictionary<AttributeType, float>();
            if (equip.BaseAttributes != null)
            {
                foreach (var kvp in equip.BaseAttributes)
                {
                    AddStat(total, kvp.Key, kvp.Value);
                }
            }

            if (equip.ForgeLevel > 0)
            {
                var keys = new List<AttributeType>(total.Keys);
                foreach (var k in keys) total[k] *= (1 + equip.ForgeLevel * 0.05f);
            }
            return total;
        }


        public Dictionary<AttributeType, float> CalculateCharacterAttributes(Character character)
        {
            var finalStats = new Dictionary<AttributeType, float>();
         
            foreach(var item in character.InventoryItems)
            {
                if(item.SlotContainer != SlotContainerType.Equipment) continue;
 
                // +基础属性
                if (!ItemJsonSerializer.ItemConfigs.TryGetValue(item.TemplateId, out var template)) continue;
                var equipConfig = template.EquipConfig;
                var baseStats = equipConfig.BaseAttributes;
                float forgeMultiplier = 1.0f + (item.ForgeLevel * 0.05f);

                foreach(var kvp in baseStats)
                {
                    int val = (int)(kvp.Value * forgeMultiplier);
                    AddStat(finalStats, kvp.Key, val);
                }

                var dynamicData = item.DynamicData;
                if(dynamicData.Affixes != null)
                {
                    foreach(var kvp in dynamicData.Affixes)
                    {
                        AddStat(finalStats, (AttributeType)kvp.Key, kvp.Value);
                    }
                }

                if(equipConfig.EquipType == EquipType.Weapon)
                {
                    var mastery = character.WeaponMasteries.FirstOrDefault(m => m.WeaponType == equipConfig.WeaponType);
                    if(mastery != null)
                    {
                      
                    }
                }

            }

            return finalStats;
        }

        private void AddStat(Dictionary<AttributeType, float> dict, AttributeType type, float val)
        {
            if (dict.ContainsKey(type)) dict[type] += val;
            else dict[type] = val;
        }
    }
}
