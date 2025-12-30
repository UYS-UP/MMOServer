using Server.DataBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DataBase.DTO
{
    public struct CreateCharacterDto
    {
        public bool Sucess;
        public string Message;
        public Character character;
    }

    public struct DeleteCharacterDto
    {
        public bool Sucess;
        public string Message;
    }
}
