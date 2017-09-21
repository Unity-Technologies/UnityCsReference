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
        [System.Obsolete("GIBakeBackend has been renamed to Lightmapper. (UnityUpgradable)", true)]
        public enum GIBakeBackend
        {
            Radiosity = 0,
            PathTracer = 1
        }
        [System.Obsolete("The giBakeBackend property has been renamed to lightmapper. (UnityUpgradable) -> lightmapper", false)]
        public static GIBakeBackend giBakeBackend
        {
            get
            {
                if (lightmapper == Lightmapper.ProgressiveCPU)
                    return GIBakeBackend.PathTracer;
                else
                    return GIBakeBackend.Radiosity;
            }
            set
            {
                if (value == GIBakeBackend.PathTracer)
                    lightmapper = Lightmapper.ProgressiveCPU;
                else
                    lightmapper = Lightmapper.Enlighten;
            }
        }
        [System.Obsolete("PathTracerSampling has been renamed to Sampling. (UnityUpgradable) -> UnityEditor.LightmapEditorSettings/Sampling", false)]
        public enum PathTracerSampling
        {
            Auto = 0,
            Fixed = 1
        }
        [System.Obsolete("The giPathTracerSampling property has been renamed to sampling. (UnityUpgradable) -> sampling", false)]
        public static PathTracerSampling giPathTracerSampling
        {
            get
            {
                if (sampling == Sampling.Auto)
                    return PathTracerSampling.Auto;
                else
                    return PathTracerSampling.Fixed;
            }
            set
            {
                if (value == PathTracerSampling.Auto)
                    sampling = Sampling.Auto;
                else
                    sampling = Sampling.Fixed;
            }
        }
        [System.Obsolete("PathTracerFilter has been renamed to FilterType. (UnityUpgradable) -> UnityEditor.LightmapEditorSettings/FilterType", false)]
        public enum PathTracerFilter
        {
            Gaussian = 0,
            ATrous = 1
        }
        [System.Obsolete("The giPathTracerFilter property has been deprecated. There are three independent properties to set individual filter types for direct, indirect and AO GI textures: filterTypeDirect, filterTypeIndirect and filterTypeAO.")]
        public static PathTracerFilter giPathTracerFilter
        {
            get
            {
                if (LightmapEditorSettings.filterTypeDirect == FilterType.Gaussian)
                    return PathTracerFilter.Gaussian;
                else
                    return PathTracerFilter.ATrous;
            }
            set
            {
                LightmapEditorSettings.filterTypeDirect = FilterType.Gaussian;
                LightmapEditorSettings.filterTypeIndirect = FilterType.Gaussian;
                LightmapEditorSettings.filterTypeAO = FilterType.Gaussian;
            }
        }
    }
}
