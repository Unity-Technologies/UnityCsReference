// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// General BlockAsset that can contain any IBlock types.
    /// Created via .blockasset files imported by <see cref="BlockAssetImporter"/>.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public class BlockAsset : BlockAssetBase<IBlock>
    {
    }
}
