// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
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

    internal abstract class EventCallbackFunctorBase : IDisposable
    {
        public long eventTypeId;
        public InvokePolicy invokePolicy;

        public abstract void Invoke(EventBase evt);
        public abstract void UnregisterCallback(CallbackEventHandler target, TrickleDown useTrickleDown);
        public abstract void Dispose();
        // The eventTypeId is necessary because we allow the same callback to be registered for multiple event types
        public abstract bool IsEquivalentTo(long eventTypeId, Delegate callback);
    }

    internal class EventCallbackFunctor<TEventType> : EventCallbackFunctorBase
        where TEventType : EventBase<TEventType>, new()
    {
        EventCallback<TEventType> m_Callback;

        public static EventCallbackFunctor<TEventType> GetPooled(long eventTypeId, EventCallback<TEventType> callback,
            InvokePolicy invokePolicy = InvokePolicy.Default)
        {
            var self = GenericPool<EventCallbackFunctor<TEventType>>.Get();
            self.eventTypeId = eventTypeId;
            self.invokePolicy = invokePolicy;
            self.m_Callback = callback;
            return self;
        }

        public override void Dispose()
        {
            eventTypeId = default;
            invokePolicy = default;
            m_Callback = default;
            GenericPool<EventCallbackFunctor<TEventType>>.Release(this);
        }

        public override void Invoke(EventBase evt)
        {
            using (new EventDebuggerLogCall(m_Callback, evt))
            {
                m_Callback(evt as TEventType);
            }
        }

        public override void UnregisterCallback(CallbackEventHandler target, TrickleDown useTrickleDown)
        {
            target.UnregisterCallback(m_Callback, useTrickleDown);
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback)
        {
            return this.eventTypeId == eventTypeId && (Delegate)m_Callback == callback;
        }
    }

    internal class EventCallbackFunctor<TEventType, TCallbackArgs> : EventCallbackFunctorBase
        where TEventType : EventBase<TEventType>, new()
    {
        EventCallback<TEventType, TCallbackArgs> m_Callback;

        internal TCallbackArgs userArgs { get; set; }

        public static EventCallbackFunctor<TEventType, TCallbackArgs> GetPooled(long eventTypeId,
            EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs,
            InvokePolicy invokePolicy = InvokePolicy.Default)
        {
            var self = GenericPool<EventCallbackFunctor<TEventType, TCallbackArgs>>.Get();
            self.eventTypeId = eventTypeId;
            self.invokePolicy = invokePolicy;
            self.userArgs = userArgs;
            self.m_Callback = callback;
            return self;
        }

        public override void Dispose()
        {
            eventTypeId = default;
            invokePolicy = default;
            userArgs = default;
            m_Callback = default;
            GenericPool<EventCallbackFunctor<TEventType, TCallbackArgs>>.Release(this);
        }

        public override void Invoke(EventBase evt)
        {
            using (new EventDebuggerLogCall(m_Callback, evt))
            {
                m_Callback(evt as TEventType, userArgs);
            }
        }

        public override void UnregisterCallback(CallbackEventHandler target, TrickleDown useTrickleDown)
        {
            target.UnregisterCallback(m_Callback, useTrickleDown);
        }

        public override bool IsEquivalentTo(long eventTypeId, Delegate callback)
        {
            return this.eventTypeId == eventTypeId && (Delegate)m_Callback == callback;
        }
    }
}
