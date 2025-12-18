using Server.Game.Actor.Core;
using Server.Game.Contracts.Actor;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Network
{
    public class NetworkGatewayActor : ActorBase
    {
        private readonly GameServer GS;

        public NetworkGatewayActor(string actorId, GameServer gs) : base(actorId) 
        {
            GS = gs;
       
        }

        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case SendToSession sendToSession:
                    await GS.SendToSession(sendToSession.SessionId, sendToSession.Protocol, sendToSession.Payload);
                    break;
                case SendToPlayer sendToPlayer:
                    await GS.SendToPlayer(sendToPlayer.PlayerId, sendToPlayer.Protocol, sendToPlayer.Payload);
                    break;
                case SendToPlayers sendToPlayers:
                    await GS.SendToPlayers(sendToPlayers.PlayerIds, sendToPlayers.Protocol, sendToPlayers.Payload);
                    break;
                case Broadcast broadCast:
                    await GS.Broadcast(broadCast.Protocol, broadCast.Payload);
                    break;
                case BatchGatewaySend batchHandleSend:
                    foreach (var sendToPlayer in batchHandleSend.SendToPlayer)
                    {
                        await GS.SendToPlayer(sendToPlayer.PlayerId, sendToPlayer.Protocol, sendToPlayer.Payload);
                    }
                    foreach(var sendToPlayers in batchHandleSend.SendToPlayers)
                    {
                        await GS.SendToPlayers(sendToPlayers.PlayerIds, sendToPlayers.Protocol, sendToPlayers.Payload);
                    }
                    break;
            }
        }

    }
}
