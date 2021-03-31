// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("Unity.PerformanceTracking.Editor")]

namespace UnityEditor.Profiling
{
    [NativeHeader("Editor/Src/Utility/Performance.h"),
     StaticAccessor("Performance::Bindings", StaticAccessorType.DoubleColon)]
    internal struct EditorPerformanceTracker : IDisposable
    {
        private bool m_Disposed;
        private readonly int m_WatchHandle;

        public EditorPerformanceTracker(string name)
        {
            m_Disposed = false;
            m_WatchHandle = StartTracker(name);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            StopTracker(m_WatchHandle);
        }

        internal const ulong InvalidTrackerHandle = ulong.MaxValue;
        private static extern ulong GetOrCreateTrackerHandleImpl(string trackerName, Type context);

        // This method is for internal use only. If you do not provide a type, we cannot
        // map the marker to an assembly.
        internal static ulong GetOrCreateTrackerHandle(string trackerName) => GetOrCreateTrackerHandleImpl(trackerName, null);

        public static ulong GetOrCreateTrackerHandle(string trackerName, Type context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return GetOrCreateTrackerHandleImpl(trackerName, context);
        }

        public static extern bool TryStartTrackerByHandle(ulong trackerHandle, out int trackerToken);

        public static extern string[] GetAvailableTrackers();
        public static extern bool Exists(string trackerName);
        public static extern void Reset(string trackerName);
        public static extern int GetSampleCount(string trackerName);
        public static extern double GetLastTime(string trackerName);
        public static extern double GetPeakTime(string trackerName);
        public static extern double GetAverageTime(string trackerName);
        public static extern double GetTotalTime(string trackerName);
        public static extern double GetTotalUsage(string trackerName);
        public static extern double GetTimestamp(string trackerName);
        public static extern void LogCallstack(string trackerName);
        public static extern void GetCallstack(string trackerName, Action<string> onCallstackCaptured);

        internal static extern int StartTracker(string trackerName);
        internal static extern void StopTracker(int trackerToken);
        internal static extern bool IsTrackerActive(int trackerToken);
    }

    struct EditorPerformanceMarker
    {
        private readonly ulong m_TrackerHandle;

        // This constructor is for internal use only. If you do not provide a type, we cannot attribute it to
        // any assembly.
        internal EditorPerformanceMarker(string name)
        {
            m_TrackerHandle = EditorPerformanceTracker.GetOrCreateTrackerHandle(name);
        }

        public EditorPerformanceMarker(string name, Type type)
        {
            m_TrackerHandle = EditorPerformanceTracker.GetOrCreateTrackerHandle(name, type);
        }

        [MethodImpl(256)] // 256 : Aggressive inlining
        public AutoScope Auto()
        {
            return new AutoScope(m_TrackerHandle);
        }

        public struct AutoScope : IDisposable
        {
            private int m_WatchHandle;

            [MethodImpl(256)]
            internal AutoScope(ulong trackerHandle)
            {
                EditorPerformanceTracker.TryStartTrackerByHandle(trackerHandle, out m_WatchHandle);
            }

            [MethodImpl(256)]
            public void Dispose()
            {
                EditorPerformanceTracker.StopTracker(m_WatchHandle);
                m_WatchHandle = -1;
            }
        }
    }
}
