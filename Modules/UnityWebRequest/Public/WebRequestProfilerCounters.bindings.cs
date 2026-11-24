// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Networking
{
    // The charted counters are a separate hook from the capture; a caller recording without a
    // UnityWebRequest must drive both, or its rows appear while the chart stays flat.
    [NativeHeader("Modules/UnityWebRequest/Profiler/WebRequestProfilerCounters.h")]
    [StaticAccessor("WebRequestProfiler", StaticAccessorType.DoubleColon)]
    internal static class WebRequestProfilerCounters
    {
        [NativeMethod("RecordRequestStarted", IsThreadSafe = true)]
        internal static extern void RequestStarted();

        [NativeMethod("RecordRequestFinished", IsThreadSafe = true)]
        internal static extern void RequestFinished(bool succeeded);

        [NativeMethod("RecordRequestsAbandoned", IsThreadSafe = true)]
        internal static extern void RequestsAbandoned(uint count);
    }
}
