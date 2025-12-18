using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class CastSkillState : HState
    {
        private readonly EntityFsmContext ctx;
        public CastSkillState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        protected override void OnEnter()
        {
            ctx.LockMove = true;
            ctx.LockTurn = false;

            int skillId = ctx.CastSkillId;
            ctx.CastRequested = false;

            // 调用SkillSystem
        }

        protected override void OnUpdate(float deltaTime)
        {
            
        }

        protected override void OnExit()
        {
            
        }

        protected override HState GetTransition()
        {
            if (ctx.DeathRequested) return Parent.AsTo<AliveState>()?.Parent.AsTo<RootState>()?.Dead;
            if (ctx.HitRequested) return Parent.AsTo<AliveState>()?.Hit;

            if (ctx.CastSkill == null || ctx.CastSkill.IsFinished)
                return Parent.AsTo<AliveState>()?.Locomotion;

            return null;
        }
    }
}
