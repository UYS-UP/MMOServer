using Google.Protobuf.WellKnownTypes;
using Server.Game.Actor.Domain.Region.AStar;
using Server.Game.Actor.Domain.Region.Services;
using Server.Game.Actor.Domain.Region.Skill;
using Server.Game.Actor.Domain.Region.Skill.Buff;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
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
