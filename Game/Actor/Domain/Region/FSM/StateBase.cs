using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM
{
    public class StateBase<TStateType>
    {
        public TStateType StateType { get; private set; }
        protected StateMachine<TStateType> FSM { get; private set; }

        public virtual int Priority => 0;

        public void Initialize(TStateType stateType, StateMachine<TStateType> fsm)
        {
            StateType = stateType;
            FSM = fsm;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public virtual bool CanEnter(TStateType from) => true;

        public virtual bool CanExit(TStateType to) => true;

        public virtual void Enter() { }
        public virtual void Update(float dt) { }
        public virtual void Exit() { }
    }
}
