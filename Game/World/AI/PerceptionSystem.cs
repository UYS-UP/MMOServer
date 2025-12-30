using Google.Protobuf.WellKnownTypes;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI
{
    public class PerceptionSystem
    {
        public HashSet<int> Tick(AIAgent agent, IReadOnlyDictionary<int, EntityRuntime> entities, IReadOnlyCollection<int> visibleEntities)
        {
            var result = new HashSet<int>(capacity: visibleEntities.Count);

            if (agent.AiFsm.Ctx.ReturningHome)
            {
                float distFromHome = Vector3.Distance(agent.Entity.Kinematics.Position, agent.HomePos);
                if (distFromHome > 100f)
                {
                    return result; 
                }
            }

            var forward = HelperUtility.YawToForward(agent.Entity.Kinematics.Yaw);
            foreach (var entityId in visibleEntities)
            {

                if (!entities.TryGetValue(entityId, out var target)) continue;
                if(target.Identity.Type != EntityType.Character) continue;
                var delta = target.Kinematics.Position - agent.Entity.Kinematics.Position;
                float distSquared = delta.X * delta.X + delta.Z * delta.Z;
                if (distSquared > agent.PerceptionRange * agent.PerceptionRange) continue;

                var direction = Vector3.Normalize(delta);
                float dot = Vector3.Dot(forward, direction);

                float angleCos = MathF.Cos(agent.FovAngle * MathF.PI / 180f / 2f);
                if (dot < angleCos) continue;

                result.Add(entityId);
            }

            return result;
        }

        public Dictionary<int, EntityRuntime> Tick(AIAgent agent, Dictionary<int, EntityRuntime> visibleEntities)
        {
            var result = new Dictionary<int, EntityRuntime>(capacity: visibleEntities.Count);

            if(agent.AiFsm.Ctx.ReturningHome)
            {
                float distFromHome = Vector3.Distance(agent.Entity.Kinematics.Position, agent.HomePos);
                if (distFromHome > 100f)
                {
                    return result;
                }
            }


            var forward = HelperUtility.YawToForward(agent.Entity.Kinematics.Yaw);
            foreach (var kv in visibleEntities)
            {
                var target = kv.Value;
                if (target.Identity.Type != EntityType.Character) continue;
                var delta = target.Kinematics.Position - agent.Entity.Kinematics.Position;
                float distSquared = delta.X * delta.X + delta.Z * delta.Z;
                if (distSquared > agent.PerceptionRange * agent.PerceptionRange) continue;

                var direction = Vector3.Normalize(delta);
                float dot = Vector3.Dot(forward, direction);

                float angleCos = MathF.Cos(agent.FovAngle * MathF.PI / 180f / 2f);
                if (dot < angleCos) continue;

                result.Add(target.EntityId, target);
            }

            return result;
        }

    }
}
