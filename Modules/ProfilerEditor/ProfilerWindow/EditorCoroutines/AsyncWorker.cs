// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    abstract class BaseAsyncWorker : IDisposable
    {
        // Track the active count of Async workers
        protected static int s_ActiveInstanceCount = 0;
        public static int ActiveInstanceCount => Interlocked.Add(ref s_ActiveInstanceCount, 0);

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }

    // This is a workaround for UUM-12988 and will (after the release of com.unity.editorcoroutines@1.0.1) be able to be deleted.
    internal class AsyncWorkerCoroutineDummyObject : ScriptableObject { }

    class AsyncWorker<T> : BaseAsyncWorker
    {
        Func<CancellationToken, T> m_Execution;
        Action<T> m_Completion;
        T m_Result;
        bool m_Completed;
        EditorCoroutine m_WaitForExecutionCompletion;
        Task m_Task;
        CancellationTokenSource m_CancellationTokenSource;
        bool m_Disposed;
        AsyncWorkerCoroutineDummyObject m_DummyObject;

        public void Execute(Func<CancellationToken, T> execution, Action<T> completion)
        {
            if (m_Task != null)
                throw new InvalidOperationException("Async worker is already executing.");

            m_Execution = execution;
            if (m_Execution == null)
                throw new ArgumentNullException(nameof(execution));

            m_Completion = completion;
            if (m_Completion == null)
                throw new ArgumentNullException(nameof(completion));

            Interlocked.Increment(ref s_ActiveInstanceCount);

            // Create a dummy object as a handle for immediate abortion of the coroutine.
            // This is a workaround for UUM-12988 and will (after the release of com.unity.editorcoroutines@1.0.1) be able to be deleted.
            m_DummyObject = ScriptableObject.CreateInstance<AsyncWorkerCoroutineDummyObject>();
            m_DummyObject.hideFlags = HideFlags.HideAndDontSave;

            m_CancellationTokenSource = new CancellationTokenSource();

            // Start a coroutine to invoke the completion handler on the main thread when the work is completed.
            m_WaitForExecutionCompletion = EditorCoroutineUtility.StartCoroutine(WaitForExecutionCompletion(m_CancellationTokenSource.Token), m_DummyObject);

            // Begin the work on a new thread.
            m_Task = new Task(WorkerThreadStart, m_CancellationTokenSource.Token, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);
            m_Task.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                m_CancellationTokenSource?.Cancel();
                m_CancellationTokenSource?.Dispose();

                // Technically, destroying the dummy object means an immediate abortion for the coroutine.
                // But lets, for the moment, pretend that we care and stop the coroutine anyways.
                // ...
                // This is a workaround for UUM-12988 and will (after the release of com.unity.editorcoroutines@1.0.1) be able to be reverted to a straight up StopCoroutine call without Dummy object.
                EditorCoroutineUtility.StartCoroutine(StopCoroutine(), this);
                //if (m_WaitForExecutionCompletion != null)
                //    EditorCoroutineUtility.StopCoroutine(m_WaitForExecutionCompletion);

                m_CancellationTokenSource = null;
                m_Task = null;
                m_Execution = null;
                m_Completion = null;

                Interlocked.Decrement(ref s_ActiveInstanceCount);
            }

            m_Disposed = true;
        }

        void WorkerThreadStart()
        {
            var token = m_CancellationTokenSource.Token;
            if (!token.IsCancellationRequested)
                m_Result = m_Execution(token);
            m_Completed = true;
        }

        IEnumerator WaitForExecutionCompletion(CancellationToken token)
        {
            while (!m_Completed && !token.IsCancellationRequested)
                yield return null;

            // Since the Dummy Object workaround gives the coroutine another frame to process, it might be finished in the next frame and invoke the completion call, even though it was aborted
            // The "if(m_DummyObject)" check is therefore a workaround for UUM-12988 and will (after the release of com.unity.editorcoroutines@1.0.1) be able to be deleted. (though retain the invokarion of m_Completion)
            if (m_DummyObject && !token.IsCancellationRequested)
                m_Completion?.Invoke(m_Result);

            if (m_DummyObject)
                UnityEngine.Object.DestroyImmediate(m_DummyObject);
        }

        // This is a workaround for UUM-12988 and will (after the release of com.unity.editorcoroutines@1.0.1) be able to be deleted.
        IEnumerator StopCoroutine()
        {
            yield return null;
            if (m_WaitForExecutionCompletion != null)
                EditorCoroutineUtility.StopCoroutine(m_WaitForExecutionCompletion);
        }
    }
}
