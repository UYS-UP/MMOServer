using Server.DataBase.Entities;
using Server.Game.Contracts;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data.Game.CharacterConfig
{
    public class CharacterGrowthConfig
    {
        public ProfessionType Type { get; set; }
        public int Attack { get; set; }
        public int Ability { get; set; }
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int Defence { get; set; }
    }
}
