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
    [StaticAccessor("Editor::Progress", StaticAccessorType.DoubleColon)]
    [NativeHeader("Editor/Src/Progress.h")]
    public static partial class Progress
    {
        // Keep in sync with Editor\src\Progress.h
        [NativeType(Header = "Editor/Src/Progress.h")]
        public enum Status
        {
            Running,
            Succeeded,
            Failed,
            Canceled
        }

        [Flags, NativeType(Header = "Editor/Src/Progress.h")]
        public enum Options
        {
            None = 0 << 0,
            Sticky = 1 << 0,
            Indefinite = 1 << 1,
            Synchronous = 1 << 2,
            Managed = 1 << 3,
            Unmanaged = 1 << 4
        }

        [NativeType(Header = "Editor/Src/Progress.h")]
        public enum TimeDisplayMode
        {
            NoTimeShown,
            ShowRunningTime,
            ShowRemainingTime
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::Start")]
        private static extern int Internal_Start(string name, string description, Options options, int parentId);

        public static int Start(string name, string description = null, Options options = Options.None, int parentId = -1)
        {
            // Automatically set C# progress as managed so they get canceled when a domain reload occurs.
            if ((options & Options.Unmanaged) == 0)
                options |= Options.Managed;
            return Internal_Start(name, description, options, parentId);
        }

        [ThreadSafe]
        public static extern void Finish(int id, Status status = Status.Succeeded);

        [ThreadSafe]
        public static extern int Remove(int id);

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

        public static extern float GetProgress(int id);

        public static extern string GetName(int id);

        public static extern string GetDescription(int id);

        [ThreadSafe]
        public static extern void SetDescription(int id, string description);

        public static extern long GetStartDateTime(int id);

        public static extern long GetUpdateDateTime(int id);

        public static extern int GetParentId(int id);

        public static extern int GetId(int index);

        public static extern bool IsCancellable(int id);

        public static extern Status GetStatus(int id);

        public static extern Options GetOptions(int id);

        [ThreadSafe]
        public static extern void SetTimeDisplayMode(int id, TimeDisplayMode displayMode);

        [ThreadSafe]
        public static extern void SetRemainingTime(int id, long seconds);

        public static extern TimeDisplayMode GetTimeDisplayMode(int id);

        public static extern bool Exists(int id);

        public static extern long GetRemainingTime(int id);

        public static extern void ClearRemainingTime(int id);

        public static void ShowDetails(bool shouldReposition = true)
        {
            EditorUIService.instance.ProgressWindowShowDetails(shouldReposition);
        }

        public static int RunTask(string name, string description, Func<int, object, IEnumerator> taskHandler, Options options = Options.None, int parentId = -1, object userData = null)
        {
            var progressId = Start(name, description, options, parentId);
            s_Tasks.Add(new Task { id = progressId, handler = taskHandler, userData = userData, iterators = new Stack<IEnumerator>() });

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
                            Finish(task.id, Status.Succeeded);
                            s_Tasks.RemoveAt(taskIndex);
                        }

                        continue;
                    }

                    var cEnumerable = iterator.Current as IEnumerable;
                    if (cEnumerable != null)
                    {
                        ++taskIndex;
                        task.iterators.Push(cEnumerable.GetEnumerator());
                        continue;
                    }

                    var cEnumerator = iterator.Current as IEnumerator;
                    if (cEnumerator != null)
                    {
                        ++taskIndex;
                        task.iterators.Push(cEnumerator);
                        continue;
                    }

                    var report = iterator.Current as TaskReport;
                    if (report?.error != null)
                    {
                        SetDescription(task.id, report.error);
                        Finish(task.id, Status.Failed);
                        s_Tasks.RemoveAt(taskIndex);
                    }
                    else
                    {
                        if (report != null)
                        {
                            if (string.IsNullOrEmpty(report.description))
                                Report(task.id, report.progress);
                            else
                                Report(task.id, report.progress, report.description);
                        }
                        else if ((GetOptions(task.id) & Options.Indefinite) == Options.Indefinite)
                        {
                            Report(task.id, -1f);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    SetDescription(task.id, ex.Message);
                    Finish(task.id, Status.Failed);
                    s_Tasks.RemoveAt(taskIndex);
                }
            }

            if (s_Tasks.Count == 0)
                EditorApplication.update -= RunTasks;
        }

        private static List<Item> s_ProgressItems = new List<Item>(8);
        private static bool s_Initialized = false;
        private static float s_Progress = 0;
        private static bool s_ProgressDirty = true;
        private static TimeSpan s_RemainingTime = TimeSpan.Zero;
        private static bool s_RemainingTimeDirty = true;
        private static DateTime s_LastRemainingTimeUpdate = DateTime.Now;
        private static List<Task> s_Tasks = new List<Task>();

        public static event Action<Item[]> added;
        public static event Action<Item[]> updated;
        public static event Action<Item[]> removed;

        static Progress()
        {
            RestoreProgressItems(); // on domain reload - request all native operations
        }

        public static IEnumerable<Item> EnumerateItems()
        {
            return s_ProgressItems;
        }

        public static Item GetProgressById(int id)
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
            return GetCountPerStatus()[(int)Status.Running];
        }

        public static bool running => GetRunningProgressCount() > 0;

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::GetGlobalProgress")]
        private static extern float Internal_GetGlobalProgress();

        public static float globalProgress
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

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::GetGlobalRemainingTime")]
        private static extern long Internal_GetGlobalRemainingTime();

        public static TimeSpan globalRemainingTime
        {
            get
            {
                if (s_RemainingTimeDirty)
                {
                    s_RemainingTime = Item.SecToTimeSpan(Internal_GetGlobalRemainingTime());
                    s_LastRemainingTimeUpdate = DateTime.Now;
                    s_RemainingTimeDirty = false;
                }

                var duration = (DateTime.Now - s_LastRemainingTimeUpdate).Duration();
                return s_RemainingTime - new TimeSpan(duration.Days, duration.Hours, duration.Minutes, duration.Seconds);
            }
        }

        [RequiredByNativeCode]
        private static void OnOperationStateCreated(int id)
        {
            if (!s_Initialized) return;
            var item = CreateProgressItem(id);

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            added?.Invoke(new[] {item});
        }

        [RequiredByNativeCode]
        private static void OnOperationStateChanged(int id)
        {
            if (!s_Initialized) return;

            var item = GetProgressById(id);
            Assert.IsNotNull(item);

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            item.Dirty();

            updated?.Invoke(new[] {item});
        }

        [RequiredByNativeCode]
        private static void OnOperationsStateChanged(int[] ids)
        {
            if (!s_Initialized) return;
            if (ids.Length == 0) return;

            var items = new Item[ids.Length];
            var i = 0;

            foreach (var id in ids)
            {
                var item = GetProgressById(id);
                Assert.IsNotNull(item);
                item.Dirty();
                items[i++] = item;
            }
            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;

            updated?.Invoke(items);
        }

        [RequiredByNativeCode]
        private static void OnOperationStateRemoved(int id)
        {
            if (!s_Initialized) return;

            var item = GetProgressById(id);
            Assert.IsNotNull(item);

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            s_ProgressItems.Remove(item);

            removed?.Invoke(new[] {item});
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
            s_RemainingTimeDirty = true;
        }

        private static Item CreateProgressItem(int id)
        {
            var item = new Item(id);
            s_ProgressItems.Add(item);
            return item;
        }
    }
}
