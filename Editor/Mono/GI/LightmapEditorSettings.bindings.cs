// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngineInternal;
using UnityEditor.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEditor
{
    // Various settings for the bake.
    [NativeHeader("Editor/Src/LightmapEditorSettings.h")]
    public static partial class LightmapEditorSettings
    {
        [Obsolete("LightmapEditorSettings.Lightmapper is obsolete. Use LightingSettings.Lightmapper instead. ", false)]
        public enum Lightmapper
        {
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [Obsolete("Use Lightmapper.ProgressiveCPU instead. (UnityUpgradable) -> UnityEditor.LightmapEditorSettings/Lightmapper.ProgressiveCPU", true)]
            Radiosity = 0,
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [Obsolete("Use Lightmapper.ProgressiveCPU instead. (UnityUpgradable) -> UnityEditor.LightmapEditorSettings/Lightmapper.ProgressiveCPU", false)]
            Enlighten = 0,
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            [Obsolete("Use Lightmapper.ProgressiveCPU instead. (UnityUpgradable) -> UnityEditor.LightmapEditorSettings/Lightmapper.ProgressiveCPU", true)]
            PathTracer = 1,

            // Lightmaps are baked by the CPU Progressive lightmapper (Wintermute + OpenRL based).
            ProgressiveCPU = 1,

            // Lightmaps are baked by the GPU Progressive lightmapper (RadeonRays + OpenCL based).
            ProgressiveGPU = 2
        }

        [Obsolete("LightmapEditorSettings.Sampling is obsolete. Use LightingSettings.Sampling instead. ", false)]
        public enum Sampling
        {
            Auto = 0,
            Fixed = 1
        }

        [Obsolete("LightmapEditorSettings.FilterMode is obsolete. Use LightingSettings.FilterMode instead. ", false)]
        public enum FilterMode
        {
            None = 0,
            Auto = 1,
            Advanced = 2
        }

        [Obsolete("LightmapEditorSettings.DenoiserType is obsolete. Use LightingSettings.DenoiserType instead. ", false)]
        public enum DenoiserType
        {
            None = 0,
            Optix = 1,
            OpenImage = 2,

            // The AMD Radeon Pro Image Processing denoiser is applied.
            RadeonPro = 3
        }

        [Obsolete("LightmapEditorSettings.FilterType is obsolete. Use LightingSettings.FilterType instead. ", false)]
        public enum FilterType
        {
            Gaussian = 0,
            ATrous = 1,
            None = 2
        }

#pragma warning disable 0618
        [Obsolete("LightmapEditorSettings.lightmapper is obsolete, use LightingSettings.lightmapper instead. ", false)]
        public static Lightmapper lightmapper
        {
            get { return ConvertToOldLightmapperEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapper); }
            set { Lightmapping.GetOrCreateLightingsSettings().lightmapper = ConvertToNewLightmapperEnum(value); }
        }

        private static Lightmapper ConvertToOldLightmapperEnum(LightingSettings.Lightmapper lightmapper)
        {
            switch (lightmapper)
            {
                case LightingSettings.Lightmapper.Enlighten:
                    return Lightmapper.Enlighten;

                case LightingSettings.Lightmapper.ProgressiveCPU:
                    return Lightmapper.ProgressiveCPU;

                case LightingSettings.Lightmapper.ProgressiveGPU:
                    return Lightmapper.ProgressiveGPU;

                default:
                {
                    Debug.LogError("Unsupported Lightmapper type was added and not handled correctly. ");
                    return Lightmapper.ProgressiveCPU;
                }
            }
        }

        private static LightingSettings.Lightmapper ConvertToNewLightmapperEnum(Lightmapper lightmapper)
        {
            switch (lightmapper)
            {
                case Lightmapper.Enlighten:
                    return LightingSettings.Lightmapper.Enlighten;

                case Lightmapper.ProgressiveCPU:
                    return LightingSettings.Lightmapper.ProgressiveCPU;

                case Lightmapper.ProgressiveGPU:
                    return LightingSettings.Lightmapper.ProgressiveGPU;

                default:
                {
                    Debug.LogError("Unsupported Lightmapper type was added and not handled correctly. ");
                    return LightingSettings.Lightmapper.ProgressiveCPU;
                }
            }
        }

#pragma warning restore 0618

        [Obsolete("LightmapEditorSettings.lightmapsMode is obsolete, use LightingSettings.directionalityMode instead. ", false)]
        public static LightmapsMode lightmapsMode
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().directionalityMode; }
            set { Lightmapping.GetOrCreateLightingsSettings().directionalityMode = value; }
        }

        [Obsolete("LightmapEditorSettings.mixedBakeMode is obsolete, use LightingSettings.mixedBakeMode instead. ", false)]
        public static MixedLightingMode mixedBakeMode
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().mixedBakeMode; }
            set { Lightmapping.GetOrCreateLightingsSettings().mixedBakeMode = value; }
        }

#pragma warning disable 0618
        [Obsolete("LightmapEditorSettings.sampling is obsolete, use LightingSettings.sampling instead. ", false)]
        public static Sampling sampling
        {
            get { return ConvertToOldSamplingEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().sampling); }
            set { Lightmapping.GetOrCreateLightingsSettings().sampling = ConvertToNewSamplingEnum(value); }
        }

        private static Sampling ConvertToOldSamplingEnum(LightingSettings.Sampling lightmapper)
        {
            switch (lightmapper)
            {
                case LightingSettings.Sampling.Auto:
                    return Sampling.Auto;

                case LightingSettings.Sampling.Fixed:
                    return Sampling.Fixed;

                default:
                {
                    Debug.LogError("Unsupported Sampling type was added and not handled correctly. ");
                    return Sampling.Fixed;
                }
            }
        }

        private static LightingSettings.Sampling ConvertToNewSamplingEnum(Sampling lightmapper)
        {
            switch (lightmapper)
            {
                case Sampling.Auto:
                    return LightingSettings.Sampling.Auto;

                case Sampling.Fixed:
                    return LightingSettings.Sampling.Fixed;

                default:
                {
                    Debug.LogError("Unsupported Sampling type was added and not handled correctly. ");
                    return LightingSettings.Sampling.Fixed;
                }
            }
        }

#pragma warning restore 0618

        [Obsolete("LightmapEditorSettings.directSampleCount is obsolete, use LightingSettings.directSampleCount instead. ", false)]
        public static int directSampleCount
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().directSampleCount; }
            set { Lightmapping.GetOrCreateLightingsSettings().directSampleCount = value; }
        }

        [Obsolete("LightmapEditorSettings.indirectSampleCount is obsolete, use LightingSettings.indirectSampleCount instead. ", false)]
        public static int indirectSampleCount
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().indirectSampleCount; }
            set { Lightmapping.GetOrCreateLightingsSettings().indirectSampleCount = value; }
        }

        [Obsolete("LightmapEditorSettings.bounces is obsolete, use LightingSettings.maxBounces instead. ", false)]
        public static int bounces
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().maxBounces; }
            set { Lightmapping.GetOrCreateLightingsSettings().maxBounces = value; }
        }

        [Obsolete("LightmapEditorSettings.prioritizeView is obsolete, use LightingSettings.prioritizeView instead. ", false)]
        public static bool prioritizeView
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().prioritizeView; }
            set { Lightmapping.GetOrCreateLightingsSettings().prioritizeView = value; }
        }

#pragma warning disable 0618
        [Obsolete("LightmapEditorSettings.filteringMode is obsolete, use LightingSettings.filteringMode instead. ", false)]
        public static FilterMode filteringMode
        {
            get { return ConvertToOldFilteringModeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringMode); }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringMode = ConvertToNewFilteringModeEnum(value); }
        }

        private static FilterMode ConvertToOldFilteringModeEnum(LightingSettings.FilterMode filteringMode)
        {
            switch (filteringMode)
            {
                case LightingSettings.FilterMode.None:
                    return FilterMode.None;

                case LightingSettings.FilterMode.Auto:
                    return FilterMode.Auto;

                case LightingSettings.FilterMode.Advanced:
                    return FilterMode.Advanced;

                default:
                {
                    Debug.LogError("Unsupported FilterMode type was added and not handled correctly. ");
                    return FilterMode.Advanced;
                }
            }
        }

        private static LightingSettings.FilterMode ConvertToNewFilteringModeEnum(FilterMode filteringMode)
        {
            switch (filteringMode)
            {
                case FilterMode.None:
                    return LightingSettings.FilterMode.None;

                case FilterMode.Auto:
                    return LightingSettings.FilterMode.Auto;

                case FilterMode.Advanced:
                    return LightingSettings.FilterMode.Advanced;

                default:
                {
                    Debug.LogError("Unsupported FilterMode type was added and not handled correctly. ");
                    return LightingSettings.FilterMode.Advanced;
                }
            }
        }

        [Obsolete("LightmapEditorSettings.denoiserTypeDirect is obsolete, use LightingSettings.denoiserTypeDirect instead. ", false)]
        public static DenoiserType denoiserTypeDirect
        {
            get { return ConvertToOldDenoiserTypeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().denoiserTypeDirect); }
            set { Lightmapping.GetOrCreateLightingsSettings().denoiserTypeDirect = ConvertToNewDenoiserTypeEnum(value); }
        }

        [Obsolete("LightmapEditorSettings.denoiserTypeIndirect is obsolete, use LightingSettings.denoiserTypeIndirect instead. ", false)]
        public static DenoiserType denoiserTypeIndirect
        {
            get { return ConvertToOldDenoiserTypeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().denoiserTypeIndirect); }
            set { Lightmapping.GetOrCreateLightingsSettings().denoiserTypeIndirect = ConvertToNewDenoiserTypeEnum(value); }
        }

        [Obsolete("LightmapEditorSettings.denoiserTypeAO is obsolete, use LightingSettings.denoiserTypeAO instead. ", false)]
        public static DenoiserType denoiserTypeAO
        {
            get { return ConvertToOldDenoiserTypeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().denoiserTypeAO); }
            set { Lightmapping.GetOrCreateLightingsSettings().denoiserTypeAO = ConvertToNewDenoiserTypeEnum(value); }
        }

        private static DenoiserType ConvertToOldDenoiserTypeEnum(LightingSettings.DenoiserType denoiserType)
        {
            switch (denoiserType)
            {
                case LightingSettings.DenoiserType.None:
                    return DenoiserType.None;

                case LightingSettings.DenoiserType.Optix:
                    return DenoiserType.Optix;

                case LightingSettings.DenoiserType.OpenImage:
                    return DenoiserType.OpenImage;

                case LightingSettings.DenoiserType.RadeonPro:
                    return DenoiserType.RadeonPro;

                default:
                {
                    Debug.LogError("Unsupported DenoiserType type was added and not handled correctly. ");
                    return DenoiserType.None;
                }
            }
        }

        private static LightingSettings.DenoiserType ConvertToNewDenoiserTypeEnum(DenoiserType denoiserType)
        {
            switch (denoiserType)
            {
                case DenoiserType.None:
                    return LightingSettings.DenoiserType.None;

                case DenoiserType.Optix:
                    return LightingSettings.DenoiserType.Optix;

                case DenoiserType.OpenImage:
                    return LightingSettings.DenoiserType.OpenImage;

                case DenoiserType.RadeonPro:
                    return LightingSettings.DenoiserType.RadeonPro;

                default:
                {
                    Debug.LogError("Unsupported DenoiserType type was added and not handled correctly. ");
                    return LightingSettings.DenoiserType.None;
                }
            }
        }

        [Obsolete("LightmapEditorSettings.filterTypeDirect is obsolete, use LightingSettings.filterTypeDirect instead. ", false)]
        public static FilterType filterTypeDirect
        {
            get {  return ConvertToOldFilterTypeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().filterTypeDirect); }
            set { Lightmapping.GetOrCreateLightingsSettings().filterTypeDirect = ConvertToNewFilterTypeEnum(value); }
        }

        [Obsolete("LightmapEditorSettings.filterTypeIndirect is obsolete, use LightingSettings.filterTypeIndirect instead. ", false)]
        public static FilterType filterTypeIndirect
        {
            get { return ConvertToOldFilterTypeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().filterTypeIndirect); }
            set { Lightmapping.GetOrCreateLightingsSettings().filterTypeIndirect = ConvertToNewFilterTypeEnum(value); }
        }

        [Obsolete("LightmapEditorSettings.filterTypeAO is obsolete, use LightingSettings.filterTypeAO instead. ", false)]
        public static FilterType filterTypeAO
        {
            get { return ConvertToOldFilterTypeEnum(Lightmapping.GetLightingSettingsOrDefaultsFallback().filterTypeAO); }
            set { Lightmapping.GetOrCreateLightingsSettings().filterTypeAO = ConvertToNewFilterTypeEnum(value); }
        }

        private static FilterType ConvertToOldFilterTypeEnum(LightingSettings.FilterType filterType)
        {
            switch (filterType)
            {
                case LightingSettings.FilterType.Gaussian:
                    return FilterType.Gaussian;

                case LightingSettings.FilterType.ATrous:
                    return FilterType.ATrous;

                case LightingSettings.FilterType.None:
                    return FilterType.None;

                default:
                {
                    Debug.LogError("Unsupported FilterType type was added and not handled correctly. ");
                    return FilterType.None;
                }
            }
        }

        private static LightingSettings.FilterType ConvertToNewFilterTypeEnum(FilterType filterType)
        {
            switch (filterType)
            {
                case FilterType.Gaussian:
                    return LightingSettings.FilterType.Gaussian;

                case FilterType.ATrous:
                    return LightingSettings.FilterType.ATrous;

                case FilterType.None:
                    return LightingSettings.FilterType.None;

                default:
                {
                    Debug.LogError("Unsupported FilterType type was added and not handled correctly. ");
                    return LightingSettings.FilterType.None;
                }
            }
        }

#pragma warning restore 0618

        [Obsolete("LightmapEditorSettings.filteringGaussRadiusDirect is obsolete, use LightingSettings.filteringGaussianRadiusDirect instead. ", false)]
        public static int filteringGaussRadiusDirect
        {
            get { return (int)Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringGaussianRadiusDirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringGaussianRadiusDirect = (float)value; }
        }

        [Obsolete("LightmapEditorSettings.filteringGaussRadiusIndirect is obsolete, use LightingSettings.filteringGaussianRadiusIndirect instead. ", false)]
        public static int filteringGaussRadiusIndirect
        {
            get { return (int)Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringGaussianRadiusIndirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringGaussianRadiusIndirect = (float)value; }
        }

        [Obsolete("LightmapEditorSettings.filteringGaussRadiusAO is obsolete, use LightingSettings.filteringGaussianRadiusAO instead. ", false)]
        public static int filteringGaussRadiusAO
        {
            get { return (int)Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringGaussianRadiusAO; }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringGaussianRadiusAO = (float)value; }
        }

        [Obsolete("LightmapEditorSettings.filteringAtrousPositionSigmaDirect is obsolete, use LightingSettings.filteringAtrousPositionSigmaDirect instead. ", false)]
        public static float filteringAtrousPositionSigmaDirect
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringAtrousPositionSigmaDirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringAtrousPositionSigmaDirect = value; }
        }

        [Obsolete("LightmapEditorSettings.filteringAtrousPositionSigmaIndirect is obsolete, use LightingSettings.filteringAtrousPositionSigmaIndirect instead. ", false)]
        public static float filteringAtrousPositionSigmaIndirect
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringAtrousPositionSigmaIndirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringAtrousPositionSigmaIndirect = value; }
        }

        [Obsolete("LightmapEditorSettings.filteringAtrousPositionSigmaIndirect is obsolete, use LightingSettings.filteringAtrousPositionSigmaIndirect instead. ", false)]
        public static float filteringAtrousPositionSigmaAO
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().filteringAtrousPositionSigmaIndirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().filteringAtrousPositionSigmaIndirect = value; }
        }

        [Obsolete("LightmapEditorSettings.environmentMIS is obsolete, use LightingSettings.environmentImportanceSampling instead. ", false)]
        internal static int environmentMIS
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().environmentImportanceSampling ? 1 : 0; }
            set { Lightmapping.GetOrCreateLightingsSettings().environmentImportanceSampling = value != 0; }
        }

        [Obsolete("LightmapEditorSettings.environmentSampleCount is obsolete, use LightingSettings.environmentSampleCount instead. ", false)]
        public static int environmentSampleCount
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().environmentSampleCount; }
            set { Lightmapping.GetOrCreateLightingsSettings().environmentSampleCount = value; }
        }

        [Obsolete("LightmapEditorSettings.environmentReferencePointCount is obsolete, use LightingSettings.environmentReferencePointCount instead. ", false)]
        internal static int environmentReferencePointCount
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().environmentReferencePointCount; }
            set { Lightmapping.GetOrCreateLightingsSettings().environmentReferencePointCount = value; }
        }

        [Obsolete("LightmapEditorSettings.lightProbeSampleCountMultiplier is obsolete, use LightingSettings.lightProbeSampleCountMultiplier instead. ", false)]
        internal static float lightProbeSampleCountMultiplier
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().lightProbeSampleCountMultiplier; }
            set { Lightmapping.GetOrCreateLightingsSettings().lightProbeSampleCountMultiplier = value; }
        }

        [Obsolete("LightmapEditorSettings.maxAtlasSize is obsolete, use LightingSettings.lightmapMaxSize instead. ", false)]
        public static int maxAtlasSize
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapMaxSize; }
            set { Lightmapping.GetOrCreateLightingsSettings().lightmapMaxSize = value; }
        }

        [Obsolete("LightmapEditorSettings.realtimeResolution is obsolete, use LightingSettings.indirectResolution instead. ", false)]
        public static float realtimeResolution
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().indirectResolution; }
            set { Lightmapping.GetOrCreateLightingsSettings().indirectResolution = value; }
        }

        [Obsolete("LightmapEditorSettings.bakeResolution is obsolete, use LightingSettings.lightmapResolution instead. ", false)]
        public static float bakeResolution
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapResolution; }
            set { Lightmapping.GetOrCreateLightingsSettings().lightmapResolution = value; }
        }

        [Obsolete("LightmapEditorSettings.textureCompression is obsolete, use LightingSettings.compressLightmaps instead. ", false)]
        public static bool textureCompression
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().compressLightmaps; }
            set { Lightmapping.GetOrCreateLightingsSettings().compressLightmaps = value; }
        }

        [StaticAccessor("GetLightmapEditorSettings()")]
        [NativeName("ReflectionCompression")]
        public extern static ReflectionCubemapCompression reflectionCubemapCompression { get; set; }

        [Obsolete("LightmapEditorSettings.enableAmbientOcclusion is obsolete, use LightingSettings.ao instead. ", false)]
        public static bool enableAmbientOcclusion
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().ao; }
            set { Lightmapping.GetOrCreateLightingsSettings().ao = value; }
        }

        [Obsolete("LightmapEditorSettings.aoMaxDistance is obsolete, use LightingSettings.aoMaxDistance instead. ", false)]
        public static float aoMaxDistance
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().aoMaxDistance; }
            set { Lightmapping.GetOrCreateLightingsSettings().aoMaxDistance = value; }
        }

        [Obsolete("LightmapEditorSettings.aoExponentIndirect is obsolete, use LightingSettings.aoExponentIndirect instead. ", false)]
        public static float aoExponentIndirect
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().aoExponentIndirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().aoExponentIndirect = value; }
        }

        [Obsolete("LightmapEditorSettings.aoExponentDirect is obsolete, use LightingSettings.aoExponentDirect instead. ", false)]
        public static float aoExponentDirect
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().aoExponentDirect; }
            set { Lightmapping.GetOrCreateLightingsSettings().aoExponentDirect = value; }
        }

        [Obsolete("LightmapEditorSettings.padding is obsolete, use LightingSettings.lightmapPadding instead. ", false)]
        public static int padding
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().lightmapPadding; }
            set { Lightmapping.GetOrCreateLightingsSettings().lightmapPadding = value; }
        }

        [Obsolete("LightmapEditorSettings.exportTrainingData is obsolete, use LightingSettings.exportTrainingData instead. ", false)]
        public static bool exportTrainingData
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().exportTrainingData; }
            set { Lightmapping.GetOrCreateLightingsSettings().exportTrainingData = value; }
        }

        [Obsolete("LightmapEditorSettings.trainingDataDestination is obsolete, use LightingSettings.trainingDataDestination instead. ", false)]
        public static string trainingDataDestination
        {
            get { return Lightmapping.GetLightingSettingsOrDefaultsFallback().trainingDataDestination; }
            set { Lightmapping.GetOrCreateLightingsSettings().trainingDataDestination = value; }
        }

        [FreeFunction]
        [NativeHeader("Runtime/Graphics/LightmapSettings.h")]
        extern internal static UnityEngine.Object GetLightmapSettings();
    }
}
