// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;

namespace UnityEditor.Search
{
    static class Dispatcher
    {
        readonly struct Task
        {
            readonly Action handler;
            readonly double delay;
            readonly long timestamp;

            public bool valid => handler != null;
            public bool expired => delay == 0d || TimeSpan.FromTicks(DateTime.UtcNow.Ticks - timestamp).TotalSeconds >= delay;

            public Task(in Action handler, in double delay)
            {
                this.handler = handler;
                this.delay = delay;
                timestamp = DateTime.UtcNow.Ticks;
            }

            public bool Invoke()
            {
                if (!expired)
                    return false;
                handler?.Invoke();
                return true;
            }
        }

        private static readonly ConcurrentQueue<Task> s_ExecutionQueue = new ConcurrentQueue<Task>();

        static Dispatcher()
        {
            Utils.tick += Update;
        }

        public static void Enqueue(Action action)
        {
            Enqueue(action, 0.0d);
        }

        public static void Enqueue(Action action, double delay)
        {
            Enqueue(new Task(action, delay));
        }

        private static void Enqueue(in Task task)
        {
            s_ExecutionQueue.Enqueue(task);
        }

        public static bool ProcessOne()
        {
            if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                return false;
            if (!s_ExecutionQueue.TryDequeue(out var task) && task.valid)
                return false;
            return Process(task);
        }

        static bool Process(in Task task)
        {
            var done = task.Invoke();
            if (!done)
                Enqueue(task);
            return done;
        }

        static void Update()
        {
            if (s_ExecutionQueue.IsEmpty)
                return;

            while (s_ExecutionQueue.TryDequeue(out var task) && task.valid)
                if (!Process(task))
                    break;
        }
    }
}
