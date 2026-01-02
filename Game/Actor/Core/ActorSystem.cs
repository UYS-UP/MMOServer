using Server.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Core
{
    public class ActorSystem : IActorSystem
    {
        private readonly ConcurrentDictionary<string, ActorBase> actors = new ConcurrentDictionary<string, ActorBase>();
        public  ActorEventBus EventBus { get; }
        public  SessionRouter SessionRouter { get; }

        public ActorSystem()
        {
            this.EventBus = new ActorEventBus(this);
            this.SessionRouter = new SessionRouter();
        }

        public ActorBase GetActor(string actorId)
        {
            actors.TryGetValue(actorId, out var actor);
            return actor;  // 返回 ActorRef
        }

        public async Task<string> CreateActor<T>(T actor) where T : ActorBase
        {
            if (actors.ContainsKey(actor.ActorId))
            {
                throw new InvalidOperationException($"Actor {actor.ActorId} 已存在");
            }

            try
            {
                if (!actors.TryAdd(actor.ActorId, actor))
                {
                    throw new InvalidOperationException($"Actor {actor.ActorId} 添加失败");
                }
                await actor.Initialize(this);
                return actor.ActorId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ActorSystem] 创建 Actor 失败: {actor.ActorId}, 原因: {ex}");
                throw;
            }
        }

        public async Task StopActor(string actorId)
        {
            if (actors.TryRemove(actorId, out var actor))
            {
                await actor.Stop();
                Console.WriteLine($"Actor {actorId} 已停止");
            }
        }

        public async Task StopAllActors()
        {
            foreach (var actor in actors.Values)
            {
                await actor.Stop();
            }
            actors.Clear();
            Console.WriteLine("所有 Actor 已停止");
        }

        public bool IsActorAlive(string actorId)
        {
            return actors.ContainsKey(actorId);
        }
    }
}
