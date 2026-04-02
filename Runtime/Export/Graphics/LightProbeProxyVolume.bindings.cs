// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/LightProbeProxyVolume.h")]
    [Obsolete("The Light Probe Proxy Volume component is deprecated now that the Built-In Render Pipeline is deprecated. To use an alternative, refer to the documentation in the component help icon. #from(6000.5)", false)]
    [SRPReplacementComponentAttribute("UnityEngine.Rendering.ProbeVolume", "Adaptive Probe Volume")]
    public sealed partial class LightProbeProxyVolume : Behaviour
    {
        public static extern bool isFeatureSupported {[NativeName("IsFeatureSupported")] get; }

        [NativeName("GlobalAABB")]
        public extern Bounds boundsGlobal { get; }

        [NativeName("BoundingBoxSizeCustom")]
        public extern Vector3 sizeCustom { get; set; }

        [NativeName("BoundingBoxOriginCustom")]
        public extern Vector3 originCustom { get; set; }

        public extern float probeDensity { get; set; }

        public extern int gridResolutionX { get; set; }

        public extern int gridResolutionY { get; set; }

        public extern int gridResolutionZ { get; set; }

        public extern LightProbeProxyVolume.BoundingBoxMode boundingBoxMode { get; set; }

        public extern LightProbeProxyVolume.ResolutionMode resolutionMode { get; set; }

        public extern LightProbeProxyVolume.ProbePositionMode probePositionMode { get; set; }

        public extern LightProbeProxyVolume.RefreshMode refreshMode { get; set; }

        public extern LightProbeProxyVolume.QualityMode qualityMode { get; set; }

        public extern LightProbeProxyVolume.DataFormat dataFormat { get; set; }

        public void Update()
        {
            SetDirtyFlag(true);
        }

        private extern void SetDirtyFlag(bool flag);
    }
}
