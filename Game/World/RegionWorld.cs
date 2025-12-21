using Google.Protobuf.WellKnownTypes;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World.AStar;
using Server.Game.World.Services;
using Server.Game.World.Skill;
using Server.Game.World.Skill.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public class RegionWorld : EntityWorld
    {
        public RegionWorld(EntityContext context, SkillSystem skill, BuffSystem buff, AreaBuffSystem areaBuff, AOIService aoi, NavVolumeService nav, AStarPathfind pathfinder) : base(context, skill, buff, areaBuff, aoi, nav, pathfinder)
        {
        }

    }
}
