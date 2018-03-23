// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
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

        [System.Flags]
        public enum ReflectionProbeSupportFlags
        {
            None = 0,
            Rotation = 1
        }

        [System.Flags]
        public enum LightmapMixedBakeMode
        {
            None = 0,
            IndirectOnly = 1,
            Subtractive = 2,
            Shadowmask = 4
        };

        public ReflectionProbeSupportFlags reflectionProbeSupportFlags { get; set; } = ReflectionProbeSupportFlags.None;

        public LightmapMixedBakeMode defaultMixedLightingMode { get; set; } = LightmapMixedBakeMode.None;

        public LightmapMixedBakeMode supportedMixedLightingModes { get; set; } =
            LightmapMixedBakeMode.IndirectOnly | LightmapMixedBakeMode.Subtractive | LightmapMixedBakeMode.Shadowmask;

        public LightmapBakeType supportedLightmapBakeTypes { get; set; } =
            LightmapBakeType.Realtime | LightmapBakeType.Mixed | LightmapBakeType.Baked;

        public LightmapsMode supportedLightmapsModes { get; set; } =
            LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional;

        public bool rendererSupportsLightProbeProxyVolumes { get; set; } = true;
        public bool rendererSupportsMotionVectors { get; set; } = true;
        public bool rendererSupportsReceiveShadows { get; set; } = true;
        public bool rendererSupportsReflectionProbes { get; set; } = true;

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
            if ((SupportedRenderingFeatures.active.defaultMixedLightingMode != LightmapMixedBakeMode.None)
                && ((SupportedRenderingFeatures.active.supportedMixedLightingModes & SupportedRenderingFeatures.active.defaultMixedLightingMode) == SupportedRenderingFeatures.active.defaultMixedLightingMode))
            {
                switch (SupportedRenderingFeatures.active.defaultMixedLightingMode)
                {
                    case LightmapMixedBakeMode.Shadowmask:
                        *fallbackMode = MixedLightingMode.Shadowmask;
                        break;

                    case LightmapMixedBakeMode.Subtractive:
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
                             ((SupportedRenderingFeatures.active.supportedMixedLightingModes &
                               LightmapMixedBakeMode.IndirectOnly) == LightmapMixedBakeMode.IndirectOnly)) ||
                            (mixedMode == MixedLightingMode.Subtractive &&
                             ((SupportedRenderingFeatures.active.supportedMixedLightingModes &
                               LightmapMixedBakeMode.Subtractive) == LightmapMixedBakeMode.Subtractive)) ||
                            (mixedMode == MixedLightingMode.Shadowmask &&
                             ((SupportedRenderingFeatures.active.supportedMixedLightingModes &
                               LightmapMixedBakeMode.Shadowmask) == LightmapMixedBakeMode.Shadowmask)));
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
                if (!isBakedSupported || SupportedRenderingFeatures.active.supportedMixedLightingModes == LightmapMixedBakeMode.None)
                {
                    *isSupported = false;
                    return;
                }
            }

            *isSupported = ((SupportedRenderingFeatures.active.supportedLightmapBakeTypes & bakeType) == bakeType);
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
            *isSupported = ((SupportedRenderingFeatures.active.supportedLightmapsModes & mode) == mode);
        }
    }
}
