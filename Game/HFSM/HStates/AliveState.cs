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
        public LocomotionState Locomotion;
        public CastSkillState CastSkill;
        public StunnedState Hit;

        public AliveState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
            Locomotion = new LocomotionState(ctx, m, this);
            CastSkill = new CastSkillState(ctx, m, this);
            Hit = new StunnedState(ctx, m, this);
        }

 

        protected override HState GetInitialState() => Locomotion;

        protected override HState GetTransition()
        {
            if(ctx.IncomingSkillRequest != null)
            {
                if (!ctx.Combat.IsSkillRunning(ctx.Entity.EntityId))
                {
                    return CastSkill;
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
