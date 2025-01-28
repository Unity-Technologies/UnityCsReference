// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Contains overall summary information about a build.</summary>
    [NativeType(Header = "Modules/BuildReportingEditor/Managed/BuildSummary.bindings.h", CodegenOptions = CodegenOptions.Custom)]
    public struct BuildSummary
    {
        internal Int64 buildStartTimeTicks;
        ///<summary>The time the build was started.</summary>
        public DateTime buildStartedAt { get { return new DateTime(buildStartTimeTicks); } }

        ///<summary>The <see cref="Application.buildGUID" /> of the build.</summary>
        [NativeName("buildGUID")]
        public GUID guid { get; }

        ///<summary>The platform that the build was created for.</summary>
        ///<remarks>See <see cref="BuildTarget" /> for possible values.</remarks>
        public BuildTarget platform { get; }
        ///<summary>The platform group the build was created for.</summary>
        ///<remarks>See <see cref="BuildTargetGroup" /> for possible values.</remarks>
        public BuildTargetGroup platformGroup { get; }
        internal int subtarget { get; }
        ///<summary>The <see cref="BuildOptions" /> used for the build, as passed to <see cref="BuildPipeline.BuildPlayer" />.</summary>
        public BuildOptions options { get; }
        internal BuildAssetBundleOptions assetBundleOptions { get; }
        ///<summary>The output path for the build, as provided to <see cref="BuildPipeline.BuildPlayer" />.</summary>
        public string outputPath { get; }
        internal uint crc { get; }
        ///<summary>The total size of the build output, in bytes.</summary>
        public ulong totalSize { get; }

        internal UInt64 totalTimeTicks;
        ///<summary>The total time taken by the build process.</summary>
        public TimeSpan totalTime { get { return new TimeSpan((long)totalTimeTicks); } }
        ///<summary>The time the build ended.</summary>
        public DateTime buildEndedAt {  get { return buildStartedAt + totalTime; } }

        ///<summary>The total number of errors and exceptions recorded during the build process.</summary>
        public int totalErrors { get; }
        ///<summary>The total number of warnings recorded during the build process.</summary>
        public int totalWarnings { get; }

        ///<summary>The outcome of the build.</summary>
        ///<remarks>See <see cref="BuildResult" /> for possible outcomes.</remarks>
        [NativeName("buildResult")]
        public BuildResult result { get; }

        ///<summary>The type of build.</summary>
        public BuildType buildType { get; }
        ///<summary>Whether the multi-process option was enabled for the build.</summary>
        ///<seealso cref="EditorBuildSettings.UseParallelAssetBundleBuilding" />
        public bool multiProcessEnabled { get; }

        private T ParseSubtarget<T, S>() where T : Enum where S : Enum
        {
            if (typeof(T) != typeof(S))
                throw new ArgumentException($"Subtarget type ({typeof(T).ToString()}) is not valid for the platform ({platform}). Expected: {typeof(S).ToString()}");

            if (!Enum.IsDefined(typeof(T), subtarget))
                throw new InvalidOperationException($"The subtarget value ({subtarget}) is not a valid {typeof(T).ToString()}");

            return (T)(object)subtarget;
        }

        ///<summary>The subtarget that the build was created for.</summary>
        ///<remarks>Valid values for type T are <see cref="StandaloneBuildSubtarget" />, <see cref="PS4BuildSubtarget" /> and <see cref="XboxBuildSubtarget" /></remarks>
        ///<returns>Returns the subtarget value.</returns>
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
