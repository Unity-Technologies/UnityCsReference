// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using UnityEngine.Rendering;

using AmbientMode = UnityEngine.Rendering.AmbientMode;
using ReflectionMode = UnityEngine.Rendering.DefaultReflectionMode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine
{
    [Flags]
    public enum TerrainQualityOverrides
    {
        None = 0,
        PixelError = 1,
        BasemapDistance = 2,
        DetailDensity = 4,
        DetailDistance = 8,
        TreeDistance = 16,
        BillboardStart = 32,
        FadeLength = 64,
        MaxTrees = 128
    }

    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [NativeHeader("Runtime/Graphics/QualitySettingsTypes.h")]
    [StaticAccessor("GetRenderSettings()", StaticAccessorType.Dot)]
    public sealed partial class RenderSettings : Object
    {
        private RenderSettings() {}

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
        extern public static Rendering.SphericalHarmonicsL2 ambientProbe { [NativeMethod("GetFinalAmbientProbe")] get; set; }

        [System.Obsolete(@"RenderSettings.customReflection has been deprecated in favor of RenderSettings.customReflectionTexture.", false)]
        public static Cubemap customReflection
        {
            get
            {
                if (!(customReflectionTexture is Cubemap cube))
                {
                    throw new ArgumentException("RenderSettings.customReflection is currently not referencing a cubemap.");
                }
                return cube;
            }
            [NativeThrows] set => customReflectionTexture = value;
        }
        [NativeProperty("CustomReflection")] extern public static Texture customReflectionTexture { get; [NativeThrows] set; }

        extern public static float          reflectionIntensity         { get; set; }
        extern public static int            reflectionBounces           { get; set; }

        [NativeProperty("GeneratedSkyboxReflection")]
        extern internal static Cubemap      defaultReflection           { get; }
        extern public static ReflectionMode defaultReflectionMode       { get; set; }
        extern public static int            defaultReflectionResolution { get; set; }

        extern public static float haloStrength   { get; set; }
        extern public static float flareStrength  { get; set; }
        extern public static float flareFadeSpeed { get; set; }

        [FreeFunction("GetRenderSettings")] extern internal static Object GetRenderSettings();
        [StaticAccessor("RenderSettingsScripting", StaticAccessorType.DoubleColon)] extern internal static void Reset();
    }

    // Keep in sync with MipmapLimitSettings in Runtime\Graphics\Texture.h
    public struct TextureMipmapLimitSettings
    {
        public TextureMipmapLimitBiasMode limitBiasMode { get; set; }
        public int limitBias { get; set; }
    };

    [NativeHeader("Runtime/Graphics/QualitySettings.h")]
    [StaticAccessor("GetQualitySettings()", StaticAccessorType.Dot)]
    public static class TextureMipmapLimitGroups
    {
        [NativeName("CreateTextureMipmapLimitGroup")]
        [NativeThrows]
        extern public static void CreateGroup([NotNull] string groupName);

        [NativeName("RemoveTextureMipmapLimitGroup")]
        [NativeThrows]
        extern public static void RemoveGroup([NotNull] string groupName);

        [NativeName("GetTextureMipmapLimitGroupNames")]
        extern public static string[] GetGroups();

        [NativeName("HasTextureMipmapLimitGroup")]
        extern public static bool HasGroup([NotNull] string groupName);
    }

    [NativeHeader("Runtime/Graphics/QualitySettings.h")]
    [StaticAccessor("GetQualitySettings()", StaticAccessorType.Dot)]
    public sealed partial class QualitySettings : Object
    {
        public static void ForEach(Action callback)
        {
            if (callback == null)
                return;

            int currentQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.count; ++i)
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                    callback();
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
            }
        }

        public static void ForEach(Action<int, string> callback)
        {
            if (callback == null)
                return;

            int currentQuality = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.count; ++i)
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                    callback(i, names[i]);
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
            }
        }

        private QualitySettings() {}

        extern public static int pixelLightCount { get; set; }

        [NativeProperty("ShadowQuality")] extern public static ShadowQuality shadows { get; set; }
        extern public static ShadowProjection shadowProjection      { get; set; }
        extern public static int              shadowCascades        { get; set; }
        extern public static float            shadowDistance        { get; set; }
        [NativeProperty("ShadowResolution")] extern public static ShadowResolution shadowResolution      { get; set; }
        [NativeProperty("ShadowmaskMode")] extern public static ShadowmaskMode   shadowmaskMode        { get; set; }
        extern public static float            shadowNearPlaneOffset { get; set; }
        extern public static float            shadowCascade2Split   { get; set; }
        extern public static Vector3          shadowCascade4Split   { get; set; }

        [NativeProperty("LODBias")] extern public static float lodBias { get; set; }
        [NativeProperty("AnisotropicTextures")] extern public static AnisotropicFiltering anisotropicFiltering { get; set; }

        [Obsolete("masterTextureLimit has been deprecated. Use globalTextureMipmapLimit instead (UnityUpgradable) -> globalTextureMipmapLimit", false)]
        [NativeProperty("GlobalTextureMipmapLimit")] extern public static int   masterTextureLimit    { get; set; }
        extern public static int   globalTextureMipmapLimit { get; set; }
        extern public static int   maximumLODLevel       { get; set; }
        extern public static bool  enableLODCrossFade    { get; set; }
        extern public static int   particleRaycastBudget { get; set; }
        extern public static bool  softParticles         { get; set; }
        extern public static bool  softVegetation        { get; set; }
        extern public static int   vSyncCount            { get; set; }
        extern public static int   realtimeGICPUUsage    { get; set; }
        extern public static int   antiAliasing          { get; set; }
        extern public static int   asyncUploadTimeSlice  { get; set; }
        extern public static int   asyncUploadBufferSize { get; set; }
        extern public static bool  asyncUploadPersistentBuffer { get; set; }

        [NativeName("SetLODSettings")]
        extern public static void SetLODSettings(float lodBias, int maximumLODLevel, bool setDirty = true);

        [NativeName("SetTextureMipmapLimitSettings")]
        [NativeThrows]
        extern public static void SetTextureMipmapLimitSettings(string groupName, TextureMipmapLimitSettings textureMipmapLimitSettings);

        [NativeName("GetTextureMipmapLimitSettings")]
        [NativeThrows]
        extern public static TextureMipmapLimitSettings GetTextureMipmapLimitSettings(string groupName);

        extern public static bool  realtimeReflectionProbes         { get; set; }
        extern public static bool  billboardsFaceCameraPosition     { get; set; }
        extern public static bool  useLegacyDetailDistribution      { get; set; }
        extern public static float resolutionScalingFixedDPIFactor  { get; set; }

        extern public static TerrainQualityOverrides terrainQualityOverrides { get; set; }
        extern public static float terrainPixelError { get; set; }
        extern public static float terrainDetailDensityScale { get; set; }
        extern public static float terrainBasemapDistance { get; set; }
        extern public static float terrainDetailDistance { get; set; }
        extern public static float terrainTreeDistance { get; set; }
        extern public static float terrainBillboardStart { get; set; }
        extern public static float terrainFadeLength { get; set; }
        extern public static float terrainMaxTrees { get; set; }

        [NativeName("RenderPipeline")] extern private static ScriptableObject INTERNAL_renderPipeline { get; set; }
        public static RenderPipelineAsset renderPipeline
        {
            get { return INTERNAL_renderPipeline as RenderPipelineAsset; }
            set { INTERNAL_renderPipeline = value; }
        }

        [NativeName("GetRenderPipelineAssetAt")]
        extern internal static ScriptableObject InternalGetRenderPipelineAssetAt(int index);
        public static RenderPipelineAsset GetRenderPipelineAssetAt(int index)
        {
            if (index < 0 || index >= names.Length)
                throw new IndexOutOfRangeException($"{nameof(index)} is out of range [0..{names.Length}[");

            return InternalGetRenderPipelineAssetAt(index) as RenderPipelineAsset;
        }

        [Obsolete("blendWeights is obsolete. Use skinWeights instead (UnityUpgradable) -> skinWeights", true)]
        extern public static BlendWeights blendWeights
        {
            [NativeName("GetSkinWeights")] get;
            [NativeThrows]
            [NativeName("SetSkinWeights")] set; }

        extern public static SkinWeights skinWeights
        {
            get;
            [NativeThrows] set;
        }

        extern public static int count { [NativeName("GetQualitySettingsCount")] get; }

        extern public static bool streamingMipmapsActive { get; set; }
        extern public static float streamingMipmapsMemoryBudget { get; set; }
        extern public static int streamingMipmapsRenderersPerFrame { get; set; }
        extern public static int streamingMipmapsMaxLevelReduction { get; set; }
        extern public static bool streamingMipmapsAddAllCameras { get; set; }
        extern public static int streamingMipmapsMaxFileIORequests { get; set; }

        [StaticAccessor("QualitySettingsScripting", StaticAccessorType.DoubleColon)] extern public static int maxQueuedFrames { get; set; }

        [NativeName("GetCurrentIndex")] extern public static int  GetQualityLevel();
        [FreeFunction] extern public static Object GetQualitySettings();
        [NativeName("SetCurrentIndex")] extern public static void SetQualityLevel(int index, [uei.DefaultValue("true")] bool applyExpensiveChanges);

        [NativeProperty("QualitySettingsNames")] extern public static string[] names { get; }

        [NativeName("IsTextureResReducedOnAnyPlatform")] extern internal static bool IsTextureResReducedOnAnyPlatform();

        [NativeName("IsPlatformIncluded")] extern public static bool IsPlatformIncluded(string buildTargetGroupName, int index);
        [NativeName("IncludePlatform")] extern internal static void IncludePlatformAt(string buildTargetGroupName, int index);
        [NativeName("ExcludePlatform")] extern internal static void ExcludePlatformAt(string buildTargetGroupName, int index);
        public static bool TryIncludePlatformAt(string buildTargetGroupName, int index, out Exception error)
        {
            if (index < 0 || index >= count)
            {
                error = new ArgumentOutOfRangeException($"{nameof(index)} must be greater than 0 and lower than {count}");
                return false;
            }

            error = null;
            IncludePlatformAt(buildTargetGroupName, index);
            return true;
        }

        public static bool TryExcludePlatformAt(string buildTargetGroupName, int index, out Exception error)
        {
            if (index < 0 || index >= count)
            {
                error = new ArgumentOutOfRangeException($"{nameof(index)} must be greater than 0 and lower than {count}");
                return false;
            }

            error = null;
            ExcludePlatformAt(buildTargetGroupName, index);
            return true;
        }

        [NativeName("GetActiveQualityLevelsForPlatform")] extern public static int[] GetActiveQualityLevelsForPlatform(string buildTargetGroupName);
        [NativeName("GetActiveQualityLevelsForPlatformCount")] extern public static int GetActiveQualityLevelsForPlatformCount(string buildTargetGroupName);

        [NativeName("GetRenderPipelineAssetsForPlatform")] extern internal static ScriptableObject[] InternalGetRenderPipelineAssetsForPlatform(string buildTargetGroupName);
        public static void GetRenderPipelineAssetsForPlatform<T>(string buildTargetGroupName, out HashSet<T> uniqueRenderPipelineAssets)
            where T : RenderPipelineAsset
        {
            var scriptableObjects = InternalGetRenderPipelineAssetsForPlatform(buildTargetGroupName);
            uniqueRenderPipelineAssets = new HashSet<T>(scriptableObjects.Length);
            for (int i = 0; i < scriptableObjects.Length; ++i)
            {
                if (scriptableObjects[i] is T rpAsset)
                    uniqueRenderPipelineAssets.Add(rpAsset);
            }
        }

        public static void GetAllRenderPipelineAssetsForPlatform(string buildTargetGroupName, ref List<RenderPipelineAsset> renderPipelineAssets)
        {
            if (renderPipelineAssets == null)
                renderPipelineAssets = new List<RenderPipelineAsset>();

            var scriptableObjects = InternalGetRenderPipelineAssetsForPlatform(buildTargetGroupName);
            for (int i = 0; i < scriptableObjects.Length; ++i)
            {
                if (scriptableObjects[i] is RenderPipelineAsset rpAsset)
                    renderPipelineAssets.Add(rpAsset);
                else
                    renderPipelineAssets.Add(GraphicsSettings.defaultRenderPipeline);
            }

            if (renderPipelineAssets.Count == 0 && GraphicsSettings.defaultRenderPipeline != null)
                renderPipelineAssets.Add(GraphicsSettings.defaultRenderPipeline);
        }

        static HashSet<Type> s_RenderPipelineAssetsTypes = new();
        static List<RenderPipelineAsset> s_RenderPipelineAssets = new();

        internal static bool SamePipelineAssetsForPlatform(string buildTargetGroupName)
        {
            s_RenderPipelineAssetsTypes.Clear();
            s_RenderPipelineAssets.Clear();

            GetAllRenderPipelineAssetsForPlatform(buildTargetGroupName, ref s_RenderPipelineAssets);
            if (!s_RenderPipelineAssets.Any())
                return true;

            for (int i = 0; i < s_RenderPipelineAssets.Count; i++)
            {
                if (s_RenderPipelineAssets[i] != null)
                    s_RenderPipelineAssetsTypes.Add(s_RenderPipelineAssets[i].GetType());
                else
                    s_RenderPipelineAssetsTypes.Add(null);
            }

            return s_RenderPipelineAssetsTypes.Count == 1;
        }
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

namespace UnityEngine.Experimental.GlobalIllumination
{
    [NativeHeader("Runtime/Camera/RenderSettings.h")]
    [StaticAccessor("GetRenderSettings()", StaticAccessorType.Dot)]
    public partial class RenderSettings
    {
        extern public static bool useRadianceAmbientProbe { get; set; }
    }
}
