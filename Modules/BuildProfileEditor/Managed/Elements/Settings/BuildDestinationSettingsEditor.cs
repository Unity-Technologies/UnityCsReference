// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements;

[CustomEditor(typeof(BuildDestinationSettings))]
class BuildDestinationSettingsEditor : Editor
{
    const string k_Uxml = "BuildProfile/UXML/BuildDestinationSettings.uxml";
    const string k_StyleSheet = "BuildProfile/StyleSheets/BuildProfile.uss";

    SerializedProperty m_BuildPath;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
        var windowUss = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
        visualTree.CloneTree(root);
        root.styleSheets.Add(windowUss);

        m_BuildPath = serializedObject.FindProperty("m_BuildPath");

        root.Bind(serializedObject);

        var browseButton = root.Q<Button>("build-destination-browse-button");
        browseButton.text = TrText.buildDestinationBrowseButton;
        browseButton.clicked += () =>
        {
            var currentPath = ((BuildDestinationSettings)target).buildPath;
            var startFolder = currentPath;
            if (!string.IsNullOrEmpty(currentPath) && !Directory.Exists(currentPath))
            {
                var parent = Path.GetDirectoryName(currentPath);
                if (Directory.Exists(parent))
                    startFolder = parent;
            }

            var path = EditorUtility.OpenFolderPanel(TrText.buildDestinationFolderTitle, startFolder, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                m_BuildPath.stringValue = path;
                serializedObject.ApplyModifiedProperties();
            }
        };

        return root;
    }
}
