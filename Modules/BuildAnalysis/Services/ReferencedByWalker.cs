// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Bounds on one expansion. Defensive limits rather than tuned values: fan-out and the presence
    /// of cycles vary from build to build.
    /// </summary>
    internal readonly struct WalkLimits
    {
        public static readonly WalkLimits Default = new WalkLimits(maxRoutesPerGroup: 999, maxDepth: 64, maxVisits: 100000);

        public WalkLimits(int maxRoutesPerGroup, int maxDepth, int maxVisits)
        {
            if (maxRoutesPerGroup < 1)
                throw new ArgumentOutOfRangeException(nameof(maxRoutesPerGroup));
            if (maxDepth < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxVisits < 1)
                throw new ArgumentOutOfRangeException(nameof(maxVisits));

            MaxRoutesPerGroup = maxRoutesPerGroup;
            MaxDepth = maxDepth;
            MaxVisits = maxVisits;
        }

        /// <summary>Routes counted per group before the count is capped.</summary>
        public int MaxRoutesPerGroup { get; }

        /// <summary>Hops above the expanded row before a climb is cut off.</summary>
        public int MaxDepth { get; }

        /// <summary>Nodes visited per expansion before the walk stops.</summary>
        public int MaxVisits { get; }
    }

    /// <summary>
    /// Answers "what pulls this asset into the build" one expansion at a time. From an expanded row
    /// it climbs the inverse edges to the declared roots and returns every simple route, grouped by
    /// where it ends. Breadth-first, so routes come out shortest first and a walk stopped by a limit
    /// still holds every route up to the depth it reached. Never climbs above a root.
    /// </summary>
    internal sealed class ReferencedByWalker
    {
        readonly IAssetReferenceProvider m_Graph;
        readonly uint[] m_Roots;

        public ReferencedByWalker(IAssetReferenceProvider graph, ReadOnlySpan<int> rootAssetIds)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_Roots = new uint[DependencyGraph.BitWordCount(graph.NodeCount)];
            foreach (var id in rootAssetIds)
            {
                if ((uint)id < (uint)graph.NodeCount)
                    DependencyGraph.SetBit(m_Roots, id);
            }
        }

        public ReferencedByWalker(IAssetReferenceProvider graph, BuildAnalysisRootAsset[] roots)
            : this(graph, RootIds(roots))
        {
        }

        /// <summary>A declared root: a member of the build's root table.</summary>
        public bool IsRoot(int assetId) =>
            (uint)assetId < (uint)m_Graph.NodeCount && DependencyGraph.GetBit(m_Roots, assetId);

        /// <summary>Whether expanding this row could show anything: it has referrers and is not itself a root.</summary>
        public bool HasAncestry(int assetId) => !IsRoot(assetId) && m_Graph.GetReferencedByCount(assetId) > 0;

        /// <summary>
        /// Climbs from the last entry of <paramref name="path"/>, the row being expanded. The whole
        /// path, from the inspected asset down to that row, counts as already visited, so a route
        /// that reaches any of it is a cycle.
        /// </summary>
        public ReferencedByExpansion Expand(ReadOnlySpan<int> path, in WalkLimits limits)
        {
            if (path.Length == 0)
                throw new ArgumentException("The path must end with the row being expanded.", nameof(path));

            var expanded = path[path.Length - 1];
            if (IsRoot(expanded) || m_Graph.GetReferencedByCount(expanded) == 0)
                return ReferencedByExpansion.Empty;

            return new Walk(m_Graph, m_Roots, path, limits).Run();
        }

        static int[] RootIds(BuildAnalysisRootAsset[] roots)
        {
            if (roots == null)
                return Array.Empty<int>();
            var ids = new int[roots.Length];
            for (var i = 0; i < roots.Length; i++)
                ids[i] = roots[i].AssetId;
            return ids;
        }

        sealed class Walk
        {
            const int k_InitialCapacity = 64;

            readonly IAssetReferenceProvider m_Graph;
            readonly uint[] m_Roots;
            readonly int[] m_Path;
            readonly WalkLimits m_Limits;

            // The tree of visited nodes. Index order is discovery order, which is breadth-first.
            int[] m_AssetIds = new int[k_InitialCapacity];
            int[] m_Parents = new int[k_InitialCapacity];
            int[] m_Depths = new int[k_InitialCapacity];
            RouteNodeKind[] m_Kinds = new RouteNodeKind[k_InitialCapacity];
            int m_Count;

            readonly Dictionary<long, int> m_GroupIndex = new Dictionary<long, int>();
            readonly List<GroupBuilder> m_Groups = new List<GroupBuilder>();
            readonly List<int> m_CycleLeaves = new List<int>();
            readonly List<int> m_DeadEndLeaves = new List<int>();
            readonly List<int> m_TruncatedLeaves = new List<int>();
            bool m_Complete = true;
            int m_ReachedDepth;

            public Walk(IAssetReferenceProvider graph, uint[] roots, ReadOnlySpan<int> path, in WalkLimits limits)
            {
                m_Graph = graph;
                m_Roots = roots;
                m_Path = path.ToArray();
                m_Limits = limits;
            }

            public ReferencedByExpansion Run()
            {
                if (AddChildren(parentNode: -1, parentAsset: m_Path[m_Path.Length - 1], depth: 1))
                {
                    // Nodes are appended as they are found, so index order walks each level before the next.
                    for (var node = 0; node < m_Count; node++)
                    {
                        if (m_Kinds[node] != RouteNodeKind.Interior)
                            continue;
                        if (!AddChildren(node, m_AssetIds[node], m_Depths[node] + 1))
                            break;
                    }
                }
                return Build();
            }

            // False when the visit budget ran out; the walk stops where it is.
            bool AddChildren(int parentNode, int parentAsset, int depth)
            {
                var referrers = m_Graph.GetReferencedBy(parentAsset);
                for (var i = 0; i < referrers.Length; i++)
                {
                    if (m_Count == m_Limits.MaxVisits)
                    {
                        m_Complete = false;
                        return false;
                    }

                    var asset = referrers[i];
                    var kind = Classify(asset, parentNode, depth);
                    var node = Add(asset, parentNode, depth, kind);
                    switch (kind)
                    {
                        case RouteNodeKind.Root:
                            RecordRoute(node, parentAsset, asset);
                            break;
                        case RouteNodeKind.Cycle:
                            m_CycleLeaves.Add(node);
                            break;
                        case RouteNodeKind.DeadEnd:
                            m_DeadEndLeaves.Add(node);
                            break;
                        case RouteNodeKind.Truncated:
                            m_TruncatedLeaves.Add(node);
                            m_Complete = false;
                            break;
                    }
                }
                return true;
            }

            // A root has no referrers of its own, so it must be recognised before the dead-end test.
            RouteNodeKind Classify(int asset, int parentNode, int depth)
            {
                if (IsOnPath(asset, parentNode))
                    return RouteNodeKind.Cycle;
                if (DependencyGraph.GetBit(m_Roots, asset))
                    return RouteNodeKind.Root;
                if (m_Graph.GetReferencedByCount(asset) == 0)
                    return RouteNodeKind.DeadEnd;
                if (depth >= m_Limits.MaxDepth)
                    return RouteNodeKind.Truncated;
                return RouteNodeKind.Interior;
            }

            bool IsOnPath(int asset, int parentNode)
            {
                for (var i = 0; i < m_Path.Length; i++)
                {
                    if (m_Path[i] == asset)
                        return true;
                }
                for (var node = parentNode; node >= 0; node = m_Parents[node])
                {
                    if (m_AssetIds[node] == asset)
                        return true;
                }
                return false;
            }

            int Add(int asset, int parentNode, int depth, RouteNodeKind kind)
            {
                if (m_Count == m_AssetIds.Length)
                    Grow();
                var node = m_Count++;
                m_AssetIds[node] = asset;
                m_Parents[node] = parentNode;
                m_Depths[node] = depth;
                m_Kinds[node] = kind;
                if (depth > m_ReachedDepth)
                    m_ReachedDepth = depth;
                return node;
            }

            void Grow()
            {
                var capacity = Math.Min(m_AssetIds.Length * 2, m_Limits.MaxVisits);
                Array.Resize(ref m_AssetIds, capacity);
                Array.Resize(ref m_Parents, capacity);
                Array.Resize(ref m_Depths, capacity);
                Array.Resize(ref m_Kinds, capacity);
            }

            void RecordRoute(int leaf, int rootPlusOne, int root)
            {
                var key = ((long)rootPlusOne << 32) | (uint)root;
                if (!m_GroupIndex.TryGetValue(key, out var index))
                {
                    index = m_Groups.Count;
                    m_GroupIndex.Add(key, index);
                    m_Groups.Add(new GroupBuilder(rootPlusOne, root));
                }

                var group = m_Groups[index];
                if (group.Count == m_Limits.MaxRoutesPerGroup)
                {
                    group.Capped = true;
                    return;
                }
                group.Count++;
                group.Leaves.Add(leaf);
            }

            ReferencedByExpansion Build()
            {
                Array.Resize(ref m_AssetIds, m_Count);
                Array.Resize(ref m_Parents, m_Count);
                Array.Resize(ref m_Depths, m_Count);
                Array.Resize(ref m_Kinds, m_Count);

                var groups = new RouteGroup[m_Groups.Count];
                for (var i = 0; i < groups.Length; i++)
                {
                    var group = m_Groups[i];
                    groups[i] = new RouteGroup(group.RootPlusOne, group.Root, group.Count, group.Capped, group.Leaves.ToArray());
                }

                return new ReferencedByExpansion(
                    m_AssetIds, m_Parents, m_Depths, m_Kinds, groups,
                    m_CycleLeaves.ToArray(), m_DeadEndLeaves.ToArray(), m_TruncatedLeaves.ToArray(),
                    m_Complete, m_ReachedDepth);
            }

            sealed class GroupBuilder
            {
                public GroupBuilder(int rootPlusOne, int root)
                {
                    RootPlusOne = rootPlusOne;
                    Root = root;
                }

                public int RootPlusOne { get; }
                public int Root { get; }
                public int Count { get; set; }
                public bool Capped { get; set; }
                public List<int> Leaves { get; } = new List<int>();
            }
        }
    }
}
