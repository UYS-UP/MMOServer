using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Region;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Contracts.Common
{
    public static class GameField
    {
        public const int TICK_INTERVAL_MS = 20;
        
        public static string GetActor<T>()
        {
            return typeof(T).Name;
        }

        public static string GetActor<T>(string suffix)
        {
            return $"{typeof(T).Name}_{suffix}";
        }
    }
}
