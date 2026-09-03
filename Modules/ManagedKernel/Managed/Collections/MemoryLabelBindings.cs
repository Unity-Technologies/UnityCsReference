// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Collections
{
    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerUnsafeUtility.bindings.h")]
    [StaticAccessor("profiling::ProfilerUnsafeUtility", StaticAccessorType.DoubleColon)]
    internal static class MemoryLabelBindings
    {
        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern IntPtr GetOrCreateMemLabel(string areaName, string objectName);

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        [RequiredMember]
        internal static extern unsafe IntPtr GetOrCreateMemLabel__Unmanaged(byte* areaName, int areaNameLen, byte* objectName, int objectNameLen);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern long GetMemLabelRelatedMemorySize(IntPtr label);
    }
}
