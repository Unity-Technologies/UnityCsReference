// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [StaticAccessor("Editor::Progress", StaticAccessorType.DoubleColon)]
    [NativeHeader(k_NativeHeader)]
    public static partial class Progress
    {
        const string k_NativeHeader = "Editor/Src/Progress.h";

        // Keep in sync with Editor\src\Progress.h
        [NativeType(Header = k_NativeHeader)]
        public enum Status
        {
            Running,
            Succeeded,
            Failed,
            Canceled,
            Paused
        }

        [Flags, NativeType(Header = k_NativeHeader)]
        public enum Options
        {
            None = 0 << 0,
            Sticky = 1 << 0,
            Indefinite = 1 << 1,
            Synchronous = 1 << 2,
            Managed = 1 << 3,
            Unmanaged = 1 << 4
        }

        [NativeType(Header = k_NativeHeader)]
        public enum TimeDisplayMode
        {
            NoTimeShown,
            ShowRunningTime,
            ShowRemainingTime
        }

        [NativeType(Header = k_NativeHeader)]
        public enum Priority
        {
            Unresponsive = 0,
            Idle = 1,
            Low = 2,
            Normal = 6,
            High = 10
        }

        [Flags, NativeType(Header = k_NativeHeader)]
        internal enum Updates : uint
        {
            NothingChanged = 0,
            StatusChanged = 1,
            ProgressChanged = 1 << 1,
            DescriptionChanged = 1 << 2,
            TimeDisplayModeChanged = 1 << 3,
            RemainingTimeChanged = 1 << 4,
            PriorityChanged = 1 << 5,
            CancellableChanged = 1 << 6,
            PausableChanged = 1 << 7,
            StepLabelChanged = 1 << 8,
            CurrentStepChanged = 1 << 9,
            TotalStepsChanged = 1 << 10,
            ElapsedTimeSinceLastPauseChanged = 1 << 11,
            LastResumeTimeChanged = 1 << 12,
            EndTimeChanged = 1 << 13,
            EverythingChanged = 0xffffffff
        }

        [NativeType(Header = k_NativeHeader)]
        internal enum ExplicitLoggingState
        {
            NotSet,
            Enabled,
            Disabled
        }

        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader(k_NativeHeader)]
        [RequiredByNativeCode(GenerateProxy = true)]
        internal struct ProgressIdAndUpdates
        {
            public int id;
            public Updates updates;
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

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::Remove")]
        private static extern int Internal_Remove(int id, bool forceSynchronous);

        public static int Remove(int id)
        {
            return Remove(id, false);
        }

        public static int Remove(int id, bool forceSynchronous)
        {
            return Internal_Remove(id, forceSynchronous);
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::Report")]
        private static extern void Internal_Report(int id, float progress, string description = null);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::Report")]
        private static extern void Internal_Report_Items(int id, int currentStep, int totalSteps, string description = null);

        public static void Report(int id, float progress)
        {
            Internal_Report(id, progress);
        }

        public static void Report(int id, int currentStep, int totalSteps)
        {
            Internal_Report_Items(id, currentStep, totalSteps);
        }

        public static void Report(int id, float progress, string description)
        {
            Internal_Report(id, progress, description);
            if (string.IsNullOrEmpty(description))
                SetDescription(id, description);
        }

        public static void Report(int id, int currentStep, int totalSteps, string description)
        {
            Internal_Report_Items(id, currentStep, totalSteps, description);
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

        public static extern bool Pause(int id);
        public static extern bool Resume(int id);

        [ThreadSafe]
        internal static extern void RegisterPauseCallbackFromScript(int id, Func<bool, bool> callback);

        public static void RegisterPauseCallback(int id, Func<bool, bool> callback)
        {
            RegisterPauseCallbackFromScript(id, callback);
        }

        [ThreadSafe]
        public static extern void UnregisterPauseCallback(int id);

        public static extern int GetCount();

        internal static extern int[] GetManagedNotifiedIds();

        public static extern int[] GetCountPerStatus();

        public static extern float GetProgress(int id);
        public static extern int GetCurrentStep(int id);
        public static extern int GetTotalSteps(int id);

        public static extern string GetName(int id);

        public static extern string GetDescription(int id);

        [ThreadSafe]
        public static extern void SetDescription(int id, string description);

        public static extern long GetStartDateTime(int id);

        public static extern long GetEndDateTime(int id);

        public static extern long GetUpdateDateTime(int id);

        public static extern int GetParentId(int id);

        public static extern int GetId(int index);

        public static extern bool IsCancellable(int id);

        public static extern bool IsPausable(int id);

        public static extern Status GetStatus(int id);

        public static extern Options GetOptions(int id);

        [ThreadSafe]
        public static extern void SetTimeDisplayMode(int id, TimeDisplayMode displayMode);

        [ThreadSafe]
        public static extern void SetRemainingTime(int id, long seconds);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::SetPriority")]
        private static extern void Internal_SetPriority(int id, int priority);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, Name = "Editor::Progress::SetPriority")]
        private static extern void Internal_SetPriority_Enum(int id, Priority priority);

        public static void SetPriority(int id, int priority)
        {
            Internal_SetPriority(id, priority);
        }

        public static void SetPriority(int id, Priority priority)
        {
            Internal_SetPriority_Enum(id, priority);
        }

        public static extern TimeDisplayMode GetTimeDisplayMode(int id);

        public static extern bool Exists(int id);

        public static extern long GetRemainingTime(int id);

        public static extern int GetPriority(int id);

        public static extern void ClearRemainingTime(int id);

        internal static extern long GetLastResumeDateTime(int id);

        internal static extern long GetElapsedTimeUntilLastPause(int id);

        [ThreadSafe]
        public static extern void SetStepLabel(int id, string label);

        public static extern string GetStepLabel(int id);

        public static void ShowDetails(bool shouldReposition = true)
        {
            ProgressWindow.ShowDetails(shouldReposition);
        }

        private static List<Item> s_ProgressItems = new List<Item>(8);
        private static bool s_Initialized;
        private static float s_Progress;
        private static bool s_ProgressDirty = true;
        private static TimeSpan s_RemainingTime = TimeSpan.Zero;
        private static bool s_RemainingTimeDirty = true;
        private static DateTime s_LastRemainingTimeUpdate = DateTime.Now;

        public static event Action<Item[]> added;
        public static event Action<Item[]> updated;
        public static event Action<Item[]> removed;

        public static IEnumerable<Item> EnumerateItems()
        {
            if (!s_Initialized)
                RestoreProgressItems();
            return s_ProgressItems;
        }

        public static Item GetProgressById(int id)
        {
            foreach (var progressItem in EnumerateItems())
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

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_ClearLogs")]
        internal static extern void ClearLogs();

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
            if (!s_Initialized)
                RestoreProgressItems();
            var item = CreateProgressItem(id);

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            added?.Invoke(new[] {item});
        }

        [RequiredByNativeCode]
        private static void OnOperationStateChanged(int id, Updates progressUpdates)
        {
            if (!s_Initialized)
                RestoreProgressItems();

            var item = GetProgressById(id);
            Assert.IsNotNull(item);

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            item.Dirty(progressUpdates);

            updated?.Invoke(new[] {item});
            item.ClearUpdates();
        }

        [RequiredByNativeCode]
        private static void OnOperationsStateChanged(ReadOnlySpan<ProgressIdAndUpdates> progressUpdates)
        {
            if (!s_Initialized)
                RestoreProgressItems();
            if (progressUpdates.Length == 0) return;

            var items = new Item[progressUpdates.Length];

            for (var i = 0; i < progressUpdates.Length; i++)
            {
                ref readonly var update = ref progressUpdates[i];
                var item = GetProgressById(update.id);
                Assert.IsNotNull(item);
                item.Dirty(update.updates);
                items[i] = item;
            }

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;

            updated?.Invoke(items);
            foreach (var item in items)
            {
                item.ClearUpdates();
            }
        }

        [RequiredByNativeCode]
        private static void OnOperationStateRemoved(int id)
        {
            if (!s_Initialized)
                RestoreProgressItems();

            var item = GetProgressById(id);
            Assert.IsNotNull(item);

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            s_ProgressItems.Remove(item);

            removed?.Invoke(new[] {item});
        }

        [RequiredByNativeCode]
        private static void OnOperationsStateRemoved(int[] ids)
        {
            if (!s_Initialized)
                RestoreProgressItems();

            var items = new Item[ids.Length];
            var i = 0;
            foreach (var id in ids)
            {
                var item = GetProgressById(id);
                items[i++] = item;
                Assert.IsNotNull(item);
                s_ProgressItems.Remove(item);
            }

            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;


            removed?.Invoke(items);
        }

        private static void RestoreProgressItems()
        {
            if (s_Initialized)
                return;

            s_ProgressItems.Clear();

            var alreadyCreatedIds = GetManagedNotifiedIds();
            foreach (var id in alreadyCreatedIds)
            {
                CreateProgressItem(id);
            }
            s_Initialized = true;
            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
        }

        private static Item CreateProgressItem(int id)
        {
            // It is possible for this function to be called during
            // RestoreProgressItems and right after in OnOperationStateCreated,
            // for the same id, if the progress comes from C++ and
            // the C# Progress class has not been initialized before. Therefore,
            // we have to check if the progress already exists in our list
            // otherwise we will have duplicates.
            var currentItem = s_ProgressItems.FirstOrDefault(i => i.id == id);
            if (currentItem != null)
                return currentItem;
            var item = new Item(id);
            s_ProgressItems.Add(item);
            return item;
        }

        internal static float GetMaxElapsedTime()
        {
            if (s_ProgressItems.Count == 0)
                return 0.0f;

            var maxElapsedTime = -1.0f;
            foreach (var progressItem in s_ProgressItems)
            {
                var elapsedTime = progressItem.elapsedTime;
                if (elapsedTime > maxElapsedTime)
                    maxElapsedTime = elapsedTime;
            }

            return maxElapsedTime;
        }

        // Anything below this line is for testing purposes only.
        internal static void ClearProgressItems()
        {
            s_ProgressItems.Clear();
            s_Initialized = false;
            s_ProgressDirty = true;
            s_RemainingTimeDirty = true;
            s_Progress = 0f;
            s_RemainingTime = TimeSpan.Zero;
            s_LastRemainingTimeUpdate = DateTime.Now;
        }

        internal static ExplicitLoggingState errorLoggingState
        {
            get => GetExplicitErrorLoggingState();
            set => SetExplicitErrorLoggingState(value);
        }
        internal static ExplicitLoggingState warningLoggingState
        {
            get => GetExplicitWarningLoggingState();
            set => SetExplicitWarningLoggingState(value);
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_GetExplicitErrorLoggingState")]
        static extern ExplicitLoggingState GetExplicitErrorLoggingState();

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_GetExplicitWarningLoggingState")]
        static extern ExplicitLoggingState GetExplicitWarningLoggingState();

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_SetExplicitErrorLoggingState")]
        static extern void SetExplicitErrorLoggingState(ExplicitLoggingState state);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_SetExplicitWarningLoggingState")]
        static extern void SetExplicitWarningLoggingState(ExplicitLoggingState state);

        internal static bool manualUpdate
        {
            get => IsManualUpdate();
            set => SetManualUpdate(value);
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_IsManualUpdate")]
        static extern bool IsManualUpdate();

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::Internal_SetManualUpdate")]
        static extern void SetManualUpdate(bool manualUpdate);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = false, Name = "Editor::Progress::ForceUpdateProgress")]
        internal static extern void ForceUpdate();
    }

    static class ProgressEnumExtensions
    {
        public static bool HasAny(this Progress.Updates flags, Progress.Updates f)
        {
            if (f == Progress.Updates.NothingChanged && flags == Progress.Updates.NothingChanged)
                return true;
            return (f & flags) != 0;
        }

        public static bool HasAll(this Progress.Updates flags, Progress.Updates all) => (flags & all) == all;
    }
}
