// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEditor.Rendering;

namespace UnityEditor.Build.Profile
{
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    sealed class BuildProfileGraphicsSettings : ScriptableObject
    {
        internal const int k_InvalidGraphicsSetting = -2;

        [SerializeField] StrippingModes m_LightmapStripping = StrippingModes.Automatic;
        [SerializeField] bool m_LightmapKeepPlain = true;
        [SerializeField] bool m_LightmapKeepDirCombined = true;
        [SerializeField] bool m_LightmapKeepDynamicPlain = true;
        [SerializeField] bool m_LightmapKeepDynamicDirCombined = true;
        [SerializeField] bool m_LightmapKeepShadowMask = true;
        [SerializeField] bool m_LightmapKeepSubtractive = true;
        [SerializeField] StrippingModes m_FogStripping = StrippingModes.Automatic;
        [SerializeField] bool m_FogKeepLinear = true;
        [SerializeField] bool m_FogKeepExp = true;
        [SerializeField] bool m_FogKeepExp2 = true;
        [SerializeField] InstancingStrippingMode m_InstancingStripping = InstancingStrippingMode.StripUnused;
        [SerializeField] BatchRendererGroupStrippingMode m_BrgStripping = BatchRendererGroupStrippingMode.KeepIfEntitiesGraphics;
        [SerializeField] bool m_LogWhenShaderIsCompiled = false;
        [SerializeField] bool m_CameraRelativeLightCulling = false;
        [SerializeField] bool m_CameraRelativeShadowCulling = false;
        [SerializeField] VideoShadersIncludeMode m_VideoShadersIncludeMode = VideoShadersIncludeMode.Always;
        [SerializeField] Shader[] m_AlwaysIncludedShaders = Array.Empty<Shader>();
        [SerializeField] LightProbeOutsideHullStrategy m_LightProbeOutsideHullStrategy = LightProbeOutsideHullStrategy.kLightProbeSearchTetrahedralHull;
        [SerializeField] ShaderVariantCollection[] m_PreloadedShaders = Array.Empty<ShaderVariantCollection>();
        [SerializeField] int m_PreloadShadersBatchTimeLimit = -1;

        internal StrippingModes lightmapStripping
        {
            get => m_LightmapStripping;
            set => m_LightmapStripping = value;
        }

        internal bool lightmapKeepPlain
        {
            get => m_LightmapKeepPlain;
            set => m_LightmapKeepPlain = value;
        }

        internal bool lightmapKeepDirCombined
        {
            get => m_LightmapKeepDirCombined;
            set => m_LightmapKeepDirCombined = value;
        }

        internal bool lightmapKeepDynamicPlain
        {
            get => m_LightmapKeepDynamicPlain;
            set => m_LightmapKeepDynamicPlain = value;
        }

        internal bool lightmapKeepDynamicDirCombined
        {
            get => m_LightmapKeepDynamicDirCombined;
            set => m_LightmapKeepDynamicDirCombined = value;
        }

        internal bool lightmapKeepShadowMask
        {
            get => m_LightmapKeepShadowMask;
            set => m_LightmapKeepShadowMask = value;
        }

        internal bool lightmapKeepSubtractive
        {
            get => m_LightmapKeepSubtractive;
            set => m_LightmapKeepSubtractive = value;
        }

        internal StrippingModes fogStripping
        {
            get => m_FogStripping;
            set => m_FogStripping = value;
        }

        internal bool fogKeepLinear
        {
            get => m_FogKeepLinear;
            set => m_FogKeepLinear = value;
        }

        internal bool fogKeepExp
        {
            get => m_FogKeepExp;
            set => m_FogKeepExp = value;
        }

        internal bool fogKeepExp2
        {
            get => m_FogKeepExp2;
            set => m_FogKeepExp2 = value;
        }

        internal InstancingStrippingMode instancingStripping
        {
            get => m_InstancingStripping;
            set => m_InstancingStripping = value;
        }

        internal BatchRendererGroupStrippingMode brgStripping
        {
            get => m_BrgStripping;
            set => m_BrgStripping = value;
        }

        internal bool logWhenShaderIsCompiled
        {
            get => m_LogWhenShaderIsCompiled;
            set => m_LogWhenShaderIsCompiled = value;
        }

        internal bool cameraRelativeLightCulling
        {
            get => m_CameraRelativeLightCulling;
            set => m_CameraRelativeLightCulling = value;
        }

        internal bool cameraRelativeShadowCulling
        {
            get => m_CameraRelativeShadowCulling;
            set => m_CameraRelativeShadowCulling = value;
        }

        internal VideoShadersIncludeMode videoShadersIncludeMode
        {
            get => m_VideoShadersIncludeMode;
            set => m_VideoShadersIncludeMode = value;
        }

        internal Shader[] alwaysIncludedShaders
        {
            get => m_AlwaysIncludedShaders;
            set => m_AlwaysIncludedShaders = value;
        }

        internal LightProbeOutsideHullStrategy lightProbeOutsideHullStrategy
        {
            get => m_LightProbeOutsideHullStrategy;
            set => m_LightProbeOutsideHullStrategy = value;
        }

        internal ShaderVariantCollection[] preloadedShaders
        {
            get => m_PreloadedShaders;
            set => m_PreloadedShaders = value;
        }

        internal int preloadShadersBatchTimeLimit
        {
            get => m_PreloadShadersBatchTimeLimit;
            set => m_PreloadShadersBatchTimeLimit = value;
        }

        public void Instantiate()
        {
            name = "Graphics Settings";
            hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        }

        public void SetGraphicsSetting(string settingName, int value)
        {
            switch (settingName)
            {
                case "LightmapStripping":
                    lightmapStripping = (StrippingModes)value;
                    break;
                case "LightmapKeepPlain":
                    lightmapKeepPlain = IntegerToBoolean(value);
                    break;
                case "LightmapKeepDirCombined":
                    lightmapKeepDirCombined = IntegerToBoolean(value);
                    break;
                case "LightmapKeepDynamicPlain":
                    lightmapKeepDynamicPlain = IntegerToBoolean(value);
                    break;
                case "LightmapKeepDynamicDirCombined":
                    lightmapKeepDynamicDirCombined = IntegerToBoolean(value);
                    break;
                case "LightmapKeepShadowMask":
                    lightmapKeepShadowMask = IntegerToBoolean(value);
                    break;
                case "LightmapKeepSubtractive":
                    lightmapKeepSubtractive = IntegerToBoolean(value);
                    break;
                case "FogStripping":
                    fogStripping = (StrippingModes)value;
                    break;
                case "FogKeepLinear":
                    fogKeepLinear = IntegerToBoolean(value);
                    break;
                case "FogKeepExp":
                    fogKeepExp = IntegerToBoolean(value);
                    break;
                case "FogKeepExp2":
                    fogKeepExp2 = IntegerToBoolean(value);
                    break;
                case "InstancingStripping":
                    instancingStripping = (InstancingStrippingMode)value;
                    break;
                case "BrgStripping":
                    brgStripping = (BatchRendererGroupStrippingMode)value;
                    break;
                case "LogWhenShaderIsCompiled":
                    logWhenShaderIsCompiled = IntegerToBoolean(value);
                    break;
                case "CameraRelativeLightCulling":
                    cameraRelativeLightCulling = IntegerToBoolean(value);
                    break;
                case "CameraRelativeShadowCulling":
                    cameraRelativeShadowCulling = IntegerToBoolean(value);
                    break;
                case "VideoShadersIncludeMode":
                    videoShadersIncludeMode = (VideoShadersIncludeMode)value;
                    break;
                case "LightProbeOutsideHullStrategy":
                    lightProbeOutsideHullStrategy = (LightProbeOutsideHullStrategy)value;
                    break;
                case "PreloadShadersBatchTimeLimit":
                    preloadShadersBatchTimeLimit = value;
                    break;
            }

            static bool IntegerToBoolean(int value) => value != 0;
        }

        public int GetGraphicsSetting(string settingName)
        {
            return settingName switch
            {
                "LightmapStripping" => (int)lightmapStripping,
                "LightmapKeepPlain" => BooleanToInteger(lightmapKeepPlain),
                "LightmapKeepDirCombined" => BooleanToInteger(lightmapKeepDirCombined),
                "LightmapKeepDynamicPlain" => BooleanToInteger(lightmapKeepDynamicPlain),
                "LightmapKeepDynamicDirCombined" => BooleanToInteger(lightmapKeepDynamicDirCombined),
                "LightmapKeepShadowMask" => BooleanToInteger(lightmapKeepShadowMask),
                "LightmapKeepSubtractive" => BooleanToInteger(lightmapKeepSubtractive),
                "FogStripping" => (int)fogStripping,
                "FogKeepLinear" => BooleanToInteger(fogKeepLinear),
                "FogKeepExp" => BooleanToInteger(fogKeepExp),
                "FogKeepExp2" => BooleanToInteger(fogKeepExp2),
                "InstancingStripping" => (int)instancingStripping,
                "BrgStripping" => (int)brgStripping,
                "LogWhenShaderIsCompiled" => BooleanToInteger(logWhenShaderIsCompiled),
                "CameraRelativeLightCulling" => BooleanToInteger(cameraRelativeLightCulling),
                "CameraRelativeShadowCulling" => BooleanToInteger(cameraRelativeShadowCulling),
                "VideoShadersIncludeMode" => (int)videoShadersIncludeMode,
                "LightProbeOutsideHullStrategy" => (int)lightProbeOutsideHullStrategy,
                "PreloadShadersBatchTimeLimit" => preloadShadersBatchTimeLimit,
                _ => k_InvalidGraphicsSetting,
            };

            static int BooleanToInteger(bool value) => value ? 1 : 0;
        }
    }
}
