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

    public class BindAccount : IActorMessage
    {
        public Guid SessionId { get; }
        public string PlayerId { get; }
        public BindAccount(Guid sessionId, string accountId)
        {
            SessionId = sessionId;
            PlayerId = accountId;
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
