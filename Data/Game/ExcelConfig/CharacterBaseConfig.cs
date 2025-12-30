using Server.DataBase.Entities;
using Server.Game.Contracts;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Data.Game.ExcelConfig
{
    public class CharacterBaseConfig
    {
        public ProfessionType Type { get; set; }
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Ability { get; set; }
        public int Evasion { get; set; }
        public int Critical { get; set; }
        public int Gold { get; set; }
        public int Level { get; set; }
    }


}
