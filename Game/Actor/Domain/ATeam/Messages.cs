using Server.Game.Actor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Team
{
    public record class CS_CreateTeam(string CharacterId, string CharacterName, int CharacterLevel) : IActorMessage;

    public record class CS_StartDungeon(int TeamId, int TemplateId, string CharacterId) : IActorMessage;

    public record class CS_QuitTeam(int TeamId, string CharacterId) : IActorMessage;

    public record class CS_EnterTeam(int TeamId, string CharacterName, string CharacterId, int Level) : IActorMessage;

}
