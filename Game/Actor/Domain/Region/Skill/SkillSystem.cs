using Server.Game.Actor.Domain.Region.Skill.Buff;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Server;
using Server.Game.World;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Game.Actor.Domain.Region.Skill
{
    public class SkillSystem
    {
        private readonly BuffSystem buffSystem;

        // CD：记录下一次可释放的 tick
        private readonly Dictionary<string, int> cooldownTracker = new();

        // 当前正在释放的技能（单实体最多一个）
        private readonly Dictionary<string, SkillInstance> activeSkills = new();

        // 技能输入意图队列（预输入/连招）
        private readonly Dictionary<string, Queue<PendingSkill>> pendingSkillQueues = new();

        private long nextSkillInstanceId = 1;

        private class PendingSkill
        {
            public int SkillId { get; set; }
            public SkillCastInputType InputType { get; set; }
            public Vector3 TargetPosition { get; set; }
            public Vector3 TargetDirection { get; set; }
            public string TargetEntityId { get; set; }
        }

        public SkillSystem(BuffSystem buffSystem)
        {
            this.buffSystem = buffSystem;
        }

        /// <summary>
        /// 只做校验：不扣 MP、不写 CD、不改 IsBusy。
        /// </summary>
        public bool CanCastSkill(
            ICombatContext combatContext,
            EntityRuntime caster,
            int skillId,
            SkillCastInputType skillCastInputType,
            Vector3 targetPosition,
            Vector3 targetDirection,
            string targetEntityId,
            out string reason)
        {
            reason = string.Empty;

            var skillBook = caster.SkillBook;
            if (!skillBook.Skills.TryGetValue(skillId, out var skill))
            {
                reason = "技能不存在";
                return false;
            }

            // 冷却校验
            var cdKey = $"{caster.Identity.EntityId}_{skillId}";
            if (cooldownTracker.TryGetValue(cdKey, out var nextTick) && combatContext.Tick < nextTick)
            {
                reason = "冷却中";
                return false;
            }

            // 目标校验（示例：单位目标需要存在且在距离内）
            if (skillCastInputType == SkillCastInputType.UnitTarget)
            {
                if (!combatContext.TryGetEntity(targetEntityId, out var target))
                {
                    reason = "目标不存在";
                    return false;
                }
                if (Vector3.Distance(target.Kinematics.Position, caster.Kinematics.Position) > 12.0f)
                {
                    reason = "目标距离过远";
                    return false;
                }
            }

            // MP 校验
            var meta = skill.GetComponent<SkillMetaComponent>();
            if (caster.Combat.Mp < meta.ManaCost)
            {
                reason = "MP不足";
                return false;
            }

            // 其他：比如死亡/硬直/沉默等，也建议在这里统一校验
            // if (caster.Kinematics.StateType == EntityStateType.Death) { ... }

            return true;
        }

        /// <summary>
        /// 玩家/AI 请求释放技能：
        /// - 若当前正在释放技能：入队（pending）
        /// - 否则：立即执行（此时才扣 MP/写 CD/Busy=true）
        /// </summary>
        public bool CastSkill(
            ICombatContext combatContext,
            int skillId,
            EntityRuntime caster,
            SkillCastInputType skillCastInputType,
            Vector3 targetPosition,
            Vector3 targetDirection,
            string targetEntityId,
            out string reason)
        {
            reason = string.Empty;

            if (!SkillTimelineJsonSerializer.SkillConfigs.TryGetValue(skillId, out var config))
            {
                reason = "技能配置不存在";
                return false;
            }

            if (!CanCastSkill(combatContext, caster, skillId, skillCastInputType, targetPosition, targetDirection, targetEntityId, out reason))
                return false;

            var pending = new PendingSkill
            {
                SkillId = skillId,
                InputType = skillCastInputType,
                TargetPosition = targetPosition,
                TargetDirection = targetDirection,
                TargetEntityId = targetEntityId,
            };

            // 当前正在释放：进入 pending 队列（不扣费、不写 CD）
            if (activeSkills.ContainsKey(caster.EntityId))
            {
                if (!pendingSkillQueues.TryGetValue(caster.EntityId, out var queue))
                {
                    queue = new Queue<PendingSkill>();
                    pendingSkillQueues[caster.EntityId] = queue;
                }

                // 可选：限制队列长度，避免疯狂预输入
                // if (queue.Count >= 2) { reason = "预输入过多"; return false; }

                queue.Enqueue(pending);
                return true;
            }

            // 空闲：立即执行（这里才扣费/写CD/Busy=true）
            return ExecuteSkill(combatContext, caster, pending, out reason);
        }

        /// <summary>
        /// 真正开始执行技能：扣 MP、写 CD、Busy=true、创建 SkillInstance。
        /// 如果执行前校验失败（例如排队期间状态改变/MP变化），会直接丢弃该意图并给 reason。
        /// </summary>
        private bool ExecuteSkill(ICombatContext combatContext, EntityRuntime caster, PendingSkill pendingSkill, out string reason)
        {
            reason = string.Empty;

            if (!SkillTimelineJsonSerializer.SkillConfigs.TryGetValue(pendingSkill.SkillId, out var config))
            {
                reason = "技能配置不存在";
                return false;
            }

            // ⚠️ 再校验一次（排队期间可能发生：被扣蓝/换目标/目标死亡/进入沉默等）
            if (!CanCastSkill(
                    combatContext,
                    caster,
                    pendingSkill.SkillId,
                    pendingSkill.InputType,
                    pendingSkill.TargetPosition,
                    pendingSkill.TargetDirection,
                    pendingSkill.TargetEntityId,
                    out reason))
            {
                return false;
            }

            // === 只有在这里才进行“有副作用”的操作 ===
            var skill = caster.SkillBook.Skills[pendingSkill.SkillId];
            var meta = skill.GetComponent<SkillMetaComponent>();

            caster.Combat.Mp -= meta.ManaCost;

            var cdKey = $"{caster.Identity.EntityId}_{pendingSkill.SkillId}";
            cooldownTracker[cdKey] = combatContext.Tick + (int)(meta.Cooldown * 1000f / GameField.TICK_INTERVAL_MS);

            caster.SkillBook.IsBusy = true;

            var instance = new SkillInstance(
                combatContext,
                buffSystem,
                nextSkillInstanceId++,
                pendingSkill.SkillId,
                config.Duration,
                caster,
                config.Events,
                pendingSkill.TargetPosition,
                pendingSkill.TargetDirection,
                pendingSkill.TargetEntityId,
                // 技能结束/打断统一回调
                (casterId) => OnSkillEnded(combatContext, casterId)
            );

            caster.FSM.Action.RequestChange(ActionStateType.CastSkill);
            activeSkills[caster.EntityId] = instance;
            combatContext.EmitEvent(new ExecuteSkillWorldEvent
            {
                Caster = caster,
                SkillId = pendingSkill.SkillId,
            });
            instance.Start();
            return true;
        }

        /// <summary>
        /// 技能结束/打断：移除 active，Busy=false，尝试执行 pending 队列的下一个
        /// </summary>
        private void OnSkillEnded(ICombatContext combatContext, string casterId)
        {
            // 释放 busy
            if (combatContext.TryGetEntity(casterId, out var caster))
                caster.SkillBook.IsBusy = false;

            activeSkills.Remove(casterId);

            // 如果有排队：取出一个尝试执行
            if (pendingSkillQueues.TryGetValue(casterId, out var queue))
            {
                while (queue.Count > 0)
                {
                    var next = queue.Dequeue();

                    // caster 可能已经不存在/死亡
                    if (!combatContext.TryGetEntity(casterId, out caster))
                        return;

                    // 尝试执行；如果失败（蓝不够/目标没了/冷却等）就继续丢弃下一个意图
                    if (ExecuteSkill(combatContext, caster, next, out _))
                        return;
                }
            }

            // 无 pending：回 Idle
            caster.FSM.Action.RequestChange(ActionStateType.CastSkill);
        }

        /// <summary>
        /// 打断技能：让 SkillInstance 走统一结束回调，从而能够继续处理 pending
        /// </summary>
        public bool InterruptSkill(string casterId)
        {
            if (!activeSkills.TryGetValue(casterId, out var skill)) return false;
            skill.Interrupt(); // Interrupt 内部会触发 onEnded
            return true;
        }

        public void Update(float deltaTime)
        {
            // 不要在这里直接 Remove，否则会绕过 OnSkillEnded 的队列逻辑
            foreach (var kv in activeSkills)
                kv.Value.Update(deltaTime);
        }
    }

    public class SkillInstance
    {
        public long InstanceId;
        public int SkillId;
        public EntityRuntime Caster;
        public float Duration;

        public float CurrentTime { get; private set; } = 0f;
        public bool IsFinished { get; private set; }
        public bool IsInterrupted { get; private set; }

        public ICombatContext CombatContext { get; private set; }
        public BuffSystem Buff { get; private set; }

        public Vector3 TargetPositon;
        public Vector3 TargetDirection;
        public string TargetEntityId;

        private readonly List<SkillEvent> events;
        private int nextEventIndex = 0;

        private readonly Action<string> onEnded; // 结束/打断统一回调（重要）

        public SkillInstance(
            ICombatContext combatContext,
            BuffSystem buff,
            long instanceId,
            int skillId,
            float duration,
            EntityRuntime caster,
            List<SkillEvent> events,
            Vector3 targetPosition,
            Vector3 targetDirection,
            string targetEntityId,
            Action<string> onEnded)
        {
            CombatContext = combatContext;
            Buff = buff;

            InstanceId = instanceId;
            SkillId = skillId;
            Duration = duration;
            Caster = caster;

            TargetPositon = targetPosition;
            TargetDirection = targetDirection;
            TargetEntityId = targetEntityId;

            this.onEnded = onEnded;

            this.events = new List<SkillEvent>(events);
            this.events.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        public void Start()
        {
            // 立即执行 Time=0 的事件
            while (nextEventIndex < events.Count && Math.Abs(events[nextEventIndex].Time) < 0.0001f)
            {
                events[nextEventIndex].Execute(this);
                nextEventIndex++;
            }

            if (nextEventIndex >= events.Count)
                End(interrupted: false);
        }

        public void Update(float dt)
        {
            if (IsFinished || IsInterrupted) return;

            CurrentTime += dt;

            while (nextEventIndex < events.Count && events[nextEventIndex].Time <= CurrentTime + 0.00001f)
            {
                events[nextEventIndex].Execute(this);
                nextEventIndex++;
            }

            if (CurrentTime >= Duration)
                End(interrupted: false);
        }

        public void Interrupt()
        {
            if (IsFinished || IsInterrupted) return;
            End(interrupted: true);
        }

        private void End(bool interrupted)
        {
            if (IsFinished || IsInterrupted) return;

            if (interrupted) IsInterrupted = true;
            else IsFinished = true;

            // Busy 的释放放在 SkillSystem.OnSkillEnded 里统一做
            onEnded?.Invoke(Caster.EntityId);
        }
    }
}
