// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Analytics
{

    [NativeHeader("Modules/UnityConnect/UnityConnectSettings.h")]
    [NativeHeader("Modules/UnityConnect/UnityAnalytics/UnityAnalyticsSettings.h")]
    [StaticAccessor("GetUnityAnalyticsSettings()", StaticAccessorType.Dot)]
    public static partial class AnalyticsSettings
    {
        public static extern bool enabled { get; set; }

        public static extern bool testMode { get; set; }

        public static extern bool initializeOnStartup { get; set; }

        public static bool deviceStatsEnabledInBuild
        {
            get { return hasCoreStatsInBuild; }
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public static extern string eventUrl { get; set; }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public static extern string configUrl { get; set; }

        internal static extern void SetEnabledServiceWindow(bool enabled);

        internal static extern bool enabledForPlatform { get; }

        internal static extern void ApplyEnableSettings(BuildTarget target);

        public delegate bool RequireInBuildDelegate();
        public static event RequireInBuildDelegate OnRequireInBuildHandler = null;

        [RequiredByNativeCode]
        internal static bool RequiresCoreStatsInBuild()
        {
            if (OnRequireInBuildHandler == null)
                return false;

            Delegate[] invokeList = OnRequireInBuildHandler.GetInvocationList();
            for (int i = 0; i < invokeList.Length; ++i)
            {
                RequireInBuildDelegate func = (RequireInBuildDelegate)invokeList[i];
                if (func())
                    return true;
            }
            return false;
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        internal static extern bool hasCoreStatsInBuild
        {
            get;
        }
    }

}
