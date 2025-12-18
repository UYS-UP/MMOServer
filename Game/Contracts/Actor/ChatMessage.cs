using Server.Game.Actor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Actor
{

    public record class SendChatMessage(
        string PlayerId,  string CharacterName, ChatType Type, 
        string Content, string TargetId) : IActorMessage;

    public record class SystemNotification(string Content, IReadOnlyList<string> TargetPlayers) : IActorMessage;

    public record class CharacterEnterRegion(string RegionId, string PlayerId) : IActorMessage;

    public record class CharacterLevelRegion(string NewRegionId, string OldRegionId, string PlayerId) : IActorMessage;

    public record class CharacterEnterTeam(int TeamId, string PlayerId) : IActorMessage;

    public record class CharacterLevelTeam(int TeamId, string PlayerId) : IActorMessage;
}
