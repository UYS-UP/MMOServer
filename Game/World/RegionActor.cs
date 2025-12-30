using Google.Protobuf;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Actor.Domain.Gateway;
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

        private readonly ActorEventBus bus;

        private readonly BatchGatewaySend gatewaySend;
        private readonly BatchActorSend actorSend;
        private readonly List<int> waitDestoryDungeon;

        private readonly Queue<IActorMessage> messageQueue;


        public RegionActor(string actorId, int mapId, ActorEventBus bus) : base(actorId)
        {
            this.bus = bus;
            this.mapId = mapId;
            gatewaySend = new BatchGatewaySend();
            actorSend = new BatchActorSend();
            waitDestoryDungeon = new List<int>();

            messageQueue = new Queue<IActorMessage>();

           
        }


        protected override async Task OnStart()
        {
            await base.OnStart();
            bus.Subscribe<TickUpdateEvent>(ActorId);
            if(RegionTemplateConfig.TryGetRegionTemplateById(mapId, out var template)){
                var context = new EntityContext
                {
                    Id = mapId,
                    Actor = actorSend,
                    Gateway = gatewaySend,
                    WaitDestory = waitDestoryDungeon,
                };
                var buff = new BuffSystem();
                var areaBuff = new AreaBuffSystem();
                var skill = new SkillSystem();
                var aoi = new AOIService(120, 100, 100);
                var nav = new NavVolumeService(template.NavMeshPath);
                var pathfinder = new AStarPathfind(nav);


                regionWorld = new RegionWorld(context, skill, buff, areaBuff, aoi, nav, pathfinder);
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

        private void HandleCharacterSpawn(A_CharacterSpawn message)
        {
            regionWorld.HandleCharacterSpawn(message.Runtime);

        }

        private void HandleCharacterDespawn(A_CharacterDespawn message)
        {
            regionWorld.HandleCharacterDespawn(message.EntityId);
            
        }

        private void HandleCharacterMove(A_CharacterMove message)
        {

            regionWorld.HandleCharacterMove(
                message.ClientTick,
                message.EntityId,
                message.Position,
                message.Yaw,
                message.Direction);
            
        }


        private void HandleCharacterSkillRelease(A_CharacterCastSkill message)
        {

            regionWorld.HandleCharacterCastSkill(message.ClientTick, message.SkillId, message.EntityId, message.InputType, message.TargetPosition, message.TargetDirection, message.TargetEntityId);
        }

        #endregion

        #region Tick & Event

        private async Task OnTickUpdateEvent(TickUpdateEvent args)
        {
            tick = args.Tick;
            gatewaySend.ClearSend();
            actorSend.ClearSend();
           

            // 处理队列中的所有消息
            while (messageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                    case A_CharacterSpawn spawn: HandleCharacterSpawn(spawn); break;
                    case A_CharacterDespawn despawn: HandleCharacterDespawn(despawn); break;
                    case A_CharacterMove move: HandleCharacterMove(move); break;
                    case A_CharacterCastSkill skill: HandleCharacterSkillRelease(skill); break;
   
                }
            }

            regionWorld.OnTickUpdate(args.Tick, args.DeltaTime);


            // 统一发送网关消息
            await TellGateway(gatewaySend.DeepCopy());

            // 统一发送Actor消息
            foreach (var (targetActorId, msg) in actorSend.Commnads)
            {
                await TellAsync(targetActorId, msg);
            }
          
        }

        private Task OnPlayerDisconnectionEvent(PlayerDisconnectionEvent args)
        {
            // TODO: 处理玩家掉线（踢出副本、状态保存等）
            return Task.CompletedTask;
        }

        #endregion
    }
}