// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
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
        // Local method use only -- created here to reduce garbage collection. Collection must be cleared before use
        static readonly List<VisualElement> s_Decorators = new List<VisualElement>();
        static readonly EditorElementDecoratorCollection s_EditorDecoratorCollection = new EditorElementDecoratorCollection();

        /// <summary>
        /// Adds the given editor decorator.
        /// </summary>
        /// <param name="editorDecorator">The editor decorator instance to be added.</param>
        internal static void AddDecorator(IEditorElementDecorator editorDecorator) => s_EditorDecoratorCollection.Add(editorDecorator);

        /// <summary>
        /// Removes the given editor decorator.
        /// </summary>
        /// <param name="editorDecorator">The editor decorator instance to be removed.</param>
        internal static void RemoveDecorator(IEditorElementDecorator editorDecorator) => s_EditorDecoratorCollection.Remove(editorDecorator);

        readonly IPropertyView inspectorWindow;
        Editor[] m_EditorCache;

        // getting activeEditors is costly, so pass in the previously retrieved editor array where possible
        private Editor[] PopulateCache(Editor[] editors = null)
        {
            if (editors == null)
                editors = inspectorWindow.tracker.activeEditors;
            m_EditorCache = editors;
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

        Object m_EditorTarget;
        Editor m_EditorUsedInDecorators;

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
        InspectorElement m_InspectorElement;
        VisualElement m_DecoratorsElement;
        IMGUIContainer m_Footer;

        bool m_WasVisible;
        bool m_IsCulled;

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

        internal EditorElement(int editorIndex, IPropertyView iw, Editor[] editors, bool isCulled = false)
        {
            m_EditorIndex = editorIndex;
            inspectorWindow = iw;
            m_IsCulled = isCulled;
            pickingMode = PickingMode.Ignore;

            var editor = editors == null || editors.Length == 0 || m_EditorIndex < 0 || m_EditorIndex >= editors.Length ? null : editors[m_EditorIndex];
            name = GetNameFromEditor(editor);

            // Register ui callbacks
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            if (isCulled)
            {
                InitCulled(editors);
                return;
            }

            Init(editors);
        }

        void InitCulled(Editor[] editors)
        {
            PopulateCache(editors);

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

        void Init(Editor[] editors)
        {
            PopulateCache(editors);
            m_EditorTarget = editor.targets[0];
            var editorTitle = ObjectNames.GetInspectorTitle(m_EditorTarget, editor.targets.Length > 1);

            m_Header = BuildHeaderElement(editorTitle);
            m_Footer = BuildFooterElement(editorTitle);

            Add(m_Header);
            Add(m_Footer);

            // For GameObjects we want to ensure the first component's title bar is flush with the header,
            // so we apply a small offset to the margin. (UUM-16138)
            if (m_EditorTarget is GameObject)
            {
                AddToClassList("game-object-inspector");
            }

            if (InspectorElement.disabledThrottling)
                CreateInspectorElement();
        }

        InspectorElement BuildInspectorElement()
        {
            var editors = PopulateCache();
            var editorTitle = ObjectNames.GetInspectorTitle(m_EditorTarget);

            var inspectorElement = new InspectorElement(editor)
            {
                focusable = false,
                name = editorTitle + "Inspector",
                style =
                {
                    paddingBottom = PropertyEditor.kEditorElementPaddingBottom
                }
            };

            if (EditorNeedsVerticalOffset(editors, m_EditorTarget))
            {
                inspectorElement.style.overflow = Overflow.Hidden;
            }

            return inspectorElement;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            s_EditorDecoratorCollection.OnAdd += OnEditorDecoratorAdded;
            s_EditorDecoratorCollection.OnRemove += OnEditorDecoratorRemoved;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            s_EditorDecoratorCollection.OnAdd -= OnEditorDecoratorAdded;
            s_EditorDecoratorCollection.OnRemove -= OnEditorDecoratorRemoved;
        }

        public void ReinitCulled(int editorIndex, Editor[] editors)
        {
            if (m_Header != null)
            {
                m_EditorIndex = editorIndex;
                m_Header = m_Footer = null;
                m_EditorUsedInDecorators = null;
                m_DecoratorsElement = null;
                Clear();
                InitCulled(editors);
                return;
            }

            PopulateCache(editors);
        }

        public void Reinit(int editorIndex, Editor[] editors)
        {
            if (m_Header == null)
            {
                m_EditorIndex = editorIndex;
                m_EditorUsedInDecorators = null;
                Clear();
                Init(editors);
                return;
            }

            PopulateCache(editors);
            Object editorTarget = editor.targets[0];
            name = GetNameFromEditor(editor);
            string editorTitle = ObjectNames.GetInspectorTitle(editorTarget);

            // If the target change we need to invalidate IMGUI container cached measurements
            // See https://fogbugz.unity3d.com/f/cases/1279830/
            if (m_EditorTarget != editorTarget)
            {
                m_Header.MarkDirtyLayout();
                m_Footer.MarkDirtyLayout();
            }

            m_EditorTarget = editorTarget;
            m_EditorIndex = editorIndex;

            m_Header.onGUIHandler = HeaderOnGUI;
            m_Footer.onGUIHandler = FooterOnGUI;

            m_Header.name = editorTitle + "Header";
            m_Footer.name = editorTitle + "Footer";

            if (m_InspectorElement != null)
            {
                m_InspectorElement.SetEditor(editor);
                m_InspectorElement.name = editorTitle + "Inspector";

                // InspectorElement should be enabled only if the Editor is open for edit.
                m_InspectorElement.SetEnabled(editor.IsOpenForEdit());

                // Update decorators
                if (m_EditorUsedInDecorators != editor)
                {
                    m_EditorUsedInDecorators = editor;
                    UpdateDecoratorsElement(m_EditorUsedInDecorators, editorTitle);
                }
            }

            UpdateInspectorVisibility();
        }

        public void CreateInspectorElement()
        {
            if (null == editor || null != m_InspectorElement || m_IsCulled)
                return;

            //set the current PropertyHandlerCache to the current editor
            ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

            // Need to update the cache for multi-object edit detection.
            if (editor.targets.Length != Selection.objects.Length)
                inspectorWindow.tracker.RebuildIfNecessary();

            var updateInspectorVisibility = false;

            // If the editor targets contain many targets and multi editing is not supported, we should not add this inspector.
            if (null != editor && (editor.targets.Length <= 1 || PropertyEditor.IsMultiEditingSupported(editor, editor.target, inspectorWindow.inspectorMode)))
            {
                m_InspectorElement = BuildInspectorElement();
                Insert(IndexOf(m_Header) + 1, m_InspectorElement);
                SetElementVisible(m_InspectorElement, m_WasVisible);
                updateInspectorVisibility = true;
            }

            // Create decorators
            if (m_InspectorElement != null && m_EditorUsedInDecorators != editor)
            {
                m_EditorUsedInDecorators = editor;
                UpdateDecoratorsElement(m_EditorUsedInDecorators);
                updateInspectorVisibility = true;
            }

            if (updateInspectorVisibility)
                UpdateInspectorVisibility();
        }

        string GetNameFromEditor(Editor editor)
        {
            return editor == null ?
                    "Nothing Selected" :
                    $"{editor.GetType().Name}_{editor.targets[0].GetType().Name}_{editor.targets[0].GetInstanceID()}";
        }

        void UpdateInspectorVisibility()
        {
            if (editor.CanBeExpandedViaAFoldoutWithoutUpdate())
            {
                if (m_Footer != null)
                    m_Footer.style.marginTop = m_WasVisible ? 0 : -kFooterDefaultHeight;

                if (m_DecoratorsElement != null)
                {
                    if (m_InspectorElement != null)
                        m_InspectorElement.style.paddingBottom = 0;

                    m_DecoratorsElement.style.paddingBottom = PropertyEditor.kEditorElementPaddingBottom;
                }
                else
                {
                    if (m_InspectorElement != null)
                        m_InspectorElement.style.paddingBottom = PropertyEditor.kEditorElementPaddingBottom;
                }
            }
            else
            {
                if (m_DecoratorsElement != null)
                {
                    if (m_Footer != null)
                        m_Footer.style.marginTop = m_WasVisible ? 0 : -kFooterDefaultHeight;

                    m_DecoratorsElement.style.paddingBottom = PropertyEditor.kEditorElementPaddingBottom;
                }
                else
                {
                    if (m_Footer != null)
                        m_Footer.style.marginTop = -kFooterDefaultHeight;
                }


                if (m_InspectorElement != null)
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

        private bool m_LastOpenForEdit;

        void HeaderOnGUI()
        {
            var editors = PopulateCache();
            if (!IsEditorValid())
            {
                if (m_InspectorElement != null)
                {
                    SetElementVisible(m_InspectorElement, false);
                }
                if (m_DecoratorsElement != null)
                {
                    SetElementVisible(m_DecoratorsElement, false);
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
                if (m_DecoratorsElement != null)
                {
                    SetElementVisible(m_DecoratorsElement, false);
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

            m_WasVisible = inspectorWindow.WasEditorVisible(editors, m_EditorIndex, target);

            GUIUtility.GetControlID(target.GetInstanceID(), FocusType.Passive);
            EditorGUIUtility.ResetGUIState();
            GUI.color = playModeTintColor;

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

            if (m_InspectorElement != null && m_WasVisible != IsElementVisible(m_InspectorElement))
            {
                SetElementVisible(m_InspectorElement, m_WasVisible);
            }
            if (m_DecoratorsElement != null && m_WasVisible != IsElementVisible(m_DecoratorsElement))
            {
                SetElementVisible(m_DecoratorsElement, m_WasVisible);
            }

            UpdateInspectorVisibility();

            var multiEditingSupported = PropertyEditor.IsMultiEditingSupported(editor, target, inspectorWindow.inspectorMode);

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

            if (m_WasVisible)
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
                            headerText = "Root in Prefab Asset (Open for full editing support)";
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

        internal static void SetElementVisible(VisualElement ve, bool visible)
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

        static void SetInspectorElementChildIMGUIContainerFocusable(VisualElement ve, bool focusable)
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

                EditorGUI.DrawOverrideBackgroundApplicable(rect, true);
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

        #region Decorator
        private class EditorElementDecoratorCollection : IEnumerable<IEditorElementDecorator>
        {
            readonly List<IEditorElementDecorator> m_EditorDecorators = new List<IEditorElementDecorator>();

            internal Action<IEditorElementDecorator> OnAdd;
            internal Action<IEditorElementDecorator> OnRemove;

            internal int Count => m_EditorDecorators.Count;

            internal void Add(IEditorElementDecorator editorDecorator)
            {
                if (m_EditorDecorators.Contains(editorDecorator))
                    return;

                m_EditorDecorators.Add(editorDecorator);
                OnAdd?.Invoke(editorDecorator);
            }

            internal void Remove(IEditorElementDecorator editorDecorator)
            {
                if (!m_EditorDecorators.Contains(editorDecorator))
                    return;

                m_EditorDecorators.Remove(editorDecorator);
                OnRemove?.Invoke(editorDecorator);
            }

            IEnumerator<IEditorElementDecorator> IEnumerable<IEditorElementDecorator>.GetEnumerator()
                => m_EditorDecorators.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => m_EditorDecorators.GetEnumerator();
        }

        static bool TryGetDecorators(Editor editor, List<VisualElement> decorators)
        {
            if (null == editor || editor.inspectorMode != InspectorMode.Normal)
                return false;

            decorators.Clear();
            foreach (var editorDecorator in s_EditorDecoratorCollection)
            {
                var decorator = editorDecorator.OnCreateFooter(editor);
                if (decorator != null)
                    decorators.Add(decorator);
            }

            return decorators.Count != 0;
        }

        void OnEditorDecoratorAdded(IEditorElementDecorator editorDecorator)
        {
            if (null == editor || null == m_InspectorElement || m_IsCulled)
                return;

            m_EditorUsedInDecorators = editor;
            UpdateDecoratorsElement(m_EditorUsedInDecorators);
            UpdateInspectorVisibility();
        }

        void OnEditorDecoratorRemoved(IEditorElementDecorator editorDecorator)
        {
            if (null == editor || null == m_InspectorElement || m_IsCulled)
                return;

            m_EditorUsedInDecorators = editor;
            UpdateDecoratorsElement(m_EditorUsedInDecorators);
            UpdateInspectorVisibility();
        }

        void UpdateDecoratorsElement(Editor editor, string editorTitle = null)
        {
            if (s_EditorDecoratorCollection.Count != 0 && TryGetDecorators(editor, s_Decorators))
            {
                editorTitle ??= ObjectNames.GetInspectorTitle(editor.targets[0], editor.targets.Length > 1);
                CreateDecoratorsElement(editorTitle, s_Decorators);
                SetElementVisible(m_DecoratorsElement, m_WasVisible);
            }
            else if (m_DecoratorsElement != null)
            {
                m_DecoratorsElement.Clear();
                m_DecoratorsElement.RemoveFromHierarchy();
                m_DecoratorsElement = null;
            }
        }

        void CreateDecoratorsElement(string editorTitle, List<VisualElement> children)
        {
            if (m_DecoratorsElement == null)
            {
                m_DecoratorsElement = BuildDecoratorsElement(editorTitle);
            }
            else
            {
                m_DecoratorsElement.Clear();
                m_DecoratorsElement.RemoveFromClassList(InspectorElement.uIEInspectorVariantUssClassName);
                m_DecoratorsElement.name = editorTitle + "Decorators";
            }

            if (editor.UseDefaultMargins())
            {
                m_DecoratorsElement.AddToClassList(InspectorElement.uIEInspectorVariantUssClassName);
                m_DecoratorsElement.style.paddingTop = 0;
            }

            foreach (var child in children)
                m_DecoratorsElement.Add(child);

            if (m_DecoratorsElement.parent != this)
                Insert(IndexOf(m_Footer), m_DecoratorsElement);
        }

        VisualElement BuildDecoratorsElement(string editorTitle)
        {
            var decoratorsParent = new VisualElement() { name = editorTitle + "Decorators" };
            decoratorsParent.AddToClassList(PropertyField.decoratorDrawersContainerClassName);
            return decoratorsParent;
        }

        #endregion Decorator

        internal bool EditorNeedsVerticalOffset(Editor[] editors, Object target)
        {
            return m_EditorIndex > 0 && IsEditorValid() && editors[m_EditorIndex - 1]?.target is GameObject && target is Component;
        }

        internal InspectorElement GetInspectorElementInternal()
        {
            return m_InspectorElement;
        }
    }
}
