using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using Server.Data;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Gateway;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Team
{
    public class TeamActor : ActorBase
    {
        private int nextTeamId = 0;
        private readonly Dictionary<int, TeamData> teams = new Dictionary<int, TeamData>();
        public TeamActor(string actorId) : base(actorId)
        {
        }

        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case A_CreateTeam createDungeonTeam:
                    await HandleCreateDungeonTeam(createDungeonTeam);
                    break;
                case A_StartDungeon startDungeon:
                    await HandleStartDungeon(startDungeon);
                    break;
                case A_QuitTeam quitTeam:
                    await HandleQuitTeam(quitTeam);
                    break;
                case A_EnterTeam enterTeam:
                    await HandleEnterTeam(enterTeam);
                    break;

            }
        }

        private async Task HandleQuitTeam(A_QuitTeam message)
        {
            if (!teams.TryGetValue(message.TeamId, out var team)) return;
            if (!team.IsLeader(message.PlayerId))
            {
                // 如果退出的是队长，直接解散
                await TellGateway(new SendToPlayers(team.GetMemberPlayerIds(), Protocol.SC_TeamQuited, "队伍已解散"));
                teams.Remove(message.TeamId);
                return;
            }

            await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_TeamQuited, "退出队伍成功"));
            team.Members.Remove(message.PlayerId);

        }

        private async Task HandleCreateDungeonTeam(A_CreateTeam message)
        {
            // 3. 通知客户端
            var leader = new TeamMember
            {
                CharacterId = message.CharacterId,
                Level = message.CharacterLevel,
                PlayerId = message.PlayerId,
                Name = message.CharacterName
            };

            var team = new TeamData(++nextTeamId, message.TeamName, leader, 5);

            teams.Add(team.TeamId, team);
            await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_TeamCreated, new ServerCreateDungeonTeam(true, "创建成功", team)));
        }


        private async Task HandleStartDungeon(A_StartDungeon message)
        {
            if (!teams.TryGetValue(message.TeamId, out var team)) return;
            if (!team.IsLeader(message.PlayerId))
            {
                await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_StartDungeon, "只有队长可以发起进入副本"));
                return;
            }

            var actor = GameField.GetActor<DungeonActor>();
            await TellAsync(actor, new A_CreateDungeon(message.TemplateId, team.GetMemberPlayerIds()));

          
            
        }

        private async Task HandleEnterTeam(A_EnterTeam message)
        {
            if(!teams.TryGetValue(message.TeamId, out var team))
            {
                await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_EnterTeam, 
                    new ServerEnterTeam(false, "队伍不存在", null)));
                return;
            }

            if(team.Members.Count >= team.MaxPlayers)
            {
                await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_EnterTeam,
                    new ServerEnterTeam(false, "队伍人数已满", null)));
                return;
            }

            var member = new TeamMember
            {
                CharacterId = message.CharacterId,
                Level = message.Level,
                PlayerId = message.PlayerId,
                Name = message.CharacterName
            };

            team.Members[member.PlayerId] = member;

            await TellGateway(new SendToPlayer(message.PlayerId, Protocol.SC_EnterTeam,
                new ServerEnterTeam(true, "加入队伍成功", team)));



        }
    }
}
