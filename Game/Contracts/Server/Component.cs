using System.Numerics;

namespace Server.Game.Contracts.Server
{

    #region 实体组件
    public interface IEntityComponent
    {
    }

    public class KinematicsComponent : IEntityComponent
    {
        public Vector3 Position;
        public float Yaw;
        public Vector3 Direction;
        public float Speed;
        public EntityState State;
    }

    public class CombatComponent : IEntityComponent
    {
        public int Level;
        public int Hp;
        public int Maxhp;
        public int Mp;
        public int MaxMp;
        public int Ex;
        public int MaxEx;
        public int Attack;
        public int Defence;
        public float AttackRange;

        public string KillerEntityId;

        public void ApplyDamage(int damage)
        {
            Hp = Math.Max(0, Hp - damage);
        }
       
    }

    public class SkillBookComponent : IEntityComponent
    {
        public Dictionary<int, SkillRuntime> Skills;
        public int MaxSkillSlots = 6;
    }

    public class IdentityComponent : IEntityComponent
    {
        public string EntityId;
        public EntityType Type;
        public string TemplateId;
        public string Name;
    }

    public class WorldRefComponent : IEntityComponent
    {
        public string RegionId;
        public string DungeonId;
    }

    public class CharacterProfileComponent : IEntityComponent
    {
        public ProfessionType Profession;
        public string PlayerId;
        public string CharacterId;
    }

    public class MonsterComponent : IEntityComponent
    {
        public MonsterRank Rank = MonsterRank.Normal;
        public int DropGroupId = 0;
    }

    public enum MonsterRank
    {
        Normal,
        Elite,
        Boss
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
