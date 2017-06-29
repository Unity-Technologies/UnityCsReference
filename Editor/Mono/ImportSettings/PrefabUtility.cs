// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    public sealed partial class PrefabUtility
    {
        [RequiredByNativeCode]
        internal static void ExtractSelectedObjectsFromPrefab()
        {
            var assetsToReload = new HashSet<string>();
            string folder = null;
            foreach (var selectedObj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(selectedObj);

                // use the first selected element as the basis for the folder path where all the materials will be extracted
                if (folder == null)
                {
                    folder = EditorUtility.SaveFolderPanel("Select Materials Folder", FileUtil.DeleteLastPathNameComponent(path), "");
                    if (string.IsNullOrEmpty(folder))
                    {
                        // cancel the extraction if the user did not select a folder
                        return;
                    }

                    folder = FileUtil.GetProjectRelativePath(folder);
                }

                // TODO: [bogdanc 3/6/2017] if we want this function really generic, we need to know what extension the new asset files should have
                var extension = selectedObj is Material ? ".mat" : string.Empty;
                var newAssetPath = FileUtil.CombinePaths(folder, selectedObj.name) + extension;
                newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                var error = AssetDatabase.ExtractAsset(selectedObj, newAssetPath);
                if (string.IsNullOrEmpty(error))
                {
                    assetsToReload.Add(path);
                }
            }

            foreach (var path in assetsToReload)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
