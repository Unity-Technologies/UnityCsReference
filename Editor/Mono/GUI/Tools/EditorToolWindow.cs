// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.EditorTools
{
    [CustomEditor(typeof(EditorTool), true)]
    class EditorToolCustomEditor : Editor
    {
        const string k_GeneratorAssetProperty = "m_GeneratorAsset";
        const string k_ScriptProperty = "m_Script";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var property = serializedObject.GetIterator();

            bool expanded = true;

            while (property.NextVisible(expanded))
            {
                if (property.propertyPath == k_GeneratorAssetProperty)
                    continue;

                using (new EditorGUI.DisabledScope(property.propertyPath == k_ScriptProperty))
                {
                    EditorGUILayout.PropertyField(property, true);
                }

                expanded = false;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    sealed class EditorToolWindow : EditorWindow
    {
        static class Styles
        {
            public static GUIContent title = EditorGUIUtility.TrTextContent("Editor Tool");
        }

        Editor m_Editor;

        EditorToolWindow() {}

        [MenuItem("Window/General/Active Tool")]
        static void ShowEditorToolWindow()
        {
            GetWindow<EditorToolWindow>();
        }

        void OnEnable()
        {
            EditorTools.activeToolChanged += ToolChanged;

            //active tool is null when opening the editor but activeToolChanged will be called soon after.
            //This is quick enough that the user shouldn't notice.
            if (EditorToolContext.activeTool)
                ToolChanged();
        }

        void OnDisable()
        {
            EditorTools.activeToolChanged -= ToolChanged;

            if (m_Editor != null)
                DestroyImmediate(m_Editor);
        }

        void ToolChanged()
        {
            if (m_Editor != null)
                DestroyImmediate(m_Editor);
            var activeTool = EditorToolContext.activeTool;
            m_Editor = Editor.CreateEditor(activeTool);
            titleContent = new GUIContent(EditorToolUtility.GetToolName(activeTool.GetType()));
            Repaint();
        }

        void OnGUI()
        {
            if (m_Editor != null)
                m_Editor.OnInspectorGUI();
        }
    }
}
