using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Game.Contracts.Common;
using Server.Game.Contracts.Server;
using Server.Game.World;
using System.Collections.Generic;

namespace Server.Game.World.Skill
{
    public class SkillSystem
    {
        // 记录正在运行的技能实例
        private readonly Dictionary<int, SkillInstance> activeSkills = new();

        // 冷却记录
        private readonly Dictionary<string, int> cooldownTracker = new();

        private long nextSkillInstanceId = 1;

        public SkillSystem() { }


        public bool IsCasting(int entityId) => activeSkills.ContainsKey(entityId);


        public int GetCurrentSkillId(int entityId)
            => activeSkills.TryGetValue(entityId, out var inst) ? inst.SkillId : 0;

        public bool TryCastSkill(
            ICombatContext ctx,
            EntityRuntime caster,
            SkillCastData? data,
            out string reason)
        {
            if (!CheckConditions(ctx, caster, data, out reason))
                return false;

            if (activeSkills.TryGetValue(caster.EntityId, out var oldSkill))
            {
                oldSkill.Interrupt();
            }

            ApplyCostAndCooldown(ctx, caster, data);

            if (SkillTimelineJsonSerializer.SkillConfigs.TryGetValue(data.SkillId, out var config))
            {
                var instance = new SkillInstance(
                    ctx,
                    caster,
                    data,
                    config,
                    OnSkillFinished
                );

                activeSkills[caster.EntityId] = instance;

                // ctx.EmitEvent(new ExecuteSkillWorldEvent { Caster = caster, SkillId = data.SkillId });

                instance.Start();
                return true;
            }

            reason = "配置错误";
            return false;
        }

        public void ForceInterrupt(int entityId)
        {
            if (activeSkills.TryGetValue(entityId, out var skill))
            {
                skill.Interrupt();
                Console.WriteLine("打断");
            }
        }

        public void Update(float dt)
        {
            foreach (var key in activeSkills.Keys.ToList())
            {
                activeSkills[key].Update(dt);
            }
        }

        // 内部回调：技能自然结束或被打断时调用
        private void OnSkillFinished(int entityId)
        {
            if (activeSkills.ContainsKey(entityId))
            {
                activeSkills.Remove(entityId);
            }
        }



        private bool CheckConditions(ICombatContext ctx, EntityRuntime caster, SkillCastData data, out string reason)
        {
            reason = "";
            if (!caster.SkillBook.Skills.TryGetValue(data.SkillId, out var skill))
            {
                reason = "未学会该技能";
                return false;
            }

            // CD 检查
            var cdKey = $"{caster.EntityId}_{data.SkillId}";
            if (cooldownTracker.TryGetValue(cdKey, out var nextTick) && ctx.Tick < nextTick)
            {
                reason = "冷却中";
                return false;
            }
        
            return true;
        }

        private void ApplyCostAndCooldown(ICombatContext ctx, EntityRuntime caster, SkillCastData data)
        {
            var skill = caster.SkillBook.Skills[data.SkillId];
            var meta = skill.GetComponent<SkillMetaComponent>();

            var cdKey = $"{caster.EntityId}_{data.SkillId}";
            cooldownTracker[cdKey] = ctx.Tick + (int)(meta.Cooldown * 1000f / GameField.TICK_INTERVAL_MS);
        }

        public bool IsSkillCooldown(ICombatContext ctx, int casterId, int skillId)
        {
            var cdKey = $"{casterId}_{skillId}";
            
            if(cooldownTracker.TryGetValue(cdKey, out var nextTick) && ctx.Tick < nextTick)
            {
                return true;
            }
            return false;
        }
    }
}