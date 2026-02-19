// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build
{
    [Serializable]
    internal struct BuildReportSummary
    {
        internal const string kBuildReportSummaryFileName = "BuildReportSummary.json";
        internal const int kVersion = 1;

        public int Version;

        // Build identity
        public string BuildGUID;
        public string BuildSessionGUID;
        public string BuildName;
        public string BuildStartedAt;
        public string BuildType;
        public string BuildManifestHash;

        // Platform info
        public string Platform;
        public string Subtarget;

        // Build result
        public string BuildResult;
        public int TotalErrors;
        public int TotalWarnings;
        public long TotalTimeMs;

        // Build options
        public string[] BuildOptions;
        public string[] BuildContentOptions;

        // Output info
        public string OutputPath;
        public long TotalSizeBytes;
        public int FileCount;

        public static BuildReportSummary LoadJson(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"Failed to load {kBuildReportSummaryFileName}: File not found at {jsonPath}");
                return default;
            }

            var json = File.ReadAllText(jsonPath);
            return JsonUtility.FromJson<BuildReportSummary>(json);
        }

        public static void WriteJson(BuildReport report)
        {
            if (report == null)
            {
                Debug.LogWarning($"Failed to write {kBuildReportSummaryFileName}: BuildReport is null");
                return;
            }

            var summary = report.summary;

            string dirPath = BuildHistory.GetDirectory(summary.outputPath);
            if (string.IsNullOrEmpty(dirPath))
            {
                Debug.LogWarning($"Failed to write {kBuildReportSummaryFileName}: Cannot determine history directory for build output: {summary.outputPath}");
                return;
            }

            var summaryData = new BuildReportSummary
            {
                Version = kVersion,

                BuildGUID = summary.guid.ToString(),
                BuildSessionGUID = summary.buildSessionGuid.ToString(),
                BuildName = summary.buildName,
                BuildStartedAt = summary.buildStartedAt.ToString("s"),
                BuildType = GetBuildTypeString(summary.buildType),
                BuildManifestHash = summary.buildManifestHash.ToString(),

                Platform = summary.platform.ToString(),
                Subtarget = summary.GetSubtargetString(),

                BuildResult = summary.result.ToString(),
                TotalErrors = summary.totalErrors,
                TotalWarnings = summary.totalWarnings,
                TotalTimeMs = (long)summary.totalTime.TotalMilliseconds,

                BuildOptions = summary.options.ToString().Split(", ", StringSplitOptions.None),
                BuildContentOptions = summary.buildContentOptions.ToString().Split(", ", StringSplitOptions.None),

                OutputPath = summary.outputPath,
                TotalSizeBytes = (long)summary.totalSize,
                FileCount = report.GetFiles().Length,
            };

            var outputPath = Path.Combine(dirPath, kBuildReportSummaryFileName);
            var json = JsonUtility.ToJson(summaryData, true);
            File.WriteAllText(outputPath, json);
        }

        /* UCBP-INTERNAL */
        private static string GetBuildTypeString(BuildType buildType)
        {
            // ContentDirectory = 3 is not in the public BuildType enum yet (commented out in BuildType.cs)
            // but the C++ code sets it to 3 for ContentDirectory builds
            if ((int)buildType == 3)
                return "ContentDirectory";

            return buildType.ToString();
        }
    }
}
