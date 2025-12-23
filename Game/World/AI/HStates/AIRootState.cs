using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Game.HFSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.World.AI.HStates
{
    public class AIRootState : HState
    {
        private readonly AIFsmContext ctx;
        public AICombatState Combat;
        public AIPeaceState Peace;
        public AIReturnHomeState ReturnHome;

        public AIRootState(AIFsmContext ctx, HStateMachine machine, HState parent) : base(machine, parent)
        {
            this.ctx = ctx;
            Combat = new AICombatState(ctx, machine, this);
            Peace = new AIPeaceState(ctx, machine, this);
            ReturnHome = new AIReturnHomeState(ctx, machine, this);
        }

        protected override HState GetInitialState() => Peace;

        protected override HState GetTransition()
        {
            float distFromHome = Vector3.Distance(ctx.Agent.Entity.Kinematics.Position, ctx.Agent.HomePos);

            if (ctx.ReturningHome || distFromHome > ctx.Agent.LeashDistance)
            {
                // 如果还没标记，补上标记（防止 dist > Leash 但 flag 没变的情况）
                if (!ctx.ReturningHome) ctx.ReturningHome = true;

                // 如果当前不在回家状态，立刻切换
                if (ActiveChild != ReturnHome) return ReturnHome;

                // 如果已经在回家状态，保持住 (返回 null)
                return null;
            }

            // ------------------------------------------------------
            // 优先级 2: 战斗 (有目标)
            // ------------------------------------------------------
            // 能走到这里，说明 ctx.ReturningHome == false
            if (ctx.Agent.Target != null)
            {
                // 如果不在战斗状态，切过去
                if (ActiveChild != Combat) return Combat;

                // 已经在战斗，保持
                return null;
            }

            // ------------------------------------------------------
            // 优先级 3: 和平 (默认状态)
            // ------------------------------------------------------
            // 能走到这里，说明既不回家，也没目标，那就必须是 Peace
            if (ActiveChild != Peace)
            {
                return Peace;
            }

            return null;
        }
    }
}
