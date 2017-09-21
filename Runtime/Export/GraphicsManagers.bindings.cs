// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

using AmbientMode = UnityEngine.Rendering.AmbientMode;
using ReflectionMode = UnityEngine.Rendering.DefaultReflectionMode;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [StaticAccessor("GetRenderSettings()", StaticAccessorType.Dot)]
    public sealed partial class RenderSettings : Object
    {
        [NativeProperty("UseFog")]         extern public static bool  fog              { get; set; }
        [NativeProperty("LinearFogStart")] extern public static float fogStartDistance { get; set; }
        [NativeProperty("LinearFogEnd")]   extern public static float fogEndDistance   { get; set; }
        extern public static FogMode fogMode    { get; set; }
        extern public static Color   fogColor   { get; set; }
        extern public static float   fogDensity { get; set; }

        extern public static AmbientMode ambientMode   { get; set; }
        extern public static Color ambientSkyColor     { get; set; }
        extern public static Color ambientEquatorColor { get; set; }
        extern public static Color ambientGroundColor  { get; set; }
        extern public static float ambientIntensity    { get; set; }
        [NativeProperty("AmbientSkyColor")] extern public static Color ambientLight { get; set; }

        extern public static Color subtractiveShadowColor { get; set; }

        [NativeProperty("SkyboxMaterial")] extern static public Material skybox { get; set; }
        extern public static Light sun { get; set; }

        extern public static float          reflectionIntensity         { get; set; }
        extern public static int            reflectionBounces           { get; set; }
        extern public static ReflectionMode defaultReflectionMode       { get; set; }
        extern public static int            defaultReflectionResolution { get; set; }

        extern public static float haloStrength   { get; set; }
        extern public static float flareStrength  { get; set; }
        extern public static float flareFadeSpeed { get; set; }
    }

    [NativeHeader("Runtime/Graphics/QualitySettings.h")]
    [StaticAccessor("GetQualitySettings()", StaticAccessorType.Dot)]
    public sealed partial class QualitySettings : Object
    {
        extern public static int pixelLightCount { get; set; }

        [NativeProperty("ShadowQuality")] extern public static ShadowQuality shadows { get; set; }
        extern public static ShadowProjection shadowProjection      { get; set; }
        extern public static int              shadowCascades        { get; set; }
        extern public static float            shadowDistance        { get; set; }
        extern public static ShadowResolution shadowResolution      { get; set; }
        extern public static ShadowmaskMode   shadowmaskMode        { get; set; }
        extern public static float            shadowNearPlaneOffset { get; set; }
        extern public static float            shadowCascade2Split   { get; set; }

        [NativeProperty("LODBias")] extern public static float lodBias { get; set; }

        extern public static int   masterTextureLimit    { get; set; }
        extern public static int   maximumLODLevel       { get; set; }
        extern public static int   particleRaycastBudget { get; set; }
        extern public static bool  softParticles         { get; set; }
        extern public static bool  softVegetation        { get; set; }
        extern public static int   vSyncCount            { get; set; }
        extern public static int   antiAliasing          { get; set; }
        extern public static int   asyncUploadTimeSlice  { get; set; }
        extern public static int   asyncUploadBufferSize { get; set; }

        extern public static bool  realtimeReflectionProbes         { get; set; }
        extern public static bool  billboardsFaceCameraPosition     { get; set; }
        extern public static float resolutionScalingFixedDPIFactor  { get; set; }
    }

    // both desiredColorSpace/activeColorSpace should be deprecated
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public sealed partial class QualitySettings : Object
    {
        extern public static ColorSpace desiredColorSpace
        {
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)][NativeName("GetColorSpace")] get;
        }
        extern public static ColorSpace activeColorSpace
        {
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)][NativeName("GetColorSpace")] get;
        }
    }
}
