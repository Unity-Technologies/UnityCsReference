// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>Anything that can expose a read-only sequence of blocks for composition (e.g. a BlockAsset).</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IBlockCollectionProvider
    {
        IEnumerable<IBlock> GetBlockCollection();
    }

    /// <summary>
    /// Non-generic base so editors and importers can refer to a BlockAsset without knowing its
    /// element type.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public class BlockAssetBase : ScriptableObject
    {
    }

    /// <summary>
    /// Generic base class for BlockAssets that store a typed block collection.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public abstract class BlockAssetBase<T> : BlockAssetBase, IBlockCollectionProvider where T : class, IBlock
    {
        public BlockCollection<T> blockCollection = new BlockCollection<T>();

        // `T : class` lets the typed collection satisfy IEnumerable<IBlock> covariantly (zero-copy):
        // an interface-only constraint permits value types, which variance would forbid.
        public IEnumerable<IBlock> GetBlockCollection()
        {
            return blockCollection;
        }
    }
}
