// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IBlock
    {
        /// <summary>
        /// The name of this block.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optional description of what this block does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Return additional blocks defined on the block's fields or on an external asset.
        /// e.g. a composite block may return blocks defined on an asset it references.
        /// Returning null is treated as no children. The returned sequence is read-only:
        /// callers only enumerate it, so implementations may safely return their own storage.
        /// </summary>
        public IEnumerable<IBlock> GetChildBlocks();
    }
}
