// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    internal class EditorElement : VisualElement
    {
        readonly InspectorWindow inspectorWindow;

        Editor[] m_Editors => inspectorWindow.tracker.activeEditors;
        int m_editorIndex;
        public Editor editor => m_Editors[m_editorIndex];

        Rect m_DragRect;
        Rect m_ContentRect;

        VisualElement m_PrefabElement;

        IMGUIContainer m_Header;
        internal InspectorElement m_InspectorElement { get; private set; }
        IMGUIContainer m_Footer;

        internal EditorElement(int editorIndex, InspectorWindow iw)
        {
            m_editorIndex = editorIndex;
            inspectorWindow = iw;

            Init();

            Add(m_Header);
            Add(m_InspectorElement);
            Add(m_Footer);
        }

        void Init()
        {
            Object editorTarget = editor.targets[0];
            string editorTitle = ObjectNames.GetInspectorTitle(editorTarget);

            var inspectorElementMode = InspectorElement.GetModeFromInspectorMode(inspectorWindow.m_InspectorMode);
            if (inspectorWindow.m_UseUIElementsDefaultInspector)
                inspectorElementMode ^= InspectorElement.Mode.IMGUIDefault;

            m_InspectorElement = new InspectorElement(editor, inspectorElementMode)
            {
                focusable = false
            };


            m_Header = BuildHeaderElement(editorTitle);
            m_Footer = BuildFooterElement(editorTitle);

            m_InspectorElement.name = editorTitle + "Inspector";
            m_InspectorElement.style.paddingBottom = InspectorWindow.kEditorElementPaddingBottom;

            if (EditorNeedsVerticalOffset(editorTarget))
            {
                // This is madness
                m_InspectorElement.cacheAsBitmap = false;
                m_InspectorElement.style.overflow = Overflow.Hidden;
            }
        }

        internal void Reinit(int editorIndex)
        {
            m_editorIndex = editorIndex;

            m_Header.onGUIHandler = HeaderOnGUI;
            m_Footer.onGUIHandler = FooterOnGUI;
            m_InspectorElement.editor = editor;
        }

        internal void AddPrefabComponent(VisualElement comp)
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

        void HeaderOnGUI()
        {
            var editors = inspectorWindow.tracker.activeEditors;
            if (editors.Length <= m_editorIndex)
            {
                SetElementVisible(m_InspectorElement, false);
                return;
            }

            var target = editor.target;

            // Avoid drawing editor if native target object is not alive, unless it's a MonoBehaviour/ScriptableObject
            // We want to draw the generic editor with a warning about missing/invalid script
            // Case 891450:
            // - ActiveEditorTracker will automatically create editors for materials of components on tracked game objects
            // - UnityEngine.UI.Mask will destroy this material in OnDisable (e.g. disabling it with the checkbox) causing problems when drawing the material editor
            if (target == null && !NativeClassExtensionUtilities.ExtendsANativeType(target))
            {
                SetElementVisible(m_InspectorElement, false);
                return;
            }

            bool wasVisible = inspectorWindow.WasEditorVisible(editors, m_editorIndex, target);

            GUIUtility.GetControlID(target.GetInstanceID(), FocusType.Passive);
            EditorGUIUtility.ResetGUIState();

            if (editor.target is AssetImporter)
                inspectorWindow.editorsWithImportedObjectLabel.Add(m_editorIndex + 1);

            //set the current PropertyHandlerCache to the current editor
            ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;
            using (new InspectorWindowUtils.LayoutGroupChecker())
            {
                m_DragRect = DrawEditorHeader(target, ref wasVisible);
            }

            if (wasVisible != m_InspectorElement.visible)
            {
                SetElementVisible(m_InspectorElement, wasVisible);
            }

            var multiEditingSupported = inspectorWindow.IsMultiEditingSupported(editor, target);

            if (!multiEditingSupported && wasVisible)
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

            m_ContentRect = m_InspectorElement.layout;
        }

        Rect DrawEditorHeader(Object target, ref bool wasVisible)
        {
            var largeHeader = DrawEditorLargeHeader(ref wasVisible);

            // Dragging handle used for editor reordering
            var dragRect = largeHeader
                ? new Rect()
                : DrawEditorSmallHeader(target, wasVisible);
            return dragRect;
        }

        bool DrawEditorLargeHeader(ref bool wasVisible)
        {
            bool largeHeader = InspectorWindow.EditorHasLargeHeader(m_editorIndex, m_Editors);

            // Draw large headers before we do the culling of unsupported editors below,
            // so the large header is always shown even when the editor can't be.
            if (largeHeader)
            {
                String message = String.Empty;
                bool IsOpenForEdit = editor.IsOpenForEdit(out message);
                wasVisible = true;

                if (inspectorWindow.editorsWithImportedObjectLabel.Contains(m_editorIndex))
                {
                    var importedObjectBarRect = GUILayoutUtility.GetRect(16, 16);
                    importedObjectBarRect.height = 17;

                    // Clip the label to avoid a black border at the bottom
                    GUI.BeginGroup(importedObjectBarRect);
                    GUI.Label(new Rect(0, 0, importedObjectBarRect.width, importedObjectBarRect.height),
                        "Imported Object", "OL Title");
                    GUI.EndGroup();
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
        Rect DrawEditorSmallHeader(Object target, bool wasVisible)
        {
            var editors = m_Editors;
            var editor = editors[m_editorIndex];
            // ensure first component's title bar is flush with the header
            if (EditorNeedsVerticalOffset(target))
            {
                // TODO: Check if we can fix this in the GameObjectInspector instead
                GUILayout.Space(
                    -1f // move back up so line overlaps
                    - EditorStyles.inspectorBig.margin.bottom -
                    EditorStyles.inspectorTitlebar.margin.top // move back up margins
                );
            }

            using (new EditorGUI.DisabledScope(!editor.IsEnabled()))
            {
                bool isVisible = EditorGUILayout.InspectorTitlebar(wasVisible, editor);
                if (wasVisible != isVisible)
                {
                    inspectorWindow.tracker.SetVisible(m_editorIndex, isVisible ? 1 : 0);
                    InternalEditorUtility.SetIsInspectorExpanded(target, isVisible);
                    if (isVisible)
                    {
                        inspectorWindow.lastInteractedEditor = editor;
                    }
                    else if (inspectorWindow.lastInteractedEditor == editor)
                    {
                        inspectorWindow.lastInteractedEditor = null;
                    }
                }
            }

            return GUILayoutUtility.GetLastRect();
        }

        internal static void SetElementVisible(InspectorElement ve, bool visible)
        {
            if (visible)
            {
                ve.style.position = Position.Relative;
                ve.style.visibility = Visibility.Visible;
                SetInspectorElementChildIMGUIContainerFocusable(ve, true);
            }
            else
            {
                ve.style.position = Position.Absolute;
                ve.style.visibility = Visibility.Hidden;
                SetInspectorElementChildIMGUIContainerFocusable(ve, false);
            }
        }

        static void SetInspectorElementChildIMGUIContainerFocusable(InspectorElement ve, bool focusable)
        {
            foreach (var child in ve.Children())
            {
                var imguiContainer = child as IMGUIContainer;
                if (imguiContainer != null)
                {
                    imguiContainer.focusable = focusable;
                }
            }
        }

        #endregion Header

        #region Footer

        IMGUIContainer BuildFooterElement(string editorTitle)
        {
            IMGUIContainer footerElement = inspectorWindow.CreateIMGUIContainer(FooterOnGUI, editorTitle + "Footer");
            footerElement.style.height = 3;
            return footerElement;
        }

        void FooterOnGUI()
        {
            var editors = m_Editors;
            if (editors.Length <= m_editorIndex)
            {
                return;
            }

            var ed = editors[m_editorIndex];

            m_ContentRect.y = -m_ContentRect.height;
            inspectorWindow.editorDragging.HandleDraggingToEditor(editors, m_editorIndex, m_DragRect, m_ContentRect);
            HandleComponentScreenshot(m_ContentRect, ed);

            var target = ed.target;
            var comp = target as Component;

            if (EditorGUI.ShouldDrawOverrideBackground(ed.targets, Event.current, comp))
            {
                var rect = GUILayoutUtility.kDummyRect;
                bool wasVisible = inspectorWindow.WasEditorVisible(editors, m_editorIndex, target);
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
                        globalComponentRect.position + inspectorWindow.m_Parent.screenPosition.position;
                    ScreenShots.ScreenShotComponent(globalComponentRect, editor.target);
                }
            }
        }

        #endregion Footer

        internal bool EditorNeedsVerticalOffset(Object target)
        {
            return m_editorIndex > 0 && m_Editors[m_editorIndex - 1].target is GameObject && target is Component;
        }
    }
}
