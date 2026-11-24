// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View.Internals
{
    class MoveBehaviourStateMachine
    {
        public enum State
        {
            Inactive = 0,
            Attached,
            Detached
        }

        public State state { get; set; }

        public TimeRange validRange => m_LastResult.validRange;
        public bool isValid => m_LastResult.isValid;
        public bool needsPreview => m_LastResult.needsPreview;

        public bool isAttached => state == State.Attached;
        public bool isDetached => state == State.Detached;
        public bool isInactive => state == State.Inactive;
        public bool isActive => state != State.Inactive;

        MoveManipulationResult m_LastResult;

        public MoveBehaviourStateMachine() : this(State.Inactive) { }

        public MoveBehaviourStateMachine(State state, TimeRange validRange = default, bool isValid = false, bool needsPreview = false)
        {
            this.state = state;
            m_LastResult = new MoveManipulationResult(isValid, needsPreview, validRange);
        }

        public void Start(ISequenceViewModel viewmodel, IManipulationHandler handler, MoveBehaviour behaviour, ManipulationContext context)
        {
            if (state != State.Inactive)
                throw new InvalidOperationException($"Cannot {nameof(Start)} when state is {state}");

            m_LastResult = behaviour.BeginManipulation(viewmodel, handler, context);
            state = State.Attached;
        }

        public void Move(MoveBehaviour behaviour, DiscreteTime atTime)
        {
            if (state != State.Attached)
                throw new InvalidOperationException($"Cannot {nameof(Move)} when state is {state}");

            m_LastResult = DoInsertion(behaviour, atTime);
            state = State.Attached;
        }

        public void Attach(MoveBehaviour behaviour, Track newTarget, DiscreteTime atTime)
        {
            if (state != State.Detached)
                throw new InvalidOperationException($"Cannot {nameof(Attach)} when state is {state}");

            bool canAttach = behaviour.ChangeManipulatedTrack(newTarget);

            if (!canAttach)
            {
                m_LastResult = new MoveManipulationResult(isValid: false, needsPreview: true);
                return;
            }

            m_LastResult = DoInsertion(behaviour, atTime);
            state = State.Attached;
        }

        public void Detach(MoveBehaviour behaviour)
        {
            if (state != State.Attached)
                throw new InvalidOperationException($"Cannot {nameof(Detach)} when state is {state}");

            behaviour.RevertInsertManipulation();

            state = State.Detached;
            m_LastResult = new MoveManipulationResult(isValid: false, needsPreview: true);
        }

        public void Commit(MoveBehaviour behaviour)
        {
            if (state == State.Inactive)
                throw new InvalidOperationException($"Cannot {nameof(Commit)} when state is {nameof(State.Inactive)}");

            if (isValid)
                behaviour.CommitManipulation();
            else
                behaviour.CancelManipulation();

            state = State.Inactive;
            m_LastResult = default;
        }

        public void Cancel(MoveBehaviour behaviour)
        {
            if (state == State.Inactive)
                throw new InvalidOperationException($"Cannot {nameof(Cancel)} when state is {nameof(State.Inactive)}");

            behaviour.CancelManipulation();

            state = State.Inactive;
            m_LastResult = default;
        }

        static MoveManipulationResult DoInsertion(MoveBehaviour behaviour, DiscreteTime atTime)
        {
            MoveManipulationResult result = behaviour.DoInsertManipulation(atTime);
            if (!result.isValid)
                behaviour.RevertInsertManipulation();
            return result;
        }
    }
}
