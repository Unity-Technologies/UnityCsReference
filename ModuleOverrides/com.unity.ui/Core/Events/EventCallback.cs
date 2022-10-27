// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Experimental;


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
        public readonly long eventTypeId;
        public readonly CallbackPhase phase;
        public readonly InvokePolicy invokePolicy;

        protected EventCallbackFunctorBase(long eventTypeId, CallbackPhase phase, InvokePolicy invokePolicy)
        {
            this.eventTypeId = eventTypeId;
            this.phase = phase;
            this.invokePolicy = invokePolicy;
        }

        // For unit tests only
        internal void Invoke(EventBase evt, PropagationPhase propagationPhase)
        {
            if (eventTypeId == evt.eventTypeId && (phase == CallbackPhase.TrickleDown
                    ? propagationPhase == PropagationPhase.TrickleDown || propagationPhase == PropagationPhase.AtTarget
                    : propagationPhase == PropagationPhase.AtTarget || propagationPhase == PropagationPhase.BubbleUp))
                Invoke(evt);
        }

        public abstract void Invoke(EventBase evt);

        public abstract bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase);
    }

    internal class EventCallbackFunctor<TEventType> : EventCallbackFunctorBase
        where TEventType : EventBase<TEventType>, new()
    {
        readonly EventCallback<TEventType> m_Callback;

        public EventCallbackFunctor(EventCallback<TEventType> callback, CallbackPhase phase,
            InvokePolicy invokePolicy = InvokePolicy.Default)
            : base(EventBase<TEventType>.TypeId(), phase, invokePolicy)
        {
            m_Callback = callback;
        }

        public override void Invoke(EventBase evt)
        {
            using (new EventDebuggerLogCall(m_Callback, evt))
            {
                m_Callback(evt as TEventType);
            }
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return ((this.eventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (this.phase == phase));
        }
    }

    internal class EventCallbackFunctor<TEventType, TCallbackArgs> : EventCallbackFunctorBase
        where TEventType : EventBase<TEventType>, new()
    {
        readonly EventCallback<TEventType, TCallbackArgs> m_Callback;

        internal TCallbackArgs userArgs { get; set; }

        public EventCallbackFunctor(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs,
            CallbackPhase phase, InvokePolicy invokePolicy = InvokePolicy.Default)
            : base(EventBase<TEventType>.TypeId(), phase, invokePolicy)
        {
            this.userArgs = userArgs;
            m_Callback = callback;
        }

        public override void Invoke(EventBase evt)
        {
            using (new EventDebuggerLogCall(m_Callback, evt))
            {
                m_Callback(evt as TEventType, userArgs);
            }
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return ((this.eventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (this.phase == phase));
        }
    }
}
