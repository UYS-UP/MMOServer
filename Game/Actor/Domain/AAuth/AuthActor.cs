using MessagePack;
using NPOI.Util;
using Org.BouncyCastle.Ocsp;
using Server.Data;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Actor.Domain.ASession;
using Server.Game.Actor.Domain.Gateway;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World;
using Server.Network;
using Server.Utility;
using System.Numerics;
using static MessagePack.GeneratedMessagePackResolver.Server.Game.Actor.Domain;

namespace Server.Game.Actor.Domain.AAuth
{
    /// <summary>
    /// 主要处理进入游戏之前的认证
    /// </summary>
    public class AuthActor : ActorBase
    {
        private readonly Dictionary<string, NetworkPlayer> onlinePlayers = new Dictionary<string, NetworkPlayer>();
        private readonly Dictionary<string, Guid> playerSessions = new Dictionary<string, Guid>();

        private ActorEventBus eventBus;

        public AuthActor(string actorId, ActorEventBus eventBus) : base(actorId)
        {
            this.eventBus = eventBus;
        }

        protected override async Task OnStart()
        {
            await base.OnStart();
            eventBus.Subscribe<PlayerDisconnectionEvent>(ActorId);
        }

        protected override async Task OnStop()
        {
            eventBus.Unsubscribe<PlayerDisconnectionEvent>(ActorId);
            await base.OnStop();
        
        }


        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case CS_PlayerLogin playerLogin:
                    await CS_HandlePlayerLogin(playerLogin);
                    break;
                case CS_CreateCharacter createRole:
                    await CS_HandleCreateCharacter(createRole);
                    break;
                case CS_PlayerRegister playerRegister:
                    await CS_HandlePlayerRegister(playerRegister);
                    break;

                case CS_PlayerEnterGame playerEnterGame:
                    await CS_HandlePlayerEnterGame(playerEnterGame);
                    break;

                case PlayerDisconnectionEvent playerDisconnection:
                    await OnPlayerDisconnection(playerDisconnection);
                    break;
            }

        }

        private async Task CS_HandlePlayerRegister(CS_PlayerRegister playerRegister)
        {
            var resp = await DatabaseService.PlayerService.RegisterAsync(playerRegister.Username, playerRegister.Password, "123");
            await TellGateway(new SendToSession(playerRegister.SessionId, Protocol.SC_Register, resp));
        }

        private async Task CS_HandlePlayerLogin(CS_PlayerLogin playerLogin)
        {

            var result = await DatabaseService.PlayerService.LoginAsync(playerLogin.Username, playerLogin.Password);
            if(!result.Succes)
            {
                await TellGateway(new SendToSession(playerLogin.SessionId, Protocol.SC_Login,
                    new ServerPlayerLogin
                    {
                        Sucess = false,
                        Message = result.Message,
                        Player = null,
                        Previews = null,
                    }));
            }
            
            Console.WriteLine("登录成功:" + result.Player.Username);
            var playerId = result.Player.PlayerId;

            if(onlinePlayers.ContainsKey(playerId) && playerSessions.ContainsKey(playerId))
            {

                await TellGateway(new SendToSession(playerLogin.SessionId, Protocol.SC_Login,
                    new ServerPlayerLogin
                    {
                        Sucess = false,
                        Message = "账号已经登录，请勿重新登录",
                        Player = null,
                        Previews = null,
                    }));
            }

            await TellAsync($"SessionActor_{playerLogin.SessionId}", new BindAccount(playerLogin.SessionId, playerId));

            var player = new NetworkPlayer
            {
                Password = playerLogin.Password,
                Username = playerLogin.Username,
                PlayerId = playerId,
            };

            onlinePlayers[playerId] = player;
            playerSessions[playerId] = playerLogin.SessionId;

            List<NetworkCharacterPreview> previews = new List<NetworkCharacterPreview>();
            foreach(var characer in result.Characters)
            {
                previews.Add(new NetworkCharacterPreview
                {
                    CharacterId = characer.CharacterId,
                    CharacterName = characer.Name,
                    Level = characer.Level,
                    MapId = characer.MapId,
                    ServerId = 0,
                    LastLoginTime = characer.LastLoginTime,
                });
            }

            await TellGateway(new SendToSession(playerLogin.SessionId, Protocol.SC_Login, 
                new ServerPlayerLogin
            {
                Sucess = true,
                Message = result.Message,
                Player = player,
                Previews = previews,
            }));
            
        }

        private async Task CS_HandleCreateCharacter(CS_CreateCharacter message)
        {
            if (!onlinePlayers.ContainsKey(message.PlayerId))
            {
                await TellGateway(new SendToSession(message.SessionId, Protocol.SC_CreateCharacter,
                    new ServerCreateCharacter
                    {
                        Message = "非法操作",
                        Success = false
                    }));
                return;
            }
            var result = await DatabaseService.CharacterService.CreateCharacterAsync(message.PlayerId, message.CharacterName, 0, message.ServerId);
            
            await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_CreateCharacter, new ServerCreateCharacter
            {
                Message = result.Message,
                Success = result.Sucess,
                CharacterId = result.character.CharacterId
            }));


        }

        private async Task CS_HandlePlayerEnterGame(CS_PlayerEnterGame message)
        {
            var character = await DatabaseService.CharacterService.GetCharacterFullAsync(message.CharacterId);
            await System.CreateActor(new CharacterActor(GameField.GetActor<CharacterActor>(character.PlayerId),
                character, eventBus));
        }

        



        private Task RemovePlayerSession(string playerId)
        {
            if (playerSessions.TryGetValue(playerId, out var session))
            {
                playerSessions.Remove(playerId);
            }
            onlinePlayers.Remove(playerId);
            return Task.CompletedTask;
        }

        private Task OnPlayerDisconnection(PlayerDisconnectionEvent args)
        {
            if (System.GetActor($"PlayerActor_{args.PlayerId}") != null)
            {
                System.StopActor($"PlayerActor_{args.PlayerId}");
            }
            RemovePlayerSession(args.PlayerId);
            Console.WriteLine($"[AuthActor] 玩家下线：{args.PlayerId}");
            return Task.CompletedTask;
        }

    }
}
