// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEditor.Rendering
{
    public enum ShaderQuality
    {
        Low,
        Medium,
        High,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TierSettings
    {
        public ShaderQuality    standardShaderQuality;
        public CameraHDRMode    hdrMode;
        public bool             reflectionProbeBoxProjection;
        public bool             reflectionProbeBlending;
        public bool             hdr;
        public bool             detailNormalMap;

        public bool             cascadedShadowMaps;
        public bool             prefer32BitShadowMaps;
        public bool             enableLPPV;
        public bool             semitransparentShadows;

        public RenderingPath    renderingPath;
        public RealtimeGICPUUsage realtimeGICPUUsage;
    }


    public sealed partial class EditorGraphicsSettings
    {
        public static TierSettings GetTierSettings(BuildTargetGroup target, GraphicsTier tier)
        {
            return GetTierSettingsImpl(target, tier);
        }

        public static void SetTierSettings(BuildTargetGroup target, GraphicsTier tier, TierSettings settings)
        {
            if (settings.renderingPath == RenderingPath.UsePlayerSettings)
                throw new ArgumentException("TierSettings.renderingPath must be actual rendering path (not UsePlayerSettings)", "settings");

            SetTierSettingsImpl(target, tier, settings);
            MakeTierSettingsAutomatic(target, tier, false);
            OnUpdateTierSettingsImpl(target, true);
        }

        internal static TierSettings GetCurrentTierSettings()
        {
            return GetCurrentTierSettingsImpl();
        }
    }


    //
    // deprecated
    //

    [Obsolete("Use TierSettings instead (UnityUpgradable) -> UnityEditor.Rendering.TierSettings", false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct PlatformShaderSettings
    {
        [MarshalAs(UnmanagedType.I1)] public bool cascadedShadowMaps;
        [MarshalAs(UnmanagedType.I1)] public bool reflectionProbeBoxProjection;
        [MarshalAs(UnmanagedType.I1)] public bool reflectionProbeBlending;

        public ShaderQuality    standardShaderQuality;
    }

    public sealed partial class EditorGraphicsSettings
    {
        [Obsolete("Use GetTierSettings() instead (UnityUpgradable) -> GetTierSettings(*)", false)]
        public static PlatformShaderSettings GetShaderSettingsForPlatform(BuildTargetGroup target, ShaderHardwareTier tier)
        {
            TierSettings ts = GetTierSettings(target, (GraphicsTier)tier);

            PlatformShaderSettings pss = new PlatformShaderSettings();
            pss.cascadedShadowMaps              = ts.cascadedShadowMaps;
            pss.standardShaderQuality           = ts.standardShaderQuality;
            pss.reflectionProbeBoxProjection    = ts.reflectionProbeBoxProjection;
            pss.reflectionProbeBlending         = ts.reflectionProbeBlending;
            return pss;
        }

        [Obsolete("Use SetTierSettings() instead (UnityUpgradable) -> SetTierSettings(*)", false)]
        public static void SetShaderSettingsForPlatform(BuildTargetGroup target, ShaderHardwareTier tier, PlatformShaderSettings settings)
        {
            // we want to preserve TierSettings members that are absent from PlatformShaderSettings
            TierSettings ts = GetTierSettings(target, (GraphicsTier)tier);

            ts.standardShaderQuality        = settings.standardShaderQuality;
            ts.cascadedShadowMaps           = settings.cascadedShadowMaps;
            ts.reflectionProbeBoxProjection = settings.reflectionProbeBoxProjection;
            ts.reflectionProbeBlending      = settings.reflectionProbeBlending;

            SetTierSettings(target, (GraphicsTier)tier, ts);
        }

        [Obsolete("Use GraphicsTier instead of ShaderHardwareTier enum", false)]
        public static TierSettings GetTierSettings(BuildTargetGroup target, ShaderHardwareTier tier)
        {
            return GetTierSettings(target, (GraphicsTier)tier);
        }

        [Obsolete("Use GraphicsTier instead of ShaderHardwareTier enum", false)]
        public static void SetTierSettings(BuildTargetGroup target, ShaderHardwareTier tier, TierSettings settings)
        {
            SetTierSettings(target, (GraphicsTier)tier, settings);
        }
    }
}
