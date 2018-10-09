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
        const int kAssemblyNameIndex = 1;

        [Flags]
        public enum TargetAttributes
        {
            None = 0,
            IsDeprecated = (1 << 0),
            IsMobile = (1 << 1),
            IsConsole = (1 << 2),
            IsX64 = (1 << 3),
            IsStandalonePlatform = (1 << 4),
            DynamicBatchingDisabled = (1 << 5),
            CompressedGPUSkinningDisabled = (1 << 6),
            UseForsythOptimizedMeshData = (1 << 7),
            DisableEnlighten = (1 << 8),
            ReflectionEmitDisabled = (1 << 9),
            OSFontsDisabled = (1 << 10),
            ETC = (1 << 11),
            ETC2 = (1 << 12),
            PVRTC = (1 << 13),
            ASTC = (1 << 14),
            DXTDisabled = (1 << 15),
            OpenGLES = (1 << 16),
            SupportsFacebook = (1 << 17),
            WarnForExpensiveQualitySettings = (1 << 18),
            WarnForMouseEvents = (1 << 19),
            HideInUI = (1 << 20),
            GPUSkinningNotSupported = (1 << 21),
            StrippingNotSupported = (1 << 22),
            IsMTRenderingDisabledByDefault = (1 << 23),
        }

        public enum TargetDefaultScriptingBackend
        {
            Il2cpp = 0,
            Mono = 1,
            DotNet = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DiscoveredTargetInfo
        {
            public string path;
            public string dllName;
            public string dirName;
            public string platformDefine;
            public string niceName;
            public string iconName;

            public BuildTarget buildTgtPlatformVal;

            // Default scripting backend
            public TargetDefaultScriptingBackend scriptingBackend;

            // Build targets can have many names to identify them
            public string[] nameList;

            // Build targets can sometimes support more than one renderer
            public int[] rendererList;

            public TargetAttributes flags;

            public bool HasFlag(TargetAttributes flag) { return (flags & flag) == flag; }
        }

        public static extern bool PlatformHasFlag(BuildTarget platform, TargetAttributes flag);

        public static extern bool PlatformGroupHasFlag(BuildTargetGroup group, TargetAttributes flag);

        public static extern DiscoveredTargetInfo[] GetBuildTargetInfoList();

        public static extern int[] GetRenderList(BuildTarget platform);

        private static extern string GetNiceNameByBuildTarget(BuildTarget platform);

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
            if (PlatformHasFlag(platform, TargetAttributes.SupportsFacebook) && buildTargetGroup == BuildTargetGroup.Facebook)
            {
                return "Facebook";
            }

            return GetNiceNameByBuildTarget(platform);
        }

        public static string GetScriptAssemblyName(DiscoveredTargetInfo btInfo)
        {
            if (btInfo.nameList.Length == 1)
                return btInfo.nameList[kShortNameIndex];
            return btInfo.nameList[kAssemblyNameIndex];
        }
    }
}
