using Server.DataBase.Entities;
using Server.Game.Actor.Domain.ASession;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public partial class CharacterActor
    {

        private async Task HandleMonsterKiller(A_MonsterKiller message)
        {
            Dictionary<SlotKey, ItemData> items = new Dictionary<SlotKey, ItemData>();
            foreach (var droppedItem in message.DroppedItems)
            {
                if (storage.AddItem(droppedItem, out var chantedSlots) != 0) continue;
                foreach (var changedSlot in chantedSlots)
                {
                    if (!storage.TryGetItem(changedSlot, out var value)) return;
                    items[changedSlot] = value;
                }
            }
            await TellGateway(state.PlayerId, Protocol.SC_AddInventoryItem, new ServerSlotUpdated { Items = items, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) });

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(state.PlayerId, Protocol.SC_QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress });

        }


        private async Task HandlItemsAcquired(A_ItemsAcquired message)
        {
            Dictionary<SlotKey, ItemData> items = new Dictionary<SlotKey, ItemData>();
            foreach (var item in message.Items)
            {
                if (storage.AddItem(item, out var chantedSlots) != 0) continue;
                foreach(var changedSlot in chantedSlots)
                {
                    if (!storage.TryGetItem(changedSlot, out var value)) return;
                    items[changedSlot] = value;
                }
            }

            if (items.Count > 0)
            {
                await TellGateway(state.PlayerId, Protocol.SC_AddInventoryItem, new ServerSlotUpdated { Items = items, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) });
            }

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(state.PlayerId, Protocol.SC_QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress });

        }

        private async Task HandleItemAcquired(A_ItemAcquired message)
        {
            if (storage.AddItem(message.Item, out var chantedSlots) != 0) return;
            Dictionary<SlotKey, ItemData> items = new Dictionary<SlotKey, ItemData>();

            foreach (var changedSlot in chantedSlots)
            {
                if (!storage.TryGetItem(changedSlot, out var value)) return;
                items[changedSlot] = value;
            }
            if (items.Count > 0)
            {
                await TellGateway(state.PlayerId, Protocol.SC_AddInventoryItem, new ServerSlotUpdated { Items = items, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) });
            }

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(state.PlayerId, Protocol.SC_QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress });

        }

        private async Task HandleLevelDungeon(A_LevelDungeon message)
        {
            state.DungeonId = -1;
            var sessionActor = System.SessionRouter.GetByPlayerId(state.PlayerId);
            await TellAsync(sessionActor, new CharacterWorldSync(state.MapId, state.DungeonId));
            await TellGateway(
                state.PlayerId, 
                Protocol.SC_LevelDungeon, 
                new ServerLevelDungeon
            {
                Cause = string.Empty,
                MapId = state.MapId,
            });
        }




    }
}
