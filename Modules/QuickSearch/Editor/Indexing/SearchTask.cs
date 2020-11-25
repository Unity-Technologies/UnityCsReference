// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEditor;
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

        private readonly string name;
        private readonly string title;
        private readonly int progressId = k_NoProgress;
        private EventWaitHandle cancelEvent;
        private readonly ResolveHandler finished;
        private readonly System.Diagnostics.Stopwatch sw;
        private string status = null;
        private volatile bool disposed = false;
        private readonly ITaskReporter reporter;

        public int total { get; set; }
        public bool canceled { get; private set; }
        public Exception error { get; private set; }

        public bool async => finished != null;
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
        public SearchTask(string name, string title, ResolveHandler finished, int total, ITaskReporter reporter)
            : this(name, title, reporter)
        {
            this.total = total;
            this.finished = finished;
            progressId = StartReport(title);
            cancelEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            if (IsProgressRunning(progressId))
                Progress.RegisterCancelCallback(progressId, () => cancelEvent.Set());
        }

        public SearchTask(string name, string title, ResolveHandler finished, ITaskReporter reporter)
            : this(name, title, finished, 1, reporter)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    Resolve();

                disposed = true;
            }
        }

        public void Dispose()
        {
            cancelEvent?.Dispose();
            cancelEvent = null;
            Dispose(true);
        }

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
            if (disposed)
                return;

            if (progressId == k_NoProgress)
                return;

            this.status = status;

            if (progressId == k_BlockingProgress)
            {
                EditorUtility.DisplayProgressBar(title, status, -1f);
                return;
            }

            Progress.SetDescription(progressId, status);
        }

        public void Report(int current)
        {
            if (total == 0)
                return;
            Report(current, total);
        }

        public void Report(int current, int total)
        {
            if (disposed)
                return;

            if (progressId == k_NoProgress)
                return;

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
                Progress.Report(progressId, current / (float)total);
            }
        }

        public void Cancel()
        {
            cancelEvent?.Set();
        }

        public bool Canceled()
        {
            if (cancelEvent == null)
                return false;

            if (!cancelEvent.WaitOne(0))
                return false;

            canceled = true;
            ClearReport(progressId);

            if (finished != null)
                Dispatcher.Enqueue(() => finished.Invoke(this, null));
            return true;
        }

        public void Resolve(T data)
        {
            if (disposed)
                return;
            if (Canceled())
                return;
            finished?.Invoke(this, data);
            Dispose();
        }

        private void Resolve()
        {
            if (disposed)
                return;
            FinishReport(progressId);
        }

        internal void Resolve(Exception err)
        {
            if (disposed)
                return;
            error = err;
            canceled = true;

            if (err != null)
            {
                ReportError(progressId, err);
                if (finished == null)
                    Debug.LogException(err);
            }

            finished?.Invoke(this, null);
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
            status = title;
            return progressId;
        }

        private void ReportError(int progressId, Exception err)
        {
            if (progressId == k_NoProgress)
                return;

            if (progressId == k_BlockingProgress)
            {
                Debug.LogException(err);
                EditorUtility.ClearProgressBar();
                return;
            }

            if (IsProgressRunning(progressId))
            {
                Progress.SetDescription(progressId, err.Message);
                Progress.Finish(progressId, Progress.Status.Failed);
            }

            status = null;
        }

        private void FinishReport(int progressId)
        {
            status = null;

            reporter?.Report(name, $"took {elapsedTime} ms");

            if (progressId == k_BlockingProgress)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            if (IsProgressRunning(progressId))
                Progress.Finish(progressId, Progress.Status.Succeeded);
        }

        private void ClearReport(int progressId)
        {
            status = null;

            if (progressId == k_BlockingProgress)
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            if (IsProgressRunning(progressId))
                Progress.Remove(progressId);
        }

        private static bool IsProgressRunning(int progressId)
        {
            if (progressId == k_NoProgress)
                return false;
            return Progress.Exists(progressId) && Progress.GetStatus(progressId) == Progress.Status.Running;
        }
    }
}
