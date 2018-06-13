// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    // Version for editors not derived from AssetImporterEditor
    internal abstract class TabbedEditor : Editor
    {
        protected System.Type[] m_SubEditorTypes = null;
        protected string[] m_SubEditorNames = null;
        private int m_ActiveEditorIndex = 0;
        private Editor m_ActiveEditor;
        public Editor activeEditor { get { return m_ActiveEditor; } }

        internal virtual void OnEnable()
        {
            m_ActiveEditorIndex = EditorPrefs.GetInt(GetType().Name + "ActiveEditorIndex", 0);
            if (m_ActiveEditor == null)
                m_ActiveEditor = CreateEditor(targets, m_SubEditorTypes[m_ActiveEditorIndex]);
        }

        void OnDestroy()
        {
            DestroyImmediate(activeEditor);
        }

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_ActiveEditorIndex = GUILayout.Toolbar(m_ActiveEditorIndex, m_SubEditorNames, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                    if (check.changed)
                    {
                        EditorPrefs.SetInt(GetType().Name + "ActiveEditorIndex", m_ActiveEditorIndex);
                        var oldEditor = activeEditor;
                        m_ActiveEditor = null;
                        DestroyImmediate(oldEditor);
                        m_ActiveEditor = CreateEditor(targets, m_SubEditorTypes[m_ActiveEditorIndex]);
                    }
                }
                GUILayout.FlexibleSpace();
            }

            activeEditor.OnInspectorGUI();
        }

        public override void OnPreviewSettings()
        {
            activeEditor.OnPreviewSettings();
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            activeEditor.OnInteractivePreviewGUI(r, background);
        }

        public override bool HasPreviewGUI()
        {
            return activeEditor.HasPreviewGUI();
        }
    }
}
