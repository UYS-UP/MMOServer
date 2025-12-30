using Server.Game.Actor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain
{
    public class SocialActor : ActorBase
    {
        public SocialActor(string actorId) : base(actorId)
        {
        }

        protected override Task OnReceive(IActorMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
