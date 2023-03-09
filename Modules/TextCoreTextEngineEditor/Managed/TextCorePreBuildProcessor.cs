// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.TextCore.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.TextCore.Text
{
    internal class TextCorePreBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Find all font assets in the project
            string searchPattern = "t:FontAsset";
            string[] fontAssetGUIDs = AssetDatabase.FindAssets(searchPattern);

            for (int i = 0; i < fontAssetGUIDs.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(fontAssetGUIDs[i]);

                // Exclude assets not located in the project.
                if (!assetPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);

                if (fontAsset != null && (fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic || fontAsset.atlasPopulationMode == AtlasPopulationMode.DynamicOS) && fontAsset.clearDynamicDataOnBuild && fontAsset.atlasTexture.width != 0)
                {
                    fontAsset.ClearCharacterAndGlyphTablesInternal();
                }
            }
        }
    }
}
