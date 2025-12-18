using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Core
{
    public interface IActorSystem
    {
        ActorBase GetActor(string actorId);
        string CreateActor<T>(T actor) where T : ActorBase;
        bool IsActorAlive(string actorId);

        void StopActor(string actorId);

        void StopAllActors();

    }
}
