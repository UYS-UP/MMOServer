using Server.Game.Contracts.Server;
using Server.Game.World.Skill;
using Server.Game.World.Skill.Buff;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public class WorldCombatContext : ICombatContext
    {
        private readonly EntityWorld world;

        public WorldCombatContext(EntityWorld world)
        {
            this.world = world;
        }


        public int Tick => world.Context.Tick;

        public EntityRuntime GetEntity(string entityId)
        {
            throw new NotImplementedException();
        }


        public bool TryGetEntity(string entityId, out EntityRuntime entity)
        {
            return world.Context.Entities.TryGetValue(entityId, out entity);
        }

        public IReadOnlySet<string> GetVisibleEntities(string entityId)
        {
            return world.AOI.GetVisibleSet(entityId);
        }


        public IReadOnlyCollection<string> QueryCircle(Vector3 center, float radius)
        {
            return world.AOI.QueryCircle(center, radius);
        }

        public IReadOnlyCollection<string> QueryCone(Vector3 center, Vector3 forward, float angle, float radius)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<string> QueryRectangle(Vector3 center, Vector3 forward, float width, float length)
        {
            throw new NotImplementedException();
        }

        public void ApplyBuff(EntityRuntime target, BuffConfig config, EntityRuntime source = null)
        {
            world.Buff.ApplyBuff(world.Combat, target, config);
        }

        public void ApplyDamage(EntityRuntime target, int amount, EntityRuntime source = null)
        {
      
        }

        public void ApplyHeal(EntityRuntime target, int amount, EntityRuntime? source = null)
        {
   
        }

        #region 技能系统

        public bool TryCastSkill(EntityRuntime caster, SkillCastData data, out string reason)
        {
            return world.Skill.TryCastSkill(this, caster, data, out reason);
        }

        public void InterruptSkill(string casterId)
        {
            world.Skill.ForceInterrupt(casterId);
        }


        public bool IsSkillRunning(string casterId)
        {
            return world.Skill.IsCasting(casterId);
        }

        #endregion

        public void RemoveBuff(string entityId, int buffId)
        {
           world.Buff.RemoveBuff(entityId, buffId);
        }

        public void EmitEvent(IWorldEvent worldEvent)
        {
            world.EmitEvent(worldEvent);
        }

        public bool IsSkillCooldown(string casterId, int skillId)
        {
            return world.Skill.IsSkillCooldown(this, casterId, skillId);
        }
    }
}
