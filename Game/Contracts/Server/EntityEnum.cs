using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Server
{
    public enum EntityType
    {
        Character,
        Monster,
        Npc,
    }


    public enum EntityState
    {
        None = 0,
        Dead = 1 << 0,
        Hit = 1 << 1,
        Invincible = 1 << 2,
        Stealth = 1 << 3,
        Rooted = 1 << 4,
        Silenced = 1 << 5,
        Idle = 1 << 6,
        Move = 1 << 7,
        CastSkill = 1 << 8,
        Attack = 1 << 9,
        Roll = 1 << 10,
    }

    public enum ProfessionType
    {
        Warrior,
        Mage,
    }

    public enum SkillCastInputType
    {
        None,           // 不需要额外选择（自施、直接朝前释放、锁定当前目标这种）
        UnitTarget,     // 选中一个单位（点怪、点队友）
        Direction,      // 选一个方向（通常以自己为原点：扇形、直线冲刺等）
        GroundPosition, // 在地面选一个点（AOE 落地圈）
    }

}
