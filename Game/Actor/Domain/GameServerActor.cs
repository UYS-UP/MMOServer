using MySqlX.XDevAPI;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Actor.Domain.ASession;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Actor.Domain.Team;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.World;
using Server.Network;

namespace Server.Game.Actor.Domain
{
    public class GameServerActor : ActorBase
    {
        private readonly GameServer gameServer;
        private readonly IActorSystem actorSystem;

        public GameServerActor(string actorId, GameServer gameServer, IActorSystem actorSystem) :base(actorId)
        {
            this.gameServer = gameServer; ;
            this.actorSystem = actorSystem;
            RegisterForwardingHandlers();

            gameServer.OnSessionOpened += async session =>
            {
                var id = await GetOrCreateSessionActor(session.Id);
                await TellAsync(id, new ConnectionOpened(session));
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
                var id = await GetOrCreateSessionActor(session.Id);
                await TellAsync(id, new RawPacketReceived(session, packet));
            }
            gameServer.RegisterHandler(Protocol.Heart, Forward);
            gameServer.RegisterHandler(Protocol.CS_Login, Forward);
            gameServer.RegisterHandler(Protocol.CS_Register, Forward);
            gameServer.RegisterHandler(Protocol.CS_CreateCharacter, Forward);
            gameServer.RegisterHandler(Protocol.CS_EnterRegion, Forward);
            gameServer.RegisterHandler(Protocol.CS_EnterGame, Forward);
            gameServer.RegisterHandler(Protocol.CS_EnterDungeon, Forward);
            gameServer.RegisterHandler(Protocol.CS_StartDungeon, Forward);
            gameServer.RegisterHandler(Protocol.CS_LevelDungeon, Forward);
            gameServer.RegisterHandler(Protocol.CS_CharacterMove, Forward);
            gameServer.RegisterHandler(Protocol.CS_CharacterCastSkill, Forward);
            gameServer.RegisterHandler(Protocol.CS_DungeonLootChoice, Forward);
            gameServer.RegisterHandler(Protocol.CS_QueryInventory, Forward);
            gameServer.RegisterHandler(Protocol.CS_SwapStorageSlot, Forward);
            gameServer.RegisterHandler(Protocol.CS_UseItem, Forward);
            gameServer.RegisterHandler(Protocol.CS_CreateTeam, Forward);
            gameServer.RegisterHandler(Protocol.CS_TeamInvite, Forward);
            gameServer.RegisterHandler(Protocol.CS_AcceptInvite, Forward);
            gameServer.RegisterHandler(Protocol.CS_AddFriend, Forward);
            gameServer.RegisterHandler(Protocol.CS_DeleteFriend, Forward);
            gameServer.RegisterHandler(Protocol.CS_FriendRequest, Forward);
            gameServer.RegisterHandler(Protocol.CS_FriendChat, Forward);
            gameServer.RegisterHandler(Protocol.CS_FriendRemark, Forward);
            gameServer.RegisterHandler(Protocol.CS_MoveFriendToGroup, Forward);
            gameServer.RegisterHandler(Protocol.CS_AlterFriendGroup, Forward);
            gameServer.RegisterHandler(Protocol.CS_AddFriendGroup, Forward);
            gameServer.RegisterHandler(Protocol.CS_AcceptQuest, Forward);
            gameServer.RegisterHandler(Protocol.CS_SubmitQuest, Forward);



            gameServer.RegisterHandler(Protocol.GM_AddItem, Forward);
        }

        private async Task<string> GetOrCreateSessionActor(Guid sessionId)
        {
            var id = $"SessionActor_{sessionId}";
            if (actorSystem.IsActorAlive(id))
            {
                return id;
            }
            id = await actorSystem.CreateActor(new SessionActor(id, sessionId, gameServer));
            return id;
        }

        private string GetSessionActorIfExists(Guid sessionId)
        {
            var id = $"SessionActor_{sessionId}";
            if (actorSystem.IsActorAlive(id))
            {
                return id;
            }
            return "";
        }

        protected override async Task OnStart()
        {
            await base.OnStart();
            await gameServer.StartAsync();
            await actorSystem.CreateActor(new TimeActor(GameField.GetActor<TimeActor>()));
            await actorSystem.CreateActor(new AuthActor(GameField.GetActor<AuthActor>()));
            await actorSystem.CreateActor(new TeamActor(GameField.GetActor<TeamActor>()));
            await actorSystem.CreateActor(new RegionActor(GameField.GetActor<RegionActor>(0), 0));
            await actorSystem.CreateActor(new DungeonActor(GameField.GetActor<DungeonActor>()));

        }

        protected override async Task OnStop()
        {
            await base.OnStop();
            await actorSystem.StopAllActors();
            gameServer.Dispose();
        }

        protected override Task OnReceive(IActorMessage message)
        {
            return Task.CompletedTask;
        }
    }
}
