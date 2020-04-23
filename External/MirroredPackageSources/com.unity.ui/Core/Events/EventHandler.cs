namespace UnityEngine.UIElements
{
    public interface IEventHandler
    {
        void SendEvent(EventBase e);

        void HandleEvent(EventBase evt);

        bool HasTrickleDownHandlers();

        bool HasBubbleUpHandlers();
    }

    public abstract class CallbackEventHandler : IEventHandler
    {
        EventCallbackRegistry m_CallbackRegistry;

        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType>(callback, useTrickleDown);
            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);
        }

        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback<TEventType, TUserArgsType>(callback, userArgs, useTrickleDown);
            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);
        }

        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }

            GlobalCallbackRegistry.UnregisterListeners<TEventType>(this, callback);
        }

        public void UnregisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }

            GlobalCallbackRegistry.UnregisterListeners<TEventType>(this, callback);
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

        internal void HandleEventAtTargetPhase(EventBase evt)
        {
            evt.currentTarget = evt.target;
            evt.propagationPhase = PropagationPhase.AtTarget;
            HandleEvent(evt);
            evt.propagationPhase = PropagationPhase.DefaultActionAtTarget;
            HandleEvent(evt);
        }

        public virtual void HandleEvent(EventBase evt)
        {
            if (evt == null)
                return;

            switch (evt.propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                case PropagationPhase.AtTarget:
                case PropagationPhase.BubbleUp:
                {
                    if (!evt.isPropagationStopped)
                    {
                        m_CallbackRegistry?.InvokeCallbacks(evt);
                    }
                    break;
                }

                case PropagationPhase.DefaultActionAtTarget:
                {
                    if (!evt.isDefaultPrevented)
                    {
                        using (new EventDebuggerLogExecuteDefaultAction(evt))
                        {
                            ExecuteDefaultActionAtTarget(evt);
                        }
                    }
                    break;
                }

                case PropagationPhase.DefaultAction:
                {
                    if (!evt.isDefaultPrevented)
                    {
                        using (new EventDebuggerLogExecuteDefaultAction(evt))
                        {
                            ExecuteDefaultAction(evt);
                        }
                    }
                    break;
                }
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

        protected virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        protected virtual void ExecuteDefaultAction(EventBase evt) {}
    }
}
