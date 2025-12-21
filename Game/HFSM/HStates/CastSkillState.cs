using NPOI.OpenXmlFormats.Spreadsheet;
using Server.Game.Contracts.Server;
using Server.Game.World;
using Server.Game.World.Skill;
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
            // 锁定状态
            Console.WriteLine($"Enter CastSkill");
            ctx.LockMove = true;
            ctx.LockTurn = false;

            ctx.Entity.Kinematics.State = EntityState.CastSkill;

            // 优先处理新输入，其次处理连招缓存
            SkillCastData dataToCast = null;
            if (ctx.IncomingSkillRequest != null)
            {
                dataToCast = ctx.IncomingSkillRequest;
                ctx.ConsumeInput();
            }
            else if (ctx.ComboBuffer != null)
            {
                dataToCast = ctx.ComboBuffer;
                ctx.ConsumeCombo();
            }

            if (dataToCast != null)
            {
                bool success = ctx.Combat.TryCastSkill(ctx.Entity, dataToCast, out var reason);

                if (!success)
                {
                    Console.WriteLine($"[FSM] 起手技能释放失败: {reason}");
                }
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (ctx.IncomingSkillRequest != null)
            {
                ctx.ComboBuffer = ctx.IncomingSkillRequest;
                ctx.ConsumeInput();
            }

            if (ctx.ComboBuffer != null)
            {
                bool success = ctx.Combat.TryCastSkill(ctx.Entity, ctx.ComboBuffer, out var reason);

                if (success)
                {
                    ctx.ConsumeCombo();
                }
                else
                {
                    Console.WriteLine($"[FSM] 连招失败: {reason}");
                    ctx.ConsumeCombo();
                }
            }
        }

        protected override void OnExit()
        {
            ctx.ConsumeCombo();
            ctx.ConsumeInput();
            if (ctx.HitRequested || ctx.DeathRequested)
            {
                ctx.ConsumeCombo();
            }

            // 确保技能系统被通知打断
            if (ctx.Combat.IsSkillRunning(ctx.Entity.EntityId))
            {
                ctx.Combat.InterruptSkill(ctx.Entity.EntityId);
            }
        }

        protected override HState GetTransition()
        {

            if (ctx.DeathRequested) return Parent.AsTo<AliveState>()?.Parent.AsTo<RootState>()?.Dead;
            if (ctx.HitRequested) return Parent.AsTo<AliveState>()?.Hit;

           
            if (!ctx.Combat.IsSkillRunning(ctx.Entity.EntityId)
                && ctx.ComboBuffer == null
                && ctx.IncomingSkillRequest == null)
            {
                return Parent.AsTo<AliveState>().Locomotion;
            }

            return null;
        }
    }
}

