// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/Enlighten/LightmapParameters.h")]
    [global::UnityEngine.NativeClass("LightmapParameters", PersistentTypeId = 1113)]
    [PreventReadOnlyInstanceModificationAttribute]
    public sealed partial class LightmapParameters : UnityEngine.Object
    {
        public LightmapParameters()
        {
            Internal_Create(this);
        }

        [FreeFunction("LightmapParametersBindings::Internal_Create")]
        private extern static void Internal_Create([UnityEngine.Writable] LightmapParameters self);

        public extern static LightmapParameters GetLightmapParametersForLightingSettings(UnityEngine.LightingSettings lightingSettings);
        public extern static void SetLightmapParametersForLightingSettings(LightmapParameters parameters, UnityEngine.LightingSettings lightingSettings);

        public void AssignToLightingSettings(UnityEngine.LightingSettings lightingSettings)
        {
            SetLightmapParametersForLightingSettings(this, lightingSettings);
        }

        // Realtime GI
        // Not obsolete like the rest of this block: the Progressive lightmapper derives terrain heightmap sampling density from this.
        public extern float resolution { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern float clusterResolution { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern int irradianceBudget { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern int irradianceQuality { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern float modellingTolerance { get; set; }

        [NativeName("EdgeStitching")]
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern bool stitchEdges { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern bool isTransparent { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public extern int systemTag { get; set; }

        // Baked GI
        public extern int blurRadius { get; set; }
        public extern int antiAliasingSamples { get; set; }
        public extern int directLightQuality { get; set; }
        public extern float pushoff { get; set; }
        public extern int bakedLightmapTag { get; set; }
        public extern bool limitLightmapCount { get; set; }
        public extern int maxLightmapCount { get; set; }

        // Baked ao
        public extern int AOQuality { get; set; }
        public extern int AOAntiAliasingSamples { get; set; }

        // General GI
        public extern float backFaceTolerance { get; set; }

        [Obsolete("edgeStitching has been deprecated. Use stitchEdges instead")]
        public float edgeStitching
        {
            get { return stitchEdges ? 1.0f : 0.0f; }
            set { stitchEdges = (value != 0.0f); }
        }

        [NativeHeader("Runtime/Graphics/LightmapEnums.h")]
        public enum AntiAliasingSamples
        {
            SSAA1 = 1,
            SSAA4 = 2,
            SSAA16 = 4,
        }
    }
}
