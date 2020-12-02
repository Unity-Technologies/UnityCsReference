using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class EditorElement : VisualElement, IEditorElement
    {
        readonly IPropertyView inspectorWindow;
        Editor[] m_EditorCache;

        private Editor[] PopulateCache()
        {
            m_EditorCache = inspectorWindow.tracker.activeEditors;
            return m_EditorCache;
        }

        Editor[] m_Editors
        {
            get
            {
                if (m_EditorCache == null || m_EditorIndex >= m_EditorCache.Length || !m_EditorCache[m_EditorIndex])
                {
                    PopulateCache();
                }
                return m_EditorCache;
            }
        }
        public IEnumerable<Editor> Editors => m_Editors.AsEnumerable();

        int m_EditorIndex;
        public Editor editor
        {
            get
            {
                if (m_EditorIndex < m_Editors.Length)
                {
                    return m_Editors[m_EditorIndex];
                }
                return null;
            }
        }

        private bool IsEditorValid()
        {
            if (m_EditorIndex < m_Editors.Length)
            {
                return m_Editors[m_EditorIndex];
            }
            return false;
        }

        Rect m_DragRect;
        Rect m_ContentRect;

        VisualElement m_PrefabElement;

        IMGUIContainer m_Header;
        internal InspectorElement m_InspectorElement { get; private set; }
        IMGUIContainer m_Footer;

        private bool m_WasVisible = false;

        static class Styles
        {
            public static GUIStyle importedObjectsHeaderStyle = new GUIStyle("IN BigTitle");

            static Styles()
            {
                importedObjectsHeaderStyle.font = EditorStyles.label.font;
                importedObjectsHeaderStyle.fontSize = EditorStyles.label.fontSize;
                importedObjectsHeaderStyle.alignment = TextAnchor.UpperLeft;
            }
        }

        internal EditorElement(int editorIndex, IPropertyView iw, bool isCulled = false)
        {
            m_EditorIndex = editorIndex;
            inspectorWindow = iw;
            pickingMode = PickingMode.Ignore;

            if (isCulled)
            {
                InitCulled();
                return;
            }

            Init();
        }

        void InitCulled()
        {
            PopulateCache();

            var container = inspectorWindow.CreateIMGUIContainer(() =>
            {
                if (editor != null)
                {
                    // Reset dirtiness when repainting, just like in EditorElement.HeaderOnGUI.
                    if (Event.current.type == EventType.Repaint)
                    {
                        editor.isInspectorDirty = false;
                    }
                }
            }, name);
            Add(container);
        }

        void Init()
        {
            var editors = PopulateCache();
            Object editorTarget = editor.targets[0];
            string editorTitle = ObjectNames.GetInspectorTitle(editorTarget);

            var inspectorElementMode = InspectorElement.GetModeFromInspectorMode(inspectorWindow.inspectorMode);
            if (inspectorWindow.useUIElementsDefaultInspector)
                inspectorElementMode &= ~(InspectorElement.Mode.IMGUIDefault);

            m_InspectorElement = new InspectorElement(editor, inspectorElementMode)
            {
                focusable = false
            };


            m_Header = BuildHeaderElement(editorTitle);
            m_Footer = BuildFooterElement(editorTitle);

            m_InspectorElement.name = editorTitle + "Inspector";
            m_InspectorElement.style.paddingBottom = InspectorWindow.kEditorElementPaddingBottom;

            if (EditorNeedsVerticalOffset(editors, editorTarget))
            {
                m_InspectorElement.style.overflow = Overflow.Hidden;
            }

            UpdateInspectorVisibility();

            Add(m_Header);
            // If the editor targets contain many target and the multi editing is not supported, we should not add this inspector.
            // However, the header and footer are kept since these are showing information regarding this state.
            if ((editor.targets.Length <= 1) || (inspectorWindow.IsMultiEditingSupported(editor, editor.target)))
            {
                Add(m_InspectorElement);
            }

            Add(m_Footer);
        }

        public void ReinitCulled(int editorIndex)
        {
            if (m_Header != null)
            {
                m_EditorIndex = editorIndex;
                m_Header = m_Footer = null;
                Clear();
                InitCulled();
                return;
            }

            PopulateCache();
        }

        public void Reinit(int editorIndex)
        {
            if (m_Header == null)
            {
                m_EditorIndex = editorIndex;
                Clear();
                Init();
                return;
            }

            PopulateCache();
            Object editorTarget = editor.targets[0];
            string editorTitle = ObjectNames.GetInspectorTitle(editorTarget);

            m_EditorIndex = editorIndex;

            m_Header.onGUIHandler = HeaderOnGUI;
            m_Footer.onGUIHandler = FooterOnGUI;
            m_InspectorElement.AssignExistingEditor(editor);

            name = editorTitle;
            m_InspectorElement.name = editorTitle + "Inspector";
            m_Header.name = editorTitle + "Header";
            m_Footer.name = editorTitle + "Footer";

            UpdateInspectorVisibility();
        }

        private void UpdateInspectorVisibility()
        {
            if (editor.CanBeExpandedViaAFoldoutWithoutUpdate())
            {
                m_Footer.style.marginTop = m_WasVisible ? 0 : -kFooterDefaultHeight;
                m_InspectorElement.style.paddingBottom = InspectorWindow.kEditorElementPaddingBottom;
            }
            else
            {
                m_Footer.style.marginTop = -kFooterDefaultHeight;
                m_InspectorElement.style.paddingBottom = 0;
            }
        }

        public void AddPrefabComponent(VisualElement comp)
        {
            if (m_PrefabElement != null)
            {
                m_PrefabElement.RemoveFromHierarchy();
                m_PrefabElement = null;
            }

            if (comp != null)
            {
                m_PrefabElement = comp;
                Insert(0, m_PrefabElement);
            }
        }

        #region Header

        IMGUIContainer BuildHeaderElement(string editorTitle)
        {
            //Create and IMGUIContainer to enclose the header
            // This also needs to validate the state of the editor tracker (stuff that was already done in the original DrawEditors
            var headerElement = inspectorWindow.CreateIMGUIContainer(HeaderOnGUI, editorTitle + "Header");
            return headerElement;
        }

        private static UQueryState<IMGUIContainer> ImguiContainersQuery = new UQueryBuilder<IMGUIContainer>(null).SingleBaseType().Build();


        internal static void InvalidateIMGUILayouts(VisualElement element)
        {
            if (element != null)
            {
                var q = ImguiContainersQuery.RebuildOn(element);
                q.ForEach(e => e.MarkDirtyLayout());
            }
        }

        void HeaderOnGUI()
        {
            var editors = PopulateCache();
            if (!IsEditorValid())
            {
                SetElementVisible(m_InspectorElement, false);
                return;
            }

            // Avoid drawing editor if native target object is not alive, unless it's a MonoBehaviour/ScriptableObject
            // We want to draw the generic editor with a warning about missing/invalid script
            // Case 891450:
            // - ActiveEditorTracker will automatically create editors for materials of components on tracked game objects
            // - UnityEngine.UI.Mask will destroy this material in OnDisable (e.g. disabling it with the checkbox) causing problems when drawing the material editor
            var target = editor.target;
            if (target == null && !NativeClassExtensionUtilities.ExtendsANativeType(target))
            {
                SetElementVisible(m_InspectorElement, false);
                return;
            }

            m_WasVisible = inspectorWindow.WasEditorVisible(editors, m_EditorIndex, target);

            GUIUtility.GetControlID(target.GetInstanceID(), FocusType.Passive);
            EditorGUIUtility.ResetGUIState();

            if (editor.target is AssetImporter)
                inspectorWindow.editorsWithImportedObjectLabel.Add(m_EditorIndex + 1);

            //set the current PropertyHandlerCache to the current editor
            ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;
            using (new InspectorWindowUtils.LayoutGroupChecker())
            {
                m_DragRect = DrawEditorHeader(editors, target, ref m_WasVisible);
            }

            if (GUI.changed)
            {
                // If the header changed something, we must trigger a layout calculating on imgui children
                // Fixes Material editor toggling layout issues (case 1148706)
                InvalidateIMGUILayouts(this);
            }

            if (m_WasVisible != IsElementVisible(m_InspectorElement))
            {
                SetElementVisible(m_InspectorElement, m_WasVisible);
            }

            UpdateInspectorVisibility();

            var multiEditingSupported = inspectorWindow.IsMultiEditingSupported(editor, target);

            if (!multiEditingSupported && m_WasVisible)
            {
                GUILayout.Label("Multi-object editing not supported.", EditorStyles.helpBox);
                return;
            }

            InspectorWindowUtils.DisplayDeprecationMessageIfNecessary(editor);

            // Reset dirtiness when repainting
            if (Event.current.type == EventType.Repaint)
            {
                editor.isInspectorDirty = false;
            }

            bool excludedClass = InspectorWindowUtils.IsExcludedClass(target);
            if (excludedClass)
                EditorGUILayout.HelpBox(
                    "The module which implements this component type has been force excluded in player settings. This object will be removed in play mode and from any builds you make.",
                    MessageType.Warning);

            if (IsElementVisible(m_InspectorElement))
            {
                m_ContentRect = m_InspectorElement.layout;
            }
            else
            {
                Rect r = m_Header.layout;
                r.y = r.y + r.height - 1;
                r.height = kFooterDefaultHeight;
                m_ContentRect = r;
            }
        }

        Rect DrawEditorHeader(Editor[] editors, Object target, ref bool wasVisible)
        {
            var largeHeader = DrawEditorLargeHeader(editors, ref wasVisible);

            // Dragging handle used for editor reordering
            var dragRect = largeHeader
                ? new Rect()
                : DrawEditorSmallHeader(editors, target, wasVisible);
            return dragRect;
        }

        bool DrawEditorLargeHeader(Editor[] editors, ref bool wasVisible)
        {
            if (!IsEditorValid())
            {
                return true;
            }

            bool largeHeader = InspectorWindow.EditorHasLargeHeader(m_EditorIndex, editors);

            // Draw large headers before we do the culling of unsupported editors below,
            // so the large header is always shown even when the editor can't be.
            if (largeHeader)
            {
                bool IsOpenForEdit = editor.IsOpenForEdit();
                wasVisible = true;

                if (inspectorWindow.editorsWithImportedObjectLabel.Contains(m_EditorIndex))
                {
                    var importedObjectBarRect = GUILayoutUtility.GetRect(16, 20);
                    importedObjectBarRect.height = 21;

                    var headerText = "Imported Object";
                    if (editors.Length > 1)
                    {
                        if (editors[0] is PrefabImporterEditor && editors[1] is GameObjectInspector)
                            headerText = "Root in Prefab Asset";
                    }

                    GUILayout.Label(headerText, Styles.importedObjectsHeaderStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(-7f); // Ensures no spacing between this header and the next header
                }

                // Header
                using (new EditorGUI.DisabledScope(!IsOpenForEdit)) // Only disable the entire header if the asset is locked by VCS
                {
                    editor.DrawHeader();
                }
            }

            return largeHeader;
        }

        // Draw small headers (the header above each component) after the culling above
        // so we don't draw a component header for all the components that can't be shown.
        Rect DrawEditorSmallHeader(Editor[] editors, Object target, bool wasVisible)
        {
            var currentEditor = editor;

            if (currentEditor == null)
                return GUILayoutUtility.GetLastRect();

            // ensure first component's title bar is flush with the header
            if (EditorNeedsVerticalOffset(editors, target))
            {
                // TODO: Check if we can fix this in the GameObjectInspector instead
                GUILayout.Space(
                    -3f // move back up so line overlaps
                    - EditorStyles.inspectorBig.margin.bottom -
                    EditorStyles.inspectorTitlebar.margin.top // move back up margins
                );
            }

            using (new EditorGUI.DisabledScope(!currentEditor.IsEnabled()))
            {
                bool isVisible = EditorGUILayout.InspectorTitlebar(wasVisible, currentEditor);

                if (wasVisible != isVisible)
                {
                    inspectorWindow.tracker.SetVisible(m_EditorIndex, isVisible ? 1 : 0);
                    InternalEditorUtility.SetIsInspectorExpanded(target, isVisible);
                    if (isVisible)
                    {
                        inspectorWindow.lastInteractedEditor = currentEditor;
                    }
                    else if (inspectorWindow.lastInteractedEditor == currentEditor)
                    {
                        inspectorWindow.lastInteractedEditor = null;
                    }
                }
            }
            return GUILayoutUtility.GetLastRect();
        }

        private bool IsElementVisible(VisualElement ve)
        {
            return (ve.resolvedStyle.display == DisplayStyle.Flex);
        }

        internal static void SetElementVisible(InspectorElement ve, bool visible)
        {
            if (visible)
            {
                ve.style.display = DisplayStyle.Flex;
                SetInspectorElementChildIMGUIContainerFocusable(ve, true);
            }
            else
            {
                ve.style.display = DisplayStyle.None;
                SetInspectorElementChildIMGUIContainerFocusable(ve, false);
            }
        }

        static void SetInspectorElementChildIMGUIContainerFocusable(InspectorElement ve, bool focusable)
        {
            var childCount = ve.childCount;

            for (int i = 0; i < childCount; ++i)
            {
                var child = ve[i];
                if (child.isIMGUIContainer)
                {
                    var imguiContainer = (IMGUIContainer)child;
                    imguiContainer.focusable = focusable;
                }
            }
        }

        #endregion Header

        #region Footer
        const float kFooterDefaultHeight = 5;
        IMGUIContainer BuildFooterElement(string editorTitle)
        {
            IMGUIContainer footerElement = inspectorWindow.CreateIMGUIContainer(FooterOnGUI, editorTitle + "Footer");
            footerElement.style.height = kFooterDefaultHeight;
            return footerElement;
        }

        void FooterOnGUI()
        {
            var editors = m_EditorCache;
            var ed = editor;

            if (ed == null)
            {
                return;
            }

            m_ContentRect.y = -m_ContentRect.height;
            inspectorWindow.editorDragging.HandleDraggingToEditor(editors, m_EditorIndex, m_DragRect, m_ContentRect);
            HandleComponentScreenshot(m_ContentRect, ed);

            var target = ed.target;
            var comp = target as Component;

            if (EditorGUI.ShouldDrawOverrideBackground(ed.targets, Event.current, comp))
            {
                var rect = GUILayoutUtility.kDummyRect;
                bool wasVisible = inspectorWindow.WasEditorVisible(editors, m_EditorIndex, target);
                // if the inspector is currently visible then the override background drawn by the footer needs to be slightly larger than if the inspector is collapsed
                if (wasVisible)
                {
                    rect.y -= 1;
                    rect.height += 1;
                }
                else
                {
                    rect.y += 1;
                    rect.height -= 1;
                }

                EditorGUI.DrawOverrideBackground(rect, true);
            }
        }

        void HandleComponentScreenshot(Rect content, Editor editor)
        {
            if (ScreenShots.s_TakeComponentScreenshot)
            {
                content.yMin -= 16;
                if (content.Contains(Event.current.mousePosition))
                {
                    Rect globalComponentRect = GUIClip.Unclip(content);
                    globalComponentRect.position =
                        globalComponentRect.position + inspectorWindow.parent.screenPosition.position;
                    ScreenShots.ScreenShotComponent(globalComponentRect, editor.target);
                }
            }
        }

        #endregion Footer

        internal bool EditorNeedsVerticalOffset(Editor[] editors, Object target)
        {
            return m_EditorIndex > 0 && IsEditorValid() && editors[m_EditorIndex - 1].target is GameObject && target is Component;
        }
    }
}
