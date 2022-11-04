// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Events;

namespace UnityEngine
{
    public static class UnityEventAwaitableExtensions
    {
        public static Awaitable.Awaiter GetAwaiter(this UnityEvent ev)
        {
            var awaitable = Awaitable.NewManagedAwaitable();
            UnityAction cb = null;
            cb = () =>
            {
                ev.RemoveListener(cb);
                awaitable.RaiseManagedCompletion();
            };
            ev.AddListener(cb);
            return awaitable.GetAwaiter();
        }
        public static Awaitable<T>.Awaiter GetAwaiter<T>(this UnityEvent<T> ev)
        {
            var awaitable = Awaitable<T>.GetManaged();
            UnityAction<T> cb = null;
            cb = (T arg) =>
            {
                ev.RemoveListener(cb);
                awaitable.SetResultAndRaiseContinuation(arg);
            };
            ev.AddListener(cb);
            return awaitable.GetAwaiter();
        }
        public static Awaitable<(T0, T1)>.Awaiter GetAwaiter<T0, T1>(this UnityEvent<T0, T1> ev)
        {
            var awaitable = Awaitable<(T0, T1)>.GetManaged();
            UnityAction<T0,T1> cb = null;
            cb = (T0 arg0, T1 arg1) =>
            {
                ev.RemoveListener(cb);
                awaitable.SetResultAndRaiseContinuation((arg0, arg1));
            };
            ev.AddListener(cb);
            return awaitable.GetAwaiter();
        }
        public static Awaitable<(T0, T1, T2)>.Awaiter GetAwaiter<T0, T1, T2>(this UnityEvent<T0, T1, T2> ev)
        {
            var awaitable = Awaitable<(T0, T1, T2)>.GetManaged();
            UnityAction<T0, T1, T2> cb = null;
            cb = (T0 arg0, T1 arg1, T2 arg2) =>
            {
                ev.RemoveListener(cb);
                awaitable.SetResultAndRaiseContinuation((arg0, arg1, arg2));
            };
            ev.AddListener(cb);
            return awaitable.GetAwaiter();
        }
        public static Awaitable<(T0, T1, T2, T3)>.Awaiter GetAwaiter<T0, T1, T2, T3>(this UnityEvent<T0, T1, T2, T3> ev)
        {
            var awaitable = Awaitable<(T0, T1, T2, T3)>.GetManaged();
            UnityAction<T0, T1, T2, T3> cb = null;
            cb = (T0 arg0, T1 arg1, T2 arg2, T3 arg3) =>
            {
                ev.RemoveListener(cb);
                awaitable.SetResultAndRaiseContinuation((arg0, arg1, arg2, arg3));
            };
            ev.AddListener(cb);
            return awaitable.GetAwaiter();
        }
    }
}
