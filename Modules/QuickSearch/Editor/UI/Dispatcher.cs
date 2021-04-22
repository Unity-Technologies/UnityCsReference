// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;

namespace UnityEditor.Search
{
    static class Dispatcher
    {
        private static readonly ConcurrentQueue<Action> s_ExecutionQueue = new ConcurrentQueue<Action>();

        static Dispatcher()
        {
            Utils.tick += Update;
        }

        public static void Enqueue(Action action)
        {
            s_ExecutionQueue.Enqueue(action);
        }

        public static bool ProcessOne()
        {
            if (!s_ExecutionQueue.TryDequeue(out var action))
                return false;
            action.Invoke();
            return true;
        }

        static void Update()
        {
            if (s_ExecutionQueue.IsEmpty)
                return;

            while (s_ExecutionQueue.TryDequeue(out var action))
                action.Invoke();
        }
    }
}
