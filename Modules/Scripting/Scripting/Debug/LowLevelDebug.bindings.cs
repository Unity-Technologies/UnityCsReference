// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Scripting.LowLevel;

[NativeHeader("Modules/Scripting/Scripting/Debug/LowLevelDebug.bindings.h")]
[StaticAccessor("DebugLowLevel", StaticAccessorType.DoubleColon)]
internal static partial class Debug
{
    [NativeMethod(IsThreadSafe = true)]
    internal extern static unsafe void LogWarning(void* message);
    [NativeMethod(IsThreadSafe = true)]
    internal extern static unsafe void LogError(void* message);
    [NativeMethod(IsThreadSafe = true)]
    internal extern static unsafe void LogAssertion(void* message);
    [NativeMethod(IsThreadSafe = true)]
    internal extern static void LogException(Exception e);
}
