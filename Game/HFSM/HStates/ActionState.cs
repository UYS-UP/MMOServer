using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class ActionState : HState
    {
        private readonly EntityFsmContext ctx;
        public readonly AttackState Attack;
        public readonly RollState Roll;
        public readonly CastSkillState CastSkill;

        public bool IsFinished => !ctx.Combat.IsSkillRunning(ctx.Entity.EntityId);

        public ActionState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
            Attack = new AttackState(ctx, m, this);
            Roll = new RollState(ctx, m, this);
            CastSkill = new CastSkillState(ctx, m, this);
        }

        protected override HState GetInitialState()
        {
            if (ctx.RollRequested) return Roll;
            if (ctx.AttackRequested) return Attack;
            if (ctx.CastRequested) return CastSkill;
            return Attack;
        }

        protected override void OnEnter()
        {
            ctx.LockMove = true;
            ctx.LockTurn = true;
        }

        protected override void OnExit()
        {
            if (ctx.Combat.IsSkillRunning(ctx.Entity.EntityId))
            {
                ctx.Combat.InterruptSkill(ctx.Entity.EntityId);
            }
            ctx.LockMove = false;
            ctx.LockTurn = false;
        }

        protected override HState GetTransition()
        {
            if (ActiveChild != Roll && ctx.RollRequested) return Roll;
            if (ActiveChild == Attack && ctx.CastRequested) return CastSkill;
            return null;
        }

    }
}
