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


        private const int k_BlockingProgress = -2;

        static readonly TimeSpan k_MaxThreadJoinWaitTime = TimeSpan.FromSeconds(5);

        public readonly string name;
        public readonly string title;
        internal int progressId = Progress.InvalidProgressId;
        private volatile float lastProgress = -1f;
        private CancellationTokenSource m_CancelEvent;
        private readonly ResolveHandler resolver;
        private readonly System.Diagnostics.Stopwatch sw;
        private string status = null;
        private volatile bool disposed = false;
        private readonly ITaskReporter reporter;
        int m_ProgressThrottleCounter;
        List<Thread> m_Threads = new();

        internal IReadOnlyList<Thread> threads => m_Threads;

        public int total { get; set; }
        public bool throttleProgressReport { get; set; } = false;
        public int throttleProgressRate { get; set; } = 1;
        public bool canceled { get; private set; }
        public Exception error { get; private set; }

        public bool async => resolver != null;
        public long elapsedTime => sw.ElapsedMilliseconds;

        public CancellationToken cancellationToken => m_CancelEvent?.Token ?? CancellationToken.None;

        private SearchTask(string name, string title, ITaskReporter reporter)
        {
            this.name = name;
            this.title = title;
            this.reporter = reporter;
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnBeforeAssemblyReload()
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
            this.resolver = resolver;
            progressId = StartReport(title);
            m_CancelEvent = new CancellationTokenSource();

            if (IsProgressRunning(progressId))
            {
                LogProgress("RegisterCallback");
                Progress.RegisterCancelCallback(progressId, () =>
                {
                    if (m_CancelEvent == null)
                        return false;
                    m_CancelEvent.Cancel();
                    return true;
                });
            }
        }

        public SearchTask(string name, string title, ResolveHandler resolver, ITaskReporter reporter)
            : this(name, title, resolver, 1, reporter)
        {
        }

        [System.Diagnostics.Conditional("DEBUG_PROGRESS")]
        private void LogProgress(in string name, params object[] args)
        {
            Debug.Log($"{name} {progressId}, {string.Join(", ", args)}, main ={UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread()}");
        }

        protected virtual void Dispose(bool disposing)
        {
            LogProgress("Dispose", disposed, disposing);

            if (disposed)
                return;

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            var allStopped = CancelImmediate();

            if (!allStopped)
                Debug.LogWarning($"SearchTask '{name}' had active threads when disposed ({(disposing ? "From Dispose" : "From Finalizer")}).");

            m_CancelEvent?.Dispose();
            m_CancelEvent = null;

            if (disposing)
                Resolve();

            ClearReport();
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SearchTask() => Dispose(false);

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
                    DispatchWaitForThread(currentThread, () => Resolve(ex));
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
            Report(status, lastProgress);
        }

        public void Report(string status, float progress)
        {
            if (!IsValid())
                return;

            this.status = status;

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
            Report(status, current);
        }

        public void Report(string status, int current)
        {
            if (total == 0)
                return;
            Report(status, current, total);
        }

        public void Report(int current, int total)
        {
            Report(status, current, total);
        }

        public void Report(string status, int current, int total)
        {
            if (!IsValid())
                return;

            this.status = status;

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
                lastProgress = current / (float)total;
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

        public void Cancel()
        {
            m_CancelEvent?.Cancel();
        }

        public bool CancelImmediate()
        {
            Cancel();
            var allStopped = WaitForAllThreads();
            m_Threads.Clear();
            return allStopped;
        }

        public bool Canceled()
        {
            if (!IsValid())
                return true;

            if (m_CancelEvent == null)
                return false;

            if (!m_CancelEvent.IsCancellationRequested)
                return false;

            // TODO: SearchTasks should not be run concurrently, even if they can run threads. So this should be good enough for now, i.e. there shouldn't be a race condition to enqueue.
            // However, we will need to revisit the whole class to prevent potential future issues.
            if (!canceled && resolver != null)
                Dispatcher.Enqueue(() =>
                {
                    WaitForAllThreads();
                    resolver.Invoke(this, null);
                });

            canceled = true;
            return true;
        }

        public void Resolve(T data, bool completed = true)
        {
            if (!IsValid())
                return;

            if (Canceled())
                return;
            resolver?.Invoke(this, data);
            if (completed)
                Dispose();
        }

        private void Resolve()
        {
            FinishReport();
        }

        public void Resolve(Exception err)
        {
            Console.WriteLine($"Search task exception: {err}");

            error = err;
            canceled = true;

            if (!IsValid())
                return;

            if (err != null)
            {
                ReportError(err);
                if (resolver == null)
                    Debug.LogException(err);
            }

            resolver?.Invoke(this, null);
        }

        private int StartBlockingReport(string title)
        {
            status = title;
            EditorUtility.DisplayProgressBar(title, null, 0f);
            return k_BlockingProgress;
        }

        private int StartReport(string title)
        {
            var progressId = Progress.Start(title);
            LogProgress("Start");
            Progress.SetPriority(progressId, (int)Progress.Priority.Low);
            status = title;
            return progressId;
        }

        private void ReportError(Exception err)
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
            status = null;
        }

        private bool IsValid()
        {
            if (disposed)
                return false;

            if (progressId == Progress.InvalidProgressId)
                return false;

            return true;
        }

        private void FinishReport()
        {
            error = null;
            status = null;
            canceled = false;

            if (!IsValid())
                return;

            LogProgress("FinishReport", reporter);

            reporter?.Report(name, $"took {elapsedTime} ms");

            if (progressId == k_BlockingProgress)
                EditorUtility.ClearProgressBar();
            else if (IsProgressRunning(progressId))
            {
                LogProgress("Finish");
                Progress.Finish(progressId, Progress.Status.Succeeded);
            }

            progressId = Progress.InvalidProgressId;
        }

        private void ClearReport()
        {
            status = null;

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

        private bool IsProgressRunning(int progressId)
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
    }
}
