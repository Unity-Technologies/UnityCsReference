// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Asset dependency adjacency for one build, in compressed sparse row form: every edge is
    /// concatenated into one array, and a second array records where each node's run starts.
    /// Nodes are <see cref="BuildAnalysisAsset.Id"/> values, which are positional, so a graph is only
    /// ever valid against the <see cref="BuildAnalysis"/> generated alongside it.
    ///
    /// Runtime-only: persisted by <see cref="DependencyGraphStore"/> into its own sibling artifact and
    /// never part of BuildAnalysis.json.
    /// </summary>
    internal sealed class DependencyGraph : IAssetReferenceProvider
    {
        // Only forward edges are stored. The inverse is a counting sort over these same arrays -
        // three linear scans, cheap enough to derive per session and half the artifact to leave out.
        private readonly int[] m_Offsets;
        private readonly int[] m_Targets;
        private readonly uint[] m_EdgeBytes;
        private readonly uint[] m_Loadable;
        private readonly uint[] m_Attributed;

        private int[] m_InverseOffsets;
        private int[] m_InverseTargets;

        public int NodeCount => m_Offsets.Length - 1;
        public int EdgeCount => m_Targets.Length;

        /// <summary>Version of the ContentLayout.json this graph was built from, recorded as read.</summary>
        public int ContentLayoutVersion { get; }

        /// <summary>The build this graph describes, matched against Summary.BuildManifestHash on load.</summary>
        public string BuildManifestHash { get; }

        public DependencyGraph(
            int[] offsets,
            int[] targets,
            uint[] edgeBytes,
            uint[] loadable,
            uint[] attributed,
            int contentLayoutVersion,
            string buildManifestHash)
        {
            m_Offsets = offsets ?? throw new ArgumentNullException(nameof(offsets));
            m_Targets = targets ?? throw new ArgumentNullException(nameof(targets));
            m_EdgeBytes = edgeBytes ?? throw new ArgumentNullException(nameof(edgeBytes));
            m_Loadable = loadable ?? throw new ArgumentNullException(nameof(loadable));
            m_Attributed = attributed ?? throw new ArgumentNullException(nameof(attributed));

            if (offsets.Length == 0)
                throw new ArgumentException("Offsets must hold at least the trailing sentinel.", nameof(offsets));
            if (offsets[0] != 0 || offsets[offsets.Length - 1] != targets.Length)
                throw new ArgumentException("Offsets must start at 0 and end at the edge count.", nameof(offsets));
            if (edgeBytes.Length != targets.Length)
                throw new ArgumentException("Edge sizes must be one per edge.", nameof(edgeBytes));

            var bitWords = BitWordCount(offsets.Length - 1);
            if (loadable.Length != bitWords)
                throw new ArgumentException("Loadable bitset does not match the node count.", nameof(loadable));
            if (attributed.Length != bitWords)
                throw new ArgumentException("Attributed bitset does not match the node count.", nameof(attributed));

            ContentLayoutVersion = contentLayoutVersion;
            BuildManifestHash = buildManifestHash ?? string.Empty;
        }

        /// <summary>The assets this asset directly references. Ascending, deduplicated.</summary>
        /// <remarks>
        /// A slice over the backing array, never a copy: the tree walks thousands of nodes per
        /// expansion. A caller that must hold the result across an await holds the offsets instead.
        /// </remarks>
        public ReadOnlySpan<int> GetReferencesTo(int assetId)
        {
            if ((uint)assetId >= (uint)NodeCount)
                return ReadOnlySpan<int>.Empty;
            var start = m_Offsets[assetId];
            return new ReadOnlySpan<int>(m_Targets, start, m_Offsets[assetId + 1] - start);
        }

        /// <summary>
        /// What each of <see cref="GetReferencesTo"/>'s targets costs this asset, aligned index for index
        /// with it. Not the target's own size: an asset that imports to many objects is written as one
        /// serialized file per object, and a referrer usually needs only some of them - referencing one
        /// mesh of a model costs that mesh, not the model. Counts the files named plus whatever those
        /// files need from the same asset, which is why referencing one sprite of a sheet also costs the
        /// sheet's texture.
        /// </summary>
        /// <remarks>
        /// Counts the binary artifacts behind those files - the content files and the streaming resources
        /// they point at, which are the bulk of a build's bytes - and counts each one once however many of
        /// the target's files share it. Saturates at <see cref="uint.MaxValue"/>.
        /// </remarks>
        public ReadOnlySpan<uint> GetReferenceSizes(int assetId)
        {
            if ((uint)assetId >= (uint)NodeCount)
                return ReadOnlySpan<uint>.Empty;
            var start = m_Offsets[assetId];
            return new ReadOnlySpan<uint>(m_EdgeBytes, start, m_Offsets[assetId + 1] - start);
        }

        public int GetReferencesToCount(int assetId)
        {
            if ((uint)assetId >= (uint)NodeCount)
                return 0;
            return m_Offsets[assetId + 1] - m_Offsets[assetId];
        }

        /// <summary>The assets that directly reference this asset. Ascending, deduplicated.</summary>
        /// <remarks>Builds the inverse adjacency on first use. Not thread-safe; call from one thread.</remarks>
        public ReadOnlySpan<int> GetReferencedBy(int assetId)
        {
            if ((uint)assetId >= (uint)NodeCount)
                return ReadOnlySpan<int>.Empty;
            EnsureInverse();
            var start = m_InverseOffsets[assetId];
            return new ReadOnlySpan<int>(m_InverseTargets, start, m_InverseOffsets[assetId + 1] - start);
        }

        public int GetReferencedByCount(int assetId)
        {
            if ((uint)assetId >= (uint)NodeCount)
                return 0;
            EnsureInverse();
            return m_InverseOffsets[assetId + 1] - m_InverseOffsets[assetId];
        }

        /// <summary>
        /// The asset is a loadable entry point - it appears in the layout's LoadableObjectIds or
        /// LoadableSceneIds. Surfaced as the italic "(loadable)" suffix on a tree row.
        /// </summary>
        public bool IsLoadable(int assetId) => GetBit(m_Loadable, assetId);

        /// <summary>
        /// The asset is in the graph at all: the layout attributed exactly one non-built-in SerializedFile
        /// to it. False means the build recorded its dependencies somewhere this graph cannot read them -
        /// it shares a file with other assets, or is in no SerializedFile - so "nothing references this" is
        /// not an answer we have.
        /// </summary>
        /// <remarks>
        /// This exists for <see cref="GetReferencedBy"/>, not for <see cref="GetReferencesTo"/>. Every
        /// script in a build shares one serialized file, so the layout records what depends on that file
        /// rather than on any individual script: reporting no referrers for one script would be plainly
        /// false, while reporting no references *from* it is correct, since scripts reference nothing in
        /// build content either way.
        ///
        /// Do not substitute a ".cs" test for this. It selects the same assets today, but a filename is
        /// not evidence about what the build recorded: if scripts ever get serialized files of their own
        /// the graph starts carrying their referrers, and this bit starts reporting them while an extension
        /// check would go on hiding them.
        /// </remarks>
        public bool IsAttributed(int assetId) => GetBit(m_Attributed, assetId);

        internal int[] Offsets => m_Offsets;
        internal int[] Targets => m_Targets;
        internal uint[] EdgeBytes => m_EdgeBytes;
        internal uint[] Loadable => m_Loadable;
        internal uint[] Attributed => m_Attributed;

        internal static int BitWordCount(int nodeCount) => (nodeCount + 31) / 32;

        internal static void SetBit(uint[] bits, int index) => bits[index >> 5] |= 1u << (index & 31);

        private bool GetBit(uint[] bits, int assetId)
        {
            if ((uint)assetId >= (uint)NodeCount)
                return false;
            return (bits[assetId >> 5] & (1u << (assetId & 31))) != 0;
        }

        internal bool IsInverseBuilt => m_InverseOffsets != null;

        // Counting sort by target id - no comparisons, no hashing, no allocation beyond the two
        // outputs and a cursor. Because sources are visited in ascending order, each row comes out
        // ascending for free, matching the canonical ordering of the forward rows.
        // The load layer can pre-build it off-thread before the graph is published.
        internal void EnsureInverse()
        {
            if (m_InverseOffsets != null)
                return;

            var nodeCount = NodeCount;
            var offsets = new int[nodeCount + 1];

            // 1. Count how often each id appears as a target. That count is the node's referrer count.
            for (var i = 0; i < m_Targets.Length; i++)
                offsets[m_Targets[i] + 1]++;

            // 2. Prefix-sum the counts into row starts.
            for (var i = 0; i < nodeCount; i++)
                offsets[i + 1] += offsets[i];

            // 3. Scatter: for each u -> v, write u into v's next free slot.
            var targets = new int[m_Targets.Length];
            var cursor = new int[nodeCount];
            Array.Copy(offsets, cursor, nodeCount);
            for (var source = 0; source < nodeCount; source++)
            {
                var end = m_Offsets[source + 1];
                for (var e = m_Offsets[source]; e < end; e++)
                    targets[cursor[m_Targets[e]]++] = source;
            }

            m_InverseTargets = targets;
            m_InverseOffsets = offsets;
        }
    }
}
