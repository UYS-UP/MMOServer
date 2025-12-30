using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.X509;
using Server.Game.Contracts.Server;
using Server.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill.Buff
{
    public class BuffSystem
    {
        private readonly Dictionary<int, List<BuffInstance>> buffsByEntity = new Dictionary<int, List<BuffInstance>>();
    
        public void Update(float dt)
        {
            foreach(var kvp in buffsByEntity)
            {
                var list = kvp.Value;
                for(int i = list.Count - 1; i >= 0; i--)
                {
                    var buff = list[i];
                    buff.Update(dt);
                    if (buff.IsFinished)
                    {
                        buff.OnRemove();
                        list.RemoveAt(i);
                    }
                }
            }
        }

        public void RemoveBuff(int target, int buffId)
        {
            if (!buffsByEntity.TryGetValue(target, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var buff = list[i];
                if (buff.BuffId == buffId)
                {
                    buff.OnRemove();
                    list.RemoveAt(i);
                }
            }
        }

        public void ApplyBuff(ICombatContext combatContext, EntityRuntime target, BuffConfig config)
        {
            if (!buffsByEntity.TryGetValue(target.EntityId, out var list))
            {
                list = new List<BuffInstance>();
                buffsByEntity[target.EntityId] = list;
            }

            BuffInstance instance = config.Type switch
            {
                BuffType.PeriodicHeal => new PeriodicHealBuff(combatContext, config, target),
                // 其他类型...
                _ => null
            };

            if (instance == null) return;

            list.Add(instance);
            instance.OnApply();
        }
    }

    public class AreaBuffSystem
    {
        private readonly List<AreaBuffInstance> areas = new List<AreaBuffInstance>();
        private readonly ICombatContext combatContext;

        public AreaBuffInstance SpawnAreaBuff(Vector3 center, float radius, float duration,
                                      BuffConfig buffConfig, EntityRuntime caster)
        {
            var instance = new AreaBuffInstance(combatContext, center, radius, duration, buffConfig, caster);
            areas.Add(instance);
            return instance;
        }

        public void Update(float dt)
        {
            for (int i = areas.Count - 1; i >= 0; i--)
            {
                var area = areas[i];
                area.Update(dt);
                if (area.IsFinished)
                {
                    areas.RemoveAt(i);
                }
            }
        }
    }

    public abstract class BuffInstance
    {
        public int BuffId;
        public EntityRuntime Owner { get; }
        public BuffConfig Config { get; }
        public ICombatContext CombatContext { get; }

        protected float elapsed;
        protected float tickTimer;


        public bool IsFinished => elapsed + 0.00001 >= Config.Duration;

        protected BuffInstance(ICombatContext combatContext, BuffConfig cfg, EntityRuntime owner)
        {
            CombatContext = combatContext;
            Config = cfg;
            Owner = owner;
            BuffId = cfg.Id;
        }

        public virtual void OnApply() { OnTick(); }
        public virtual void OnRemove() { }

        public virtual void OnTick() { }

        public void Update(float dt)
        {
            elapsed += dt;
            tickTimer += dt;
            while(Config.TickInterval > 0 &&
                tickTimer >= Config.TickInterval &&
                !IsFinished)
            {
                tickTimer -= Config.TickInterval;
                OnTick();
            }
        }
    }


    public class AreaBuffInstance
    {
        public int Id;                    // 唯一ID（可选）
        public Vector3 Center;            // 圆心（世界坐标）
        public float Radius;              // 半径
        public float Duration;            // 持续时间
        public float Elapsed;

        public BuffConfig BuffConfig;     // 要施加的 Buff
        public EntityRuntime Caster;      // 谁放的技能（可为空）

        // 当前在圈里的实体Id集合（用于判断进入/离开）
        private readonly HashSet<int> insideEntities = new HashSet<int>();

        private readonly ICombatContext combatContext;  // 需要能访问 AOI 和 BuffSystem

        public bool IsFinished => Elapsed >= Duration;

        private float tickTimer;
        private const float CHECK_INTERVAL = 0.2f; // 5 次/秒

        public AreaBuffInstance(ICombatContext combatContext, Vector3 center, float radius, float duration,
                                BuffConfig buffConfig, EntityRuntime caster)
        {
            this.combatContext = combatContext;
            Center = center;
            Radius = radius;
            Duration = duration;
            BuffConfig = buffConfig;
            Caster = caster;
        }

        public void Update(float dt)
        {
            Elapsed += dt;
            tickTimer += dt;

            if (Elapsed >= Duration)
            {
                RemoveBuffFromAll();
                return;
            }

            if (tickTimer >= CHECK_INTERVAL)
            {
                tickTimer -= CHECK_INTERVAL;
                UpdateEntitiesInArea();
            }
        }

        private void UpdateEntitiesInArea()
        {
            var ids = combatContext.QueryCircle(Center, Radius);
            var current = new List<int>();

            foreach(var id in ids)
            {
                if (!combatContext.TryGetEntity(id, out var entity)) continue;
                if(Caster.Identity.Type == EntityType.Monster && entity.Identity.Type == EntityType.Character)
                {
                    current.Add(entity.EntityId);
                    if (insideEntities.Add(id))
                    {
                        combatContext.ApplyBuff(entity, BuffConfig);
                    }
                }

                if(Caster.Identity.Type == EntityType.Character && entity.Identity.Type == EntityType.Monster)
                {
                    current.Add(entity.EntityId);
                    if (insideEntities.Add(id))
                    {
                        combatContext.ApplyBuff(entity, BuffConfig);
                    }
                }
            }


            var toLeave = new List<int>();
            foreach (var id in insideEntities)
            {
                if (!current.Contains(id))
                {
                    toLeave.Add(id);
                }
            }


            foreach (var id in toLeave)
            {
                insideEntities.Remove(id);
                if (!combatContext.TryGetEntity(id, out var entity)) continue;
                combatContext.RemoveBuff(entity.EntityId, BuffConfig.Id);
                
            }


        }

        private void RemoveBuffFromAll()
        {
            foreach (var id in insideEntities)
            {
                if (!combatContext.TryGetEntity(id, out var target))
                {
                    combatContext.RemoveBuff(target.EntityId, BuffConfig.Id);
                }
            }
            insideEntities.Clear();
        }
    }
}
