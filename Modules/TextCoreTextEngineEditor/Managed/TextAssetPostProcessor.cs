// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    /// <summary>
    /// Asset post processor used to handle text assets changes.
    /// This includes tracking of changes to textures used by sprite assets as well as font assets potentially getting updated outside of the Unity editor.
    /// </summary>
    internal class TextAssetPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                // Return if imported asset path is outside of the project.
                if (asset.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) == false || !asset.EndsWith(".asset"))
                    continue;

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(asset);

                if (assetType == typeof(FontAsset))
                {
                    FontAsset fontAsset = AssetDatabase.LoadAssetAtPath(asset, typeof(FontAsset)) as FontAsset;

                    if (fontAsset != null)
                        TextEditorResourceManager.RegisterFontAssetForDefinitionRefresh(fontAsset);
                }

                if (assetType == typeof(Texture2D))
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath(asset, typeof(Texture2D)) as Texture2D;

                    if (tex != null)
                        TextEventManager.ON_SPRITE_ASSET_PROPERTY_CHANGED(true, tex);
                }
            }
        }
    }

    internal class TMP_FontAssetPostProcessor : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(FontAsset))
                TextResourceManager.RebuildFontAssetCache();

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
