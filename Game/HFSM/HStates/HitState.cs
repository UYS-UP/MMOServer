using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NPOI.HSSF.Util.HSSFColor;

namespace Server.Game.HFSM.HStates
{
    public class HitState : HState
    {
        private readonly EntityFsmContext ctx;
        private const float duration = 1.167f;

        public HitState(EntityFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
        }

        public bool IsFinished;
        private float currentTime;

        protected override void OnEnter()
        {
            ctx.Entity.Kinematics.State = EntityState.Hit;
            ctx.LockMove = true;
            ctx.LockTurn = true;
            ctx.HitRequested = false;
            IsFinished = false;
            currentTime = 0;
        }

        protected override void OnUpdate(float deltaTime)
        {
            currentTime += deltaTime;
            if (currentTime >= duration)
            {
                IsFinished = true;
            }
        }
    }
}
