// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [VisibleToOtherModules]
    internal enum DefaultMaterialType
    {
        Default = 0,
        Particle = 1,
        Line = 2,
        Terrain = 3,
        Sprite = 4,
        SpriteMask = 5,
        UGUI = 6,
        UGUI_Overdraw = 7,
        UGUI_ETC1Supported = 8
    }
    
    [VisibleToOtherModules]
    internal enum DefaultShaderType
    {
        Default = 0,
        AutodeskInteractive = 1,
        AutodeskInteractiveTransparent = 2,
        AutodeskInteractiveMasked = 3,
        TerrainDetailLit = 4,
        TerrainDetailGrass = 5,
        TerrainDetailGrassBillboard = 6,
        SpeedTree7 = 7,
        SpeedTree8 = 8,
        SpeedTree9 = 9,
    }

    [NativeHeader("Runtime/Camera/GraphicsSettings.h")]
    [StaticAccessor("GetGraphicsSettings()", StaticAccessorType.Dot)]
    public sealed partial class GraphicsSettings : Object
    {
        private GraphicsSettings() {}

        extern public static TransparencySortMode   transparencySortMode { get; set; }
        extern public static Vector3                transparencySortAxis { get; set; }
        extern public static bool realtimeDirectRectangularAreaLights { get; set; }
        extern public static bool lightsUseLinearIntensity   { get; set; }
        extern public static bool lightsUseColorTemperature  { get; set; }
        [Obsolete ($"This property is obsolete. Use {nameof(RenderingLayerMask)} API and Tags & Layers project settings instead. #from(23.3)")]
        extern public static uint defaultRenderingLayerMask { get; set; }
        extern public static Camera.GateFitMode defaultGateFitMode { get; set; }
        extern public static bool useScriptableRenderPipelineBatching { get; set; }
        extern public static bool logWhenShaderIsCompiled { get; set; }
        extern public static bool disableBuiltinCustomRenderTextureUpdate { get; set; }
        extern public static VideoShadersIncludeMode videoShadersIncludeMode
        {
            get;
            set;
        }
        extern public static LightProbeOutsideHullStrategy lightProbeOutsideHullStrategy { get; set; }

        extern public static bool HasShaderDefine(GraphicsTier tier, BuiltinShaderDefine defineHash);
        public static bool HasShaderDefine(BuiltinShaderDefine defineHash)
        {
            return HasShaderDefine(Graphics.activeTier, defineHash);
        }

        [NativeName("CurrentRenderPipeline")] extern private static ScriptableObject INTERNAL_currentRenderPipeline { get; }
        public static RenderPipelineAsset currentRenderPipeline
        {
            get { return INTERNAL_currentRenderPipeline as RenderPipelineAsset; }
        }

        public static bool isScriptableRenderPipelineEnabled => INTERNAL_currentRenderPipeline != null;

        public static Type currentRenderPipelineAssetType => isScriptableRenderPipelineEnabled ? INTERNAL_currentRenderPipeline.GetType() : null;

        [Obsolete("renderPipelineAsset has been deprecated. Use defaultRenderPipeline instead (UnityUpgradable) -> defaultRenderPipeline", false)]
        public static RenderPipelineAsset renderPipelineAsset
        {
            get { return defaultRenderPipeline; }
            set { defaultRenderPipeline = value; }
        }

        [NativeName("DefaultRenderPipeline")] extern private static ScriptableObject INTERNAL_defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset defaultRenderPipeline
        {
            get { return INTERNAL_defaultRenderPipeline as RenderPipelineAsset; }
            set { INTERNAL_defaultRenderPipeline = value; }
        }

        [NativeName("GetAllConfiguredRenderPipelinesForScript")] extern static private ScriptableObject[] GetAllConfiguredRenderPipelines();

        public static RenderPipelineAsset[] allConfiguredRenderPipelines
        {
            get
            {
                return GetAllConfiguredRenderPipelines().Cast<RenderPipelineAsset>().ToArray();
            }
        }

        [FreeFunction] extern public static Object GetGraphicsSettings();

        [NativeName("SetShaderModeScript")]   extern static public void                 SetShaderMode(BuiltinShaderType type, BuiltinShaderMode mode);
        [NativeName("GetShaderModeScript")]   extern static public BuiltinShaderMode    GetShaderMode(BuiltinShaderType type);

        [NativeName("SetCustomShaderScript")] extern static public void     SetCustomShader(BuiltinShaderType type, Shader shader);
        [NativeName("GetCustomShaderScript")] extern static public Shader   GetCustomShader(BuiltinShaderType type);

        extern public static bool cameraRelativeLightCulling { get; set; }
        extern public static bool cameraRelativeShadowCulling { get; set; }

        [RequiredByNativeCode]
        [VisibleToOtherModules]
        internal static Shader GetDefaultShader(DefaultShaderType type)
        {
            var rp = currentRenderPipeline;
            if (currentRenderPipeline == null)
                return null;

            return type switch
            {
                DefaultShaderType.Default => rp.defaultShader,
                DefaultShaderType.AutodeskInteractive => rp.autodeskInteractiveShader,
                DefaultShaderType.AutodeskInteractiveTransparent => rp.autodeskInteractiveTransparentShader,
                DefaultShaderType.AutodeskInteractiveMasked => rp.autodeskInteractiveMaskedShader,
                DefaultShaderType.TerrainDetailLit => rp.terrainDetailLitShader,
                DefaultShaderType.TerrainDetailGrass => rp.terrainDetailGrassShader,
                DefaultShaderType.TerrainDetailGrassBillboard => rp.terrainDetailGrassBillboardShader,
                DefaultShaderType.SpeedTree7 => rp.defaultSpeedTree7Shader,
                DefaultShaderType.SpeedTree8 => rp.defaultSpeedTree8Shader,
                DefaultShaderType.SpeedTree9 => rp.defaultSpeedTree9Shader,
                _ => throw new NotImplementedException($"DefaultShaderType {type} not implemented")
            };
        }

        [RequiredByNativeCode]
        [VisibleToOtherModules]
        internal static Material GetDefaultMaterial(DefaultMaterialType type)
        {
            var rp = currentRenderPipeline;
            if (currentRenderPipeline == null)
                return null;

            return type switch
            {
                DefaultMaterialType.Default => rp.defaultMaterial,
                DefaultMaterialType.Particle => rp.defaultParticleMaterial,
                DefaultMaterialType.Line => rp.defaultLineMaterial,
                DefaultMaterialType.Terrain => rp.defaultTerrainMaterial,
                DefaultMaterialType.Sprite => rp.default2DMaterial,
                DefaultMaterialType.SpriteMask => rp.default2DMaskMaterial,
                DefaultMaterialType.UGUI => rp.defaultUIMaterial,
                DefaultMaterialType.UGUI_Overdraw => rp.defaultUIOverdrawMaterial,
                DefaultMaterialType.UGUI_ETC1Supported => rp.defaultUIETC1SupportedMaterial,
                _ => throw new NotImplementedException($"DefaultMaterialType {type} not implemented")
            };
        }
    }
}
