// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // TODO: we need to use status in the UI
    // Keep in sync with Editor\src\Progress.h
    public enum ProgressStatus
    {
        Running,
        Succeeded,
        Failed,
        Canceled,
        Count
    }

    [Flags]
    public enum ProgressOptions
    {
        None        = 0 << 0,
        Sticky      = 1 << 0,
        Indefinite  = 1 << 1,
        Synchronous = 1 << 2,
        Managed     = 1 << 3,
        Unmanaged   = 1 << 4
    }

    public class ProgressReport
    {
        public ProgressReport(float progress = -1f, string description = null)
        {
            this.progress = progress;
            this.description = description;
            error = null;
        }

        public float progress { get; internal set; }
        public string description { get; internal set; }
        public string error { get; internal set; }
    }

    public class ProgressError : ProgressReport
    {
        public ProgressError(string error)
            : base(0f, null)
        {
            this.error = error;
        }
    }

    class ProgressTask
    {
        public int id { get; internal set; }
        public Func<int, object, IEnumerator> handler { get; internal set; }
        public object userData { get; internal set; }
        public Stack<IEnumerator> iterators { get; internal set; }
    }

    [StaticAccessor("Editor::Progress", StaticAccessorType.DoubleColon)]
    [NativeHeader("Editor/Src/Progress.h")]
    public static class Progress
    {
        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::Start")]
        private static extern int Internal_Start(string name, string description, ProgressOptions options, int parentId);

        public static int Start(string name, string description = null, ProgressOptions options = ProgressOptions.None, int parentId = -1)
        {
            // Automatically set C# progress as managed so they gets canceled when a domain reload occurs.
            if ((options & ProgressOptions.Unmanaged) == 0)
                options |= ProgressOptions.Managed;
            return Internal_Start(name, description, options, parentId);
        }

        [ThreadSafe]
        public static extern void Finish(int id, ProgressStatus status = ProgressStatus.Succeeded);

        [ThreadSafe]
        public static extern int Clear(int id);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::Report")]
        private static extern void Internal_Report(int id, float progress, string description = null);

        public static void Report(int id, float progress)
        {
            Internal_Report(id, progress);
        }

        public static void Report(int id, float progress, string description)
        {
            Internal_Report(id, progress, description);
            if (string.IsNullOrEmpty(description))
                SetDescription(id, description);
        }

        public static extern bool Cancel(int id);

        [ThreadSafe]
        internal static extern void RegisterCancelCallbackFromScript(int id, Func<bool> callback);

        public static void RegisterCancelCallback(int id, Func<bool> callback)
        {
            RegisterCancelCallbackFromScript(id, callback);
        }

        [ThreadSafe]
        public static extern void UnregisterCancelCallback(int id);

        public static extern int GetCount();

        public static extern int[] GetCountPerStatus();

        internal static extern float GetProgress(int id);

        public static extern string GetName(int id);

        public static extern string GetDescription(int id);

        [ThreadSafe]
        public static extern void SetDescription(int id, string description);

        public static extern long GetStartDateTime(int id);

        public static extern long GetUpdateDateTime(int id);

        public static extern int GetParentId(int id);

        public static extern int GetId(int index);

        public static extern bool IsCancellable(int id);

        public static extern ProgressStatus GetStatus(int id);

        public static extern ProgressOptions GetOptions(int id);

        public static extern bool Exists(int id);

        public static void ShowDetails(bool shouldReposition = true)
        {
            ProgressWindow.ShowDetails(shouldReposition);
        }

        public static int RunTask(string name, string description, Func<int, object, IEnumerator> taskHandler, ProgressOptions options = ProgressOptions.None, int parentId = -1, object userData = null)
        {
            var progressId = Start(name, description, options, parentId);
            s_Tasks.Add(new ProgressTask { id = progressId, handler = taskHandler, userData = userData, iterators = new Stack<IEnumerator>() });

            EditorApplication.update -= RunTasks;
            EditorApplication.update += RunTasks;

            return progressId;
        }

        private static void RunTasks()
        {
            for (var taskIndex = s_Tasks.Count - 1; taskIndex >= 0; --taskIndex)
            {
                var task = s_Tasks[taskIndex];
                try
                {
                    if (task.iterators.Count == 0)
                        task.iterators.Push(task.handler(task.id, task.userData));

                    var iterator = task.iterators.Peek();
                    var finished = !iterator.MoveNext();

                    if (finished)
                    {
                        if (task.iterators.Count > 1)
                        {
                            ++taskIndex;
                            task.iterators.Pop();
                        }
                        else
                        {
                            Finish(task.id, ProgressStatus.Succeeded);
                            s_Tasks.RemoveAt(taskIndex);
                        }

                        continue;
                    }

                    var cEnumrable = iterator.Current as IEnumerable;
                    if (cEnumrable != null)
                    {
                        ++taskIndex;
                        task.iterators.Push(cEnumrable.GetEnumerator());
                        continue;
                    }

                    var cEnumarator = iterator.Current as IEnumerator;
                    if (cEnumarator != null)
                    {
                        ++taskIndex;
                        task.iterators.Push(cEnumarator);
                        continue;
                    }

                    var report = iterator.Current as ProgressReport;
                    if (report != null && report.error != null)
                    {
                        SetDescription(task.id, report.error);
                        Finish(task.id, ProgressStatus.Failed);
                        s_Tasks.RemoveAt(taskIndex);
                    }
                    else
                    {
                        if (report != null)
                        {
                            if (String.IsNullOrEmpty(report.description))
                                Report(task.id, report.progress);
                            else
                                Report(task.id, report.progress, report.description);
                        }
                        else if ((GetOptions(task.id) & ProgressOptions.Indefinite) == ProgressOptions.Indefinite)
                        {
                            Report(task.id, -1f);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    SetDescription(task.id, ex.Message);
                    Finish(task.id, ProgressStatus.Failed);
                    s_Tasks.RemoveAt(taskIndex);
                }
            }

            if (s_Tasks.Count == 0)
                EditorApplication.update -= RunTasks;
        }

        private static List<ProgressItem> s_ProgressItems = new List<ProgressItem>(8);
        private static bool s_Initialized = false;
        private static float s_Progress = 0;
        private static bool s_ProgressDirty = true;
        private static List<ProgressTask> s_Tasks = new List<ProgressTask>();

        public delegate void ProgressEventHandler(ProgressItem[] operations);

        public static event ProgressEventHandler OnAdded;
        public static event ProgressEventHandler OnUpdated;
        public static event ProgressEventHandler OnRemoved;

        static Progress()
        {
            RestoreProgressItems(); // on domain reload - request all native operations
        }

        public static IEnumerable<ProgressItem> EnumerateItems()
        {
            return s_ProgressItems;
        }

        public static ProgressItem GetProgressById(int id)
        {
            foreach (var progressItem in s_ProgressItems)
            {
                if (progressItem.id == id)
                    return progressItem;
            }
            return null;
        }

        public static int GetRunningProgressCount()
        {
            return GetCountPerStatus()[(int)ProgressStatus.Running];
        }

        public static bool IsRunning => GetRunningProgressCount() > 0;

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::GetGlobalProgress")]
        private static extern float Internal_GetGlobalProgress();

        public static float GlobalProgress
        {
            get
            {
                if (!s_ProgressDirty)
                    return s_Progress;
                s_Progress = Internal_GetGlobalProgress();
                s_ProgressDirty = false;
                return s_Progress;
            }
        }

        [RequiredByNativeCode]
        private static void OnOperationStateCreated(int id)
        {
            if (!s_Initialized) return;
            var item = CreateProgressItem(id);

            s_ProgressDirty = true;
            OnAdded?.Invoke(new[] {item});
        }

        [RequiredByNativeCode]
        private static void OnOperationStateChanged(int id)
        {
            if (!s_Initialized) return;

            ProgressItem item = GetProgressById(id);
            Assert.IsNotNull(item);

            s_ProgressDirty = true;
            item.Dirty();

            OnUpdated?.Invoke(new[] {item});
        }

        [RequiredByNativeCode]
        private static void OnOperationsStateChanged(int[] ids)
        {
            if (!s_Initialized) return;
            if (ids.Length == 0) return;

            var items = new ProgressItem[ids.Length];
            var i = 0;

            foreach (var id in ids)
            {
                var item = GetProgressById(id);
                Assert.IsNotNull(item);
                item.Dirty();
                items[i++] = item;
            }
            s_ProgressDirty = true;

            OnUpdated?.Invoke(items);
        }

        [RequiredByNativeCode]
        private static void OnOperationStateRemoved(int id)
        {
            if (!s_Initialized) return;

            var item = GetProgressById(id);
            Assert.IsNotNull(item);

            s_ProgressDirty = true;
            s_ProgressItems.Remove(item);

            OnRemoved?.Invoke(new[] {item});
        }

        private static void RestoreProgressItems()
        {
            Assert.IsFalse(s_Initialized);

            s_ProgressItems.Clear();

            var n = GetCount();
            for (var i = 0; i < n; i++)
            {
                var id = GetId(i);
                CreateProgressItem(id);
            }
            s_Initialized = true;
            s_ProgressDirty = true;
        }

        private static ProgressItem CreateProgressItem(int id)
        {
            var item = new ProgressItem(id);
            s_ProgressItems.Add(item);
            return item;
        }
    }
}
