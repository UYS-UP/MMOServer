using NPOI.SS.Formula.Functions;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Actor.Domain.Gateway;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Network;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ASession
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

        private async Task ConnctionClosed(ConnectionClosed closed)
        {
            if(closed.SessionId == sessionId)
            {
                await Stop();
            }
        }

        private Task BindAccount(BindAccount bind)
        {
            if(bind.SessionId != sessionId) return Task.CompletedTask;
            accountId = bind.PlayerId;
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
                    {
                        var data = packet.DeSerializePayload<ClientHeartPing>();
                        message = new HeartPing(data.ClientUtcMs, sessionId);
                        receiver = GameField.GetActor<TimeActor>();
                        break;
                    }
                case Protocol.CS_Register:
                    {
                        var data = packet.DeSerializePayload<ClientPlayerRegister>();
                        message = new CS_PlayerRegister(data.Username, data.Password, data.RePassword, session.Id);
                        receiver = GameField.GetActor<AuthActor>();
                        break;
                    }

                case Protocol.CS_Login:
                    {
                        var data = packet.DeSerializePayload<ClientPlayerLogin>();
                        message = new CS_PlayerLogin(session.Id, data.Username, data.Password);
                        receiver = GameField.GetActor<AuthActor>();
                        break;
                    }
                case Protocol.CS_CreateCharacter:
                    {
                        var data = packet.DeSerializePayload<ClientCreateCharacter>();
                        message = new CS_CreateCharacter(sessionId, accountId, data.CharacterName, data.ServerId);
                        receiver = GameField.GetActor<AuthActor>();
                    }
                    break;






                case Protocol.CS_EnterGame:
                    {
                        var data = packet.DeSerializePayload<ClientEnterGame>();
                        message = new CS_PlayerEnterGame(data.CharacterId);
                        receiver = GameField.GetActor<AuthActor>();
                        break;
                    }
                case Protocol.CS_EnterRegion:
                    {
                        var data = packet.DeSerializePayload<ClientEnterRegion>();
                        message = new CS_CharacterEnterRegion(data.MapId);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }
                case Protocol.CS_StartDungeon:
                    {
                        var data = packet.DeSerializePayload<ClientStartDungeon>();
                        message = new CS_StartDungeon(data.TeamId, data.TemplateId);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }
      
                case Protocol.CS_EnterDungeon:
                    {
                        var data = packet.DeSerializePayload<ClientEnterDungeon>();
                        message = new CS_CharacterEnterDungeon(data.DungeonTemplateId);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }
                case Protocol.CS_LevelDungeon:
                    {
                        message = new CS_CharacterLevelDungeon();
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                    }
                    break;



                case Protocol.CS_CharacterMove:
                    {
                        var data = packet.DeSerializePayload<ClientCharacterMove>();
                        message = new CS_CharacterMove(
                            data.ClientTick,
                            HelperUtility.ShortArrayToVector3(data.Position),
                            HelperUtility.ShortToYaw(data.Yaw),
                            HelperUtility.SbyteArrayToVector3(data.Direction)
                            );
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }
                case Protocol.CS_CharacterCastSkill:
                    {
                        var data = packet.DeSerializePayload<ClientCharacterCastSkill>();
                        message = new CS_CharacterCastSkill(
                            data.ClientTick,
                            data.SkillId,
                            data.InputType,
                            data.TargetPosition,
                            data.TargetDirection,
                            data.TargetEntityId
                            );
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }
        
                case Protocol.CS_DungeonLootChoice:
                    {
                        var data = packet.DeSerializePayload<ClientDungeonLootChoice>();
                        message = new CS_DungeonLootChoice(data.ItemId, data.IsRoll);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }
                   

                case Protocol.CS_QueryInventory:
                    {
                        var data = packet.DeSerializePayload<ClientQueryInventory>();
                        message = new CS_QueryInventory(data.StartSlot, data.EndSlot);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }

                case Protocol.CS_SwapStorageSlot:
                    {
                        var swapStorageSlot = packet.DeSerializePayload<ClientSwapStorageSlot>();
                        message = new CS_SwapStorageSlot(swapStorageSlot.ReqId, swapStorageSlot.Slot1, swapStorageSlot.Slot2);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                        break;
                    }




                case Protocol.CS_AcceptQuest:
                    var questAccept = packet.DeSerializePayload<ClientQuestAccept>();
                    break;
                case Protocol.CS_CreateDungeonTeam:
                    break;
                case Protocol.CS_TeamInvite:
                    break;
                case Protocol.CS_AcceptInvite:
                    break;
                case Protocol.CS_AddFriend:
                    break;
                case Protocol.CS_FriendRequest:
                    break;



                case Protocol.GM_AddItem:
                    {
                        var data = packet.DeSerializePayload<GMAddItem>();
                        message = new GM_AddItem(data.TemplateId, data.Count);
                        receiver = GameField.GetActor<CharacterActor>(accountId);
                    }
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

