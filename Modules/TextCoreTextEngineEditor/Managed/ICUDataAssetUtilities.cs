// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System.IO;

namespace UnityEngine.TextCore.Text
{
    [InitializeOnLoad]
    internal class ICUDataAssetUtilities
    {
        private static string assetPath = "Assets/UI Toolkit/icudt73l.bytes";

        internal static void CreateAsset()
        {
            var filePath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/icudt73l.dat");
            File.Copy(filePath, assetPath, true);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static ICUDataAssetUtilities()
        {
            TextLib.GetICUAssetEditorDelegate = GetICUAsset;
        }

        internal static UnityEngine.TextAsset GetICUAsset()
        {
            //Try any ICU data asset, if none found check at the default path
            foreach(var path in AssetDatabase.FindAssets("t:" + typeof(UnityEngine.TextAsset).FullName))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(AssetDatabase.GUIDToAssetPath(path));
                if (asset != null)
                {
                    return asset;
                }
            }
            return AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(assetPath);
        }
    }
}
