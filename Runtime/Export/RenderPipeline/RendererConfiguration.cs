// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.Rendering
{
    [Flags]
    public enum RendererConfiguration
    {
        None = 0,
        PerObjectLightProbe = (1 << 0),
        PerObjectReflectionProbes = (1 << 1),
        PerObjectLightProbeProxyVolume = (1 << 2),
        PerObjectLightmaps = (1 << 3),
        ProvideLightIndices = (1 << 4),
        PerObjectMotionVectors = (1 << 5),
        PerObjectLightIndices8 = (1 << 6),
    }
}
