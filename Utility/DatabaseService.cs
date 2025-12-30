using Server.DataBase.Service;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Utility
{
    public static class DatabaseService
    {
        public static PlayerService PlayerService { get; private set; }
        public static CharacterService CharacterService { get; private set; }

        public static void Initalize(PlayerService playerService, CharacterService characterService)
        {
            PlayerService = playerService;
            CharacterService = characterService;

        }

    }
}
