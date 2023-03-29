// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor.Profiling;

namespace UnityEditor
{
    /// <summary>
    /// Use this to enhance C# events with editor performance trackers. This can be used as a replacement of a C# event without changing
    /// the public API. Using editor performance trackers means that the pop-up progress bar will provide more helpful information.
    /// </summary>
    /// <remarks>
    /// Like C# events, adding and removing subscribers is thread-safe. Invoking the event however should only happen on the main thread
    /// (because of the performance trackers).
    ///
    /// Using this type will give your performance trackers sensible names and cache them. Like regular C# events, adding and removing
    /// subscribers may cause garbage allocations.
    /// </remarks>
    /// <example>
    /// <code>
    /// public static class Events {
    ///     public static event SomeDelegate MyEvent;
    ///
    ///     private static void InvokeEvent() {
    ///         MyEvent?.Invoke(1, 2, 3);
    ///     }
    /// }
    /// </code>
    /// can be rewritten to this:
    /// <code>
    /// public static class Events {
    ///     public static event SomeDelegate MyEvent {
    ///         add => m_MyEvent.Add(value);
    ///         remove => m_MyEvent.Remove(value);
    ///     }
    ///     private static EventWithPerformanceTracker&lt;SomeDelegate&gt; m_MyEvent =
    ///         new EventWithPerformanceTracker&lt;SomeDelegate&gt;($"{nameof(Events)}.{nameof(MyEvent)}");
    ///
    ///     private static void InvokeEvent() {
    ///         foreach (var evt in m_Event) {
    ///             // A performance tracker for this delegate is active here!
    ///             evt.Invoke(1, 2, 3);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="T">The delegate type for this event.</typeparam>
    struct EventWithPerformanceTracker<T> where T : Delegate
    {
        private readonly string m_Scope;
        // This is either a reference to a delegate or a reference to a list. This mirrors the implementation of
        // multicast delegates in .NET. This needs to be a class to make thread-safe add/remove work.
        private DelegateWithHandle m_Delegate;

        public EventWithPerformanceTracker(string scope)
        {
            m_Scope = scope;
            m_Delegate = null;
        }

        /// <summary>
        /// Returns whether this event has any subscribers.
        /// </summary>
        public bool hasSubscribers => m_Delegate != null && m_Delegate.isNonEmpty;

        /// <summary>
        /// Add a subscriber to this event. This is equivalent to using += on a C# event.
        /// </summary>
        /// <param name="value"></param>
        public void Add(T value)
        {
            // Thread-safe method to subscribe to this event. The implementation closely mirrors the compiler-generated code for events.
            // see https://sharplab.io/#v2:CYLg1APgAgTAjAWAFBQMwAJboMLoN7LpGYYCmAbqQHYAumcMAPAJa0B86AopbQLIDchYkKJpMAFnS8AFAEp8I9AF9kSoA===
            var del = m_Delegate;
            while (true)
            {
                var del2 = del;
                var value2 = DelegateWithHandle.Add(del2, value);
                del = Interlocked.CompareExchange(ref m_Delegate, value2, del2);
                if (del == del2)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Remove a subscriber from this event. This is equivalent to using -= on a C# event.
        /// </summary>
        /// <param name="value"></param>
        public void Remove(T value)
        {
            var del = m_Delegate;
            while (true)
            {
                var del2 = del;
                var value2 = DelegateWithHandle.Remove(del2, value);
                del = Interlocked.CompareExchange(ref m_Delegate, value2, del2);
                if (del == del2)
                {
                    break;
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        internal struct Enumerator : IEnumerator<T>
        {
            private readonly DelegateWithHandle m_Delegate;
            private readonly string m_Scope;
            private readonly int m_Length;
            private int m_ActivePerformanceTracker;
            private int m_CurrentIndex;

            internal Enumerator(EventWithPerformanceTracker<T> evt)
            {
                m_Length = evt.m_Delegate?.length ?? 0;
                m_Scope = evt.m_Scope;
                m_ActivePerformanceTracker = -1;
                m_CurrentIndex = -1;
                m_Delegate = evt.m_Delegate;
            }

            public void Dispose()
            {
                if (m_ActivePerformanceTracker >= 0)
                    EditorPerformanceTracker.StopTracker(m_ActivePerformanceTracker);
            }

            public bool MoveNext()
            {
                if (m_ActivePerformanceTracker >= 0)
                    EditorPerformanceTracker.StopTracker(m_ActivePerformanceTracker);
                m_CurrentIndex++;
                m_ActivePerformanceTracker = m_CurrentIndex < m_Length ? m_Delegate.StartTrackerAt(m_CurrentIndex, m_Scope) : -1;
                return m_CurrentIndex < m_Length;
            }

            public void Reset() => throw new NotImplementedException();

            public T Current => m_Delegate.GetDelegateAt(m_CurrentIndex);

            object IEnumerator.Current => (this as IEnumerator<T>).Current;
        }

        internal static EventWithPerformanceTracker.Entry BuildDelegate(string scope, T del)
        {
            if (del == null)
            {
                return default;
            }

            var ls = del.GetInvocationList();
            switch (ls.Length)
            {
                case 0:
                    return default;
                case 1:
                    return new EventWithPerformanceTracker.Entry(del, EventWithPerformanceTracker.GetPerformanceTrackerHandleForEvent(scope, del));
                default:
                {
                    var entries = new EventWithPerformanceTracker.Entry[ls.Length];
                    for (int i = 0; i < entries.Length; i++)
                        entries[i] = new EventWithPerformanceTracker.Entry(ls[i] as T, EventWithPerformanceTracker.GetPerformanceTrackerHandleForEvent(scope, ls[i]));
                    return new EventWithPerformanceTracker.Entry(entries);
                }
            }
        }

        private class DelegateWithHandle
        {
            private EventWithPerformanceTracker.Entry m_DelegateOrList;

            private DelegateWithHandle() {}

            private DelegateWithHandle(EventWithPerformanceTracker.Entry entry)
            {
                m_DelegateOrList = entry;
            }

            public bool isNonEmpty => m_DelegateOrList.Reference != null;

            internal int length => m_DelegateOrList.length;

            internal int StartTrackerAt(int idx, string scope)
            {
                // We have to lazily initialize the tracker because subscription might happen from any thread
                // but we can only create editor performance trackers from the main thread.
                ref var handle = ref m_DelegateOrList.Reference is EventWithPerformanceTracker.Entry[] list
                    ? ref list[idx].EditorPerformanceTrackerHandle
                    : ref m_DelegateOrList.EditorPerformanceTrackerHandle;

                if (handle == EditorPerformanceTracker.InvalidTrackerHandle)
                    handle = EventWithPerformanceTracker.GetPerformanceTrackerHandleForEvent(scope, GetDelegateAt(idx));
                EditorPerformanceTracker.TryStartTrackerByHandle(handle, out var token);
                return token;
            }

            internal T GetDelegateAt(int idx) => (m_DelegateOrList.Reference is EventWithPerformanceTracker.Entry[] list ? list[idx].Reference : m_DelegateOrList.Reference) as T;

            internal static DelegateWithHandle Add(DelegateWithHandle dwh, T del)
            {
                if (dwh == null)
                {
                    return new DelegateWithHandle(new EventWithPerformanceTracker.Entry(del));
                }
                switch (dwh.m_DelegateOrList.Reference)
                {
                    case null:
                        return new DelegateWithHandle(new EventWithPerformanceTracker.Entry(del));
                    case EventWithPerformanceTracker.Entry[] list:
                    {
                        // This may seem wasteful, but mirrors the C# implementation of delegates. This is important
                        // to make thread-safe events work without locking, and also to support re-entrant events.
                        var newList = new EventWithPerformanceTracker.Entry[list.Length + 1];
                        int n = list.Length;
                        for (int i = 0; i < n; i++)
                            newList[i] = list[i];
                        newList[n] = new EventWithPerformanceTracker.Entry(del);
                        return new DelegateWithHandle(new EventWithPerformanceTracker.Entry(newList));
                    }
                    default:
                    {
                        var newList = new EventWithPerformanceTracker.Entry[2];
                        newList[0] = dwh.m_DelegateOrList;
                        newList[1] = new EventWithPerformanceTracker.Entry(del);
                        return new DelegateWithHandle(new EventWithPerformanceTracker.Entry(newList));
                    }
                }
            }

            private static DelegateWithHandle RemoveAt(EventWithPerformanceTracker.Entry[] array, int idx)
            {
                if (array.Length == 1)
                    return null;
                if (array.Length == 2)
                {
                    return new DelegateWithHandle(array[1 - idx]);
                }

                int n = array.Length;
                var newArray = new EventWithPerformanceTracker.Entry[n - 1];
                for (int k = 0; k < idx; k++)
                {
                    newArray[k] = array[k];
                }

                for (int k = idx + 1; k < n; k++)
                {
                    newArray[k - 1] = array[k];
                }

                return new DelegateWithHandle(new EventWithPerformanceTracker.Entry(newArray));
            }

            internal static DelegateWithHandle Remove(DelegateWithHandle dwh, T del)
            {
                if (dwh == null)
                    return null;
                switch (dwh.m_DelegateOrList.Reference)
                {
                    case null:
                        return dwh;
                    case EventWithPerformanceTracker.Entry[] list:
                    {
                        for (int i = list.Length - 1; i >= 0; i--)
                        {
                            if ((list[i].Reference as T) == del)
                            {
                                return RemoveAt(list, i);
                            }
                        }
                        return dwh;
                    }
                    default:
                    {
                        if ((dwh.m_DelegateOrList.Reference as T) == del)
                            return null;
                        return dwh;
                    }
                }
            }
        }
    }

    static class EventWithPerformanceTracker
    {
        internal struct Entry
        {
            internal ulong EditorPerformanceTrackerHandle;
            internal readonly object Reference;

            public Entry(Delegate del, ulong trackerHandle = EditorPerformanceTracker.InvalidTrackerHandle)
            {
                Reference = del;
                EditorPerformanceTrackerHandle = trackerHandle;
            }

            public Entry(Entry[] entries)
            {
                Reference = entries;
                EditorPerformanceTrackerHandle = EditorPerformanceTracker.InvalidTrackerHandle;
            }

            internal int length
            {
                get
                {
                    if (Reference == null)
                        return 0;
                    if (Reference is Entry[] list)
                        return list.Length;
                    return 1;
                }
            }
        }

        internal static string GetPerformanceTrackerName(string scope, Delegate d)
        {
            if (d.Method.DeclaringType != null)
            {
                if (Attribute.IsDefined(d.Method.DeclaringType, typeof(CompilerGeneratedAttribute)))
                {
                    // find the outermost type
                    var type = d.Method.DeclaringType;
                    while (type.DeclaringType != null)
                    {
                        type = type.DeclaringType;
                    }

                    return $"{scope}: callback in {type.FullName}";
                }

                return $"{scope}: {d.Method.DeclaringType.FullName}.{d.Method.Name}";
            }
            return scope;
        }

        internal static ulong GetPerformanceTrackerHandleForEvent(string scope, Delegate d)
        {
            var declaringType = d.Method.DeclaringType;
            if (declaringType != null)
            {
                return EditorPerformanceTracker.GetOrCreateTrackerHandle(GetPerformanceTrackerName(scope, d), declaringType);
            }
            return EditorPerformanceTracker.GetOrCreateTrackerHandle(GetPerformanceTrackerName(scope, d));
        }
    }

    /// <summary>
    /// Use this to add editor performance trackers to events that are implemented via raw delegates instead of proper
    /// C# events. Using editor performance trackers means that the pop-up progress bar will provide more helpful information.
    /// </summary>
    /// <remarks>
    /// This delegate should only be invoked on the main thread (because of the performance trackers).
    ///
    /// The implementation of this type assumes that the delegate it is wrapping changes infrequently compared to how often
    /// it is invoked. Invoking the delegate is cheap, but (as with C# delegates) changing the delegate can cause additional
    /// work as we need to recompute the performance markers.
    /// </remarks>
    /// <example>
    /// <code>
    /// public static class Events {
    ///     public static SomeDelegate MyPoorlyImplementedEvent;
    ///
    ///     private static void InvokeEvent() {
    ///         MyPoorlyImplementedEvent?.Invoke(1, 2, 3);
    ///     }
    /// }
    /// </code>
    /// can be rewritten to this:
    /// <code>
    /// public static class Events {
    ///     public static SomeDelegate MyPoorlyImplementedEvent;
    ///     private static DelegateWithPerformanceTracker&lt;SomeDelegate&gt; m_MyPoorlyImplementedEvent =
    ///         new DelegateWithPerformanceTracker&lt;SomeDelegate&gt;($"{nameof(Events)}.{nameof(MyPoorlyImplementedEvent)}");
    ///
    ///     private static void InvokeEvent() {
    ///         foreach (var evt in m_MyPoorlyImplementedEvent.UpdateAndInvoke(MyPoorlyImplementedEvent)) {
    ///             evt.Invoke(1, 2, 3);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="T">The delegate type.</typeparam>
    struct DelegateWithPerformanceTracker<T> where T : Delegate
    {
        private readonly string m_Scope;
        // We follow the same pattern as in EventWithPerformanceTracker to allow for re-entrant events.
        private EventWithPerformanceTracker.Entry m_Delegates;
        private T m_CachedDelegate;

        public DelegateWithPerformanceTracker(string scope)
        {
            m_Scope = scope;
            m_Delegates = default;
            m_CachedDelegate = null;
        }

        /// <summary>
        /// Update the delegate wrapped by this tracker and acquire an enumerator for invoking it via iteration.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Invoker UpdateAndInvoke(T value)
        {
            if (!ReferenceEquals(m_CachedDelegate, value))
            {
                m_CachedDelegate = value;
                m_Delegates = EventWithPerformanceTracker<T>.BuildDelegate(m_Scope, value);
            }

            return new Invoker(ref m_Delegates);
        }

        internal readonly struct Invoker
        {
            private readonly EventWithPerformanceTracker.Entry m_Delegates;
            public Enumerator GetEnumerator() => new Enumerator(in this);

            public Invoker(ref EventWithPerformanceTracker.Entry dlg)
            {
                m_Delegates = dlg;
            }

            internal struct Enumerator : IEnumerator<T>
            {
                private readonly EventWithPerformanceTracker.Entry m_Delegates;
                private readonly int m_Length;
                private int m_ActivePerformanceTracker;
                private int m_CurrentIndex;

                internal Enumerator(in Invoker evt)
                {
                    m_Delegates = evt.m_Delegates;
                    m_Length = evt.m_Delegates.length;
                    m_ActivePerformanceTracker = -1;
                    m_CurrentIndex = -1;
                }

                public void Dispose()
                {
                    if (m_ActivePerformanceTracker >= 0)
                        EditorPerformanceTracker.StopTracker(m_ActivePerformanceTracker);
                }

                public bool MoveNext()
                {
                    if (m_ActivePerformanceTracker >= 0)
                        EditorPerformanceTracker.StopTracker(m_ActivePerformanceTracker);
                    m_CurrentIndex++;
                    if (m_CurrentIndex < m_Length)
                    {
                        ulong handle;
                        if (m_Length == 1)
                            handle = m_Delegates.EditorPerformanceTrackerHandle;
                        else
                            handle = (m_Delegates.Reference as EventWithPerformanceTracker.Entry[])[m_CurrentIndex].EditorPerformanceTrackerHandle;
                        EditorPerformanceTracker.TryStartTrackerByHandle(handle, out m_ActivePerformanceTracker);
                    }
                    else
                        m_ActivePerformanceTracker = -1;
                    return m_CurrentIndex < m_Length;
                }

                public void Reset() => throw new NotImplementedException();

                public T Current
                {
                    get
                    {
                        if (m_Length == 1)
                            return m_Delegates.Reference as T;
                        return (m_Delegates.Reference as EventWithPerformanceTracker.Entry[])[m_CurrentIndex].Reference as T;
                    }
                }

                object IEnumerator.Current => (this as IEnumerator<T>).Current;
            }
        }
    }
}
