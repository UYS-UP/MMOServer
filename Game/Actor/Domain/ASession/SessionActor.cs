using MessagePack;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Actor.Domain.Team;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.World;
using Server.Network;
using Server.Utility;

namespace Server.Game.Actor.Domain.ASession
{
    public class SessionActor : ActorBase
    {
        private readonly Guid sessionId;
        private ISession session;
        private string playerId;
        private string characterId;
        private int entityId;
        private int mapId;
        private int dungeonId;
        private readonly GameServer gameServer;
        private ActorEventBus EventBus => System.EventBus;

        public SessionActor(string actorId, Guid sessionId, GameServer gameServer) : base(actorId)
        {
            this.sessionId = sessionId;
            this.gameServer = gameServer;
        }



        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case ConnectionOpened opened:
                    await ConnectionOpened(opened);
                    break;
                case ConnectionClosed closed when closed.SessionId == sessionId:
                    await ConnctionClosed(closed);
                    break;
                case RawPacketReceived incoming:
                    await RouteIncoming(incoming.Packet);
                    break;


                case SendTo sendTo:
                    await SendTo(sendTo);
                    break;

                case BindPlayerId bindPlayerId:
                    await BindPlayerId(bindPlayerId);
                    break;
                case BindCharacterIdAndEntityId bindCharacterIdAndEntityId:
                    await BindCharacterIdAndEntityId(bindCharacterIdAndEntityId);
                    break;
                case CharacterWorldSync bindWorldPos:
                    await CharacterWorldSync(bindWorldPos);
                    break;

            }
        }

        private async Task ConnctionClosed(ConnectionClosed closed)
        {
            if(closed.SessionId == sessionId)
            {
                if (!string.IsNullOrEmpty(playerId))
                {
                    System.SessionRouter.UnregisterAccount(playerId);
        
                }
                if (!string.IsNullOrEmpty(characterId))
                {
                    System.SessionRouter.UnregisterCharacter(characterId);
                }
                await Stop();
            }
        }

        private async Task SendTo(SendTo sendTo)
        {
            await gameServer.SendTo(sessionId, sendTo.Protocol, sendTo.Bytes);
        }

        private Task BindPlayerId(BindPlayerId bind)
        {
            playerId = bind.PlayerId;
            System.SessionRouter.RegisterAccount(playerId, ActorId);
            return Task.CompletedTask;
        }

        private Task BindCharacterIdAndEntityId(BindCharacterIdAndEntityId bindCharacterId)
        {
            Console.WriteLine("BindCharacterIdAndEntityId");
            characterId = bindCharacterId.CharacterId;
            entityId = bindCharacterId.EntityId;
            System.SessionRouter.RegisterCharacter(characterId, ActorId);
            return Task.CompletedTask;
        }

        private Task CharacterWorldSync(CharacterWorldSync characterWorldSync)
        {
            mapId = characterWorldSync.MapId;
            dungeonId = characterWorldSync.DungeonId;
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
                        message = new CS_CreateCharacter(playerId, data.CharacterName, data.ServerId);
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
                        receiver = GameField.GetActor<CharacterActor>(characterId);
                        break;
                    }
                case Protocol.CS_StartDungeon:
                    {
                        var data = packet.DeSerializePayload<ClientStartDungeon>();
                        message = new CS_StartDungeon(data.TeamId, data.TemplateId, playerId);
                        receiver = GameField.GetActor<TeamActor>();
                        break;
                    }
      
                case Protocol.CS_EnterDungeon:
                    {
                        var data = packet.DeSerializePayload<ClientEnterDungeon>();
                        message = new CS_CharacterEnterDungeon(data.DungeonTemplateId);
                        receiver = GameField.GetActor<CharacterActor>(characterId);
                        break;
                    }
                case Protocol.CS_LevelDungeon:
                    {
                        message = new CS_CharacterLevelDungeon();
                        receiver = GameField.GetActor<CharacterActor>(characterId);
                        break;
                    }
                case Protocol.CS_CreateTeam:
                    {
                        var data = packet.DeSerializePayload<ClientCreateTeam>();
                        message = new CS_CreateTeam(characterId, data.CharacterName, data.CharacterLevel);
                        receiver = GameField.GetActor<TeamActor>();
                        break;
                    }
                case Protocol.CS_QuitTeam:
                    {
                        var data = packet.DeSerializePayload<ClientQuitTeam>();
                        message = new CS_QuitTeam(data.TeamId, characterId);
                        break;
                    }
                case Protocol.CS_TeamInvite:
                    {
                        break;
                    }


                case Protocol.CS_CharacterMove:
                    {
                        var data = packet.DeSerializePayload<ClientCharacterMove>();
                        message = new CS_CharacterMove(
                            data.ClientTick,
                            entityId,
                            HelperUtility.ShortArrayToVector3(data.Position),
                            HelperUtility.ShortToYaw(data.Yaw),
                            HelperUtility.SbyteArrayToVector3(data.Direction),
                            mapId,
                            dungeonId
                            );
                        if (dungeonId == -1) receiver = GameField.GetActor<RegionActor>(mapId);
                        else receiver = GameField.GetActor<DungeonActor>();
                        break;
                    }
                case Protocol.CS_CharacterCastSkill:
                    {
                        var data = packet.DeSerializePayload<ClientCharacterCastSkill>();
                        message = new CS_CharacterCastSkill(
                            data.ClientTick,
                            data.SkillId,
                            entityId,
                            data.InputType,
                            data.TargetPosition,
                            data.TargetDirection,
                            data.TargetEntityId,
                            mapId,
                            dungeonId
                            );
                        if (dungeonId == -1) receiver = GameField.GetActor<RegionActor>(mapId);
                        else receiver = GameField.GetActor<DungeonActor>();
                        break;
                    }
        
                case Protocol.CS_DungeonLootChoice:
                    {
                        var data = packet.DeSerializePayload<ClientDungeonLootChoice>();
                        message = new CS_DungeonLootChoice(dungeonId, characterId, data.ItemId, data.IsRoll);
                        receiver = GameField.GetActor<DungeonActor>();
                        break;
                    }
                   

                case Protocol.CS_QueryInventory:
                    {
                        var data = packet.DeSerializePayload<ClientQueryInventory>();
                        message = new CS_QueryInventory(data.StartSlot, data.EndSlot);
                        receiver = GameField.GetActor<CharacterActor>(characterId);
                        break;
                    }

                case Protocol.CS_SwapStorageSlot:
                    {
                        var swapStorageSlot = packet.DeSerializePayload<ClientSwapStorageSlot>();
                        message = new CS_SwapStorageSlot(swapStorageSlot.ReqId, swapStorageSlot.Slot1, swapStorageSlot.Slot2);
                        receiver = GameField.GetActor<CharacterActor>(characterId);
                        break;
                    }




                case Protocol.CS_AcceptQuest:
                    var questAccept = packet.DeSerializePayload<ClientQuestAccept>();
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
                        receiver = GameField.GetActor<CharacterActor>(characterId);
                    }
                    break;


                default:
                    Console.WriteLine($"[SessionActor {sessionId}] 未映射协议 {packet.ProtocolId}");
                    return;
            }

            if (!System.IsActorAlive(receiver))
            {
                Console.WriteLine($"[SessionActor {sessionId}] 目标Actor不存在 Receiver={receiver}");
                return;
            }
            await TellAsync(receiver, message);
        }
    }
}

