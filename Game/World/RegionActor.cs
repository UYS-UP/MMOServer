using Google.Protobuf;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Team;
using Server.Game.Contracts.Actor;
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
        private string regionId;
        private RegionWorld regionWorld;

        private readonly ActorEventBus bus;

        private readonly BatchGatewaySend gatewaySend;
        private readonly BatchActorSend actorSend;
        private readonly List<string> waitDestoryDungeon;

        private readonly Queue<IActorMessage> messageQueue;


        public RegionActor(string actorId, string regionId, ActorEventBus bus) : base(actorId)
        {
            this.bus = bus;
            this.regionId = regionId;
            gatewaySend = new BatchGatewaySend();
            actorSend = new BatchActorSend();
            waitDestoryDungeon = new List<string>();

            messageQueue = new Queue<IActorMessage>();

           
        }


        protected override void OnStart()
        {
            base.OnStart();
            bus.Subscribe<TickUpdateEvent>(ActorId);
            if(RegionTemplateConfig.TryGetRegionTemplateById(regionId, out var template)){
                var context = new EntityContext
                {
                    Id = regionId,
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

        private void HandleCharacterSpawn(CharacterSpawn message)
        {
            var kinematics = new KinematicsComponent
            {
                Position = message.Position,
                Yaw = message.Yaw,
                Direction = Vector3.Zero,
                Speed = message.Speed,
                State = EntityState.Idle 
            };

            var combat = new CombatComponent
            {
                Attack = 50,
                Level = message.Level,
                Hp = 1000,
                Maxhp = 1000,
                Mp = 1000,
                MaxMp = 1000,
                Ex = message.Ex,
                MaxEx = message.MaxEx,
            };



            var skillBook = new SkillBookComponent
            {
                Skills = message.Skills
            };

            var identity = new IdentityComponent
            {
                EntityId = message.EntityId,
                Type = message.Type,
                TemplateId = message.TemplateId,
                Name = message.Name,
            };

            var worldRef = new WorldRefComponent
            {
                RegionId = message.RegionId,
                DungeonId = message.DungeonId
            };

            var characterProfile = new CharacterProfileComponent
            {
                Profession = message.Profession,
                PlayerId = message.PlayerId,
                CharacterId = message.CharacterId,
            };


            var entity = new EntityRuntime
            {
                Kinematics = kinematics,
                Combat = combat,
                SkillBook = skillBook,
                Identity = identity,
                WorldRef = worldRef,
                Profile = characterProfile,
            };

            regionWorld.HandleCharacterSpawn(entity);

        }

        private void HandleCharacterDespawn(CharacterDespawn message)
        {
            regionWorld.HandleCharacterDespawn(message.EntityId);
            
        }

        private void HandleCharacterMove(CharacterMove message)
        {

            regionWorld.HandleCharacterMove(
                message.ClientTick,
                message.EntityId,
                message.Position,
                message.Yaw,
                message.Direction);
            
        }


        private void HandleCharacterSkillRelease(CharacterSkillRelease message)
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
                    case CharacterSpawn spawn: HandleCharacterSpawn(spawn); break;
                    case CharacterDespawn despawn: HandleCharacterDespawn(despawn); break;
                    case CharacterMove move: HandleCharacterMove(move); break;
                    case CharacterSkillRelease skill: HandleCharacterSkillRelease(skill); break;
   
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