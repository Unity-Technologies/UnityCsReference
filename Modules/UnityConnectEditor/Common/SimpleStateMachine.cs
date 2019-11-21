// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A very lightweight state machine which uses generics as events
    /// (unless you have specific needs, enums are recommended) and states
    /// (either instance of the base state or your own extensions).
    /// </summary>
    internal class SimpleStateMachine<T>
    {
        HashSet<T> m_Events = new HashSet<T>();
        Dictionary<string, State> m_StateByName = new Dictionary<string, State>();
        bool m_Initialized;

        /// <summary>
        /// The current state
        /// </summary>
        public State currentState { get; private set; }

        /// <summary>
        /// Initializes the state machine.
        /// This should be called after instantiating the state machine and instantiating the initial state.
        /// Since the state machine needs an initial state instance and the state needs a state machine instance, the
        /// proper order is:
        /// 1- instantiate state machine
        /// 2- instantiate initial state
        /// 3- initialize state machine with initial state
        /// A state machine cannot be initialized multiple times. Further attempts will be ignored.
        /// </summary>
        /// <param name="initialState"></param>
        public void Initialize(State initialState)
        {
            if (!m_Initialized)
            {
                if (!StateExists(initialState))
                {
                    AddState(initialState);
                }
                m_Initialized = true;
                currentState = initialState;
                currentState.EnterState();
            }
        }

        /// <summary>
        /// Clears the current state
        /// Allows for a full redraw on the state machine in the activate action without reallocating memory
        /// </summary>
        public void ClearCurrentState()
        {
            if (m_Initialized)
            {
                currentState = null;
                m_Initialized = false;
            }
        }

        /// <summary>
        /// Adds a new event to the state machine
        /// Don't forget that events are sent to states. So when adding an event to the state machine,
        /// it generally means that this event should also be configured on some states by using
        /// mySimpleStateMachineState.ModifyActionForEvent(myEvent, myHandler);
        /// </summary>
        /// <param name="simpleStateMachineEvent">the event</param>
        public void AddEvent(T simpleStateMachineEvent)
        {
            m_Events.Add(simpleStateMachineEvent);
        }

        public bool EventExists(T simpleStateMachineEvent)
        {
            foreach (T knownEvent in m_Events)
            {
                if (knownEvent.Equals(simpleStateMachineEvent))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets a copy of the state machine events.
        /// Copy means you cannot alter the state machine by altering this list.
        /// It's a shallow copy though, be careful what you do with the list entries
        /// </summary>
        /// <returns>a copy of the state machine events</returns>
        public List<T> GetEvents()
        {
            return new List<T>(m_Events);
        }

        /// <summary>
        /// Adds a new state to the state machine.
        /// This state can be generic (straight instance of SimpleStateMachineState) or an extended version if you need
        /// to override the EnterState() method of the SimpleStateMachineState.
        /// A state should react to events. Configure the state by using ModifyActionForEvent
        /// </summary>
        /// <param name="state"></param>
        public void AddState(State state)
        {
            m_StateByName.Add(state.name, state);
        }

        /// <summary>
        /// This method is exposed to allow states to return a new state when an event action is completed successfully
        /// and a transition is made to another state.
        /// </summary>
        /// <param name="stateName">The name of the state to find</param>
        /// <returns>The state; or null if none found</returns>
        public State GetStateByName(string stateName)
        {
            return m_StateByName.ContainsKey(stateName) ? m_StateByName[stateName] : null;
        }

        public bool StateExists(State state)
        {
            return m_StateByName.ContainsKey(state.name);
        }

        /// <summary>
        /// Gets a shallow copy of the state machine states.
        /// Copy means you cannot alter the state machine by altering this list
        /// (but changing the configuration of a state will modify the state machine: shallow copy)
        /// </summary>
        /// <returns>a copy of the state machine states</returns>
        public List<State> GetStates()
        {
            return new List<State>(m_StateByName.Values);
        }

        /// <summary>
        /// Call this method when an event occurs on the state machine.
        /// This will often result in a state change, but it's not mandatory.
        /// Nothing will happen if the event does not exist within the state machine events'
        /// </summary>
        /// <param name="simpleStateMachineEvent">The event to process</param>
        public void ProcessEvent(T simpleStateMachineEvent)
        {
            if (m_Initialized && EventExists(simpleStateMachineEvent))
            {
                var previousState = currentState;
                currentState = currentState.GetActionForEvent(simpleStateMachineEvent).Invoke(simpleStateMachineEvent);
                if (currentState != previousState)
                {
                    if (currentState != null)
                    {
                        currentState.EnterState();
                    }
                    else
                    {
                        Debug.LogError("SimpleStateMachine.ProcessEvent: " + L10n.Tr("Attempting to change to an undefined state. Contact Unity Support."));
                    }
                }
            }
        }

        /// <summary>
        /// Base state for a simple state machine. It can be used as-is or extended to gain additional functionality
        /// </summary>
        internal class State
        {
            List<ActionForEvent> m_ActionForEvent = new List<ActionForEvent>();

            /// <summary>
            /// Access to the state machine. Mostly to GetStateByName when transitioning from one state to another
            /// </summary>
            protected SimpleStateMachine<T> stateMachine { get; }
            public string name { get; }

            public State(string name, SimpleStateMachine<T> simpleStateMachine)
            {
                this.name = name;
                stateMachine = simpleStateMachine;
            }

            public virtual bool ActionExistsForEvent(T simpleStateMachineEvent)
            {
                foreach (var actionForEvent in m_ActionForEvent)
                {
                    if (actionForEvent.simpleStateMachineEvent.Equals(simpleStateMachineEvent))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Updates or Inserts a new action to execute when an event occurs when the state machine is at this state.
            /// </summary>
            /// <param name="simpleStateMachineEvent">The event</param>
            /// <param name="transitionAction">The action to accomplish when the specified event occurs</param>
            public virtual void ModifyActionForEvent(T simpleStateMachineEvent, Func<T, State> transitionAction)
            {
                foreach (var actionForEvent in m_ActionForEvent)
                {
                    if (actionForEvent.simpleStateMachineEvent.Equals(simpleStateMachineEvent))
                    {
                        actionForEvent.action = transitionAction;
                        return;
                    }
                }
                m_ActionForEvent.Add(new ActionForEvent(simpleStateMachineEvent, transitionAction));
            }

            /// <summary>
            /// This method is called by the state machine when an event occurs. By default,
            /// if the state machine does not find an action to execute, the state will remain unchanged
            /// and the event will be ignored.
            /// </summary>
            /// <param name="simpleStateMachineEvent">the event we search an action for</param>
            /// <returns>an action to execute; can be DoNothing if ModifyActionForEvent wasn't done for this event</returns>
            public virtual Func<T, State> GetActionForEvent(T simpleStateMachineEvent)
            {
                foreach (var actionForEvent in m_ActionForEvent)
                {
                    if (actionForEvent.simpleStateMachineEvent.Equals(simpleStateMachineEvent))
                    {
                        return actionForEvent.action;
                    }
                }

                return DoNothing;
            }

            /// <summary>
            /// Default action taken when no handler is found for an event.
            /// Will simply return the current state and thus, does nothing
            /// </summary>
            /// <returns></returns>
            State DoNothing(T simpleStateMachineEvent)
            {
                return this;
            }

            /// <summary>
            /// This method will be called by the state machine when the a state becomes active.
            /// It allows to do a common operation on the current state without having all the other states repeat this
            /// code within their transition actions.
            /// </summary>
            public virtual void EnterState() {}

            class ActionForEvent
            {
                internal T simpleStateMachineEvent { get; }
                internal Func<T, State> action { get; set; }

                internal ActionForEvent(T simpleStateMachineEvent, Func<T, State> action)
                {
                    this.simpleStateMachineEvent = simpleStateMachineEvent;
                    this.action = action;
                }
            }
        }
    }
}
