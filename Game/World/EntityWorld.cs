using Google.Protobuf.WellKnownTypes;
using MessagePack;
using NPOI.SS.Formula.Functions;
using Server.DataBase.Entities;
using Server.Game.Actor.Core;
using Server.Game.Actor.Domain.ASession;
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
using Server.Utility;
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
        public readonly ActorBase Actor;

        private readonly AStarPathfind pathfinder;
        private int tickCounter;

        private readonly Queue<IWorldEvent> eventQueue = new Queue<IWorldEvent>();


        protected EntityWorld(
            ActorBase actor,
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
            Actor = actor;

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

        protected virtual async Task ProccessWorldEvents()
        {
            while(eventQueue.Count > 0)
            {
                var ev = eventQueue.Dequeue();
                await HandleWorldEvent(ev);
            }
        }

        protected virtual async Task HandleWorldEvent(IWorldEvent ev)
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
                        await BroadcastToVisible(damageWorldEvent.Source, Protocol.SC_EntityDamage, payload, true);
                        break;
                    }
                case ExecuteSkillWorldEvent executeSkillWorldEvent:
                    {
                        var payload = new ServerEntityCastSkill(Context.Tick, executeSkillWorldEvent.Caster.EntityId, executeSkillWorldEvent.SkillId,
                                executeSkillWorldEvent.Caster.Kinematics.Position, executeSkillWorldEvent.Caster.Kinematics.Yaw, executeSkillWorldEvent.Caster.Kinematics.State);
                        await BroadcastToVisible(executeSkillWorldEvent.Caster.EntityId, Protocol.SC_EntityCastSkill, payload, false);
                    }
                    break;
            }
        }


        private async Task BroadcastToVisible(int entityId, Protocol protocol, object payload, bool isInclude = false)
        {
            var wathcers = AOI.GetVisibleSet(entityId);
            if(isInclude) wathcers.Add(entityId);
            if (wathcers.Count == 0) return;
            var characterIds = Context.GetCharacterIdsByEntityIds(wathcers);
            if(characterIds.Count == 0) return;
            var bytes = MessagePackSerializer.Serialize(payload);
            foreach ( var characterId in characterIds)
            {
                await Actor.TellGateway(characterId, protocol, bytes);
            }
            
            
           
        }


        public virtual Task HandleCharacterSpawn(EntityRuntime entity)
        {
            return Task.CompletedTask;
        }


        public async Task HandleCharacterDespawn(int entityId)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;

            Context.RemoveEntity(entityId);

            var payload = new ServerEntityDespawn(
                Context.Tick,
                new HashSet<int> { entityId }
            );
            await BroadcastToVisible(entityId, Protocol.SC_EntityDespawn, payload);
        }


        public async Task HandleCharacterMove(int clientTick, int entityId, Vector3 pos, float yaw, Vector3 dir)
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
            var bytes = MessagePackSerializer.Serialize(payload);
            await Actor.TellGateway(entity.Identity.CharacterId, Protocol.SC_CharacterMove, bytes);

        }

        public void HandleEntityMove(int entityId, Vector3 pos, float yaw, Vector3 dir)
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


        public void HandleCharacterCastSkill(int clientTick, int skillId, int entityId, SkillCastInputType skillCastInputType, Vector3 targetPosition, Vector3 targetDirection, string targetEntityId)
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
            entity.HFSM.Ctx.OnRequestSkill(castData);
        }


        public void HandleEntiyRelaseSkill(int skillId, int entityId)
        {
            if (!Context.TryGetEntity(entityId, out var entity)) return;
            var castData = new SkillCastData(skillId);

            entity.HFSM.Ctx.OnRequestSkill(castData);
        }

        public virtual async Task OnTickUpdate(int tick, float deltaTime)
        {
            tickCounter++;
            Context.Tick = tick;

            Skill.Update(deltaTime);
            Buff.Update(deltaTime);
            AreaBuff.Update(deltaTime);

            UpdateAI(deltaTime);
            UpdateFSM(deltaTime);
            await ProccessWorldEvents();

            if (tickCounter % 2 == 0)
            {
                await BroadcastMovement();
            }
        }


        private async Task BroadcastMovement()
        {
            foreach (var kv in Context.Entities)
            {
                var entity = kv.Value;
                var (enterWatchers, leaveWatchers) = AOI.Update(entity.EntityId, entity.Kinematics.Position);
                if (enterWatchers.Count > 0)
                {
                    var characterIds = Context.GetCharacterIdsByEntityIds(enterWatchers);
                    if (characterIds.Count != 0)
                    {
                        var spawnEntity = Context.GetNetworkEntityByEntityId(entity.EntityId);
                        var payload = new ServerEntitySpawn(
                            Context.Tick,
                            spawnEntity
                        );
                        var bytes = MessagePackSerializer.Serialize(payload);
                        foreach (var characterId in characterIds)
                        {
                            await Actor.TellGateway(characterId, Protocol.SC_EntitySpawn, bytes);
                        }
                    }
                }

                if (leaveWatchers.Count > 0)
                {
                    var characterIds = Context.GetCharacterIdsByEntityIds(leaveWatchers);
                    if (characterIds.Count != 0)
                    {
                        var payload = new ServerEntityDespawn(
                            Context.Tick,
                            new HashSet<int> { entity.EntityId }
                        );
                        var bytes = MessagePackSerializer.Serialize(payload);
                        foreach (var characterId in characterIds)
                        {
                            await Actor.TellGateway(characterId, Protocol.SC_EntityDespawn, bytes);
                        }
   
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
                    await BroadcastToVisible(entity.EntityId, Protocol.SC_EntityMove, payload);

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


        protected virtual async Task HandleEntityDeath(int entityId, EntityType entityType)
        {
            var payload = new ServerEntityDespawn(Context.Tick, new HashSet<int> { entityId });
            await BroadcastToVisible(entityId, Protocol.SC_EntityDespawn, payload);

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
                agent.AiFsm.Update(deltaTime);
                batchIntents.AddRange(agent.AiFsm.Ctx.Intents);
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
