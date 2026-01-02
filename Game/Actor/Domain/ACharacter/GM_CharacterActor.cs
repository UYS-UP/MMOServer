using MessagePack;
using Server.Data.Game.Json.ItemJson;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public partial class CharacterActor
    {

        private async Task GM_HandleAddItem(GM_AddItem message)
        {
            if (!ItemJsonSerializer.ItemConfigs.TryGetValue(message.ItemTemplateId, out var config)) return;
            var items = new Dictionary<SlotKey, ItemData>();
            switch (config.Type)
            {
                case ItemType.Equip:
                    {
                        for (int i = 0; i < message.Count; i++)
                        {
                            var item = new EquipData
                            {
                                InstanceId = HelperUtility.GetKey(),
                                TemplateId = config.Id,
                                ItemName = config.Name,
                                ItemType = config.Type,
                                QuantityType = config.Quality,
                                Description = config.Description,
                                Price = config.Price,
                                IsStack = false,
                                ItemCount = 1,
                                EquipType = config.EquipConfig.EquipType,
                                WeaponSubType = config.EquipConfig.WeaponType,
                                ForgeLevel = 0,
                                BaseAttributes = new Dictionary<AttributeType, float>(config.EquipConfig.BaseAttributes)
                            };
                            var result = storage.AddItem(item, out var changedSlots);
                            if (result != 0) break;
                            foreach(var changedSlot in changedSlots)
                            {
                                if (!storage.TryGetItem(changedSlot, out var value)) return;
                                items[changedSlot] = value;
                            }
                          
                           
                        }
                    }
                    break;
                case ItemType.Consumable:
                    {
                        var item = new ConsumableData
                        {
                            InstanceId = HelperUtility.GetKey(),
                            TemplateId = config.Id,
                            ItemName = config.Name,
                            ItemType = config.Type,
                            QuantityType = config.Quality,
                            Description = config.Description,
                            Price = config.Price,
                            IsStack = true,
                            ItemCount = message.Count,
                            EffectType = config.ConsumableConfig.EffectType,
                            EffectValue = config.ConsumableConfig.EffectValue,
                            Cooldown = config.ConsumableConfig.Cooldown,
                        };
                        var result = storage.AddItem(item, out var changedSlots);
                        if (result != 0) break;
                        foreach (var changedSlot in changedSlots)
                        {
                            if (!storage.TryGetItem(changedSlot, out var value)) return;
                            items[changedSlot] = value;
                        }
                    }

                    break;
                case ItemType.Material:
                    break;
            }

            if(items.Count > 0)
            {
                var bytes = MessagePackSerializer.Serialize(new ServerSlotUpdated
                {
                    Items = items,
                    MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory)
                });
                await TellGateway(state.CharacterId, Protocol.SC_AddInventoryItem, bytes);
            }

        }
    }
}
