// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/Enlighten/LightmapParameters.h")]
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

        // Realtime GI (Deprecated)
        public extern float resolution { get; set; }
        public extern float clusterResolution { get; set; }
        public extern int irradianceBudget { get; set; }
        public extern int irradianceQuality { get; set; }
        public extern float modellingTolerance { get; set; }

        [NativeName("EdgeStitching")]
        public extern bool stitchEdges { get; set; }
        public extern bool isTransparent { get; set; }
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
    }
}
