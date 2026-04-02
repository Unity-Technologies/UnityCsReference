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

        /// <summary>The schema version of the <see cref="BuildReportSummary"/> JSON format.</summary>
        public int Version;

        /// <summary>A unique identifier for the build session in the Unity Editor.</summary>
        /// <remarks>This identifier uniquely identifies each build session, regardless of whether the build produces identical output.
        /// Failed or cancelled builds will also have a unique session GUID.
        /// This identifier is used in the <see cref="BuildHistory"/> API as the main key to look up additional build information.
        ///
        /// This identifier is not stored in the built output.
        /// </remarks>
        public GUID BuildSessionGUID;

        ///<summary>For ContentDirectory builds, the build name. For Player builds, the product name from PlayerSettings.</summary>
        public string BuildName;

        /// <summary>The time the build was started, as a UTC timestamp in ISO 8601 round-trip format.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildStartedAt"/>.</remarks>
        public string BuildStartedAt;

        /// <summary>The type of build, as the string form of the <see cref="Build.Reporting.BuildType"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildType"/>.</remarks>
        public string BuildTypeName;

        /// <summary>The type of build.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.buildType"/>.</remarks>
        public BuildType BuildType;

        /// <summary>For ContentDirectory builds, the Hash128 of the build manifest as a string. For other build types, this is a default Hash128 string.</summary>
        public string BuildManifestHash;

        // Platform info
        /// <summary>The platform the build was created for, as the string form of the <see cref="BuildTarget"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.platform"/>.</remarks>
        public string PlatformName;

        /// <summary>The platform the build was created for.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.platform"/>.</remarks>
        public BuildTarget Platform;

        /// <summary>The subtarget the build was created for, as a string.</summary>
        /// <remarks>Valid subtarget values depend on the platform. See <see cref="Build.Reporting.BuildSummary.GetSubtarget{T}"/> for details.</remarks>
        public string SubtargetName;

        /// <summary>The subtarget the build was created for, as an integer.</summary>
        /// <remarks>The integer maps to a platform-specific subtarget enum. For standalone platforms this is <see cref="StandaloneBuildSubtarget"/>.
        /// Valid subtarget values depend on the platform. See <see cref="Build.Reporting.BuildSummary.GetSubtarget{T}"/> for details.</remarks>
        public int Subtarget;

        // Build result
        /// <summary>The outcome of the build, as the string form of the <see cref="Build.Reporting.BuildResult"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.result"/>.</remarks>
        public string BuildResultName;

        /// <summary>The outcome of the build.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.result"/>.</remarks>
        public BuildResult BuildResult;

        /// <summary>The total number of errors and exceptions recorded during the build process.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalErrors"/>.</remarks>
        public int TotalErrors;

        /// <summary>The total number of warnings recorded during the build process.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalWarnings"/>.</remarks>
        public int TotalWarnings;

        /// <summary>The total time taken by the build process, in milliseconds.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalTime"/>.</remarks>
        public long TotalTimeMs;

        /// <summary>For Player builds, the build options, as an array of string values from the <see cref="UnityEditor.BuildOptions"/> enum.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.options"/>.</remarks>
        public string[] BuildOptions;

        /// <summary>For ContentDirectory builds, the content build options, as an array of string values from the <see cref="UnityEditor.BuildContentOptions"/> enum.</summary>
        public string[] BuildContentOptions;

        /// <summary>The output path for the build.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.outputPath"/>.</remarks>
        public string OutputPath;

        /// <summary>The total size of the build output, in bytes.</summary>
        /// <remarks>This value corresponds to <see cref="Build.Reporting.BuildSummary.totalSize"/>.</remarks>
        public long TotalSizeBytes;

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

            var buildStartedAtUtc = DateTime.SpecifyKind(summary.buildStartedAt, DateTimeKind.Utc);

            var summaryData = new BuildReportSummary
            {
                Version = kVersion,

                BuildSessionGUID = summary.buildSessionGuid,
                BuildName = summary.buildName,
                BuildStartedAt = buildStartedAtUtc.ToString("o", CultureInfo.InvariantCulture),
                BuildTypeName = summary.buildType.ToString(),
                BuildType = summary.buildType,
                BuildManifestHash = summary.buildManifestHash.ToString(),

                PlatformName = summary.platform.ToString(),
                Platform = summary.platform,
                SubtargetName = summary.GetSubtargetString(),
                Subtarget = summary.subtarget,

                BuildResultName = summary.result.ToString(),
                BuildResult = summary.result,
                TotalErrors = summary.totalErrors,
                TotalWarnings = summary.totalWarnings,
                TotalTimeMs = (long)summary.totalTime.TotalMilliseconds,

                BuildOptions = summary.options.ToString().Split(", ", StringSplitOptions.None),
                BuildContentOptions = summary.buildContentOptions.ToString().Split(", ", StringSplitOptions.None),

                OutputPath = summary.outputPath,
                TotalSizeBytes = (long)summary.totalSize,
            };

            return JsonUtility.ToJson(summaryData, true);
        }
    }
}
