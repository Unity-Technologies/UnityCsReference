// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    internal static class EventArgId
    {
        public const int None = 0;
        public const int Self = 1;
    }

    /// <summary>
    /// A static class that allows creation of <see cref="EventArg{TArg}"/>.
    /// </summary>
    public static class EventArg
    {
        private static int s_NextId = 2;

        /// <summary>
        /// Creates a <see cref="EventArg{TArg}"/> containing a unique identifier.
        /// </summary>
        /// <typeparam name="TArg">The type of <see cref="EventArg{TArg}"/> to create.</typeparam>
        /// <returns>A valid <see cref="EventArg{TArg}"/> instance.</returns>
        public static EventArg<TArg> Create<TArg>()
        {
            return new(s_NextId++);
        }
    }

    /// <summary>
    /// A reusable identifier for an event callback argument of type @@TArg@@.
    /// </summary>
    /// <typeparam name="TArg">The type of the callback argument for which this identifier can be used.</typeparam>
    /// <seealso cref="EventCallbackInternal.Create{TEvent, TArg}(EventCallback{TEvent, TArg}, EventArg{TArg}, TrickleDown)"/>
    public readonly struct EventArg<TArg>
    {
        internal readonly int m_Id;
        internal EventArg(int id) { m_Id = id; }

        /// <summary>
        /// Sets a value for the provided element for this argument identifier. That value can then be retrieved by
        /// callbacks that have a second argument of the same type.
        /// </summary>
        /// <remarks>
        /// If a callback is triggered on an element for which the expected argument value has not been set,
        /// that callback will be skipped and a warning will be printed.
        /// </remarks>
        /// <param name="element">The element to set a value for.</param>
        /// <param name="value">The value that will be used by callbacks on this element for the provided argument.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(VisualElement element, in TArg value)
        {
            Debug.Assert(m_Id != 0, "EventArg<TArg>.Register: m_Id != 0. EventArg values for this method must be the result of a call to EventArg.Create<TArg>.");
            (element.m_CallbackRegistry ??= EventCallbackRegistry.GetPooled()).RegisterArg(m_Id, in value);
        }

        /// <summary>
        /// Removes the value for the provided element for this argument identifier. Until a new value is set,
        /// a value can no longer be retrieved by callbacks that have a second argument of the same type.
        /// </summary>
        /// <remarks>
        /// If a callback is triggered on an element for which the expected argument value has not been set,
        /// that callback will be skipped and a warning will be printed.
        /// </remarks>
        /// <param name="element">The element to unset a value for.</param>
        /// <returns>True if the arg was found and removed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Unregister(VisualElement element)
        {
            Debug.Assert(m_Id != 0, "EventArg<TArg>.Unregister: m_Id != 0. EventArg values for this method must be the result of a call to EventArg.Create<TArg>.");
            return element.m_CallbackRegistry?.UnregisterArg(m_Id) ?? false;
        }
    }

    internal struct EventSelfArg<TElement> where TElement : VisualElement
    {
        public static readonly EventArg<TElement> Self = new(EventArgId.Self);
        private static EventSelfArgValue<TElement> s_SelfInvoker;
        public static EventSelfArgValue<TElement> GetSelfInvoker() => s_SelfInvoker ??= new();
    }

    internal interface IEventInvoker
    {
        void Invoke(EventBase evt, EventCallbackInternal c);
    }

    internal abstract class EventArgValue : IEventInvoker
    {
        public EventArgValue nextArg;
        public int argId;
        public int temporaryCount;

        public static readonly EventNoArgValue None = new();
        public static IEventInvoker Self(VisualElement ve) => ve.typeData.selfEventInvoker;

        public abstract void Dispose();
        public abstract void Invoke(EventBase evt, EventCallbackInternal c);
        public abstract bool ValueEquals<TArg>(in TArg other);
    }

    internal class EventNoArgValue : IEventInvoker
    {
        public void Invoke(EventBase evt, EventCallbackInternal c) =>
            evt.InvokeCallback(c.userCallback);
    }

    internal class EventSelfArgValue<TElement> : IEventInvoker
    {
        public void Invoke(EventBase evt, EventCallbackInternal c) =>
            evt.InvokeCallback(c.userCallback, (TElement)evt.currentTarget);
    }

    internal static class EventArgValueFactory<TArg>
    {
        public delegate EventArgValue GetPooledFunc(int id, in TArg value);

        public static readonly GetPooledFunc GetPooled = typeof(TArg).IsValueType
            ? EventArgValue<TArg>.GetPooled
            : EventArgObjectValue.GetPooled;
    }

    internal class EventArgObjectValue : EventArgValue
    {
        private static readonly ObjectPool<EventArgObjectValue> k_Pool = new(() => new(),
            EventCallbackRegistry.k_PoolMaxSize);

        private object value;
        private Action<EventArgObjectValue, EventBase, EventCallbackInternal> invoke;

        private static class Invoker<TArg>
        {
            public static readonly Action<EventArgObjectValue, EventBase, EventCallbackInternal> k_Invoke =
                (self, evt, c) => evt.InvokeCallback(c.userCallback, (TArg)self.value);
        }

        public static EventArgObjectValue GetPooled<TArg>(int id, in TArg value)
        {
            var v = k_Pool.Get();
            v.argId = id;
            v.value = value;
            v.invoke = Invoker<TArg>.k_Invoke;
            return v;
        }

        public sealed override void Dispose()
        {
            nextArg = null;
            value = default;
            k_Pool.Release(this);
        }

        public sealed override void Invoke(EventBase evt, EventCallbackInternal c)
        {
            invoke(this, evt, c);
        }

        public sealed override bool ValueEquals<TArg>(in TArg other)
        {
            return Equals(value, other);
        }
    }

    internal class EventArgValue<TArg> : EventArgValue
    {
        private const int k_PoolMaxSize = 64;
        private static readonly EqualityComparer<TArg> k_EqualityComparer = EqualityComparer<TArg>.Default;
        private static ObjectPool<EventArgValue<TArg>> s_Pool;

        private TArg value;

        public static EventArgValue<TArg> GetPooled(int id, in TArg value)
        {
            var v = (s_Pool ??= new(() => new(), k_PoolMaxSize)).Get();
            v.argId = id;
            v.value = value;
            return v;
        }

        public sealed override void Dispose()
        {
            nextArg = null;
            value = default;
            s_Pool?.Release(this);
        }

        public sealed override void Invoke(EventBase evt, EventCallbackInternal c)
        {
            evt.InvokeCallback(c.userCallback, in value);
        }

        public sealed override bool ValueEquals<TOtherArg>(in TOtherArg other)
        {
            return this is EventArgValue<TOtherArg> v && v.ValueEquals(in other);
        }

        private bool ValueEquals(in TArg other)
        {
            return k_EqualityComparer.Equals(value, other);
        }
    }

    /// <summary>
    /// A callback object that can be registered on VisualElements to react to any event type.
    /// </summary>
    /// <remarks>
    /// The same callback can be registered on multiple different elements, resulting in effective memory sharing.
    /// If the element instance is needed by the callback code, an optional argument can be used to provide it.
    /// </remarks>
    internal class EventCallbackInternal
    {
        internal EventBase.TypeData eventData { get; private set; }
        internal Delegate userCallback { get; private set; }
        internal InvokePolicy invokePolicy { get; private set; }
        internal int argId { get; private set; }

        internal long eventTypeId => eventData.eventTypeId;
        internal int eventCategories => eventData.eventCategories;
        internal Type eventType => eventData.eventType;

        internal TrickleDown useTrickleDown => (invokePolicy & InvokePolicy.TrickleDown) != 0
            ? TrickleDown.TrickleDown
            : TrickleDown.NoTrickleDown;

        /// <summary>
        /// Creates an event callback instance that will call the given @@userCallback@@ delegate when registered on
        /// an element that is part of the propagation chain of any event of type @@TEvent@@.
        /// </summary>
        /// <param name="userCallback">The delegate called when this callback is triggered.</param>
        /// <param name="useTrickleDown">Whether this callback is triggered during the TrickleDown phase of the event
        /// propagation or not. If not, it will be triggered during the BubbleUp phase (default).</param>
        /// <typeparam name="TEvent">The type of event this callback reacts to.</typeparam>
        /// <returns>The created event callback instance</returns>
        public static EventCallbackInternal Create<TEvent>(EventCallback<TEvent> userCallback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new() =>
            Create(userCallback, InvokePolicy.Default, useTrickleDown);

        /// <summary>
        /// Creates an event callback instance that will call the given @@userCallback@@ delegate when registered on
        /// an element that is part of the propagation chain of any event of type @@TEvent@@.
        /// The delegate will always be called with its second argument set to the element on which it's registered.
        /// </summary>
        /// <param name="userCallback">The delegate called when this callback is triggered.</param>
        /// <param name="useTrickleDown">Whether this callback is triggered during the TrickleDown phase of the event
        /// propagation or not. If not, it will be triggered during the BubbleUp phase (default).</param>
        /// <typeparam name="TEvent">The type of event this callback reacts to.</typeparam>
        /// <typeparam name="TElement">The type of element this callback can be registered to.</typeparam>
        /// <returns>The created event callback instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventCallbackInternal Create<TEvent, TElement>(EventCallback<TEvent, TElement> userCallback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new() where TElement : VisualElement =>
            Create(userCallback, InvokePolicy.Default, useTrickleDown);

        /// <summary>
        /// Creates an event callback instance that will call the given @@userCallback@@ delegate when registered on
        /// an element that is part of the propagation chain of any event of type @@TEvent@@.
        /// The delegate will be called with its second argument set to whichever value the @@arg@@ parameter holds
        /// for the element on which it's registered.
        /// </summary>
        /// <param name="userCallback">The delegate called when this callback is triggered.</param>
        /// <param name="arg">The identifier used to send specific argument values to the delegate for any element
        /// on which this callback is registered.</param>
        /// <param name="useTrickleDown">Whether this callback is triggered during the TrickleDown phase of the event
        /// propagation or not. If not, it will be triggered during the BubbleUp phase (default).</param>
        /// <typeparam name="TEvent">The type of event this callback reacts to.</typeparam>
        /// <typeparam name="TArg">The identifier used to get and set the argument value for this callback.</typeparam>
        /// <returns>The created event callback instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventCallbackInternal Create<TEvent, TArg>(EventCallback<TEvent, TArg> userCallback, EventArg<TArg> arg,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new() =>
            Create(userCallback, arg, InvokePolicy.Default, useTrickleDown);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackInternal Create<TEvent>(EventCallback<TEvent> userCallback, InvokePolicy invokePolicy, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new() =>
            Create<TEvent>(userCallback, EventArgId.None, invokePolicy, trickleDown);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackInternal Create<TEvent, TElement>(EventCallback<TEvent, TElement> userCallback,
            InvokePolicy invokePolicy, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new() where TElement : VisualElement =>
            Create<TEvent>(userCallback, EventSelfArg<TElement>.Self.m_Id, invokePolicy, trickleDown);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackInternal Create<TEvent, TArg>(EventCallback<TEvent, TArg> userCallback, EventArg<TArg> arg, InvokePolicy invokePolicy, TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new() =>
            Create<TEvent>(userCallback, arg.m_Id, invokePolicy, trickleDown);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackInternal Create<TEvent>(Delegate userCallback, int argId, InvokePolicy invokePolicy, TrickleDown trickleDown)
            where TEvent : EventBase<TEvent>, new()
        {
            return new(EventBase<TEvent>.k_TypeData, userCallback, invokePolicy | (trickleDown == TrickleDown.TrickleDown ? InvokePolicy.TrickleDown : 0), argId);
        }

        public EventCallbackInternal() { }

        internal EventCallbackInternal(EventBase.TypeData eventData, Delegate userCallback, InvokePolicy invokePolicy, int argId)
        {
            this.eventData = eventData;
            this.userCallback = userCallback;
            this.invokePolicy = invokePolicy;
            this.argId = argId;
        }

        internal void Reset<TEvent>(Delegate userCallback, int argId, InvokePolicy invokePolicy, TrickleDown trickleDown)
            where TEvent : EventBase<TEvent>, new()
        {
            eventData = EventBase<TEvent>.k_TypeData;
            this.userCallback = userCallback;
            this.invokePolicy = invokePolicy | (trickleDown == TrickleDown.TrickleDown ? InvokePolicy.TrickleDown : 0);
            this.argId = argId;
        }

        internal void Reset()
        {
            eventData = null;
            userCallback = null;
        }

        /// <summary>
        /// Adds this callback's event handler to the given element.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp), this method has no effect.
        ///
        /// Refer to the [[wiki:UIE-Events-Handling|Handle event callbacks and value changes]] manual page for more information and examples.
        ///
        /// This method allows this callback instance to be shared by multiple elements, resulting in a reduced memory footprint.
        /// </remarks>
        /// <param name="element">The element to add the callback to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(VisualElement element)
        {
            GlobalCallbackRegistry.RegisterListeners(eventType, element, userCallback, useTrickleDown);
            element.AddEventCallbackCategories(eventCategories, useTrickleDown);

            (element.m_CallbackRegistry ??= EventCallbackRegistry.GetPooled()).RegisterCallback(this);
        }

        /// <summary>
        /// Removes this callback from the given element.
        /// </summary>
        /// <seealso cref="Register"/>
        /// <param name="element">The element to remove the callback from. If this callback was never registered, nothing happens.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(VisualElement element)
        {
            GlobalCallbackRegistry.UnregisterListeners(eventType, element, userCallback);
            element.m_CallbackRegistry?.UnregisterCallback(this);
        }
    }

    /// <summary>
    /// A static class that allows creation of <see cref="EventCallbackDefinition"/> and
    /// <see cref="EventCallbackDefinition{TElement}"/> instances.
    /// </summary>
    public static class EventCallback
    {
        /// <summary>
        /// Creates a callback instance that will call the given @@userCallback@@ delegate when registered on
        /// an element that is part of the propagation chain of any event of type @@TEvent@@.
        /// </summary>
        /// <param name="userCallback">The delegate called when this callback is triggered.</param>
        /// <param name="useTrickleDown">Whether this callback is triggered during the TrickleDown phase of the event
        /// propagation or not. If not, it will be triggered during the BubbleUp phase (default).</param>
        /// <param name="invokeOnce">If true, callback will be unregistered automatically when invoked.</param>
        /// <typeparam name="TEvent">The type of event this callback reacts to.</typeparam>
        /// <returns>The created callback instance</returns>
        public static EventCallbackDefinition Create<TEvent>(EventCallback<TEvent> userCallback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, bool invokeOnce = false)
            where TEvent : EventBase<TEvent>, new() =>
            Create(userCallback, Policy(useTrickleDown, invokeOnce));

        /// <summary>
        /// Creates a callback instance that will call the given @@userCallback@@ delegate when registered on
        /// an element that is part of the propagation chain of any event of type @@TEvent@@.
        /// The delegate will always be called with its second argument set to the element on which it's registered.
        /// </summary>
        /// <param name="userCallback">The delegate called when this callback is triggered.</param>
        /// <param name="useTrickleDown">Whether this callback is triggered during the TrickleDown phase of the event
        /// propagation or not. If not, it will be triggered during the BubbleUp phase (default).</param>
        /// <param name="invokeOnce">If true, callback will be unregistered automatically when invoked.</param>
        /// <typeparam name="TEvent">The type of event this callback reacts to.</typeparam>
        /// <typeparam name="TElement">The type of element this callback can be registered to.</typeparam>
        /// <returns>The created callback instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventCallbackDefinition<TElement> Create<TEvent, TElement>(EventCallback<TEvent, TElement> userCallback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, bool invokeOnce = false)
            where TEvent : EventBase<TEvent>, new() where TElement : VisualElement =>
            Create(userCallback, Policy(useTrickleDown, invokeOnce));

        /// <summary>
        /// Creates a callback instance that will call the given @@userCallback@@ delegate when registered on
        /// an element that is part of the propagation chain of any event of type @@TEvent@@.
        /// The delegate will be called with its second argument set to whichever value the @@arg@@ parameter holds
        /// for the element on which it's registered.
        /// </summary>
        /// <param name="userCallback">The delegate called when this callback is triggered.</param>
        /// <param name="arg">The identifier used to send specific argument values to the delegate for any element
        /// on which this callback is registered.</param>
        /// <param name="useTrickleDown">Whether this callback is triggered during the TrickleDown phase of the event
        /// propagation or not. If not, it will be triggered during the BubbleUp phase (default).</param>
        /// <param name="invokeOnce">If true, callback will be unregistered automatically when invoked.</param>
        /// <typeparam name="TEvent">The type of event this callback reacts to.</typeparam>
        /// <typeparam name="TArg">The identifier used to get and set the argument value for this callback.</typeparam>
        /// <returns>The created callback instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventCallbackDefinition Create<TEvent, TArg>(EventCallback<TEvent, TArg> userCallback, EventArg<TArg> arg,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, bool invokeOnce = false)
            where TEvent : EventBase<TEvent>, new() =>
            Create(userCallback, arg, Policy(useTrickleDown, invokeOnce));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackDefinition Create<TEvent>(EventCallback<TEvent> userCallback,
            InvokePolicy invokePolicy)
            where TEvent : EventBase<TEvent>, new() =>
            Create<TEvent>(userCallback, EventArgId.None, invokePolicy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackDefinition<TElement> Create<TEvent, TElement>(
            EventCallback<TEvent, TElement> userCallback, InvokePolicy invokePolicy)
            where TEvent : EventBase<TEvent>, new() where TElement : VisualElement =>
            Create<TEvent, TElement>(userCallback, EventSelfArg<TElement>.Self.m_Id, invokePolicy);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackDefinition Create<TEvent, TArg>(EventCallback<TEvent, TArg> userCallback,
            EventArg<TArg> arg, InvokePolicy invokePolicy)
            where TEvent : EventBase<TEvent>, new()
        {
            Debug.Assert(arg.m_Id != 0, "EventCallback.Create: arg.m_Id != 0. EventArg values for this method must be the result of a call to EventArg.Create<TArg>.");
            return Create<TEvent>(userCallback, arg.m_Id, invokePolicy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackDefinition Create<TEvent>(Delegate userCallback, int argId,
            InvokePolicy invokePolicy)
            where TEvent : EventBase<TEvent>, new()
        {
            return new EventCallbackDefinition(new (EventBase<TEvent>.k_TypeData, userCallback, invokePolicy, argId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EventCallbackDefinition<TElement> Create<TEvent, TElement>(Delegate userCallback, int argId,
            InvokePolicy invokePolicy)
            where TEvent : EventBase<TEvent>, new() where TElement : VisualElement
        {
            return new EventCallbackDefinition<TElement>(new (EventBase<TEvent>.k_TypeData, userCallback, invokePolicy, argId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InvokePolicy Policy(TrickleDown useTrickleDown, bool invokeOnce)
        {
            return (useTrickleDown == TrickleDown.TrickleDown ? InvokePolicy.TrickleDown : 0) |
                   (invokeOnce ? InvokePolicy.Once : 0);
        }
    }

    /// <summary>
    /// An object that can be registered on VisualElements to react to any event type.
    /// </summary>
    /// <remarks>
    /// The same callback instance can be registered on multiple different elements, resulting in effective memory
    /// sharing. If the element instance is needed by the callback code, an optional argument can be used to provide it.
    /// </remarks>
    /// <seealso cref="EventCallback"/>
    /// <seealso cref="EventCallbackDefinition{TElement}"/>
    /// <seealso cref="EventCallbackGroup"/>
    public readonly struct EventCallbackDefinition
    {
        internal readonly EventCallbackInternal c;

        internal EventCallbackDefinition(EventCallbackInternal c)
        {
            this.c = c;
        }

        /// <summary>
        /// Adds this callback's event handler to the given element.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp), this method has no effect.
        ///
        /// Refer to the [[wiki:UIE-Events-Handling|Handle event callbacks and value changes]] manual page for more information and examples.
        ///
        /// This method allows this callback instance to be shared by multiple elements, resulting in a reduced memory footprint.
        /// </remarks>
        /// <param name="element">The element to add the callback to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(VisualElement element) => c.Register(element);

        /// <summary>
        /// Removes this callback from the given element.
        /// </summary>
        /// <seealso cref="Register"/>
        /// <param name="element">The element to remove the callback from. If this callback was never registered, nothing happens.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(VisualElement element) => c.Unregister(element);
    }

    /// <summary>
    /// An object that can be registered on elements of the specified type to react to any event type.
    /// </summary>
    /// <typeparam name="TElement">The type of element this callback can be registered to.</typeparam>
    /// <remarks>
    /// The same callback instance can be registered on multiple different elements, resulting in effective memory
    /// sharing. If the element instance is needed by the callback code, an optional argument can be used to provide it.
    /// </remarks>
    /// <seealso cref="EventCallback"/>
    /// <seealso cref="EventCallbackDefinition"/>
    /// <seealso cref="EventCallbackGroup{TElement}"/>
    public readonly struct EventCallbackDefinition<TElement> where TElement : VisualElement
    {
        internal readonly EventCallbackInternal c;

        internal EventCallbackDefinition(EventCallbackInternal c)
        {
            this.c = c;
        }

        /// <summary>
        /// Adds this callback's event handler to the given element.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp), this method has no effect.
        ///
        /// Refer to the [[wiki:UIE-Events-Handling|Handle event callbacks and value changes]] manual page for more information and examples.
        ///
        /// This method allows this callback instance to be shared by multiple elements, resulting in a reduced memory footprint.
        /// </remarks>
        /// <param name="element">The element to add the callback to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(TElement element) => c.Register(element);

        /// <summary>
        /// Removes this callback from the given element.
        /// </summary>
        /// <seealso cref="Register"/>
        /// <param name="element">The element to remove the callback from. If this callback was never registered, nothing happens.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(TElement element) => c.Unregister(element);

        /// <summary>
        /// Converts this callback to a more constrained callback type.
        /// </summary>
        /// <remarks>
        /// This allows a callback declared for a base class to be used as part of a callback group alongside other
        /// callbacks from a derived class.
        /// </remarks>
        /// <typeparam name="TDerived">A more constrained type for the returned callback.</typeparam>
        /// <returns>The same callback but with a more constrained type signature.</returns>
        public EventCallbackDefinition<TDerived> As<TDerived>() where TDerived : TElement => new(c);

        /// <summary>
        /// Converts a given callback to a more constrained callback type.
        /// </summary>
        /// <remarks>
        /// This allows a callback declared for a base class to be used as part of a callback group alongside other
        /// callbacks from a derived class.
        /// </remarks>
        /// <returns>The same callback but with a more constrained type signature.</returns>
        public static implicit operator EventCallbackDefinition<TElement>(EventCallbackDefinition c) => new(c.c);
    }

    /// <summary>
    /// An object that can be registered on VisualElements to rapidly register multiple event callbacks.
    /// </summary>
    /// <remarks>
    /// The same callback group can be registered on multiple different elements, resulting in effective memory
    /// sharing. Note that individual callbacks that are also part of a registered group should not be unregistered
    /// separately from the rest of their group.
    /// </remarks>
    /// <seealso cref="EventCallbackInternal"/>
    internal readonly struct EventCallbackGroupInternal
    {
        internal readonly EventCallbackInternal[] m_BubbleUpCallbacks;
        internal readonly EventCallbackInternal[] m_TrickleDownCallbacks;
        internal readonly int m_BubbleUpCount;
        internal readonly int m_TrickleDownCount;
        internal readonly int m_BubbleUpCategories;
        internal readonly int m_TrickleDownCategories;

        /// <summary>
        /// Creates a callback group instance allowing the provided callbacks to be registered and unregistered
        /// all at once on any VisualElement.
        /// </summary>
        /// <param name="callbacks">The callbacks included in this group.</param>
        public EventCallbackGroupInternal(params EventCallbackInternal[] callbacks)
        {
            if (callbacks.Length == 0)
            {
                Debug.LogWarning("Callback group needs to contain at least 1 callback.");
            }

            int bubbleUpCount = 0;
            int trickleDownCount = 0;
            foreach (var callback in callbacks)
            {
                if ((callback.invokePolicy & InvokePolicy.Once) != 0)
                {
                    Debug.LogWarning("Callback group can't contain callbacks with InvokePolicy.Once.");
                }

                if (callback.useTrickleDown == TrickleDown.TrickleDown)
                {
                    m_TrickleDownCategories |= callback.eventCategories;
                    trickleDownCount++;
                }
                else
                {
                    m_BubbleUpCategories |= callback.eventCategories;
                    bubbleUpCount++;
                }
            }

            m_BubbleUpCallbacks = bubbleUpCount > 0 ? new EventCallbackInternal[bubbleUpCount] : null;
            m_TrickleDownCallbacks = trickleDownCount > 0 ? new EventCallbackInternal[trickleDownCount] : null;
            m_BubbleUpCount = bubbleUpCount;
            m_TrickleDownCount = trickleDownCount;

            bubbleUpCount = trickleDownCount = 0;
            foreach (var callback in callbacks)
            {
                if (callback.useTrickleDown == TrickleDown.TrickleDown)
                {
                    m_TrickleDownCallbacks[trickleDownCount] = callback;
                    trickleDownCount++;
                }
                else
                {
                    m_BubbleUpCallbacks[bubbleUpCount] = callback;
                    bubbleUpCount++;
                }
            }
        }

        internal void Register(VisualElement ve)
        {
            AddListenersAndCategories(ve);
            var registry = ve.m_CallbackRegistry ??= EventCallbackRegistry.GetPooled();

            if (m_BubbleUpCallbacks != null)
            {
                var list = registry.m_BubbleUpCallbacks.GetCallbackListForWriting();
                list.AddRange(m_BubbleUpCallbacks, m_BubbleUpCount);
            }

            if (m_TrickleDownCallbacks != null)
            {
                var list = registry.m_TrickleDownCallbacks.GetCallbackListForWriting();
                list.AddRange(m_TrickleDownCallbacks, m_TrickleDownCount);
            }
        }

        internal void Unregister(VisualElement ve)
        {
            var registry = ve.m_CallbackRegistry;
            if (registry == null) return;

            if (m_BubbleUpCallbacks != null)
            {
                var list = registry.m_BubbleUpCallbacks.GetCallbackListForWriting();
                var i = list.FindGroup(m_BubbleUpCallbacks[0]);
                if (i >= 0)
                    list.RemoveGroupAt(i, m_BubbleUpCount);
            }

            if (m_TrickleDownCallbacks != null)
            {
                var list = registry.m_TrickleDownCallbacks.GetCallbackListForWriting();
                var i = list.FindGroup(m_TrickleDownCallbacks[0]);
                if (i >= 0)
                    list.RemoveGroupAt(i, m_TrickleDownCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddListenersAndCategories(VisualElement ve)
        {
            if (GlobalCallbackRegistry.IsEventDebuggerConnected)
            {
                if (m_BubbleUpCallbacks != null)
                {
                    foreach(var callback in m_BubbleUpCallbacks)
                        GlobalCallbackRegistry.RegisterListeners(callback.eventType, ve, callback.userCallback, callback.useTrickleDown);
                }

                if (m_TrickleDownCallbacks != null)
                {
                    foreach(var callback in m_TrickleDownCallbacks)
                        GlobalCallbackRegistry.RegisterListeners(callback.eventType, ve, callback.userCallback, callback.useTrickleDown);
                }
            }
            ve.AddEventCallbackCategories(trickleDownCategories: m_TrickleDownCategories,
                bubbleUpCategories: m_BubbleUpCategories);
        }
    }

    /// <summary>
    /// An object that can be registered on elements of a any type to rapidly register multiple event callbacks.
    /// </summary>
    /// <remarks>
    /// The same callback group can be registered on multiple different elements, resulting in effective memory
    /// sharing. Note that individual callbacks that are also part of a registered group should not be unregistered
    /// separately from the rest of their group.
    /// </remarks>
    /// <seealso cref="EventCallbackDefinition"/>
    public readonly struct EventCallbackGroup
    {
        internal readonly EventCallbackGroupInternal g;

        /// <summary>
        /// Creates a callback group allowing the provided callbacks to be registered and unregistered
        /// all at once on any VisualElement.
        /// </summary>
        /// <param name="callbacks">The callbacks included in this group.</param>
        public EventCallbackGroup(params EventCallbackDefinition[] callbacks)
        {
            var eventCallbacks = new EventCallbackInternal[callbacks.Length];
            for (var i = 0; i < callbacks.Length; i++)
                eventCallbacks[i] = callbacks[i].c;
            g = new(eventCallbacks);
        }

        /// <summary>
        /// Creates a callback group allowing the provided callbacks to be registered and unregistered
        /// all at once on any VisualElement.
        /// </summary>
        /// <param name="callbacks">The callbacks included in this group.</param>
        public EventCallbackGroup(ReadOnlySpan<EventCallbackDefinition> callbacks)
        {
            var eventCallbacks = new EventCallbackInternal[callbacks.Length];
            for (var i = 0; i < callbacks.Length; i++)
                eventCallbacks[i] = callbacks[i].c;
            g = new(eventCallbacks);
        }

        /// <summary>
        /// Adds all group event handlers to the given element.
        /// </summary>
        /// <remarks>
        /// This method allows the contained callbacks to be shared by multiple elements, resulting in a
        /// reduced memory footprint.
        ///
        /// This method is usually faster than registering the corresponding callbacks individually.
        /// </remarks>
        /// <param name="element">The element to add the group to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(VisualElement element)
        {
            g.Register(element);
        }

        /// <summary>
        /// Removes all group event handlers from the given element.
        /// </summary>
        /// <remarks>
        /// This method is usually much faster than unregistering the corresponding callbacks individually.
        ///
        /// If any callback contained in this group is unregistered individually prior to calling
        /// this method, an undefined behavior will result.
        /// </remarks>
        /// <param name="element">The element to remove the group from. If this callback group was never registered, nothing happens.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(VisualElement element)
        {
            g.Unregister(element);
        }
    }

    /// <summary>
    /// An object that can be registered on elements of a specific type to rapidly register multiple event callbacks.
    /// </summary>
    /// <remarks>
    /// The same callback group can be registered on multiple different elements, resulting in effective memory
    /// sharing. Note that individual callbacks that are also part of a registered group should not be unregistered
    /// separately from the rest of their group.
    /// </remarks>
    /// <typeparam name="TElement">The type of element callbacks in this group apply to.</typeparam>
    /// <seealso cref="EventCallbackDefinition{TElement}"/>
    public readonly struct EventCallbackGroup<TElement> where TElement : VisualElement
    {
        internal readonly EventCallbackGroupInternal g;

        /// <summary>
        /// Creates a callback group allowing the provided callbacks to be registered and unregistered
        /// all at once on any element of a compatible type.
        /// </summary>
        /// <param name="callbacks">The callbacks included in this group.</param>
        public EventCallbackGroup(params EventCallbackDefinition<TElement>[] callbacks)
        {
            var eventCallbacks = new EventCallbackInternal[callbacks.Length];
            for (var i = 0; i < callbacks.Length; i++)
                eventCallbacks[i] = callbacks[i].c;
            g = new(eventCallbacks);
        }

        /// <summary>
        /// Creates a callback group allowing the provided callbacks to be registered and unregistered
        /// all at once on any element of a compatible type.
        /// </summary>
        /// <param name="callbacks">The callbacks included in this group.</param>
        public EventCallbackGroup(ReadOnlySpan<EventCallbackDefinition<TElement>> callbacks)
        {
            var eventCallbacks = new EventCallbackInternal[callbacks.Length];
            for (var i = 0; i < callbacks.Length; i++)
                eventCallbacks[i] = callbacks[i].c;
            g = new(eventCallbacks);
        }

        /// <summary>
        /// Adds all group event handlers to the given element.
        /// </summary>
        /// <remarks>
        /// This method allows the contained callbacks to be shared by multiple elements, resulting in a
        /// reduced memory footprint.
        ///
        /// This method is usually faster than registering the corresponding callbacks individually.
        /// </remarks>
        /// <param name="element">The element to add the group to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Register(TElement element)
        {
            g.Register(element);
        }

        /// <summary>
        /// Removes all group event handlers from the given element.
        /// </summary>
        /// <remarks>
        /// This method is usually much faster than unregistering the corresponding callbacks individually.
        ///
        /// If any callback contained in this group is unregistered individually prior to calling
        /// this method, an undefined behavior will result.
        /// </remarks>
        /// <param name="element">The element to remove the group from. If this callback group was never registered, nothing happens.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unregister(TElement element)
        {
            g.Unregister(element);
        }
    }

    partial class VisualElement
    {
        /// <summary>
        /// Adds an event handler to this element.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp), this method has no effect.
        ///
        /// Refer to the [[wiki:UIE-Events-Handling|Handle event callbacks and value changes]] manual page for more information and examples.
        ///
        /// This element is also used as the data to pass to the callback. Use this method to avoid closing on local variables
        /// </remarks>
        /// <example>
        /// <code source="../../Tests/UIElementsExamples/Assets/Examples/RegisterCallbackExample.cs"/>
        /// </example>
        /// <seealso cref="PropagationPhase"/>
        /// <param name="callback">The event handler to add. If the handler is null, this method throws an exception.</param>
        /// <param name="arg">The event argument identifier used by the handler.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        /// <typeparam name="TEventType">The event type handled by this callback.</typeparam>
        /// <typeparam name="TArg">The type of event argument expected by the handler.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RegisterCallback<TEventType, TArg>(EventCallback<TEventType, TArg> callback, EventArg<TArg> arg, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            AddListenersAndCategories<TEventType>(callback, useTrickleDown);
            (m_CallbackRegistry ??= EventCallbackRegistry.GetPooled()).RegisterCallback(callback, this, arg.m_Id, useTrickleDown, InvokePolicy.Default);
        }

        /// <summary>
        /// Adds an event handler to this element.
        /// The event handler is automatically unregistered after it has been invoked exactly once.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp)
        /// and hasn't been invoked yet, this method has no effect.
        ///
        /// This element is also used as the data to pass to the callback. Use this method to avoid closing on local variables
        /// </remarks>
        /// <example>
        /// <code source="../../Tests/UIElementsExamples/Assets/Examples/RegisterCallbackExample.cs"/>
        /// </example>
        /// <seealso cref="PropagationPhase"/>
        /// <param name="callback">The event handler to add. If the handler is null, this method throws an exception.</param>
        /// <param name="arg">The event argument identifier used by the handler.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        /// <typeparam name="TEventType">The event type handled by this callback.</typeparam>
        /// <typeparam name="TArg">The type of event argument expected by the handler.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RegisterCallbackOnce<TEventType, TArg>(EventCallback<TEventType, TArg> callback, EventArg<TArg> arg, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new()
        {
            AddListenersAndCategories<TEventType>(callback, useTrickleDown);
            (m_CallbackRegistry ??=EventCallbackRegistry.GetPooled()).RegisterCallback(callback, this, arg.m_Id, useTrickleDown, InvokePolicy.Once);
        }
    }

    public static partial class VisualElementExtensions
    {
        /// <summary>
        /// Adds an event handler to this element.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp), this method has no effect.
        ///
        /// Refer to the [[wiki:UIE-Events-Handling|Handle event callbacks and value changes]] manual page for more information and examples.
        ///
        /// This element is also used as the data to pass to the callback. Use this method to avoid closing on local variables
        /// </remarks>
        /// <example>
        /// <code source="../../Tests/UIElementsExamples/Assets/Examples/RegisterCallbackExample.cs"/>
        /// </example>
        /// <seealso cref="PropagationPhase"/>
        /// <param name="element">The element to add the handler to.</param>
        /// <param name="callback">The event handler to add. If the handler is null, this method throws an exception.
        /// If this element is not and instance of the type of the second argument of the handler, this method throws an exception.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        /// <typeparam name="TEventType">The event type handled by this callback.</typeparam>
        /// <typeparam name="TElement">The type of element this callback is registered to.</typeparam>
        internal static void RegisterCallback<TEventType, TElement>(this TElement element, EventCallback<TEventType, TElement> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new() where TElement : VisualElement
        {
            element.AddListenersAndCategories<TEventType>(callback, useTrickleDown);
            (element.m_CallbackRegistry ??= EventCallbackRegistry.GetPooled()).RegisterCallback(callback, element, EventSelfArg<TElement>.Self.m_Id, useTrickleDown, InvokePolicy.Default);
        }

        /// <summary>
        /// Adds an event handler to this element.
        /// The event handler is automatically unregistered after it has been invoked exactly once.
        /// </summary>
        /// <remarks>
        /// If the event handler is already registered for the same phase (either TrickleDown or BubbleUp)
        /// and hasn't been invoked yet, this method has no effect.
        ///
        /// This element is also used as the data to pass to the callback. Use this method to avoid closing on local variables
        /// </remarks>
        /// <example>
        /// <code source="../../Tests/UIElementsExamples/Assets/Examples/RegisterCallbackExample.cs"/>
        /// </example>
        /// <seealso cref="PropagationPhase"/>
        /// <param name="element">The element to add the handler to.</param>
        /// <param name="callback">The event handler to add. If the handler is null, this method throws an exception.
        /// If this element is not and instance of the type of the second argument of the handler, this method throws an exception.</param>
        /// <param name="useTrickleDown">By default, this callback is called during the BubbleUp phase. Pass @@TrickleDown.TrickleDown@@ to call this callback during the TrickleDown phase.</param>
        /// <typeparam name="TEventType">The event type handled by this callback.</typeparam>
        /// <typeparam name="TElement">The type of element this callback is registered to.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RegisterCallbackOnce<TEventType, TElement>(this TElement element, EventCallback<TEventType, TElement> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown)
            where TEventType : EventBase<TEventType>, new() where TElement : VisualElement
        {
            element.AddListenersAndCategories<TEventType>(callback, useTrickleDown);
            (element.m_CallbackRegistry ??= EventCallbackRegistry.GetPooled()).RegisterCallback(callback, element, EventSelfArg<TElement>.Self.m_Id, useTrickleDown, InvokePolicy.Once);
        }
    }
}
