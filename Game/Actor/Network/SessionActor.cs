using NPOI.SS.Formula.Functions;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Auth;
using Server.Game.Actor.Domain.Player;
using Server.Game.Actor.Domain.Team;
using Server.Game.Actor.Domain.Time;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Network;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Network
{
    public class SessionActor : ActorBase
    {
        private readonly Guid sessionId;
        private ISession session;
        private string accountId;
        private readonly GameServer gameServer;
        private readonly ActorEventBus bus;

        public SessionActor(string actorId, Guid sessionId, GameServer gameServer, ActorEventBus bus) : base(actorId)
        {
            this.sessionId = sessionId;
            this.gameServer = gameServer;
            this.bus = bus;
        }



        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case ConnectionOpened opened:
                    await ConnectionOpened(opened);
                    break;

                case RawPacketReceived incoming:
                    await RouteIncoming(incoming.Packet);
                    break;

                case BindAccount bind:
                    await BindAccount(bind);
                    break;

                case ConnectionClosed closed when closed.SessionId == sessionId:
                    await ConnctionClosed(closed);
                    break;
            }
        }

        private Task ConnctionClosed(ConnectionClosed closed)
        {
            if(closed.SessionId == sessionId)
            {
                this.Stop();
            }
            return Task.CompletedTask;
        }

        private Task BindAccount(BindAccount bind)
        {
            if(bind.SessionId != sessionId) return Task.CompletedTask;
            accountId = bind.PlayerId;
            System.CreateActor(new PlayerActor($"PlayerActor_{accountId}", accountId, bus));
            gameServer.BindSessionToAccount(bind.SessionId, accountId);
            return Task.CompletedTask;
        }

        private Task ConnectionOpened(ConnectionOpened opened)
        {
            session = opened.Session;
            return Task.CompletedTask;
        }

        private async Task RouteIncoming(GamePacket packet)
        {
            var protocol = (Protocol)packet.ProtocolId;
            
            IActorMessage message = null;
            string receiver = "";
            switch (protocol)
            {
                case Protocol.Heart:
                    var ping = packet.DeSerializePayload<ClientHeartPing>();
                    message = new HeartPing(ping.ClientUtcMs, sessionId);
                    receiver = nameof(TimeActor);
                    break;
                case Protocol.Register:
                    var registerData = packet.DeSerializePayload<ClientPlayerRegister>();
                    message =  new PlayerRegister(registerData.Username, registerData.Password, registerData.RePassword, session.Id);
                    receiver = nameof(AuthActor);
                    break;
                case Protocol.Login:
                    var loginData = packet.DeSerializePayload<ClientPlayerLogin>();
                    message = new PlayerLogin(session.Id, loginData.Username, loginData.Password);
                    receiver = nameof(AuthActor);
                    break;
                case Protocol.CreateCharacter:
                    var createRoleData = packet.DeSerializePayload<ClientCreateCharacter>();
                    message = new PlayerCreateCharacter(sessionId, accountId, createRoleData.CharacterName, createRoleData.Profession);
                    receiver = nameof(AuthActor);
                    break;
                case Protocol.EnterGame:
                    var CharacterId = packet.DeSerializePayload<string>();
                    message = new PlayerEnterGameRequest(CharacterId);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.PlayerMove:
                    var playerMove = packet.DeSerializePayload<ClientPlayerMove>();
                    var pos = HelperUtility.ShortArrayToVector3(playerMove.Position);
                    var yaw = HelperUtility.ShortToYaw(playerMove.Yaw);
                    var direction = HelperUtility.SbyteArrayToVector3(playerMove.Direction);
                    message =  new PlayerMoveRequest(playerMove.ClientTick, pos, yaw, direction);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.PlayerReleaseSkill:
                    var releaseSkill = packet.DeSerializePayload<ClientPlayerReleaseSkill>();
                    message = new PlayerSkillReleaseRequest(releaseSkill.SkillId, releaseSkill.SkillId, releaseSkill.InputType, 
                        releaseSkill.TargetPosition, releaseSkill.TargetDirection, releaseSkill.TargetEntityId);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.QueryInventory:
                    var queryInventory = packet.DeSerializePayload<ClientPlayerQueryInventory>();
                    message = new PlayerQueryInventoryRequest(queryInventory.StartSlot, queryInventory.EndSlot);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.SwapStorageSlot:
                    var swapStorageSlot = packet.DeSerializePayload<ClientSwapStorageSlot>();
                    message = new PlayerSwapStorageSlotRequest(swapStorageSlot.ReqId, swapStorageSlot.Slot1, swapStorageSlot.Slot2);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.QuestAccept:
                    var questAccept = packet.DeSerializePayload<ClientQuestAccept>();
                    break;


                case Protocol.InvitePlayer:
                    var invitePlayer = packet.DeSerializePayload<int>();
                    message = new PlayerInviteRegionCharacterRequest();
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.AcceptInvite:
                    var acceptInvite = packet.DeSerializePayload<int>();
                    message = new PlayerAcceptTeamInviteRequest(acceptInvite);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.AddFriend:
                    var addFriend = packet.DeSerializePayload<ClientAddFriend>();
                    message = new PlayerAddFriendRequest(addFriend.CharacterName);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.HandleFriendRequest:
                    var handleFriendRequest = packet.DeSerializePayload<ClientHandleAddFriend>();
                    message = new PlayerHandleAddFriendRequest(handleFriendRequest.Accept, handleFriendRequest.RequestId);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.DungeonLootChoice:
                    var dungonLootChoice = packet.DeSerializePayload<ClientDungeonLootChoice>();
                    message = new PlayerDungeonLootChoiceRequest(dungonLootChoice.ItemId, dungonLootChoice.IsRoll);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                //  <!-----------队伍相关-----------!>
                case Protocol.CreateDungeonTeam:
                    var createDungeonTeam = packet.DeSerializePayload<ClientCreateDungeonTeam>();
                    message = new PlayerCreateDungeonTeamRequest(createDungeonTeam.TemplateId, createDungeonTeam.TeamName);
                    receiver = $"PlayerActor_{accountId}";
                    break;

                // <!-----------进入/离开 副本/区域-----------!>
                case Protocol.StartDungeon:
                    var startDungeon = packet.DeSerializePayload<int>();
                    message = new PlayerStartDungeonRequest(startDungeon);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.EnterDungeon:
                    message = new PlayerEnterDungeonRequest();
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.LevelDungeon:
                    message = new PlayerLevelDungeonRequest();
                    receiver = $"PlayerActor_{accountId}";
                    break;

                case Protocol.EnterRegion:
                    message = new PlayerEnterRegionRequest();
                    receiver = $"PlayerActor_{accountId}";
                    break;
                case Protocol.LevelRegion:
                    var levelRegion = packet.DeSerializePayload<string>();
                    message = new PlayerLevelRegionRequest(levelRegion);
                    receiver = $"PlayerActor_{accountId}";
                    break;
                default:
                    Console.WriteLine($"[SessionActor {sessionId}] 未映射协议 {packet.ProtocolId}");
                    return;
            }

            if (!System.IsActorAlive(receiver))
            {
                Console.WriteLine($"[SessionActor {sessionId}] 目标Actor不存在 Prot={packet.ProtocolId}");
                return;
            }
            await TellAsync(receiver, message);
        }
    }
}

