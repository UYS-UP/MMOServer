using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Utility
{
    public static class Counter
    {
        private static int id = 0;

        public static int NextId()
        {
            return Interlocked.Increment(ref id);
        }

        public static int CurrentId => Volatile.Read(ref id);

        public static void Reset(int startValue = 0)
        {
            Interlocked.Exchange(ref id, startValue);
        }
    }
}
