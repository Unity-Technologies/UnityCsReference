// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("PlatformDependent/Win/GfxDevice/d3d12/D3D12ValidationBindings.h")]
    [NativeConditional("(PLATFORM_WIN || PLATFORM_WINRT) && !PLATFORM_GAMECORE")]
    public static class D3D12Validation
    {
        [FreeFunction("ClearD3D12ValidationErrors", IsThreadSafe = false)]
        public static extern void ClearValidationErrors();

        [FreeFunction("GetD3D12ValidationErrorCount", IsThreadSafe = false)]
        public static extern int GetValidationErrorCount();

        [FreeFunction("GetD3D12ValidationError", IsThreadSafe = false)]
        public static extern string GetValidationError(int index);

        [FreeFunction("GetD3D12ValidationErrorsDroppedCount", IsThreadSafe = false)]
        public static extern int GetValidationErrorsDroppedCount();

        [FreeFunction("SetD3D12ValidationErrorLoggingSuppressed", IsThreadSafe = false)]
        public static extern void SetValidationErrorLoggingSuppressed(bool suppressed);

        [FreeFunction("IsD3D12ValidationErrorLoggingSuppressed", IsThreadSafe = false)]
        public static extern bool IsValidationErrorLoggingSuppressed();

        [FreeFunction("IsD3D12ValidationRequested", IsThreadSafe = false)]
        public static extern bool IsValidationRequested();

        [FreeFunction("IsD3D12ValidationActive", IsThreadSafe = false)]
        public static extern bool IsValidationActive();
    }
}
