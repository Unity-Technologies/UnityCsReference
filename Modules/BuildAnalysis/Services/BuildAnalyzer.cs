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
        private readonly IDependencyGraphStore m_GraphStore;

        public BuildAnalyzer(
            IBuildReportConverter buildReportConverter,
            IBuildAnalysisFileSystem fileSystem,
            IBuildHistoryProvider buildHistory,
            ISourceBuildAssetResolver assetResolver,
            IDependencyGraphStore graphStore)
        {
            m_BuildReportConverter = buildReportConverter ?? throw new ArgumentNullException(nameof(buildReportConverter));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_BuildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
            m_AssetResolver = assetResolver ?? throw new ArgumentNullException(nameof(assetResolver));
            m_GraphStore = graphStore ?? throw new ArgumentNullException(nameof(graphStore));
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
        /// What one generation produces: the analysis, plus the dependency graph for builds that have a
        /// content layout to build one from. The graph deliberately does not travel back to
        /// <see cref="BuildAnalysisService"/> - it is written to disk and dropped, so the analysis cache
        /// can never pin a graph per cached build, which would dominate its memory on a large project.
        /// </summary>
        private readonly struct GeneratedArtifacts
        {
            public readonly BuildAnalysis Analysis;
            public readonly DependencyGraph Graph;

            public GeneratedArtifacts(BuildAnalysis analysis, DependencyGraph graph)
            {
                Analysis = analysis;
                Graph = graph;
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
                var artifacts = AssembleAnalysis(inputs);
                PersistAnalysis(artifacts.Analysis, inputs.MetadataPath);
                PersistGraph(artifacts.Graph, inputs.MetadataPath);
                return artifacts.Analysis;
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
            var artifacts = await Task.Run(() => AssembleAnalysis(inputs), ct);

            // Background, fire-and-forget: persisting the cache is not on the time-to-interactive path and is
            // intentionally not tied to ct. A build the user navigated away from is still worth caching.
            // The two artifacts are guarded separately so a graph that fails to write cannot cost the analysis.
            var metadataPath = inputs.MetadataPath;
            var guid = entry.BuildSessionGUID;
            _ = Task.Run(() =>
            {
                try
                {
                    PersistAnalysis(artifacts.Analysis, metadataPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to persist analysis for '{guid}': {e.Message}");
                }

                try
                {
                    PersistGraph(artifacts.Graph, metadataPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to persist the dependency graph for '{guid}': {e.Message}");
                }
            });

            return artifacts.Analysis;
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

        private GeneratedArtifacts AssembleAnalysis(GatheredInputs inputs)
        {
            // Player builds ship no ContentLayout.json, so they have neither root assets nor a graph.
            var layout = inputs.ReportSummary.BuildType == BuildType.ContentDirectory
                ? LoadContentLayout(inputs.MetadataPath)
                : null;

            var rootStats = layout != null
                ? RootAssetStatsCalculator.Calculate(layout)
                : Array.Empty<RootAssetStats>();

            BuildAnalysis analysis;
            using (s_AssembleMarker.Auto())
                analysis = BuildAnalysisAssembler.Assemble(inputs.ReportSummary, inputs.ReportData, rootStats, inputs.SourceBuildAssets);

            // The graph is built after Assemble rather than beside the root-asset walk: its nodes are
            // Assets-table ids, and that table doesn't exist until the analysis is assembled. What both
            // consumers share is the parse above, which is the ~700 ms part.
            return new GeneratedArtifacts(analysis, layout == null ? null : BuildGraph(layout, analysis));
        }

        private static DependencyGraph BuildGraph(ContentLayout layout, BuildAnalysis analysis)
        {
            // A layout of another schema version parses into these classes with defaulted fields
            // (every v2 file reads as ArtifactIndex 0), so building from it is not just wasted work
            // that the reader would refuse - it walks garbage indices. The analysis itself still
            // degrades softly above; only the graph is withheld.
            if (layout.Version != BuildAnalysisConstants.k_SupportedContentLayoutVersion)
                return null;

            var assets = analysis.Tables.Assets;
            var graph = DependencyGraphBuilder.Build(
                layout,
                BuildAnalysisAssembler.BuildPathToAssetId(assets),
                assets.Length,
                out var stats);

            // A populated build that yields no edges at all is the shape an unreadable layout takes: the
            // nodes come from SourceAssets and survive, so only the edges vanish, and every asset then
            // reports no references - which reads as data rather than as a failure. When nothing mapped
            // onto the Assets table the blame lies elsewhere, so stay quiet rather than misattribute.
            var serializedFileCount = layout.SerializedFiles?.Length ?? 0;
            if (graph != null && graph.EdgeCount == 0 && serializedFileCount > 1
                && stats.UnmappedSerializedFiles == 0)
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} " +
                                 $"No dependency edges were found across {serializedFileCount} SerializedFiles " +
                                 $"(ContentLayout version {layout.Version}). This may indicate a ContentLayout " +
                                 "format this version cannot read; dependency data will be empty for this build.");
            }

            return graph;
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

        private void PersistGraph(DependencyGraph graph, string metadataPath)
        {
            var graphPath = Path.Combine(metadataPath, BuildAnalysisConstants.k_DependencyGraphRelativePath);
            if (graph == null)
            {
                // The graph and the analysis are a generated pair. A generation that produced no graph
                // must not leave an earlier one beside the fresh analysis, where it would read as current.
                m_GraphStore.Delete(graphPath);
                return;
            }
            m_GraphStore.Write(graphPath, graph);
        }

        /// <summary>
        /// Parses the build's ContentLayout.json once, for every consumer that needs it. Returns null
        /// when there isn't one to read, or it can't be parsed - both already degrade to an empty
        /// RootAssets table, and now also to no dependency graph.
        /// </summary>
        private ContentLayout LoadContentLayout(string metadataPath)
        {
            var contentLayoutPath = Path.Combine(metadataPath, BuildAnalysisConstants.k_ContentLayoutFileName);
            if (!m_FileSystem.Exists(contentLayoutPath))
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} ContentLayout.json not found at '{contentLayoutPath}'. RootAssets will be empty.");
                return null;
            }

            try
            {
                // FromJson is preferred over ContentLayout.Load so all I/O stays behind
                // IBuildAnalysisFileSystem (testable). FromJson still emits the version-mismatch warning.
                using (s_ParseContentLayoutMarker.Auto())
                    return ContentLayout.FromJson(m_FileSystem.ReadAllText(contentLayoutPath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to read or parse ContentLayout.json at '{contentLayoutPath}': {e.Message}");
                return null;
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
