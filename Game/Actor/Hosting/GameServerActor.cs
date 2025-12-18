using Server.Game.Actor.Domain.Auth;
using Server.Game.Actor.Domain.Region;
using Server.Game.Actor.Domain.Time;
using Server.Game.Actor.Network;
using Server.Network;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Actor;
using Server.Game.Actor.Domain.Team;
using Server.Game.Actor.Domain.Chat;
using Server.Game.Actor.Core;
using Server.Game.World;

namespace Server.Game.Actor.Hosting
{
    public class GameServerActor : ActorBase
    {
        private readonly GameServer gameServer;
        private readonly ActorEventBus actorEventBus;
        private readonly IActorSystem actorSystem;

        public GameServerActor(string actorId, GameServer gameServer, IActorSystem actorSystem) :base(actorId)
        {
            this.gameServer = gameServer; ;
            this.actorSystem = actorSystem;
            actorEventBus = new ActorEventBus(actorSystem);
            actorSystem.CreateActor(new TimeActor(nameof(TimeActor), actorEventBus));
            actorSystem.CreateActor(new ChatActor(nameof(ChatActor)));
            actorSystem.CreateActor(new TeamActor(nameof(TeamActor)));



            actorSystem.CreateActor(new NetworkGatewayActor(nameof(NetworkGatewayActor), gameServer));
            actorSystem.CreateActor(new AuthActor(nameof(AuthActor), actorEventBus));
            actorSystem.CreateActor(new RegionActor("RegionActor_001", "001", actorEventBus));
            actorSystem.CreateActor(new DungeonActor(nameof(DungeonActor), actorEventBus));
 
            RegisterForwardingHandlers();

            gameServer.OnSessionOpened += async ses =>
            {
                await TellAsync(GetOrCreateSessionActor(ses.Id), new ConnectionOpened(ses));
            };
            gameServer.OnSessionClosed += async (sid, reason) =>
            {
                var sa = GetSessionActorIfExists(sid);
                if(string.IsNullOrEmpty(sa)) return;
                await TellAsync(sa, new ConnectionClosed(sid, reason));
            };

            Console.WriteLine("ActorGameServer 已初始化（SessionActor 本地路由 + NetworkGateway 出站）");
        }

        private void RegisterForwardingHandlers()
        {
            async Task Forward(GamePacket packet, ISession session)
            {
                await TellAsync(GetOrCreateSessionActor(session.Id), new RawPacketReceived(session, packet));
            }

            gameServer.RegisterHandler((ushort)Protocol.Heart, Forward);
            gameServer.RegisterHandler((ushort)Protocol.Login, Forward);
            gameServer.RegisterHandler((ushort)Protocol.Register, Forward);
            gameServer.RegisterHandler((ushort)Protocol.CreateCharacter, Forward);
            gameServer.RegisterHandler((ushort)Protocol.EnterGame, Forward);
            gameServer.RegisterHandler((ushort)Protocol.PlayerMove, Forward);
            gameServer.RegisterHandler((ushort)Protocol.PlayerReleaseSkill, Forward);
            gameServer.RegisterHandler((ushort)Protocol.QueryInventory, Forward);
            gameServer.RegisterHandler((ushort)Protocol.SwapStorageSlot, Forward);
            gameServer.RegisterHandler((ushort)Protocol.QuestAccept, Forward);
            gameServer.RegisterHandler((ushort)Protocol.CreateDungeonTeam, Forward);
            gameServer.RegisterHandler((ushort)Protocol.StartDungeon, Forward);
            gameServer.RegisterHandler((ushort)Protocol.EnterDungeon, Forward);
            gameServer.RegisterHandler((ushort)Protocol.InvitePlayer, Forward);
            gameServer.RegisterHandler((ushort)Protocol.AcceptInvite, Forward);
            gameServer.RegisterHandler((ushort)Protocol.AddFriend, Forward);
            gameServer.RegisterHandler((ushort)Protocol.HandleFriendRequest, Forward);
            gameServer.RegisterHandler((ushort)Protocol.DungeonLootChoice, Forward);

        }

        /// <summary>
        /// 获取或创建该连接的 SessionActor（ActorId 规范：SessionActor_{sessionGuid}）
        /// </summary>
        private string GetOrCreateSessionActor(Guid sessionId)
        {
            var id = $"SessionActor_{sessionId}";
            if (actorSystem.IsActorAlive(id))
            {
                return id;
            }
            return actorSystem.CreateActor(new  SessionActor(id, sessionId, gameServer, actorEventBus));
        }

        /// <summary>
        /// 仅获取（不存在则返回 null，不创建）
        /// </summary>
        private string GetSessionActorIfExists(Guid sessionId)
        {
            var id = $"SessionActor_{sessionId}";
            if (actorSystem.IsActorAlive(id))
            {
                return id;
            }
            return "";
        }

        public async Task StartAsync()
        {
            await gameServer.StartAsync();
        }

        protected override void OnStop()
        {
            base.OnStop();
            actorSystem.StopAllActors();
            Console.WriteLine("ActorGameServer 已停止");
            gameServer.Dispose();
        }

        protected override Task OnReceive(IActorMessage message)
        {
            return Task.CompletedTask;
        }
    }
}
