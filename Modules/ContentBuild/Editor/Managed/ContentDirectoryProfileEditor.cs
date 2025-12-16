// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Build;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using System.IO;
using System;

namespace UnityEditor.Build.Content
{
    [CustomEditor(typeof(ContentDirectoryProfile))]
    internal sealed class ContentDirectoryProfileEditor : Editor
    {
        private SerializedProperty outputPathProperty;
        private SerializedProperty rootAssetsProperty;
        private SerializedProperty optionsProperty;
        private SerializedProperty compressionTypeProperty;
        private SerializedProperty targetPlatformProperty;
        private SerializedProperty subtargetProperty;
        private SerializedProperty extraScriptingDefinesProperty;

        private static readonly string kProjectRootPath = Directory.GetParent(Application.dataPath).FullName;

        private void OnEnable()
        {
            outputPathProperty = serializedObject.FindProperty("outputPath");
            rootAssetsProperty = serializedObject.FindProperty("rootAssets");
            optionsProperty = serializedObject.FindProperty("options");
            compressionTypeProperty = serializedObject.FindProperty("compressionType");
            targetPlatformProperty = serializedObject.FindProperty("targetPlatform");
            subtargetProperty = serializedObject.FindProperty("subtarget");
            extraScriptingDefinesProperty = serializedObject.FindProperty("extraScriptingDefines");
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // Output Path
            var outputPathContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            outputPathContainer.Add(new PropertyField(outputPathProperty, "Output Path") { style = { flexGrow = 1 } });
            outputPathContainer.Add(new Button(OnBrowseButtonClicked) { text = "Browse" });
            root.Add(outputPathContainer);

            root.Add(new PropertyField(rootAssetsProperty));
            root.Add(new PropertyField(optionsProperty));
            root.Add(new PropertyField(targetPlatformProperty));
            root.Add(new PropertyField(compressionTypeProperty));
            root.Add(new PropertyField(subtargetProperty));
            root.Add(new PropertyField(extraScriptingDefinesProperty));
            root.Add(new Button(OnBuildButtonClicked) { text = "Build Content Directory" });

            root.Bind(serializedObject);
            return root;
        }

        static private bool IsSubfolder(string subfolder, string parentFolder)
        {
            var parentFolderInfo = new DirectoryInfo(parentFolder).FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var subfolderInfo = new DirectoryInfo(subfolder).FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            return subfolderInfo.StartsWith(parentFolderInfo, StringComparison.OrdinalIgnoreCase);
        }

        private void OnBrowseButtonClicked()
        {
            var profile = (ContentDirectoryProfile)target;
            var outputPath = profile.outputPath;
            string displayPath = Directory.Exists(outputPath) ? outputPath : kProjectRootPath;
            string path = EditorUtility.SaveFolderPanel("Choose Content Directory output location", displayPath, displayPath);
            if (!string.IsNullOrEmpty(path))
            {
                Undo.RecordObject(profile, "Modify ContentDirectoryProfile output path");
                if (IsSubfolder(path, kProjectRootPath))
                {
                    profile.outputPath = Path.GetRelativePath(kProjectRootPath, path);
                }
                else
                {
                    profile.outputPath = path;
                }
                EditorUtility.SetDirty(profile);
            }
        }

        private void OnBuildButtonClicked()
        {
            serializedObject.ApplyModifiedProperties();
            var rootAssetListSO = (ContentDirectoryProfile)target;

            if (string.IsNullOrEmpty(outputPathProperty.stringValue))
            {
                string oneTimeOutputPath = EditorUtility.SaveFolderPanel("Choose Content Directory output location", kProjectRootPath, kProjectRootPath);
                if (!string.IsNullOrEmpty(oneTimeOutputPath))
                {
                    rootAssetListSO.BuildContentDirectory(oneTimeOutputPath);
                }
            }
            else
            {
                rootAssetListSO.BuildContentDirectory();
            }
        }
    }
}
