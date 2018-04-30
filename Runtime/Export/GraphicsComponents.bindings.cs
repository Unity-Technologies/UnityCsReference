// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/OcclusionPortal.h")]
    public sealed partial class OcclusionPortal : Component
    {
        [NativeProperty("IsOpen")] public extern bool open { get; set; }
    }

    [NativeHeader("Runtime/Camera/OcclusionArea.h")]
    public sealed partial class OcclusionArea : Component
    {
        public extern Vector3 center { get; set; }
        public extern Vector3 size { get; set; }
    }

    [NativeHeader("Runtime/Camera/Flare.h")]
    public sealed partial class Flare : Object
    {
    }

    [NativeHeader("Runtime/Camera/Flare.h")]
    public sealed partial class LensFlare : Behaviour
    {
        extern public float brightness { get; set; }
        extern public float fadeSpeed  { get; set; }
        extern public Color color      { get; set; }
        extern public Flare flare      { get; set; }
    }

    [NativeHeader("Runtime/Camera/Projector.h")]
    public sealed partial class Projector : Behaviour
    {
        extern public float nearClipPlane    { get; set; }
        extern public float farClipPlane     { get; set; }
        extern public float fieldOfView      { get; set; }
        extern public float aspectRatio      { get; set; }
        extern public bool  orthographic     { get; set; }
        extern public float orthographicSize { get; set; }
        extern public int   ignoreLayers     { get; set; }

        extern public Material material { get; set; }
    }

    [NativeHeader("Runtime/Camera/SharedLightData.h")]
    public struct LightBakingOutput
    {
        public int probeOcclusionLightIndex;
        public int occlusionMaskChannel;
        [NativeName("lightmapBakeMode.lightmapBakeType")]
        public LightmapBakeType lightmapBakeType;
        [NativeName("lightmapBakeMode.mixedLightingMode")]
        public MixedLightingMode mixedLightingMode;
        public bool isBaked;
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Camera/Light.h")]
    public sealed partial class Light : Behaviour
    {
        [NativeProperty("LightType")] extern public LightType type { get; set; }

        extern public float spotAngle        { get; set; }
        extern public Color color            { get; set; }
        extern public float colorTemperature { get; set; }
        extern public float intensity        { get; set; }
        extern public float bounceIntensity  { get; set; }

        extern public int   shadowCustomResolution { get; set; }
        extern public float shadowBias             { get; set; }
        extern public float shadowNormalBias       { get; set; }
        extern public float shadowNearPlane        { get; set; }

        extern public float range { get; set; }
        extern public Flare flare { get; set; }

        extern public LightBakingOutput bakingOutput { get; set; }
        extern public int  cullingMask        { get; set; }
    }

    [NativeHeader("Runtime/Camera/Skybox.h")]
    public sealed partial class Skybox : Behaviour
    {
        extern public Material material { get; set; }
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Graphics/Mesh/MeshFilter.h")]
    public sealed partial class MeshFilter : Component
    {
        extern public Mesh sharedMesh { get; set; }
        extern public Mesh mesh
        {
            [NativeMethod(Name = "GetInstantiatedMeshFromScript")] get;
            [NativeMethod(Name = "SetInstantiatedMesh")] set;
        }
    }

    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Camera/HaloManager.h")]
    internal sealed partial class Halo : Behaviour
    {
    }
}
