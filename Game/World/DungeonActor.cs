using MessagePack;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ACharacter;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Actor.Domain.Team;
using Server.Game.Contracts.Network;
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
        private readonly Dictionary<int, DungeonWorld> dungons;

        private ActorEventBus EventBus => System.EventBus;

        private readonly List<int> waitDestoryDungeon;

        private readonly Queue<IActorMessage> messageQueue;


        private int nextId = 0;

        public DungeonActor(string actorId) : base(actorId)
        {
            dungons = new Dictionary<int, DungeonWorld>();
            waitDestoryDungeon = new List<int>();

            messageQueue = new Queue<IActorMessage>();
        }

        protected override async Task OnStart()
        {
            await base.OnStart();
            EventBus.Subscribe<TickUpdateEvent>(ActorId);
        }


        protected override async Task OnReceive(IActorMessage message)
        {
            switch (message)
            {
                case TickUpdateEvent tickUpdateEvent:
                    await OnTickUpdateEvent(tickUpdateEvent);
                    break;



                default:
                    messageQueue.Enqueue(message);
                    break;
            }
        }




        private async Task OnTickUpdateEvent(TickUpdateEvent args)
        {
            tick = args.Tick;
            waitDestoryDungeon.Clear();

            // 处理队列中的所有消息
            while (messageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                        case A_CreateDungeon create: await HandleCreateDungeonInstance(create); break;
                        case A_CharacterSpawn spawn: await HandleCharacterSpawn(spawn); break;
                        case A_CharacterDespawn despawn: await HandleCharacterDespawn(despawn); break;
                        case CS_CharacterMove move: await CS_HandleCharacterMove(move); break;

                        case CS_CharacterCastSkill skill: await CS_HandleCharacterCastSkill(skill); break;
                        case A_DungeonDesotryTimer destroy: dungons.Remove(destroy.DungeonId); break;
                        case CS_DungeonLootChoice loot: await CS_HandleDungeonLootChoice(loot); break;
                        case PlayerDisconnectionEvent disconnection:
                            await OnPlayerDisconnectionEvent(disconnection);
                            break;
                }
            }

            foreach (var kv in dungons)
            {
                await kv.Value.OnTickUpdate(args.Tick, args.DeltaTime);
            }
              
            foreach (var dungeonId in waitDestoryDungeon)
            {
                dungons.Remove(dungeonId);
            }

                
        }



        private Task OnPlayerDisconnectionEvent(PlayerDisconnectionEvent args)
        {
            // TODO: 处理玩家掉线（踢出副本、状态保存等）

            return Task.CompletedTask;
        }


        private async Task HandleCreateDungeonInstance(A_CreateDungeon message)
        {
            if (!RegionTemplateConfig.TryGetDungeonTemplateById(message.TemplateId, out var template)) return;
            var context = new EntityContext 
            { 
                Id = nextId,
                WaitDestory = waitDestoryDungeon,
            };
            nextId++;
            var buff = new BuffSystem();
            var areaBuff = new AreaBuffSystem();
            var skill = new SkillSystem();
            var nav = new NavVolumeService(template.NavMeshPath);
            var (min, max) = nav.GetMapBoundsXZ();
            var aoi = new AOIService(Vector2.Distance(min, max), 100, 100);

            var pathfinder = new AStarPathfind(nav);
            var dungeonInstance = new DungeonWorld(this, context, skill, buff, areaBuff, aoi, nav, pathfinder, template.LimitTime);
            dungons[context.Id] = dungeonInstance;
            var payload = new ServerEnterDungeon
            {
                DungeonTemplateId = template.Id,
                LimitTime = template.LimitTime,
            };
            var bytes = MessagePackSerializer.Serialize(payload);
            foreach (var playerId in message.Members)
            {
                await TellGateway(playerId, Protocol.SC_EnterDungeon, bytes);
            }
   
        }

        private Task HandleCharacterSpawn(A_CharacterSpawn message)
        {
            if (!dungons.TryGetValue(message.Runtime.World.DungeonId, out var dungeon)) return Task.CompletedTask;
           
            dungeon.HandleCharacterSpawn(message.Runtime);

            return Task.CompletedTask;
        }

        private async Task HandleCharacterDespawn(A_CharacterDespawn message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
                await dungeon.HandleCharacterDespawn(message.EntityId);
        }

        private async Task CS_HandleCharacterMove(CS_CharacterMove message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                await dungeon.HandleCharacterMove(
                    message.ClientTick,
                    message.EntityId,
                    message.Position,
                    message.Yaw,
                    message.Direction);
            }
        }

        private Task CS_HandleCharacterCastSkill(CS_CharacterCastSkill message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                dungeon.HandleCharacterCastSkill(message.ClientTick, message.SkillId, message.EntityId, message.InputType, message.TargetPosition, message.TargetDirection, message.TargetEntityId);
            }
            return Task.CompletedTask;
        }

        private async Task CS_HandleDungeonLootChoice(CS_DungeonLootChoice message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                await dungeon.HandleLootChoice(message.CharacterId, message.ItemId, message.IsRoll);
            }
        }
    }
}
