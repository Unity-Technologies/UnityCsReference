// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/GfxDevice/GraphicsApiValidationBindings.h")]
    public static class GraphicsApiValidation
    {
        public static bool IsValidationSupported()
        {
            return false;
        }

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("ClearGraphicsApiValidationErrors", IsThreadSafe = false)]
        public static extern void ClearValidationErrors();

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("GetGraphicsApiValidationErrorCount", IsThreadSafe = false)]
        public static extern int GetValidationErrorCount();

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("GetGraphicsApiValidationError", IsThreadSafe = false)]
        public static extern string GetValidationError(int index);

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("GetGraphicsApiValidationErrorsDroppedCount", IsThreadSafe = false)]
        public static extern int GetValidationErrorsDroppedCount();

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("SetGraphicsApiValidationErrorLoggingSuppressed", IsThreadSafe = false)]
        public static extern void SetValidationErrorLoggingSuppressed(bool suppressed);

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("IsGraphicsApiValidationErrorLoggingSuppressed", IsThreadSafe = false)]
        public static extern bool IsValidationErrorLoggingSuppressed();

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("IsGraphicsApiValidationRequested", IsThreadSafe = false)]
        public static extern bool IsValidationRequested();

        [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
        [FreeFunction("IsGraphicsApiValidationActive", IsThreadSafe = false)]
        public static extern bool IsValidationActive();
    }
}
