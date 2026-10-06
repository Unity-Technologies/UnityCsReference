// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// A <see cref="BlockCollectionReference"/> whose provider is a BlockAsset. Supports any
    /// <see cref="BlockAssetBase"/> subtype via the generic parameter.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    [ImportBlock(1, "Block Asset Reference Base", "Base class for blocks that reference a Block Asset.")]
    public abstract class BlockAssetReference<TBlockAsset> : BlockCollectionReference
        where TBlockAsset : BlockAssetBase, IBlockCollectionProvider
    {
        /// <summary>The referenced asset whose blocks are included as children of this block.</summary>
        public LazyLoadReference<TBlockAsset> blockAsset;

        public override string Name =>
            blockAsset.isSet ? $"Block Asset Reference ({blockAsset.asset?.name})" : "Block Asset Reference";

        internal const string k_BlocksPath = "blockCollection.m_Blocks";

        internal override string ReferenceFieldName => "blockAsset";
        internal override string ProviderBlocksPath => k_BlocksPath;
        internal override UnityEngine.Object GetReferencedObject() => blockAsset.asset;

        /// <inheritdoc/>
        public override void RegisterDependencies(AssetImportContext ctx)
        {
            ctx.DependsOnArtifact(blockAsset);
        }

        /// <inheritdoc/>
        public override IEnumerable<IBlock> GetChildBlocks()
        {
            var asset = blockAsset.asset;
            if (asset == null)
                return Array.Empty<IBlock>();

            return DeepCloneBlocks(asset.GetBlockCollection());
        }
    }

    /// <summary>
    /// The concrete reference for general BlockAssets (BlockCollection&lt;IBlock&gt;).
    /// Use this to embed a BlockAsset into any block collection.
    /// </summary>
    [MovedFrom(false, "UnityEditor.AssetImporters.ImportBlocks")]
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    [ImportBlock(1, "Block Asset Reference", "Reference the Blocks in a Block Asset.")]
    public class BlockAssetReference : BlockAssetReference<BlockAsset>
    {
    }
}
