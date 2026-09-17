// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// How a graph load resolved, one value per distinct message the inspector shows. Every state
    /// is terminal for the build's current artifacts: generation is deterministic, so none of them
    /// is fixed by regenerating - the recoverable cases (a persist still in flight, a lost or torn
    /// file) are healed here, by rebuilding.
    /// </summary>
    internal enum DependencyGraphStatus
    {
        /// <summary>Graph read or rebuilt, validated, inverse pre-built. References is non-null.</summary>
        Loaded,

        /// <summary>
        /// The layout exists but no graph could be read or rebuilt from it - it does not parse, the
        /// build failed, or it belongs to a different build than the analysis. "Dependency data
        /// couldn't be read for this build." No advice: the failure is deterministic.
        /// </summary>
        Unreadable,

        /// <summary>
        /// ContentLayout.json is not in the build's metadata folder - the build never recorded
        /// dependency data. "This build didn't record dependency data."
        /// </summary>
        NoContentLayout,

        /// <summary>
        /// The graph was built from a ContentLayout shape this code cannot interpret -
        /// "builds made with an older Unity version".
        /// </summary>
        UnsupportedLayoutVersion,
    }

    internal sealed class DependencyGraphResult
    {
        public DependencyGraphStatus Status { get; }

        /// <summary>Non-null exactly when <see cref="Status"/> is <see cref="DependencyGraphStatus.Loaded"/>.</summary>
        public IAssetReferenceProvider References { get; }

        // Failure states carry no per-build data, so one shared instance each - the
        // AnalyzedBuild.Unavailable idiom, and the non-Loaded paths allocate nothing.
        [NoAutoStaticsCleanup]
        public static readonly DependencyGraphResult Unreadable =
            new DependencyGraphResult(DependencyGraphStatus.Unreadable, null);
        [NoAutoStaticsCleanup]
        public static readonly DependencyGraphResult NoContentLayout =
            new DependencyGraphResult(DependencyGraphStatus.NoContentLayout, null);
        [NoAutoStaticsCleanup]
        public static readonly DependencyGraphResult UnsupportedLayoutVersion =
            new DependencyGraphResult(DependencyGraphStatus.UnsupportedLayoutVersion, null);

        public static DependencyGraphResult FromGraph(DependencyGraph graph) =>
            new DependencyGraphResult(DependencyGraphStatus.Loaded, graph ?? throw new ArgumentNullException(nameof(graph)));

        private DependencyGraphResult(DependencyGraphStatus status, IAssetReferenceProvider references)
        {
            Status = status;
            References = references;
        }
    }

    // The tab view's seam, so UI tests control load completion. Deliberately just the read:
    // invalidation is the window's business (the Regenerate action), on the concrete service.
    internal interface IDependencyGraphService
    {
        /// <inheritdoc cref="DependencyGraphService.LoadAsync"/>
        Task<DependencyGraphResult> LoadAsync(BuildEntry entry, BuildAnalysis analysis);
    }

    /// <summary>
    /// Loads the dependency graph for a selected build, off the main thread, with a small cache.
    ///
    /// The artifact is a derived cache - generation is deterministic and byte-canonical - so a graph
    /// that cannot be read is rebuilt from ContentLayout.json rather than reported: a lost, torn or
    /// stale-schema file heals itself, at the cost of one layout parse in a state that shouldn't
    /// occur. That determinism is also what makes every result cacheable, including the failures.
    ///
    /// Deliberately separate from <see cref="BuildAnalysisService"/>: its cache is deep because
    /// analyses are small summaries, cheap to keep per build. The graph is the feature's largest
    /// per-build allocation, growing with build size, and only the inspected build's is ever
    /// queried - riding that cache would multiply the heaviest allocation by its depth, for builds
    /// nobody is looking at. So the graph gets its own shallow cache with its own lifetime.
    /// </summary>
    internal sealed class DependencyGraphService : IDependencyGraphService
    {
        static readonly ProfilerMarker s_RebuildMarker = new ProfilerMarker("DependencyGraphService.Rebuild");
        static readonly ProfilerMarker s_TransposeMarker = new ProfilerMarker("DependencyGraphService.Transpose");

        const int k_DefaultReadAttempts = 2;
        static readonly TimeSpan k_DefaultRetryDelay = TimeSpan.FromMilliseconds(250);

        readonly IDependencyGraphStore m_Store;
        readonly IBuildAnalysisFileSystem m_FileSystem;
        readonly int m_ReadAttempts;
        readonly TimeSpan m_RetryDelay;

        readonly LRUCache<GUID, DependencyGraphResult> m_Cache = new LRUCache<GUID, DependencyGraphResult>(2);
        readonly Dictionary<GUID, Task<DependencyGraphResult>> m_InFlight = new Dictionary<GUID, Task<DependencyGraphResult>>();
        readonly object m_InFlightLock = new object();

        public DependencyGraphService(IDependencyGraphStore store, IBuildAnalysisFileSystem fileSystem)
            : this(store, fileSystem, k_DefaultReadAttempts, k_DefaultRetryDelay)
        {
        }

        // Test seam: attempts and delay are injectable so retry tests run with TimeSpan.Zero
        // instead of real waiting. Two parameters, not a scheduler abstraction.
        internal DependencyGraphService(
            IDependencyGraphStore store,
            IBuildAnalysisFileSystem fileSystem,
            int readAttempts,
            TimeSpan retryDelay)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            if (readAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(readAttempts));
            m_ReadAttempts = readAttempts;
            m_RetryDelay = retryDelay;
        }

        /// <summary>
        /// The graph for one build, read off the main thread. Returns the cached result synchronously
        /// when available, joins an in-flight load when one exists, otherwise starts one. Takes the
        /// analysis as well as the entry because the graph is only meaningful against the analysis it
        /// pairs with - node ids are that analysis's Assets-table ids, and its manifest hash is what
        /// both the read and the rebuild are validated against.
        /// </summary>
        public Task<DependencyGraphResult> LoadAsync(BuildEntry entry, BuildAnalysis analysis)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            if (entry.BuildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(entry));

            var guid = entry.BuildSessionGUID;

            var cached = m_Cache.Get(guid);
            if (cached != null)
                return Task.FromResult(cached);

            lock (m_InFlightLock)
            {
                if (m_InFlight.TryGetValue(guid, out var inflight))
                    return inflight;
            }

            // An entry without a resolved metadata folder has nothing to read from. Not cached:
            // a refreshed build list can resolve it next time.
            if (string.IsNullOrEmpty(entry.FolderPath))
                return Task.FromResult(DependencyGraphResult.Unreadable);

            // Resolved before the task starts so its body touches no shared state.
            var layoutPath = Path.Combine(entry.FolderPath, BuildAnalysisConstants.k_ContentLayoutFileName);
            var graphPath = Path.Combine(entry.FolderPath, BuildAnalysisConstants.k_DependencyGraphRelativePath);
            var manifestHash = analysis.Summary.BuildManifestHash ?? string.Empty;
            var assets = analysis.Tables.Assets;

            return Register(guid, () => Task.Run(() => LoadCore(guid, layoutPath, graphPath, manifestHash, assets)));
        }

        /// <summary>
        /// Drop the cached result for one build. The regenerate action calls this so a freshly
        /// written graph is re-read rather than served stale.
        /// </summary>
        public void Invalidate(GUID buildSessionGUID) => m_Cache.Remove(buildSessionGUID);

        // Registers a freshly-started task as the in-flight entry for a build, and removes it on
        // completion. Same shape as BuildAnalysisService.Register.
        private Task<DependencyGraphResult> Register(GUID guid, Func<Task<DependencyGraphResult>> factory)
        {
            Task<DependencyGraphResult> task = null;

            async Task<DependencyGraphResult> Tracked()
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

        // Runs entirely on the thread pool. Every outcome is cached: the failures are deterministic
        // for the build's current artifacts and the transient cases - a persist still in flight,
        // a lost file - resolve to Loaded via the rebuild rather than surfacing at all.
        private async Task<DependencyGraphResult> LoadCore(
            GUID guid, string layoutPath, string graphPath, string manifestHash, BuildAnalysisAsset[] assets)
        {
            // Checked first, so builds that structurally never record dependency data - Player
            // builds ship no ContentLayout.json - resolve immediately.
            if (!m_FileSystem.Exists(layoutPath))
                return Cache(guid, DependencyGraphResult.NoContentLayout);

            DependencyGraph graph = null;
            for (var attempt = 1; attempt <= m_ReadAttempts; attempt++)
            {
                if (m_Store.TryRead(graphPath, manifestHash, out graph))
                    break;
                if (attempt < m_ReadAttempts)
                    await Task.Delay(m_RetryDelay);
            }

            // The artifact is a derived cache, so a miss is rebuilt, not reported. This is what
            // heals a persist that never landed (a crash between the two writes), a torn or
            // deleted file, and a stale-schema artifact deterministically.
            if (graph == null)
                graph = Rebuild(layoutPath, graphPath, manifestHash, assets);

            if (graph == null)
                return Cache(guid, DependencyGraphResult.Unreadable);

            if (graph.ContentLayoutVersion != BuildAnalysisConstants.k_SupportedContentLayoutVersion)
                return Cache(guid, DependencyGraphResult.UnsupportedLayoutVersion);

            // The transpose is not thread-safe, so it runs here, before the graph is published.
            // A Loaded result is immutable from this point and safe to share.
            using (s_TransposeMarker.Auto())
                graph.EnsureInverse();

            return Cache(guid, DependencyGraphResult.FromGraph(graph));
        }

        private DependencyGraphResult Cache(GUID guid, DependencyGraphResult result)
        {
            m_Cache.Put(guid, result);
            return result;
        }

        private DependencyGraph Rebuild(string layoutPath, string graphPath, string manifestHash, BuildAnalysisAsset[] assets)
        {
            using (s_RebuildMarker.Auto())
            {
                ContentLayout layout;
                try
                {
                    layout = ContentLayout.FromJson(m_FileSystem.ReadAllText(layoutPath));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to read or parse ContentLayout.json at '{layoutPath}': {e.Message}");
                    return null;
                }
                if (layout == null)
                    return null;

                // A layout from a different build must not be graphed against this analysis's ids.
                // The store enforces the same pairing for the on-disk artifact.
                if (!string.Equals(layout.BuildManifestHash ?? string.Empty, manifestHash, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} '{layoutPath}' belongs to a different build than the analysis; not rebuilding a graph from it.");
                    return null;
                }

                var graph = DependencyGraphBuilder.Build(
                    layout, BuildAnalysisAssembler.BuildPathToAssetId(assets), assets.Length, out _);
                if (graph == null)
                    return null;

                // Write-through, so the rebuild is one-time. Serving the in-memory graph matters
                // more than persisting it, so a failed write only warns.
                try
                {
                    m_Store.Write(graphPath, graph);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Rebuilt the dependency graph but failed to persist it to '{graphPath}': {e.Message}");
                }

                return graph;
            }
        }
    }
}
