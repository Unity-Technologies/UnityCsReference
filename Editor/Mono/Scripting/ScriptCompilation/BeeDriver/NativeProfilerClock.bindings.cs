// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Managed access to the native profiler clock (baselib high_precision_clock).
    [NativeHeader("Editor/Mono/Scripting/ScriptCompilation/BeeDriver/NativeProfilerClock.bindings.h")]
    [StaticAccessor("NativeProfilerClock", StaticAccessorType.DoubleColon)]
    internal static class NativeProfilerClock
    {
        [NativeMethod(IsThreadSafe = true)]
        internal static extern long GetTicks();

        [NativeMethod(IsThreadSafe = true)]
        internal static extern long GetMicroseconds();
    }
}
