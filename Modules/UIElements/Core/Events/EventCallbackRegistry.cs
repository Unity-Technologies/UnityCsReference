// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Use this enum to specify during which propagation phase the event handler is executed.
    /// </summary>
    /// <seealso cref="PropagationPhase"/>
    /// <seealso cref="CallbackEventHandler.RegisterCallback{T}(EventCallback{T}, TrickleDown)"/>
    public enum TrickleDown
    {
        /// <summary>
        /// Execute the event handler during the BubbleUp propagation phase
        /// if the event bubbles and the element is a parent of the event's target,
        /// or if the callback is registered directly on the event's target.
        /// </summary>
        /// <remarks>
        /// Handlers that use <see cref="TrickleDown.TrickleDown"/> are executed before those that
        /// use <see cref="TrickleDown.NoTrickleDown"/>.
        /// </remarks>
        /// <seealso cref="EventBase.bubbles"/>
        /// <seealso cref="PropagationPhase.BubbleUp"/>
        NoTrickleDown = 0,
        /// <summary>
        /// Execute the event handler during the TrickleDown propagation phase
        /// if the event tricklesDown and the element is a parent of the event's target,
        /// or if the callback is registered directly on the event's target.
        /// </summary>
        /// <remarks>
        /// Handlers that use <see cref="TrickleDown.TrickleDown"/> are executed before those
        /// that use <see cref="TrickleDown.NoTrickleDown"/>.
        /// </remarks>
        /// <seealso cref="EventBase.tricklesDown"/>
        /// <seealso cref="PropagationPhase.TrickleDown"/>
        TrickleDown = 1
    }

    [Flags]
    internal enum InvokePolicy
    {
        /// <summary>
        /// No option enabled.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Callback reacts during the TrickleDown phase. If not set, callback reacts during the BubbleUp phase.
        /// </summary>
        TrickleDown = 1,
        /// <summary>
        /// Callback executes on disabled target even if the event has <see cref="EventBase.skipDisabledElements"/>.
        /// </summary>
        IncludeDisabled = 2,
        /// <summary>
        /// Callback is unregistered automatically after being successfully executed.
        /// </summary>
        Once = 4,
        /// <summary>
        /// Callback is a temporary object created from a delegate and can only be accessed by the event registry that
        /// created it. That callback can safely be released when unregistered or when the registry is disposed.
        /// </summary>
        Local = 8,
    }

    internal class EventCallbackListPool
    {
        private readonly ObjectPool<EventCallbackList> m_Items = new(() => new(), EventCallbackRegistry.k_PoolMaxSize);

        [NotNull] public EventCallbackList Get()
        {
            return m_Items.Get();
        }

        [NotNull] public EventCallbackList Get([NotNull] EventCallbackList initializer)
        {
            var element = m_Items.Get();
            element.AddRange(initializer);
            return element;
        }

        public void Release(EventCallbackList element)
        {
            element.Clear();
            m_Items.Release(element);
        }
    }

    internal class EventCallbackList
    {
        public static readonly EventCallbackList EmptyList = new EventCallbackList();

        private EventCallbackInternal[] m_Array = Array.Empty<EventCallbackInternal>();
        private int m_Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Delegate callback, long eventTypeId)
        {
            return Find(callback, eventTypeId) >= 0;
        }

        public int FindGroup(EventCallbackInternal callback)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i] == callback)
                {
                    return i;
                }
            }
            return -1;
        }

        public int Find(EventCallbackInternal callback)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i] == callback)
                {
                    return i;
                }
            }
            return -1;
        }

        public int Find(Delegate callback, long eventTypeId)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i].eventTypeId == eventTypeId && m_Array[i].userCallback == callback)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool RemoveGroupAt(int i, int count)
        {
            m_Count -= count;
            Array.Copy(m_Array, i + count, m_Array, i, m_Count - i);
            Array.Clear(m_Array, m_Count, count);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int i)
        {
            m_Count--;
            Array.Copy(m_Array, i + 1, m_Array, i, m_Count - i);
            m_Array[m_Count] = default;
        }

        public void Add(EventCallbackInternal item)
        {
            if (m_Count >= m_Array.Length)
                Array.Resize(ref m_Array, Mathf.NextPowerOfTwo(m_Count + 4)); // size goes 0, 4, 8, 16, etc.
            m_Array[m_Count++] = item;
        }

        public void AddRange(EventCallbackInternal[] list, int count)
        {
            if (m_Count + count > m_Array.Length)
                Array.Resize(ref m_Array, Mathf.NextPowerOfTwo(m_Count + count));
            Array.Copy(list, 0, m_Array, m_Count, count);
            m_Count += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(EventCallbackList list) => AddRange(list.m_Array, list.m_Count);

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Count;
        }

        public Span<EventCallbackInternal> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(m_Array, 0, m_Count);
        }

        public ref EventCallbackInternal this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Array[index];
        }

        public void Clear()
        {
            Array.Clear(m_Array, 0, m_Count);
            m_Count = 0;
            // Don't un-assign m_Array, we're going to reuse it when the list is put in the pool again.
        }
    }

    internal class EventCallbackPool
    {
        // A max pool size of 1024 is not a lot considering the amount of elements and callbacks that don't use the
        // EventCallbackDefinition and EventCallbackGroup apis at the moment. We can bring this number down a bit when they do.
        internal const int k_PoolMaxSize = 1024;

        // Pool is modified by perf tests to exclude the allocation costs from the measurements.
        internal static ObjectPool<EventCallbackInternal> s_Pool = new(() => new(), k_PoolMaxSize);

        public EventCallbackInternal Get<TEvent>(Delegate userCallback, int argId, InvokePolicy invokePolicy, TrickleDown trickleDown)
            where TEvent : EventBase<TEvent>, new()
        {
            var c = s_Pool.Get();
            c.Reset<TEvent>(userCallback, argId, invokePolicy, trickleDown);
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release(EventCallbackInternal c)
        {
            c.Reset();
            s_Pool.Release(c);
        }
    }

    internal class EventCallbackRegistry
    {
        public const int k_PoolMaxSize = 1024;

        private static readonly EventCallbackListPool k_ListPool = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull] private static EventCallbackList GetCallbackList()
        {
            return k_ListPool.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull] private static EventCallbackList GetCallbackList([NotNull] EventCallbackList initializer)
        {
            return k_ListPool.Get(initializer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseCallbackList(EventCallbackList toRelease)
        {
            k_ListPool.Release(toRelease);
        }

        internal struct DynamicCallbackList
        {
            [NotNull] private EventCallbackList m_Callbacks;
            [CanBeNull] private EventCallbackList m_TemporaryCallbacks;

            private int m_IsInvoking;
            public bool isInvoking
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_IsInvoking != 0;
            }

            public int Count => m_Callbacks.Count;

            public static DynamicCallbackList Create()
            {
                return new DynamicCallbackList
                {
                    m_Callbacks = EventCallbackList.EmptyList,
                    m_TemporaryCallbacks = null,
                    m_IsInvoking = 0
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [NotNull] public EventCallbackList GetCallbackListForWriting()
            {
                if (m_IsInvoking == 0)
                    return m_Callbacks != EventCallbackList.EmptyList ? m_Callbacks : m_Callbacks = GetCallbackList();
                return m_TemporaryCallbacks ??= GetCallbackList(m_Callbacks);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [NotNull] public readonly EventCallbackList GetCallbackListForReading()
            {
                return m_TemporaryCallbacks ?? m_Callbacks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [NotNull] public EventCallbackList GetCallbackListForRemoving()
            {
                return m_IsInvoking == 0 ? m_Callbacks : m_TemporaryCallbacks ??= GetCallbackList(m_Callbacks);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [NotNull] public EventCallbackList GetCallbackListForRemovingOutsideInvoke()
            {
                return m_Callbacks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [NotNull] public EventCallbackList GetCallbackListForRemovingDuringInvoke()
            {
                return m_TemporaryCallbacks ??= GetCallbackList(m_Callbacks);
            }

            public void Invoke(EventBase evt, BaseVisualElementPanel panel, VisualElement target, EventCallbackRegistry registry)
            {
                BeginInvoke();
                try
                {
                    // Some callbacks require an enabled target. For uniformity, we don't update this between calls.
                    var enabled = !evt.skipDisabledElements || target.enabledInHierarchy;
                    var eventTypeId = evt.eventTypeId;
                    foreach (var callback in m_Callbacks.Span)
                    {
                        if (callback.eventTypeId == eventTypeId && target.elementPanel == panel &&
                            (enabled || (callback.invokePolicy & InvokePolicy.IncludeDisabled) != 0))
                        {
                            // Unregister once callbacks before invoke so they can be re-registered if needed
                            if ((callback.invokePolicy & InvokePolicy.Once) != 0)
                            {
                                // We are removing in m_TemporaryCallbacks, so we can't use the index from m_Callbacks
                                m_TemporaryCallbacks ??= GetCallbackList(m_Callbacks);
                                int removeIndex = m_TemporaryCallbacks.Find(callback);
                                if (removeIndex >= 0)
                                {
                                    if ((callback.invokePolicy & InvokePolicy.Local) != 0)
                                        k_UnregisteredLocalCallbacksDuringInvoke.Add((registry, callback));
                                    m_TemporaryCallbacks.RemoveAt(removeIndex);
                                }
                            }

                            var invoker = registry.GetEventInvoker(callback.argId, target);
                            if (invoker != null)
                                invoker.Invoke(evt, callback);
                            else
                                Debug.LogWarning($"Callback invoked with unregistered argument for its target. Ignoring. userCallback:{callback.userCallback}, target:{target}, argument ID:{callback.argId}");

                            if (evt.isImmediatePropagationStopped)
                                break;
                        }
                    }
                }
                finally
                {
                    EndInvoke();
                }
            }

            public void Clear()
            {
                if (m_Callbacks == EventCallbackList.EmptyList)
                    return;
                ReleaseCallbackList(m_Callbacks);
                m_Callbacks = EventCallbackList.EmptyList;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void BeginInvoke()
            {
                m_IsInvoking++;
                s_GlobalInvokeCount++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EndInvoke()
            {
                m_IsInvoking--;
                s_GlobalInvokeCount--;

                if (m_IsInvoking == 0)
                {
                    // If callbacks were modified during callback invocation, update them now.
                    if (m_TemporaryCallbacks != null)
                    {
                        if (m_Callbacks != EventCallbackList.EmptyList)
                            ReleaseCallbackList(m_Callbacks);
                        m_Callbacks = GetCallbackList(m_TemporaryCallbacks);
                        ReleaseCallbackList(m_TemporaryCallbacks);
                        m_TemporaryCallbacks = null;
                    }

                    // If callbacks were removed during invocation, they can now be safely disposed
                    if (s_GlobalInvokeCount == 0 && k_UnregisteredLocalCallbacksDuringInvoke.Count > 0)
                    {
                        foreach (var c in k_UnregisteredLocalCallbacksDuringInvoke)
                            c.registry.ReleaseLocalCallback(c.callback);
                        k_UnregisteredLocalCallbacksDuringInvoke.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Each event registry has a list of values associated with EventArg ids.
        /// While EventArg ids produced with EventArg.Create are unique and permanent and can be reused between
        /// multiple elements and callbacks, the EventArgValues are per element and disposed with the event registry.
        /// </summary>
        /// <seealso cref="EventArg{TArg}"/>
        private struct ArgValueList
        {
            private EventArgValue m_FirstArg;
            private int m_PrevLocalArgId;

            public EventArgValue Find(int argId)
            {
                for (var arg = m_FirstArg; arg != null; arg = arg.nextArg)
                    if (arg.argId == argId)
                        return arg;
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool FindByValue<TArg>(in TArg value, out EventArgValue arg)
            {
                for (arg = m_FirstArg; arg != null; arg = arg.nextArg)
                    if (arg.ValueEquals(in value))
                        return true;
                return false;
            }

            public bool Remove(int argId)
            {
                EventArgValue prevArg = null;
                for (var arg = m_FirstArg; arg != null; prevArg = arg, arg = arg.nextArg)
                    if (arg.argId == argId)
                    {
                        if (prevArg != null) prevArg.nextArg = arg.nextArg; else m_FirstArg = arg.nextArg;
                        arg.Dispose();
                        return true;
                    }
                return false;
            }

            public void Add<TArg>(int argId, in TArg value)
            {
                Append(EventArgValueFactory<TArg>.GetPooled(argId, in value));
            }

            public int AcquireTemporary<TArg>(in TArg value)
            {
                // Identical args are very common. Collapse the temp args as much as possible.
                if (FindByValue(in value, out var arg))
                {
                    arg.temporaryCount++;
                    return arg.argId;
                }

                int argId = GenerateLocallyUniqueTemporaryId();
                arg = EventArgValueFactory<TArg>.GetPooled(argId, in value);
                arg.temporaryCount = 1;
                Append(arg);
                return argId;
            }

            public void ReleaseTemporary(int argId)
            {
                EventArgValue prevArg = null;
                for (var arg = m_FirstArg; arg != null; prevArg = arg, arg = arg.nextArg)
                    if (arg.argId == argId)
                    {
                        if (--arg.temporaryCount == 0)
                        {
                            if (prevArg != null) prevArg.nextArg = arg.nextArg; else m_FirstArg = arg.nextArg;
                            arg.Dispose();
                        }
                        break;
                    }
            }

            public void Clear()
            {
                while (m_FirstArg != null)
                {
                    var nextArg = m_FirstArg.nextArg;
                    m_FirstArg.Dispose();
                    m_FirstArg = nextArg;
                }

                m_PrevLocalArgId = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Append(EventArgValue argValue)
            {
                argValue.nextArg = m_FirstArg;
                m_FirstArg = argValue;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GenerateLocallyUniqueTemporaryId()
            {
                // Generate an Id for a temporary arg. The Id doesn't need to be unique, as long as it's unique in
                // this event registry's arg list.  Here we use negative numbers because they're sure to not collide
                // with any Id created from EventArg.Create.
                return --m_PrevLocalArgId;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsTemporaryId(int argId)
            {
                return argId < 0;
            }
        }

        private ArgValueList m_ArgValues;

        internal IEventInvoker GetEventInvoker(int argId, VisualElement target)
        {
            if (argId == EventArgId.None) return EventArgValue.None;
            if (argId == EventArgId.Self) return EventArgValue.Self(target);
            return m_ArgValues.Find(argId);
        }

        internal DynamicCallbackList m_TrickleDownCallbacks = DynamicCallbackList.Create();
        internal DynamicCallbackList m_BubbleUpCallbacks = DynamicCallbackList.Create();

        // It's important to always call this with `ref`, otherwise we'll mutate a copy of the struct.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref DynamicCallbackList GetDynamicCallbackList(TrickleDown useTrickleDown)
        {
            return ref useTrickleDown == TrickleDown.TrickleDown ? ref m_TrickleDownCallbacks : ref m_BubbleUpCallbacks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterArg<TArg>(int argId, in TArg value)
        {
            m_ArgValues.Add(argId, in value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnregisterArg(int argId)
        {
            return m_ArgValues.Remove(argId);
        }

        internal static readonly EventCallbackPool k_LocalCallbackPool = new();
        internal static readonly List<(EventCallbackRegistry registry, EventCallbackInternal callback)>
            k_UnregisteredLocalCallbacksDuringInvoke = new();
        internal static int s_GlobalInvokeCount;
        private bool m_HasLocalCallbacks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterCallback<TEventType>([NotNull] EventCallback<TEventType> userCallback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, InvokePolicy invokePolicy = default)
            where TEventType : EventBase<TEventType>, new()
        {
            _RegisterLocalCallback(k_LocalCallbackPool.Get<TEventType>(userCallback, EventArgId.None,
                invokePolicy | InvokePolicy.Local, useTrickleDown));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterCallback<TEventType, TCallbackArgs>(
            [NotNull] EventCallback<TEventType, TCallbackArgs> userCallback, CallbackEventHandler element,
            TCallbackArgs userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown,
            InvokePolicy invokePolicy = default)
            where TEventType : EventBase<TEventType>, new()
        {
            _RegisterLocalCallback(k_LocalCallbackPool.Get<TEventType>(userCallback,
                m_ArgValues.AcquireTemporary(in userArgs), invokePolicy | InvokePolicy.Local, useTrickleDown));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterCallback<TEventType, TCallbackArgs>(
            [NotNull] EventCallback<TEventType, TCallbackArgs> userCallback, CallbackEventHandler element,
            int argId, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown,
            InvokePolicy invokePolicy = default)
            where TEventType : EventBase<TEventType>, new()
        {
            _RegisterLocalCallback(k_LocalCallbackPool.Get<TEventType>(userCallback, argId,
                invokePolicy | InvokePolicy.Local, useTrickleDown));
        }

        private void _RegisterLocalCallback(EventCallbackInternal callback)
        {
            m_HasLocalCallbacks = true;

            ref var dynamicCallbackList = ref GetDynamicCallbackList(callback.useTrickleDown);

            EventCallbackList callbackList = dynamicCallbackList.GetCallbackListForReading();
            var index = callbackList.Find(callback.userCallback, callback.eventTypeId);
            if (index >= 0)
            {
                var oldCallback = callbackList[index];
                if ((oldCallback.invokePolicy & InvokePolicy.Local) != 0)
                {
                    if (dynamicCallbackList.isInvoking)
                        k_UnregisteredLocalCallbacksDuringInvoke.Add((this, oldCallback));
                    else
                        ReleaseLocalCallback(oldCallback);
                }
                // Don't modify the callback directly, as it may be in the temporary list or shared!
                callbackList[index] = callback;
                return;
            }

            dynamicCallbackList.GetCallbackListForWriting().Add(callback);
        }

        public void RegisterCallback(EventCallbackInternal callback)
        {
            // For performance considerations, re-registering callback to modify the argument or policy isn't supported
            // with GlobalEventCallbacks. Use UnregisterCallback then RegisterCallback to achieve the same result.
            GetDynamicCallbackList(callback.useTrickleDown)
                .GetCallbackListForWriting()
                .Add(callback);
        }

        // Return value is used for unit tests only
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnregisterCallback<TEventType>([NotNull] EventCallback<TEventType> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            return _UnregisterLocalCallback(callback, EventBase<TEventType>.TypeId(), useTrickleDown);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UnregisterCallback<TEventType, TCallbackArgs>(
            [NotNull] EventCallback<TEventType, TCallbackArgs> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            return _UnregisterLocalCallback(callback, EventBase<TEventType>.TypeId(), useTrickleDown);
        }

        public bool UnregisterCallback(EventCallbackInternal callback)
        {
            var callbackList = GetDynamicCallbackList(callback.useTrickleDown).GetCallbackListForRemoving();
            int functorIndex = callbackList.Find(callback);
            if (functorIndex >= 0)
            {
                Debug.Assert((callbackList[functorIndex].invokePolicy & InvokePolicy.Local) == 0, "(callbackList[functorIndex].invokePolicy & InvokePolicy.Local) == 0");
                callbackList.RemoveAt(functorIndex);
                return true;
            }

            return false;
        }

        private bool _UnregisterLocalCallback(Delegate userCallback, long eventTypeId, TrickleDown useTrickleDown)
        {
            ref var dynamicCallbackList = ref GetDynamicCallbackList(useTrickleDown);
            if (dynamicCallbackList.isInvoking)
            {
                var callbackList = dynamicCallbackList.GetCallbackListForRemovingDuringInvoke();
                int index = callbackList.Find(userCallback, eventTypeId);
                if (index < 0)
                    return false;

                var callback = callbackList[index];
                if ((callback.invokePolicy & InvokePolicy.Local) != 0)
                    k_UnregisteredLocalCallbacksDuringInvoke.Add((this, callback));
                callbackList.RemoveAt(index);
            }
            else
            {
                var callbackList = dynamicCallbackList.GetCallbackListForRemovingOutsideInvoke();
                int index = callbackList.Find(userCallback, eventTypeId);
                if (index < 0)
                    return false;

                var callback = callbackList[index];
                if ((callback.invokePolicy & InvokePolicy.Local) != 0)
                    ReleaseLocalCallback(callback);
                callbackList.RemoveAt(index);
            }
            return true;
        }

        private bool UnregisterCallbackDuringInvoke(EventCallbackList callbackList, Delegate callback, long eventTypeId)
        {
            int index = callbackList.Find(callback, eventTypeId);
            if (index >= 0)
            {
                var oldCallback = callbackList[index];
                if ((oldCallback.invokePolicy & InvokePolicy.Local) != 0)
                    k_UnregisteredLocalCallbacksDuringInvoke.Add((this, oldCallback));
                callbackList.RemoveAt(index);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseLocalCallback(EventCallbackInternal c)
        {
            int argId = c.argId;
            if (ArgValueList.IsTemporaryId(argId))
                m_ArgValues.ReleaseTemporary(argId);
            k_LocalCallbackPool.Release(c);
        }

        // For unit tests only
        internal void InvokeCallbacks(EventBase evt, PropagationPhase propagationPhase)
        {
            var target = (VisualElement) evt.currentTarget;
            var panel = target.elementPanel;

            switch (propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                    InvokeCallbacksTrickleDown(evt, panel, target);
                    break;
                case PropagationPhase.BubbleUp:
                    InvokeCallbacksBubbleUp(evt, panel, target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(propagationPhase),
                        "Propagation phases other than TrickleDown and BubbleUp are not supported");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeCallbacksTrickleDown(EventBase evt, BaseVisualElementPanel panel, VisualElement target)
        {
            m_TrickleDownCallbacks.Invoke(evt, panel, target, this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeCallbacksBubbleUp(EventBase evt, BaseVisualElementPanel panel, VisualElement target)
        {
            m_BubbleUpCallbacks.Invoke(evt, panel, target, this);
        }

        public void Clear()
        {
            if (m_HasLocalCallbacks)
            {
                foreach (var c in m_BubbleUpCallbacks.GetCallbackListForReading().Span)
                    if ((c.invokePolicy & InvokePolicy.Local) != 0)
                        k_LocalCallbackPool.Release(c);
                foreach (var c in m_TrickleDownCallbacks.GetCallbackListForReading().Span)
                    if ((c.invokePolicy & InvokePolicy.Local) != 0)
                        k_LocalCallbackPool.Release(c);
                m_HasLocalCallbacks = false;
            }

            m_ArgValues.Clear();
            m_BubbleUpCallbacks.Clear();
            m_TrickleDownCallbacks.Clear();
        }

        public bool HasTrickleDownHandlers()
        {
            return m_TrickleDownCallbacks.Count > 0;
        }

        public bool HasBubbleHandlers()
        {
            return m_BubbleUpCallbacks.Count > 0;
        }

        // For unit tests
        internal bool ContainsArgValue<TArg>(TArg value)
        {
            return m_ArgValues.FindByValue(in value, out _);
        }

        private static readonly ObjectPool<EventCallbackRegistry> k_RegistryPool = new(() => new(), k_PoolMaxSize);
        public static EventCallbackRegistry GetPooled() => k_RegistryPool.Get();
        public void Dispose() => k_RegistryPool.Release(this);
    }
}
