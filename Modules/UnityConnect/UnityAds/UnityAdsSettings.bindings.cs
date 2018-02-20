// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Advertisements
{
    [NativeHeader("Modules/UnityConnect/UnityAds/UnityAdsSettings.h")]
    static internal class UnityAdsSettings
    {
        [ThreadAndSerializationSafe()]
        [StaticAccessor("GetUnityAdsSettings()", StaticAccessorType.Dot)]
        public extern static bool enabled { get; set; }

        [Obsolete("warning No longer supported and will always return true")]
        public static bool IsPlatformEnabled(RuntimePlatform platform)
        {
            return true;
        }

        [Obsolete("warning No longer supported and will do nothing")]
        public static void SetPlatformEnabled(RuntimePlatform platform, bool value)
        {
        }

        [StaticAccessor("GetUnityAdsSettings()", StaticAccessorType.Dot)]
        public extern static bool initializeOnStartup { get; set; }
        [StaticAccessor("GetUnityAdsSettings()", StaticAccessorType.Dot)]
        public extern static bool testMode { get; set; }

        [StaticAccessor("GetUnityAdsSettings()", StaticAccessorType.Dot)]
        public extern static string GetGameId(RuntimePlatform platform);

        [StaticAccessor("GetUnityAdsSettings()", StaticAccessorType.Dot)]
        public extern static void SetGameId(RuntimePlatform platform, string gameId);
    }
}
