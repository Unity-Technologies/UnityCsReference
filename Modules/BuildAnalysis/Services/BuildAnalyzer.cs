// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildAnalyzer
    {
        Task<BuildAnalysis> GenerateAsync(BuildEntry entry, CancellationToken ct);
    }

    internal sealed class BuildAnalyzer : IBuildAnalyzer
    {
        static readonly ProfilerMarker s_GenerateMarker = new ProfilerMarker("BuildAnalyzer.Generate");
        static readonly ProfilerMarker s_LoadBuildReportMarker = new ProfilerMarker("BuildAnalyzer.LoadBuildReport");
        static readonly ProfilerMarker s_ParseContentLayoutMarker = new ProfilerMarker("BuildAnalyzer.ParseContentLayout");
        static readonly ProfilerMarker s_AssembleMarker = new ProfilerMarker("BuildAnalyzer.Assemble");
        static readonly ProfilerMarker s_SerializeMarker = new ProfilerMarker("BuildAnalyzer.Serialize");
        static readonly ProfilerMarker s_WriteMarker = new ProfilerMarker("BuildAnalyzer.Write");

        private readonly IBuildReportConverter m_BuildReportConverter;
        private readonly IBuildAnalysisFileSystem m_FileSystem;
        private readonly IBuildHistoryProvider m_BuildHistory;
        private readonly ISourceBuildAssetResolver m_AssetResolver;

        public BuildAnalyzer(
            IBuildReportConverter buildReportConverter,
            IBuildAnalysisFileSystem fileSystem,
            IBuildHistoryProvider buildHistory,
            ISourceBuildAssetResolver assetResolver)
        {
            m_BuildReportConverter = buildReportConverter ?? throw new ArgumentNullException(nameof(buildReportConverter));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_BuildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
            m_AssetResolver = assetResolver ?? throw new ArgumentNullException(nameof(assetResolver));
        }

        private readonly struct GatheredInputs
        {
            public readonly BuildReportSummary ReportSummary;
            public readonly BuildReportData ReportData;
            public readonly string MetadataPath;
            public readonly SourceBuildAssets? SourceBuildAssets;

            public GatheredInputs(BuildReportSummary reportSummary, BuildReportData reportData, string metadataPath, SourceBuildAssets? sourceBuildAssets)
            {
                ReportSummary = reportSummary;
                ReportData = reportData;
                MetadataPath = metadataPath;
                SourceBuildAssets = sourceBuildAssets;
            }
        }

        /// <summary>
        /// Synchronous composition of the same three stages as <see cref="GenerateAsync"/>. Not on
        /// <see cref="IBuildAnalyzer"/> and not called in production (the UI uses <see cref="GenerateAsync"/>);
        /// it exists as the deterministic test seam for the full pipeline.
        /// </summary>
        public BuildAnalysis Generate(BuildEntry entry)
        {
            using (s_GenerateMarker.Auto())
            {
                var inputs = GatherMainThreadInputs(entry);
                var analysis = AssembleAnalysis(inputs);
                PersistAnalysis(analysis, inputs.MetadataPath);
                return analysis;
            }
        }

        /// <summary>
        /// Async generation: native build-report access stays on the main thread (pre-await); the heavy
        /// pure-managed work (ContentLayout parse + root-asset BFS + assembly) runs on a background thread;
        /// the disk cache is written fire-and-forget so the UI never waits on serialization.
        /// </summary>
        public async Task<BuildAnalysis> GenerateAsync(BuildEntry entry, CancellationToken ct)
        {
            // Already torn down before we started: skip the native gather entirely.
            ct.ThrowIfCancellationRequested();

            // Main thread (pre-await): native BuildReport load + convert + AssetDatabase importer lookup.
            var inputs = GatherMainThreadInputs(entry);

            // Off the main thread: all pure managed transform.
            var analysis = await Task.Run(() => AssembleAnalysis(inputs), ct);

            // Background, fire-and-forget: persisting the cache is not on the time-to-interactive path and is
            // intentionally not tied to ct. A build the user navigated away from is still worth caching.
            var metadataPath = inputs.MetadataPath;
            var guid = entry.BuildSessionGUID;
            _ = Task.Run(() =>
            {
                try
                {
                    PersistAnalysis(analysis, metadataPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to persist analysis for '{guid}': {e.Message}");
                }
            });

            return analysis;
        }

        private GatheredInputs GatherMainThreadInputs(BuildEntry entry)
        {
            ValidateEntry(entry);

            var reportSummary = m_BuildHistory.GetBuildSummary(entry.BuildSessionGUID);

            BuildReport buildReport;
            using (s_LoadBuildReportMarker.Auto())
            {
                if (!m_BuildHistory.TryLoadBuildReport(entry.BuildSessionGUID, out buildReport))
                    throw new InvalidDataException($"Missing build report for build '{entry.BuildSessionGUID}'.");
            }
            var reportData = m_BuildReportConverter.Convert(buildReport);

            // A Player build that recorded no assets (scripts-only, or an incremental build that reused its data
            // cache) borrows the asset table from the exact source build the pipeline recorded on this build's
            // summary. The resolver limits itself to Player builds, so no build-type check is needed here.
            SourceBuildAssets? sourceBuildAssets = null;
            if (reportData.Assets.Length == 0 && m_AssetResolver.TryResolveSourceBuildAssets(reportSummary, out var resolved))
                sourceBuildAssets = resolved;

            if (!m_BuildHistory.TryGetBuildReportDirectory(entry.BuildSessionGUID, out var metadataPath))
                throw new InvalidDataException($"No build report directory available for build '{entry.BuildSessionGUID}'.");

            return new GatheredInputs(reportSummary, reportData, metadataPath, sourceBuildAssets);
        }

        private BuildAnalysis AssembleAnalysis(GatheredInputs inputs)
        {
            var rootStats = inputs.ReportSummary.BuildType == BuildType.ContentDirectory
                ? LoadRootAssetStats(inputs.MetadataPath)
                : Array.Empty<RootAssetStats>();

            using (s_AssembleMarker.Auto())
                return BuildAnalysisAssembler.Assemble(inputs.ReportSummary, inputs.ReportData, rootStats, inputs.SourceBuildAssets);
        }

        private void PersistAnalysis(BuildAnalysis analysis, string metadataPath)
        {
            var analysisPath = Path.Combine(metadataPath, BuildAnalysisConstants.k_BuildAnalysisRelativePath);
            string json;
            using (s_SerializeMarker.Auto())
                json = JsonUtility.ToJson(analysis, false);
            using (s_WriteMarker.Auto())
                m_FileSystem.WriteAllText(analysisPath, json);
        }

        private RootAssetStats[] LoadRootAssetStats(string metadataPath)
        {
            var contentLayoutPath = Path.Combine(metadataPath, BuildAnalysisConstants.k_ContentLayoutFileName);
            if (!m_FileSystem.Exists(contentLayoutPath))
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} ContentLayout.json not found at '{contentLayoutPath}'. RootAssets will be empty.");
                return Array.Empty<RootAssetStats>();
            }

            try
            {
                // FromJson is preferred over ContentLayout.Load so all I/O stays behind
                // IBuildAnalysisFileSystem (testable). FromJson still emits the version-mismatch warning.
                ContentLayout layout;
                using (s_ParseContentLayoutMarker.Auto())
                    layout = ContentLayout.FromJson(m_FileSystem.ReadAllText(contentLayoutPath));
                if (layout == null)
                    return Array.Empty<RootAssetStats>();
                return RootAssetStatsCalculator.Calculate(layout);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to read or parse ContentLayout.json at '{contentLayoutPath}': {e.Message}");
                return Array.Empty<RootAssetStats>();
            }
        }

        private static void ValidateEntry(BuildEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (entry.BuildSessionGUID.Empty())
                throw new InvalidDataException("BuildSessionGUID is required to generate BuildAnalysis.");
        }
    }
}
