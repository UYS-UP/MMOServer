using Google.Protobuf.WellKnownTypes;
using Server.DataBase.Entities;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public interface IWorldEvent
    {
        
    }

    public class ExecuteSkillWorldEvent : IWorldEvent
    {
        public int SkillId;
        public EntityRuntime Caster;
    }


    public class DamageWorldEvent : IWorldEvent
    {
        public List<EntityDeath> Deaths;
        public List<EntityWound> Wounds;
        public int Source;
    }
}
