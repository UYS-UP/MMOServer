using Server.Game.Actor.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.ACharacter
{
    public record class GM_AddItem(string ItemTemplateId, int Count): IActorMessage;
}
