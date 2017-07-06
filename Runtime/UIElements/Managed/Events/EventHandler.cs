// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    // TODO this interface is going to be refactored in a later iteration
    public interface IEventHandler
    {
        IPanel panel { get; }

        void OnLostCapture();

        void OnLostKeyboardFocus();

        void HandleEvent(EventBase evt);
    }

    public static class EventHandlerExtensions
    {
        public static void TakeCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.TakeCapture(handler);
            }
        }

        public static bool HasCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                return handler.panel.dispatcher.capture == handler;
            }
            else
            {
                return false;
            }
        }

        public static void ReleaseCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.ReleaseCapture(handler);
            }
        }

        public static void RemoveCapture(this IEventHandler handler)
        {
            if (handler.panel != null)
            {
                handler.panel.dispatcher.RemoveCapture();
            }
        }

        public static ScheduleBuilder Schedule(this IEventHandler handler, Action<TimerState> timerUpdateEvent)
        {
            if (handler.panel == null || handler.panel.scheduler == null)
            {
                Debug.LogError("Cannot schedule an event without a valid panel");
                return new ScheduleBuilder();
            }

            return handler.panel.scheduler.Schedule(timerUpdateEvent, handler);
        }

        public static void Unschedule(this IEventHandler handler, Action<TimerState> timerUpdateEvent)
        {
            if (handler.panel == null || handler.panel.scheduler == null)
            {
                Debug.LogError("Cannot unschedule an event without a valid panel");
                return;
            }

            handler.panel.scheduler.Unschedule(timerUpdateEvent);
        }
    }

    public abstract class CallbackEventHandler : IEventHandler
    {
        EventCallbackRegistry m_CallbackRegistry;

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback(callback, useCapture);
        }

        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback(callback, userArgs, useCapture);
        }

        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useCapture);
            }
        }

        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, Capture useCapture = Capture.NoCapture) where TEventType : EventBase, new()
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
                if (m_CallbackRegistry != null)
                {
                    m_CallbackRegistry.InvokeCallbacks(evt);
                }
            }
            else
            {
                ExecuteDefaultAction(evt);
            }
        }

        protected internal virtual void ExecuteDefaultAction(EventBase evt) {}

        public virtual void OnLostCapture()
        {
        }

        public virtual void OnLostKeyboardFocus()
        {
        }

        public virtual IPanel panel
        {
            get { return null; }
        }
    }
}
