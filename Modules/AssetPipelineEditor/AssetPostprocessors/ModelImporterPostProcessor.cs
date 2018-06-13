// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class ModelImporterPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
            List<Object> loadedAssets = new List<Object>();
            try
            {
                foreach (var assetPath in importedAssets)
                {
                    var importer = AssetImporter.GetAtPath(assetPath);
                    var isMaterialAsset = Path.GetExtension(assetPath) == ".mat";
                    if (isMaterialAsset == true || importer is ModelImporter || importer is SpeedTreeImporter)
                    {
                        if (!AssetDatabase.IsMainAssetAtPathLoaded(assetPath))
                        {
                            loadedAssets.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                .Where(o => !(o is GameObject || o is Component || o is MonoBehaviour || o is ScriptableObject)));
                        }

                        var embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Material>();
                        foreach (var material in embeddedMaterials)
                        {
                            BumpMapSettings.PerformBumpMapCheck(material);
                        }
                    }
                }
            }
            finally
            {
                foreach (var o in loadedAssets)
                {
                    Resources.UnloadAsset(o);
                }
            }
        }
    }
}
