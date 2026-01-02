using Google.Protobuf;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.World.AStar;
using Server.Game.World.Services;
using Server.Game.World.Skill;
using Server.Game.World.Skill.Buff;
using System.Numerics;
using System.Threading.Tasks;

namespace Server.Game.World
{
    public class RegionActor : ActorBase
    {
        private int tick;
        private int mapId;
        private RegionWorld regionWorld;

        private ActorEventBus EventBus => System.EventBus;
        private readonly List<int> waitDestoryDungeon;

        private readonly Queue<IActorMessage> messageQueue;


        public RegionActor(string actorId, int mapId) : base(actorId)
        {
            this.mapId = mapId;
            waitDestoryDungeon = new List<int>();

            messageQueue = new Queue<IActorMessage>();

           
        }


        protected override async Task OnStart()
        {
            await base.OnStart();
            EventBus.Subscribe<TickUpdateEvent>(ActorId);
            if(RegionTemplateConfig.TryGetRegionTemplateById(mapId, out var template)){
                var context = new EntityContext
                {
                    Id = mapId,
                    WaitDestory = waitDestoryDungeon,
                };
                var buff = new BuffSystem();
                var areaBuff = new AreaBuffSystem();
                var skill = new SkillSystem();
                var aoi = new AOIService(120, 100, 100);
                var nav = new NavVolumeService(template.NavMeshPath);
                var pathfinder = new AStarPathfind(nav);


                regionWorld = new RegionWorld(this, context, skill, buff, areaBuff, aoi, nav, pathfinder);
            }

        }

        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case TickUpdateEvent tickUpdateEvent:
                    await OnTickUpdateEvent(tickUpdateEvent);
                    break;

                case PlayerDisconnectionEvent playerDisconnectionEvent:
                    await OnPlayerDisconnectionEvent(playerDisconnectionEvent);
                    break;

                default:
                    messageQueue.Enqueue(message);
                    break;
            }
        }

        #region Message Handlers

        private async Task HandleCharacterSpawn(A_CharacterSpawn message)
        {
            await regionWorld.HandleCharacterSpawn(message.Runtime);

        }

        private async Task HandleCharacterDespawn(A_CharacterDespawn message)
        {
            await regionWorld.HandleCharacterDespawn(message.EntityId);
            
        }

        private async Task CS_HandleCharacterMove(CS_CharacterMove message)
        {

            await regionWorld.HandleCharacterMove(
                message.ClientTick,
                message.EntityId,
                message.Position,
                message.Yaw,
                message.Direction);
            
        }


        private Task CS_HandleCharacterCastSkill(CS_CharacterCastSkill message)
        {

            regionWorld.HandleCharacterCastSkill(
                message.ClientTick, 
                message.SkillId, 
                message.EntityId, 
                message.InputType, 
                message.TargetPosition, 
                message.TargetDirection, 
                message.TargetEntityId);
            return Task.CompletedTask;
        }

        #endregion

        #region Tick & Event

        private async Task OnTickUpdateEvent(TickUpdateEvent args)
        {
            tick = args.Tick;
           

            // 处理队列中的所有消息
            while (messageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                    case A_CharacterSpawn spawn: await HandleCharacterSpawn(spawn); break;
                    case A_CharacterDespawn despawn: await HandleCharacterDespawn(despawn); break;
                    case CS_CharacterMove move: await CS_HandleCharacterMove(move); break;
                    case CS_CharacterCastSkill skill: await CS_HandleCharacterCastSkill(skill); break;
   
                }
            }

            await regionWorld.OnTickUpdate(args.Tick, args.DeltaTime);
          
        }

        private Task OnPlayerDisconnectionEvent(PlayerDisconnectionEvent args)
        {
            // TODO: 处理玩家掉线（踢出副本、状态保存等）
            return Task.CompletedTask;
        }

        #endregion
    }
}