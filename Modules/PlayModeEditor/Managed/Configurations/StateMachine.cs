// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// A simple state machine that supports async transitions between states.
    /// </summary>
    /// <typeparam name="T">The enum type of the states.</typeparam>
    [Serializable]
    class StateMachine<T> where T : Enum
    {
        [Serializable]
        private struct TransitionDefinition
        {
            public T From;
            public T To;
        }

        private T m_DefaultState;
        private T m_CurrentState;
        private T m_NextState;

        // This is a List<Transition> only because it can be serialized and therefore it
        // can persit between domain reloads.
        private List<TransitionDefinition> m_Transitions = new();

        /// <summary>
        /// The current state of the state machine.
        /// </summary>
        public T CurrentState => m_CurrentState;

        /// <summary>
        /// The state the state machine is transitioning to.
        /// </summary>
        public T NextState => m_NextState;

        /// <summary>
        /// An event that is raised when the state machine changes state.
        /// </summary>
        public event Action<T> StateChanged;

        /// <summary>
        /// Creates a new state machine.
        /// </summary>
        /// <param name="defaultState">The initial state of the state machine.</param>
        public StateMachine(T defaultState)
        {
            m_DefaultState = defaultState;
            Reset();
        }

        private void Reset()
        {
            m_CurrentState = m_DefaultState;
            m_NextState = m_DefaultState;
        }

        /// <summary>
        /// Defines a transition between two states.
        /// </summary>
        /// <param name="from">The initial state of the transition.</param>
        /// <param name="to">The final state of the transition.</param>
        public void DefineTransition(T from, T to)
        {
            var transition = new TransitionDefinition { From = from, To = to };

            if (m_Transitions.Contains(transition))
                return;

            m_Transitions.Add(transition);
        }

        /// <summary>
        /// Check if a transition is valid.
        /// A transition is valid if it has been defined previously for state machine.
        /// </summary>
        /// <param name="from">The initial strate of the transition.</param>
        /// <param name="to">The final state of the transition.</param>
        /// <returns>Returns true if the transition is valid for the state machine.</returns>
        public bool IsValidTransition(T from, T to)
        {
            return m_Transitions.Contains(new TransitionDefinition { From = from, To = to });
        }

        /// <summary>
        /// Check if the state machine is transitioning between states.
        /// </summary>
        /// <returns>Returns true if the state machine is transitioning between states.</returns>
        public bool IsTransitioning()
        {
            return !EqualityComparer<T>.Default.Equals(m_CurrentState, m_NextState);
        }

        /// <summary>
        /// Synchronously transition between two states. This method will block until the transition is complete.
        /// </summary>
        /// <param name="to">The state to transition to.</param>
        /// <exception cref="InvalidOperationException">Throws if the transition is not valid.</exception>
        public void Transition(T to, Action<T, T> transitionAction)
        {
            StartTransition(to);

            try
            {
                transitionAction(CurrentState, to);
            }
            catch (Exception)
            {
                AbortTransition();
                return;
            }

            CompleteTransition();
        }

        /// <summary>
        /// Asynchronously transition between two states.
        /// </summary>
        /// <param name="to">The state to transition to</param>
        /// <param name="transitionAction">An action to execute during the transition.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the transition.</param>
        /// <exception cref="InvalidOperationException">>Throws if the transition is not valid.</exception>
        public async Task TransitionAsync(T to, Func<T, T, CancellationToken, Task> transitionAction, CancellationToken cancellationToken)
        {
            StartTransition(to);

            Task task;

            try
            {
                task = transitionAction(CurrentState, to, cancellationToken);
                await task;
            }
            catch (TaskCanceledException)
            {
                AbortTransition();
                return;
            }
            catch (Exception e)
            {
                AbortTransition();
                Debug.LogException(e);
                throw;
            }

            if (task.IsCompletedSuccessfully)
                CompleteTransition();
            else
                AbortTransition();
        }

        /// <summary>
        /// Start a transition between two states.
        /// </summary>
        /// <param name="to">The state to transition to.</param>
        /// <exception cref="InvalidOperationException">Throws if the transition is not valid.</exception>
        private void StartTransition(T to)
        {
            if (!IsValidTransition(CurrentState, to))
                throw new InvalidOperationException($"Invalid transition from {CurrentState} to {to}");

            m_NextState = to;
        }

        /// <summary>
        /// Complete the current ongoing transition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if the state machine is not transitioning.</exception>
        private void CompleteTransition()
        {
            if (!IsTransitioning())
                throw new InvalidOperationException($"Cannot complete transition because no transition is in progress");

            m_CurrentState = m_NextState;
            StateChanged?.Invoke(m_CurrentState);
        }

        /// <summary>
        /// Abort the current ongoing transition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws if the state machine is not transitioning.</exception>
        private void AbortTransition()
        {
            if (!IsTransitioning())
                throw new InvalidOperationException($"Cannot abort transition because no transition is in progress");

            m_NextState = m_CurrentState;
        }
    }
}
