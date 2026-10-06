// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Marker interface for blocks that participate in <see cref="TextureImporter"/> import.
    /// Implement one or more of the hook interfaces below to run code during texture import.
    /// Each hook mirrors the matching <see cref="AssetPostprocessor"/> texture callback, with a trailing
    /// <see cref="AssetImportContext"/> for registering dependencies/objects.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface ITextureImporterBlock : IBlock
    {
    }

    /// <summary>Called before the texture is imported, mirroring AssetPostprocessor.OnPreprocessTexture.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPreprocessTexture : ITextureImporterBlock
    {
        public void OnPreprocessTexture(TextureImporter textureImporter, AssetImportContext ctx);
    }

    /// <summary>Called after a Texture2D is imported, mirroring AssetPostprocessor.OnPostprocessTexture.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessTexture : ITextureImporterBlock
    {
        public void OnPostprocessTexture(Texture2D texture, AssetImportContext ctx);
    }

    /// <summary>Called after a Cubemap is imported, mirroring AssetPostprocessor.OnPostprocessCubemap.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessCubemap : ITextureImporterBlock
    {
        public void OnPostprocessCubemap(Cubemap texture, AssetImportContext ctx);
    }

    /// <summary>Called after a Texture2DArray is imported, mirroring AssetPostprocessor.OnPostprocessTexture2DArray.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessTexture2DArray : ITextureImporterBlock
    {
        public void OnPostprocessTexture2DArray(Texture2DArray texture, AssetImportContext ctx);
    }

    /// <summary>Called after a Texture3D is imported, mirroring AssetPostprocessor.OnPostprocessTexture3D.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessTexture3D : ITextureImporterBlock
    {
        public void OnPostprocessTexture3D(Texture3D texture, AssetImportContext ctx);
    }

    /// <summary>Called after sprites are imported, mirroring AssetPostprocessor.OnPostprocessSprites.</summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IOnPostprocessSprites : ITextureImporterBlock
    {
        public void OnPostprocessSprites(Texture2D texture, Sprite[] sprites, AssetImportContext ctx);
    }
}
