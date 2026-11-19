// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Build.Analysis
{
    /// <summary>How a node of a Referenced By expansion ended, or that it did not.</summary>
    internal enum RouteNodeKind : byte
    {
        /// <summary>Has referrers of its own; its children are the next level up.</summary>
        Interior,

        /// <summary>A declared root: the load boundary, and the end of a completed route.</summary>
        Root,

        /// <summary>Already on the path being climbed. The route loops and stops here.</summary>
        Cycle,

        /// <summary>Referenced by nothing and not a root. Should not occur on a valid build.</summary>
        DeadEnd,

        /// <summary>Has referrers, but the depth limit stopped the climb.</summary>
        Truncated,
    }

    /// <summary>
    /// Every route from one expanded row to the same terminal: the declared root that ends it and
    /// the asset one step below that root. One group is one branch of the tree.
    /// </summary>
    internal sealed class RouteGroup
    {
        public RouteGroup(int rootPlusOne, int root, int routeCount, bool capped, int[] leaves)
        {
            RootPlusOne = rootPlusOne;
            Root = root;
            RouteCount = routeCount;
            Capped = capped;
            Leaves = leaves ?? throw new ArgumentNullException(nameof(leaves));
        }

        /// <summary>
        /// The asset one step below the root on every route in the group. Equals the expanded row
        /// itself when a root references it directly.
        /// </summary>
        public int RootPlusOne { get; }

        public int Root { get; }

        /// <summary>Routes counted, stopping at the limit; see <see cref="Capped"/>.</summary>
        public int RouteCount { get; }

        /// <summary>Counting stopped at the limit: there are more routes than <see cref="RouteCount"/>.</summary>
        public bool Capped { get; }

        /// <summary>The recorded routes as leaf node indices, shortest first.</summary>
        public int[] Leaves { get; }
    }

    /// <summary>
    /// The result of climbing from one expanded row towards the roots. The nodes visited form a
    /// tree whose parent links are the routes: every node is addressed by index, and a route is
    /// read by following parents from a leaf. Owns its arrays; it outlives the walk that made it.
    /// </summary>
    internal sealed class ReferencedByExpansion
    {
        [NoAutoStaticsCleanup]
        public static readonly ReferencedByExpansion Empty = new ReferencedByExpansion(
            Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<RouteNodeKind>(),
            Array.Empty<RouteGroup>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(),
            complete: true, reachedDepth: 0);

        readonly int[] m_AssetIds;
        readonly int[] m_Parents;
        readonly int[] m_Depths;
        readonly RouteNodeKind[] m_Kinds;

        public ReferencedByExpansion(
            int[] assetIds,
            int[] parents,
            int[] depths,
            RouteNodeKind[] kinds,
            RouteGroup[] groups,
            int[] cycleLeaves,
            int[] deadEndLeaves,
            int[] truncatedLeaves,
            bool complete,
            int reachedDepth)
        {
            m_AssetIds = assetIds ?? throw new ArgumentNullException(nameof(assetIds));
            m_Parents = parents ?? throw new ArgumentNullException(nameof(parents));
            m_Depths = depths ?? throw new ArgumentNullException(nameof(depths));
            m_Kinds = kinds ?? throw new ArgumentNullException(nameof(kinds));
            if (parents.Length != assetIds.Length || depths.Length != assetIds.Length || kinds.Length != assetIds.Length)
                throw new ArgumentException("Node arrays must be one entry per visited node.");

            Groups = groups ?? throw new ArgumentNullException(nameof(groups));
            CycleLeaves = cycleLeaves ?? throw new ArgumentNullException(nameof(cycleLeaves));
            DeadEndLeaves = deadEndLeaves ?? throw new ArgumentNullException(nameof(deadEndLeaves));
            TruncatedLeaves = truncatedLeaves ?? throw new ArgumentNullException(nameof(truncatedLeaves));
            Complete = complete;
            ReachedDepth = reachedDepth;
        }

        /// <summary>Route groups in discovery order. Display order is the tree's concern.</summary>
        public RouteGroup[] Groups { get; }

        /// <summary>Leaves that closed a cycle back onto the path.</summary>
        public int[] CycleLeaves { get; }

        /// <summary>Leaves referenced by nothing that are not roots.</summary>
        public int[] DeadEndLeaves { get; }

        /// <summary>Leaves the depth limit cut off.</summary>
        public int[] TruncatedLeaves { get; }

        /// <summary>
        /// False when a limit stopped the walk before every route was seen. Every count is then a
        /// lower bound.
        /// </summary>
        public bool Complete { get; }

        /// <summary>Nodes visited: one per node of the tree.</summary>
        public int VisitCount => m_AssetIds.Length;

        /// <summary>The deepest level reached, in hops above the expanded row.</summary>
        public int ReachedDepth { get; }

        public int AssetIdAt(int node) => m_AssetIds[node];

        /// <summary>The node one hop below, or -1 for a direct referrer of the expanded row.</summary>
        public int ParentOf(int node) => m_Parents[node];

        /// <summary>Hops above the expanded row; a direct referrer is at depth 1.</summary>
        public int DepthOf(int node) => m_Depths[node];

        public RouteNodeKind KindOf(int node) => m_Kinds[node];

        /// <summary>
        /// Fills <paramref name="chain"/> with the route ending at <paramref name="leaf"/>: the first
        /// hop above the expanded row first, the leaf last.
        /// </summary>
        public void GetChain(int leaf, List<int> chain)
        {
            chain.Clear();
            for (var node = leaf; node >= 0; node = m_Parents[node])
                chain.Add(m_AssetIds[node]);
            chain.Reverse();
        }
    }
}
