// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// What a graph build had to discard. Every counter should be zero or negligible; they exist
    /// because a partial graph looks exactly like a build with few dependencies. Not logged at
    /// generation - a nonzero <see cref="UnmappedSerializedFiles"/> means the layout and the analysis
    /// disagree about the asset set, which no user action causes or fixes; the manual Analyze menu is
    /// where these are read when a graph looks incomplete.
    /// </summary>
    internal sealed class DependencyGraphBuildStats
    {
        /// <summary>
        /// SerializedFiles whose source asset was not in the Assets table, so they became no node.
        /// </summary>
        public int UnmappedSerializedFiles;

        /// <summary>
        /// Edges whose target has no node. The MonoScript cluster and "Library/unity default resources",
        /// the one IsBuiltIn file, which lists no source asset and so could not be a node in any case.
        /// </summary>
        public int DroppedEdges;

        /// <summary>
        /// Edges from an asset to itself. Anything that is not a component, scene object, audio mixer,
        /// built-in or script gets a SerializedFile of its own (DetermineCluster's default rule), so a .fbx
        /// or .psb becomes one file per built object plus a container that references them all - and every
        /// one of those references collapses back onto the same asset.
        /// </summary>
        public int SelfEdges;

        /// <summary>
        /// Repeated edges collapsed into one, from the same fan-out: a prefab using several meshes of one
        /// model lists a dependency per mesh, and they all resolve to that one asset.
        /// </summary>
        public int DuplicateEdges;

        public override string ToString() =>
            $"unmapped serialized files: {UnmappedSerializedFiles}, dropped edges: {DroppedEdges}, " +
            $"self edges: {SelfEdges}, duplicate edges: {DuplicateEdges}";
    }

    /// <summary>
    /// Builds the forward <see cref="DependencyGraph"/> for a parsed <see cref="ContentLayout"/>.
    /// Three linear passes: attribute serialized files to assets, emit their edges, then compress
    /// into CSR.
    /// </summary>
    /// <remarks>
    /// A node exists only where the layout attributes a serialized file to a single asset. Assets
    /// that share a serialized file are excluded, because their edges cannot be told apart: today
    /// that is the MonoScript cluster, whose scripts would otherwise fabricate every referrer
    /// of the cluster as a referrer of every script. The rule is the cluster, not the ".cs"
    /// extension, so that it self-heals if scripts ever get their own serialized files.
    ///
    /// The MonoScript file being the only one is not just an observation. ContentBuild's DetermineCluster
    /// (Modules/ContentBuild/Editor/Ucbp/CalculateBuildLayout.cpp) decides which file an object is written
    /// to with six rules, and only the MonoScript rule keys on a constant shared across assets - the other
    /// five key on the object's own GUID or its own id. IdentifySourceAssets then lists the distinct GUIDs
    /// of a file's objects, and one cluster becomes exactly one content file, so every other file names
    /// exactly one asset.
    ///
    /// It stays a rule rather than an assumption because it has not always been true - build-time merging
    /// of circular references packed each cycle into one file until it was deleted in July 2026 - but it
    /// is not instrumented beyond that: those builds carry an older ContentLayout version, which the
    /// artifact header records so the Stage 2 reader can refuse them whole (nothing gates on it yet).
    /// <see cref="DependencyGraph.IsAttributed"/> is what tells the UI which assets a cluster took
    /// out, whatever produced it.
    /// </remarks>
    internal static class DependencyGraphBuilder
    {
        static readonly ProfilerMarker s_BuildMarker = new ProfilerMarker("DependencyGraphBuilder.Build");

        /// <param name="pathToAssetId">Asset path to Assets-table id. Unmatched paths are dropped and counted.</param>
        /// <param name="nodeCount">Length of the Assets table: every asset gets a row, most of them empty.</param>
        public static DependencyGraph Build(
            ContentLayout layout,
            IReadOnlyDictionary<string, int> pathToAssetId,
            int nodeCount,
            out DependencyGraphBuildStats stats)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (pathToAssetId == null)
                throw new ArgumentNullException(nameof(pathToAssetId));
            if (nodeCount < 0)
                throw new ArgumentOutOfRangeException(nameof(nodeCount));

            stats = new DependencyGraphBuildStats();
            try
            {
                using (s_BuildMarker.Auto())
                    return new Context(layout, pathToAssetId, nodeCount, stats).Build();
            }
            catch (Exception e)
            {
                // Malformed ContentLayout shouldn't take down analysis generation. Match the
                // RootAssetStatsCalculator pattern: log, and let the caller carry on without a graph.
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to build the dependency graph: {e.Message}");
                return null;
            }
        }

        /// <summary>One serialized file depending on another that belongs to the same asset.</summary>
        private readonly struct FilePair
        {
            public readonly int From;
            public readonly int To;

            public FilePair(int from, int to)
            {
                From = from;
                To = to;
            }
        }

        private sealed class Context
        {
            readonly ContentLayout m_Layout;
            readonly SerializedFileLayout[] m_SerializedFiles;
            readonly IReadOnlyDictionary<string, int> m_PathToAssetId;
            readonly int m_NodeCount;
            readonly DependencyGraphBuildStats m_Stats;
            readonly LoadableObjectIdLayout[] m_LoadableObjects;
            readonly Dictionary<string, int> m_ScenePathToSf;

            readonly int[] m_SfToNode;
            // Every binary artifact each serialized file needs - its content file plus the streaming
            // resources that file points at - as CSR over serialized files. Kept as a set of artifact ids
            // rather than a byte total so that an artifact two files share is paid for once per edge.
            int[] m_FileArtifactOffsets;
            int[] m_FileArtifactIds;
            readonly uint[] m_Loadable;
            readonly uint[] m_Attributed;

            int[] m_EdgeSources;

            // Edge keys - see EdgeKey at the bottom of this class. Sorting them groups a row by target
            // and then by file, which is what lets Pass C add up what each edge actually costs while
            // still collapsing the row to one entry per target.
            long[] m_EdgeTargets;
            int m_EdgeCount;

            // Dependencies that stay inside one asset - the edges Pass B drops as self-edges. A referrer
            // naming one file of an asset also pays for whatever that file needs from the same asset, so
            // sizing an edge means following these. Grown rather than sized up front: they are a small
            // fraction of the dependencies walked, and counting them first would cost more than it saves.
            readonly List<FilePair> m_WithinAsset = new List<FilePair>();
            int[] m_WithinAssetOffsets;
            int[] m_WithinAssetTargets;

            // Reused by the per-edge walk. The mark is stamped with an edge counter so it never needs clearing.
            int[] m_Mark;
            int[] m_ArtifactMark;
            int[] m_Pending;
            int[] m_Seeds;
            int m_EdgeStamp;

            public Context(
                ContentLayout layout,
                IReadOnlyDictionary<string, int> pathToAssetId,
                int nodeCount,
                DependencyGraphBuildStats stats)
            {
                m_Layout = layout;
                m_SerializedFiles = layout.SerializedFiles ?? Array.Empty<SerializedFileLayout>();
                m_PathToAssetId = pathToAssetId;
                m_NodeCount = nodeCount;
                m_Stats = stats;

                m_LoadableObjects = layout.LoadableObjectIds ?? Array.Empty<LoadableObjectIdLayout>();
                var loadableScenes = layout.LoadableSceneIds ?? Array.Empty<LoadableSceneIdLayout>();

                m_ScenePathToSf = new Dictionary<string, int>(loadableScenes.Length, StringComparer.Ordinal);
                foreach (var scene in loadableScenes)
                {
                    if (scene.SerializedFile >= 0)
                        m_ScenePathToSf[scene.Path] = scene.SerializedFile;
                }

                m_SfToNode = new int[m_SerializedFiles.Length];
                var bitWords = DependencyGraph.BitWordCount(nodeCount);
                m_Loadable = new uint[bitWords];
                m_Attributed = new uint[bitWords];
            }

            public DependencyGraph Build()
            {
                AttributeNodes();
                MarkLoadables();
                IndexFileArtifacts();
                EmitEdges();
                BuildWithinAssetAdjacency();
                return Compress();
            }

            // Pass A - which asset, if any, each serialized file speaks for.
            void AttributeNodes()
            {
                for (var i = 0; i < m_SerializedFiles.Length; i++)
                {
                    m_SfToNode[i] = -1;

                    var sf = m_SerializedFiles[i];
                    if (sf.IsBuiltIn)
                        continue;

                    var sources = sf.SourceAssets;
                    if (sources == null || sources.Length == 0)
                        continue;

                    // More than one asset in a file: their edges cannot be told apart, so none of them
                    // becomes a node. Today that is only the MonoScript file.
                    if (sources.Length > 1)
                        continue;

                    if (!m_PathToAssetId.TryGetValue(sources[0], out var assetId) || (uint)assetId >= (uint)m_NodeCount)
                    {
                        m_Stats.UnmappedSerializedFiles++;
                        continue;
                    }

                    // Several serialized files naming the same asset all map to the one node, which is
                    // what keeps an asset's referrers in a single slice instead of split across copies.
                    m_SfToNode[i] = assetId;
                    DependencyGraph.SetBit(m_Attributed, assetId);
                }
            }

            // Loadable identity is the layout's own loadable tables, not the containing file.
            void MarkLoadables()
            {
                foreach (var obj in m_LoadableObjects)
                    MarkLoadableSf(obj.SerializedFile);

                var loadableScenes = m_Layout.LoadableSceneIds;
                if (loadableScenes != null)
                {
                    foreach (var scene in loadableScenes)
                    {
                        if (scene.SerializedFile >= 0)
                            MarkLoadable(scene.Path);
                    }
                }
            }

            void MarkLoadableSf(int sfIndex)
            {
                var assetId = NodeOfSf(sfIndex);
                if (assetId >= 0)
                    DependencyGraph.SetBit(m_Loadable, assetId);
            }

            void MarkLoadable(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath))
                    return;
                if (m_PathToAssetId.TryGetValue(assetPath, out var assetId) && (uint)assetId < (uint)m_NodeCount)
                    DependencyGraph.SetBit(m_Loadable, assetId);
            }

            // Pass B - the three edge kinds, translated from serialized files to assets. Raw pairs:
            // deduplication belongs in Pass C, where all of a node's edges are finally together.
            void EmitEdges()
            {
                var capacity = CountRawEdges();
                m_EdgeSources = new int[capacity];
                m_EdgeTargets = new long[capacity];

                for (var i = 0; i < m_SerializedFiles.Length; i++)
                {
                    var source = m_SfToNode[i];
                    if (source < 0)
                        continue;

                    var sf = m_SerializedFiles[i];

                    if (sf.SerializedFileDependencies != null)
                    {
                        foreach (var dependency in sf.SerializedFileDependencies)
                            Emit(source, i, dependency);
                    }

                    if (sf.LoadableDependencies != null)
                    {
                        foreach (var loadableIndex in sf.LoadableDependencies)
                            Emit(source, i, SfOfLoadable(loadableIndex));
                    }

                    if (sf.LoadableSceneDependencies != null)
                    {
                        foreach (var path in sf.LoadableSceneDependencies)
                            Emit(source, i, m_ScenePathToSf.TryGetValue(path, out var sfIndex) ? sfIndex : -1);
                    }
                }
            }

            // Exact, so the edge buffers never grow.
            int CountRawEdges()
            {
                var total = 0;
                for (var i = 0; i < m_SerializedFiles.Length; i++)
                {
                    if (m_SfToNode[i] < 0)
                        continue;
                    var sf = m_SerializedFiles[i];
                    total += Length(sf.SerializedFileDependencies)
                             + Length(sf.LoadableDependencies)
                             + Length(sf.LoadableSceneDependencies);
                }
                return total;
            }

            void Emit(int source, int sourceSf, int targetSf)
            {
                var target = NodeOfSf(targetSf);
                if (target < 0)
                {
                    // The target is built in, inside a cluster, or the reference didn't resolve. Dropped
                    // rather than spliced through, which is only safe while clusters are pure sinks.
                    m_Stats.DroppedEdges++;
                    return;
                }

                if (target == source)
                {
                    m_Stats.SelfEdges++;
                    m_WithinAsset.Add(new FilePair(sourceSf, targetSf));
                    return;
                }

                m_EdgeSources[m_EdgeCount] = source;
                m_EdgeTargets[m_EdgeCount] = EdgeKey(target, targetSf);
                m_EdgeCount++;
            }

            int NodeOfSf(int sfIndex) =>
                (uint)sfIndex < (uint)m_SfToNode.Length ? m_SfToNode[sfIndex] : -1;

            int SfOfLoadable(int loadableIndex) =>
                (uint)loadableIndex < (uint)m_LoadableObjects.Length ? m_LoadableObjects[loadableIndex].SerializedFile : -1;

            // Pass C - counting sort by source into CSR, then sort and compact each row.
            //
            // Sorting is what makes the same content produce the same bytes: it keeps the artifact
            // diffable across regenerations and leaves display ordering a pure UI concern rather than
            // something that silently inherits emission order.
            //
            // Compacting is what removes duplicates, and there are many of them: a multi-object asset
            // becomes many serialized files, and a referrer lists every one it uses. Most repeat inside a
            // single dependency list, which a per-file check would catch - but not all, because an asset
            // that is the sole source of several files emits its edges from each of them. Doing it once
            // the row is assembled covers both for the same cost.
            DependencyGraph Compress()
            {
                // Two offsets arrays, and they are not the same. scatterOffsets indexes the raw entries
                // in scattered; edgeOffsets, filled below, indexes the finished edges in targets. A row
                // shrinks between them, because entries for one target collapse into a single edge.
                var scatterOffsets = new int[m_NodeCount + 1];
                for (var i = 0; i < m_EdgeCount; i++)
                    scatterOffsets[m_EdgeSources[i] + 1]++;
                for (var i = 0; i < m_NodeCount; i++)
                    scatterOffsets[i + 1] += scatterOffsets[i];

                var scattered = new long[m_EdgeCount];
                var cursor = new int[m_NodeCount];
                Array.Copy(scatterOffsets, cursor, m_NodeCount);
                for (var i = 0; i < m_EdgeCount; i++)
                    scattered[cursor[m_EdgeSources[i]]++] = m_EdgeTargets[i];

                var targets = new int[m_EdgeCount];
                var edgeBytes = new uint[m_EdgeCount];
                var edgeOffsets = new int[m_NodeCount + 1];
                var write = 0;

                for (var node = 0; node < m_NodeCount; node++)
                {
                    edgeOffsets[node] = write;

                    var start = scatterOffsets[node];
                    var end = scatterOffsets[node + 1];
                    if (start == end)
                        continue;

                    Array.Sort(scattered, start, end - start);

                    // Sorted by target, then by target file, so one target's files are contiguous: read a
                    // whole group, then size it once from all of its files together.
                    var i = start;
                    while (i < end)
                    {
                        var target = TargetAssetOf(scattered[i]);

                        var seedCount = 0;
                        var groupCount = 0;
                        while (i < end && TargetAssetOf(scattered[i]) == target)
                        {
                            // Keys are sorted, so repeats of one file sit next to each other: comparing
                            // against the last seed kept is enough to drop them.
                            var file = TargetFileOf(scattered[i]);
                            var isRepeat = seedCount > 0 && m_Seeds[seedCount - 1] == file;
                            if (!isRepeat)
                                m_Seeds[seedCount++] = file;

                            groupCount++;
                            i++;
                        }

                        // Everything past the first entry collapsed into this one edge.
                        m_Stats.DuplicateEdges += groupCount - 1;

                        targets[write] = target;
                        edgeBytes[write] = Saturate(MeasureEdge(seedCount));
                        write++;
                    }
                }
                edgeOffsets[m_NodeCount] = write;

                if (write != targets.Length)
                {
                    Array.Resize(ref targets, write);
                    Array.Resize(ref edgeBytes, write);
                }

                // Stamped with the layout's own manifest hash. The reader compares it against the
                // analysis, so a graph built from a layout that doesn't belong to this build is refused
                // rather than served against ids it never matched.
                return new DependencyGraph(
                    edgeOffsets, targets, edgeBytes, m_Loadable, m_Attributed,
                    m_Layout.Version, m_Layout.BuildManifestHash);
            }

            // What a reference to this target costs: every binary artifact behind the files it named, plus
            // the files those need from the same asset. Both halves matter. Following within-asset
            // dependencies is why referencing one sprite of a sheet costs the sheet's texture - that
            // dependency never becomes an edge, so nothing else would count it. And summing artifacts
            // rather than per-file totals is why a resource two of the target's files share is paid for
            // once: the two stamps below deduplicate files and artifacts by the same mechanism.
            ulong MeasureEdge(int seedCount)
            {
                var binaries = m_Layout.BinaryArtifacts;
                var stamp = ++m_EdgeStamp;
                var top = 0;

                for (var i = 0; i < seedCount; i++)
                {
                    var file = m_Seeds[i];
                    m_Mark[file] = stamp;
                    m_Pending[top++] = file;
                }

                var total = 0UL;
                while (top > 0)
                {
                    var file = m_Pending[--top];

                    var artifactEnd = m_FileArtifactOffsets[file + 1];
                    for (var a = m_FileArtifactOffsets[file]; a < artifactEnd; a++)
                    {
                        var artifact = m_FileArtifactIds[a];
                        if (m_ArtifactMark[artifact] == stamp)
                            continue;
                        m_ArtifactMark[artifact] = stamp;
                        total += binaries[artifact].Size;
                    }

                    var end = m_WithinAssetOffsets[file + 1];
                    for (var e = m_WithinAssetOffsets[file]; e < end; e++)
                    {
                        var next = m_WithinAssetTargets[e];
                        if (m_Mark[next] == stamp)
                            continue;
                        m_Mark[next] = stamp;
                        m_Pending[top++] = next;
                    }
                }

                return total;
            }

            // CSR over serialized files, from the pairs Pass B set aside as self-edges.
            void BuildWithinAssetAdjacency()
            {
                var fileCount = m_SerializedFiles.Length;

                m_WithinAssetOffsets = new int[fileCount + 1];
                m_WithinAssetTargets = new int[m_WithinAsset.Count];
                m_Mark = new int[fileCount];
                m_ArtifactMark = new int[(m_Layout.BinaryArtifacts ?? Array.Empty<BinaryArtifact>()).Length];
                m_Pending = new int[fileCount];
                m_Seeds = new int[fileCount];

                // Counting sort by the depending file: count, prefix-sum into row starts, then scatter.
                foreach (var pair in m_WithinAsset)
                    m_WithinAssetOffsets[pair.From + 1]++;
                for (var file = 0; file < fileCount; file++)
                    m_WithinAssetOffsets[file + 1] += m_WithinAssetOffsets[file];

                var cursor = new int[fileCount];
                Array.Copy(m_WithinAssetOffsets, cursor, fileCount);
                foreach (var pair in m_WithinAsset)
                    m_WithinAssetTargets[cursor[pair.From]++] = pair.To;
            }

            // Which binary artifacts each serialized file needs: its own content file, plus the streaming
            // resources that file points at - a mesh .resS, an audio or video .resource. Those are the bulk
            // of a build's bytes, so a size that skipped them would be meaningless.
            //
            // Stored as ids rather than summed here, because two files of one asset can point at the same
            // resource and an edge that reaches both must pay for it once. MeasureEdge does the summing,
            // where it can see every file the edge touches.
            void IndexFileArtifacts()
            {
                var binaries = m_Layout.BinaryArtifacts ?? Array.Empty<BinaryArtifact>();
                var fileCount = m_SerializedFiles.Length;
                m_FileArtifactOffsets = new int[fileCount + 1];

                var ids = new List<int>(fileCount);
                var visited = new HashSet<int>();
                var pending = new Stack<int>();

                for (var i = 0; i < fileCount; i++)
                {
                    m_FileArtifactOffsets[i] = ids.Count;
                    if (m_SfToNode[i] < 0)
                        continue;

                    // -1 for the built-in entry (already skipped as no node); range-checked because a
                    // hand-constructed layout can carry the field's 0 default with no artifacts at all.
                    var root = m_SerializedFiles[i].ArtifactIndex;
                    if ((uint)root >= (uint)binaries.Length)
                        continue;

                    visited.Clear();
                    pending.Clear();
                    pending.Push(root);

                    // A content file only ever points at streaming resources, never at another content
                    // file, so this cannot wander into a neighbouring file's bytes.
                    while (pending.Count > 0)
                    {
                        var index = pending.Pop();
                        if ((uint)index >= (uint)binaries.Length || !visited.Add(index))
                            continue;

                        ids.Add(index);

                        var references = binaries[index].ArtifactReferences;
                        if (references == null)
                            continue;
                        foreach (var reference in references)
                            pending.Push(reference);
                    }
                }

                m_FileArtifactOffsets[fileCount] = ids.Count;
                m_FileArtifactIds = ids.ToArray();
            }

            static uint Saturate(ulong bytes) => bytes > uint.MaxValue ? uint.MaxValue : (uint)bytes;

            // An edge's target asset and the target file it came through, packed into one long so a single
            // Array.Sort orders a row by asset first and by file second. The asset sits in the high half,
            // so it dominates the comparison; both halves are non-negative, so the ordering is plain
            // ascending. Splitting them back out is a shift and a mask.
            static long EdgeKey(int targetAsset, int targetFile) => ((long)targetAsset << 32) | (uint)targetFile;

            static int TargetAssetOf(long key) => (int)(key >> 32);

            static int TargetFileOf(long key) => (int)(key & 0xFFFFFFFFL);

            static int Length(Array array) => array?.Length ?? 0;
        }
    }
}
