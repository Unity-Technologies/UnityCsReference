// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngineInternal;
using UnityEngine.Bindings;


namespace UnityEngine.Diagnostics
{
    public enum ForcedCrashCategory
    {
        AccessViolation = 0,
        FatalError = 1,
        Abort = 2,
        PureVirtualFunction = 3,
        MonoAbort = 4
    }

    [NativeHeader("Runtime/Export/Diagnostics/DiagnosticsUtils.bindings.h")]
    public static class Utils
    {
        [FreeFunction("DiagnosticsUtils_Bindings::ForceCrash", ThrowsException = true)]
        extern public static void ForceCrash(ForcedCrashCategory crashCategory);

        [FreeFunction("DiagnosticsUtils_Bindings::NativeAssert")]
        extern public static void NativeAssert(string message);

        [FreeFunction("DiagnosticsUtils_Bindings::NativeError")]
        extern public static void NativeError(string message);

        [FreeFunction("DiagnosticsUtils_Bindings::NativeWarning")]
        extern public static void NativeWarning(string message);
    }
}
