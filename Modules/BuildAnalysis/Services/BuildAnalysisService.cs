// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Main service for build analysis functionality
    /// </summary>
    internal class BuildAnalysisService : IDisposable
    {
        static readonly ProfilerMarker s_LoadFromDiskMarker = new ProfilerMarker("BuildAnalysisService.LoadFromDisk");

        private readonly IBuildEnumerator m_Enumerator;
        private readonly IBuildAnalyzer m_Analyzer;
        private readonly IBuildAnalysisFileSystem m_FileSystem;
        private readonly IBuildHistoryProvider m_BuildHistory;
        private readonly IBuildLogReader m_LogReader;
        private readonly LRUCache<GUID, AnalyzedBuild> m_Cache;

        // In-flight de-duplication: a second request for a build that is already loading/generating awaits
        // the same Task instead of recomputing.
        private readonly Dictionary<GUID, Task<AnalyzedBuild>> m_InFlight = new Dictionary<GUID, Task<AnalyzedBuild>>();
        private readonly object m_InFlightLock = new object();

        // Cancels all in-flight work on teardown (window close / domain reload). The window builds a fresh
        // service per OnEnable, so each session gets a fresh token (no reuse-after-cancel).
        private readonly CancellationTokenSource m_Cts = new CancellationTokenSource();
        private bool m_Disposed;

        public BuildAnalysisService(
            IBuildEnumerator enumerator,
            IBuildAnalyzer analyzer,
            IBuildAnalysisFileSystem fileSystem,
            IBuildHistoryProvider buildHistory,
            IBuildLogReader logReader)
        {
            m_Enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            m_Analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_BuildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
            m_LogReader = logReader ?? throw new ArgumentNullException(nameof(logReader));
            m_Cache = new LRUCache<GUID, AnalyzedBuild>(20);
        }

        /// <summary>
        /// Cancel any in-flight analysis without disposing (e.g. before a domain reload, where OnEnable
        /// rebuilds the service afterwards).
        /// </summary>
        public void CancelPending()
        {
            if (m_Disposed)
                return;
            m_Cts.Cancel();
        }

        /// <summary>
        /// Cancels in-flight analysis and releases the cancellation source. Call on window teardown.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Cts.Cancel();
            m_Cts.Dispose();
        }

        /// <summary>
        /// Refresh the build history state from disk, then clear the cache.
        /// Call this before GetBuilds when an explicit user-initiated refresh is needed.
        /// </summary>
        public void Refresh()
        {
            m_BuildHistory.Refresh();
            ClearCache();
        }

        /// <summary>
        /// Get all available builds
        /// </summary>
        public BuildEntry[] GetBuilds()
        {
            try
            {
                return m_Enumerator.GetBuilds();
            }
            catch (Exception e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to get builds: {e.Message}");
                return Array.Empty<BuildEntry>();
            }
        }

        /// <summary>
        /// Try to get a specific build by session GUID
        /// </summary>
        public bool TryGetBuild(GUID buildSessionGUID, out BuildEntry entry)
        {
            return m_Enumerator.TryGetBuild(buildSessionGUID, out entry);
        }

        /// <summary>
        /// The raw <see cref="BuildReportSummary"/> for a build, or false when it is not tracked.
        /// </summary>
        public bool TryGetBuildSummary(GUID buildSessionGUID, out BuildReportSummary summary)
        {
            summary = default;
            if (buildSessionGUID.Empty())
                return false;

            try
            {
                summary = m_BuildHistory.GetBuildSummary(buildSessionGUID);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether this build can be shown in the default report view.
        /// </summary>
        public bool CanDrawDefaultReport(GUID buildSessionGUID)
        {
            return HasBuildReport(buildSessionGUID)
                || TryGetBuildAnalysisPath(buildSessionGUID, out _);
        }

        /// <summary>
        /// Whether a build report is on disk for this build.
        /// </summary>
        public bool HasBuildReport(GUID buildSessionGUID)
        {
            return m_BuildHistory.HasBuildReport(buildSessionGUID);
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void ClearCache()
        {
            m_Cache.Clear();
        }

        /// <summary>
        /// Delete a single build by its session GUID.
        /// </summary>
        public void DeleteBuild(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(buildSessionGUID));

            var deleted = m_BuildHistory.DeleteHistory(new[] { buildSessionGUID });
            if (deleted == 0)
                throw new ArgumentException($"No build found for '{buildSessionGUID}'.", nameof(buildSessionGUID));

            m_Cache.Remove(buildSessionGUID);
        }

        /// <summary>
        /// Delete all builds from build history.
        /// </summary>
        public void DeleteAllBuilds()
        {
            m_BuildHistory.DeleteAllHistory();
            ClearCache();
        }

        /// <summary>
        /// Get analysis for a specific build, off the main thread. Returns the cached instance synchronously
        /// when available, joins an in-flight load/generation when one exists, otherwise starts one.
        /// </summary>
        public Task<AnalyzedBuild> GetBuildAnalysisAsync(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(buildSessionGUID));

            var cached = m_Cache.Get(buildSessionGUID);
            if (cached != null)
                return Task.FromResult(cached);

            lock (m_InFlightLock)
            {
                if (m_InFlight.TryGetValue(buildSessionGUID, out var inflight))
                    return inflight;
            }

            if (!m_Enumerator.TryGetBuild(buildSessionGUID, out var entry))
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} No build found for BuildSessionGUID '{buildSessionGUID}'.");
                return Task.FromResult(AnalyzedBuild.Unavailable);
            }

            if (!CanDrawDefaultReport(buildSessionGUID))
                return Task.FromResult(AnalyzedBuild.Unavailable);

            return Register(buildSessionGUID, () => LoadOrGenerateAsync(buildSessionGUID, entry, regenerate: false));
        }

        /// <summary>
        /// Force regeneration of BuildAnalysis.json for the given build and update the cache. Bypasses both the
        /// memory cache and the on-disk file, and supersedes any in-flight load for the same build.
        /// </summary>
        public Task<AnalyzedBuild> RegenerateBuildAnalysisAsync(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(buildSessionGUID));

            if (!m_Enumerator.TryGetBuild(buildSessionGUID, out var entry))
                throw new ArgumentException($"No build found for BuildSessionGUID '{buildSessionGUID}'.", nameof(buildSessionGUID));

            if (!HasBuildReport(buildSessionGUID))
                return Task.FromResult(AnalyzedBuild.Unavailable);

            // Invalidate first so a concurrent GetBuildAnalysisAsync can't serve the stale entry we're replacing.
            m_Cache.Remove(buildSessionGUID);
            return Register(buildSessionGUID, () => LoadOrGenerateAsync(buildSessionGUID, entry, regenerate: true));
        }

        /// <summary>
        /// Check if analysis is available for a build
        /// </summary>
        public bool HasBuildAnalysis(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                return false;

            if (m_Cache.Contains(buildSessionGUID))
                return true;
            if (!m_Enumerator.TryGetBuild(buildSessionGUID, out _))
                return false;

            return TryGetBuildAnalysisPath(buildSessionGUID, out _);
        }

        // Registers a freshly-started task as the in-flight entry for a build, and removes it on completion
        private Task<AnalyzedBuild> Register(GUID guid, Func<Task<AnalyzedBuild>> factory)
        {
            Task<AnalyzedBuild> task = null;

            async Task<AnalyzedBuild> Tracked()
            {
                try
                {
                    return await factory();
                }
                finally
                {
                    lock (m_InFlightLock)
                    {
                        if (m_InFlight.TryGetValue(guid, out var current) && ReferenceEquals(current, task))
                            m_InFlight.Remove(guid);
                    }
                }
            }

            task = Tracked();
            if (!task.IsCompleted)
            {
                lock (m_InFlightLock)
                    m_InFlight[guid] = task;
            }
            return task;
        }

        private async Task<AnalyzedBuild> LoadOrGenerateAsync(GUID guid, BuildEntry entry, bool regenerate)
        {
            try
            {
                // BuildHistory path lookups read editor settings and the native root directory, so they
                // are main-thread only. Resolve before any await; the off-thread read takes only a path.
                var logPath = m_BuildHistory.TryGetFilePath(guid, BuildAnalysisConstants.k_BuildLogFileName, out var resolved)
                    ? resolved
                    : null;

                // Sequential rather than overlapped. Running the two together measured slower, not faster:
                // both passes allocate heavily and a collection suspends every managed thread, so they stall each other.
                var analysis = await GetAnalysisAsync(guid, entry, regenerate);
                var messages = await Task.Run(() => m_LogReader.Read(logPath), m_Cts.Token);
                if (messages.Status == BuildLogStatus.FileMissing)
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} " +
                                     $"{BuildAnalysisConstants.k_BuildLogFileName} not found for build '{guid}'. " +
                                     "Messages will be empty.");
                }

                var result = new AnalyzedBuild(analysis, messages);
                m_Cache.Put(guid, result);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to get analysis for '{guid}': {e.Message}");
                return AnalyzedBuild.Unavailable;
            }
        }

        private async Task<BuildAnalysis> GetAnalysisAsync(GUID guid, BuildEntry entry, bool regenerate)
        {
            if (!regenerate && TryGetBuildAnalysisPath(guid, out var analysisPath))
            {
                var cached = await Task.Run(() => LoadBuildAnalysisFromDisk(analysisPath), m_Cts.Token);

                // Only serve a cache written by the current schema, otherwise regenerate from the source
                // BuildReport rather than serving stale data. Keeping this a simple version compare makes
                // every future schema bump self-healing. If the source report has since been pruned,
                // GenerateAsync throws and the caller degrades to Unavailable, which is acceptable for
                // that rare stale-cache-without-report case.
                if (cached.Version == BuildAnalysisConstants.k_SchemaVersion)
                    return cached;
            }

            return await m_Analyzer.GenerateAsync(entry, m_Cts.Token);
        }

        private bool TryGetBuildAnalysisPath(GUID buildSessionGUID, out string path)
        {
            return m_BuildHistory.TryGetFilePath(buildSessionGUID, BuildAnalysisConstants.k_BuildAnalysisRelativePath, out path);
        }

        private BuildAnalysis LoadBuildAnalysisFromDisk(string analysisPath)
        {
            using (s_LoadFromDiskMarker.Auto())
            {
                var json = m_FileSystem.ReadAllText(analysisPath);
                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidDataException($"Build analysis file is empty: '{analysisPath}'.");

                var analysis = JsonUtility.FromJson<BuildAnalysis>(json);
                ValidateBuildAnalysis(analysis, analysisPath);
                return analysis;
            }
        }

        private static void ValidateBuildAnalysis(BuildAnalysis analysis, string path)
        {
            if (analysis == null)
                throw new InvalidDataException($"Build analysis could not be parsed: '{path}'.");
            if (analysis.Summary == null)
                throw new InvalidDataException($"Build analysis summary is missing: '{path}'.");
            if (analysis.Version <= 0)
                throw new InvalidDataException($"Build analysis has invalid Version '{analysis.Version}' in '{path}'.");
        }
    }
}
