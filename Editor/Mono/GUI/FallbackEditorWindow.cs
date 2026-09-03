// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIControls not yet converted
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

namespace UnityEditor
{
    internal class FallbackEditorWindow : EditorWindow
    {
        FallbackEditorWindow()
        {
        }

        void OnEnable()
        {
            titleContent = EditorGUIUtility.TrTextContent("Failed to load");
        }

        void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("EditorWindow could not be loaded because the script is not found in the project", "WordWrapLabel");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
} // namespace
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
