// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for class capable of handling events.
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Sends an event to the event handler.
        /// </summary>
        /// <param name="e">The event to send.</param>
        void SendEvent(EventBase e);

        /// <summary>
        /// Handle an event.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        void HandleEvent(EventBase evt);

        /// <summary>
        /// Returns true if event handlers, for the event propagation TrickleDown phase, are attached to this object.
        /// </summary>
        /// <returns>True if the object already has event handlers for the TrickleDown phase.</returns>
        bool HasTrickleDownHandlers();

        /// <summary>
        /// Returns true if event handlers for the event propagation BubbleUp phase, have been attached on this object.
        /// </summary>
        /// <returns>True if object has event handlers for the BubbleUp phase.</returns>
        bool HasBubbleUpHandlers();
    }

    /// <summary>
    /// Interface for classes capable of having callbacks to handle events.
    /// </summary>
    public abstract class CallbackEventHandler : IEventHandler
    {
        EventCallbackRegistry m_CallbackRegistry;

        /// <summary>
        /// Adds an event handler to the instance. If the event handler has already been registered for the same phase (either TrickleDown or BubbleUp) then this method has no effect.
        /// </summary>
        /// <param name="callback">The event handler to add.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass TrickleDown.TrickleDown to call this callback during the TrickleDown phase.</param>
        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback(callback, useTrickleDown, default);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);
        }

        /// <summary>
        /// Adds an event handler to the instance. If the event handler has already been registered for the same phase (either TrickleDown or BubbleUp) then this method has no effect.
        /// </summary>
        /// <param name="callback">The event handler to add.</param>
        /// <param name="userArgs">Data to pass to the callback.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass TrickleDown.TrickleDown to call this callback during the TrickleDown phase.</param>
        public void RegisterCallback<TEventType, TUserArgsType>(EventCallback<TEventType, TUserArgsType> callback, TUserArgsType userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback(callback, userArgs, useTrickleDown, default);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);
        }

        internal void RegisterCallback<TEventType>(EventCallback<TEventType> callback, InvokePolicy invokePolicy, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry == null)
            {
                m_CallbackRegistry = new EventCallbackRegistry();
            }

            m_CallbackRegistry.RegisterCallback(callback, useTrickleDown, invokePolicy);

            GlobalCallbackRegistry.RegisterListeners<TEventType>(this, callback, useTrickleDown);
        }

        /// <summary>
        /// Remove callback from the instance.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        /// <param name="useTrickleDown">Set this parameter to true to remove the callback from the TrickleDown phase. Set this parameter to false to remove the callback from the BubbleUp phase.</param>
        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.UnregisterCallback(callback, useTrickleDown);
            }

            GlobalCallbackRegistry.UnregisterListeners<TEventType>(this, callback);
        }

        /// <summary>
        /// Remove callback from the instance.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        /// <param name="useTrickleDown">Set this parameter to true to remove the callback from the TrickleDown phase. Set this parameter to false to remove the callback from the BubbleUp phase.</param>
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

        /// <summary>
        /// Sends an event to the event handler.
        /// </summary>
        /// <param name="e">The event to send.</param>
        public abstract void SendEvent(EventBase e);

        internal abstract void SendEvent(EventBase e, DispatchMode dispatchMode);

        internal void HandleEventAtTargetPhase(EventBase evt)
        {
            evt.currentTarget = evt.target;
            evt.propagationPhase = PropagationPhase.AtTarget;
            HandleEvent(evt);
            evt.propagationPhase = PropagationPhase.DefaultActionAtTarget;
            HandleEvent(evt);
        }

        /// <summary>
        /// Handle an event, most often by executing the callbacks associated with the event.
        /// </summary>
        /// <param name="evt">The event to handle.</param>
        public virtual void HandleEvent(EventBase evt)
        {
            if (evt == null)
                return;

            switch (evt.propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                case PropagationPhase.BubbleUp:
                {
                    if (!evt.isPropagationStopped)
                    {
                        m_CallbackRegistry?.InvokeCallbacks(evt, evt.propagationPhase);
                    }
                    break;
                }

                case PropagationPhase.AtTarget:
                {
                    //We make sure we invoke callbacks from the TrickleDownPhase before the BubbleUp ones when we are directly at target
                    if (!evt.isPropagationStopped)
                    {
                        m_CallbackRegistry?.InvokeCallbacks(evt, PropagationPhase.TrickleDown);
                    }
                    if (!evt.isPropagationStopped)
                    {
                        m_CallbackRegistry?.InvokeCallbacks(evt, PropagationPhase.BubbleUp);
                    }
                }
                break;

                case PropagationPhase.DefaultActionAtTarget:
                {
                    if (!evt.isDefaultPrevented)
                    {
                        using (new EventDebuggerLogExecuteDefaultAction(evt))
                        {
                            if (evt.skipDisabledElements && this is VisualElement ve && !ve.enabledInHierarchy)
                                ExecuteDefaultActionDisabledAtTarget(evt);
                            else
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
                            if (evt.skipDisabledElements && this is VisualElement ve && !ve.enabledInHierarchy)
                                ExecuteDefaultActionDisabled(evt);
                            else
                                ExecuteDefaultAction(evt);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if event handlers, for the event propagation TrickleDown phase, are attached to this object.
        /// </summary>
        /// <returns>True if object has event handlers for the TrickleDown phase.</returns>
        public bool HasTrickleDownHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasTrickleDownHandlers();
        }

        /// <summary>
        /// Return true if event handlers for the event propagation BubbleUp phase have been attached on this object.
        /// </summary>
        /// <returns>True if object has event handlers for the BubbleUp phase.</returns>
        public bool HasBubbleUpHandlers()
        {
            return m_CallbackRegistry != null && m_CallbackRegistry.HasBubbleHandlers();
        }

        /// <summary>
        /// Executes logic after the callbacks registered on the event target have executed,
        /// unless the event is marked to prevent its default behaviour.
        /// <see cref="EventBase{T}.PreventDefault"/>.
        /// </summary>
        /// <remarks>
        /// This method is designed to be overriden by subclasses. Use it to implement event handling without
        /// registering callbacks, which guarantees precedences of callbacks registered by users of the subclass.
        /// Unlike <see cref="ExecuteDefaultAction"/>, this method is called after the callbacks registered on
        /// the element but before callbacks registered on its ancestors with <see cref="TrickleDown.NoTrickleDown"/>.
        /// </remarks>
        /// <param name="evt">The event instance.</param>
        protected virtual void ExecuteDefaultActionAtTarget(EventBase evt) {}

        /// <summary>
        /// Executes logic after the callbacks registered on the event target have executed,
        /// unless the event has been marked to prevent its default behaviour.
        /// <see cref="EventBase{T}.PreventDefault"/>.
        /// </summary>
        /// <remarks>
        /// This method is designed to be overriden by subclasses. Use it to implement event handling without
        /// registering callbacks which guarantees precedences of callbacks registered by users of the subclass.
        /// Unlike <see cref="ExecuteDefaultActionAtTarget"/>, this method is called after both the callbacks registered
        /// on the element and callbacks registered on its ancestors with <see cref="TrickleDown.NoTrickleDown"/>.
        /// </remarks>
        /// <param name="evt">The event instance.</param>
        protected virtual void ExecuteDefaultAction(EventBase evt) {}

        internal virtual void ExecuteDefaultActionDisabledAtTarget(EventBase evt) {}
        internal virtual void ExecuteDefaultActionDisabled(EventBase evt) {}
    }
}
