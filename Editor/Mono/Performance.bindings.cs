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
}
