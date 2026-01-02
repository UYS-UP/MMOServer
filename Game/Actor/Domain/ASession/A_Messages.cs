using Server.Game.Actor.Core;
using Server.Game.Contracts.Network;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ASession
{
    public class RawPacketReceived : IActorMessage
    {
        public ISession Session;
        public GamePacket Packet;
        public RawPacketReceived(ISession session, GamePacket packet)
        {
            Session = session;
            Packet = packet;
        }
    }

    public class ConnectionOpened : IActorMessage
    {
        public ISession Session { get; }
        public ConnectionOpened(ISession session)
        {
            Session = session;
        }
    }

    public class ConnectionClosed : IActorMessage
    {
        public Guid SessionId { get; }
        public string Reason { get; }
        public ConnectionClosed(Guid sessionId, string reason)
        {
            SessionId = sessionId;
            Reason = reason;
        }
    }

    public class SendTo : IActorMessage
    {
        public byte[] Bytes { get; }
        public Protocol Protocol { get; }

        public SendTo(Protocol protocol, byte[] bytes)
        {
            Bytes = bytes;
            Protocol = protocol;
        }
    }

    public class BindPlayerId : IActorMessage
    {
        public string PlayerId { get; }

        public BindPlayerId(string playerId)
        {
            PlayerId = playerId;
        }
    }

    public class BindCharacterIdAndEntityId : IActorMessage
    {
        public string CharacterId { get; }
        public int EntityId { get; }

        public BindCharacterIdAndEntityId(string characterId, int entityId)
        {
            CharacterId = characterId;
            EntityId = entityId;
        }
    }

    public class BindEntityId : IActorMessage
    {
        public int EntityId { get; }

        public BindEntityId(int entityId)
        {
            EntityId = entityId;
        }
    }

    public class CharacterWorldSync : IActorMessage
    {
        public int MapId { get; }
        public int DungeonId { get; }

        public CharacterWorldSync(int regionId, int dungeonId)
        {
            MapId = regionId;
            DungeonId = dungeonId;
        }
    }


    public class SendToSession : IActorMessage
    {
        public Guid SessionId { get; }
        public Protocol Protocol { get; }
        public object Payload { get; }
        public SendToSession(Guid sessionId, Protocol protocol, object payload)
        {
            SessionId = sessionId;
            Protocol = protocol;
            Payload = payload;
        }
    }

}
