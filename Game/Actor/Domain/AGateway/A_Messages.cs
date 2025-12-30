using Server.Game.Actor.Core;
using Server.Game.Contracts.Network;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Gateway
{
    
    public class SendToPlayer : IActorMessage
    {
        public string PlayerId { get; }
        public Protocol Protocol { get; }
        public object Payload { get; }
        public SendToPlayer(string playerId, Protocol protocol, object payload)
        {
            PlayerId = playerId;
            Protocol = protocol;
            Payload = payload;
        }
    }

    public class SendToPlayers : IActorMessage
    {
        public IReadOnlyCollection<string> PlayerIds { get; }
        public Protocol Protocol { get; }
        public object Payload { get; }
        public SendToPlayers(IReadOnlyCollection<string> playerIds, Protocol protocol, object payload)
        {
            PlayerIds = playerIds;
            Protocol = protocol;
            Payload = payload;
        }
    }

    public class BatchGatewaySend : IActorMessage
    {
        public List<SendToPlayers> SendToPlayers { get; }
        public List<SendToPlayer> SendToPlayer { get; }

        public BatchGatewaySend(List<SendToPlayers> sendToPlayers, List<SendToPlayer> sendToPlayer)
        {
            SendToPlayers = sendToPlayers;
            SendToPlayer = sendToPlayer;
        }

        public BatchGatewaySend()
        {
            if(SendToPlayers == null) SendToPlayers = new List<SendToPlayers>();
            if(SendToPlayer == null) SendToPlayer = new List<SendToPlayer>();
        }

        public void ClearSend()
        {
            SendToPlayers.Clear();
            SendToPlayer.Clear();
        }

        public void AddSend(IReadOnlyCollection<string> playerIds, Protocol protocol, object payload)
        {
            SendToPlayers.Add(new SendToPlayers(playerIds, protocol, payload));
        }

        public void AddSend(string playerId, Protocol protocol, object payload)
        {
            SendToPlayer.Add(new SendToPlayer(playerId, protocol, payload));
        }

        public BatchGatewaySend DeepCopy()
        {
            return new BatchGatewaySend(new List<SendToPlayers>(SendToPlayers), new List<SendToPlayer>(SendToPlayer));
        }
    }




    public sealed class Broadcast : IActorMessage
    {
        public ushort ProtocolId { get; }
        public Protocol Protocol { get; }
        public object Payload { get; }
        public Broadcast(ushort protocolId, Protocol protocol, object payload)
        {
            ProtocolId = protocolId;
            Protocol = protocol;
            Payload = payload;
        }
    }
}
