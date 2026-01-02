using Google.Protobuf.WellKnownTypes;
using MessagePack;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain;
using Server.Game.Actor.Domain.ASession;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.HFSM;
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

        public RegionWorld(
            ActorBase actor, 
            EntityContext context, 
            SkillSystem skill, 
            BuffSystem buff, 
            AreaBuffSystem areaBuff, 
            AOIService aoi, 
            NavVolumeService nav, 
            AStarPathfind pathfinder) : 
            base(actor, context, skill, buff, areaBuff, aoi, nav, pathfinder)
        {
            
        }


        public override async Task HandleCharacterSpawn(EntityRuntime entity)
        {
            Context.AddEntity(entity);
            entity.HFSM = new EntityHFSM(entity, Combat);
            AOI.Add(entity.Identity.EntityId, entity.Kinematics.Position);
            var spawnEntity = Context.GetNetworkEntityByEntityId(entity.Identity.EntityId);
            var (enterWatchers, _) = AOI.Update(entity.Identity.EntityId, entity.Kinematics.Position);
            enterWatchers.Add(entity.EntityId);
            var characterIds = Context.GetCharacterIdsByEntityIds(enterWatchers);
            if (characterIds.Count == 0) return;
            var payload = new ServerEntitySpawn(Context.Tick, spawnEntity);
            var bytes = MessagePackSerializer.Serialize(payload);
            foreach ( var characterId in characterIds)
            {
                await Actor.TellGateway(
                    characterId,
                    Protocol.SC_EntitySpawn,
                    bytes
                );
            }


            
        }

    }
}
