// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Build;
using System.Runtime.InteropServices;
using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;

namespace UnityEditor
{
    [StaticAccessor("BuildTargetDiscovery::GetInstance()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Src/BuildPipeline/BuildTargetDiscovery.h")]
    internal static class BuildTargetDiscovery
    {
        const int kShortNameIndex = 0;

        [Flags]
        public enum TargetAttributes
        {
            None                            = 0,
            IsDeprecated                    = (1 << 0),
            HasIntegratedGPU                = (1 << 1),
            IsConsole                       = (1 << 2),
            IsX64                           = (1 << 3),
            IsStandalonePlatform            = (1 << 4),
            DynamicBatchingDisabled         = (1 << 5),
            CompressedGPUSkinningDisabled   = (1 << 6),
            UseForsythOptimizedMeshData     = (1 << 7),
            DisableEnlighten                = (1 << 8),
            ReflectionEmitDisabled          = (1 << 9),
            OSFontsDisabled                 = (1 << 10),
            NoDefaultUnityFonts             = (1 << 11),
            [Obsolete("Facebook support was removed in 2019.3")]
            SupportsFacebook                = (1 << 12),
            WarnForMouseEvents              = (1 << 13),
            HideInUI                        = (1 << 14),
            GPUSkinningNotSupported         = (1 << 15),
            StrippingNotSupported           = (1 << 16),
            DisableNativeHDRLightmaps       = (1 << 17),
            UsesNativeHDR                   = (1 << 18),
            // ProtectedGraphicsMem            = (1 << 19),     This was removed.
            IsMTRenderingDisabledByDefault  = (1 << 20)
        }

        [Flags]
        public enum SupportedTextureCompression
        {
            None   = 0,
            ETC    = (1 << 0),
            ETC2   = (1 << 1),
            PVRTC  = (1 << 2),
            ASTC   = (1 << 3),
            DXTC   = (1 << 4),
            DXT5nm = (1 << 5)
        }

        [Flags]
        public enum VRAttributes
        {
            None                                = 0,
            SupportSinglePassStereoRendering    = (1 << 0),
            SupportStereoInstancingRendering    = (1 << 1),
            SupportStereoMultiviewRendering     = (1 << 2),
            SupportStereo360Capture             = (1 << 3),
            SupportTango                        = (1 << 4)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DiscoveredTargetInfo
        {
            public string path;
            public string moduleName;
            public string dirName;
            public string platformDefine;
            public string niceName;
            public string iconName;
            public string assemblyName;

            public BuildTarget buildTargetPlatformVal;

            // Build targets can have many names to identify them
            public string[] nameList;

            // Build targets can sometimes support more than one renderer
            public int[] rendererList;

            public TargetAttributes flags;

            public VRAttributes vrFlags;

            public bool HasFlag(TargetAttributes flag) { return (flags & flag) == flag; }
        }

        public static extern bool PlatformHasFlag(BuildTarget platform, TargetAttributes flag);

        public static extern bool PlatformGroupHasFlag(BuildTargetGroup group, TargetAttributes flag);

        public static extern bool PlatformGroupHasVRFlag(BuildTargetGroup group, VRAttributes flag);

        public static extern DiscoveredTargetInfo[] GetBuildTargetInfoList();

        public static extern int[] GetRenderList(BuildTarget platform);

        private static extern string GetNiceNameByBuildTarget(BuildTarget platform);

        public static extern string GetModuleNameForBuildTarget(BuildTarget platform);

        public static extern string GetModuleNameForBuildTargetGroup(BuildTargetGroup group);

        public static bool BuildTargetSupportsRenderer(BuildPlatform platform, GraphicsDeviceType type)
        {
            BuildTarget buildTarget = platform.defaultTarget;
            if (platform.targetGroup == BuildTargetGroup.Standalone)
                buildTarget = DesktopStandaloneBuildWindowExtension.GetBestStandaloneTarget(buildTarget);

            foreach (int var in GetRenderList(buildTarget))
            {
                if ((GraphicsDeviceType)var == type)
                    return true;
            }

            return false;
        }

        public static string GetBuildTargetNiceName(BuildTarget platform, BuildTargetGroup buildTargetGroup = BuildTargetGroup.Unknown)
        {
            return GetNiceNameByBuildTarget(platform);
        }

        public static string GetScriptAssemblyName(DiscoveredTargetInfo btInfo)
        {
            return !String.IsNullOrEmpty(btInfo.assemblyName) ? btInfo.assemblyName : btInfo.nameList[kShortNameIndex];
        }
    }
}
