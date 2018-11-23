// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor
{
    internal class PreviewWindow : InspectorWindow
    {
        [SerializeField]
        private InspectorWindow m_ParentInspectorWindow;

        VisualElement m_previewElement;

        VisualElement previewElement => m_previewElement ?? (m_previewElement = rootVisualContainer.Q<VisualElement>("preview"));

        public void SetParentInspector(InspectorWindow inspector)
        {
            m_ParentInspectorWindow = inspector;

            // Create tracker after parent inspector window has been set (case 829182, 846156)
            CreateTracker();
        }

        protected override void OnEnable()
        {
            titleContent = EditorGUIUtility.TrTextContent("Preview");
            minSize = new Vector2(260, 220);

            AddInspectorWindow(this);
            var tpl = EditorGUIUtility.Load("UXML/InspectorWindow/PreviewWindow.uxml") as VisualTreeAsset;
            var container = tpl.CloneTree(null);
            container.AddToClassList("mainContainer");
            rootVisualContainer.shadow.Add(container);

            rootVisualContainer.AddStyleSheetPath("StyleSheets/InspectorWindow/PreviewWindow.uss");

            RebuildContentsContainers();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_ParentInspectorWindow.RebuildContentsContainers();
        }

        protected override void CreateTracker()
        {
            if (m_ParentInspectorWindow != null)
                m_Tracker = m_ParentInspectorWindow.tracker;
        }

        internal override Editor GetLastInteractedEditor()
        {
            return m_ParentInspectorWindow.GetLastInteractedEditor();
        }

        internal override void RebuildContentsContainers()
        {
            var preview = previewElement;
            preview.Clear();
            var container = new IMGUIContainer(() =>
            {
                CreatePreviewables();
                DrawPreview();
            });
            container.style.flexGrow = 1f;
            container.style.flexShrink = 0f;
            container.style.flexBasis = 0f;

            preview.Add(container);
        }

        protected void DrawPreview()
        {
            if (m_ParentInspectorWindow == null)
            {
                Close();
                EditorGUIUtility.ExitGUI();
            }

            Editor.m_AllowMultiObjectAccess = true;

            // Do we have an editor that supports previews? Null if not.
            IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
            IPreviewable editor = GetEditorThatControlsPreview(editorsWithPreviews);

            bool hasPreview = (editor != null) && editor.HasPreviewGUI();

            // Toolbar
            Rect toolbarRect = EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.preToolbar, GUILayout.Height(kBottomToolbarHeight));
            {
                GUILayout.FlexibleSpace();
                var labelRect = GUILayoutUtility.GetLastRect();
                // Label
                string label = string.Empty;
                if ((editor != null))
                {
                    label = editor.GetPreviewTitle().text;
                }
                GUI.Label(labelRect, label, Styles.preToolbar2);
                if (hasPreview)
                    editor.OnPreviewSettings();
            } EditorGUILayout.EndHorizontal();


            Event evt = Event.current;
            if (evt.type == EventType.MouseUp && evt.button == 1 && toolbarRect.Contains(evt.mousePosition))
            {
                Close();
                evt.Use();
                // Don't draw preview if we just closed this window
                return;
            }

            // Preview
            Rect previewPosition = GUILayoutUtility.GetRect(0, 10240, 64, 10240);

            // Draw background
            if (Event.current.type == EventType.Repaint)
                Styles.preBackground.Draw(previewPosition, false, false, false, false);

            // Draw preview
            if ((editor != null) && editor.HasPreviewGUI())
                editor.DrawPreview(previewPosition);
        }

        public override void AddItemsToMenu(GenericMenu menu) {}

        protected override void ShowButton(Rect r) {}
    }
}
