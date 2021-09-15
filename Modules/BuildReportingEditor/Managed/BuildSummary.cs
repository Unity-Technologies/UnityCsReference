// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    [NativeType(Header = "Modules/BuildReportingEditor/Managed/BuildSummary.bindings.h", CodegenOptions = CodegenOptions.Custom)]
    public struct BuildSummary
    {
        internal Int64 buildStartTimeTicks;
        public DateTime buildStartedAt { get { return new DateTime(buildStartTimeTicks); } }

        [NativeName("buildGUID")]
        public GUID guid { get; }

        public BuildTarget platform { get; }
        public BuildTargetGroup platformGroup { get; }
        internal int subtarget { get; }
        public BuildOptions options { get; }
        internal BuildAssetBundleOptions assetBundleOptions { get; }
        public string outputPath { get; }
        internal uint crc { get; }
        public ulong totalSize { get; }

        internal UInt64 totalTimeTicks;
        public TimeSpan totalTime { get { return new TimeSpan((long)totalTimeTicks); } }
        public DateTime buildEndedAt {  get { return buildStartedAt + totalTime; } }

        public int totalErrors { get; }
        public int totalWarnings { get; }

        [NativeName("buildResult")]
        public BuildResult result { get; }

        internal BuildType buildType { get; }

        private T ParseSubtarget<T, S>() where T : Enum where S : Enum
        {
            if (typeof(T) != typeof(S))
                throw new ArgumentException($"Subtarget type ({typeof(T).ToString()}) is not valid for the platform ({platform}). Expected: {typeof(S).ToString()}");

            if (!Enum.IsDefined(typeof(T), subtarget))
                throw new InvalidOperationException($"The subtarget value ({subtarget}) is not a valid {typeof(T).ToString()}");

            return (T)(object)subtarget;
        }

        public T GetSubtarget<T>() where T : Enum
        {
            switch (platform)
            {
                // ADD_NEW_PLATFORM_HERE
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return ParseSubtarget<T, StandaloneBuildSubtarget>();
                case BuildTarget.PS4:
                    return ParseSubtarget<T, PS4BuildSubtarget>();
                case BuildTarget.XboxOne:
                    return ParseSubtarget<T, XboxBuildSubtarget>();
                default:
                    throw new ArgumentException($"Subtarget property is not available for the platform ({platform})");
            }
        }
    }
}
