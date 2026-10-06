// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Export/Graphics/PrecompiledGraphicsState.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PrecompiledGraphicsStateStats
    {
        public readonly uint createdCount;
        public readonly uint precompiledCacheMissCount;
        public readonly uint allCachesMissCount;
        public readonly uint unknownCacheStatusCount;
        private readonly uint m_IsActive;

        public bool isActive => m_IsActive != 0;
    }

    [NativeHeader("Runtime/Export/Graphics/PrecompiledGraphicsState.bindings.h")]
    public static class PrecompiledGraphicsState
    {
        [FreeFunction("PrecompiledGraphicsState_Bindings::GetStats", IsThreadSafe = false)]
        public static extern PrecompiledGraphicsStateStats GetStats();
    }
}
