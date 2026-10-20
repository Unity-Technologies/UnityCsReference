// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Scripting.LowLevel;

[NativeHeader("Modules/Scripting/Scripting/Debug/LowLevelDebug.bindings.h")]
[StaticAccessor("DebugLowLevel", StaticAccessorType.DoubleColon)]
internal static partial class Debug
{
    // Burst cannot compile the managed Log/LogWarning/LogError(string) above, because the ScriptingCore bridge it
    // forwards to dispatches through a static property returning an interface. Instead, Burst rewrites
    // calls to each of them into its __Unmanaged overload and hands over the UTF-8 buffer it already built for
    // the interpolated string, so callers keep using LogError($"...") and the others unchanged in both paths.
    //
    // The __Unmanaged suffix and the (byte*, int) pair per string parameter are what Burst matches on;
    // renaming either breaks the rewrite silently in bursted code. Same pattern as
    // Unity.Collections.MemoryLabelBindings.GetOrCreateMemLabel__Unmanaged.
    //
    // The buffer is not null terminated, hence the explicit length; the native side copies it.
    // Only ever referenced from Burst-generated code, so it must be kept from being stripped.
    [RequiredMember]
    [VisibleToOtherModules]
    [NativeMethod(IsThreadSafe = true)]
    internal static extern unsafe void LogError__Unmanaged(byte* message, int messageLen);

    [RequiredMember]
    [VisibleToOtherModules]
    [NativeMethod(IsThreadSafe = true)]
    internal static extern unsafe void Log__Unmanaged(byte* message, int messageLen);

    [RequiredMember]
    [VisibleToOtherModules]
    [NativeMethod(IsThreadSafe = true)]
    internal static extern unsafe void LogWarning__Unmanaged(byte* message, int messageLen);

    [NativeMethod("LogWarning", IsThreadSafe = true)]
    static extern void LogWarningMessage(string message);

    // Stays on the native binding rather than the ScriptingCore Debug bridge, whose log_exception callback
    // skips the post-process callbacks; like UnityEngine.Debug.LogException, this runs them so the exception
    // reaches LogAssert.
    [VisibleToOtherModules]
    [NativeMethod(IsThreadSafe = true)]
    internal extern static void LogException(Exception e);
}
