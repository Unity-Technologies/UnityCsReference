// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.AdaptivePerformance
{
    internal static class APLog
    {
        [NoAutoStaticsCleanup] // logging toggle, set externally
        public static bool enabled = false;
        [NoAutoStaticsCleanup] // reusable string-building buffer, populated fresh each log call
        public static readonly StringBuilder s_LogBuilder = new StringBuilder(512);
        static readonly string s_AdaptivePerformancePrefix = "[Adaptive Performance] ";
        public static void Debug(string format, params object[] args)
        {
            if (!ShouldLog()) return;
            UnityEngine.Debug.LogFormat(s_AdaptivePerformancePrefix + format, args);
        }
        public static void LogMessage(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat( s_AdaptivePerformancePrefix + format, args);
        }
        public static bool ShouldLog()
        {
            return enabled && UnityEngine.Debug.isDebugBuild;
        }
    }
}
