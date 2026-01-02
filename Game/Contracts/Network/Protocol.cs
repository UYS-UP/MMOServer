using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Network
{

    public enum Protocol : ushort
    {
        Heart,
        SC_Login,
        SC_Register,
        SC_CreateCharacter,
        SC_EnterGame,
        SC_EnterRegion,
        SC_LevelDungeon,
        SC_EnterDungeon,

        SC_EntitySpawn,
        SC_EntityDespawn,
        SC_CharacterMove,
        SC_EntityMove,
        SC_CharacterCastSkill,
        SC_EntityCastSkill,
        SC_EntityDeath,
        SC_ApplyBuff,
        SC_EntityDamage,

        SC_EntityStatsUpdate,

        SC_DungeonLootInfo,
        SC_DungeonLootChoice,
        SC_QuestListSync,
        SC_QuestUpdated,
        SC_QuestCompleted,
        SC_QuestAccepted,

        SC_QueryInventory,
        SC_AddInventoryItem,
        SC_SwapStorageSlot,
        SC_UseItem,
        SC_DropItem,

        SC_FriendListSync,

        SC_TeamCreated,
        SC_StartDungeon,
        SC_TeamQuited,
        SC_EnterTeam,


        CS_Login,
        CS_Register,
        CS_CreateCharacter,

        CS_EnterRegion,
        CS_EnterGame,
        CS_EnterDungeon,
        CS_StartDungeon,
        CS_LevelDungeon,


        CS_CharacterMove,
        CS_CharacterCastSkill,
        CS_DungeonLootChoice,


        CS_QueryInventory,
        CS_SwapStorageSlot,
        CS_UseItem,
        CS_DropItem,

        CS_CreateTeam,
        CS_TeamInvite,
        CS_AcceptInvite,

        CS_AddFriend,
        CS_DeleteFriend,
        CS_FriendRequest,
        CS_FriendChat,
        CS_FriendRemark,
        CS_MoveFriendToGroup,
        CS_AlterFriendGroup,
        CS_AddFriendGroup,

        CS_AcceptQuest,
        CS_SubmitQuest,
        GM_AddItem,
        CS_QuitTeam,
    }

    public enum StateCode
    {
        Success = 200,         // 成功
        BadRequest = 400,      // 错误请求
        Unauthorized = 401,    // 未授权
        NotFound = 404,        // 未找到
        InternalError = 500,   // 服务器内部错误
    }
}
