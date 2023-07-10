// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Use this enum to specify during which phases the event handler is executed.
    /// </summary>
    public enum TrickleDown
    {
        /// <summary>
        /// The event handler should be executed during the AtTarget and BubbleUp phases.
        /// </summary>
        NoTrickleDown = 0,
        /// <summary>
        /// The event handler should be executed during the AtTarget and TrickleDown phases.
        /// </summary>
        TrickleDown = 1
    }

    [Flags]
    internal enum InvokePolicy
    {
        Default = 0,
        IncludeDisabled = 1,
        Once = 2
    }

    internal class EventCallbackListPool
    {
        readonly Stack<EventCallbackList> m_Stack = new Stack<EventCallbackList>();

        public EventCallbackList Get(EventCallbackList initializer)
        {
            EventCallbackList element;
            if (m_Stack.Count == 0)
            {
                if (initializer != null)
                    element = new EventCallbackList(initializer);
                else
                    element = new EventCallbackList();
            }
            else
            {
                element = m_Stack.Pop();
                if (initializer != null)
                    element.AddRange(initializer);
            }
            return element;
        }

        public void Release(EventCallbackList element)
        {
            element.Clear();
            m_Stack.Push(element);
        }
    }

    internal class EventCallbackList
    {
        public static readonly EventCallbackList EmptyList = new EventCallbackList();
        private static readonly EventCallbackFunctorBase[] EmptyArray = new EventCallbackFunctorBase[0];

        private EventCallbackFunctorBase[] m_Array;
        private int m_Count;

        public EventCallbackList()
        {
            m_Array = EmptyArray;
        }

        public EventCallbackList(EventCallbackList source)
        {
            m_Count = source.m_Count;
            m_Array = new EventCallbackFunctorBase[m_Count];
            Array.Copy(source.m_Array, m_Array, m_Count);
        }

        public bool Contains(long eventTypeId, [NotNull] Delegate callback)
        {
            return Find(eventTypeId, callback) != null;
        }

        public EventCallbackFunctorBase Find(long eventTypeId, [NotNull] Delegate callback)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i].IsEquivalentTo(eventTypeId, callback))
                {
                    return m_Array[i];
                }
            }
            return null;
        }

        public bool Remove(long eventTypeId, [NotNull] Delegate callback, out EventCallbackFunctorBase removedFunctor)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i].IsEquivalentTo(eventTypeId, callback))
                {
                    removedFunctor = m_Array[i];
                    m_Count--;
                    Array.Copy(m_Array, i+1, m_Array, i, m_Count-i);
                    m_Array[m_Count] = default;
                    return true;
                }
            }

            removedFunctor = null;
            return false;
        }

        public void Add(EventCallbackFunctorBase item)
        {
            if (m_Count >= m_Array.Length)
                Array.Resize(ref m_Array, Mathf.NextPowerOfTwo(m_Count + 4)); // size goes 0, 4, 8, 16, etc.
            m_Array[m_Count++] = item;
        }

        public void AddRange(EventCallbackList list)
        {
            if (m_Count + list.m_Count > m_Array.Length)
                Array.Resize(ref m_Array, Mathf.NextPowerOfTwo(m_Count + list.m_Count));
            Array.Copy(list.m_Array, 0, m_Array, m_Count, list.m_Count);
            m_Count += list.m_Count;
        }

        public int Count => m_Count;

        public Span<EventCallbackFunctorBase> Span => new Span<EventCallbackFunctorBase>(m_Array, 0, m_Count);

        public EventCallbackFunctorBase this[int i]
        {
            get { return m_Array[i]; }
            set { m_Array[i] = value; }
        }

        public void Clear()
        {
            Array.Clear(m_Array, 0, m_Count);
            m_Count = 0;
        }
    }

    internal class EventCallbackRegistry
    {
        private static readonly EventCallbackListPool s_ListPool = new EventCallbackListPool();

        private static EventCallbackList GetCallbackList(EventCallbackList initializer = null)
        {
            return s_ListPool.Get(initializer);
        }

        private static void ReleaseCallbackList(EventCallbackList toRelease)
        {
            s_ListPool.Release(toRelease);
        }

        internal struct DynamicCallbackList
        {
            private TrickleDown m_UseTrickleDown;
            [NotNull] private EventCallbackList m_Callbacks;
            [CanBeNull] private EventCallbackList m_TemporaryCallbacks;
            [CanBeNull] private List<EventCallbackFunctorBase> m_UnregisteredCallbacksDuringInvoke;
            private int m_IsInvoking;

            public int Count => m_Callbacks.Count;

            public static DynamicCallbackList Create(TrickleDown useTrickleDown)
            {
                return new DynamicCallbackList
                {
                    m_UseTrickleDown = useTrickleDown,
                    m_Callbacks = EventCallbackList.EmptyList,
                    m_TemporaryCallbacks = null,
                    m_UnregisteredCallbacksDuringInvoke = null,
                    m_IsInvoking = 0
                };
            }

            [NotNull] public EventCallbackList GetCallbackListForWriting()
            {
                return m_IsInvoking == 0
                    ? (m_Callbacks != EventCallbackList.EmptyList ? m_Callbacks : m_Callbacks = GetCallbackList())
                    : (m_TemporaryCallbacks ??= GetCallbackList(m_Callbacks));
            }

            [NotNull] public readonly EventCallbackList GetCallbackListForReading()
            {
                return m_TemporaryCallbacks ?? m_Callbacks;
            }

            public bool UnregisterCallback(long eventTypeId, [NotNull] Delegate callback)
            {
                EventCallbackList callbackList = GetCallbackListForWriting();
                if (!callbackList.Remove(eventTypeId, callback, out var functor))
                    return false;

                if (m_IsInvoking > 0)
                    (m_UnregisteredCallbacksDuringInvoke ??= ListPool<EventCallbackFunctorBase>.Get()).Add(functor);
                else
                    functor.Dispose();

                return true;
            }

            public void Invoke(EventBase evt, BaseVisualElementPanel panel, VisualElement target)
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
                                callback.UnregisterCallback(target, m_UseTrickleDown);

                            callback.Invoke(evt);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void BeginInvoke()
            {
                m_IsInvoking++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EndInvoke()
            {
                m_IsInvoking--;

                if (m_IsInvoking == 0)
                {
                    // If callbacks were modified during callback invocation, update them now.
                    if (m_TemporaryCallbacks != null)
                    {
                        if (m_Callbacks != EventCallbackList.EmptyList) ReleaseCallbackList(m_Callbacks);
                        m_Callbacks = GetCallbackList(m_TemporaryCallbacks);
                        ReleaseCallbackList(m_TemporaryCallbacks);
                        m_TemporaryCallbacks = null;

                        // If callbacks were removed during invocation, their functors can now be safely disposed
                        if (m_UnregisteredCallbacksDuringInvoke != null)
                        {
                            foreach (var functor in m_UnregisteredCallbacksDuringInvoke)
                                functor.Dispose();
                            ListPool<EventCallbackFunctorBase>.Release(m_UnregisteredCallbacksDuringInvoke);
                            m_UnregisteredCallbacksDuringInvoke = null;
                        }
                    }
                }
            }
        }

        internal DynamicCallbackList m_TrickleDownCallbacks = DynamicCallbackList.Create(TrickleDown.TrickleDown);
        internal DynamicCallbackList m_BubbleUpCallbacks = DynamicCallbackList.Create(TrickleDown.NoTrickleDown);

        // It's important to always call this with `ref`, otherwise we'll mutate a copy of the struct.
        private ref DynamicCallbackList GetDynamicCallbackList(TrickleDown useTrickleDown)
        {
            return ref useTrickleDown == TrickleDown.TrickleDown ? ref m_TrickleDownCallbacks : ref m_BubbleUpCallbacks;
        }

        public void RegisterCallback<TEventType>([NotNull] EventCallback<TEventType> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, InvokePolicy invokePolicy = default)
            where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            ref var dynamicCallbackList = ref GetDynamicCallbackList(useTrickleDown);

            EventCallbackList callbackList = dynamicCallbackList.GetCallbackListForReading();
            if (callbackList.Find(eventTypeId, callback) is EventCallbackFunctor<TEventType> functor)
            {
                functor.invokePolicy = invokePolicy;
                return;
            }

            callbackList = dynamicCallbackList.GetCallbackListForWriting();
            callbackList.Add(EventCallbackFunctor<TEventType>.GetPooled(eventTypeId, callback, invokePolicy));
        }

        public void RegisterCallback<TEventType, TCallbackArgs>(
            [NotNull] EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, InvokePolicy invokePolicy = default)
            where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            ref var dynamicCallbackList = ref GetDynamicCallbackList(useTrickleDown);

            EventCallbackList callbackList = dynamicCallbackList.GetCallbackListForReading();
            if (callbackList.Find(eventTypeId, callback) is EventCallbackFunctor<TEventType, TCallbackArgs> functor)
            {
                functor.invokePolicy = invokePolicy;
                functor.userArgs = userArgs;
                return;
            }

            callbackList = dynamicCallbackList.GetCallbackListForWriting();
            callbackList.Add(
                EventCallbackFunctor<TEventType, TCallbackArgs>.GetPooled(eventTypeId, callback, userArgs,
                    invokePolicy));
        }

        // Return value is used for unit tests only
        public bool UnregisterCallback<TEventType>([NotNull] EventCallback<TEventType> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            return GetDynamicCallbackList(useTrickleDown).UnregisterCallback(EventBase<TEventType>.TypeId(), callback);
        }

        public bool UnregisterCallback<TEventType, TCallbackArgs>(
            [NotNull] EventCallback<TEventType, TCallbackArgs> callback,
            TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            return GetDynamicCallbackList(useTrickleDown).UnregisterCallback(EventBase<TEventType>.TypeId(), callback);
        }

        // For unit tests only
        internal void InvokeCallbacks(EventBase evt, PropagationPhase propagationPhase)
        {
            var target = (VisualElement) evt.currentTarget;
            var panel = target.elementPanel;

            switch (propagationPhase)
            {
                case PropagationPhase.TrickleDown:
                    GetDynamicCallbackList(TrickleDown.TrickleDown).Invoke(evt, panel, target);
                    break;
                case PropagationPhase.BubbleUp:
                    GetDynamicCallbackList(TrickleDown.NoTrickleDown).Invoke(evt, panel, target);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(propagationPhase),
                        "Propagation phases other than TrickleDown and BubbleUp are not supported");
            }
        }

        public bool HasTrickleDownHandlers()
        {
            return m_TrickleDownCallbacks.Count > 0;
        }

        public bool HasBubbleHandlers()
        {
            return m_BubbleUpCallbacks.Count > 0;
        }
    }
}
