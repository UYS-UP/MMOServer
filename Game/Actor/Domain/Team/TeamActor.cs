using Org.BouncyCastle.Asn1.Ocsp;
using Server.Data;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Chat;
using Server.Game.Actor.Domain.Player;
using Server.Game.Actor.Network;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
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
        private readonly Dictionary<int, TeamBaseData> teams = new Dictionary<int, TeamBaseData>();
        public TeamActor(string actorId) : base(actorId)
        {
        }

        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case CreateDungeonTeam createDungeonTeam:
                    await HandleCreateDungeonTeam(createDungeonTeam);
                    break;
                case CreateDungeonInstance createDungeonInstance:
                    await HandleCreateDungeonInstance(createDungeonInstance);
                    break;
                case StartDungeon startDungeon:
                    await HandleStartDungeon(startDungeon);
                    break;
                case LoadedDungeon loadedDungeon:
                    await HandleLoadedDungeon(loadedDungeon);
                    break;
                case EnterTeam enterTeam:
                    await HandleEnterTeam(enterTeam);
                    break;

            }
        }

        private async Task HandleLoadedDungeon(LoadedDungeon message)
        {
            if (!teams.TryGetValue(message.TeamId, out var team)) return;
            if (team is DungeonTeamData dungeonTeam)
            {
                dungeonTeam.LoadedPlayers.Add(message.PlayerId);
                if(dungeonTeam.LoadedPlayers.Count == dungeonTeam.TeamMembers.Count)
                {
                    foreach(var member in dungeonTeam.TeamMembers)
                    {
                        await TellAsync($"PlayerActor_{member.PlayerId}", new EnterDungeon(dungeonTeam.DungeonTemplateId));
                    }
                }
            }
        }

        private async Task HandleCreateDungeonTeam(CreateDungeonTeam message)
        {
            // 1. 从配置文件读取副本信息
            if (!RegionTemplateConfig.TryGetDungeonTemplateById(message.TemplateId, out var template)) return;
            
            // 2. 判断等级
            if(template.MinLevel > message.CharacterLevel)
            {
                await TellGateway(new SendToPlayer(message.PlayerId, Protocol.CreateDungeonTeam, new ServerCreateDungeonTeam(false, "等级不符合副本要求", null)));
                return;
            }


            // 3. 通知客户端
            var leader = new TeamMember
            {
                CharacterId = message.CharacterId,
                Level = message.CharacterLevel,
                PlayerId = message.PlayerId,
                Name = message.CharacterName
            };

            var team = new DungeonTeamData
            {
                Leader = leader,
                TeamMembers = new List<TeamMember> { leader },
                TeamName = message.TeamName,
                TeamId = nextTeamId,
                MaxPlayers = template.MaxPlayers,
                MinPlayers = template.MinPlayers,
                DungeonTemplateId = message.TemplateId,
                LoadedPlayers = new HashSet<string>()
            };

            teams.Add(team.TeamId, team);


            await TellAsync($"PlayerActor_{message.PlayerId}", new TeamSnpot(team.TeamId, team.TeamName, team.TeamType, team.GetTeamPlayers()));
            await TellAsync(nameof(ChatActor), new CharacterEnterTeam(team.TeamId, message.PlayerId));
            await TellGateway(new SendToPlayer(message.PlayerId, Protocol.CreateDungeonTeam, new ServerCreateDungeonTeam(true, "创建成功", team)));
        }

        private async Task HandleStartDungeon(StartDungeon startDungeon)
        {
            if (!teams.TryGetValue(startDungeon.TeamId, out var team)) return;
            if(team is DungeonTeamData dungeonTeam)
            {
                if(!dungeonTeam.CheckMemberCount()) return;
                var dungeonId = HelperUtility.GetKey();
                if (!RegionTemplateConfig.TryGetDungeonTemplateById(dungeonTeam.DungeonTemplateId, out var dungeonTemplate)) return;
                await TellAsync($"DungeonActor", 
                    new CreateDungeonInstance(dungeonId,
					dungeonTemplate.Id, dungeonTeam.TeamId, 
                    dungeonTemplate.EntryPosition));
            }
            
        }

        private async Task HandleCreateDungeonInstance(CreateDungeonInstance message)
        {
            if (!teams.TryGetValue(message.TeamId, out var team)) return;
            foreach(var member in team.TeamMembers)
            {
                await TellAsync($"PlayerActor_{member.PlayerId}", 
                    new LoadDungeon(message.DungeonId, message.TemplateId));
            }
            

           
        }

        private async Task HandleEnterTeam(EnterTeam message)
        {
            if(!teams.TryGetValue(message.TeamId, out var team))
            {
                await TellGateway(new SendToPlayer(message.PlayerId, Protocol.EnterTeam, 
                    new ServerPlayerEnterTeam(false, "队伍已不存在", null, string.Empty)));
                return;
            }

            if(!team.TryAddMemeber(message.CharacterName, message.PlayerId, message.CharacterId, message.Level, out var member))
            {
                await TellGateway(new SendToPlayer(message.PlayerId, Protocol.EnterTeam,
                    new ServerPlayerEnterTeam(false, "人数已满", null, string.Empty)));
                return;
            }

            await TellAsync($"PlayerActor_{message.PlayerId}", new TeamSnpot(team.TeamId, team.TeamName, team.TeamType, team.GetTeamPlayers()));
            await TellAsync(nameof(ChatActor), new CharacterEnterTeam(team.TeamId, message.PlayerId));
            await TellGateway(new SendToPlayers(team.GetTeamPlayers(), Protocol.EnterTeam,
                new ServerPlayerEnterTeam(true, "加入成功", team, message.PlayerId)));



        }
    }
}
