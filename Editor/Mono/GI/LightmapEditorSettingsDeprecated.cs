// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngineInternal;

namespace UnityEditor
{
    [System.Obsolete("LightmapBakeQuality has been deprecated.", false)]
    public enum LightmapBakeQuality
    {
        High = 0,
        Low = 1,
    }

    public partial class LightmapEditorSettings
    {
        [System.Obsolete("LightmapEditorSettings.aoContrast has been deprecated.", false)]
        public static float aoContrast
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.aoAmount has been deprecated.", false)]
        public static float aoAmount
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.lockAtlas has been deprecated.", false)]
        public static bool lockAtlas
        {
            get { return false; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.skyLightColor has been deprecated.", false)]
        public static Color skyLightColor
        {
            get { return Color.black; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.skyLightIntensity has been deprecated.", false)]
        public static float skyLightIntensity
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.quality has been deprecated.", false)]
        public static LightmapBakeQuality quality
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.bounceBoost has been deprecated.", false)]
        public static float bounceBoost
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.finalGatherRays has been deprecated.", false)]
        public static int finalGatherRays
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.finalGatherContrastThreshold has been deprecated.", false)]
        public static float finalGatherContrastThreshold
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.finalGatherGradientThreshold has been deprecated.", false)]
        public static float finalGatherGradientThreshold
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.finalGatherInterpolationPoints has been deprecated.", false)]
        public static int finalGatherInterpolationPoints
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.lastUsedResolution has been deprecated.", false)]
        public static float lastUsedResolution
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.bounces has been deprecated.", false)]
        public static int bounces
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("LightmapEditorSettings.bounceIntensity has been deprecated.", false)]
        public static float bounceIntensity
        {
            get { return 0; }
            set {}
        }
        [System.Obsolete("resolution is now called realtimeResolution (UnityUpgradable) -> realtimeResolution", false)]
        public static float resolution
        {
            get { return realtimeResolution; }
            set { realtimeResolution = value; }
        }
    }
}
