using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Player;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Actor
{

    #region NetworkMessage->ActorMessage
    public record class PlayerLogin(Guid SessionId, string Username, string Password) : IActorMessage;

    public record class PlayerRegister(string Username, string Password, string RePassword, Guid SessionId) : IActorMessage;

    public record class PlayerCreateCharacter(Guid SessionId, string PlayerId, string CharacterName, ProfessionType Profession) : IActorMessage;

    public record class PlayerEnterGameRequest(string CharacterId) : IActorMessage;

    public record class PlayerMoveRequest(int ClientTick, Vector3 Position, float Yaw, Vector3 Direction) : IActorMessage;

    public record class PlayerQueryInventoryRequest(int StartSlot, int EndSlot) : IActorMessage;

    public record class PlayerSwapStorageSlotRequest(int ReqId, SlotKey Slot1, SlotKey Slot2) : IActorMessage;

    public record class PlayerCreateDungeonTeamRequest(string TemplateId, string TeamName) : IActorMessage;

    public record class PlayerStartDungeonRequest(int TeamId) : IActorMessage;

    public record class PlayerEnterDungeonRequest() : IActorMessage;
    public record class PlayerEnterRegionRequest(): IActorMessage;

    public record class PlayerInviteRegionCharacterRequest() : IActorMessage;

    public record class PlayerAcceptTeamInviteRequest(int TeamId) : IActorMessage;

    public record class PlayerSkillReleaseRequest(int ClientTick, int SkillId, SkillCastInputType InputType, Vector3 TargetPosition, Vector3 TargetDirection, string TargetEntityId) : IActorMessage;

    public record class PlayerAddFriendRequest(string CharacterName) : IActorMessage;

    public record class PlayerAddFriendGroupRequest(string GroupName) : IActorMessage;
    
    public record class PlayerAlterFriendGroupRequest(string GroupId, string NewName) : IActorMessage;

    public record class PlayerMoveFriendToGroupRequest(string CharacterId, string GroupName) : IActorMessage;

    public record class PlayerHandleAddFriendRequest(bool Accept, string RequestId) : IActorMessage;

    public record class PlayerDeleteFriendRequest(string CharacterId) : IActorMessage;

    public record class PlayerAlterFriendRemarkRequest(string CharacterId, string Remark) : IActorMessage;

    public record class PlayerFriendChatRequest(string CharacterId) : IActorMessage;

    public record class PlayerInviteFriendRequest(string CharacterId) : IActorMessage;

    public record class PlayerDungeonLootChoiceRequest(string ItemId, bool IsRoll) : IActorMessage;


    public record class PlayerLevelDungeonRequest() : IActorMessage;
    public record class PlayerLevelRegionRequest(string RegionId) : IActorMessage;
    #endregion

    public record class CreateDungeonTeam(
        string PlayerId, string CharacterId, string CharacterName, 
        int CharacterLevel, string TemplateId, string TeamName) : IActorMessage;

    public record class StartDungeon(int TeamId) : IActorMessage;

    public record class LoadedDungeon(int TeamId, string PlayerId) : IActorMessage;

    public record class EnterDungeon(string TemplateId) : IActorMessage;

    public record class CharacterSpawn(
        string EntityId, EntityType Type, string TemplateId,
        string Name, Vector3 Position, float Yaw, float Speed,
        int Level, int Hp, int MaxHp, int Mp, int MaxMp, int Ex, int MaxEx, int Attack, int Defence, float AttackRange,
        Dictionary<int, SkillRuntime> Skills, string RegionId, string DungeonId,
        ProfessionType Profession, string PlayerId, string CharacterId
        ) : IActorMessage;

    public record class LevelDungeon(string Cause) : IActorMessage;
    public record class LoadDungeon(string DungeonId, string TemplateId) : IActorMessage;

    public record class MonsterKiller(string EntityId, string MonsterTemplateId, IReadOnlyList<ItemData> DroppedItems) : IActorMessage, IQuestEvent
    {
        public bool Match(QuestObjective obj) => obj.Type switch
        {
            ObjectiveType.KillMonster => obj.TargetId == MonsterTemplateId,
            ObjectiveType.CollectItem => DroppedItems.Any(i => i.ItemTemplateId == obj.TargetId),
            _ => false
        };
    }

    public record class ItemAcquired(ItemData Item) : IActorMessage, IQuestEvent
    {
        public bool Match(QuestObjective obj) => obj.Type switch
        {
            ObjectiveType.CollectItem => obj.TargetId == Item.ItemTemplateId,
            _ => false
        };
    }

    public record class ItemsAcquired(IReadOnlyList<ItemData> Items) : IActorMessage, IQuestEvent
    {
        public bool Match(QuestObjective obj) => obj.Type switch
        {
            ObjectiveType.CollectItem => Items.Any(i => i.ItemTemplateId == obj.TargetId),
            _ => false
        };
    }

    public record class EnterTeam(int TeamId, string CharacterName, string PlayerId, string CharacterId, int Level) : IActorMessage;

    public record class AddFriendRequest(string RequestId, string SenderCharacterName, string ReceiverCharacterName, string Remark) : IActorMessage;

    public record class AddFriend(string CharacterId, NetworkFriendData Data) : IActorMessage;

    public record class PlayerDisconnectionEvent(string PlayerId) : IActorMessage;

}
