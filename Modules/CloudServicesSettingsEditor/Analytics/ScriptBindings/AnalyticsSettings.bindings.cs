// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Analytics
{

    [NativeHeader("Modules/UnityConnect/UnityAnalytics/UnityAnalyticsSettings.h")]
    [StaticAccessor("GetUnityAnalyticsSettings()", StaticAccessorType.Dot)]
    public static partial class AnalyticsSettings
    {
        public static extern bool enabled { get; set; }

        public static extern bool testMode { get; set; }

        internal static extern void SetEnabledServiceWindow(bool enabled);

        internal static extern bool enabledForPlatform { get; }

        internal static extern void ApplyEnableSettings(BuildTarget target);
    }

}
