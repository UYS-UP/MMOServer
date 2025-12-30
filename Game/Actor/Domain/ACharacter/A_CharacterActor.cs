using Server.DataBase.Entities;
using Server.Game.Actor.Domain.Gateway;
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
            Dictionary<SlotKey, ItemData> itmes = new Dictionary<SlotKey, ItemData>();
            foreach (var droppedItem in message.DroppedItems)
            {
                if (!storage.AddItem(droppedItem, out var slot)) continue;
                itmes[slot] = droppedItem;
            }
            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_AddInventoryItem, new ServerAddItem { Items = itmes, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) }));

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress }));

        }


        private async Task HandlItemsAcquired(A_ItemsAcquired message)
        {
            Dictionary<SlotKey, ItemData> itmes = new Dictionary<SlotKey, ItemData>();
            foreach (var item in message.Items)
            {
                if (!storage.AddItem(item, out var slot)) continue;
                itmes[slot] = item;
            }

            if (itmes.Count > 0)
            {
                await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_AddInventoryItem, new ServerAddItem { Items = itmes, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) }));
            }

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress }));

        }

        private async Task HandleItemAcquired(A_ItemAcquired message)
        {
            if (!storage.AddItem(message.Item, out var slot)) return;
            Dictionary<SlotKey, ItemData> itmes = new Dictionary<SlotKey, ItemData>
            {
                { slot, message.Item }
            };
            if (itmes.Count > 0)
            {
                await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_AddInventoryItem, new ServerAddItem { Items = itmes, MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory) }));
            }

            var progress = quest.OnEvent(message);
            if (progress.Count == 0) return;

            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_QuestUpdated, new ServerQuestProgressUpdate { QuestUpdates = progress }));

        }

        private async Task HandleLevelDungeon(A_LevelDungeon message)
        {
            state.DungeonId = -1;
            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_LevelDungeon, new ServerLevelDungeon
            {
                Cause = string.Empty,
                MapId = state.MapId,
            }));
        }




    }
}
