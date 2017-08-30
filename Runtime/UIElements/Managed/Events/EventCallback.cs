// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
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
                case CallbackPhase.CaptureAndTarget:
                    if (evt.propagationPhase != PropagationPhase.Capture && evt.propagationPhase != PropagationPhase.AtTarget)
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
        EventCallback<TEventType> m_Callback;
        long m_EventTypeId;

        public EventCallbackFunctor(EventCallback<TEventType> callback, CallbackPhase phase) : base(phase)
        {
            m_Callback = callback;
            m_EventTypeId = EventBase<TEventType>.TypeId();
        }

        public override void Invoke(EventBase evt)
        {
            if (evt == null)
                throw new ArgumentNullException();

            if (evt.GetEventTypeId() != m_EventTypeId)
                return;

            if (PhaseMatches(evt))
            {
                m_Callback(evt as TEventType);
            }
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return ((m_EventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (this.phase == phase));
        }
    }

    internal class EventCallbackFunctor<TEventType, TCallbackArgs> : EventCallbackFunctorBase where TEventType : EventBase<TEventType>, new()
    {
        EventCallback<TEventType, TCallbackArgs> m_Callback;
        TCallbackArgs m_UserArgs;
        long m_EventTypeId;

        public EventCallbackFunctor(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, CallbackPhase phase) : base(phase)
        {
            m_Callback = callback;
            m_UserArgs = userArgs;
            m_EventTypeId = EventBase<TEventType>.TypeId();
        }

        public override void Invoke(EventBase evt)
        {
            if (evt == null)
                throw new ArgumentNullException();

            if (evt.GetEventTypeId() != m_EventTypeId)
                return;

            if (PhaseMatches(evt))
            {
                m_Callback(evt as TEventType, m_UserArgs);
            }
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return ((m_EventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (this.phase == phase));
        }
    }
}
