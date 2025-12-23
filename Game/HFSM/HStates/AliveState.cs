using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM.HStates
{
    public class AliveState : HState
    {
        private readonly EntityFsmContext ctx;
        public readonly LocomotionState Locomotion;
        public readonly ActionState Action;
        public StunnedState Hit;

        public AliveState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
            Locomotion = new LocomotionState(ctx, m, this);
            Action = new ActionState(ctx, m, this);
            Hit = new StunnedState(ctx, m, this);
        }

 

        protected override HState GetInitialState() => Locomotion;

        protected override HState GetTransition()
        {
            if (ctx.DeathRequested) return Parent.AsTo<RootState>()?.Dead;
            if (ctx.HitRequested) return Hit;
            if (ActiveChild == Locomotion)
            {
                if (ctx.AttackRequested || ctx.CastRequested || ctx.RollRequested)
                {
                    return Action;
                }
            }
            else if (ActiveChild == Action)
            {
                if (!ctx.Combat.IsSkillRunning(ctx.Entity.EntityId))
                {
                    return Locomotion;
                }
            }

            return null;
        }

        protected override void OnUpdate(float deltaTime)
        {
            ctx.ConsumeOneFrameFlags();
        }
    }
}
