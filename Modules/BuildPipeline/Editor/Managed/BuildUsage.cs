// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Serialize/BuildUsageTags.h")]
    public struct BuildUsageTagGlobal
    {
        internal uint m_LightmapModesUsed;
        internal uint m_LegacyLightmapModesUsed;
        internal uint m_DynamicLightmapsUsed;
        internal uint m_FogModesUsed;
        internal bool m_ShadowMasksUsed;
        internal bool m_SubtractiveUsed;

        public static BuildUsageTagGlobal operator|(BuildUsageTagGlobal x, BuildUsageTagGlobal y)
        {
            var results = new BuildUsageTagGlobal();
            results.m_LightmapModesUsed = x.m_LightmapModesUsed | y.m_LightmapModesUsed;
            results.m_LegacyLightmapModesUsed = x.m_LegacyLightmapModesUsed | y.m_LegacyLightmapModesUsed;
            results.m_DynamicLightmapsUsed = x.m_LightmapModesUsed | y.m_DynamicLightmapsUsed;
            results.m_FogModesUsed = x.m_FogModesUsed | y.m_FogModesUsed;
            results.m_ShadowMasksUsed = x.m_ShadowMasksUsed | y.m_ShadowMasksUsed;
            results.m_SubtractiveUsed = x.m_SubtractiveUsed | y.m_SubtractiveUsed;
            return results;
        }
    }
}
