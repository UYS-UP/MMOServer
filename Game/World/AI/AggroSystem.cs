using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI
{
    public class AggroSystem
    {
        private float maxThreatDist = 50f;
        private float baseThreat = 100;
        private readonly PriorityQueue<string, float> threatQueue = new PriorityQueue<string, float>();

        public void Tick(AIAgent agent, IReadOnlyDictionary<string, EntityRuntime> entities, IReadOnlyCollection<string> visibleEntities)
        {
            threatQueue.Clear();
            if (agent.ReturningHome)
            {
                float distFromHome = Vector3.Distance(agent.Entity.Kinematics.Position, agent.HomePos);
                if (distFromHome > 100f)
                {
                    threatQueue.Clear();
                    agent.Target = null;
                    return;
                }
            }
            foreach (var entityId in visibleEntities)
            {
                if(!entities.TryGetValue(entityId, out var target)) continue;
                // 计算威胁
                var dist = Vector3.Distance(agent.Entity.Kinematics.Position, target.Kinematics.Position);
                float distFactor = Math.Max(0, 1f - dist / maxThreatDist);
                float threat = baseThreat * distFactor;  
                if (threat < 0.1f || dist > agent.PerceptionRange) continue;
                threatQueue.Enqueue(entityId, -threat);
            }

            if (threatQueue.TryDequeue(out var topTarget, out _))
            {
                if (!entities.TryGetValue(topTarget, out var target)) return;
                agent.Target = target;
            }
        }

        public void Tick(AIAgent agent, Dictionary<string, EntityRuntime> visibleEntities)
        {
            threatQueue.Clear();
            if(agent.ReturningHome)
            {
                float distFromHome = Vector3.Distance(agent.Entity.Kinematics.Position, agent.HomePos);
                if (distFromHome > 100f)
                {
                    threatQueue.Clear();
                    agent.Target = null;
                    return;
                }
            }

            foreach (var kv in visibleEntities)
            {
                var target = kv.Value;
                var dist = Vector3.Distance(agent.Entity.Kinematics.Position, target.Kinematics.Position);
                float distFactor = Math.Max(0, 1f - dist / maxThreatDist);
                float threat = baseThreat * distFactor;
                if (threat < 0.1f || dist > agent.PerceptionRange) continue;
                threatQueue.Enqueue(target.EntityId, -threat);
            }

            if (threatQueue.TryDequeue(out var topTarget, out _))
            {
                if (!visibleEntities.TryGetValue(topTarget, out var target)) return;
                agent.Target = target;
            }
        }
    }
}
