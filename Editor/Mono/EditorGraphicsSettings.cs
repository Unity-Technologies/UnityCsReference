// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;

namespace UnityEditor.Rendering
{
    public enum ShaderQuality
    {
        Low,
        Medium,
        High,
    }

    public struct AlbedoSwatchInfo
    {
        public string name;
        public Color  color;
        public float  minLuminance;
        public float  maxLuminance;
    }

    public enum BatchRendererGroupStrippingMode
    {
        [InspectorName("Strip if Entities Graphics Package is not installed")]
        KeepIfEntitiesGraphics = 0,
        StripAll,
        KeepAll
    }

    internal enum InstancingStrippingMode
    {
        StripUnused = 0,
        StripAll,
        KeepAll
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
        // Use the version with NamedBuildTarget passed instead.
        // [Obsolete("Use SetTierSettings() instead (UnityUpgradable) -> SetTierSettings(*)", false)]
        public static void SetTierSettings(BuildTargetGroup target, GraphicsTier tier, TierSettings settings)
        {
            if (settings.renderingPath == RenderingPath.UsePlayerSettings)
                throw new ArgumentException("TierSettings.renderingPath must be actual rendering path (not UsePlayerSettings)", "settings");

            SetTierSettingsImpl(target, tier, settings);
            MakeTierSettingsAutomatic(target, tier, false);
            OnUpdateTierSettings(target, true);
        }

        public static void SetTierSettings(NamedBuildTarget target, GraphicsTier tier, TierSettings settings) => SetTierSettings(target.ToBuildTargetGroup(), tier, settings);
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
        [Obsolete("Use GetTierSettings() instead (UnityUpgradable) -> GetTierSettings(*)", true)]
        public static PlatformShaderSettings GetShaderSettingsForPlatform(BuildTargetGroup target, ShaderHardwareTier tier)
        {
            PlatformShaderSettings pss = new PlatformShaderSettings();
            return pss;
        }

        [Obsolete("Use SetTierSettings() instead (UnityUpgradable) -> SetTierSettings(*)", true)]
        public static void SetShaderSettingsForPlatform(BuildTargetGroup target, ShaderHardwareTier tier, PlatformShaderSettings settings) {}

        [Obsolete("Use GraphicsTier instead of ShaderHardwareTier enum", true)]
        public static TierSettings GetTierSettings(BuildTargetGroup target, ShaderHardwareTier tier)
        {
            return GetTierSettings(target, (GraphicsTier)tier);
        }

        [Obsolete("Use GraphicsTier instead of ShaderHardwareTier enum", true)]
        public static void SetTierSettings(BuildTargetGroup target, ShaderHardwareTier tier, TierSettings settings)
        {
            SetTierSettings(target, (GraphicsTier)tier, settings);
        }
    }
}
