using Server.Game.Actor.Domain.Gateway;
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
            var item = new ConsumableData
            {
                ItemId = HelperUtility.GetKey(),
                ItemTemplateId = message.ItemTemplateId,
                ItemName = "血瓶",
                ItemType = ItemType.Consumable,
                QuantityType = QuantityType.Common,
                Description = "一瓶血瓶",
                Gold = 500,
                IsStack = true,
                ItemCount = 1,
                EffectType = EffectType.HealHP,
                EffectValue = 20,
                Cooldown = 5f
            };
            var result = storage.AddItem(item, out var slot);

            if (!result) return;
            var items = new Dictionary<SlotKey, ItemData>();
            if (!storage.TryGetItem(slot, out var value)) return;
            items[slot] = value;
            await TellGateway(new SendToPlayer(
                state.PlayerId,
                Protocol.SC_AddInventoryItem,
                new ServerAddItem {
                    Items = items,
                    MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory)
                }));
        }
    }
}
