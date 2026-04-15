// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    /// <summary>Contains overall summary information about a build.</summary>
    /// <remarks>This struct is part of the <see cref="BuildReport"/> and is accessible via <see cref="BuildReport.summary"/>.
    /// It contains detailed information such as build times, platform, options, and result.
    ///
    /// For a lightweight alternative that can be read without loading the full <see cref="BuildReport"/>,
    /// use <see cref="Build.BuildReportSummary"/>, which is stored as JSON and tracked by the <see cref="Build.BuildHistory"/>.
    /// </remarks>
    /// <seealso cref="Build.BuildReportSummary"/>
    /// <seealso cref="Build.BuildHistory"/>
    [NativeHeader("Modules/BuildReportingEditor/Public/BuildReport.h")]
    public struct BuildSummary
    {
        [NativeName("buildStartTime.ticks")]
        internal Int64 buildStartTimeTicks;
        ///<summary>The time the build was started, in UTC.</summary>
        ///<remarks>Call <c>.ToLocalTime()</c> to convert to the local timezone for display.
        ///</remarks>
        ///<example>
        /// <code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/BuildSummaryTimes.cs"/>
        ///</example>
        public DateTime buildStartedAt { get { return new DateTime(buildStartTimeTicks, DateTimeKind.Utc); } }

        ///<summary>The GUID of a Player build.</summary>
        ///<remarks>For successful Player builds this GUID is written to boot.config in the build output,
        /// unless <see cref="BuildOptions.NoUniqueIdentifier"/> is set.
        /// It is available in the runtime through <see cref="Application.buildGUID" />.
        /// Incremental builds that produce the same output may reuse a previously
        /// generated build GUID.
        ///</remarks>
        [NativeName("buildGUID")]
        public GUID guid { get; }

        ///<summary>A unique identifier for the build session in the Unity Editor.</summary>
        ///<remarks>The buildSessionGuid is set for Player, AssetBundle and ContentDirectory builds.
        ///This GUID uniquely identifies each build session, regardless of whether the build produces identical output.
        ///Failed or cancelled builds will also have a unique session guid.
        ///Unlike <see cref="guid"/>, this identifier is not stored in the Player's built output, and is only used for Editor build tracking and analytics.
        ///</remarks>
        [NativeName("buildSessionGUID")]
        public GUID buildSessionGuid { get; }

        ///<summary>The platform that the build was created for.</summary>
        ///<remarks>See <see cref="BuildTarget" /> for possible values.</remarks>
        public BuildTarget platform { get; }
        ///<summary>The platform group the build was created for.</summary>
        ///<remarks>See <see cref="BuildTargetGroup" /> for possible values.</remarks>
        public BuildTargetGroup platformGroup { get; }
        internal int subtarget { [VisibleToOtherModules("UnityEditor.BurstModule")] get; }
        ///<summary>The <see cref="BuildOptions" /> used for the build, as passed to <see cref="BuildPipeline.BuildPlayer" />.</summary>
        public BuildOptions options { get; }

        ///<summary>If the build is a ContentDirectory build this returns the <see cref="BuildContentOptions" /> passed to <see cref="BuildPipeline.BuildContentDirectory" />.</summary>
        public BuildContentOptions buildContentOptions { get; }

        ///<summary>If the build is an AssetBundle build this returns the <see cref="BuildAssetBundleOptions" /> passed to <see cref="BuildPipeline.BuildAssetBundles" />.</summary>
        public BuildAssetBundleOptions assetBundleOptions { get; }

        ///<summary>The output path for the build, as provided to <see cref="BuildPipeline.BuildPlayer" />.</summary>
        public string outputPath { get; }
        ///<summary>The platform-specific path of the Data folder for a player build. For AssetBundle builds, this will be identical to the output path.</summary>
        public string dataPath { get; }

        ///<summary>For ContentDirectory builds this returns the build name. For Player builds this returns the product name from PlayerSettings.</summary>
        public string buildName { get; }

        internal uint crc { get; }
        ///<summary>The total size of the build output, in bytes.</summary>
        public ulong totalSize { get; }

        internal UInt64 totalTimeTicks;
        ///<summary>The total time taken by the build process.</summary>
        public TimeSpan totalTime { get { return new TimeSpan((long)totalTimeTicks); } }
        ///<summary>The time the build ended, in UTC.</summary>
        ///<remarks>Calculated as <see cref="buildStartedAt"/> plus <see cref="totalTime"/>.</remarks>
        public DateTime buildEndedAt { get { return buildStartedAt + totalTime; } }

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

		// TODO: This should be deprecated, it was tracking use of Multi-process assetbundle building, removed in 6.4
        ///<summary>Whether the multi-process option was enabled for the build.</summary>
        public bool multiProcessEnabled { get; }

        ///<summary>For ContentDirectory builds this returns the Hash128 of the build manifest. For other build types this returns a default Hash128.</summary>
        public Hash128 buildManifestHash { get; }

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

        internal string GetSubtargetString()
        {
            switch (platform)
            {
                // ADD_NEW_PLATFORM_HERE
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return ParseSubtarget<StandaloneBuildSubtarget, StandaloneBuildSubtarget>().ToString();

                case BuildTarget.PS4:
                    return ParseSubtarget<PS4BuildSubtarget, PS4BuildSubtarget>().ToString();

                case BuildTarget.XboxOne:
                    return ParseSubtarget<XboxBuildSubtarget, XboxBuildSubtarget>().ToString();

                default:
                    return subtarget.ToString();
            }
        }
    }
}
