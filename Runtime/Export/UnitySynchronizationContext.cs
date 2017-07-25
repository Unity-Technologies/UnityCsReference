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

        // Send will process the call synchronously
        public override void Send(SendOrPostCallback callback, object state)
        {
            callback(state);
        }

        // Post will add the call to a task list to be executed later
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

            public WorkRequest(SendOrPostCallback callback, object state)
            {
                m_DelagateCallback = callback;
                m_DelagateState = state;
            }

            public void Invoke()
            {
                m_DelagateCallback(m_DelagateState);
            }
        }
    }
}
