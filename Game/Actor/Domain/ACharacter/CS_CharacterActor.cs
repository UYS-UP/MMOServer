using Server.DataBase.Entities;
using Server.Game.Actor.Domain.Gateway;
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
            var actor = GameField.GetActor<RegionActor>(state.MapId);
            await TellAsync(actor, new A_CharacterSpawn(CreateRuntimeFromState()));
        }

        private async Task CS_HandleCharacterChangeRegion(CS_CharacterChangeRegion message)
        {
            var actor = GameField.GetActor<RegionActor>(state.MapId);
            await TellAsync(actor, new A_CharacterDespawn(state.EntityId));
            state.MapId = message.MapId;
            await TellGateway(
                new SendToPlayer(
                    state.PlayerId, 
                    Protocol.SC_EnterRegion, 
                    new ServerEnterRegion { MapId = state.MapId }
                )
            );
        }

        private async Task CS_HandleCharacterEnterDungeon(CS_CharacterEnterDungeon message)
        {
            var actor = GameField.GetActor<DungeonActor>();
            await TellAsync(actor, new A_CharacterSpawn(CreateRuntimeFromState()));
        }

        private async Task CS_HandleCharacterLevelDungeon(CS_CharacterLevelDungeon message)
        {
            var actor = GameField.GetActor<DungeonActor>();
            await TellAsync(actor, new A_CharacterDespawn(state.EntityId, state.DungeonId));
        }

        private async Task CS_HandleCharacterMove(CS_CharacterMove message)
        {
            var actor = GameField.GetActor<RegionActor>(state.MapId);
            if(state.DungeonId == -1)
            {
                actor = GameField.GetActor<DungeonActor>();
            }
            await TellAsync(actor, new A_CharacterMove(
                message.ClientTick, state.EntityId,
                message.Position, message.Yaw, message.Direction,
                state.MapId, state.DungeonId));
        }

        private async Task CS_HandleCharacterCastSkill(CS_CharacterCastSkill message)
        {
            var actor = GameField.GetActor<RegionActor>(state.MapId);
            if (state.DungeonId == -1)
            {
                actor = GameField.GetActor<DungeonActor>();
            }
            await TellAsync(actor, new A_CharacterCastSkill(message.ClientTick,
                message.SkillId, state.EntityId, state.DungeonId,
                message.InputType, message.TargetPosition, message.TargetDirection,
                message.TargetEntityId));
        }


        #endregion


        #region 副本相关

        private async Task CS_HandleStartDungeon(CS_StartDungeon message)
        {
            await TellAsync(nameof(TeamActor), new A_StartDungeon(message.TeamId, message.TemplateId, state.PlayerId));
        }

        private async Task CS_DungeonLootChoice(CS_DungeonLootChoice message)
        {
            var actor = GameField.GetActor<DungeonActor>();
            await TellAsync(
                actor,
                new A_DungeonLootChoice(
                    state.DungeonId,
                    state.EntityId,
                    message.ItemId,
                    message.IsRoll)
                );
        }


        #endregion

        #region 队伍相关
        private async Task CS_HandleCreateTeam(CS_CreateTeam message)
        {
            await TellAsync(nameof(TeamActor), new A_CreateTeam(
                state.PlayerId,
                state.CharacterId,
                state.Name,
                state.Level,
                message.TeamName));
        }

        private Task CS_HandleTeamInvite(CS_TeamInvite message)
        {
            return Task.CompletedTask;
        }
        #endregion


        #region 背包相关

        private async Task CS_HandleQueryInventory(CS_QueryInventory message)
        {
            var payload = new ServerQueryInventory(
                storage.MaxInventorySize,
                storage.GetRangeSlotItems(SlotContainerType.Inventory, message.StartSlot, message.EndSlot),
                storage.GetMaxOccupiedSlotIndex(SlotContainerType.Inventory)
                );
            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_QueryInventory, payload));
        }

        private async Task CS_HandleSwapStorageSlot(CS_SwapStorageSlot message)
        {
            var result = storage.SwapItems(message.Slot1, message.Slot2);
            ItemData item1 = null;
            ItemData item2 = null;
            if (!result)
            {
                storage.TryGetItem(message.Slot1, out item1);
                storage.TryGetItem(message.Slot2, out item2);
            }
            var payload = new ServerSwapStorageSlotResponse(message.ReqId, result, item1, item2);
            await TellGateway(new SendToPlayer(state.PlayerId, Protocol.SC_SwapStorageSlot, payload));
        }

        private async Task CS_HandleUseItem()
        {

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
