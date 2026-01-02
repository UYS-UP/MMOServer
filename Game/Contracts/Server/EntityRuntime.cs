using Server.Game.HFSM;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Server
{
    public class EntityRuntime
    {
        public int EntityId => Get<IdentityComponent>().EntityId;

        private IdentityComponent identityCache;
        public IdentityComponent Identity => identityCache ??= Get<IdentityComponent>();

        private KinematicsComponent kinematicsCache;
        public KinematicsComponent Kinematics => kinematicsCache ??= Get<KinematicsComponent>();

        private StatsComponent statsCache; 
        public StatsComponent Stats => statsCache ??= Get<StatsComponent>();

        private SkillBookComponent skillBookCache;
        public SkillBookComponent SkillBook => skillBookCache ??= Get<SkillBookComponent>();

        private WorldRefComponent worldRefCache;
        public WorldRefComponent World => worldRefCache ??= Get<WorldRefComponent>();
        
        public EntityHFSM HFSM { get; set; }


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

    }


}
