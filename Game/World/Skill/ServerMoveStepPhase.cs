using Server.Game.Contracts.Server;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill
{
    [JsonTypeAlias(nameof(ServerMoveStepPhase))]
    public class ServerMoveStepPhase : SkillPhase
    {
        public float Distance { get; set; }
        public Vector3 MoveDirection { get; set; }
        public float[] DistanceSamples { get; set; }

        private float elapsed;
        private float duration;
        private float lastRatio;
        private Vector3 cachedWorldDir;


        public override void OnStart(SkillInstance instance)
        {
            if (instance.Caster.Identity.Type != EntityType.Monster) return;
            elapsed = 0f;
            lastRatio = 0f;
            duration = Math.Max(0.001f, EndTime - StartTime);
            Vector3 localDir = Vector3.Normalize(MoveDirection);
            cachedWorldDir = HelperUtility.RotateVector(localDir, instance.Caster.Kinematics.Yaw);
        }

        public override void OnUpdate(SkillInstance instance, float dt)
        {
            if (instance.Caster.Identity.Type != EntityType.Monster) return;

            elapsed += dt;
            float t = Math.Clamp(elapsed / duration, 0f, 1f);

            float currentRatio = Evaluate(DistanceSamples, t);

            float deltaRatio = currentRatio - lastRatio;

            lastRatio = currentRatio;

            if (Math.Abs(deltaRatio) < 1e-5f) return;

            Vector3 displacement = cachedWorldDir * (Distance * deltaRatio);

            instance.Caster.Kinematics.Position += displacement;

            // Console.WriteLine($"[ServerBakedMove] t:{t:F2} ratio:{currentRatio:F2} delta:{displacement}");
        }


        public static float Evaluate(float[] distanceSmaples, float normalizedTime)
        {
            if (distanceSmaples == null || distanceSmaples.Length == 0)
                return 0f;

            if (normalizedTime <= 0f) return distanceSmaples[0];
            if (normalizedTime >= 1f) return distanceSmaples[^1];

            int count = distanceSmaples.Length - 1;
            float floatIndex = normalizedTime * count;

            int index = (int)floatIndex;
            float t = floatIndex - index;

            float v1 = distanceSmaples[index];
            float v2 = distanceSmaples[index + 1];

            return v1 + (v2 - v1) * t;
        }
    }
}
