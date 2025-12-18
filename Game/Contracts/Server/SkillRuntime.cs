using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Server
{

    public enum SkillType
    {
        SingleDamage,
        AreaDamage,
        SingleHeal,
        AreaHeal,
        Buff,
        Debuff
    }

    public class SkillRuntime
    {
        private readonly Dictionary<Type, ISkillComponent> components = new();

        public T AddComponent<T>(T component) where T : class, ISkillComponent
        {
            components[typeof(T)] = component;
            return component;
        }

        public T GetComponent<T>() where T : class, ISkillComponent
        {
            return components.TryGetValue(typeof(T), out var component) ? component as T : null;
        }

        public bool HasComponent<T>() where T : class, ISkillComponent
        {
            return components.ContainsKey(typeof(T));
        }

        public bool RemoveComponent<T>() where T : class, ISkillComponent
        {
            return components.Remove(typeof(T));
        }

        public IEnumerable<ISkillComponent> GetAllComponents()
        {
            return components.Values;
        }

        public IEnumerable<T> GetComponentsByType<T>() where T : class, ISkillComponent
        {
            return components.Values.OfType<T>();
        }

        public bool IsReady()
        {
            var meta = GetComponent<SkillMetaComponent>();
            return meta?.CooldownRemaining <= 0;
        }

        public float GetCooldownPercentage()
        {
            var meta = GetComponent<SkillMetaComponent>();
            if (meta == null || meta.Cooldown <= 0) return 0f;
            return Math.Clamp(meta.CooldownRemaining / meta.Cooldown, 0f, 1f);
        }
        

        // 更新冷却时间
        public void UpdateCooldown(float deltaTime)
        {
            var meta = GetComponent<SkillMetaComponent>();
            if (meta != null && meta.CooldownRemaining > 0)
            {
                meta.CooldownRemaining = Math.Max(0, meta.CooldownRemaining - deltaTime);
            }
        }

    }
}
