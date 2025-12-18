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
        public HitState Hit;

        public AliveState(EntityFsmContext ctx, HStateMachine m, HState p) : base(m, p)
        {
            this.ctx = ctx;
            Locomotion = new LocomotionState(ctx, m, this);
            CastSkill = new CastSkillState(ctx, m, this);
            Hit = new HitState(ctx, m, this);
        }

        protected override HState GetInitialState() => Locomotion;

        protected override void OnUpdate(float deltaTime)
        {
            ctx.ConsumeOneFrameFlags();
        }
    }
}
