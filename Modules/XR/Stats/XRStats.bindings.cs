// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Experimental;

namespace UnityEngine.XR.Provider
{
    public static class XRStats
    {
        public static bool TryGetStat(IntegratedSubsystem xrSubsystem, string tag, out float value)
        {
            return TryGetStat_Internal(xrSubsystem.m_Ptr, tag, out value);
        }

        [NativeHeader("Modules/XR/Stats/XRStats.h")]
        [NativeConditional("ENABLE_XR")]
        [StaticAccessor("XRStats::Get()", StaticAccessorType.Dot)]
        [NativeMethod("TryGetStatByName_Internal")]
        private static extern bool TryGetStat_Internal(IntPtr ptr, string tag, out float value);
    }
}
