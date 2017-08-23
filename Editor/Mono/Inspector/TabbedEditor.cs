// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.AssetImporters;
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

    // Version for AssetImporterInspector derived editors
    internal abstract class AssetImporterTabbedEditor : AssetImporterEditor
    {
        protected string[] m_TabNames = null;
        private int m_ActiveEditorIndex = 0;

        /// <summary>
        /// The list of child inspectors.
        /// </summary>
        private BaseAssetImporterTabUI[] m_Tabs = null;
        public BaseAssetImporterTabUI activeTab { get; private set; }
        protected BaseAssetImporterTabUI[] tabs { get { return m_Tabs; } set { m_Tabs = value; } }

        public override void OnEnable()
        {
            foreach (var tab in m_Tabs)
            {
                tab.OnEnable();
            }

            m_ActiveEditorIndex = EditorPrefs.GetInt(this.GetType().Name + "ActiveEditorIndex", 0);
            if (activeTab == null)
                activeTab = m_Tabs[m_ActiveEditorIndex];
        }

        void OnDestroy()
        {
            if (m_Tabs != null)
            {
                foreach (var tab in m_Tabs)
                {
                    tab.OnDestroy();
                }

                // destroy all the child tabs
                m_Tabs = null;
                activeTab = null;
            }
        }

        protected override void ResetValues()
        {
            base.ResetValues();

            if (m_Tabs != null)
            {
                foreach (var tab in m_Tabs)
                {
                    tab.ResetValues();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // Always allow user to switch between tabs even when the editor is disabled, so they can look at all parts
            // of read-only assets
            using (new EditorGUI.DisabledScope(false)) // this doesn't enable the UI, but it seems correct to push the stack
            {
                GUI.enabled = true;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        m_ActiveEditorIndex = GUILayout.Toolbar(m_ActiveEditorIndex, m_TabNames, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                        if (check.changed)
                        {
                            EditorPrefs.SetInt(GetType().Name + "ActiveEditorIndex", m_ActiveEditorIndex);
                            activeTab = m_Tabs[m_ActiveEditorIndex];

                            activeTab.OnInspectorGUI();
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            // the activeTab can get destroyed when opening particular sub-editors (such as the Avatar configuration editor on the Rig tab)
            if (activeTab != null)
            {
                activeTab.OnInspectorGUI();
            }

            // show a single Apply/Revert set of buttons for all the tabs
            ApplyRevertGUI();
        }

        public override void OnPreviewSettings()
        {
            if (activeTab != null)
            {
                activeTab.OnPreviewSettings();
            }
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (activeTab != null)
            {
                activeTab.OnInteractivePreviewGUI(r, background);
            }
        }

        public override bool HasPreviewGUI()
        {
            if (activeTab == null)
                return false;
            return activeTab.HasPreviewGUI();
        }

        protected override void Apply()
        {
            if (m_Tabs != null)
            {
                // tabs can do work before or after the application of changes in the serialization object
                foreach (var tab in m_Tabs)
                {
                    tab.PreApply();
                }

                base.Apply();

                foreach (var tab in m_Tabs)
                {
                    tab.PostApply();
                }
            }
        }
    }
}
