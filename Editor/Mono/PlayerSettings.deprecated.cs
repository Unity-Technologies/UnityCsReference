// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

using EditorGraphicsSettings = UnityEditor.Rendering.EditorGraphicsSettings;

namespace UnityEditor
{
    // deprecated in 5.1.
    [Obsolete("TargetGlesGraphics is ignored, use SetGraphicsAPIs/GetGraphicsAPIs APIs", false)]
    public enum TargetGlesGraphics
    {
        OpenGLES_1_x    = 0,
        OpenGLES_2_0    = 1,
        OpenGLES_3_0    = 2,
        Automatic       = -1,
    }
    [Obsolete("TargetIOSGraphics is ignored, use SetGraphicsAPIs/GetGraphicsAPIs APIs", false)]
    public enum TargetIOSGraphics
    {
        OpenGLES_2_0    = 2,
        OpenGLES_3_0    = 3,
        Metal           = 4,
        Automatic       = -1,
    }
    // deprecated in 5.3
    [Obsolete("Use Screen.SetResolution APIs", true)]
    public enum iOSTargetResolution
    {
        Native = 0,
        ResolutionAutoPerformance = 3,
        ResolutionAutoQuality = 4,
        Resolution320p = 5,
        Resolution640p = 6,
        Resolution768p = 7
    }
    // deprecated in 5.5
    [Obsolete("targetOSVersion is obsolete, use targetOSVersionString", false)]
    public enum iOSTargetOSVersion
    {
        iOS_4_0 = 10,
        iOS_4_1 = 12,
        iOS_4_2 = 14,
        iOS_4_3 = 16,
        iOS_5_0 = 18,
        iOS_5_1 = 20,
        iOS_6_0 = 22,
        iOS_7_0 = 24,
        iOS_7_1 = 26,
        iOS_8_0 = 28,
        iOS_8_1 = 30,
        Unknown = 999,
    }

    // deprecated in 5.6
    [Flags]
    [Obsolete("Use UnityEngine.iOS.SystemGestureDeferMode instead. (UnityUpgradable) -> [UnityEngine] UnityEngine.iOS.SystemGestureDeferMode", true)]
    public enum iOSSystemGestureDeferMode: uint
    {
        None = 0,
        TopEdge = 1 << 0,
        LeftEdge = 1 << 1,
        BottomEdge = 1 << 2,
        RightEdge = 1 << 3,
        All = TopEdge | LeftEdge | BottomEdge | RightEdge
    }

    partial class PlayerSettings
    {
        // deprecated since forever
        [Obsolete("The option alwaysDisplayWatermark is deprecated and is always false", true)]
        public static bool alwaysDisplayWatermark { get { return false; } set {} }
        [Obsolete("Use AssetBundles instead for streaming data", true)]
        public static int firstStreamedLevelWithResources { get { return 0; } set {} }

        // deprecated in 5.1.
        [Obsolete("targetGlesGraphics is ignored, use SetGraphicsAPIs/GetGraphicsAPIs APIs", false)]
        public static TargetGlesGraphics targetGlesGraphics { get { return TargetGlesGraphics.Automatic; } set {} }
        [Obsolete("targetIOSGraphics is ignored, use SetGraphicsAPIs/GetGraphicsAPIs APIs", false)]
        public static TargetIOSGraphics targetIOSGraphics { get { return TargetIOSGraphics.Automatic; } set {} }

        // deprecated in 5.5
        [Obsolete("Use PlayerSettings.iOS.locationUsageDescription instead (UnityUpgradable) -> UnityEditor.PlayerSettings/iOS.locationUsageDescription", false)]
        public static string locationUsageDescription { get { return iOS.locationUsageDescription; } set { iOS.locationUsageDescription = value; } }

        // deprecated in 5.5
        [Obsolete("renderingPath is ignored, use UnityEditor.Rendering.TierSettings with UnityEditor.Rendering.SetTierSettings/GetTierSettings instead", false)]
        public static RenderingPath renderingPath { get { return EditorGraphicsSettings.GetCurrentTierSettings().renderingPath; } set {} }
        [Obsolete("mobileRenderingPath is ignored, use UnityEditor.Rendering.TierSettings with UnityEditor.Rendering.SetTierSettings/GetTierSettings instead", false)]
        public static RenderingPath mobileRenderingPath { get { return EditorGraphicsSettings.GetCurrentTierSettings().renderingPath; } set {} }

        // deprecated in 5.6
        [Obsolete("Use PlayerSettings.applicationIdentifier instead (UnityUpgradable) -> UnityEditor.PlayerSettings.applicationIdentifier", true)]
        public static string bundleIdentifier { get { return applicationIdentifier; } set { applicationIdentifier = value; } }
    }

    partial class PlayerSettings
    {
        partial class iOS
        {
            // deprecated in 5.0
            [Obsolete("exitOnSuspend is deprecated, use appInBackgroundBehavior", false)]
            public static bool exitOnSuspend
            {
                get { return appInBackgroundBehavior == iOSAppInBackgroundBehavior.Exit; }
                set { appInBackgroundBehavior = iOSAppInBackgroundBehavior.Exit; }
            }

            // deprecated in 5.3
            [Obsolete("Use Screen.SetResolution at runtime", true)]
            public static iOSTargetResolution targetResolution { get { return 0; } set {} }

            // deprecated in 5.5
            [Obsolete("Use PlayerSettings.muteOtherAudioSources instead (UnityUpgradable) -> UnityEditor.PlayerSettings.muteOtherAudioSources", false)]
            public static bool overrideIPodMusic { get { return PlayerSettings.muteOtherAudioSources; } set { PlayerSettings.muteOtherAudioSources = value; } }
        }
    }
}

