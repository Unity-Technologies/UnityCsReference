// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class PreviewWindow : InspectorWindow
    {
        [SerializeField]
        private InspectorWindow m_ParentInspectorWindow;

        VisualElement m_previewElement;

        VisualElement previewElement => m_previewElement ?? (m_previewElement = rootVisualElement?.Q(className: "unity-inspector-preview"));

        internal bool IsFloatingWindow => parent is { window.rootView: not null, window.showMode: not ShowMode.MainWindow };

        private readonly string k_PreviewName = "preview-container";
        internal override BindingLogLevel defaultBindingLogLevel => BindingLogLevel.None;
        public void SetParentInspector(InspectorWindow inspector)
        {
            m_SelectedPreview = null;
            m_ParentInspectorWindow = inspector;

            ClearPreviewables();
            CreatePreviewables();

            // If the parent inspector has a selected preview, we update the preview window accordingly
            var preview = m_ParentInspectorWindow.selectedPreview;
            if (preview != null && preview is not Editor)
            {
                using var _ = ListPool<IPreviewable>.Get(out var previewsInWindow);
                GetEditorsWithPreviews(previewsInWindow);
                var matchedPreview = previewsInWindow.Find(p => ReferenceEquals(p, preview) ||
                                                                (p.GetType() == preview.GetType() && p.target == preview.target));
                if (matchedPreview != null)
                    m_SelectedPreview = matchedPreview;
            }

            // Create tracker after parent inspector window has been set (case 829182, 846156)
            CreateTracker();

            // Deregistering the callback if not used yet to avoid to duplicate the call
            EditorApplication.update -= RebuildContentsContainers;
            InitPreview();
        }

        // It's important to NOT call the base.OnDestroy() here!
        // The InspectorWindow.OnDestroy() deletes the tracker if we are not using the
        // shared tracker. This makes sense when we are an InspectorWindow about to die,
        // but it does not make sense when we are a PreviewWindow sharing this tracker with
        // a perfectly not dead InspectorWindow. Killing the tracker used by a still-alive
        // InspectorWindow cause many problems.
        // case 1119612
        protected override void OnDestroy()
        {
            ClearPreviewables();
        }

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

            EditorApplication.update += RebuildContentsContainers;
        }

        protected override void OnDisable()
        {
            // Safeguard if the window is closed before the callback has been unregistered
            EditorApplication.update -= RebuildContentsContainers;
            base.OnDisable();
            ClearPreviewables();
            if (m_ParentInspectorWindow != null && GetInspectors().Contains(m_ParentInspectorWindow))
            {
                m_ParentInspectorWindow.SetPreviewPopOutStateAndRebuild(false);
                // If the dropdown is displayed, sync the inspector dropdown to the detached one
                var dropdown = m_PreviewRootElement?.GetDropdown();
                if(dropdown != null && dropdown.style.display.value == DisplayStyle.Flex)
                    m_ParentInspectorWindow.SetSelectedPreviewIndex(m_PreviewRootElement.GetDropdown().index);
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
            EditorApplication.update -= RebuildContentsContainers;

            ClearPreviewables();
            InitPreview();
        }

        void InitPreview()
        {
            Editor.m_AllowMultiObjectAccess = true;

            var preview = previewElement;
            preview.Clear();
            CreatePreviewables();

            m_PreviewRootElement = new PreviewRootElement();
            IPreviewable editor = m_ParentInspectorWindow != null
                ? m_ParentInspectorWindow.selectedPreview
                : GetEditorThatControlsPreview();
            var previewItem = editor?.CreatePreview(m_PreviewRootElement);

            m_HasPreview = editor != null && editor.HasPreviewGUI();

            if (m_ParentInspectorWindow != null && previewItem is PreviewRootElement)
            {
                PrepareToolbar(true);
                UpdateHeader();
                VisualElement previewPane = m_PreviewRootElement.GetPreviewPane();

                // IMGUI fallback
                if (previewPane?.childCount == 0)
                {
                    previewPane.Add(DrawPreview());
                }

                SetPreviewStyle(m_PreviewRootElement);

                if (preview.Q(k_PreviewName) == null)
                    preview.Add(m_PreviewRootElement);
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

        protected override void ResetPreviewContainer()
        {
            InitPreview();
        }

        protected override void OnPreviewSelected(object userData, string[] options, int selected)
        {
            var availablePreviews = (IPreviewable[])userData;
            m_SelectedPreview = availablePreviews[selected];

            //Only difference with the parent class, the selected preview needs to be updated here
            if (m_ParentInspectorWindow != null)
                m_ParentInspectorWindow.SetSelectedPreviewIndex(selected);

            // In case of mixed UITK / IMGUI preview we need to rebuild the review window content
            ResetPreviewContainer();
        }

        protected override void OnPreviewDropdownChanged(ChangeEvent<string> evt)
        {
            var dropdown = evt.elementTarget as DropdownField;
            if (dropdown == null)
                return;

            using var _ = ListPool<IPreviewable>.Get(out var editorsWithPreviews);
            GetEditorsWithPreviews(editorsWithPreviews);

            int selectedIndex = dropdown.index;
            if (selectedIndex >= 0 && selectedIndex < editorsWithPreviews.Count)
            {
                m_SelectedPreview = editorsWithPreviews[selectedIndex];

                //Only difference with the parent class, the selected preview needs to be updated here
                if (m_ParentInspectorWindow != null)
                    m_ParentInspectorWindow.SetSelectedPreviewIndex(dropdown.index);

                // In case of mixed UITK / IMGUI preview we need to rebuild the review window content
                ResetPreviewContainer();
            }
        }

        IMGUIContainer DrawPreview(bool drawToolbar = false)
        {
            return new IMGUIContainer(() =>
            {
                using var _ = ListPool<IPreviewable>.Get(out var editorsWithPreviews);
                GetEditorsWithPreviews(editorsWithPreviews);
                IPreviewable previewEditor = GetEditorThatControlsPreview(editorsWithPreviews);
                if (previewEditor == null)
                    return;

                if (drawToolbar)
                {
                    Rect toolbarRect = EditorGUILayout.BeginHorizontal(GUIContent.none, EditorStyles.toolbar,
                        GUILayout.Height(kBottomToolbarHeight));
                    {
                        //If we have more than one component with Previews, show a DropDown menu.
                        if (editorsWithPreviews.Count > 1)
                        {
                            GUIContent pTitle;
                            if (m_HasPreview)
                                pTitle = previewEditor.GetPreviewTitle() ?? Styles.preTitle;
                            else
                                pTitle = Styles.labelTitle;

                            if (GUILayout.Button(pTitle, Styles.preDropDown))
                            {
                                GUIContent[] panelOptions = new GUIContent[editorsWithPreviews.Count];
                                int selectedPreviewIndex = -1;
                                for (int index = 0; index < editorsWithPreviews.Count; index++)
                                {
                                    IPreviewable currentEditor = editorsWithPreviews[index];
                                    GUIContent previewTitle = currentEditor.GetPreviewTitle() ?? Styles.preTitle;

                                    string fullTitle;
                                    if (previewTitle == Styles.preTitle)
                                    {
                                        string componentTitle = ObjectNames.GetTypeName(currentEditor.target);
                                        if (NativeClassExtensionUtilities.ExtendsANativeType(currentEditor.target))
                                        {
                                            componentTitle = MonoScript.FromScriptedObject(currentEditor.target).GetClass()
                                                .Name;
                                        }

                                        fullTitle = previewTitle.text + " - " + componentTitle;
                                    }
                                    else
                                    {
                                        fullTitle = previewTitle.text;
                                    }

                                    panelOptions[index] = new GUIContent(fullTitle);
                                    if (editorsWithPreviews[index] == previewEditor)
                                        selectedPreviewIndex = index;
                                }

                                var foldoutRect = GUILayoutUtility.GetLastRect();
                                EditorUtility.DisplayCustomMenu(foldoutRect, panelOptions, selectedPreviewIndex,
                                    OnPreviewSelected, editorsWithPreviews.ToArray());
                            }
                            GUILayout.FlexibleSpace();
                        }
                        else
                        {
                            // Label
                            GUILayout.Label(previewEditor.GetPreviewTitle(), Styles.preToolbarLabel);
                            GUILayout.FlexibleSpace();
                        }

                        if (previewEditor != null && previewEditor.HasPreviewGUI())
                            previewEditor.OnPreviewSettings();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                Rect previewPosition = GUILayoutUtility.GetRect(0, 10240, 64, 10240);

                // Draw background
                if (Event.current.type == EventType.Repaint)
                    Styles.preBackground.Draw(previewPosition, false, false, false, false);

                // Draw preview
                if (previewEditor != null && previewEditor.HasPreviewGUI())
                    previewEditor.DrawPreview(previewPosition);
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
