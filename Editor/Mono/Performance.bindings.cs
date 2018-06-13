// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/Performance.h"),
     StaticAccessor("Performance::Bindings", StaticAccessorType.DoubleColon)]
    internal static class Performance
    {
        public static extern bool IsTrackerExists(string name);
        public static extern int GetTrackerSampleCount(string name);
        public static extern double GetTrackerPeakTime(string name);
        public static extern double GetTrackerAverageTime(string name);
        public static extern double GetTrackerTotalTime(string name);
        public static extern double GetTrackerTotalUsage(string name);

        internal static extern int StartTracker(string name);
        internal static extern void StopTracker(int trackerToken);
    }

    internal struct PerformanceTracker : IDisposable
    {
        private bool m_Disposed;
        private readonly int m_WatchHandle;

        public PerformanceTracker(string name)
        {
            m_Disposed = false;
            m_WatchHandle = Performance.StartTracker(name);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Performance.StopTracker(m_WatchHandle);
        }
    }
}
