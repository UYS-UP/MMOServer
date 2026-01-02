using MessagePack;
using Server.DataBase.Entities;
using Server.Game.Actor.Domain.ASession;
using Server.Game.Actor.Domain.Team;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public partial class CharacterActor
    {
        #region 场景相关

        private async Task CS_HandleCharacterEnterRegion(CS_CharacterEnterRegion message)
        {
            var regionActor = GameField.GetActor<RegionActor>(state.MapId);
            var entity = await CreateRuntimeFromState();
            await TellAsync(regionActor, new A_CharacterSpawn(entity));
       
        }

        private async Task CS_HandleCharacterChangeRegion(CS_CharacterChangeRegion message)
        {
            var actor = GameField.GetActor<RegionActor>(state.MapId);
            await TellAsync(actor, new A_CharacterDespawn(state.EntityId));
            state.MapId = message.MapId;
           
         
            var sessionActor = System.SessionRouter.GetByPlayerId(state.PlayerId);
            var bytes = MessagePackSerializer.Serialize(new ServerEnterRegion { MapId = state.MapId });
            await TellAsync(sessionActor, new CharacterWorldSync(state.MapId, state.DungeonId));
            await TellAsync(sessionActor, new SendTo(Protocol.SC_EnterRegion, bytes));
        }

        private async Task CS_HandleCharacterEnterDungeon(CS_CharacterEnterDungeon message)
        {
            var actor = GameField.GetActor<DungeonActor>();
            var entity = await CreateRuntimeFromState();
            await TellAsync(actor, new A_CharacterSpawn(entity));
        }

        private async Task CS_HandleCharacterLevelDungeon(CS_CharacterLevelDungeon message)
        {
            var dungeonActor = GameField.GetActor<DungeonActor>();
            state.DungeonId = -1;
            var sessionActor = System.SessionRouter.GetByPlayerId(state.PlayerId);
            await TellAsync(sessionActor, new CharacterWorldSync(state.MapId, state.DungeonId));
            await TellAsync(dungeonActor, new A_CharacterDespawn(state.EntityId, state.DungeonId));
        }


        #endregion


        #region 背包相关

        private async Task CS_HandleQueryInventory(CS_QueryInventory message)
        {
            
            var payload = new ServerQueryInventory(
                storage.GetMaxContainerSize(SlotContainerType.Inventory),
                storage.GetItemsInRange(SlotContainerType.Inventory, message.StartSlot, message.EndSlot),
                storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory)
                );
            await TellGateway(state.PlayerId, Protocol.SC_QueryInventory, payload);
        }

        private async Task CS_HandleSwapStorageSlot(CS_SwapStorageSlot message)
        {
            var result = storage.SwapItem(message.Slot1, message.Slot2, out var _);
            ItemData item1 = null;
            ItemData item2 = null;
            if (!result)
            {
                storage.TryGetItem(message.Slot1, out item1);
                storage.TryGetItem(message.Slot2, out item2);
            }
            var payload = new ServerSwapStorageSlotResponse(message.ReqId, result, item1, item2);
            await TellGateway(state.PlayerId, Protocol.SC_SwapStorageSlot, payload);
        }

        private async Task CS_HandleUseItem(CS_UseItem message)
        {
            if (!storage.TryGetItem(message.Slot, out var item) && item.InstanceId == message.InstanceId) return;
            switch (item)
            {
                case EquipData equip:
                    await HandleEquipItem(message.Slot, equip);
                    break;
            }
        }

        private async Task HandleEquipItem(SlotKey sourceSlot, EquipData equip)
        {
            if(!storage.TryGetEquipSlotIndex(equip.EquipType, out var targetSlot)) return;
            if(storage.SwapItem(sourceSlot, targetSlot, out var changedSlots))
            {
                var changes = new Dictionary<SlotKey, ItemData>();

                if (storage.TryGetItem(sourceSlot, out var oldItem)) changes[sourceSlot] = oldItem;
                else changes[sourceSlot] = null;

                if (storage.TryGetItem(targetSlot, out var newItem)) changes[targetSlot] = newItem;
                var payload = new ServerSlotUpdated
                {
                    Items = changes,
                    MaxSize = storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory)
                };

                await TellGateway(state.PlayerId, Protocol.SC_UseItem, payload);
                var attributeChanges = attribute.CalculateAttributeChange((EquipData)newItem, (EquipData)oldItem);

                ApplyAttributesToCharacter(attributeChanges);

                //await TellGateway(
                //    state.PlayerId, 
                //    Protocol.SC_EntityStatsUpdate, 
                //    new ServerEntityStatsUpdate
                //{
                //    EntityId = state.EntityId,
                //    Attributes = attributeChanges
                //});
            }
        }

        private void ApplyAttributesToCharacter(Dictionary<AttributeType, float> changes)
        {
            foreach (var kvp in changes)
            {
                if (state.ExtraAttributes.ContainsKey(kvp.Key))
                {
                    state.ExtraAttributes[kvp.Key] += kvp.Value;
                }
                else
                {
                    state.ExtraAttributes[kvp.Key] = kvp.Value;
                }
            }

            // 特殊处理：如果涉及到 MaxHp / MaxMp 的变化，可能需要调整 CurrentHp
            // 比如脱下加血装备，当前血量不能超过新的上限
            if (changes.ContainsKey(AttributeType.MaxHp))
            {
                float newMaxHp = state.BaseAttributes.GetValueOrDefault(AttributeType.MaxHp, 0)
                                 + state.ExtraAttributes.GetValueOrDefault(AttributeType.MaxHp, 0);
                
                if (state.Hp > newMaxHp) state.Hp = newMaxHp;
            }
        }

        #endregion

        #region 好友相关
        private async Task CS_HandleAddFriend()
        {

        }

        private async Task CS_HandleDeleteFriend()
        {

        }

        private async Task CS_HandleAddFriendRequest()
        {

        }

        private async Task CS_HandleFirendChat()
        {

        }

        private async Task CS_HandleAlterFriendRemark()
        {

        }

        private async Task CS_HandleMoveFriendToGroup()
        {

        }

        private async Task CS_HandleAlterFriendGroup()
        {

        }

        private async Task CS_HandleAddFriendGroup()
        {

        }


        #endregion

    }
}
