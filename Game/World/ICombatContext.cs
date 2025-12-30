using Server.Game.Contracts.Server;
using Server.Game.World.Skill;
using Server.Game.World.Skill.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public interface ICombatContext
    {
        int Tick {  get; }

        bool TryGetEntity(int entityId, out EntityRuntime entity);
        IReadOnlySet<int> GetVisibleEntities(int entityId);
        EntityRuntime GetEntity(int entityId);

        IReadOnlyCollection<int> QueryCircle(Vector3 center, float radius);
        IReadOnlyCollection<int> QueryCone(Vector3 center, Vector3 forward, float angle, float radius);
        IReadOnlyCollection<int> QueryRectangle(Vector3 center, Vector3 forward, float width, float length);
        
      

        void ApplyBuff(EntityRuntime target, BuffConfig config, EntityRuntime source = null);
        void RemoveBuff(int entityId, int buffId);

        bool TryCastSkill(EntityRuntime caster, SkillCastData data, out string resone);

        void InterruptSkill(int casterId);
        bool IsSkillRunning(int casterId);
        bool IsSkillCooldown(int casterId, int skillId);

        void EmitEvent(IWorldEvent worldEvent);
    }
}
