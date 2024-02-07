// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using System.IO;
using Unity.Collections;

namespace UnityEngine.TextCore.Text
{
    [InitializeOnLoad]
    internal class ICUDataAssetUtilities
    {
        private static string k_ICUDataAssetPath = "Assets/UI Toolkit/icudt73l.bytes";

        internal static void CreateAsset()
        {
            var directory = Path.GetDirectoryName(k_ICUDataAssetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filePath = Path.Combine(EditorApplication.applicationContentsPath, "Resources/icudt73l.dat");
            File.Copy(filePath, k_ICUDataAssetPath, true);
            AssetDatabase.ImportAsset(k_ICUDataAssetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static ICUDataAssetUtilities()
        {
            TextLib.GetICUAssetEditorDelegate = GetICUAsset;
        }

        internal static UnityEngine.TextAsset GetICUAsset()
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(k_ICUDataAssetPath);
        }
    }
}
