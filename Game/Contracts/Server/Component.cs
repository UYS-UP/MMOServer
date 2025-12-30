using System.Numerics;

namespace Server.Game.Contracts.Server
{

    #region 实体组件
    public interface IEntityComponent {}

    public class IdentityComponent : IEntityComponent
    {
        public int EntityId;         // 运行时唯一ID (如 Runtime_GUID)
        public EntityType Type;
        public string TemplateId;       // 配置表ID (怪物、NPC、武器等)
        public string Name;
    }

    public class KinematicsComponent : IEntityComponent
    {
        public Vector3 Position;
        public float Yaw;               // 朝向 (Y轴旋转)
        public Vector3 Direction;       // 移动方向向量
        public float Speed;             // 基础移动速度
        public EntityState State;       // 行为状态 (Idle, Move, Attack, Dead)
    }

    public class StatsComponent : IEntityComponent 
    {
        public int Level;
        public float CurrentHp;
        public int CurrentStamina;
        public float CurrentEx;

        // 基础属性和修饰属性应通过属性系统计算，这里只存它们的最终结果或用于计算的中间值
        public Dictionary<AttributeType, float> BaseStats = new Dictionary<AttributeType, float>();
        public Dictionary<AttributeType, float> ModifierStats = new Dictionary<AttributeType, float>();
    }

    public class SkillBookComponent : IEntityComponent
    {
        public Dictionary<int, SkillRuntime> Skills = new Dictionary<int, SkillRuntime>();
        public int MaxSkillSlots = 6;
    }

    public class WorldRefComponent : IEntityComponent
    {
        public int MapId;
        public int DungeonId;
        public Vector3 SpawnPoint;
    }

    public class CharacterProfileComponent : IEntityComponent
    {
        public string PlayerId;
        public string CharacterId;
    }

    public class MonsterComponent : IEntityComponent
    {
        public MonsterRank Rank = MonsterRank.Normal;
        public int DropGroupId = 0;
        public int ExperienceOnKill;
    }

    public enum MonsterRank
    {
        Normal,
        Elite,
        Boss
    }

    public enum AttributeType
    {
        None,
        MaxHp,
        MaxEx,
        MaxStamina,
        Attack,
        Defence,
        CritRate,
        CritDamage,
        MovementSpeed,
        CooldownReduction
    }

    #endregion


    #region 技能组件
    public interface ISkillComponent { }

    public class SkillMetaComponent : ISkillComponent
    {
        public int SkillId;
        public string Name;
        public string Description;
        public int CurrentLevel;
        public int MaxLevel;
        public float GrowthFactor;
        public int RequiredLevel;
        public float Cooldown;
        public float CooldownRemaining;
        public int ManaCost;
        public SkillType Type;
    }


    public class SkillRangeComponent : ISkillComponent
    {
        public float Range;
        public float Angle; // 扇形角度
        public RangeShape Shape; // 范围形状
        public bool RequiresLineOfSight; // 是否需要视线
    }

    // 技能目标选择组件
    public class SkillTargetingComponent : ISkillComponent
    {
        public TargetType TargetType;
        public int MaxTargets;
        public TargetTeam TeamFilter; // 敌我筛选
        public TargetFilter Filter; // 额外筛选条件
    }


    public sealed class SkillPowerC : ISkillComponent
    {
        public int BasePower;
    }

    public enum RangeShape
    {
        Circle,     // 圆形
        Sector,     // 扇形  
        Rectangle,  // 矩形
        Line        // 直线
    }

    public enum DamageType
    {
        Physical,
        Magical,
        Pure
    }

    public enum TargetType
    {
        Single,     // 单体
        Area,       // 区域
        Self,       // 自身
        Chain,      // 连锁
        Projectile  // 投射物
    }

    public enum TargetTeam
    {
        Enemy,
        Ally,
        Both
    }

    [Flags]
    public enum TargetFilter
    {
        None = 0,
        Hero = 1,
        Creep = 2,
        Structure = 4,
        Boss = 8
    }

    #endregion
}
