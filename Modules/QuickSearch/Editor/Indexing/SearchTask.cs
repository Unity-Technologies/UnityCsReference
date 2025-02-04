// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_PROGRESS
// #define DEBUG_SEARCHTASK_DISPOSE
using System;
using System.Threading;
using UnityEngine;

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


        private const int k_NoProgress = -1;
        private const int k_BlockingProgress = -2;

        public readonly string name;
        public readonly string title;
        private int progressId = k_NoProgress;
        private volatile float lastProgress = -1f;
        private EventWaitHandle cancelEvent;
        private readonly ResolveHandler resolver;
        private readonly System.Diagnostics.Stopwatch sw;
        private string status = null;
        private volatile bool disposed = false;
        private readonly ITaskReporter reporter;
        int m_ProgressThrottleCounter;

        public int total { get; set; }
        public bool throttleProgressReport { get; set; } = false;
        public int throttleProgressRate { get; set; } = 1;
        public bool canceled { get; private set; }
        public Exception error { get; private set; }

        public bool async => resolver != null;
        public long elapsedTime => sw.ElapsedMilliseconds;

        private SearchTask(string name, string title, ITaskReporter reporter)
        {
            this.name = name;
            this.title = title;
            this.reporter = reporter;
            sw = new System.Diagnostics.Stopwatch();
            sw.Start();
        }

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
            cancelEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            if (IsProgressRunning(progressId))
            {
                LogProgress("RegisterCallback");
                Progress.RegisterCancelCallback(progressId, () => cancelEvent != null && cancelEvent.Set());
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

            cancelEvent?.Dispose();
            cancelEvent = null;

            if (disposing)
                Resolve();

            LogProgress("Exists");
            if (Progress.Exists(progressId))
            {
                LogProgress("Remove");
                Progress.Remove(progressId);
            }
            progressId = k_NoProgress;
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SearchTask() => Dispose(false);

        public bool RunThread(Action routine, Action finalize = null)
        {
            var t = new Thread(() =>
            {
                try
                {
                    routine();
                    if (finalize != null)
                        Dispatcher.Enqueue(finalize);
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    Dispatcher.Enqueue(() => Resolve(ex));
                }
            })
            {
                Name = name
            };
            t.Start();
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
                if (cancelEvent == null)
                    EditorUtility.DisplayProgressBar(title, status, current / (float)total);
                else
                {
                    if (EditorUtility.DisplayCancelableProgressBar(title, status, current / (float)total))
                        cancelEvent.Set();
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
            cancelEvent?.Set();
        }

        public bool Canceled()
        {
            if (!IsValid())
                return true;

            if (cancelEvent == null)
                return false;

            if (!cancelEvent.WaitOne(0))
                return false;

            canceled = true;
            ClearReport();

            if (resolver != null)
                Dispatcher.Enqueue(() => resolver.Invoke(this, null));
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

            if (!IsValid())
                return;

            error = err;
            canceled = true;

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

            progressId = k_NoProgress;
            status = null;
        }

        private bool IsValid()
        {
            if (disposed)
                return false;

            if (progressId == k_NoProgress)
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

            progressId = k_NoProgress;
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

            progressId = k_NoProgress;
        }

        private bool IsProgressRunning(int progressId)
        {
            if (progressId == k_NoProgress)
                return false;
            LogProgress("Exists");
            if (!Progress.Exists(progressId))
                return false;
            var status = Progress.GetStatus(progressId);
            LogProgress("GetStatus", status);
            return status == Progress.Status.Running;
        }
    }
}
