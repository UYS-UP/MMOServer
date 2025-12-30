using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain;
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



    public record class PlayerMoveRequest(int ClientTick, Vector3 Position, float Yaw, Vector3 Direction) : IActorMessage;

    public record class PlayerQueryInventoryRequest(int StartSlot, int EndSlot) : IActorMessage;




    public record class PlayerEnterRegionRequest(): IActorMessage;

    public record class PlayerInviteRegionCharacterRequest() : IActorMessage;

    public record class PlayerAcceptTeamInviteRequest(int TeamId) : IActorMessage;


    public record class PlayerAddFriendRequest(string CharacterName) : IActorMessage;

    public record class PlayerAddFriendGroupRequest(string GroupName) : IActorMessage;
    
    public record class PlayerAlterFriendGroupRequest(string GroupId, string NewName) : IActorMessage;

    public record class PlayerMoveFriendToGroupRequest(string CharacterId, string GroupName) : IActorMessage;

    public record class PlayerHandleAddFriendRequest(bool Accept, string RequestId) : IActorMessage;

    public record class PlayerDeleteFriendRequest(string CharacterId) : IActorMessage;

    public record class PlayerAlterFriendRemarkRequest(string CharacterId, string Remark) : IActorMessage;

    public record class PlayerFriendChatRequest(string CharacterId) : IActorMessage;

    public record class PlayerInviteFriendRequest(string CharacterId) : IActorMessage;




    public record class PlayerLevelRegionRequest(string RegionId) : IActorMessage;
    #endregion





    public record class LoadedDungeon(int TeamId, string PlayerId) : IActorMessage;

    public record class EnterDungeon(string TemplateId) : IActorMessage;






    public record class LevelDungeon(string Cause) : IActorMessage;
    public record class LoadDungeon(string DungeonId, string TemplateId) : IActorMessage;




    public record class AddFriendRequest(string RequestId, string SenderCharacterName, string ReceiverCharacterName, string Remark) : IActorMessage;

    public record class AddFriend(string CharacterId, NetworkFriendData Data) : IActorMessage;



}
