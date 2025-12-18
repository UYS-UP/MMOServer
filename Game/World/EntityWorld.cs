using Google.Protobuf.WellKnownTypes;
using NPOI.SS.Formula.Functions;
using Server.DataBase.Entities;
using Server.Game.Actor.Domain.Chat;
using Server.Game.Actor.Domain.Player;
using Server.Game.Actor.Domain.Region.AI;
using Server.Game.Actor.Domain.Region.AStar;
using Server.Game.Actor.Domain.Region.FSM;
using Server.Game.Actor.Domain.Region.FSM.Action;
using Server.Game.Actor.Domain.Region.FSM.Motion;
using Server.Game.Actor.Domain.Region.Services;
using Server.Game.Actor.Domain.Region.Skill;
using Server.Game.Actor.Domain.Region.Skill.Buff;
using Server.Game.Contracts.Actor;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Network;
using Server.Game.Contracts.Server;
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
                        //var payload = new ServerEntityReleaseSkill(Context.Tick, executeSkillWorldEvent.Caster.EntityId, executeSkillWorldEvent.SkillId,
                        //        executeSkillWorldEvent.Caster.Kinematics.Position, executeSkillWorldEvent.Caster.Kinematics.Yaw, executeSkillWorldEvent.Caster.Kinematics.StateType);
                        //BroadcastToVisible(executeSkillWorldEvent.Caster.EntityId, Protocol.EntityReleaseSkill, payload, true);
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
            AOI.Add(entity.Identity.EntityId, entity.Kinematics.Position);

            Context.Actor.AddTell(GameField.GetActor<PlayerActor>(entity.Profile.PlayerId), new CharacterEntitySnapshot(
                entity.Profile.PlayerId, entity.Profile.CharacterId,
                entity.Identity.EntityId, entity.Identity.Name, entity.Identity.Type,
                entity.Combat.Level, entity.Profile.Profession,
                entity.WorldRef.RegionId, entity.WorldRef.DungeonId));
                        Context.Actor.AddTell(GameField.GetActor<ChatActor>(),
                            new CharacterEnterRegion(Context.Id, entity.Profile.PlayerId));

            entity.FSM = new LayeredStateCoordinator(entity);
            entity.FSM.Motion.AddState(MotionStateType.Idle, new IdleState());
            entity.FSM.Motion.AddState(MotionStateType.Move, new MoveState());
            entity.FSM.Action.AddState(ActionStateType.None, new NoneActionState());
            entity.FSM.Motion.SetInitialState(MotionStateType.Idle);
            entity.FSM.Action.SetInitialState(ActionStateType.None);
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
            //if (!Context.TryGetEntity(entityId, out var entity)) return;

            //if (!Skill.CastSkill(Combat, skillId, entity, skillCastInputType, targetPosition, targetDirection, targetEntityId, out var reason))
            //{
            //    Context.Gateway.AddSend(entity.Profile.PlayerId,
            //        Protocol.PlayerReleaseSkill, new ServerPlayerReleaseSkill(Context.Tick, clientTick, skillId,
            //        entity.Kinematics.StateType, false, reason));
            //    return;
            //}
        }


        public void HandleEntiyRelaseSkill(int skillId, string entityId)
        {
            if (!Context.TryGetEntity(entityId, out var entity))
                return;
            entity.Kinematics.Direction = Vector3.Zero;
            if (!Skill.CastSkill(Combat, skillId, entity, SkillCastInputType.None, Vector3.Zero, Vector3.Zero, string.Empty, out var _))
                return;
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

            // 玩家位置同步(每3帧一次)
            if (tickCounter % 3 == 0)
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


                var last = Context.GetEntityLastBroadcast(entity.EntityId);
                bool stateChanged = last.Action != entity.Kinematics.ActionState || last.Motion != entity.Kinematics.MotionState;
                bool moved = entity.Kinematics.Direction != Vector3.Zero; // 或 position delta 更稳


                if (moved || stateChanged)
                {

                    var payload = new ServerEntityMoveSync(
                        Context.Tick,
                        entity.EntityId,
                        entity.Identity.Type,
                        entity.Kinematics.Position,
                        entity.Kinematics.Yaw,
                        entity.Kinematics.Direction,
                        entity.Kinematics.MotionState,
                        entity.Kinematics.ActionState,
                        entity.Kinematics.Speed
                    );
                    BroadcastToVisible(entity.EntityId, Protocol.EntityMove, payload);
                    Context.UpdateEntityLastBroadcast(entity.EntityId, entity.Kinematics.ActionState, entity.Kinematics.MotionState);
                }
            }
        }

        private void UpdateFSM(float deltaTime)
        {
            var batchIntents = new List<EntityIntent>();
            foreach (var kv in Context.Entities)
            {
                var fsm = kv.Value.FSM;
                fsm.Intents.Clear();
                fsm.Update(deltaTime);

                batchIntents.AddRange(fsm.Intents);
            }


            if (batchIntents.Count <= 0) return;
            foreach (var intent in batchIntents)
            {
                switch (intent)
                {
                    case RemoveEntityIntent removeEntityIntent:
                        HandleEntityDeath(removeEntityIntent.Entity.Identity.EntityId, removeEntityIntent.Entity.Identity.Type);
                        break;
                }
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
                if (agent.Entity.Kinematics.ActionState == ActionStateType.Death) continue;

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
