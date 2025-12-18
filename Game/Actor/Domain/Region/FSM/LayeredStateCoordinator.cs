using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM
{
    public interface IActionControl
    {
        bool LockMove { get; }      // 锁移动输入/位移
        bool LockTurn { get; }      // 锁转向
        bool OverlayMotion { get; } // 覆盖 Motion 表现（动作播时不让 Motion 改动画）
    }

    public abstract class EntityIntent
    {
        public EntityRuntime Entity { get; protected set; }
        protected EntityIntent(EntityRuntime entity) => Entity = entity;
    }

    public class RemoveEntityIntent : EntityIntent
    {
        public RemoveEntityIntent(EntityRuntime entity) : base(entity)
        {
        }
    }



    public class LayeredStateCoordinator
    {
        public StateMachine<MotionStateType> Motion { get; }
        public StateMachine<ActionStateType> Action { get; }

        public List<EntityIntent> Intents;

        public bool LockMove { get; private set; }
        public bool LockTurn { get; private set; }
        public bool OverlayMotion { get; private set; }

        public LayeredStateCoordinator(EntityRuntime entity)
        {
            Motion = new StateMachine<MotionStateType>(entity);
            Action = new StateMachine<ActionStateType>(entity);
            Intents = new List<EntityIntent>();
        }

        public void Update(float dt)
        {
            Action.Update(dt);

            // 输出覆盖/锁定标志
            if (Action.CurrentState is IActionControl ctrl)
            {
                LockMove = ctrl.LockMove;
                LockTurn = ctrl.LockTurn;
                OverlayMotion = ctrl.OverlayMotion;
            }
            else
            {
                LockMove = LockTurn = OverlayMotion = false;
            }

            if (!OverlayMotion)
            {
                Motion.Update(dt);
            }
        }
    }
}
