using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.Team;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Server;
using Server.Game.World.AStar;
using Server.Game.World.Services;
using Server.Game.World.Skill;
using Server.Game.World.Skill.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World
{
    /// <summary>
    /// 这个Actor管理所有的副本
    /// </summary>
    public class DungeonActor : ActorBase
    {
        private int tick;
        private readonly Dictionary<string, DungeonWorld> dungons;

        private readonly ActorEventBus bus;

        private readonly BatchGatewaySend gatewaySend;
        private readonly BatchActorSend actorSend;
        private readonly List<string> waitDestoryDungeon;

        private readonly Queue<IActorMessage> messageQueue;


        public DungeonActor(string actorId, ActorEventBus bus) : base(actorId)
        {
            this.bus = bus;
            dungons = new Dictionary<string, DungeonWorld>();
            gatewaySend = new BatchGatewaySend();
            actorSend = new BatchActorSend();
            waitDestoryDungeon = new List<string>();

            messageQueue = new Queue<IActorMessage>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            bus.Subscribe<TickUpdateEvent>(ActorId);
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




        private async Task OnTickUpdateEvent(TickUpdateEvent args)
        {
            tick = args.Tick;
            gatewaySend.ClearSend();
            actorSend.ClearSend();
            waitDestoryDungeon.Clear();

            // 处理队列中的所有消息
            while (messageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                        case CreateDungeonInstance create: await HandleCreateDungeonInstance(create); break;

                        case CharacterSpawn spawn: await HandleCharacterSpawn(spawn); break;
                        case CharacterDespawn despawn: await HandleCharacterDespawn(despawn); break;
                        case CharacterMove move: await HandleCharacterMove(move); break;

                        case CharacterSkillRelease skill: await HandleCharacterCastSkill(skill); break;
                        case DungeonDesotryTimer destroy: dungons.Remove(destroy.DungeonId); break;
                        case DungeonLootChoice loot: await HandleDungeonLootChoice(loot); break;
                }
            }

            foreach (var kv in dungons)
            {
                kv.Value.OnTickUpdate(args.Tick, args.DeltaTime);
            }
              
            foreach (var dungeonId in waitDestoryDungeon)
            {
                dungons.Remove(dungeonId);
            }



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


        private Task HandleCreateDungeonInstance(CreateDungeonInstance message)
        {
            if (!RegionTemplateConfig.TryGetDungeonTemplateById(message.TemplateId, out var dungeonTemplate)) return Task.CompletedTask;
            var context = new EntityContext 
            { 
                Id = message.DungeonId,
                Actor = actorSend,
                Gateway = gatewaySend,
                WaitDestory = waitDestoryDungeon,
            };
            var buff = new BuffSystem();
            var areaBuff = new AreaBuffSystem();
            var skill = new SkillSystem();
            var nav = new NavVolumeService(dungeonTemplate.NavMeshPath);
            var (min, max) = nav.GetMapBoundsXZ();
            var aoi = new AOIService(Vector2.Distance(min, max), 100, 100);

            var pathfinder = new AStarPathfind(nav);
            var dungeonInstance = new DungeonWorld(context, skill, buff, areaBuff, aoi, nav, pathfinder);
            dungons[message.DungeonId] = dungeonInstance;
            actorSend.AddTell(nameof(TeamActor), message);
            return Task.CompletedTask;
        }

        private Task HandleCharacterSpawn(CharacterSpawn message)
        {
            if (!dungons.TryGetValue(message.DungeonId, out var dungeon)) return Task.CompletedTask;
            var kinematics = new KinematicsComponent
            {
                Position = message.Position,
                Yaw = message.Yaw,
                Direction = Vector3.Zero,
                Speed = message.Speed,
                State = EntityState.Idle,
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
            dungeon.HandleCharacterSpawn(entity);

            return Task.CompletedTask;
        }

        private Task HandleCharacterDespawn(CharacterDespawn message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
                dungeon.HandleCharacterDespawn(message.EntityId);
            return Task.CompletedTask;
        }

        private Task HandleCharacterMove(CharacterMove message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                dungeon.HandleCharacterMove(
                    message.ClientTick,
                    message.EntityId,
                    message.Position,
                    message.Yaw,
                    message.Direction);
            }

            return Task.CompletedTask;
        }

        private Task HandleCharacterCastSkill(CharacterSkillRelease message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                dungeon.HandleCharacterCastSkill(message.ClientTick, message.SkillId, message.EntityId, message.InputType, message.TargetPosition, message.TargetDirection, message.TargetEntityId);
            }
            return Task.CompletedTask;
        }

        private Task HandleDungeonLootChoice(DungeonLootChoice message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                dungeon.HandleLootChoice(message.EntityId, message.ItemId, message.IsRoll);
            }
            return Task.CompletedTask;
        }
    }
}
