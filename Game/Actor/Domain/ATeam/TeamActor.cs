using Google.Protobuf;
using MessagePack;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Ocsp;
using Server.Data;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.ACharacter;
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
                case CS_CreateTeam createDungeonTeam:
                    await HandleCreateDungeonTeam(createDungeonTeam);
                    break;
                case CS_StartDungeon startDungeon:
                    await HandleStartDungeon(startDungeon);
                    break;
                case CS_QuitTeam quitTeam:
                    await HandleQuitTeam(quitTeam);
                    break;
                case CS_EnterTeam enterTeam:
                    await HandleEnterTeam(enterTeam);
                    break;

            }
        }

        private async Task HandleQuitTeam(CS_QuitTeam message)
        {
            if (!teams.TryGetValue(message.TeamId, out var team)) return;
            if (!team.IsLeader(message.CharacterId))
            {
                // 如果退出的是队长，直接解散
                var bytes = MessagePackSerializer.Serialize("队伍已经解散");
                foreach(var member in team.Members)
                {
                    await TellGateway(member.Key, Protocol.SC_TeamQuited, bytes);
                }
               
                teams.Remove(message.TeamId);
                return;
            }

            await TellGateway(message.CharacterId, 
                Protocol.SC_TeamQuited, 
                MessagePackSerializer.Serialize("退出队伍成功"));
            team.Members.Remove(message.CharacterId);

        }

        private async Task HandleCreateDungeonTeam(CS_CreateTeam message)
        {
            // 3. 通知客户端
            var leader = new TeamMember
            {
                CharacterId = message.CharacterId,
                Level = message.CharacterLevel,
                Name = message.CharacterName
            };

            var team = new TeamData(++nextTeamId, leader, 5);

            teams.Add(team.TeamId, team);

            var bytes = MessagePackSerializer.Serialize(new ServerCreateDungeonTeam(true, "创建成功", team));
            await TellGateway(message.CharacterId, Protocol.SC_TeamCreated, bytes);
        }


        private async Task HandleStartDungeon(CS_StartDungeon message)
        {
            if (!teams.TryGetValue(message.TeamId, out var team)) return;
            if (!team.IsLeader(message.CharacterId))
            {
                var bytes = MessagePackSerializer.Serialize("只有队长可以发起进入副本");
                await TellGateway(message.CharacterId, Protocol.SC_StartDungeon, bytes);
                return;
            }

            var actor = GameField.GetActor<DungeonActor>();
            await TellAsync(actor, new A_CreateDungeon(message.TemplateId, team.GetMemberPlayerIds()));

          
            
        }

        private async Task HandleEnterTeam(CS_EnterTeam message)
        {
            byte[] bytes;
            if(!teams.TryGetValue(message.TeamId, out var team))
            {
                bytes = MessagePackSerializer.Serialize(new ServerEnterTeam(false, "队伍不存在", null));
                await TellGateway(message.CharacterId, Protocol.SC_EnterTeam, bytes);
                return;
            }

            if(team.Members.Count >= team.MaxPlayers)
            {
                bytes = MessagePackSerializer.Serialize(new ServerEnterTeam(false, "队伍人数已满", null));
                await TellGateway(message.CharacterId, Protocol.SC_EnterTeam, bytes);
                return;
            }

            var member = new TeamMember
            {
                CharacterId = message.CharacterId,
                Level = message.Level,
                Name = message.CharacterName
            };

            team.Members[member.CharacterId] = member;
            bytes = MessagePackSerializer.Serialize(new ServerEnterTeam(true, "加入队伍成功", team));
            await TellGateway(message.CharacterId, Protocol.SC_EnterTeam, bytes);



        }
    }
}
