// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace UnityEngine
{
    internal sealed class UnitySynchronizationContext : SynchronizationContext
    {
        private const int kAwqInitialCapacity = 20;
        private readonly Queue<WorkRequest> m_AsyncWorkQueue = new Queue<WorkRequest>(kAwqInitialCapacity);
        private readonly int m_MainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

        // Send will process the call synchronously. If the call is processed on the main thread, we'll invoke it
        // directly here. If the call is processed on another thread it will be queued up like POST to be executed
        // on the main thread and it will wait. Once the main thread processes the work we can continue
        public override void Send(SendOrPostCallback callback, object state)
        {
            if (m_MainThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId)
            {
                callback(state);
            }
            else
            {
                using (var waitHandle = new ManualResetEvent(false))
                {
                    lock (m_AsyncWorkQueue)
                    {
                        m_AsyncWorkQueue.Enqueue(new WorkRequest(callback, state, waitHandle));
                    }
                    waitHandle.WaitOne();
                }
            }
        }

        // Post will add the call to a task list to be executed later on the main thread then work will continue asynchronously
        public override void Post(SendOrPostCallback callback, object state)
        {
            lock (m_AsyncWorkQueue)
            {
                m_AsyncWorkQueue.Enqueue(new WorkRequest(callback, state));
            }
        }

        // Exec will execute tasks off the task list
        private void Exec()
        {
            lock (m_AsyncWorkQueue)
            {
                while (m_AsyncWorkQueue.Count > 0)
                {
                    var work = m_AsyncWorkQueue.Dequeue();
                    work.Invoke();
                }
            }
        }

        // SynchronizationContext must be set before any user code is executed. This is done on
        // Initial domain load and domain reload at MonoManager ReloadAssembly
        [RequiredByNativeCode]
        private static void InitializeSynchronizationContext()
        {
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new UnitySynchronizationContext());
        }

        // All requests must be processed on the main thread where the full Unity API is available
        // See ScriptRunDelayedTasks in PlayerLoopCallbacks.h
        [RequiredByNativeCode]
        private static void ExecuteTasks()
        {
            var context = SynchronizationContext.Current as UnitySynchronizationContext;
            if (context != null)
                context.Exec();
        }

        private struct WorkRequest
        {
            private readonly SendOrPostCallback m_DelagateCallback;
            private readonly object m_DelagateState;
            private readonly ManualResetEvent m_WaitHandle;

            public WorkRequest(SendOrPostCallback callback, object state, ManualResetEvent waitHandle = null)
            {
                m_DelagateCallback = callback;
                m_DelagateState = state;
                m_WaitHandle = waitHandle;
            }

            public void Invoke()
            {
                m_DelagateCallback(m_DelagateState);
                if (m_WaitHandle != null)
                    m_WaitHandle.Set();
            }
        }
    }
}
