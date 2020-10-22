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

        public bool enlighten { get; set; } = true;
        public bool lightProbeProxyVolumes { get; set; } = true;
        public bool motionVectors { get; set; } = true;
        public bool receiveShadows { get; set; } = true;
        public bool reflectionProbes { get; set; } = true;
        public bool rendererPriority { get; set; } = false;
        public bool terrainDetailUnsupported { get; set; } = false;
        public bool rendersUIOverlay { get; set; }
        public bool overridesEnvironmentLighting { get; set; } = false;
        public bool overridesFog { get; set; } = false;
        public bool overridesRealtimeReflectionProbes { get; set; } = false;
        public bool overridesOtherLightingSettings { get; set; } = false;
        public bool editableMaterialRenderQueue { get; set; } = true;
        public bool overridesLODBias { get; set; } = false;
        public bool overridesMaximumLODLevel { get; set; } = false;
        public bool rendererProbes { get; set; } = true;
        public bool particleSystemInstancing { get; set; } = true;

        // if this is overridden, please provide a message telling the user where to find it, otherwise it will be an empty string (to prevent confusion)
        public bool overridesShadowmask { get; set; } = false;
        public string overrideShadowmaskMessage { get; set; } = "";

        public string shadowmaskMessage
        {
            get
            {
                if (!overridesShadowmask)
                    return "The Shadowmask Mode used at run time can be set in the Quality Settings panel.";
                return overrideShadowmaskMessage;
            }
        }

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

            // if we are using realtime GI and Enlighten has been deprecated, don't allow realtime GI
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
            // if the lightmapper is Enlighten but Enlighten is deprecated, it's not supported
            *isSupported = ((lightmapper == 0) && !active.enlighten) ? false : true;
        }

        [RequiredByNativeCode]
        internal static unsafe void IsUIOverlayRenderedBySRP(IntPtr isSupportedPtr)
        {
            var isSupported = (bool*)isSupportedPtr;
            *isSupported = active.rendersUIOverlay;
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
    }
}
