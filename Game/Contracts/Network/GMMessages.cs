using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace Server.Game.Contracts.Network
{

    [MessagePackObject]
    public class GMAddItem
    {
        [Key(0)] public string TemplateId;
        [Key(1)] public int Count;
    }
}
