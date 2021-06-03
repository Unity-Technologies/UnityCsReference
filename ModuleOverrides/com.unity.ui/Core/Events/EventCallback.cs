// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines the structure of a callback that can be registered onto an element for an event type
    /// </summary>
    /// <param name="evt">The event instance</param>
    /// <typeparam name="TEventType">The type of event to register the callback for</typeparam>
    public delegate void EventCallback<in TEventType>(TEventType evt);

    /// <summary>
    /// Defines the structure of a callback that can be registered onto an element for an event type,
    /// along with a custom user defined argument.
    /// </summary>
    /// <param name="evt">The event instance.</param>
    /// <param name="userArgs">The user argument instance.</param>
    /// <typeparam name="TEventType">The type of event registered for the callback.</typeparam>
    /// <typeparam name="TCallbackArgs">The type of the user argument.</typeparam>
    public delegate void EventCallback<in TEventType, in TCallbackArgs>(TEventType evt, TCallbackArgs userArgs);

    internal abstract class EventCallbackFunctorBase
    {
        public CallbackPhase phase { get; }
        public InvokePolicy invokePolicy { get; }

        protected EventCallbackFunctorBase(CallbackPhase phase, InvokePolicy invokePolicy)
        {
            this.phase = phase;
            this.invokePolicy = invokePolicy;
        }

        public abstract void Invoke(EventBase evt, PropagationPhase propagationPhase);

        public abstract bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase);

        protected bool PhaseMatches(PropagationPhase propagationPhase)
        {
            switch (phase)
            {
                case CallbackPhase.TrickleDownAndTarget:
                    if (propagationPhase != PropagationPhase.TrickleDown && propagationPhase != PropagationPhase.AtTarget)
                        return false;
                    break;

                case CallbackPhase.TargetAndBubbleUp:
                    if (propagationPhase != PropagationPhase.AtTarget && propagationPhase != PropagationPhase.BubbleUp)
                        return false;
                    break;
            }

            return true;
        }
    }

    internal class EventCallbackFunctor<TEventType> : EventCallbackFunctorBase where TEventType : EventBase<TEventType>, new()
    {
        readonly EventCallback<TEventType> m_Callback;
        readonly long m_EventTypeId;

        public EventCallbackFunctor(EventCallback<TEventType> callback, CallbackPhase phase, InvokePolicy invokePolicy = default) : base(phase, invokePolicy)
        {
            m_Callback = callback;
            m_EventTypeId = EventBase<TEventType>.TypeId();
        }

        public override void Invoke(EventBase evt, PropagationPhase propagationPhase)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (evt.eventTypeId != m_EventTypeId)
                return;

            if (PhaseMatches(propagationPhase))
            {
                using (new EventDebuggerLogCall(m_Callback, evt))
                {
                    m_Callback(evt as TEventType);
                }
            }
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return ((m_EventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (this.phase == phase));
        }
    }

    internal class EventCallbackFunctor<TEventType, TCallbackArgs> : EventCallbackFunctorBase where TEventType : EventBase<TEventType>, new()
    {
        readonly EventCallback<TEventType, TCallbackArgs> m_Callback;
        readonly long m_EventTypeId;

        internal TCallbackArgs userArgs { get; set; }

        public EventCallbackFunctor(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, CallbackPhase phase, InvokePolicy invokePolicy) : base(phase, invokePolicy)
        {
            this.userArgs = userArgs;
            m_Callback = callback;
            m_EventTypeId = EventBase<TEventType>.TypeId();
        }

        public override void Invoke(EventBase evt, PropagationPhase propagationPhase)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (evt.eventTypeId != m_EventTypeId)
                return;

            if (PhaseMatches(propagationPhase))
            {
                using (new EventDebuggerLogCall(m_Callback, evt))
                {
                    m_Callback(evt as TEventType, userArgs);
                }
            }
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return ((m_EventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (this.phase == phase));
        }
    }
}
