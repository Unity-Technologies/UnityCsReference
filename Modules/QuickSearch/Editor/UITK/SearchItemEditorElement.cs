// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    /// <summary>
    /// Element to display the editor of an object.
    /// </summary>
    // The implementation of this element is largely copied over from EditorElement.
    // The EditorElement is tightly coupled with the inspector window and the ActiveEditorTracker and cannot be used
    // outside that context. This element can be used in any context where an Editor is available.
    class SearchItemEditorElement : VisualElement
    {
        private UnityEngine.Object m_EditorTarget;

        private IMGUIContainer m_Header;
        private InspectorElement m_InspectorElement;
        private IMGUIContainer m_Footer;

        private Editor m_Editor;

        private Rect m_ContentRect;

        private bool m_WasVisible = true; // start with the editor expanded
        private bool m_LastOpenForEdit;

        const float kFooterDefaultHeight = 5;

        public static readonly string ussClassName = "search-editor-element";
        public static readonly string inspectorElementClassName = ussClassName.WithUssElement("inspector");
        public static readonly string headerElementClassName = ussClassName.WithUssElement("header");
        public static readonly string footerElementClassName = ussClassName.WithUssElement("footer");

        public Editor editor => m_Editor;
        public InspectorElement inspectorElement => m_InspectorElement;

        public SearchItemEditorElement(string name, Editor editor, params string[] classes)
        {
            this.name = name;
            AddToClassList(ussClassName);
            this.classList.AddRange(classes);

            m_Editor = editor;

            Init();
        }

        void Init()
        {
            m_EditorTarget = editor.targets[0];
            var editorTitle = ObjectNames.GetInspectorTitle(m_EditorTarget);

            m_Header = BuildHeaderElement(editorTitle);
            m_Footer = BuildFooterElement(editorTitle);

            Add(m_Header);
            Add(m_Footer);

            CreateInspectorElement();
        }

        public void CreateInspectorElement()
        {
            if (null == editor || null != m_InspectorElement)
                return;

            // If the editor targets contain many targets and multi editing is not supported, we should not add this inspector.
            if (null != editor && (editor.targets.Length <= 1 || PropertyEditor.IsMultiEditingSupported(editor, editor.target, InspectorMode.Normal)))
            {
                m_InspectorElement = BuildInspectorElement();
                Insert(IndexOf(m_Header) + 1, m_InspectorElement);
                UpdateInspectorVisibility();
                SetElementVisible(m_InspectorElement, m_WasVisible);
            }
        }

        InspectorElement BuildInspectorElement()
        {
            var editorTitle = ObjectNames.GetInspectorTitle(m_EditorTarget);

            InspectorElement.Mode inspectorElementMode = InspectorElement.Mode.Normal;
            if (editor is GenericInspector)
                inspectorElementMode = InspectorElement.Mode.UIEDefault;

            var inspectorElement = new InspectorElement(editor, inspectorElementMode)
            {
                focusable = false,
                name = editorTitle + "Inspector",
                style =
                {
                    paddingBottom = PropertyEditor.kEditorElementPaddingBottom
                }
            };

            if (EditorNeedsVerticalOffset(editor, m_EditorTarget))
            {
                inspectorElement.style.overflow = Overflow.Hidden;
            }

            inspectorElement.AddToClassList(inspectorElementClassName);

            return inspectorElement;
        }

        IMGUIContainer BuildHeaderElement(string editorTitle)
        {
            //Create and IMGUIContainer to enclose the header
            // This also needs to validate the state of the editor tracker (stuff that was already done in the original DrawEditors
            var headerElement = CreateIMGUIContainer(HeaderOnGUI, editorTitle + "Header");

            headerElement.AddToClassList(headerElementClassName);

            return headerElement;
        }

        void HeaderOnGUI()
        {
            if (editor == null)
            {
                if (m_InspectorElement != null)
                {
                    SetElementVisible(m_InspectorElement, false);
                }
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
                if (m_InspectorElement != null)
                {
                    SetElementVisible(m_InspectorElement, false);
                }
                return;
            }

            // Active polling of "open for edit" changes.
            // If the header is moving to UI Toolkit, we may have to rely on a scheduler instead.
            if (editor != null)
            {
                bool openForEdit = editor.IsOpenForEdit();
                if (openForEdit != m_LastOpenForEdit)
                {
                    m_LastOpenForEdit = openForEdit;
                    m_InspectorElement?.SetEnabled(openForEdit);
                }
            }

            GUIUtility.GetControlID(target.GetInstanceID(), FocusType.Passive);
            EditorGUIUtility.ResetGUIState();

            //set the current PropertyHandlerCache to the current editor
            ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;
            var visible = m_WasVisible;
            using (new InspectorWindowUtils.LayoutGroupChecker())
            {
                DrawEditorHeader(editor, target, ref visible);
            }

            if (GUI.changed)
            {
                // If the header changed something, we must trigger a layout calculating on imgui children
                // Fixes Material editor toggling layout issues (case 1148706)
                InvalidateIMGUILayouts(this);
            }

            if (m_InspectorElement != null && visible != IsElementVisible(m_InspectorElement))
            {
                SetElementVisible(m_InspectorElement, visible);
            }

            UpdateInspectorVisibility();

            InspectorWindowUtils.DisplayDeprecationMessageIfNecessary(editor);

            // Reset dirtiness when repainting
            if (Event.current.type == EventType.Repaint)
            {
                editor.isInspectorDirty = false;
            }

            // Case 1359247:
            // Object might have been unloaded. Calling into native code down here will crash the editor.
            if (editor.target != null)
            {
                bool excludedClass = InspectorWindowUtils.IsExcludedClass(target);
                if (excludedClass)
                    EditorGUILayout.HelpBox(
                        "The module which implements this component type has been force excluded in player settings. This object will be removed in play mode and from any builds you make.",
                        MessageType.Warning);
            }

            if (visible)
            {
                m_ContentRect = m_InspectorElement?.layout ?? Rect.zero;
            }
            else
            {
                Rect r = m_Header.layout;
                r.y = r.y + r.height - 1;
                r.height = kFooterDefaultHeight;
                m_ContentRect = r;
            }

            m_WasVisible = visible;
        }

        static Rect DrawEditorHeader(Editor editor, Object target, ref bool wasVisible)
        {
            var largeHeader = DrawEditorLargeHeader(editor, ref wasVisible);

            // Dragging handle used for editor reordering
            var dragRect = largeHeader
                ? new Rect()
                : DrawEditorSmallHeader(editor, target, ref wasVisible);
            return dragRect;
        }

        static bool DrawEditorLargeHeader(Editor editor, ref bool wasVisible)
        {
            if (editor == null)
            {
                return true;
            }

            bool largeHeader = editor.HasLargeHeader();

            // Draw large headers before we do the culling of unsupported editors below,
            // so the large header is always shown even when the editor can't be.
            if (largeHeader)
            {
                bool IsOpenForEdit = editor.IsOpenForEdit();
                wasVisible = true;

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
        static Rect DrawEditorSmallHeader(Editor editor, Object target, ref bool wasVisible)
        {
            var currentEditor = editor;

            if (currentEditor == null)
                return GUILayoutUtility.GetLastRect();

            // ensure first component's title bar is flush with the header
            if (EditorNeedsVerticalOffset(editor, target))
            {
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
                    UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(target, isVisible);
                    wasVisible = isVisible;
                }
            }
            return GUILayoutUtility.GetLastRect();
        }

        IMGUIContainer BuildFooterElement(string editorTitle)
        {
            IMGUIContainer footerElement = CreateIMGUIContainer(FooterOnGUI, editorTitle + "Footer");
            footerElement.style.height = kFooterDefaultHeight;
            footerElement.AddToClassList(footerElementClassName);
            return footerElement;
        }

        void FooterOnGUI()
        {
            var ed = editor;

            if (ed == null)
            {
                return;
            }

            m_ContentRect.y = -m_ContentRect.height;
        }

        void UpdateInspectorVisibility()
        {
            if (editor.CanBeExpandedViaAFoldoutWithoutUpdate())
            {
                if (m_Footer != null)
                    m_Footer.style.marginTop = m_WasVisible ? 0 : -kFooterDefaultHeight;

                if (m_InspectorElement != null)
                    m_InspectorElement.style.paddingBottom = PropertyEditor.kEditorElementPaddingBottom;
            }
            else
            {
                if (m_Footer != null)
                    m_Footer.style.marginTop = -kFooterDefaultHeight;

                if (m_InspectorElement != null)
                    m_InspectorElement.style.paddingBottom = 0;
            }
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

        internal static bool EditorNeedsVerticalOffset(Editor editor, Object target)
        {
            return editor != null && editor.target is GameObject && target is Component;
        }

        private static UQueryState<IMGUIContainer> s_ImguiContainersQuery = new UQueryBuilder<IMGUIContainer>(null).SingleBaseType().Build();
        internal static void InvalidateIMGUILayouts(VisualElement element)
        {
            if (element != null)
            {
                var q = s_ImguiContainersQuery.RebuildOn(element);
                q.ForEach(e => e.MarkDirtyLayout());
            }
        }

        static IMGUIContainer CreateIMGUIContainer(Action onGUIHandler, string name = null)
        {
            IMGUIContainer result = new IMGUIContainer(onGUIHandler);
            if (name != null)
            {
                result.name = name;
            }

            return result;
        }
    }
}
