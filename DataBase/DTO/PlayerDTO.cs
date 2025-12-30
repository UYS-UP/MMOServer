using Server.DataBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.DTO
{
    public struct LoginResultDto
    {
        public bool Succes;
        public string Message;
        public Player Player;
        public List<Character> Characters;
    }

    public struct RegisterResultDto
    {
        public bool Succes;
        public string Message;
        public string Username;
    }
}
