using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class UITKTextSettingsCreationMenu
    {
        [MenuItem("Assets/Create/UI Toolkit/Text Settings", false)]
        public static void CreateUITKTextSettingsAsset()
        {
            string filePath;
            if (Selection.assetGUIDs.Length == 0)
            {
                // No asset selected.
                filePath = "Assets";
            }
            else
            {
                // Get the path of the selected folder or asset.
                filePath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);

                // Get the file extension of the selected asset as it might need to be removed.
                string fileExtension = Path.GetExtension(filePath);
                if (fileExtension != "")
                {
                    filePath = Path.GetDirectoryName(filePath);
                }
            }

            string filePathWithName = AssetDatabase.GenerateUniqueAssetPath(filePath + "/UITK Text Settings.asset");

            // Create new TextSettings asset
            PanelTextSettings textSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
            AssetDatabase.CreateAsset(textSettings, filePathWithName);

            // Not sure if this is still necessary in newer versions of Unity.
            EditorUtility.SetDirty(textSettings);

            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(textSettings);
        }
    }
}
