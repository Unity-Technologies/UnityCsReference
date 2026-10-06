// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>The generic-free face of <see cref="BlockCollection{T}"/>.</summary>
    internal interface IBlockCollection
    {
        /// <summary>Number of blocks in the immediate collection (not traversal descendants).</summary>
        int Count { get; }

        /// <summary>
        /// Builds the dispatch for this import and registers every block's dependencies against
        /// <paramref name="ctx"/>. Never returns null.
        /// </summary>
        BlockDispatch PrepareForDispatch(AssetImportContext ctx);
    }

    /// <summary>Implemented by importers that carry a block collection.</summary>
    internal interface IBlockImporter
    {
        IBlockCollection Blocks { get; }
    }
}
