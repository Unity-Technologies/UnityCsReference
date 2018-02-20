// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        // this is done since the original enum doesn't allow flags due to it starting at 0, not 1
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

        [RequiredByNativeCode]
        internal static MixedLightingMode FallbackMixedLightingMode()
        {
            // if the pipeline has picked a default value that is supported
            if ((SupportedRenderingFeatures.active.defaultMixedLightingMode != LightmapMixedBakeMode.None)
                && ((SupportedRenderingFeatures.active.supportedMixedLightingModes & SupportedRenderingFeatures.active.defaultMixedLightingMode) == SupportedRenderingFeatures.active.defaultMixedLightingMode))
            {
                switch (SupportedRenderingFeatures.active.defaultMixedLightingMode)
                {
                    case LightmapMixedBakeMode.Shadowmask:
                        return MixedLightingMode.Shadowmask;

                    case LightmapMixedBakeMode.Subtractive:
                        return MixedLightingMode.Subtractive;

                    default:
                        return MixedLightingMode.IndirectOnly;
                }
            }

            // otherwise we try to find a value that is supported
            if (IsMixedLightingModeSupported(MixedLightingMode.Shadowmask))
                return MixedLightingMode.Shadowmask;

            if (IsMixedLightingModeSupported(MixedLightingMode.Subtractive))
                return MixedLightingMode.Subtractive;

            // last restort. make sure Mixed mode is even supported, otherwise the baking pipeline will treat the Mixed lights it as Realtime
            return MixedLightingMode.IndirectOnly;
        }

        [RequiredByNativeCode]
        internal static bool IsMixedLightingModeSupported(MixedLightingMode mixedMode)
        {
            // if Mixed mode hasn't been turned off completely and the Mixed lights will be treated as Realtime
            if (!IsLightmapBakeTypeSupported(LightmapBakeType.Mixed))
                return false;

            // this is done since the original enum doesn't allow flags due to it starting at 0, not 1
            return ((mixedMode == MixedLightingMode.IndirectOnly &&
                     ((SupportedRenderingFeatures.active.supportedMixedLightingModes &
                       LightmapMixedBakeMode.IndirectOnly) == LightmapMixedBakeMode.IndirectOnly)) ||
                    (mixedMode == MixedLightingMode.Subtractive &&
                     ((SupportedRenderingFeatures.active.supportedMixedLightingModes &
                       LightmapMixedBakeMode.Subtractive) == LightmapMixedBakeMode.Subtractive)) ||
                    (mixedMode == MixedLightingMode.Shadowmask &&
                     ((SupportedRenderingFeatures.active.supportedMixedLightingModes &
                       LightmapMixedBakeMode.Shadowmask) == LightmapMixedBakeMode.Shadowmask)));
        }

        [RequiredByNativeCode]
        internal static bool IsLightmapBakeTypeSupported(LightmapBakeType bakeType)
        {
            if (bakeType == LightmapBakeType.Mixed)
            {
                // we can't have Mixed without Bake
                if (!IsLightmapBakeTypeSupported(LightmapBakeType.Baked))
                    return false;

                // we can't support Mixed mode and then not support any of the different modes
                if (SupportedRenderingFeatures.active.supportedMixedLightingModes == LightmapMixedBakeMode.None)
                    return false;
            }

            return ((SupportedRenderingFeatures.active.supportedLightmapBakeTypes & bakeType) == bakeType);
        }

        [RequiredByNativeCode]
        internal static bool IsLightmapsModeSupported(LightmapsMode mode)
        {
            return ((SupportedRenderingFeatures.active.supportedLightmapsModes & mode) == mode);
        }
    }
}
