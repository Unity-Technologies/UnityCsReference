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
        public abstract void Invoke(EventBase evt);

        public abstract bool IsEquivalentTo(long eventTypeId, Delegate callback, Capture useCapture);
    }

    internal class EventCallbackFunctor<TEventType> : EventCallbackFunctorBase where TEventType : EventBase, new()
    {
        EventCallback<TEventType> m_Callback;
        bool m_UseCapture;
        long m_EventTypeId;

        public EventCallbackFunctor(EventCallback<TEventType> callback, Capture useCapture)
        {
            m_Callback = callback;
            m_UseCapture = useCapture == Capture.Capture;
            m_EventTypeId = new TEventType().GetEventTypeId();
        }

        public override void Invoke(EventBase evt)
        {
            if (evt == null)
                throw new ArgumentNullException();

            if (evt.GetEventTypeId() != m_EventTypeId)
                return;

            if (evt.propagationPhase == PropagationPhase.Capture && !m_UseCapture)
                return;

            if (evt.propagationPhase == PropagationPhase.BubbleUp && m_UseCapture)
                return;

            m_Callback(evt as TEventType);
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, Capture useCapture)
        {
            return ((m_EventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (m_UseCapture == (useCapture == Capture.Capture)));
        }
    }

    internal class EventCallbackFunctor<TEventType, TCallbackArgs> : EventCallbackFunctorBase where TEventType : EventBase, new()
    {
        EventCallback<TEventType, TCallbackArgs> m_Callback;
        TCallbackArgs m_UserArgs;
        bool m_UseCapture;
        long m_EventTypeId;

        public EventCallbackFunctor(EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, Capture useCapture)
        {
            m_Callback = callback;
            m_UserArgs = userArgs;
            m_UseCapture = useCapture == Capture.Capture;
            m_EventTypeId = new TEventType().GetEventTypeId();
        }

        public override void Invoke(EventBase evt)
        {
            if (evt == null)
                throw new ArgumentNullException();

            if (evt.GetEventTypeId() != m_EventTypeId)
                return;

            if (evt.propagationPhase == PropagationPhase.Capture && !m_UseCapture)
                return;

            if (evt.propagationPhase == PropagationPhase.BubbleUp && m_UseCapture)
                return;

            m_Callback(evt as TEventType, m_UserArgs);
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback, Capture useCapture)
        {
            return ((m_EventTypeId == eventTypeId) && ((Delegate)m_Callback) == callback && (m_UseCapture == (useCapture == Capture.Capture)));
        }
    }
}
