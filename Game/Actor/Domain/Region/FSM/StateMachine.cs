using Server.DataBase.Entities;
using Server.Game.Contracts.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Actor.Domain.Region.FSM
{
    public class StateMachine<TStateType>
    {
        private readonly Dictionary<TStateType, StateBase<TStateType>> states = new Dictionary<TStateType, StateBase<TStateType>>();
        private StateBase<TStateType> current;

        private bool isUpdating;
        private bool hasPending;
        private TStateType pendingState;

        public EntityRuntime Entity;

        public StateBase<TStateType> CurrentState => current;
        public TStateType CurrentStateType => current != null ? current.StateType : default;

        public event Action<TStateType, TStateType> OnStateChanged;

        public StateMachine(EntityRuntime entity)
        {
            Entity = entity;
        }

        public void AddState(TStateType id, StateBase<TStateType> state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            states[id] = state;
            state.Initialize(id, this);
        }

        public bool TryGetState(TStateType id, out StateBase<TStateType> state) => states.TryGetValue(id, out state);

        public void SetInitialState(TStateType id, bool callEnter = true)
        {
            if (!states.TryGetValue(id, out var s))
            {
                Console.WriteLine($"[FSM] 找不到该状态: {id}");
                return;
            }

            current = s;
            hasPending = false;

            if (callEnter)
                current.Enter();
        }

        public bool RequestChange(TStateType to)
        {
            if (!states.ContainsKey(to)) return false;

            if (current != null && EqualityComparer<TStateType>.Default.Equals(current.StateType, to))
                return false;

            pendingState = to;
            hasPending = true;

            if (!isUpdating)
                ApplyPending();

            return true;
        }

        public bool ForceChange(TStateType to) => InternalChange(to, ignoreGuards: true, usePriority: false);

        public void Update(float dt)
        {
            if (current == null) return;
            isUpdating = true;
            current.Update(dt);
            isUpdating = false;

            ApplyPending();
        }


        private void ApplyPending()
        {
            if (!hasPending) return;
            hasPending = false;

            InternalChange(pendingState, ignoreGuards: false, usePriority: true);
        }


        private bool InternalChange(TStateType to, bool ignoreGuards, bool usePriority)
        {
            if (!states.TryGetValue(to, out var next)) return false;

            var fromState = current;
            var fromId = fromState != null ? fromState.StateType : default;

            if (fromState != null)
            {
                if (!ignoreGuards)
                {
                    if (!fromState.CanExit(to)) return false;
                    if (!next.CanEnter(fromId)) return false;

                    if (usePriority && next.Priority < fromState.Priority)
                    {
                        return false;
                    }
                }

                fromState.Exit();
            }
            if(to is MotionStateType)
            {
                Console.WriteLine($"[MotionFSM]Id: {Entity.EntityId} 状态转换 {fromState.StateType} => {next.StateType}");
            }
            else if(to is ActionStateType)
            {
                Console.WriteLine($"[ActionFSM]Id: {Entity.EntityId} 状态转换 {fromState.StateType} => {next.StateType}");
            }
            current = next;
            current.Enter();

            OnStateChanged?.Invoke(fromId, to);
            return true;
        }
    }
}
