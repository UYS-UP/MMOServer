using Server.Game.Actor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Team
{
    public record class A_CreateTeam(
        string PlayerId, string CharacterId, string CharacterName, int CharacterLevel, string TeamName) : IActorMessage;

    public record class A_StartDungeon(int TeamId, int TemplateId, string PlayerId) : IActorMessage;

    public record class A_QuitTeam(int TeamId, string PlayerId) : IActorMessage;

    public record class A_EnterTeam(int TeamId, string CharacterName, string PlayerId, string CharacterId, int Level) : IActorMessage;

}
