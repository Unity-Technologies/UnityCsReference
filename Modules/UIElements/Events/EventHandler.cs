// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public interface IEventHandler
    {
        void HandleEvent(EventBase evt);

        bool HasCaptureHandlers();

        bool HasBubbleHandlers();
    }

    public abstract class CallbackEventHandler : IEventHandler
    {
        EventCallbackRegistry m_CallbackRegistry;

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType>(callback, useCapture);
        }

        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType, TUserArgsType>(callback, userArgs, useCapture);
        }

        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useCapture);
            }
        }

        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useCapture);
            }
        }

        public virtual void HandleEvent(EventBase evt)
        {
            if (evt.propagationPhase != PropagationPhase.DefaultAction)
            {
                if (!evt.isPropagationStopped)
                {
                    if (m_CallbackRegistry != null)
                    {
                        m_CallbackRegistry.InvokeCallbacks(evt);
                    }
                }

                if (evt.propagationPhase == PropagationPhase.AtTarget && !evt.isDefaultPrevented)
                {
                    ExecuteDefaultActionAtTarget(evt);
                }
            }
            else if (!evt.isDefaultPrevented)
            {
                ExecuteDefaultAction(evt);
            }
        }

        public bool HasCaptureHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasCaptureHandlers();
        }

        public bool HasBubbleHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasBubbleHandlers();
        }

        protected internal virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        protected internal virtual void ExecuteDefaultAction(EventBase evt) {}
    }
}
