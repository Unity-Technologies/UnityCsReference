// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Profiler/PerformanceTools/FrameDebugger.h")]
    [StaticAccessor("FrameDebugger", StaticAccessorType.DoubleColon)]
    public static class FrameDebugger
    {
        public static bool enabled
        {
            get => IsLocalEnabled() || IsRemoteEnabled();
        }

        internal static extern bool IsLocalEnabled();
        internal static extern bool IsRemoteEnabled();
    }
}
