using Server.Game.Actor.Core;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.AAuth
{
    public record class CS_PlayerLogin(Guid SessionId, string Username, string Password) : IActorMessage;

    public record class CS_PlayerRegister(string Username, string Password, string RePassword, Guid SessionId) : IActorMessage;

    public record class CS_CreateCharacter(Guid SessionId, string PlayerId, string CharacterName) : IActorMessage;

    public record class PlayerDisconnectionEvent(string PlayerId) : IActorMessage;
}
