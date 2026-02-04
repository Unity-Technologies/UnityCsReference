// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_PROGRESS
// #define DEBUG_SEARCHTASK_DISPOSE
using System;
using System.Collections.Generic;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Search
{
    interface ITaskReporter
    {
        void Report(string status, params string[] args);
    }

    class SearchTask<T> : IDisposable
        where T : class
    {
        public delegate void ResolveHandler(SearchTask<T> task, T data);


        const int k_BlockingProgress = -2;

        static readonly TimeSpan k_MaxThreadJoinWaitTime = TimeSpan.FromSeconds(5);

        internal int progressId = Progress.InvalidProgressId;
        volatile float m_LastProgress = -1f;
        CancellationTokenSource m_LocalCancelEvent;
        CancellationTokenSource m_CancelEvent;
        CancellationTokenRegistration m_CancellationTokenRegistration;
        readonly ResolveHandler m_Resolver;
        readonly System.Diagnostics.Stopwatch m_Sw;
        string m_Status = null;
        volatile bool m_Disposed = false;
        readonly ITaskReporter m_Reporter;
        int m_ProgressThrottleCounter;
        readonly List<Thread> m_Threads = new();
        readonly List<SearchTask<T>> m_ChildrenTask = new();

        internal IReadOnlyList<Thread> threads => m_Threads;

        public string name { get; }
        public string title { get; }
        public int total { get; set; }
        public bool throttleProgressReport { get; set; } = false;
        public int throttleProgressRate { get; set; } = 1;
        public Exception error { get; private set; }

        public bool async => m_Resolver != null;
        public long elapsedTime => m_Sw.ElapsedMilliseconds;

        public CancellationTokenSource cancellationTokenSource => m_CancelEvent;
        public CancellationToken cancellationToken => m_CancelEvent?.Token ?? CancellationToken.None;

        internal bool disposed => m_Disposed;

        public object userData { get; set; }

        SearchTask(string name, string title, ITaskReporter reporter)
        {
            this.name = name;
            this.title = title;
            this.m_Reporter = reporter;
            m_Sw = new System.Diagnostics.Stopwatch();
            m_Sw.Start();

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        void OnBeforeAssemblyReload()
        {
            CancelImmediate();
        }

        public SearchTask(string name)
            : this(name, string.Empty, null)
        { }

        /// <summary>
        /// Create blocking task
        /// </summary>
        public SearchTask(string name, string title, int total, ITaskReporter reporter)
            : this(name, title, reporter)
        {
            this.total = total;
            progressId = StartBlockingReport(title);
        }

        /// <summary>
        /// Create async or threaded task
        /// </summary>
        public SearchTask(string name, string title, ResolveHandler resolver, int total, ITaskReporter reporter)
            : this(name, title, reporter)
        {
            this.total = total;
            this.m_Resolver = resolver;
            progressId = StartReport(title);
            m_LocalCancelEvent = new CancellationTokenSource();
            m_CancelEvent = CancellationTokenSource.CreateLinkedTokenSource(m_LocalCancelEvent.Token);

            SetupProgressCancellation();
            SetupCancelCallback();
        }

        public SearchTask(string name, string title, ResolveHandler resolver, ITaskReporter reporter)
            : this(name, title, resolver, 1, reporter)
        {
        }

        SearchTask(int parentTaskProgressId, CancellationTokenSource parentCancellationTokenSource, string name, string title, ResolveHandler resolver, int total, ITaskReporter reporter)
            : this(name, title, reporter)
        {
            this.total = total;
            this.m_Resolver = resolver;
            progressId = StartReport(title, parentTaskProgressId);
            m_LocalCancelEvent = new CancellationTokenSource();
            m_CancelEvent = CancellationTokenSource.CreateLinkedTokenSource(parentCancellationTokenSource.Token, m_LocalCancelEvent.Token);

            // Do not setup progress cancellation for child tasks. Only the parent task should be canceled manually from the progress window.
            SetupCancelCallback();
        }

        void SetupProgressCancellation()
        {
            if (IsProgressRunning(progressId))
            {
                LogProgress("RegisterCallback");
                Progress.RegisterCancelCallback(progressId, () =>
                {
                    if (m_LocalCancelEvent == null)
                        return false;
                    m_LocalCancelEvent.Cancel();
                    return true;
                });
            }
        }

        void SetupCancelCallback()
        {
            if (m_CancelEvent != null)
            {
                // From Microsoft: Callbacks are executed synchronously in LIFO order.
                m_CancellationTokenRegistration = m_CancelEvent.Token.Register(() =>
                {
                    if (m_Resolver != null)
                        Dispatcher.Enqueue(ResolveCancel);
                });
            }
        }

        /// <summary>
        /// Creates a child SearchTask that will be canceled when the parent task is canceled. Cancellation is done in LIFO order.
        /// </summary>
        /// <param name="childName">Name of the child task.</param>
        /// <param name="childTitle">Title of the task for reporting purpose.</param>
        /// <param name="childResolver">Resolver of the task.</param>
        /// <param name="childReporter">Reporter of the task.</param>
        /// <returns>Returns a SearchTask.</returns>
        /// <exception cref="InvalidOperationException">Throws an exception if the parent task is not asynchronous.</exception>
        public SearchTask<T> CreateChildTask(string childName, string childTitle, ResolveHandler childResolver, ITaskReporter childReporter)
        {
            if (!async)
                throw new InvalidOperationException("Cannot create child task from a blocking SearchTask.");
            var childTask = new SearchTask<T>(progressId, m_CancelEvent, childName, childTitle, childResolver, total, childReporter);
            m_ChildrenTask.Add(childTask);
            return childTask;
        }

        [System.Diagnostics.Conditional("DEBUG_PROGRESS")]
        void LogProgress(in string name, params object[] args)
        {
            Debug.Log($"{name} {progressId}, {string.Join(", ", args)}, main ={UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread()}");
        }

        void Dispose(bool disposing, bool callResolver)
        {
            LogProgress("Dispose", m_Disposed, disposing);

            if (m_Disposed)
                return;

            // Dispose in reverse order to respect LIFO cancellation order
            for (var i = m_ChildrenTask.Count - 1; i >= 0; --i)
            {
                var childTask = m_ChildrenTask[i];
                childTask.Dispose(disposing, callResolver);
            }

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            var allStopped = CancelImmediateWithoutResolver();
            if (!allStopped)
                Debug.LogWarning($"SearchTask '{name}' had active threads when disposed ({(disposing ? "From Dispose" : "From Finalizer")}).");
            if (callResolver)
            {
                ResolveCancel();
            }

            m_CancelEvent?.Dispose();
            m_CancelEvent = null;
            m_LocalCancelEvent?.Dispose();
            m_LocalCancelEvent = null;

            if (disposing)
            {
                FinishReport();
                if (userData is IDisposable disposable)
                    disposable.Dispose();
            }

            ClearReport();
            userData = null;
            m_Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool callResolver)
        {
            Dispose(true, callResolver);
            GC.SuppressFinalize(this);
        }

        ~SearchTask() => Dispose(false, false);

        public bool RunThread(Action routine)
        {
            return RunThread(routine, null);
        }

        public bool RunThread(Action routine, Action finalize)
        {
            var t = new Thread((currentThreadObject) =>
            {
                var currentThread = (Thread)currentThreadObject;
                try
                {
                    if (Canceled())
                    {
                        return;
                    }

                    routine();

                    if (Canceled())
                    {
                        return;
                    }

                    DispatchWaitForThread(currentThread, finalize);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    DispatchWaitForThread(currentThread, () => ResolveException(ex));
                }
            })
            {
                Name = name
            };
            t.Start(t);
            m_Threads.Add(t);
            return t.ThreadState == ThreadState.Running;
        }

        public void Report(string status)
        {
            Report(status, m_LastProgress);
        }

        public void Report(string status, float progress)
        {
            if (!IsValid())
                return;

            this.m_Status = status;

            if (progressId == k_BlockingProgress)
            {
                // TODO: should this report the actual progress instead of -1?
                EditorUtility.DisplayProgressBar(title, status, -1f);
                return;
            }

            LogProgress("Report");
            ReportAsyncProgress(status, progress);
        }

        public void Report(int current)
        {
            Report(m_Status, current);
        }

        public void Report(string status, int current)
        {
            if (total == 0)
                return;
            Report(status, current, total);
        }

        public void Report(int current, int total)
        {
            Report(m_Status, current, total);
        }

        public void Report(string status, int current, int total)
        {
            if (!IsValid())
                return;

            this.m_Status = status;

            if (progressId == k_BlockingProgress)
            {
                if (m_CancelEvent == null)
                    EditorUtility.DisplayProgressBar(title, status, current / (float)total);
                else
                {
                    if (EditorUtility.DisplayCancelableProgressBar(title, status, current / (float)total))
                        m_CancelEvent.Cancel();
                }
            }
            else
            {
                m_LastProgress = current / (float)total;
                LogProgress("Report");
                ReportAsyncProgress(status, current, total);
            }
        }

        void ReportAsyncProgress(string status, int current, int total)
        {
            if (!throttleProgressReport)
                Progress.Report(progressId, current, total, status);
            else
            {
                if (m_ProgressThrottleCounter % throttleProgressRate == 0)
                    Progress.Report(progressId, current, total, status);
                ++m_ProgressThrottleCounter;
            }
        }

        void ReportAsyncProgress(string status, float progress)
        {
            if (!throttleProgressReport)
                Progress.Report(progressId, progress, status);
            else
            {
                if (m_ProgressThrottleCounter % throttleProgressRate == 0)
                    Progress.Report(progressId, progress, status);
                ++m_ProgressThrottleCounter;
            }
        }

        public void ResetProgressThrottle()
        {
            m_ProgressThrottleCounter = 0;
        }

        /// <summary>
        /// Cancels the task and all its children.
        /// </summary>
        public void Cancel()
        {
            m_LocalCancelEvent?.Cancel();
        }

        /// <summary>
        /// Cancels the task and all its children and waits for all threads to stop. The ResolveCancel will be called before returning.
        /// </summary>
        /// <returns>True if all threads (including children's) were stopped.</returns>
        public bool CancelImmediate()
        {
            var allStopped = CancelImmediateWithoutResolver();

            // Resolve cancel after all threads have stopped.
            ResolveCancel();

            return allStopped;
        }

        /// <summary>
        /// Cancels the task and all its children and waits for all threads to stop. Does not call the ResolveCancel.
        /// </summary>
        /// <returns>True if all threads (including children's) were stopped.</returns>
        public bool CancelImmediateWithoutResolver()
        {
            // Dispose the cancellation registration to avoid calling the resolver multiple times.
            m_CancellationTokenRegistration.Dispose();

            Cancel();
            var allStopped = CancelChildrenImmediate();
            allStopped = WaitForAllThreads() && allStopped;
            m_Threads.Clear();

            return allStopped;
        }

        public bool Canceled()
        {
            if (!IsValid())
                return true;

            if (m_CancelEvent == null)
                return false;

            return m_CancelEvent.IsCancellationRequested;
        }

        public void Resolve(T data)
        {
            if (!IsValid())
                return;

            // If canceled, the cancel callback will handle the resolve and dispose
            if (Canceled())
                return;

            m_Resolver?.Invoke(this, data);
            Dispose(false);
        }

        public void ResolveException(Exception err)
        {
            Console.WriteLine($"Search task exception: {err}");

            error = err;

            if (!IsValid())
                return;

            // Call resolver before calling ReportError. ReportError clears some state
            // that make the task invalid and considered cancelled.
            m_Resolver?.Invoke(this, null);

            if (err != null)
            {
                ReportError(err);
                if (m_Resolver == null)
                    Debug.LogException(err);
            }

            Dispose(false);
        }

        void ResolveCancel()
        {
            if (!IsValid())
                return;

            WaitForAllThreads();
            m_Resolver?.Invoke(this, null);

            Dispose(false);
        }

        int StartBlockingReport(string title)
        {
            m_Status = title;
            EditorUtility.DisplayProgressBar(title, null, 0f);
            return k_BlockingProgress;
        }

        int StartReport(string title, int parentProgressId = Progress.InvalidProgressId)
        {
            var progressId = Progress.Start(title, parentId: parentProgressId);
            LogProgress("Start");
            Progress.SetPriority(progressId, Progress.Priority.Low);
            m_Status = title;
            return progressId;
        }

        public void SetProgressPriority(Progress.Priority priority)
        {
            SetProgressPriority((int)priority);
        }

        public void SetProgressPriority(int priority)
        {
            if (progressId == Progress.InvalidProgressId)
                return;
            Progress.SetPriority(progressId, priority);
        }

        void ReportError(Exception err)
        {
            if (!IsValid())
                return;

            if (progressId == k_BlockingProgress)
            {
                Debug.LogException(err);
                EditorUtility.ClearProgressBar();
            }
            else if (IsProgressRunning(progressId))
            {
                LogProgress("SetDescription", "ReportError");
                Progress.SetDescription(progressId, err.Message);
                LogProgress("Finish", "ReportError");
                Progress.Finish(progressId, Progress.Status.Failed);
            }

            progressId = Progress.InvalidProgressId;
            m_Status = null;
        }

        bool IsValid()
        {
            if (m_Disposed)
                return false;

            if (progressId == Progress.InvalidProgressId)
                return false;

            return true;
        }

        void FinishReport()
        {
            error = null;
            m_Status = null;

            if (!IsValid())
                return;

            LogProgress("FinishReport", m_Reporter);

            m_Reporter?.Report(name, $"took {elapsedTime} ms");

            if (progressId == k_BlockingProgress)
                EditorUtility.ClearProgressBar();
            else if (IsProgressRunning(progressId))
            {
                LogProgress("Finish");
                Progress.Finish(progressId, Progress.Status.Succeeded);
            }

            progressId = Progress.InvalidProgressId;
        }

        void ClearReport()
        {
            m_Status = null;

            if (!IsValid())
                return;

            if (progressId == k_BlockingProgress)
                EditorUtility.ClearProgressBar();
            else if (IsProgressRunning(progressId))
            {
                Progress.Remove(progressId);
                LogProgress("Remove");
            }

            progressId = Progress.InvalidProgressId;
        }

        bool IsProgressRunning(int progressId)
        {
            if (progressId == Progress.InvalidProgressId)
                return false;
            LogProgress("Exists");
            if (!Progress.Exists(progressId))
                return false;
            var status = Progress.GetStatus(progressId);
            LogProgress("GetStatus", status);
            return status == Progress.Status.Running;
        }

        static void DispatchWaitForThread(Thread t, Action actionToRunAfterThread)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));
            if (actionToRunAfterThread == null)
                return;
            Dispatcher.Enqueue(() =>
            {
                var stopped = t.Join(k_MaxThreadJoinWaitTime);
                if (!stopped)
                {
                    Debug.LogWarning($"SearchTask '{t.Name}' thread did not stop within {k_MaxThreadJoinWaitTime.TotalSeconds} seconds.");
                }
                actionToRunAfterThread.Invoke();
            });
        }

        bool WaitForAllThreads()
        {
            var allStopped = true;
            foreach (var thread in m_Threads)
            {
                if (thread.IsAlive)
                {
                    var stopped = thread.Join(k_MaxThreadJoinWaitTime);
                    allStopped &= stopped;
                    if (!stopped)
                    {
                        Debug.LogWarning($"SearchTask '{name}' thread did not stop within {k_MaxThreadJoinWaitTime.TotalSeconds} seconds.");
                    }
                }
            }

            return allStopped;
        }

        bool WaitForAllChildrenThreads()
        {
            var allStopped = true;
            foreach (var childTask in m_ChildrenTask)
            {
                allStopped &= childTask.WaitForAllThreads();
            }
            return allStopped;
        }

        bool CancelChildrenImmediate()
        {
            var allStopped = true;
            // Cancel in reverse order to respect LIFO cancellation order
            for (var i = m_ChildrenTask.Count - 1; i >= 0; --i)
            {
                var childTask = m_ChildrenTask[i];
                allStopped &= childTask.CancelImmediate();
            }
            return allStopped;
        }
    }
}
