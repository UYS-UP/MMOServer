using Server.Game.Actor.Domain.Region.FSM;
using Server.Game.Actor.Domain.Region.Skill.Buff;
using Server.Game.Contracts.Server;
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

        bool TryGetEntity(string entityId, out EntityRuntime entity);
        IReadOnlySet<string> GetVisibleEntities(string entityId);
        EntityRuntime GetEntity(string entityId);

        IReadOnlyCollection<string> QueryCircle(Vector3 center, float radius);
        IReadOnlyCollection<string> QueryCone(Vector3 center, Vector3 forward, float angle, float radius);
        IReadOnlyCollection<string> QueryRectangle(Vector3 center, Vector3 forward, float width, float length);
        
        void ApplyDamage(EntityRuntime target, int amount, EntityRuntime source = null);
        void ApplyHeal(EntityRuntime target, int amount, EntityRuntime? source = null);

        void ApplyBuff(EntityRuntime target, BuffConfig config, EntityRuntime source = null);
        void RemoveBuff(string entityId, int buffId);

        void InterruptSkill(string casterId);

        void EmitEvent(IWorldEvent worldEvent);
    }
}
