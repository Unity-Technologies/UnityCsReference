// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// A reference to a <see cref="TextureImporterBlockAsset"/>. Implements
    /// <see cref="ITextureImporterBlock"/> so it can live in a texture importer's block collection.
    /// </summary>
    [MovedFrom(false, "UnityEditor.AssetImporters.ImportBlocks")]
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    [ImportBlock(1, "Texture Importer Block Asset Reference", "Reference the Blocks in a Texture Importer Block Asset")]
    public class TextureImporterBlockAssetReference : BlockAssetReference<TextureImporterBlockAsset>,
        ITextureImporterBlock
    {
    }
}
