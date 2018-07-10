// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.Experimental.EditorMode
{
    [CannotBeUnsupported]
    internal class EditorModeViewer : EditorWindow
    {
        [MenuItem("Window/Internal/Editor Modes", false, 1, true)]
        private static void ShowViewer()
        {
            var window = GetWindow<EditorModeViewer>();
            window.titleContent.text = "Editor Modes";
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(EditorGUIUtility.TempContent("Current Mode"), EditorGUIUtility.TempContent(EditorModes.CurrentModeName));
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (!(EditorModes.Current is DefaultEditorMode) && GUILayout.Button(EditorGUIUtility.TempContent($"Return to {EditorModes.DefaultMode.Name}")))
                {
                    EditorModes.RequestDefaultMode();
                }
                GUILayout.FlexibleSpace();
            }
        }
    }
}
