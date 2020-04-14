// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/LightingSettings.h")]
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

        // This is only here due to issues with giWorkflowMode being in UnityEngine.
        // Lightmapping.GIWorkflowMode should be used in the rest of the code
        [NativeHeader("Runtime/Graphics/LightmapEnums.h")]
        internal enum GIWorkflowMode
        {
            Iterative = 0,
            OnDemand = 1,
            Legacy = 2
        }

        [NativeName("GIWorkflowMode")]
        internal extern GIWorkflowMode giWorkflowMode { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        public bool autoGenerate
        {
            get { return giWorkflowMode == GIWorkflowMode.Iterative; }
            set { giWorkflowMode = (value ? GIWorkflowMode.Iterative : GIWorkflowMode.OnDemand); }
        }

        [NativeName("MixedBakeMode")]
        [NativeConditional("UNITY_EDITOR")]
        public extern MixedLightingMode mixedBakeMode { get; set; }

        [NativeName("AlbedoBoost")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float albedoBoost { get; set; }

        [NativeName("IndirectOutputScale")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float indirectScale { get; set; }

        [NativeName("BakeBackend")]
        [NativeConditional("UNITY_EDITOR")]
        public extern Lightmapper lightmapper { get; set; }

        // The maximum size of an individual lightmap texture.
        [NativeName("LightmapMaxSize")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int lightmapMaxSize { get; set; }

        // Static lightmap resolution in texels per world unit.
        [NativeName("BakeResolution")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float lightmapResolution { get; set; }

        // Texel separation between shapes.
        [NativeName("Padding")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int lightmapPadding { get; set; }

        // Whether to use DXT1 compression on the generated lightmaps.
        [NativeName("TextureCompression")]
        [NativeConditional("UNITY_EDITOR")]
        public extern bool compressLightmaps { get; set; }

        // Whether to apply ambient occlusion to the lightmap.
        [NativeName("AO")]
        [NativeConditional("UNITY_EDITOR")]
        public extern bool ao { get; set; }

        // Beyond this distance a ray is considered to be un-occluded.
        [NativeName("AOMaxDistance")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float aoMaxDistance { get; set; }

        // Exponent for ambient occlusion on indirect lighting.
        [NativeName("CompAOExponent")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float aoExponentIndirect { get; set; }

        // Exponent for ambient occlusion on direct lighting.
        [NativeName("CompAOExponentDirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float aoExponentDirect { get; set; }

        // If we should write out AO to disk. Only works in On Demand bakes
        [NativeName("ExtractAO")]
        [NativeConditional("UNITY_EDITOR")]
        public extern bool extractAO { get; set; }

        [NativeName("LightmapsBakeMode")]
        [NativeConditional("UNITY_EDITOR")]
        public extern LightmapsMode directionalityMode { get; set; }

        [NativeName("FilterMode")]
        [NativeConditional("UNITY_EDITOR")]
        internal extern UnityEngine.FilterMode lightmapFilterMode { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        public extern bool exportTrainingData { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        public extern string trainingDataDestination { get; set; }

        // Realtime lightmap resolution in texels per world unit. Also used for indirect resolution when using baked GI.
        [NativeName("RealtimeResolution")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float indirectResolution { get; set; }

        [NativeName("ForceWhiteAlbedo")]
        [NativeConditional("UNITY_EDITOR")]
        internal extern bool realtimeForceWhiteAlbedo { get; set; }

        [NativeName("ForceUpdates")]
        [NativeConditional("UNITY_EDITOR")]
        internal extern bool realtimeForceUpdates { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        internal extern bool finalGather { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        internal extern float finalGatherRayCount { get; set; }

        [NativeConditional("UNITY_EDITOR")]
        internal extern bool finalGatherFiltering { get; set; }

        [NativeName("PVRSampling")]
        [NativeConditional("UNITY_EDITOR")]
        public extern Sampling sampling { get; set; }

        [NativeName("PVRDirectSampleCount")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int directSampleCount { get; set; }

        [NativeName("PVRSampleCount")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int indirectSampleCount { get; set; }

        // Amount of light bounce used for the path tracer.
        [NativeName("PVRBounces")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int bounces { get; set; }

        // Choose at which bounce we start to apply russian roulette to the ray
        [NativeName("PVRRussianRouletteStartBounce")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int russianRouletteStartBounce { get; set; }

        // Is view prioritisation enabled?
        [NativeName("PVRCulling")]
        [NativeConditional("UNITY_EDITOR")]
        public extern bool prioritizeView { get; set; }

        // Which path tracer filtering mode is used.
        [NativeName("PVRFilteringMode")]
        [NativeConditional("UNITY_EDITOR")]
        public extern FilterMode filteringMode { get; set; }

        // Which path tracer denoiser is used for the direct light.
        [NativeName("PVRDenoiserTypeDirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern DenoiserType denoiserTypeDirect { get; set; }

        // Which path tracer denoiser is used for the indirect light.
        [NativeName("PVRDenoiserTypeIndirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern DenoiserType denoiserTypeIndirect { get; set; }

        // Which path tracer denoiser is used for ambient occlusion.
        [NativeName("PVRDenoiserTypeAO")]
        [NativeConditional("UNITY_EDITOR")]
        public extern DenoiserType denoiserTypeAO { get; set; }

        // Which path tracer filter is used for the direct light.
        [NativeName("PVRFilterTypeDirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern FilterType filterTypeDirect { get; set; }

        // Which path tracer filter is used for the indirect light.
        [NativeName("PVRFilterTypeIndirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern FilterType filterTypeIndirect { get; set; }

        // Which path tracer filter is used for ambient occlusion.
        [NativeName("PVRFilterTypeAO")]
        [NativeConditional("UNITY_EDITOR")]
        public extern FilterType filterTypeAO { get; set; }

        // Which radius is used for the direct light path tracer filter if gauss is chosen.
        [NativeName("PVRFilteringGaussRadiusDirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int filteringGaussRadiusDirect { get; set; }

        // Which radius is used for the indirect light path tracer filter if gauss is chosen.
        [NativeName("PVRFilteringGaussRadiusIndirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int filteringGaussRadiusIndirect { get; set; }

        // Which radius is used for AO path tracer filter if gauss is chosen.
        [NativeName("PVRFilteringGaussRadiusAO")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int filteringGaussRadiusAO { get; set; }

        // Which position sigma is used for the direct light path tracer filter if Atrous is chosen.
        [NativeName("PVRFilteringAtrousPositionSigmaDirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float filteringAtrousPositionSigmaDirect { get; set; }

        // Which position sigma is used for the indirect light path tracer filter if Atrous is chosen.
        [NativeName("PVRFilteringAtrousPositionSigmaIndirect")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float filteringAtrousPositionSigmaIndirect { get; set; }

        // Which position sigma is used for AO path tracer filter if Atrous is chosen.
        [NativeName("PVRFilteringAtrousPositionSigmaAO")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float filteringAtrousPositionSigmaAO { get; set; }

        // Whether to enable or disable environment multiple importance sampling
        [NativeName("PVREnvironmentMIS")]
        [NativeConditional("UNITY_EDITOR")]
        internal extern int environmentMIS { get; set; }

        // How many samples to use for environment sampling
        [NativeName("PVREnvironmentSampleCount")]
        [NativeConditional("UNITY_EDITOR")]
        public extern int environmentSampleCount { get; set; }

        // How many reference points to generate when using MIS
        [NativeName("PVREnvironmentReferencePointCount")]
        [NativeConditional("UNITY_EDITOR")]
        internal extern int environmentReferencePointCount { get; set; }

        // How many samples to use for light probes relative to lightmap texels
        [NativeName("LightProbeSampleCountMultiplier")]
        [NativeConditional("UNITY_EDITOR")]
        public extern float lightProbeSampleCountMultiplier { get; set; }
        #endregion
    }
}
