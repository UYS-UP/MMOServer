using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.HFSM
{
    public abstract class HState
    {
        public readonly HStateMachine Machine;
        public readonly HState Parent;
        public HState ActiveChild;

        private readonly List<IActivity> activities = new List<IActivity>();
        public IReadOnlyList<IActivity> Activities => activities;

        protected HState(HStateMachine machine, HState parent)
        {
            this.Machine = machine;
            this.Parent = parent;
        }

        protected virtual HState GetInitialState() => null;
        protected virtual HState GetTransition() => null;

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate(float deltaTime) { }

        public void Enter()
        {
            if (Parent != null) Parent.ActiveChild = this;
            OnEnter();
            HState init = GetInitialState();
            if (init != null) init.Enter();
        }

        public void Exit()
        {
            if (ActiveChild != null) ActiveChild.Exit();
            ActiveChild = null;
            OnExit();
        }

        public void Update(float deltaTime)
        {
            var t = GetTransition();
            if (t != null)
            {
                var fromLeaf = Leaf();
                Machine.Sequencer.RequestTransition(fromLeaf, t);
                return;
            }

            ActiveChild?.Update(deltaTime);
            OnUpdate(deltaTime);
        }

        public HState Leaf()
        {
            HState s = this;
            while (s.ActiveChild != null)
            {
                s = s.ActiveChild;
            }

            return s;
        }

        public IEnumerable<HState> PathToRoot()
        {
            for (var s = this; s.Parent != null; s = s.Parent)
            {
                yield return s;
            }
        }

        public T AsTo<T>() where T : HState
        {
            return this as T;
        }
    }
}
