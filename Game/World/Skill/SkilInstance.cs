using Server.Game.Contracts.Server;
using Server.Game.World.Skill.Buff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.Skill
{
    public class SkillInstance
    {
        // 1. 持有环境依赖 (解决依赖注入问题)
        public readonly ICombatContext Combat;

        // 2. 持有技能数据 (目标是谁，往哪放)
        public readonly SkillCastData Data;
        public readonly EntityRuntime Caster;

        public int SkillId => Data.SkillId;

        private readonly SkillTimelineRunner runner;
        private readonly Action<int> onFinished;

        public float CurrentTime => runner.CurrentTime;
        public bool IsFinished => runner.IsFinished;


        public SkillInstance(
           ICombatContext combatContext,
           EntityRuntime caster,
           SkillCastData data,
           SkillTimelineConfig config,
           Action<int> onFinished)
        {
            this.Combat = combatContext;
            this.Caster = caster;
            this.Data = data;
            this.onFinished = onFinished;
      
            // 初始化 Runner
            runner = new SkillTimelineRunner(config.Duration, config.ServerEvents, config.ServerPhases);
        }

        public void Start()
        {
            runner.Start(this);
        }

        public void Update(float dt)
        {
            if (runner.IsFinished) return;
            runner.Tick(this, dt);
            if (runner.IsFinished)
            {
                onFinished?.Invoke(Caster.EntityId);
            }
        }

        public void Interrupt()
        {
            if (runner.IsFinished) return;
            runner.Interrupt(this);
            onFinished?.Invoke(Caster.EntityId);
        }
    }
}
