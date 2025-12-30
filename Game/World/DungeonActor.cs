using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.AAuth;
using Server.Game.Actor.Domain.ATime;
using Server.Game.Actor.Domain.Gateway;
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

        private readonly ActorEventBus bus;

        private readonly BatchGatewaySend gatewaySend;
        private readonly BatchActorSend actorSend;
        private readonly List<int> waitDestoryDungeon;

        private readonly Queue<IActorMessage> messageQueue;


        private int nextId = 0;

        public DungeonActor(string actorId, ActorEventBus bus) : base(actorId)
        {
            this.bus = bus;
            dungons = new Dictionary<int, DungeonWorld>();
            gatewaySend = new BatchGatewaySend();
            actorSend = new BatchActorSend();
            waitDestoryDungeon = new List<int>();

            messageQueue = new Queue<IActorMessage>();
        }

        protected override async Task OnStart()
        {
            await base.OnStart();
            bus.Subscribe<TickUpdateEvent>(ActorId);
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
            gatewaySend.ClearSend();
            actorSend.ClearSend();
            waitDestoryDungeon.Clear();

            // 处理队列中的所有消息
            while (messageQueue.TryDequeue(out var message))
            {
                switch (message)
                {
                        case A_CreateDungeon create: await HandleCreateDungeonInstance(create); break;

                        case A_CharacterSpawn spawn: await HandleCharacterSpawn(spawn); break;
                        case A_CharacterDespawn despawn: await HandleCharacterDespawn(despawn); break;
                        case A_CharacterMove move: await HandleCharacterMove(move); break;

                        case A_CharacterCastSkill skill: await HandleCharacterCastSkill(skill); break;
                        case A_DungeonDesotryTimer destroy: dungons.Remove(destroy.DungeonId); break;
                        case A_DungeonLootChoice loot: await HandleDungeonLootChoice(loot); break;
                        case PlayerDisconnectionEvent disconnection:
                            await OnPlayerDisconnectionEvent(disconnection);
                            break;
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


        private Task HandleCreateDungeonInstance(A_CreateDungeon message)
        {
            if (!RegionTemplateConfig.TryGetDungeonTemplateById(message.TemplateId, out var template)) return Task.CompletedTask;
            var context = new EntityContext 
            { 
                Id = nextId,
                Actor = actorSend,
                Gateway = gatewaySend,
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
            var dungeonInstance = new DungeonWorld(context, skill, buff, areaBuff, aoi, nav, pathfinder, template.LimitTime);
            dungons[context.Id] = dungeonInstance;
            gatewaySend.AddSend(message.Members, Protocol.SC_EnterDungeon,
                new ServerEnterDungeon
                {
                    DungeonTemplateId = template.Id,
                    LimitTime = template.LimitTime,
                });
            return Task.CompletedTask;
        }

        private Task HandleCharacterSpawn(A_CharacterSpawn message)
        {
            if (!dungons.TryGetValue(message.Runtime.World.DungeonId, out var dungeon)) return Task.CompletedTask;
           
            dungeon.HandleCharacterSpawn(message.Runtime);

            return Task.CompletedTask;
        }

        private Task HandleCharacterDespawn(A_CharacterDespawn message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
                dungeon.HandleCharacterDespawn(message.EntityId);
            return Task.CompletedTask;
        }

        private Task HandleCharacterMove(A_CharacterMove message)
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

        private Task HandleCharacterCastSkill(A_CharacterCastSkill message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                dungeon.HandleCharacterCastSkill(message.ClientTick, message.SkillId, message.EntityId, message.InputType, message.TargetPosition, message.TargetDirection, message.TargetEntityId);
            }
            return Task.CompletedTask;
        }

        private Task HandleDungeonLootChoice(A_DungeonLootChoice message)
        {
            if (dungons.TryGetValue(message.DungeonId, out var dungeon))
            {
                dungeon.HandleLootChoice(message.EntityId, message.ItemId, message.IsRoll);
            }
            return Task.CompletedTask;
        }
    }
}
