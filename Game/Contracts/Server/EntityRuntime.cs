using Server.Game.Actor.Domain.Region.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Server
{
    public class EntityRuntime
    {
        public KinematicsComponent Kinematics;
        public CombatComponent Combat;
        public SkillBookComponent SkillBook;
        public IdentityComponent Identity;
        public WorldRefComponent WorldRef;
        public CharacterProfileComponent Profile;
        public LayeredStateCoordinator FSM;

        public string EntityId => Identity.EntityId;


        private readonly Dictionary<Type, IEntityComponent> extensions = new Dictionary<Type, IEntityComponent>();

        public T Get<T>() where T : class, IEntityComponent
                    => extensions.TryGetValue(typeof(T), out var c) ? (T)c : null;

        public T GetOrAdd<T>(Func<T> factory) where T : class, IEntityComponent
            => extensions.TryGetValue(typeof(T), out var c) ? (T)c : (T)(extensions[typeof(T)] = factory());

        public bool TryGet<T>(out T component) where T : class, IEntityComponent
        {
            if (extensions.TryGetValue(typeof(T), out var c))
            {
                component = (T)c;
                return true;
            }
            component = null;
            return false;
        }

        public void Add<T>(T component) where T : class, IEntityComponent
            => extensions[typeof(T)] = component;

        public bool Remove<T>() where T : class, IEntityComponent
            => extensions.Remove(typeof(T));

        public bool Has<T>() where T : class, IEntityComponent
            => extensions.ContainsKey(typeof(T));

        public IEnumerable<T> GetAll<T>() where T : class, IEntityComponent
            => extensions.Values.OfType<T>();

        public SkillRuntime GetSkill(int skillId)
        {
            return SkillBook?.Skills.GetValueOrDefault(skillId);
        }

        // 检查是否拥有技能
        public bool HasSkill(int skillId)
        {
            return SkillBook?.Skills.ContainsKey(skillId) ?? false;
        }

        // 获取所有技能
        public IEnumerable<SkillRuntime> GetAllSkills()
        {
            return SkillBook?.Skills.Values ?? Enumerable.Empty<SkillRuntime>();
        }

    }


}
