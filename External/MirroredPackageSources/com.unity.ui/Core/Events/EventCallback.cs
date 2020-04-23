using System;

namespace UnityEngine.UIElements
{
    public delegate void EventCallback<in TEventType>(TEventType evt);

    public delegate void EventCallback<in TEventType, in TCallbackArgs>(TEventType evt, TCallbackArgs userArgs);

    internal abstract class EventCallbackFunctorBase
    {
        public CallbackPhase phase { get; private set; }

        protected EventCallbackFunctorBase(CallbackPhase phase)
        {
            this.phase = phase;
        }

        public abstract void Invoke(EventBase evt);

        public abstract bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase);

        protected bool PhaseMatches(EventBase evt)
        {
            switch (phase)
            {
                case CallbackPhase.TrickleDownAndTarget:
                    if (evt.propagationPhase != PropagationPhase.TrickleDown && evt.propagationPhase != PropagationPhase.AtTarget)
                        return false;
                    break;

                case CallbackPhase.TargetAndBubbleUp:
                    if (evt.propagationPhase != PropagationPhase.AtTarget && evt.propagationPhase != PropagationPhase.BubbleUp)
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

        public EventCallbackFunctor(EventCallback<TEventType> callback, CallbackPhase phase) : base(phase)
        {
            m_Callback = callback;
            m_EventTypeId = EventBase<TEventType>.TypeId();
        }

        public override void Invoke(EventBase evt)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (evt.eventTypeId != m_EventTypeId)
                return;

            if (PhaseMatches(evt))
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

        public EventCallbackFunctor(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, CallbackPhase phase) : base(phase)
        {
            this.userArgs = userArgs;
            m_Callback = callback;
            m_EventTypeId = EventBase<TEventType>.TypeId();
        }

        public override void Invoke(EventBase evt)
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (evt.eventTypeId != m_EventTypeId)
                return;

            if (PhaseMatches(evt))
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
