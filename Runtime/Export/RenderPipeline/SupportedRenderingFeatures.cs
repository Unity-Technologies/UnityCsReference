// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    public class SupportedRenderingFeatures
    {
        private static SupportedRenderingFeatures s_Active = new SupportedRenderingFeatures();
        public static SupportedRenderingFeatures active
        {
            get
            {
                if (s_Active == null)
                    s_Active = new SupportedRenderingFeatures();
                return s_Active;
            }
            set { s_Active = value; }
        }

        [Flags]
        public enum ReflectionProbeModes
        {
            None = 0,
            Rotation = 1
        }

        // this is done since the original enum doesn't allow flags due to it starting at 0, not 1
        [Flags]
        public enum LightmapMixedBakeModes
        {
            None = 0,
            IndirectOnly = 1,
            Subtractive = 2,
            Shadowmask = 4
        }

        public ReflectionProbeModes reflectionProbeModes { get; set; } = ReflectionProbeModes.None;

        public LightmapMixedBakeModes defaultMixedLightingModes { get; set; } = LightmapMixedBakeModes.None;

        public LightmapMixedBakeModes mixedLightingModes { get; set; } =
            LightmapMixedBakeModes.IndirectOnly | LightmapMixedBakeModes.Subtractive | LightmapMixedBakeModes.Shadowmask;

        public LightmapBakeType lightmapBakeTypes { get; set; } =
            LightmapBakeType.Realtime | LightmapBakeType.Mixed | LightmapBakeType.Baked;

        public LightmapsMode lightmapsModes { get; set; } =
            LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional;
        [Obsolete("Bake with the Progressive Lightmapper. The backend that uses Enlighten to bake is obsolete.", true)]
        public bool enlightenLightmapper { get; set; } = false;
        public bool enlighten { get; set; } = true;
        public bool lightProbeProxyVolumes { get; set; } = true;
        public bool motionVectors { get; set; } = true;
        public bool receiveShadows { get; set; } = true;
        public bool reflectionProbes { get; set; } = true;
        public bool reflectionProbesBlendDistance { get; set; } = true;
        public bool rendererPriority { get; set; } = false;
        public bool rendersUIOverlay { get; set; } = false;
        public bool overridesEnvironmentLighting { get; set; } = false;
        public bool overridesFog { get; set; } = false;
        public bool overridesRealtimeReflectionProbes { get; set; } = false;
        public bool overridesOtherLightingSettings { get; set; } = false;
        public bool editableMaterialRenderQueue { get; set; } = true;
        public bool overridesLODBias { get; set; } = false;
        public bool overridesMaximumLODLevel { get; set; } = false;
        public bool overridesEnableLODCrossFade { get; set; } = false;
        public bool rendererProbes { get; set; } = true;
        public bool particleSystemInstancing { get; set; } = true;
        [Obsolete("autoAmbientProbeBaking is obsolete. To enable or disable baking of the ambient probe, use ambientProbeBaking instead. (UnityUpgradable) -> ambientProbeBaking", false)]
        public bool autoAmbientProbeBaking { get { return ambientProbeBaking; } set { ambientProbeBaking = value; } }
        [Obsolete("autoDefaultReflectionProbeBaking is obsolete. To enable or disable baking of the default reflection probe, use defaultReflectionProbeBaking instead. (UnityUpgradable) -> defaultReflectionProbeBaking", false)]
        public bool autoDefaultReflectionProbeBaking { get { return defaultReflectionProbeBaking; } set { defaultReflectionProbeBaking = value; } }
        public bool ambientProbeBaking { get; set; } = true;
        public bool defaultReflectionProbeBaking { get; set; } = true;
        public bool overridesShadowmask { get; set; } = false;
        public bool overridesLightProbeSystem { get; set; } = false;
        public bool supportsHDR { get; set; } = false;
        public bool supportsClouds { get; set; } = false;
        public string overridesLightProbeSystemWarningMessage { get; set; } = "Light Probe Groups are unavailable as Probe Volumes have been enabled by the current Render Pipeline.";

        internal static unsafe MixedLightingMode FallbackMixedLightingMode()
        {
            MixedLightingMode fallbackMode;
            FallbackMixedLightingModeByRef(new IntPtr(&fallbackMode));
            return fallbackMode;
        }

        [RequiredByNativeCode]
        internal static unsafe void FallbackMixedLightingModeByRef(IntPtr fallbackModePtr)
        {
            var fallbackMode = (MixedLightingMode*)fallbackModePtr;

            // if the pipeline has picked a default value that is supported
            if ((active.defaultMixedLightingModes != LightmapMixedBakeModes.None)
                && ((active.mixedLightingModes & active.defaultMixedLightingModes) == active.defaultMixedLightingModes))
            {
                switch (active.defaultMixedLightingModes)
                {
                    case LightmapMixedBakeModes.Shadowmask:
                        *fallbackMode = MixedLightingMode.Shadowmask;
                        break;

                    case LightmapMixedBakeModes.Subtractive:
                        *fallbackMode = MixedLightingMode.Subtractive;
                        break;

                    default:
                        *fallbackMode = MixedLightingMode.IndirectOnly;
                        break;
                }
                return;
            }

            // otherwise we try to find a value that is supported
            if (IsMixedLightingModeSupported(MixedLightingMode.Shadowmask))
            {
                *fallbackMode = MixedLightingMode.Shadowmask;
                return;
            }

            if (IsMixedLightingModeSupported(MixedLightingMode.Subtractive))
            {
                *fallbackMode = MixedLightingMode.Subtractive;
                return;
            }

            // last restort. make sure Mixed mode is even supported, otherwise the baking pipeline will treat the Mixed lights it as Realtime
            *fallbackMode = MixedLightingMode.IndirectOnly;
        }

        internal static unsafe bool IsMixedLightingModeSupported(MixedLightingMode mixedMode)
        {
            bool isSupported;
            IsMixedLightingModeSupportedByRef(mixedMode, new IntPtr(&isSupported));
            return isSupported;
        }

        [RequiredByNativeCode]
        internal unsafe static void IsMixedLightingModeSupportedByRef(MixedLightingMode mixedMode, IntPtr isSupportedPtr)
        {
            // if Mixed mode hasn't been turned off completely and the Mixed lights will be treated as Realtime
            bool* isSupported = (bool*)isSupportedPtr;

            if (!IsLightmapBakeTypeSupported(LightmapBakeType.Mixed))
            {
                *isSupported = false;
                return;
            }

            // this is done since the original enum doesn't allow flags due to it starting at 0, not 1
            *isSupported = ((mixedMode == MixedLightingMode.IndirectOnly &&
                ((active.mixedLightingModes &
                    LightmapMixedBakeModes.IndirectOnly) == LightmapMixedBakeModes.IndirectOnly)) ||
                (mixedMode == MixedLightingMode.Subtractive &&
                    ((active.mixedLightingModes &
                        LightmapMixedBakeModes.Subtractive) == LightmapMixedBakeModes.Subtractive)) ||
                (mixedMode == MixedLightingMode.Shadowmask &&
                    ((active.mixedLightingModes &
                        LightmapMixedBakeModes.Shadowmask) == LightmapMixedBakeModes.Shadowmask)));
        }

        internal static unsafe bool IsLightmapBakeTypeSupported(LightmapBakeType bakeType)
        {
            bool isSupported;
            IsLightmapBakeTypeSupportedByRef(bakeType, new IntPtr(&isSupported));
            return isSupported;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsLightmapBakeTypeSupportedByRef(LightmapBakeType bakeType, IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;

            if (bakeType == LightmapBakeType.Mixed)
            {
                // we can't have Mixed without Bake
                bool isBakedSupported = IsLightmapBakeTypeSupported(LightmapBakeType.Baked);

                // we can't support Mixed mode and then not support any of the different modes
                if (!isBakedSupported || active.mixedLightingModes == LightmapMixedBakeModes.None)
                {
                    *isSupported = false;
                    return;
                }
            }

            *isSupported = ((active.lightmapBakeTypes & bakeType) == bakeType);

            // If we are using real time lighting and Enlighten is disabled, don't allow realtime GI.
            if (bakeType == LightmapBakeType.Realtime && !active.enlighten)
                *isSupported = false;
        }

        internal static unsafe bool IsLightmapsModeSupported(LightmapsMode mode)
        {
            bool isSupported;
            IsLightmapsModeSupportedByRef(mode, new IntPtr(&isSupported));
            return isSupported;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsLightmapsModeSupportedByRef(LightmapsMode mode, IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;
            *isSupported = ((active.lightmapsModes & mode) == mode);
        }

        internal static unsafe bool IsLightmapperSupported(int lightmapper)
        {
            bool isSupported;
            IsLightmapperSupportedByRef(lightmapper, new IntPtr(&isSupported));
            return isSupported;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsLightmapperSupportedByRef(int lightmapper, IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;

            // 0 = Enlighten
            *isSupported = lightmapper != 0;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsUIOverlayRenderedBySRP(IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;
            *isSupported = active.rendersUIOverlay;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsAmbientProbeBakingSupported(IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;
            *isSupported = active.ambientProbeBaking;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsDefaultReflectionProbeBakingSupported(IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;
            *isSupported = active.defaultReflectionProbeBaking;
        }
        [RequiredByNativeCode]
        internal static unsafe void OverridesLightProbeSystem(IntPtr overridesPtr)
        {
            var overrides = (bool*)overridesPtr;
            *overrides = active.overridesLightProbeSystem;
        }

        internal static unsafe int FallbackLightmapper()
        {
            int lightmapper;
            FallbackLightmapperByRef(new IntPtr(&lightmapper));
            return lightmapper;
        }

        [RequiredByNativeCode]
        internal static unsafe void FallbackLightmapperByRef(IntPtr lightmapperPtr)
        {
            var lightmapper = (int*)lightmapperPtr;

            *lightmapper = 1; // Progressive CPU
        }

        [Obsolete("terrainDetailUnsupported is deprecated.")]
        public bool terrainDetailUnsupported
        {
            get { return true; }
            set {}
        }
    }
}
