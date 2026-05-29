// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build
{
    /// <summary>Contains summary information about a build tracked in the <see cref="BuildHistory"/>.</summary>
    /// <remarks>
    /// Each build tracked in the <see cref="BuildHistory"/> writes a <see cref="BuildReportSummary"/> file in JSON format
    /// alongside its other build metadata. This structure contains a subset of the information available in the full
    /// <see cref="Build.Reporting.BuildReport"/>, and uses JSON format to facilitate parsing by external tools.
    /// To access more detailed build information from within the Editor, use <see cref="BuildHistory.LoadBuildReport"/> to load the full <see cref="Build.Reporting.BuildReport"/>.
    /// </remarks>
    /// <seealso cref="BuildHistory"/>
    /// <seealso cref="Build.Reporting.BuildReport"/>
    /// <seealso cref="Build.Reporting.BuildSummary"/>
    [VisibleToOtherModules]
    [Serializable]
    public struct BuildReportSummary
    {
        internal const string kBuildReportSummaryFileName = "BuildReportSummary.json";
        internal const int kVersion = 2;

        // The schema version is pinned at the top; remaining fields are alphabetical so new
        // additions slot in mechanically and the JSON sidecar stays predictable for human readers.

        /// <summary>The schema version of the <see cref="BuildReportSummary"/> JSON format.</summary>
        public int Version;

        /// <summary>For ContentDirectory builds, the content build options, as an array of string values from the <see cref="UnityEditor.BuildContentOptions"/> enum.</summary>
        public string[] BuildContentOptions;

        /// <summary>For ContentDirectory builds, the Hash128 of the build manifest as a string. For other build types, this is a default Hash128 string.</summary>
        public string BuildManifestHash;

        ///<summary>For ContentDirectory builds, the build name. For Player builds, the product name from PlayerSettings.</summary>
        public string BuildName;

        /// <summary>For Player builds, the build options, as an array of string values from the <see cref="UnityEditor.BuildOptions"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.options"/>.</remarks>
        public string[] BuildOptions;

        /// <summary>The Asset path of the BuildProfile that was active when the build started, or an empty string when none was active.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildProfilePath"/>.
        /// The AssetDatabase GUID is intentionally not mirrored here — load the full <see cref="Build.Reporting.BuildReport"/>
        /// via <see cref="BuildHistory.LoadBuildReport"/> and read <see cref="Build.Reporting.BuildSummary.buildProfileGuid"/>
        /// when the GUID is needed (for example to track the profile across renames or moves).</remarks>
        public string BuildProfilePath;

        /// <summary>The outcome of the build.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.result"/>.</remarks>
        public BuildResult BuildResult;

        /// <summary>The outcome of the build, as the string form of the <see cref="Build.Reporting.BuildResult"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.result"/>.</remarks>
        public string BuildResultName;

        /// <summary>A unique identifier for the build session in the Unity Editor.</summary>
        /// <remarks>This identifier uniquely identifies each build session, regardless of whether the build produces identical output.
        /// Failed or cancelled builds will also have a unique session GUID.
        /// This identifier is used in the <see cref="BuildHistory"/> API as the main key to look up additional build information.
        ///
        /// This identifier is not stored in the built output.
        /// </remarks>
        public GUID BuildSessionGUID;

        /// <summary>The time the build was started, as a UTC timestamp in ISO 8601 round-trip format.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildStartedAt"/>.</remarks>
        public string BuildStartedAt;

        /// <summary>The type of build.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildType"/>.</remarks>
        public BuildType BuildType;

        /// <summary>The type of build, as the string form of the <see cref="Build.Reporting.BuildType"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildType"/>.</remarks>
        public string BuildTypeName;

        /// <summary>The output path for the build.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.outputPath"/>.</remarks>
        public string OutputPath;

        /// <summary>The platform the build was created for.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.platform"/>.</remarks>
        public BuildTarget Platform;

        /// <summary>The platform the build was created for, as the string form of the <see cref="BuildTarget"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.platform"/>.</remarks>
        public string PlatformName;

        /// <summary>The subtarget the build was created for, as an integer.</summary>
        /// <remarks>The integer maps to a platform-specific subtarget enum. For standalone platforms this is <see cref="StandaloneBuildSubtarget"/>.
        /// Valid subtarget values depend on the platform. See <see cref="Build.Reporting.BuildSummary.GetSubtarget{T}"/> for details.</remarks>
        public int Subtarget;

        /// <summary>The subtarget the build was created for, as a string.</summary>
        /// <remarks>Valid subtarget values depend on the platform. See <see cref="Build.Reporting.BuildSummary.GetSubtarget{T}"/> for details.</remarks>
        public string SubtargetName;

        /// <summary>The total number of errors and exceptions recorded during the build process.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalErrors"/>.</remarks>
        public int TotalErrors;

        /// <summary>The total size of the build output, in bytes.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalSize"/>.</remarks>
        public long TotalSizeBytes;

        /// <summary>The total time taken by the build process, in milliseconds.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalTime"/>.</remarks>
        public long TotalTimeMs;

        /// <summary>The total number of warnings recorded during the build process.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalWarnings"/>.</remarks>
        public int TotalWarnings;

        /// <summary>Loads a <see cref="BuildReportSummary"/> from a JSON file at the specified path.</summary>
        /// <param name="jsonPath">The path to the JSON file to load.</param>
        /// <returns>The <see cref="BuildReportSummary"/> loaded from the file, or a default <see cref="BuildReportSummary"/> if the file is not found.</returns>
        public static BuildReportSummary Load(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"Failed to load {kBuildReportSummaryFileName}: File not found at {jsonPath}");
                return default;
            }

            var json = File.ReadAllText(jsonPath);
            return JsonUtility.FromJson<BuildReportSummary>(json);
        }

        internal static void Save(BuildReport report, string dirPath)
        {
            if (report == null)
            {
                Debug.LogWarning($"Failed to write {kBuildReportSummaryFileName}: BuildReport is null");
                return;
            }

            if (string.IsNullOrEmpty(dirPath))
            {
                Debug.LogWarning($"Failed to write {kBuildReportSummaryFileName}: Cannot determine build history directory. Build history may not be enabled.");
                return;
            }

            var outputPath = Path.Combine(dirPath, kBuildReportSummaryFileName);
            var json = ToJson(report);
            File.WriteAllText(outputPath, json);
        }

        internal static string ToJson(BuildReport report)
        {
            var summary = report.summary;

            var summaryData = new BuildReportSummary
            {
                Version = kVersion,
                BuildContentOptions = summary.buildContentOptions.ToString().Split(", ", StringSplitOptions.None),
                BuildManifestHash = summary.buildManifestHash.ToString(),
                BuildName = summary.buildName,
                BuildOptions = summary.options.ToString().Split(", ", StringSplitOptions.None),
                BuildProfilePath = summary.buildProfilePath,
                BuildResult = summary.result,
                BuildResultName = summary.result.ToString(),
                BuildSessionGUID = summary.buildSessionGuid,
                BuildStartedAt = summary.buildStartedAt.ToString("o", CultureInfo.InvariantCulture),
                BuildType = summary.buildType,
                BuildTypeName = summary.buildType.ToString(),
                OutputPath = summary.outputPath,
                Platform = summary.platform,
                PlatformName = summary.platform.ToString(),
                Subtarget = summary.subtarget,
                SubtargetName = summary.GetSubtargetString(),
                TotalErrors = summary.totalErrors,
                TotalSizeBytes = (long)summary.totalSize,
                TotalTimeMs = (long)summary.totalTime.TotalMilliseconds,
                TotalWarnings = summary.totalWarnings,
            };

            return JsonUtility.ToJson(summaryData, true);
        }
    }
}
