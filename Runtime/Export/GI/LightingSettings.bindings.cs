// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/LightingSettings.h")]
    [PreventReadOnlyInstanceModificationAttribute]
    public sealed partial class LightingSettings : Object
    {
        [RequiredByNativeCode]
        internal void LightingSettingsDontStripMe() {}

        public LightingSettings()
        {
            Internal_Create(this);
        }

        private extern static void Internal_Create([Writable] LightingSettings self);

        [NativeName("EnableBakedLightmaps")]
        public extern bool bakedGI { get; set; }

        [NativeName("EnableRealtimeLightmaps")]
        public extern bool realtimeGI { get; set; }

        [NativeName("RealtimeEnvironmentLighting")]
        public extern bool realtimeEnvironmentLighting { get; set; }

        #region Editor Only
        // Which baking backend is used.
        public enum Lightmapper
        {
            // Lightmaps are baked by Enlighten
            Enlighten = 0,

            // Lightmaps are baked by the CPU Progressive lightmapper (Wintermute + OpenRL based).
            ProgressiveCPU = 1,

            // Lightmaps are baked by the GPU Progressive lightmapper (RadeonRays + OpenCL based).
            ProgressiveGPU = 2
        }

        // Which path tracer sampling scheme is used.
        public enum Sampling
        {
            // Convergence testing is automatic, stops when lightmap has converged.
            Auto = 0,

            // No convergence testing, always uses the given number of samples.
            Fixed = 1
        }

        // Set the path tracer filter mode.
        public enum FilterMode
        {
            // Do not filter.
            None = 0,

            // Select settings for filtering automatically
            Auto = 1,

            // Setup filtering manually
            Advanced = 2
        }

        // Which path tracer denoiser is used.
        public enum DenoiserType
        {
            // No denoiser
            None = 0,

            // The NVIDIA Optix AI denoiser is applied.
            Optix = 1,

            // The Intel Open Image AI denoiser is applied.
            OpenImage = 2,

            // The AMD Radeon Pro Image Processing denoiser is applied.
            RadeonPro = 3
        }

        // Which path tracer filter is used.
        public enum FilterType
        {
            // A Gaussian filter is applied.
            Gaussian = 0,

            // An A-Trous filter is applied.
            ATrous = 1,

            // No filter
            None = 2
        }

        [NativeName("AutoGenerate")]
        public extern bool autoGenerate { get; set; }

        [NativeName("MixedBakeMode")]
        public extern MixedLightingMode mixedBakeMode { get; set; }

        [NativeName("AlbedoBoost")]
        public extern float albedoBoost { get; set; }

        [NativeName("IndirectOutputScale")]
        public extern float indirectScale { get; set; }

        [NativeName("BakeBackend")]
        public extern Lightmapper lightmapper { get; set; }

        // The maximum size of an individual lightmap texture.
        [NativeName("LightmapMaxSize")]
        public extern int lightmapMaxSize { get; set; }

        // Static lightmap resolution in texels per world unit.
        [NativeName("BakeResolution")]
        public extern float lightmapResolution { get; set; }

        // Texel separation between shapes.
        [NativeName("Padding")]
        public extern int lightmapPadding { get; set; }

        // Whether to use DXT1 compression on the generated lightmaps.
        [NativeName("TextureCompression")]
        public extern bool compressLightmaps { get; set; }

        // Whether to apply ambient occlusion to the lightmap.
        [NativeName("AO")]
        public extern bool ao { get; set; }

        // Beyond this distance a ray is considered to be un-occluded.
        [NativeName("AOMaxDistance")]
        public extern float aoMaxDistance { get; set; }

        // Exponent for ambient occlusion on indirect lighting.
        [NativeName("CompAOExponent")]
        public extern float aoExponentIndirect { get; set; }

        // Exponent for ambient occlusion on direct lighting.
        [NativeName("CompAOExponentDirect")]
        public extern float aoExponentDirect { get; set; }

        // If we should write out AO to disk. Only works in On Demand bakes
        [NativeName("ExtractAO")]
        public extern bool extractAO { get; set; }

        [NativeName("LightmapsBakeMode")]
        public extern LightmapsMode directionalityMode { get; set; }

        [NativeName("FilterMode")]
        internal extern UnityEngine.FilterMode lightmapFilterMode { get; set; }

        public extern bool exportTrainingData { get; set; }

        public extern string trainingDataDestination { get; set; }

        // Realtime lightmap resolution in texels per world unit. Also used for indirect resolution when using baked GI.
        [NativeName("RealtimeResolution")]
        public extern float indirectResolution { get; set; }

        [NativeName("ForceWhiteAlbedo")]
        internal extern bool realtimeForceWhiteAlbedo { get; set; }

        [NativeName("ForceUpdates")]
        internal extern bool realtimeForceUpdates { get; set; }

        public extern bool finalGather { get; set; }

        public extern float finalGatherRayCount { get; set; }

        public extern bool finalGatherFiltering { get; set; }

        [NativeName("PVRSampling")]
        public extern Sampling sampling { get; set; }

        [NativeName("PVRDirectSampleCount")]
        public extern int directSampleCount { get; set; }

        [NativeName("PVRSampleCount")]
        public extern int indirectSampleCount { get; set; }

        // Amount of light bounce used for the path tracer.
        [NativeName("PVRBounces")]
        public extern int bounces { get; set; }

        // Choose at which bounce we start to apply russian roulette to the ray
        [NativeName("PVRRussianRouletteStartBounce")]
        public extern int russianRouletteStartBounce { get; set; }

        // Is view prioritisation enabled?
        [NativeName("PVRCulling")]
        public extern bool prioritizeView { get; set; }

        // Which path tracer filtering mode is used.
        [NativeName("PVRFilteringMode")]
        public extern FilterMode filteringMode { get; set; }

        // Which path tracer denoiser is used for the direct light.
        [NativeName("PVRDenoiserTypeDirect")]
        public extern DenoiserType denoiserTypeDirect { get; set; }

        // Which path tracer denoiser is used for the indirect light.
        [NativeName("PVRDenoiserTypeIndirect")]
        public extern DenoiserType denoiserTypeIndirect { get; set; }

        // Which path tracer denoiser is used for ambient occlusion.
        [NativeName("PVRDenoiserTypeAO")]
        public extern DenoiserType denoiserTypeAO { get; set; }

        // Which path tracer filter is used for the direct light.
        [NativeName("PVRFilterTypeDirect")]
        public extern FilterType filterTypeDirect { get; set; }

        // Which path tracer filter is used for the indirect light.
        [NativeName("PVRFilterTypeIndirect")]
        public extern FilterType filterTypeIndirect { get; set; }

        // Which path tracer filter is used for ambient occlusion.
        [NativeName("PVRFilterTypeAO")]
        public extern FilterType filterTypeAO { get; set; }

        // Which radius is used for the direct light path tracer filter if gauss is chosen.
        [NativeName("PVRFilteringGaussRadiusDirect")]
        public extern int filteringGaussRadiusDirect { get; set; }

        // Which radius is used for the indirect light path tracer filter if gauss is chosen.
        [NativeName("PVRFilteringGaussRadiusIndirect")]
        public extern int filteringGaussRadiusIndirect { get; set; }

        // Which radius is used for AO path tracer filter if gauss is chosen.
        [NativeName("PVRFilteringGaussRadiusAO")]
        public extern int filteringGaussRadiusAO { get; set; }

        // Which position sigma is used for the direct light path tracer filter if Atrous is chosen.
        [NativeName("PVRFilteringAtrousPositionSigmaDirect")]
        public extern float filteringAtrousPositionSigmaDirect { get; set; }

        // Which position sigma is used for the indirect light path tracer filter if Atrous is chosen.
        [NativeName("PVRFilteringAtrousPositionSigmaIndirect")]
        public extern float filteringAtrousPositionSigmaIndirect { get; set; }

        // Which position sigma is used for AO path tracer filter if Atrous is chosen.
        [NativeName("PVRFilteringAtrousPositionSigmaAO")]
        public extern float filteringAtrousPositionSigmaAO { get; set; }

        // Whether to enable or disable environment multiple importance sampling
        [NativeName("PVREnvironmentMIS")]
        internal extern int environmentMIS { get; set; }

        // How many samples to use for environment sampling
        [NativeName("PVREnvironmentSampleCount")]
        public extern int environmentSampleCount { get; set; }

        // How many reference points to generate when using MIS
        [NativeName("PVREnvironmentReferencePointCount")]
        internal extern int environmentReferencePointCount { get; set; }

        // How many samples to use for light probes relative to lightmap texels
        [NativeName("LightProbeSampleCountMultiplier")]
        public extern float lightProbeSampleCountMultiplier { get; set; }
        #endregion
    }
}
