using Google.Protobuf.WellKnownTypes;
using NPOI.SS.Formula.Functions;
using Server.DataBase.Entities;
using Server.Game.Actor.Domain.Chat;
using Server.Game.Actor.Domain.Player;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
using Server.Game.HFSM;
using Server.Game.World.AI;
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
    public abstract class EntityWorld
    {
        public readonly EntityContext Context;
        public readonly SkillSystem Skill;
        public readonly BuffSystem Buff;
        public readonly AreaBuffSystem AreaBuff;
        public readonly AOIService AOI;
        public readonly NavVolumeService Nav;
        public readonly ICombatContext Combat;

        private readonly AStarPathfind pathfinder;
        private int tickCounter;

        private readonly Queue<IWorldEvent> eventQueue = new Queue<IWorldEvent>();

        protected EntityWorld(
            EntityContext context,
            SkillSystem skill,
            BuffSystem buff,
            AreaBuffSystem areaBuff,
            AOIService aoi,
            NavVolumeService nav,
            AStarPathfind pathfinder)
        {
            Context = context;
            Skill = skill;
            Buff = buff;
            AreaBuff = areaBuff;
            AOI = aoi;
            Nav = nav;

            this.pathfinder = pathfinder;

            Combat = new WorldCombatContext(this);

            Initialize();
        }

        protected virtual void Initialize()
        {

        }

        public void EmitEvent(IWorldEvent worldEvent)
        {
            eventQueue.Enqueue(worldEvent);
        }

        protected virtual void ProccessWorldEvents()
        {
            while(eventQueue.Count > 0)
            {
                var ev = eventQueue.Dequeue();
                HandleWorldEvent(ev);
            }
        }

        protected virtual void HandleWorldEvent(IWorldEvent ev)
        {
            switch(ev)
            {
                case DamageWorldEvent damageWorldEvent:
                    {
                        var payload = new ServerEntityDamage
                        {
                            Deaths = damageWorldEvent.Deaths,
                            Wounds = damageWorldEvent.Wounds,
                            Source = damageWorldEvent.Source,
                            Tick = Context.Tick,
                        };
                        BroadcastToVisible(damageWorldEvent.Source, Protocol.EntityDamage, payload, true);
                        break;
                    }
                case ExecuteSkillWorldEvent executeSkillWorldEvent:
                    {
                        Console.WriteLine("ExecuteSkill:" + executeSkillWorldEvent.SkillId);
                        var payload = new ServerEntityReleaseSkill(Context.Tick, executeSkillWorldEvent.Caster.EntityId, executeSkillWorldEvent.SkillId,
                                executeSkillWorldEvent.Caster.Kinematics.Position, executeSkillWorldEvent.Caster.Kinematics.Yaw, executeSkillWorldEvent.Caster.Kinematics.State);
                        BroadcastToVisible(executeSkillWorldEvent.Caster.EntityId, Protocol.EntityReleaseSkill, payload, false);
                    }
                    break;
            }
        }


        private void BroadcastToVisible(string entityId, Protocol protocol, object payload, bool isInclude = false)
        {
            var wathcers = AOI.GetVisibleSet(entityId);
            if(isInclude) wathcers.Add(entityId);
            if (wathcers.Count == 0) return;
            var playerIds = Context.GetPlayerIdsByEntityIds(wathcers);
            if(playerIds.Count == 0) return;
            Context.Gateway.AddSend(playerIds, protocol, payload);
            
           
        }


        public void HandleCharacterSpawn(EntityRuntime entity)
        {
            Context.AddEntity(entity);
            entity.HFSM = new EntityHFSM(entity, Combat);
            AOI.Add(entity.Identity.EntityId, entity.Kinematics.Position);

            Context.Actor.AddTell(GameField.GetActor<PlayerActor>(entity.Profile.PlayerId), new CharacterEntitySnapshot(
                entity.Profile.PlayerId, entity.Profile.CharacterId,
                entity.Identity.EntityId, entity.Identity.Name, entity.Identity.Type,
                entity.Combat.Level, entity.Profile.Profession,
                entity.WorldRef.RegionId, entity.WorldRef.DungeonId));
                        Context.Actor.AddTell(GameField.GetActor<ChatActor>(),
                            new CharacterEnterRegion(Context.Id, entity.Profile.PlayerId));

            var spawnEntity = Context.GetNetworkEntityByEntityId(entity.Identity.EntityId);
            var visibleEntities = AOI.GetVisibleSet(spawnEntity.EntityId);

            var enterGamePayload = new ServerPlayerEnterGame(spawnEntity);
            Context.Gateway.AddSend(
                entity.Profile.PlayerId,
                Protocol.EnterGame,
                enterGamePayload
            );

            var (enterWatchers, _) = AOI.Update(entity.Identity.EntityId, entity.Kinematics.Position);
            if (enterWatchers.Count > 0)
            {
                var players = Context.GetPlayerIdsByEntityIds(enterWatchers);
                if (players.Count == 0) return;

                var entitySpawnPayload = new ServerEntitySpawn(Context.Tick, spawnEntity);

                Context.Gateway.AddSend(
                    players,
                    Protocol.EntitySpawn,
                    entitySpawnPayload);
            }
        }


        public void HandleCharacterDespawn(string entityId)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;

            // 1. 销毁实体
            Context.RemoveEntity(entityId);

            // 2. 向AOI范围内的其他玩家广播实体销毁

            var payload = new ServerEntityDespawn(
                Context.Tick,
                new HashSet<string> { entityId }
            );
            BroadcastToVisible(entityId, Protocol.EntityDespawn, payload);
        }


        public void HandleCharacterMove(int clientTick, string entityId, Vector3 pos, float yaw, Vector3 dir)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;
            bool isValid = Nav.IsValidVector3(pos);
            if (isValid)
            {
                entity.Kinematics.Position = pos;
                entity.Kinematics.Direction = dir;
                entity.Kinematics.Yaw = yaw;
            }

            var payload = new ServerPlayerMoveSync(
               clientTick, Context.Tick, entityId,
               entity.Kinematics.Position, entity.Kinematics.Yaw,
               entity.Kinematics.Direction, entity.Kinematics.Speed,
               isValid);

            Context.Gateway.AddSend(entity.Profile.PlayerId, Protocol.PlayerMove, payload);
        }

        public void HandleEntityMove(string entityId, Vector3 pos, float yaw, Vector3 dir)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;
            bool isValid = Nav.IsValidVector3(pos);
            if (isValid)
            {
                entity.Kinematics.Position = pos;
                entity.Kinematics.Direction = dir;
                entity.Kinematics.Yaw = yaw;
            }
        }


        public void HandleCharacterCastSkill(int clientTick, int skillId, string entityId, SkillCastInputType skillCastInputType, Vector3 targetPosition, Vector3 targetDirection, string targetEntityId)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;

            var castData = new SkillCastData
            {
                SkillId = skillId,
                InputType = skillCastInputType,
                TargetPosition = targetPosition,
                TargetDirection = targetDirection,
                TargetEntityId = targetEntityId
            };
            if(skillId == 1)
            {
                Console.WriteLine();
            }
            entity.HFSM.Ctx.OnReceiveSkillInput(castData);
        }


        public void HandleEntiyRelaseSkill(int skillId, string entityId)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;
            var castData = new SkillCastData(skillId);

            entity.HFSM.Ctx.OnReceiveSkillInput(castData);
        }

        public virtual void OnTickUpdate(int tick, float deltaTime)
        {
            tickCounter++;
            Context.Tick = tick;

            Skill.Update(deltaTime);
            Buff.Update(deltaTime);
            AreaBuff.Update(deltaTime);

            UpdateAI(deltaTime);
            UpdateFSM(deltaTime);
            ProccessWorldEvents();

            if (tickCounter % 2 == 0)
            {
                BroadcastMovement();
            }
        }


        private void BroadcastMovement()
        {
            foreach (var kv in Context.Entities)
            {
                var entity = kv.Value;
                var (enterWatchers, leaveWatchers) = AOI.Update(entity.EntityId, entity.Kinematics.Position);
                if (enterWatchers.Count > 0)
                {
                    var players = Context.GetPlayerIdsByEntityIds(enterWatchers);
                    if (players.Count != 0)
                    {
                        var spawnEntity = Context.GetNetworkEntityByEntityId(entity.EntityId);
                        var payload = new ServerEntitySpawn(
                            Context.Tick,
                            spawnEntity
                        );

                        Context.Gateway.AddSend(
                            players,
                            Protocol.EntitySpawn,
                            payload
                        );
                    }
                }

                if (leaveWatchers.Count > 0)
                {
                    var players = Context.GetPlayerIdsByEntityIds(leaveWatchers);
                    if (players.Count != 0)
                    {
                        var payload = new ServerEntityDespawn(
                            Context.Tick,
                            new HashSet<string> { entity.EntityId }
                        );

                        Context.Gateway.AddSend(
                            players,
                            Protocol.EntityDespawn,
                            payload
                        );
                    }


                }


                var lastSnap = Context.GetEntityLastBroadcast(entity.EntityId);

                var snap = new BroadcastSnapshot
                {
                    State = entity.Kinematics.State,
                    Position = entity.Kinematics.Position,
                    Yaw = entity.Kinematics.Yaw,
                    Dir = entity.Kinematics.Direction
                };
                if (!lastSnap.Equals(snap))
                {
                    var payload = new ServerEntityMoveSync(
                        Context.Tick,
                        entity.EntityId,
                        entity.Identity.Type,
                        entity.Kinematics.Position,
                        entity.Kinematics.Yaw,
                        entity.Kinematics.Direction,
                        entity.Kinematics.State,
                        entity.Kinematics.Speed
                    );
                    Context.UpdateEntityLastBroadcast(entity.EntityId, snap);
                    BroadcastToVisible(entity.EntityId, Protocol.EntityMove, payload);

                }

                
                
            }
        }

        private void UpdateFSM(float deltaTime)
        {
            foreach (var kv in Context.Entities)
            {
                var entity = kv.Value;
                if (entity.HFSM == null) continue;

                entity.HFSM.Update(deltaTime);
            }
        }


        protected virtual void HandleEntityDeath(string entityId, EntityType entityType)
        {
            var payload = new ServerEntityDespawn(Context.Tick, new HashSet<string> { entityId });
            BroadcastToVisible(entityId, Protocol.EntityDespawn, payload);

            AOI.Remove(entityId);
            Context.RemoveEntity(entityId);
        }

        private void UpdateAI(float deltaTime)
        {
            var batchIntents = new List<AIBaseIntent>();

            foreach (var (entityId, agent) in Context.AIAgents)
            {
                if (agent.Entity.Kinematics.State == EntityState.Dead) continue;

                var perceived = agent.Perception.Tick(agent, Context.Entities, AOI.GetVisibleSet(entityId));
                agent.Aggro.Tick(agent, Context.Entities, perceived);

                agent.StateMachine.Intents.Clear();
                agent.StateMachine.Tick(deltaTime);
                batchIntents.AddRange(agent.StateMachine.Intents);
            }

            if (batchIntents.Count <= 0) return;
            foreach (var intent in batchIntents)
            {
                switch (intent)
                {
                    case AIMoveIntent move:
                        HandleEntityMove(
                            entityId: move.EntityId,
                            pos: move.TargetPos,
                            yaw: move.TargetYaw,
                            dir: move.Direction
                        );
                        break;
                    case AIAttackIntent attack:
                        HandleEntiyRelaseSkill(
                            attack.SkillId, attack.EntityId);
                        break;
                }
            }
        }
    }
}
