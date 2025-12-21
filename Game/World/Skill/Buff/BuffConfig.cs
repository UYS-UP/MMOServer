using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill.Buff
{
    public enum BuffType
    {
        PeriodicHeal,
        PeriodicDamage,
        AttributeModifier,
    }


    public class BuffConfig
    {
        public int Id;
        public string Name;
        public float Duration;
        public float TickInterval;
        public BuffType Type;
        public int AmountPertTick;
        public int MaxStack;
        public bool Dispellable;

    }
}
