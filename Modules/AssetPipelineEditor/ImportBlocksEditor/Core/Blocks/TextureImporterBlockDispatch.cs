// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal static class TextureImporterBlockDispatch
    {
        internal static void BlockPreprocessTexture(TextureImporter importer, AssetImportContext ctx)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPreprocessTexture>())
                b.OnPreprocessTexture(importer, ctx);
        }

        internal static void BlockPostprocessTexture(AssetImportContext ctx, Texture2D texture)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessTexture>())
                b.OnPostprocessTexture(texture, ctx);
        }

        internal static void BlockPostprocessCubemap(AssetImportContext ctx, Cubemap texture)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessCubemap>())
                b.OnPostprocessCubemap(texture, ctx);
        }

        internal static void BlockPostprocessTexture3D(AssetImportContext ctx, Texture3D texture)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessTexture3D>())
                b.OnPostprocessTexture3D(texture, ctx);
        }

        internal static void BlockPostprocessTexture2DArray(AssetImportContext ctx, Texture2DArray texture)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessTexture2DArray>())
                b.OnPostprocessTexture2DArray(texture, ctx);
        }

        internal static void BlockPostprocessSprites(AssetImportContext ctx, Texture2D texture, Sprite[] sprites)
        {
            foreach (var b in AssetImportCallbackDispatch.CurrentBlockDispatch().Implementing<IOnPostprocessSprites>())
                b.OnPostprocessSprites(texture, sprites, ctx);
        }
    }
}
