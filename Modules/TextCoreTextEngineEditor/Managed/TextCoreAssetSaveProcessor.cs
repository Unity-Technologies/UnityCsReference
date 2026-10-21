// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.TextCore.Text;

namespace UnityEditor.TextCore.Text
{
    class TextCoreAssetSaveProcessor : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];

                if (string.IsNullOrEmpty(path) || !typeof(FontAsset).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)))
                    continue;

                FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);

                if (fontAsset == null)
                    continue;

                TextEditorResourceManager.ConfigureAtlasTexturePersistence(fontAsset);

                if (fontAsset.hasSessionOnlyDynamicData)
                    fontAsset.ClearSessionDynamicData();
                else
                    TextEditorResourceManager.PersistAtlasTextures(fontAsset);
            }

            return paths;
        }
    }

    class TextCoreAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(FontAsset))
                    continue;

                FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);

                // A reimport re-reads the emptied atlas but keeps the live tables, whose rects now point at nothing.
                if (fontAsset != null && fontAsset.hasSessionOnlyDynamicData && fontAsset.m_GlyphTable.Count > 0 && fontAsset.atlasTexture != null && fontAsset.atlasTexture.width <= 1)
                    fontAsset.ClearSessionDynamicData();
            }
        }
    }
}
