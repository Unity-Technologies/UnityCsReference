// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Read access to one build's asset dependency adjacency, keyed by Assets-table id. The seam
    /// that keeps the inspector and the tree UI independent of the storage format.
    ///
    /// Both directions return slices over backing arrays, never copies: the tree walks thousands
    /// of nodes per expansion, and a provider that copies allocates per node. A caller that must
    /// hold a result across an await holds the asset id and re-queries instead.
    /// </summary>
    internal interface IAssetReferenceProvider
    {
        int NodeCount { get; }

        /// <summary>The assets this asset directly references. Ascending, deduplicated.</summary>
        ReadOnlySpan<int> GetReferencesTo(int assetId);

        /// <summary>
        /// What each of <see cref="GetReferencesTo"/>'s targets costs this asset, aligned index for
        /// index with it. Not the target's own size.
        /// </summary>
        ReadOnlySpan<uint> GetReferenceSizes(int assetId);

        int GetReferencesToCount(int assetId);

        /// <summary>The assets that directly reference this asset. Ascending, deduplicated.</summary>
        ReadOnlySpan<int> GetReferencedBy(int assetId);

        int GetReferencedByCount(int assetId);

        /// <summary>The asset is a loadable entry point.</summary>
        bool IsLoadable(int assetId);

        /// <summary>
        /// The asset is in the graph at all. False means the build recorded its dependencies
        /// somewhere this graph cannot read them, so "nothing references this" is not an answer we have.
        /// </summary>
        bool IsAttributed(int assetId);
    }
}
