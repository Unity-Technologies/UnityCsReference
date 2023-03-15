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

    internal enum CallbackPhase
    {
        BubbleUp = 0,
        TrickleDown = 1
    }

    internal enum InvokePolicy
    {
        Default = default,
        IncludeDisabled
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

        public bool Contains(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            return Find(eventTypeId, callback, phase) != null;
        }

        public EventCallbackFunctorBase Find(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i].IsEquivalentTo(eventTypeId, callback, phase))
                {
                    return m_Array[i];
                }
            }
            return null;
        }

        public bool Remove(long eventTypeId, Delegate callback, CallbackPhase phase)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Array[i].IsEquivalentTo(eventTypeId, callback, phase))
                {
                    m_Count--;
                    Array.Copy(m_Array, i+1, m_Array, i, m_Count-i);
                    m_Array[m_Count] = default;
                    return true;
                }
            }
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
            [NotNull] private EventCallbackList m_Callbacks;
            [CanBeNull] private EventCallbackList m_TemporaryCallbacks;
            private int m_IsInvoking;

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
                            (enabled || callback.invokePolicy == InvokePolicy.IncludeDisabled))
                        {
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
                    }
                }
            }
        }

        internal DynamicCallbackList m_TrickleDownCallbacks = DynamicCallbackList.Create();
        internal DynamicCallbackList m_BubbleUpCallbacks = DynamicCallbackList.Create();

        ref DynamicCallbackList GetDynamicCallbackList(CallbackPhase phase)
        {
            return ref phase == CallbackPhase.TrickleDown ? ref m_TrickleDownCallbacks : ref m_BubbleUpCallbacks;
        }

        private bool UnregisterCallback(long eventTypeId, [NotNull] Delegate callback, TrickleDown useTrickleDown)
        {
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDown : CallbackPhase.BubbleUp;
            ref var dynamicCallbackList = ref GetDynamicCallbackList(callbackPhase);
            EventCallbackList callbackList = dynamicCallbackList.GetCallbackListForWriting();
            return callbackList.Remove(eventTypeId, callback, callbackPhase);
        }

        public void RegisterCallback<TEventType>([NotNull] EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, InvokePolicy invokePolicy = default) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDown : CallbackPhase.BubbleUp;
            ref var dynamicCallbackList = ref GetDynamicCallbackList(callbackPhase);

            EventCallbackList callbackList = dynamicCallbackList.GetCallbackListForReading();
            if (callbackList.Contains(eventTypeId, callback, callbackPhase))
                return;

            callbackList = dynamicCallbackList.GetCallbackListForWriting();
            callbackList.Add(new EventCallbackFunctor<TEventType>(callback, callbackPhase, invokePolicy));
        }

        public void RegisterCallback<TEventType, TCallbackArgs>([NotNull] EventCallback<TEventType, TCallbackArgs> callback, TCallbackArgs userArgs, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown, InvokePolicy invokePolicy = default) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            var callbackPhase = useTrickleDown == TrickleDown.TrickleDown ? CallbackPhase.TrickleDown : CallbackPhase.BubbleUp;
            ref var dynamicCallbackList = ref GetDynamicCallbackList(callbackPhase);

            EventCallbackList callbackList = dynamicCallbackList.GetCallbackListForReading();
            if (callbackList.Find(eventTypeId, callback, callbackPhase) is EventCallbackFunctor<TEventType, TCallbackArgs> functor)
            {
                functor.userArgs = userArgs;
                return;
            }

            callbackList = dynamicCallbackList.GetCallbackListForWriting();
            callbackList.Add(new EventCallbackFunctor<TEventType, TCallbackArgs>(callback, userArgs, callbackPhase, invokePolicy));
        }

        // Return value is used for unit tests only
        public bool UnregisterCallback<TEventType>([NotNull] EventCallback<TEventType> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            return UnregisterCallback(eventTypeId, callback, useTrickleDown);
        }

        public bool UnregisterCallback<TEventType, TCallbackArgs>([NotNull] EventCallback<TEventType, TCallbackArgs> callback, TrickleDown useTrickleDown = TrickleDown.NoTrickleDown) where TEventType : EventBase<TEventType>, new()
        {
            long eventTypeId = EventBase<TEventType>.TypeId();
            return UnregisterCallback(eventTypeId, callback, useTrickleDown);
        }

        // For unit tests only
        internal void InvokeCallbacks(EventBase evt, PropagationPhase propagationPhase)
        {
            var target = (VisualElement) evt.currentTarget;
            var panel = target.elementPanel;
            if (propagationPhase == PropagationPhase.TrickleDown)
                InvokeCallbacks(evt, panel, target, CallbackPhase.TrickleDown);
            else if (propagationPhase == PropagationPhase.BubbleUp)
                InvokeCallbacks(evt, panel, target, CallbackPhase.BubbleUp);
        }

        public void InvokeCallbacks(EventBase evt, [NotNull] BaseVisualElementPanel panel,
            [NotNull] VisualElement target, CallbackPhase phase)
        {
            ref var dynamicCallbackList = ref GetDynamicCallbackList(phase);
            dynamicCallbackList.Invoke(evt, panel, target);
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
