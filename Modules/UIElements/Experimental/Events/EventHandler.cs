// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public interface IEventHandler
    {
        void SendEvent(EventBase e);

        void HandleEvent(EventBase evt);

        bool HasTrickleDownHandlers();

        bool HasBubbleUpHandlers();

        [Obsolete("Use HasTrickleDownHandlers instead of HasCaptureHandlers.")]
        bool HasCaptureHandlers();
        [Obsolete("Use HasBubbleUpHandlers instead of HasBubbleHandlers.")]
        bool HasBubbleHandlers();
    }

    public abstract class CallbackEventHandler : IEventHandler
    {
        EventCallbackRegistry m_CallbackRegistry;

        [Obsolete("Use TrickleDown instead of Capture.")]
        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture) where TEventType : EventBase<TEventType>, new()
        {
            TrickleDown td = (TrickleDown)useCapture;
            RegisterCallback<TEventType>(callback, td);
        }

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType>(callback, useTrickleDown);
        }

        [Obsolete("Use TrickleDown instead of Capture.")]
        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, Capture useCapture) where TEventType : EventBase<TEventType>, new()
        {
            TrickleDown td = (TrickleDown)useCapture;
            RegisterCallback<TEventType, TUserArgsType>(callback, userArgs, td);
        }

        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType, TUserArgsType>(callback, userArgs, useTrickleDown);
        }

        [Obsolete("Use TrickleDown instead of Capture.")]
        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, Capture useCapture) where TEventType : EventBase<TEventType>, new()
        {
            TrickleDown td = (TrickleDown)useCapture;
            UnregisterCallback<TEventType>(callback, td);
        }

        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }
        }

        [Obsolete("Use TrickleDown instead of Capture.")]
        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, Capture useCapture) where TEventType : EventBase<TEventType>, new()
        {
            TrickleDown td = (TrickleDown)useCapture;
            UnregisterCallback<TEventType, TUserArgsType>(callback, td);
        }

        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }
        }

        internal bool TryGetUserArgs<TEventType, TCallbackArgs>(EventCallback<TEventType, TCallbackArgs> callback, TrickleDown useTrickleDown, out TCallbackArgs userData) where TEventType : EventBase<TEventType>, new()
        {
            userData = default(TCallbackArgs);

            if (m_CallbackRegistry != null)
            {
                return m_CallbackRegistry.TryGetUserArgs(callback, useTrickleDown, out userData);
            }

            return false;
        }

        public abstract void SendEvent(EventBase e);

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

        public bool HasTrickleDownHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasTrickleDownHandlers();
        }

        public bool HasBubbleUpHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasBubbleHandlers();
        }

        [Obsolete("Use HasTrickleDownHandlers instead of HasCaptureHandlers.")]
        public bool HasCaptureHandlers()
        {
            return HasTrickleDownHandlers();
        }

        [Obsolete("Use HasBubbleUpHandlers instead of HasBubbleHandlers.")]
        public bool HasBubbleHandlers()
        {
            return HasBubbleUpHandlers();
        }

        protected internal virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        protected internal virtual void ExecuteDefaultAction(EventBase evt) {}
    }
}
