using MessagePack;
using Org.BouncyCastle.Ocsp;
using Server.Data;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Player;
using Server.Game.Actor.Network;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Network;
using Server.Utility;
using static MessagePack.GeneratedMessagePackResolver.Server.Game.Actor.Domain;

namespace Server.Game.Actor.Domain.Auth
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

        protected override void OnStart()
        {
            base.OnStart();
            eventBus.Subscribe<PlayerDisconnectionEvent>(ActorId);
        }

        protected override void OnStop()
        {
            base.OnStop();
            eventBus.Unsubscribe<PlayerDisconnectionEvent>(ActorId);
        }


        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case PlayerLogin playerLogin:
                    await HandlePlayerLogin(playerLogin);
                    break;
                case PlayerCreateCharacter createRole:
                    await HandleCreateRole(createRole);
                    break;
                case PlayerRegister playerRegister:
                    await HandlePlayerRegister(playerRegister);
                    break;


                case PlayerDisconnectionEvent playerDisconnection:
                    await OnPlayerDisconnection(playerDisconnection);
                    break;
            }

        }

        private async Task HandlePlayerRegister(PlayerRegister playerRegister)
        {
            var resp = await DatabaseService.PlayerService.RegisterAsync(playerRegister.Username, playerRegister.Password, "123");
            await TellGateway(new SendToSession(playerRegister.SessionId, Protocol.Register, resp));
        }

        private async Task HandlePlayerLogin(PlayerLogin playerLogin)
        {

            var resp = await DatabaseService.PlayerService.LoginAsync(playerLogin.Username, playerLogin.Password);
            if(resp.Code == StateCode.Success)
            {
                Console.WriteLine("登录成功:" + resp.Data.Username);
                var playerId = resp.Data.PlayerId;

                if(onlinePlayers.ContainsKey(playerId) && playerSessions.ContainsKey(playerId))
                {

                    await TellGateway(new SendToSession(playerLogin.SessionId, Protocol.Login, ResponseMessage<string>.Fail("账户已经被登录")));
                }

                await TellAsync($"SessionActor_{playerLogin.SessionId}", new BindAccount(playerLogin.SessionId, playerId));

                onlinePlayers[playerId] = resp.Data;
                playerSessions[playerId] = playerLogin.SessionId;


                await TellGateway(new SendToSession(playerLogin.SessionId, Protocol.Login, resp));
            }
        }

        private async Task HandleCreateRole(PlayerCreateCharacter createRole)
        {
            if (!onlinePlayers.ContainsKey(createRole.PlayerId))
            {
                var err = ResponseMessage<string>.Fail("玩家未登录", StateCode.Unauthorized);
                await TellGateway(new SendToSession(createRole.SessionId, Protocol.CreateCharacter, err));
                return;
            }
            // var config = DataLoader.RoleBaseTable[((int)createRole.Profession).ToString()];
            var (resp, character) =  await DatabaseService.CharacterService.CreateCharacterAsync(createRole.PlayerId, new Character
            {

                CharacterName = createRole.CharacterName,
                HP = 100,
                MP = 100,
                Gold = 0,
                Level = 1,
                Profession = createRole.Profession
            });
            await DatabaseService.FriendService.CreateGroupAsync(character.CharacterId, "我的好友");
            await TellGateway(new SendToPlayer(createRole.PlayerId, Protocol.CreateCharacter, resp));
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
