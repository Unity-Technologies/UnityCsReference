// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Interface for enumerating builds
    /// </summary>
    internal interface IBuildEnumerator
    {
        BuildEntry[] GetBuilds();
        bool TryGetBuild(GUID buildSessionGUID, out BuildEntry entry);
    }

    /// <summary>
    /// Service for discovering and enumerating build history
    /// </summary>
    internal class BuildEnumerator : IBuildEnumerator
    {
        private const int k_ExpectedSummaryVersion = 2;

        private readonly IBuildHistoryProvider m_BuildHistory;

        public BuildEnumerator(IBuildHistoryProvider buildHistory)
        {
            m_BuildHistory = buildHistory;
        }

        /// <summary>
        /// Get all available builds, sorted by date descending (most-recent-first)
        /// </summary>
        public BuildEntry[] GetBuilds()
        {
            var guids = m_BuildHistory.GetAllBuilds();
            var builds = new List<BuildEntry>(guids.Length);
            foreach (var guid in guids)
            {
                var summary = m_BuildHistory.GetBuildSummary(guid);
                builds.Add(BuildEntryFromSummary(summary, guid));
            }
            return builds.ToArray();
        }

        /// <summary>
        /// Try to get a specific build by session GUID
        /// </summary>
        public bool TryGetBuild(GUID buildSessionGUID, out BuildEntry entry)
        {
            entry = null;

            if (buildSessionGUID.Empty())
                return false;

            try
            {
                var summary = m_BuildHistory.GetBuildSummary(buildSessionGUID);
                entry = BuildEntryFromSummary(summary, buildSessionGUID);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private BuildEntry BuildEntryFromSummary(BuildReportSummary summary, GUID guid)
        {
            if (summary.Version != k_ExpectedSummaryVersion)
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Build summary version mismatch for '{guid}': expected {k_ExpectedSummaryVersion}, got {summary.Version}. Some build data may be incorrect.");

            string folderPath = string.Empty;
            if (m_BuildHistory.TryGetFilePath(guid, BuildAnalysisConstants.k_BuildReportSummaryFileName, out var fp))
                folderPath = Path.GetDirectoryName(fp);

            return new BuildEntry
            {
                BuildSessionGUID = guid,
                BuildName = summary.BuildName ?? string.Empty,
                BuildStartedAt = ParseBuildStartedAtLocal(summary.BuildStartedAt),
                Platform = summary.Platform,
                BuildResult = summary.BuildResult,
                BuildType = summary.BuildType,
                TotalSizeBytes = summary.TotalSizeBytes,
                TotalTimeMs = summary.TotalTimeMs,
                FolderPath = folderPath,
            };
        }

        private static DateTime ParseBuildStartedAtLocal(string buildStartedAt)
        {
            if (DateTimeOffset.TryParseExact(buildStartedAt, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDateTime))
                return parsedDateTime.ToLocalTime().DateTime;

            throw new FormatException($"Invalid BuildStartedAt value: '{buildStartedAt}'.");
        }
    }
}
