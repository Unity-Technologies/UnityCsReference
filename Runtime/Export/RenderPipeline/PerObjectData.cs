// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    [Flags]
    public enum PerObjectData
    {
        None = 0,
        LightProbe = (1 << 0),
        ReflectionProbes = (1 << 1),
        LightProbeProxyVolume = (1 << 2),
        Lightmaps = (1 << 3),
        LightIndices = (1 << 4),
        MotionVectors = (1 << 5),
        LightIndices8 = (1 << 6),
        ReflectionProbeIndices = (1 << 7),
        OcclusionProbe = (1 << 8),
        OcclusionProbeProxyVolume = (1 << 9),
        ShadowMask = (1 << 10),
    }
}
