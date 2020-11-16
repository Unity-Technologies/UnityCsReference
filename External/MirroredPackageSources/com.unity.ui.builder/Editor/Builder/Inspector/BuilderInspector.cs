using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using System;

namespace Unity.UI.Builder
{
    internal class BuilderInspector : BuilderPaneContent, IBuilderSelectionNotifier
    {
        enum Section
        {
            NothingSelected = 1 << 0,
            StyleSheet = 1 << 1,
            StyleSelector = 1 << 2,
            ElementAttributes = 1 << 3,
            ElementInheritedStyles = 1 << 4,
            LocalStyles = 1 << 5,
            VisualTreeAsset = 1 << 6,
            MultiSelection = 1 << 7,
        }

        // View Data
        // HACK: So...we want to restore the scroll position of the inspector but
        // lots of events cause it to reset. For example, undo/redo will reset the
        // builder, select nothing, then restore the selection. While nothing is selected,
        // the ScrolView will rightly reset the scroll position to 0 since it does not
        // need a scroller just to display the "Nothing selected" message. Then, when
        // the selection is restored, the scroll position will still be zero.
        //
        // The solution here, which is definitely overkill, is to cache the previous
        // s_MaxCachedScrollPositions m_ScrollView.contentContainer.layout.heights
        // and their associated scroll positions. Then, when we detect a
        // m_ScrollView.contenContainer GeometryChangeEvent, we look up our
        // cache and restore the correct scroll position for this particular content
        // height.
        [Serializable]
        struct CachedScrollPosition
        {
            public float scrollPosition;
            public float maxScrollValue;
        }
        ScrollView m_ScrollView;
        static readonly int s_MaxCachedScrollPositions = 5;
        [SerializeField] int m_CachedScrollPositionCount = 0;
        [SerializeField] int m_OldestScrollPositionIndex = 0;
        [SerializeField] float[] m_CachedContentHeights = new float[s_MaxCachedScrollPositions];
        [SerializeField] CachedScrollPosition[] m_CachedScrollPositions = new CachedScrollPosition[s_MaxCachedScrollPositions];
        float contentHeight => m_ScrollView.contentContainer.layout.height;

        // Utilities
        BuilderInspectorMatchingSelectors m_MatchingSelectors;
        BuilderInspectorStyleFields m_StyleFields;
        public BuilderInspectorMatchingSelectors matchingSelectors => m_MatchingSelectors;
        public BuilderInspectorStyleFields styleFields => m_StyleFields;

        // Sections
        BuilderInspectorCanvas m_CanvasSection;
        BuilderInspectorAttributes m_AttributesSection;
        BuilderInspectorInheritedStyles m_InheritedStyleSection;
        BuilderInspectorLocalStyles m_LocalStylesSection;
        BuilderInspectorSelector m_SelectorSection;
        BuilderInspectorStyleSheet m_StyleSheetSection;
        public BuilderInspectorCanvas canvasInspector => m_CanvasSection;

        // Constants
        static readonly string s_UssClassName = "unity-builder-inspector";

        // External References
        BuilderPaneWindow m_PaneWindow;
        BuilderSelection m_Selection;

        // Current Selection
        StyleRule m_CurrentRule;
        VisualElement m_CurrentVisualElement;

        // Cached Selection
        VisualElement m_CachedVisualElement;

        // Sections List (for hiding/showing based on current selection)
        List<VisualElement> m_Sections;

        // Minor Sections
        Label m_NothingSelectedSection;
        VisualElement m_MultiSelectionSection;

        public BuilderSelection selection => m_Selection;
        public BuilderDocument document => m_PaneWindow.document;
        public BuilderPaneWindow paneWindow => m_PaneWindow;

        public StyleSheet styleSheet
        {
            get
            {
                if (currentVisualElement == null)
                    return null;

                if (BuilderSharedStyles.IsStyleSheetElement(currentVisualElement))
                    return currentVisualElement.GetStyleSheet();

                if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                    return currentVisualElement.GetClosestStyleSheet();

                return visualTreeAsset.inlineSheet;
            }
        }

        public VisualTreeAsset visualTreeAsset
        {
            get
            {
                var element = currentVisualElement;
                if (element == null)
                    return m_PaneWindow.document.visualTreeAsset;

                // It's important to return the VTA of the element, not the
                // currently active VTA.
                var elementVTA = element.GetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName) as VisualTreeAsset;
                if (elementVTA == null)
                    return m_PaneWindow.document.visualTreeAsset;

                return elementVTA;
            }
        }

        public StyleRule currentRule
        {
            get
            {
                if (m_CurrentRule != null)
                    return m_CurrentRule;

                if (currentVisualElement == null)
                    return null;

                if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                {
                    var complexSelector = currentVisualElement.GetStyleComplexSelector();
                    m_CurrentRule = complexSelector?.rule;
                }
                else if (currentVisualElement.GetVisualElementAsset() != null && currentVisualElement.IsPartOfActiveVisualTreeAsset(document))
                {
                    var vea = currentVisualElement.GetVisualElementAsset();
                    m_CurrentRule = visualTreeAsset.GetOrCreateInlineStyleRule(vea);
                }
                else
                {
                    return null;
                }

                return m_CurrentRule;
            }
            set
            {
                m_CurrentRule = value;
            }
        }

        public VisualElement currentVisualElement
        {
            get
            {
                return m_CurrentVisualElement != null ? m_CurrentVisualElement : m_CachedVisualElement;
            }
        }

        HighlightOverlayPainter m_HighlightOverlayPainter;
        public HighlightOverlayPainter highlightOverlayPainter => m_HighlightOverlayPainter;

        public BuilderInspector(BuilderPaneWindow paneWindow, BuilderSelection selection, HighlightOverlayPainter highlightOverlayPainter = null)
        {
            m_HighlightOverlayPainter = highlightOverlayPainter;

            // Yes, we give ourselves a view data key. Don't do this at home!
            viewDataKey = "unity-ui-builder-inspector";

            // Init External References
            m_Selection = selection;
            m_PaneWindow = paneWindow;

            // Load Template
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/BuilderInspector.uxml");
            template.CloneTree(this);

            // Get the scroll view.
            // HACK: ScrollView is not capable of remembering a scroll position for content that changes often.
            // The main issue is that we expande/collapse/display/hide different parts of the Inspector
            // all the time so initially the ScrollView is empty and it restores the scroll position to zero.
            m_ScrollView = this.Q<ScrollView>("inspector-scroll-view");
            m_ScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(OnScrollViewContentGeometryChange);
            m_ScrollView.verticalScroller.valueChanged += (newValue) =>
            {
                CacheScrollPosition(newValue, m_ScrollView.verticalScroller.highValue);
                SaveViewData();
            };

            // Load styles.
            AddToClassList(s_UssClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.InspectorUssPathNoExt + ".uss"));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.InspectorUssPathNoExt + "Dark.uss"));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.InspectorUssPathNoExt + "Light.uss"));

            // Matching Selectors
            m_MatchingSelectors = new BuilderInspectorMatchingSelectors(this);

            // Style Fields
            m_StyleFields = new BuilderInspectorStyleFields(this);

            // Sections
            m_Sections = new List<VisualElement>();

            // Nothing Selected Section
            m_NothingSelectedSection = this.Q<Label>("nothing-selected-label");
            m_Sections.Add(m_NothingSelectedSection);

            // Multi-Selection Section
            m_MultiSelectionSection = this.Q("multi-selection-unsupported-message");
            m_MultiSelectionSection.Add(new IMGUIContainer(
                () => EditorGUILayout.HelpBox(BuilderConstants.MultiSelectionNotSupportedMessage, MessageType.Info, true)));
            m_Sections.Add(m_MultiSelectionSection);

            // Canvas Section
            m_CanvasSection = new BuilderInspectorCanvas(this);
            m_Sections.Add(m_CanvasSection.root);

            // StyleSheet Section
            m_StyleSheetSection = new BuilderInspectorStyleSheet(this);
            m_Sections.Add(m_StyleSheetSection.root);

            // Style Selector Section
            m_SelectorSection = new BuilderInspectorSelector(this);
            m_Sections.Add(m_SelectorSection.root);

            // Attributes Section
            m_AttributesSection = new BuilderInspectorAttributes(this);
            m_Sections.Add(m_AttributesSection.root);

            // Inherited Styles Section
            m_InheritedStyleSection = new BuilderInspectorInheritedStyles(this, m_MatchingSelectors);
            m_Sections.Add(m_InheritedStyleSection.root);

            // Local Styles Section
            m_LocalStylesSection = new BuilderInspectorLocalStyles(this, m_StyleFields);
            m_Sections.Add(m_LocalStylesSection.root);

            // This will take into account the current selection and then call RefreshUI().
            SelectionChanged();

            // Forward focus to the panel header.
            this.Query().Where(e => e.focusable).ForEach((e) => AddFocusable(e));
        }

        public new void AddFocusable(VisualElement focusable)
        {
            base.AddFocusable(focusable);
        }

        void RefreshAfterFirstInit(GeometryChangedEvent evt)
        {
            currentVisualElement?.UnregisterCallback<GeometryChangedEvent>(RefreshAfterFirstInit);
            RefreshUI();
        }

        void ResetSection(VisualElement section)
        {
            // For performance reasons, it's important NOT to use a style class!
            section.style.display = DisplayStyle.None;
        }

        void EnableSection(VisualElement section)
        {
            // For performance reasons, it's important NOT to use a style class!
            section.style.display = DisplayStyle.Flex;
        }

        void EnableFields()
        {
            m_SelectorSection.Enable();
            m_AttributesSection.Enable();
            m_InheritedStyleSection.Enable();
            m_LocalStylesSection.Enable();
        }

        void DisableFields()
        {
            m_SelectorSection.Disable();
            m_AttributesSection.Disable();
            m_InheritedStyleSection.Disable();
            m_LocalStylesSection.Disable();
        }

        void EnableSections(Section section)
        {
            if (section.HasFlag(Section.NothingSelected))
                EnableSection(m_NothingSelectedSection);
            if (section.HasFlag(Section.StyleSheet))
                EnableSection(m_StyleSheetSection.root);
            if (section.HasFlag(Section.StyleSelector))
                EnableSection(m_SelectorSection.root);
            if (section.HasFlag(Section.ElementAttributes))
                EnableSection(m_AttributesSection.root);
            if (section.HasFlag(Section.ElementInheritedStyles))
                EnableSection(m_InheritedStyleSection.root);
            if (section.HasFlag(Section.LocalStyles))
                EnableSection(m_LocalStylesSection.root);
            if (section.HasFlag(Section.VisualTreeAsset))
                EnableSection(m_CanvasSection.root);
            if (section.HasFlag(Section.MultiSelection))
                EnableSection(m_MultiSelectionSection);
        }

        void ResetSections()
        {
            EnableFields();

            foreach (var section in m_Sections)
                ResetSection(section);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);

            SetScrollerPositionFromSavedState();
        }

        void OnScrollViewContentGeometryChange(GeometryChangedEvent evt)
        {
            SetScrollerPositionFromSavedState();
        }

        void CacheScrollPosition(float currentScrollPosition, float currentMaxScrollValue)
        {
            // This avoid pushing legitimate cached positions out of the cache with
            // short (nothing selected) content.
            if (!m_ScrollView.needsVertical)
                return;

            int index = -1;
            for (int i = 0; i < m_CachedScrollPositionCount; ++i)
                if (m_CachedContentHeights[i] == contentHeight)
                {
                    index = i;
                    break;
                }

            if (index < 0)
            {
                if (m_CachedScrollPositionCount < s_MaxCachedScrollPositions)
                {
                    index = m_CachedScrollPositionCount;
                    m_CachedScrollPositionCount++;
                }
                else
                {
                    index = m_OldestScrollPositionIndex;
                    m_OldestScrollPositionIndex = (m_OldestScrollPositionIndex + 1) % s_MaxCachedScrollPositions;
                }
            }

            var cached = m_CachedScrollPositions[index];
            cached.scrollPosition = currentScrollPosition;
            cached.maxScrollValue = currentMaxScrollValue;
            m_CachedScrollPositions[index] = cached;

            m_CachedContentHeights[index] = contentHeight;
        }

        int GetCachedScrollPositionIndex()
        {
            int index = -1;
            for (int i = 0; i < m_CachedScrollPositionCount; ++i)
                if (m_CachedContentHeights[i] == contentHeight)
                {
                    index = i;
                    break;
                }

            return index;
        }

        void SetScrollerPositionFromSavedState()
        {
            var index = GetCachedScrollPositionIndex();
            if (index < 0)
                return;

            var cached = m_CachedScrollPositions[index];
            m_ScrollView.verticalScroller.highValue = cached.maxScrollValue;
            m_ScrollView.verticalScroller.value = cached.scrollPosition;
        }

        public void RefreshUI()
        {
            // On the first RefreshUI, if an element is already selected, we need to make sure it
            // has a valid style. If not, we need to delay our UI building until it is properly initialized.
            if (currentVisualElement != null &&
                // TODO: This is just for tests to pass. When adding selectors via the fake events
                // we sometimes get selector elements that have no layout and will not layout
                // no matter how many yields we add. They do have the correct panel.
                m_Selection.selectionType != BuilderSelectionType.StyleSelector &&
                float.IsNaN(currentVisualElement.layout.width))
            {
                currentVisualElement.RegisterCallback<GeometryChangedEvent>(RefreshAfterFirstInit);
                return;
            }

            // Determine what to show based on selection.
            ResetSections();
            if (m_Selection.selectionCount > 1)
            {
                EnableSections(Section.MultiSelection);
                return;
            }
            switch (m_Selection.selectionType)
            {
                case BuilderSelectionType.Nothing:
                    EnableSections(Section.NothingSelected);
                    return;
                case BuilderSelectionType.StyleSheet:
                    EnableSections(Section.StyleSheet);
                    return;
                case BuilderSelectionType.ParentStyleSelector:
                case BuilderSelectionType.StyleSelector:
                    EnableSections(
                        Section.StyleSelector |
                        Section.LocalStyles);
                    break;
                case BuilderSelectionType.ElementInTemplateInstance:
                case BuilderSelectionType.ElementInParentDocument:
                case BuilderSelectionType.Element:
                    EnableSections(
                        Section.ElementAttributes |
                        Section.ElementInheritedStyles |
                        Section.LocalStyles);
                    break;
                case BuilderSelectionType.VisualTreeAsset:
                    EnableSections(Section.VisualTreeAsset);
                    m_CanvasSection.Refresh();
                    return;
            }
            bool selectionInTemplateInstance = m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance;
            bool selectionInParentSelector = m_Selection.selectionType == BuilderSelectionType.ParentStyleSelector;
            bool selectionInParentDocument = m_Selection.selectionType == BuilderSelectionType.ElementInParentDocument;
            if (selectionInTemplateInstance || selectionInParentSelector || selectionInParentDocument)
                DisableFields();

            // Bind the style selector controls.
            m_SelectorSection.Refresh();

            // Recreate Attribute Fields
            m_AttributesSection.Refresh();

            // Reset current style rule.
            currentRule = null;

            // Get all shared style selectors and draw their fields.
            m_MatchingSelectors.GetElementMatchers();
            m_InheritedStyleSection.Refresh();

            // Create the fields for the overridable styles.
            m_LocalStylesSection.Refresh();

            m_CanvasSection.Refresh();
        }

        public void OnAfterBuilderDeserialize()
        {
            m_CanvasSection.Refresh();
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            m_AttributesSection.Refresh();
        }

        public void SelectionChanged()
        {
            if (m_CurrentVisualElement != null && BuilderSharedStyles.IsSelectorElement(m_CurrentVisualElement))
            {
                StyleSheetUtilities.RemoveFakeSelector(m_CurrentVisualElement);
            }

            m_CurrentVisualElement = null;

            foreach (var element in m_Selection.selection)
            {
                if (m_CurrentVisualElement != null) // We only support editing one element. Disable for for multiple elements.
                {
                    m_CurrentVisualElement = null;
                    break;
                }

                m_CurrentVisualElement = element;
            }

            if (m_CurrentVisualElement != null)
            {
                m_CachedVisualElement = m_CurrentVisualElement;
            }

            if (m_CurrentVisualElement != null && BuilderSharedStyles.IsSelectorElement(m_CurrentVisualElement))
            {
                StyleSheetUtilities.AddFakeSelector(m_CurrentVisualElement);
                m_Selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);
            }
            else
            {
                RefreshUI();
            }
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType = BuilderStylingChangeType.Default)
        {
            if (styles != null)
            {
                foreach (var styleName in styles)
                {
                    var fieldList = m_StyleFields.GetFieldListForStyleName(styleName);
                    if (fieldList == null)
                        continue;

                    foreach (var field in fieldList)
                        m_StyleFields.RefreshStyleField(styleName, field);
                }

                m_LocalStylesSection.UpdateStyleCategoryFoldoutOverrides();
            }
            else
            {
                RefreshUI();
            }
        }
    }
}
