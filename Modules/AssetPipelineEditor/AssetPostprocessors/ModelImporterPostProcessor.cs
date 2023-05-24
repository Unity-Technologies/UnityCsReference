// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class ModelImporterPostProcessor : AssetPostprocessor
    {
        private static bool IsTypeOf(System.Type query, System.Type desiredType)
        {
            return query != null && (query.IsSubclassOf(desiredType) || query == desiredType);
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
            if (!EditorPrefs.GetBool("PerformBumpMapChecks",true))
                return;

            if (UnityEditor.Experimental.AssetDatabaseExperimental.ActiveOnDemandMode != UnityEditor.Experimental.AssetDatabaseExperimental.OnDemandMode.Off || UnityEditor.Experimental.AssetDatabaseExperimental.VirtualizationEnabled)
            {
                // This PostProcessAllAssets will forcefully import everything in on-demand mode
                // We need to find a better way of filtering on asset type without forcing an import
                return;
            }

            List<Object> loadedAssets = new List<Object>();
            bool oneFound = false;
            try
            {
                System.Type modelImporterType = typeof(ModelImporter);
                System.Type speedTreeImporterType = typeof(SpeedTreeImporter);

                foreach (var assetPath in importedAssets)
                {
                    var importer = AssetDatabase.GetImporterType(assetPath);
                    var isMaterialAsset = assetPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase);
                    if (isMaterialAsset == true || IsTypeOf(importer, modelImporterType) || IsTypeOf(importer, speedTreeImporterType))
                    {
                        if (!AssetDatabase.IsMainAssetAtPathLoaded(assetPath))
                        {
                            loadedAssets.AddRange(AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                .Where(o => !(o is GameObject || o is Component || o is MonoBehaviour || o is ScriptableObject)));
                        }

                        var embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Material>();
                        foreach (var material in embeddedMaterials)
                        {
                            oneFound = true;
                            BumpMapSettings.PerformBumpMapCheck(material);
                        }
                    }
                }
            }
            finally
            {
                if (oneFound && !HasRegisteredOpenBumpMapCheckWindow())
                {
                    // We cannot open the BumpMapTexturesWindow here because the Editor Layout may not have been loaded yet
                    // and will destroy the window when doing so. So lets wait for the first frame to open it.
                    EditorApplication.update += OpenBumpMapCheckWindow;
                }
                foreach (var o in loadedAssets)
                {
                    Resources.UnloadAsset(o);
                }
            }
        }

        static bool HasRegisteredOpenBumpMapCheckWindow()
        {
            var invocationList = EditorApplication.update.GetInvocationList();
            for (int i = 0; i < invocationList.Length; ++i)
            {
                if (invocationList[i] == OpenBumpMapCheckWindow)
                    return true;
            }

            return false;
        }

        static void OpenBumpMapCheckWindow()
        {
            EditorApplication.update -= OpenBumpMapCheckWindow;
            InternalEditorUtility.PerformUnmarkedBumpMapTexturesFixing();
        }
    }
}
