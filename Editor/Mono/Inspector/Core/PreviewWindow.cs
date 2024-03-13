// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class PreviewWindow : InspectorWindow
    {
        [SerializeField]
        private InspectorWindow m_ParentInspectorWindow;

        VisualElement m_previewElement;

        VisualElement previewElement => m_previewElement ?? (m_previewElement = rootVisualElement.Q(className: "unity-inspector-preview"));

        internal bool IsFloatingWindow => parent is { window.rootView: not null, window.showMode: not ShowMode.MainWindow };

        private readonly string k_PreviewName = "preview-container";
        internal override BindingLogLevel defaultBindingLogLevel => BindingLogLevel.None;
        public void SetParentInspector(InspectorWindow inspector)
        {
            m_ParentInspectorWindow = inspector;

            // Create tracker after parent inspector window has been set (case 829182, 846156)
            CreateTracker();
        }

        // It's important to NOT call the base.OnDestroy() here!
        // The InspectorWindow.OnDestroy() deletes the tracker if we are not using the
        // shared tracker. This makes sense when we are an InspectorWindow about to die,
        // but it does not make sense when we are a PreviewWindow sharing this tracker with
        // a perfectly not dead InspectorWindow. Killing the tracker used by a still-alive
        // InspectorWindow cause many problems.
        // case 1119612
        protected override void OnDestroy() {}

        protected override void OnEnable()
        {
            titleContent = EditorGUIUtility.TrTextContent("Preview");
            minSize = new Vector2(260, 220);

            AddInspectorWindow(this);
            var tpl = EditorGUIUtility.Load("UXML/InspectorWindow/PreviewWindow.uxml") as VisualTreeAsset;
            var container = tpl.Instantiate();
            container.AddToClassList(s_MainContainerClassName);
            rootVisualElement.hierarchy.Add(container);

            rootVisualElement.AddStyleSheetPath("StyleSheets/InspectorWindow/PreviewWindow.uss");
            RebuildContentsContainers();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_ParentInspectorWindow != null && GetInspectors().Contains(m_ParentInspectorWindow))
            {
                m_ParentInspectorWindow.hasFloatingPreviewWindow = false;
                m_ParentInspectorWindow.RebuildContentsContainers();
            }
        }

        protected override void CreateTracker()
        {
            if (m_ParentInspectorWindow != null)
                m_Tracker = m_ParentInspectorWindow.tracker;
            else if (m_Tracker == null)
                base.CreateTracker();
        }

        internal override Editor GetLastInteractedEditor()
        {
            if (m_ParentInspectorWindow == null)
                return null;

            return m_ParentInspectorWindow.GetLastInteractedEditor();
        }

        internal override void RebuildContentsContainers()
        {
            Editor.m_AllowMultiObjectAccess = true;
            var preview = previewElement;
            preview.Clear();
            CreatePreviewables();

            previewWindow = new InspectorPreviewWindow();
            IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
            IPreviewable editor = GetEditorThatControlsPreview(editorsWithPreviews);
            previewWindow = editor?.CreatePreview(previewWindow) as InspectorPreviewWindow;

            if (m_ParentInspectorWindow != null && previewWindow != null)
            {
                if (previewWindow.childCount == 0)
                {
                    PrepareToolbar(previewWindow, true);
                    UpdateLabel(previewWindow);
                    VisualElement previewPane = previewWindow.GetPreviewPane();

                    // IMGUI fallback
                    if (previewPane?.childCount == 0)
                    {
                        previewPane.Add(DrawPreview());
                    }
                }

                SetPreviewStyle(previewWindow);

                if (preview.Q(k_PreviewName) == null)
                    preview.Add(previewWindow);
            }
            else
            {
                var container = DrawPreview(true);
                SetPreviewStyle(container);

                if (preview.Q(k_PreviewName) == null)
                    preview.Add(container);
            }
        }

        void SetPreviewStyle(VisualElement element)
        {
            element.style.flexGrow = 1f;
            element.style.flexShrink = 0f;
            element.style.flexBasis = 0f;
            element.name = k_PreviewName;
        }

        IMGUIContainer DrawPreview(bool drawToolbar = false)
        {
            return new IMGUIContainer(() =>
            {
                IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
                IPreviewable editor = GetEditorThatControlsPreview(editorsWithPreviews);

                if (drawToolbar)
                {
                    Rect toolbarRect = EditorGUILayout.BeginHorizontal(GUIContent.none, EditorStyles.toolbar,
                        GUILayout.Height(kBottomToolbarHeight));
                    {
                        // Label
                        string label = string.Empty;
                        if ((editor != null))
                        {
                            label = editor.GetPreviewTitle().text;
                        }

                        GUILayout.Label(label, Styles.preToolbarLabel);

                        GUILayout.FlexibleSpace();

                        if (editor != null && editor.HasPreviewGUI())
                            editor.OnPreviewSettings();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                Rect previewPosition = GUILayoutUtility.GetRect(0, 10240, 64, 10240);

                // Draw background
                if (Event.current.type == EventType.Repaint)
                    Styles.preBackground.Draw(previewPosition, false, false, false, false);

                // Draw preview
                if (editor != null && editor.HasPreviewGUI())
                    editor.DrawPreview(previewPosition);
            });
        }

        public override void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Dock Preview to Inspector"), false, Close);
        }

        protected override void ShowButton(Rect r) {}

        internal override bool CanMaximize()
        {
            /*Since preview window is tightly coupled with Ispector window, maximizing this would destroy inspector
             * which internally closes all the windows tied with it which in this case would be this window so there
             * is no point in maximizing a winodw that will be closed as a part of maximizing*/
            return false;
        }
    }
}
