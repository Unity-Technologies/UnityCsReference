// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.AssetImporters;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// ScriptedImporter for .textureblockasset files.
    /// Creates a <see cref="TextureImporterBlockAsset"/> ScriptableObject containing
    /// BlockCollection&lt;ITextureImporterBlock&gt;.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    [ScriptedImporter(1, new[] { "textureblockasset" }, -5000)] // After scripts, before all assets
    public class TextureImporterBlockAssetImporter : BlockAssetImporter<ITextureImporterBlock, TextureImporterBlockAsset>
    {
        internal const string k_CreateMenuPath = "Assets/Create/Import Blocks/Texture Importer Block Asset";

        [MenuItem(k_CreateMenuPath)]
        static void CreateTextureImporterBlockAsset() =>
            ProjectWindowUtil.CreateAssetWithTextContent("New Texture Block Asset.textureblockasset", "");

        // Only the create-menu entry is gated; the [ScriptedImporter] registration stays active either
        // way, so existing .textureblockasset files keep importing.
        [InitializeOnLoadMethod]
        static void RemoveCreateMenuItemIfDisabled()
        {
            if (ImportBlocksToggle.IsEnabled)
                return;

            // Menus are still being built while [InitializeOnLoadMethod] runs, so a removal made here is
            // undone. Deferring until the load settles is how RemoveLegacyMenuItems.cs does the same thing.
            EditorApplication.delayCall += () => Menu.RemoveMenuItem(k_CreateMenuPath);
        }
    }
}
