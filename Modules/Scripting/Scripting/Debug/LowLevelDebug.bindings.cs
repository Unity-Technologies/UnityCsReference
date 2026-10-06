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
    // Burst cannot compile the managed LogError(string) above, because the ScriptingCore bridge it
    // forwards to dispatches through a static property returning an interface. Instead, Burst rewrites
    // calls to LogError(string) into this overload and hands over the UTF-8 buffer it already built for
    // the interpolated string, so callers keep using LogError($"...") unchanged in both paths.
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

    // Unlike the warning and error paths, this one stays on the native binding rather than the
    // ScriptingCore Debug bridge: invokePostProcessCallbacks has to reach ScriptingLogException, and the
    // bridge's log_exception callback carries only the exception pointer. Callers in bursted jobs need
    // the post-process callbacks to run so the exception reaches LogAssert.
    [VisibleToOtherModules]
    [NativeMethod(IsThreadSafe = true)]
    internal extern static void LogException(Exception e, bool invokePostProcessCallbacks = false);
}
