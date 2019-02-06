// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore;
using System.IO;


namespace UnityEditor.TextCore
{
    static class TextSettingsCreationMenu
    {
        [MenuItem("Assets/Create/TextCore/Text Settings", false, 200, true)]
        public static void CreateTextSettingsAsset()
        {
            Object target = Selection.activeObject;

            // Make sure the selection is a font file
            if (target == null)
            {
                //Debug.LogWarning("A Font file must first be selected in order to create a Font Asset.");
                return;
            }

            string targetPath = AssetDatabase.GetAssetPath(target);
            string folderPath = Path.GetDirectoryName(targetPath);
            string newAssetFilePathWithName = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/Text Settings.asset");

            //// Create new TM Font Asset.
            TextSettings textSettings = ScriptableObject.CreateInstance<TextSettings>();
            AssetDatabase.CreateAsset(textSettings, newAssetFilePathWithName);

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(textSettings);

            AssetDatabase.SaveAssets();
        }
    }
}
