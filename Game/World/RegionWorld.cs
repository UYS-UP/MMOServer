using Google.Protobuf.WellKnownTypes;
using Server.Game.Actor.Domain;
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
        public RegionWorld(EntityContext context, SkillSystem skill, BuffSystem buff, AreaBuffSystem areaBuff, AOIService aoi, NavVolumeService nav, AStarPathfind pathfinder) : base(context, skill, buff, areaBuff, aoi, nav, pathfinder)
        {
        }

        // 玩家点击进入游戏->AuthActor响应->发包给玩家携带玩家基础信息->玩家收到切场景->场景加载完毕发送进入区域给服务端->服务端响应->触发CharacterSpawn->同步周围的玩家给他

        public override void HandleCharacterSpawn(EntityRuntime entity)
        {
            Context.AddEntity(entity);
            entity.HFSM = new EntityHFSM(entity, Combat);
            AOI.Add(entity.Identity.EntityId, entity.Kinematics.Position);

            //Context.Actor.AddTell(GameField.GetActor<CharacterActor>(entity.Profile.PlayerId), new CharacterEntitySnapshot(
            //    entity.Profile.PlayerId, entity.Profile.CharacterId,
            //    entity.Identity.EntityId, entity.Identity.Name, entity.Identity.Type,
            //    entity.Combat.Level, entity.Profile.Profession,
            //    entity.WorldRef.RegionId, entity.WorldRef.DungeonId));
            //Context.Actor.AddTell(GameField.GetActor<ChatActor>(),
            //    new CharacterEnterRegion(Context.Id, entity.Profile.PlayerId));
            var spawnEntity = Context.GetNetworkEntityByEntityId(entity.Identity.EntityId);
            var (enterWatchers, _) = AOI.Update(entity.Identity.EntityId, entity.Kinematics.Position);
            enterWatchers.Add(entity.EntityId);
            var players = Context.GetPlayerIdsByEntityIds(enterWatchers);
            if (players.Count == 0) return;
            var entitySpawnPayload = new ServerEntitySpawn(Context.Tick, spawnEntity);
            Context.Gateway.AddSend(
                players,
                Protocol.SC_EntitySpawn,
                entitySpawnPayload);
            
        }

    }
}
