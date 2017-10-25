// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Advertisements
{

    [NativeHeader("Modules/UnityConnect/UnityAds/UnityAdsSettings.h")]
    [StaticAccessor("GetUnityAdsSettings()", StaticAccessorType.Dot)]
    public static partial class AdvertisementSettings
    {
        public static extern bool enabled { get; set; }

        public static extern bool testMode { get; set; }

        public static extern bool initializeOnStartup { get; set; }

        public static extern string GetGameId(RuntimePlatform platform);

        public static extern void SetGameId(RuntimePlatform platform, string gameId);

        [System.Obsolete("No longer supported and will always return true")]
        public static bool IsPlatformEnabled(RuntimePlatform platform)
        {
            return true;
        }

        [System.Obsolete("No longer supported and will do nothing")]
        public static void SetPlatformEnabled(RuntimePlatform platform, bool value)
        {
        }

        [NativeMethod("GetGameId")]
        public static extern string GetPlatformGameId(string platformName);

        [NativeMethod("SetGameId")]
        public static extern void SetPlatformGameId(string platformName, string gameId);

        internal static extern void SetEnabledServiceWindow(bool enabled);

        internal static extern bool enabledForPlatform { get; }

        internal static extern void ApplyEnableSettings(BuildTarget target);
    }

}
