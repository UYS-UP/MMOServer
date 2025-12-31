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
