// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEditor.UIElements.Debugger;
using UnityEngine.UIElements.StyleSheets;
using System.Collections;

namespace Unity.UI.Builder
{
    internal class BuilderInspector : BuilderPaneContent, IBuilderSelectionNotifier
    {
        enum Section
        {
            NothingSelected = 1 << 0,
            Header = 1 << 1,
            StyleSheet = 1 << 2,
            StyleSelector = 1 << 3,
            ElementAttributes = 1 << 4,
            ElementInheritedStyles = 1 << 5,
            LocalStyles = 1 << 6,
            VisualTreeAsset = 1 << 7,
            MultiSelection = 1 << 8,
        }

        // View Data
        // HACK: So...we want to restore the scroll position of the inspector but
        // lots of events cause it to reset. For example, undo/redo will reset the
        // builder, select nothing, then restore the selection. While nothing is selected,
        // the ScrollView will rightly reset the scroll position to 0 since it does not
        // need a scroller just to display the "Nothing selected" message. Then, when
        // the selection is restored, the scroll position will still be zero.
        //
        // The solution here, which is definitely overkill, is to cache the previous
        // s_MaxCachedScrollPositions m_ScrollView.contentContainer.layout.heights
        // and their associated scroll positions. Then, when we detect a
        // m_ScrollView.contentContainer GeometryChangeEvent, we look up our
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

        const float m_PreviewDefaultHeight = 200;
        const float m_PreviewMinHeight = 20;
        float m_CachedPreviewHeight = m_PreviewDefaultHeight;

        // Utilities
        BuilderInspectorMatchingSelectors m_MatchingSelectors;
        BuilderInspectorStyleFields m_StyleFields;
        BuilderBindingsCache m_BindingsCache;
        BuilderNotifications m_Notifications;
        public BuilderBindingsCache bindingsCache => m_BindingsCache;

        public BuilderInspectorMatchingSelectors matchingSelectors => m_MatchingSelectors;
        public BuilderInspectorStyleFields styleFields => m_StyleFields;

        // Header
        BuilderInspectorHeader m_HeaderSection;
        internal BuilderInspectorHeader headerSection => m_HeaderSection;

        // Sections
        BuilderInspectorCanvas m_CanvasSection;
        BuilderInspectorAttributes m_AttributesSection;
        BuilderInspectorInheritedStyles m_InheritedStyleSection;
        BuilderInspectorLocalStyles m_LocalStylesSection;
        BuilderInspectorStyleSheet m_StyleSheetSection;

        // Selector Preview
        TwoPaneSplitView m_SplitView;
        BuilderInspectorPreview m_SelectorPreview;
        BuilderInspectorPreviewWindow m_PreviewWindow;

        public BuilderInspectorCanvas canvasInspector => m_CanvasSection;
        public BuilderInspectorAttributes attributesSection => m_AttributesSection;

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
        VisualElement m_NothingSelectedSection;
        VisualElement m_NothingSelectedDayZeroVisualElement;
        VisualElement m_NothingSelectedIdleStateVisualElement;
        VisualElement m_MultiSelectionSection;

        HashSet<VisualElement> m_ResolvedBoundFields = new();

        public BuilderSelection selection => m_Selection;
        public BuilderDocument document => m_PaneWindow.document;
        public BuilderPaneWindow paneWindow => m_PaneWindow;
        public BuilderInspectorAttributes attributeSection => m_AttributesSection;
        public BuilderInspectorPreview preview => m_SelectorPreview;
        public BuilderInspectorPreviewWindow previewWindow => m_PreviewWindow;
        public bool showingPreview => m_SplitView?.fixedPane?.resolvedStyle.height > m_PreviewMinHeight;

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

        public VisualElement selectedVisualElement => m_CurrentVisualElement;

        public VisualElement currentVisualElement
        {
            get
            {
                return m_CurrentVisualElement != null ? m_CurrentVisualElement : m_CachedVisualElement;
            }
        }

        HighlightOverlayPainter m_HighlightOverlayPainter;
        public HighlightOverlayPainter highlightOverlayPainter => m_HighlightOverlayPainter;

        public bool isBoundFieldInlineValueBeingEdited { get; private set; }

        public BuilderInspector(BuilderPaneWindow paneWindow, BuilderSelection selection, HighlightOverlayPainter highlightOverlayPainter = null, BuilderBindingsCache bindingsCache = null, BuilderNotifications builderNotifications = null)
        {
            m_BindingsCache = bindingsCache;
            m_HighlightOverlayPainter = highlightOverlayPainter;
            m_Notifications = builderNotifications;

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
            // The main issue is that we expand/collapse/display/hide different parts of the Inspector
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
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_InspectorWindow));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UssPath_InspectorWindow_Themed));

            // Matching Selectors
            m_MatchingSelectors = new BuilderInspectorMatchingSelectors(this);

            // Style Fields
            m_StyleFields = new BuilderInspectorStyleFields(this);

            // Sections
            m_Sections = new List<VisualElement>();

            // Header Section
            m_HeaderSection = new BuilderInspectorHeader(this);
            m_Sections.Add(m_HeaderSection.header);

            // Nothing Selected Section
            m_NothingSelectedSection = this.Q<VisualElement>("nothing-selected-visual-element");
            m_NothingSelectedDayZeroVisualElement = this.Q<VisualElement>("day-zero-visual-element");
            m_NothingSelectedIdleStateVisualElement = this.Q<VisualElement>("idle-state-visual-element");
            m_NothingSelectedDayZeroVisualElement.style.display = DisplayStyle.None;
            m_NothingSelectedIdleStateVisualElement.style.display = DisplayStyle.None;
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

            // Attributes Section
            m_AttributesSection = new BuilderInspectorAttributes(this);
            m_Sections.Add(m_AttributesSection.root);

            // Inherited Styles Section
            m_InheritedStyleSection = new BuilderInspectorInheritedStyles(this, m_MatchingSelectors);
            m_Sections.Add(m_InheritedStyleSection.root);

            // Local Styles Section
            m_LocalStylesSection = new BuilderInspectorLocalStyles(this, m_StyleFields);
            m_Sections.Add(m_LocalStylesSection.root);

            m_SplitView = this.Q<TwoPaneSplitView>("inspector-content");
            m_SplitView.RegisterCallback<GeometryChangedEvent>(OnFirstDisplay);
            var previewPane = this.Q<BuilderPane>("inspector-selector-preview");

            // Preview Section
            m_SelectorPreview = new BuilderInspectorPreview(this);
            previewPane.Add(m_SelectorPreview);
            // Adding transparency toggle to toolbar
            previewPane.toolbar.Add(m_SelectorPreview.backgroundToggle);

            previewPane.RegisterCallback<GeometryChangedEvent>(OnSizeChange);

            RegisterDraglineInteraction();

            // This will take into account the current selection and then call RefreshUI().
            SelectionChanged();

            // Forward focus to the panel header.
            this.Query().Where(e => e.focusable).ForEach((e) => AddFocusable(e));

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            if (m_BindingsCache != null)
            {
                m_BindingsCache.onBindingStatusChanged += OnBindingStatusChanged;
                m_BindingsCache.onBindingRemoved += OnBindingStatusChanged;
            }
        }

        public void UnsetBoundFieldInlineValue(DropdownMenuAction menuAction)
        {
            isBoundFieldInlineValueBeingEdited = true;
            var fieldElement = menuAction.userData as VisualElement;
            attributeSection.UnsetAttributeProperty(fieldElement, false);
            isBoundFieldInlineValueBeingEdited = false;
        }

        public void SetBoundPropertyFieldDisplay(VisualElement fieldElement, bool display)
        {
            var boundPropertyField = fieldElement.Q<PropertyField>(BuilderUxmlAttributesView.builderBoundPropertyFieldName);
            boundPropertyField.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;

            var serializedPropertyField = fieldElement.Q<PropertyField>(BuilderUxmlAttributesView.builderSerializedPropertyFieldName);
            serializedPropertyField.style.display = display ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void EnableInlineValueEditing(VisualElement fieldElement)
        {
            isBoundFieldInlineValueBeingEdited = true;

            var notificationData = new BuilderNotifications.NotificationData()
            {
                key = BuilderConstants.inlineEditingNotificationKey,
                message = BuilderConstants.InlineValuesOnCanvasNotification,
            };

            m_Notifications.AddNotification(notificationData);

            if (fieldElement == null)
            {
                return;
            }

            SetFieldsEnabled(fieldElement, true);
            ToggleInlineEditingClasses(fieldElement, true);

            var isAttribute = fieldElement.HasLinkedAttributeDescription();
            if (isAttribute)
            {
                SetBoundPropertyFieldDisplay(fieldElement, false);
            }
            else
            {
                var styleName =
                    fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

                if (!string.IsNullOrEmpty(styleName) &&
                    StylePropertyUtil.s_NameToId.TryGetValue(styleName, out var id) &&
                    id.IsTransitionId())
                {
                    var transitionsListView = fieldElement.GetFirstAncestorOfType<TransitionsListView>();
                    styleFields.RefreshInlineEditedTransitionField(transitionsListView, id);
                }
                else
                {
                    styleFields.RefreshStyleFieldValue(styleName, fieldElement, true);
                }
            }

            var baseField = fieldElement.Q<VisualElement>(className: BaseField<string>.ussClassName);
            baseField?.Focus();
        }

        public void RegisterFieldToInlineEditingEvents(VisualElement field)
        {
            field.RegisterCallback<FocusOutEvent, VisualElement>((evt, e) =>
            {
                DisableInlineValueEditing(e, false);
            }, field, TrickleDown.TrickleDown);
            field.RegisterCallback<KeyDownEvent, VisualElement>((evt, e) =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Escape)
                {
                    DisableInlineValueEditing(e, true);
                }
            }, field, TrickleDown.TrickleDown);

            var objectFields = field.Query<ObjectField>();
            objectFields.ForEach(x => x.onObjectSelectorShow += x.Focus);
        }

        private void DisableInlineValueEditing(VisualElement fieldElement, bool skipFocusCheck)
        {
            isBoundFieldInlineValueBeingEdited = false;
            m_Notifications.ClearNotifications(BuilderConstants.inlineEditingNotificationKey);

            if (!IsInlineEditingEnabled(fieldElement))
            {
                return;
            }

            if (skipFocusCheck)
            {
                DisableInlineEditedField(fieldElement);
            }
            else
            {
                // We keep inline editing enabled if focus is still in the field.
                // This check is delayed so the focus is updated properly.
                fieldElement.schedule.Execute(t =>
                {
                    var focusedElement = fieldElement.FindElement(x => x.IsFocused());
                    if (focusedElement != null)
                    {
                        return;
                    }

                    DisableInlineEditedField(fieldElement);
                });
            }
        }

        private void DisableInlineEditedField(VisualElement fieldElement)
        {
            ToggleInlineEditingClasses(fieldElement, false);
            SetFieldsEnabled(fieldElement, false);
        }

        public void ToggleInlineEditingClasses(VisualElement fieldElement, bool value)
        {
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            var statusIndicator = fieldElement.GetFieldStatusIndicator();

            styleRow?.EnableInClassList(BuilderConstants.InspectorFieldBindingInlineEditingEnabledClassName, value);
            statusIndicator?.EnableInClassList(BuilderConstants.InspectorFieldBindingInlineEditingEnabledClassName, value);
            fieldElement.SetProperty(BuilderConstants.InspectorFieldBindingInlineEditingEnabledPropertyName, value);
        }

        public bool IsInlineEditingEnabled(VisualElement field)
        {
            return field.HasProperty(BuilderConstants.InspectorFieldBindingInlineEditingEnabledPropertyName)
                   && (bool)field.GetProperty(BuilderConstants.InspectorFieldBindingInlineEditingEnabledPropertyName);
        }

        private void OnBindingStatusChanged(VisualElement target, string bindingPath)
        {
            if (target != currentVisualElement || !IsElementSelected())
                return;

            // Find field
            var field = BuilderInspectorUtilities.FindInspectorField(this, bindingPath);

            if (field != null)
            {
                UpdateFieldStatus(field, null);
            }
        }

        public void UpdateBoundFields()
        {
            if (!IsElementSelected())
                return;

            foreach (var field in m_ResolvedBoundFields)
            {
                // Update value current visual element
                UpdateBoundValue(field);
            }
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

            if (section != m_NothingSelectedSection)
                return;

            m_ScrollView.contentContainer.style.flexGrow = 0;
            m_ScrollView.contentContainer.style.justifyContent = Justify.FlexStart;
        }

        void EnableSection(VisualElement section)
        {
            // For performance reasons, it's important NOT to use a style class!
            section.style.display = DisplayStyle.Flex;
        }

        void EnableFields()
        {
            m_HeaderSection.Enable();
            m_AttributesSection.Enable();
            m_InheritedStyleSection.Enable();
            m_LocalStylesSection.Enable();
        }

        void DisableFields()
        {
            m_HeaderSection.Disable();
            m_AttributesSection.Disable();
            m_InheritedStyleSection.Disable();
            m_LocalStylesSection.Disable();
        }

        void EnableSections(Section section)
        {
            if (section.HasFlag(Section.NothingSelected))
                EnableSection(m_NothingSelectedSection);
            if (section.HasFlag(Section.Header))
                EnableSection(m_HeaderSection.header);
            if (section.HasFlag(Section.StyleSheet))
                EnableSection(m_StyleSheetSection.root);
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

        public void UpdateFieldStatus(VisualElement field, StyleProperty property)
        {
            if (m_CurrentVisualElement == null)
            {
                return;
            }

            var valueInfo = FieldValueInfo.Get(this, field, property);

            field.SetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName, valueInfo);
            UpdateFieldStatusIconAndStyling(currentVisualElement, field, valueInfo);
            UpdateFieldTooltip(field, valueInfo, currentVisualElement);
            UpdateBoundFieldsState(field, valueInfo);

            var isAttribute = field.HasLinkedAttributeDescription();
            if (isAttribute)
            {
                attributeSection.UpdateAttributeOverrideStyle(field);
            }
            else
            {
                m_StyleFields.UpdateOverrideStyles(field, property);
                var isSelectorElement = currentVisualElement.IsSelector();
                if (isSelectorElement)
                {
                    var isVariable = valueInfo.valueBinding.type == FieldValueBindingInfoType.USSVariable;
                    if (isVariable)
                    {
                        var isResolvedVariable = valueInfo.valueBinding.variable.sheet != null;
                        SetFieldsEnabled(field, !isResolvedVariable);
                    }
                }
            }
        }

        private void UpdateBoundFieldsState(VisualElement field, FieldValueInfo valueInfo)
        {
            var hasBinding = valueInfo.valueBinding.type == FieldValueBindingInfoType.Binding;
            var hasResolvedBinding = false;

            if (hasBinding)
            {
                hasResolvedBinding = valueInfo.valueSource.type == FieldValueSourceInfoType.ResolvedBinding;

                if (hasResolvedBinding)
                {
                    m_ResolvedBoundFields.Add(field);
                }
                else
                {
                    if (m_ResolvedBoundFields.Contains(field))
                    {
                        if (field.HasLinkedAttributeDescription())
                        {
                            SetBoundPropertyFieldDisplay(field, false);
                        }

                        m_ResolvedBoundFields.Remove(field);
                    }
                }
            }
            else
            {
                if (m_ResolvedBoundFields.Contains(field))
                {
                    if (field.HasLinkedAttributeDescription())
                    {
                        SetBoundPropertyFieldDisplay(field, false);
                    }

                    m_ResolvedBoundFields.Remove(field);
                }
            }

            SetFieldsEnabled(field, !hasResolvedBinding);
        }

        public void SetFieldsEnabled(VisualElement field, bool enabled)
        {
            if (IsInlineEditingEnabled(field))
            {
                return;
            }

            if (field is TextShadowStyleField)
            {
                // Special case for TextShadowStyleField.
                // We need to disabled the fields inside so the foldout is still functional
                var foldout = field.Q<Foldout>();
                foldout.contentContainer.SetEnabled(enabled);
            }
            else
            {
                field.SetEnabled(enabled);

                if (field.GetProperty(BuilderConstants.FoldoutFieldPropertyName) is FoldoutField foldout)
                {
                    var fields = foldout.Query<VisualElement>(classes: BaseField<float>.ussClassName);
                    var hasBindings = false;
                    var hasResolvedBindings = false;

                    fields.ForEach(x =>
                    {
                        var bindingProperty = BuilderInspectorUtilities.GetBindingProperty(this, x);
                        var bindingId = new BindingId(bindingProperty);
                        if (DataBindingUtility.TryGetLastUIBindingResult(bindingId, currentVisualElement, out var bindingResult))
                        {
                            hasBindings = true;
                            hasResolvedBindings |= bindingResult.status == BindingStatus.Success;
                        }
                    });

                    foldout.EnableInClassList(BuilderConstants.BoundFoldoutFieldClassName, hasBindings);
                    foldout.SetHeaderInputEnabled(!hasResolvedBindings);
                }
            }
        }

        void UpdateBoundValue(VisualElement field)
        {
            var isAttribute = field.HasLinkedAttributeDescription();

            if (IsInlineEditingEnabled(field))
            {
                // Don't update value now, it's being edited
                return;
            }

            if (isAttribute)
            {
                var attributeName = BuilderInspectorUtilities.GetBindingProperty(this, field);
                var value = currentVisualElement.GetValueByReflection(attributeName);

                var boundPropertyField = field.Q<PropertyField>(BuilderUxmlAttributesView.builderBoundPropertyFieldName);
                boundPropertyField.style.display = DisplayStyle.Flex;

                var serializedPropertyField = field.Q<PropertyField>(BuilderUxmlAttributesView.builderSerializedPropertyFieldName);
                serializedPropertyField.style.display = DisplayStyle.None;

                attributeSection.SetBoundValue(field, value);
            }
            else
            {
                var styleName =
                    field.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

                StylePropertyUtil.s_NameToId.TryGetValue(styleName, out var id);

                if (id.IsTransitionId())
                {
                    var transitionsListView = field.GetFirstAncestorOfType<TransitionsListView>();
                    styleFields.RefreshStyleField(transitionsListView);
                }
                else
                {
                    styleFields.RefreshStyleFieldValue(styleName, field);
                }
            }
        }

        internal static void UpdateFieldStatusIconAndStyling(VisualElement currentElement, VisualElement field, FieldValueInfo valueInfo)
        {
            var statusIndicator = field.GetFieldStatusIndicator();

            void ClearClassLists(VisualElement ve)
            {
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleDefaultStatusClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleInheritedClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleSelectorClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleVariableClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleUnresolvedVariableClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleBindingClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleUnresolvedBindingClassName);
                ve.RemoveFromClassList(BuilderConstants.InspectorLocalStyleSelectorElementClassName);
            };

            ClearClassLists(field);
            ClearClassLists(statusIndicator);

            var statusClassName = valueInfo.valueBinding.type switch
            {
                FieldValueBindingInfoType.USSVariable => valueInfo.valueBinding.variable.sheet != null
                    ? BuilderConstants.InspectorLocalStyleVariableClassName
                    : BuilderConstants.InspectorLocalStyleUnresolvedVariableClassName,
                FieldValueBindingInfoType.Binding => valueInfo.valueSource.type == FieldValueSourceInfoType.ResolvedBinding ?
                BuilderConstants.InspectorLocalStyleBindingClassName : BuilderConstants.InspectorLocalStyleUnresolvedBindingClassName,
                _ => valueInfo.valueSource.type switch
                {
                    FieldValueSourceInfoType.Inherited => BuilderConstants.InspectorLocalStyleInheritedClassName,
                    FieldValueSourceInfoType.MatchingUSSSelector => BuilderConstants.InspectorLocalStyleSelectorClassName,
                    _ => BuilderConstants.InspectorLocalStyleDefaultStatusClassName
                }
            };

            statusIndicator.AddToClassList(statusClassName);
            field.AddToClassList(statusClassName);

            if (currentElement != null)
            {
                var isSelector = currentElement.IsSelector();
                if (isSelector)
                {
                    statusIndicator.AddToClassList(BuilderConstants.InspectorLocalStyleSelectorElementClassName);
                    field.AddToClassList(BuilderConstants.InspectorLocalStyleSelectorElementClassName);
                }
            }
        }

        internal static void UpdateFieldTooltip(VisualElement field, FieldValueInfo valueInfo, VisualElement currentElement = null)
        {
            var draggerLabel = GetDraggerLabel(field);
            var tooltipValue = GetFieldTooltip(field, valueInfo);

            if (draggerLabel != null)
            {
                draggerLabel.tooltip = tooltipValue;
            }

            field.GetFieldStatusIndicator().tooltip = GetFieldStatusIndicatorTooltip(valueInfo, field, currentElement);
        }

        internal static Label GetDraggerLabel(VisualElement field)
        {
            var labelDraggers = field.Query<Label>(classes: BaseField<float>.labelDraggerVariantUssClassName).Build();
            return GetFirstItemIfCountIs1(labelDraggers);
        }

        static Label GetFirstItemIfCountIs1(UQueryState<Label> query)
        {
            using (var enumerator = query.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var firstItem = enumerator.Current;
                    if (!enumerator.MoveNext())
                    {
                        return firstItem;
                    }
                }

                return null;
            }
        }

        static string GetFieldStatusIndicatorTooltip(FieldValueInfo info, VisualElement field, VisualElement currentElement, string description = null)
        {
            if (info.valueSource.type == FieldValueSourceInfoType.Default)
                return BuilderConstants.FieldStatusIndicatorDefaultTooltip;
            if (info.valueBinding.type == FieldValueBindingInfoType.USSVariable)
                return info.valueBinding.variable.sheet != null ? GetVariableTooltip(info.valueBinding.variable) : BuilderConstants.FieldStatusIndicatorUnresolvedVariableTooltip;

            // data binding
            if (info.valueBinding.type == FieldValueBindingInfoType.Binding)
            {
                // detailed binding information
                var inspector = Builder.ActiveWindow.inspector;
                var currentVisualElement = inspector.currentVisualElement;
                var property = BuilderInspectorUtilities.GetBindingProperty(inspector, field);

                if (!BuilderBindingUtility.TryGetBinding(property, out var binding, out var bindingUxml) ||
                    binding is not DataBinding dataBinding)
                    return BuilderConstants.FieldStatusIndicatorUnresolvedBindingTooltip;

                var notDefinedString = L10n.Tr(BuilderConstants.BindingNotDefinedAttributeString);
                var currentElementDataSource =
                    BuilderBindingUtility.GetBindingDataSourceOrRelativeHierarchicalDataSource(currentVisualElement,
                        property);
                var dataSourceString = currentElementDataSource ?? notDefinedString;
                var bindingMode = dataBinding.bindingMode;
                var dataSourcePathStr = dataBinding.dataSourcePath.IsEmpty
                    ? notDefinedString
                    : dataBinding.dataSourcePath.ToString();
                var convertersToSource =
                    bindingUxml.GetAttributeValue(BuilderBindingUxmlAttributesView
                        .k_BindingAttr_ConvertersToSource);
                var convertersToUI =
                    bindingUxml.GetAttributeValue(BuilderBindingUxmlAttributesView.k_BindingAttr_ConvertersToUi);
                var converters = GetFormattedConvertersString(convertersToSource, convertersToUI);

                return info.valueSource.type switch
                {
                    FieldValueSourceInfoType.ResolvedBinding => string.Format(BuilderConstants.FieldTooltipDataDefinitionBindingFormatString,
                        BuilderConstants.FieldStatusIndicatorResolvedBindingTooltip,
                        dataSourceString, dataSourcePathStr, bindingMode, converters),
                    FieldValueSourceInfoType.UnhandledBinding => string.Format(BuilderConstants.FieldTooltipDataDefinitionBindingFormatString,
                        BuilderConstants.FieldStatusIndicatorUnhandledBindingTooltip,
                        dataSourceString, dataSourcePathStr, bindingMode, converters),
                    _ => string.Format(BuilderConstants.FieldTooltipDataDefinitionBindingFormatString,
                        BuilderConstants.FieldStatusIndicatorUnresolvedBindingTooltip,
                        dataSourceString, dataSourcePathStr, bindingMode, converters),
                };
            }

            return info.valueSource.type switch
            {
                FieldValueSourceInfoType.Inline => BuilderConstants.FieldStatusIndicatorInlineTooltip,
                FieldValueSourceInfoType.Inherited => GetInheritedValueTooltip(currentElement),
                FieldValueSourceInfoType.MatchingUSSSelector => GetMatchingStyleSheetRuleSourceTooltip(info.valueSource.matchedRule),
                FieldValueSourceInfoType.LocalUSSSelector => BuilderConstants.FieldStatusIndicatorLocalTooltip,
                _ => null
            };
        }

        internal static string GetFieldTooltip(VisualElement field, FieldValueInfo info, string description = null, bool allowValueDescription = true)
        {
            if (info.type == FieldValueInfoType.None)
                return "";

            var tooltipFormat = BuilderConstants.FieldTooltipNameOnlyFormatString;

            var value = string.Format(tooltipFormat, info.type.ToDisplayString(), info.name);
            var valueDataText = "";
            var valueDefinitionDataText = "";
            // binding
            if (info.valueSource.type != FieldValueSourceInfoType.Default
                && info.valueSource.type != FieldValueSourceInfoType.Inherited)
            {
                if (allowValueDescription)
                    tooltipFormat = BuilderConstants.FieldTooltipFormatString;

                    // if the value is bound to variable then display the variable info
                if (allowValueDescription && info.valueBinding.type == FieldValueBindingInfoType.USSVariable)
                    valueDataText = $"\n{GetVariableTooltip(info.valueBinding.variable)}";
            }

            // source
            if (allowValueDescription && info.valueSource.type.IsFromUSSSelector())
                valueDefinitionDataText = $"\n{GetMatchingStyleSheetRuleSourceTooltip(info.valueSource.matchedRule)}";

            // For UX purposes, some USS properties have custom tooltips. If no custom tooltip is found, fall back to generated tooltip
            if (info.type == FieldValueInfoType.USSProperty && BuilderConstants.InspectorStylePropertiesTooltipsDictionary.TryGetValue(info.name, out var ussTooltip))
            {
                tooltipFormat = BuilderConstants.FieldTooltipWithDescription;
                value = string.Format(tooltipFormat, info.type.ToDisplayString(), info.name, ussTooltip);
            }
            else
            {
                value = string.Format(tooltipFormat, info.type.ToDisplayString(), info.name, info.valueBinding.type.ToDisplayString(), valueDataText, info.valueSource.type.ToDisplayString(), valueDefinitionDataText);
            }
            if (!string.IsNullOrEmpty(description))
                value += "\n\n" + description;
            return value;
        }

        static string GetFormattedConvertersString(string convertersToSource, string convertersToUI)
        {
            if (string.IsNullOrEmpty(convertersToSource) && string.IsNullOrEmpty(convertersToUI))
            {
                return L10n.Tr(BuilderConstants.EmptyConvertersString);
            }
            if (string.IsNullOrEmpty(convertersToSource))
            {
                return convertersToUI;
            }
            if (string.IsNullOrEmpty(convertersToUI))
            {
                return convertersToSource;
            }

            return $"{convertersToSource}, {convertersToUI}";
        }

        static string GetMatchingStyleSheetRuleSourceTooltip(MatchedRule matchedRule)
        {
            var displayPath = matchedRule.displayPath;

            // Remove line number
            var index = displayPath.IndexOf(':');

            if (index != -1)
            {
                displayPath = displayPath.Substring(0, index);
            }

            return string.Format(BuilderConstants.FieldStatusIndicatorFromSelectorTooltip, StyleSheetToUss.ToUssSelector(matchedRule.matchRecord.complexSelector), displayPath);
        }

        static string GetVariableTooltip(VariableInfo info)
        {
            string variableName = "";
            string sourceStyleSheet = "";

            if (info.sheet)
            {
                var varStyleSheetOrigin = info.sheet;
                var fullPath = AssetDatabase.GetAssetPath(varStyleSheetOrigin);

                if (string.IsNullOrEmpty(fullPath))
                {
                    sourceStyleSheet = varStyleSheetOrigin.name;
                }
                else
                {
                    sourceStyleSheet = fullPath == BuilderConstants.EditorResourcesBundlePath ? varStyleSheetOrigin.name : Path.GetFileName(fullPath);
                }
            }
            else
            {
                sourceStyleSheet = BuilderConstants.FileNotFoundMessage;
            }

            variableName = info.name;

            return string.Format(BuilderConstants.FieldStatusIndicatorVariableTooltip, variableName, sourceStyleSheet);
        }

        static string GetInheritedValueTooltip(VisualElement child)
        {
            var parent = child.parent;
            return string.Format(BuilderConstants.FieldStatusIndicatorInheritedTooltip,
                parent != null ? parent.typeName : "",
                parent != null ? parent.name : "");
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

            m_ResolvedBoundFields.Clear();

            // Determine what to show based on selection.
            ResetSections();
            UpdateSelectorPreviewsVisibility();
            if (m_Selection.selectionCount > 1)
            {
                EnableSections(Section.MultiSelection);
                return;
            }
            switch (m_Selection.selectionType)
            {
                case BuilderSelectionType.Nothing:

                    EnableSections(Section.NothingSelected);
                    m_ScrollView.contentContainer.style.flexGrow = 1;
                    m_ScrollView.contentContainer.style.justifyContent = Justify.Center;
                    var hierarchyOrStyleSectionsNotEmpty = document.visualTreeAsset.visualElementAssets.Count > 1 ||
                                                    document.activeStyleSheet != null;
                    if (!string.IsNullOrEmpty(document.activeOpenUXMLFile.uxmlPath) || hierarchyOrStyleSectionsNotEmpty)
                    {
                        m_NothingSelectedIdleStateVisualElement.style.display = DisplayStyle.Flex;
                        m_NothingSelectedDayZeroVisualElement.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        m_NothingSelectedDayZeroVisualElement.style.display = DisplayStyle.Flex;
                        m_NothingSelectedIdleStateVisualElement.style.display = DisplayStyle.None;
                    }
                    return;

                case BuilderSelectionType.StyleSheet:
                    EnableSections(Section.StyleSheet);
                    return;
                case BuilderSelectionType.ParentStyleSelector:
                case BuilderSelectionType.StyleSelector:
                    EnableSections(
                        Section.Header |
                        Section.StyleSelector |
                        Section.LocalStyles);
                    break;
                case BuilderSelectionType.ElementInTemplateInstance:
                case BuilderSelectionType.ElementInControlInstance:
                case BuilderSelectionType.ElementInParentDocument:
                case BuilderSelectionType.Element:
                    EnableSections(
                        Section.Header |
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
            bool selectionInControlInstance = m_Selection.selectionType == BuilderSelectionType.ElementInControlInstance;
            bool selectionInParentSelector = m_Selection.selectionType == BuilderSelectionType.ParentStyleSelector;
            bool selectionInParentDocument = m_Selection.selectionType == BuilderSelectionType.ElementInParentDocument;

            if (selectionInTemplateInstance || selectionInParentSelector || selectionInParentDocument || selectionInControlInstance)
            {
                DisableFields();
            }
            if (selectionInTemplateInstance && !string.IsNullOrEmpty(currentVisualElement.name))
            {
                m_HeaderSection.Enable();
                m_AttributesSection.Enable();
            }

            // Reselect Icon, Type & Name in Header
            m_HeaderSection.Refresh();

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

            if (selectionInTemplateInstance)
            {
                m_HeaderSection.Disable();
            }
        }

        public void OnAfterBuilderDeserialize()
        {
            m_CanvasSection.Refresh();
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            m_HeaderSection.Refresh();

            if ((changeType & BuilderHierarchyChangeType.Attributes) == BuilderHierarchyChangeType.Attributes)
            {
                m_AttributesSection.Refresh();
            }
        }

        public void BeforeSelectionChanged()
        {
            // Check whether the focused element is a field in the inspector. If so, then blur it immediately
            // to commit its value (e.g: delayed text field such as the name field) before the selection changes
            if (focusController is { focusedElement: VisualElement focusedElement } && Contains(focusedElement))
            {
                focusedElement.BlurImmediately();
            }
        }

        public void SelectionChanged()
        {
            if (isBoundFieldInlineValueBeingEdited)
            {
                var element = this.focusController.focusedElement as VisualElement;
                if (element != null && IsInlineEditingEnabled(element))
                {
                    DisableInlineValueEditing(element, true);
                }
            }

            if (m_CurrentVisualElement != null)
            {
                if (BuilderSharedStyles.IsSelectorElement(m_CurrentVisualElement))
                {
                    StyleSheetUtilities.RemoveFakeSelector(m_CurrentVisualElement);
                }

                m_CurrentVisualElement.UnregisterCallback<PropertyChangedEvent>(OnPropertyChanged, TrickleDown.TrickleDown);
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
                m_CurrentVisualElement.RegisterCallback<PropertyChangedEvent>(OnPropertyChanged, TrickleDown.TrickleDown);
                m_CachedVisualElement = m_CurrentVisualElement;
            }

            if (IsElementSelected())
            {
                m_AttributesSection.SetAttributesOwner(visualTreeAsset, currentVisualElement, m_Selection.selectionType == BuilderSelectionType.ElementInTemplateInstance);
            }
            else
            {
                m_AttributesSection.ResetAttributesOwner();
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

        private void OnPropertyChanged(PropertyChangedEvent evt)
        {
            if (isBoundFieldInlineValueBeingEdited)
            {
                // Stop propagation during inline editing to avoid changing data source
                evt.StopImmediatePropagation();
            }
        }

        private bool IsElementSelected()
        {
            return m_CurrentVisualElement != null && m_Selection.selectionType is BuilderSelectionType.Element
                or BuilderSelectionType.ElementInTemplateInstance or BuilderSelectionType.ElementInControlInstance;
        }

        private UnityEngine.UIElements.UxmlTraits GetCurrentElementTraits()
        {
            var currentVisualElementTypeName = currentVisualElement.GetType().ToString();

            if (!VisualElementFactoryRegistry.TryGetValue(currentVisualElementTypeName, out var factoryList))
            {
                // We fallback on the BindableElement factory if we don't find any so
                // we can update the modified attributes. This fixes the TemplateContainer
                // factory not found.
                if (!VisualElementFactoryRegistry.TryGetValue(typeof(BindableElement).FullName,
                        out factoryList))
                {
                    return null;
                }
            }

            var traits = factoryList[0].GetTraits() as UxmlTraits;
            return traits;
        }

        internal void CallInitOnElement()
        {
            var traits = GetCurrentElementTraits();

            if (traits == null)
                return;

            // We need to clear bindings before calling Init to avoid corrupting the data source.
            BuilderBindingUtility.ClearUxmlBindings(currentVisualElement);

            var context = new CreationContext(null, null, visualTreeAsset, currentVisualElement);
            var vea = currentVisualElement.GetVisualElementAsset();
            traits.Init(currentVisualElement, vea, context);
        }

        internal void CallInitOnTemplateChild(VisualElement visualElement, VisualElementAsset vea,
            List<CreationContext.AttributeOverrideRange> attributeOverridesRanges)
        {
            var traits = GetCurrentElementTraits();

            if (traits == null)
                return;

            // We need to clear bindings before calling Init to avoid corrupting the data source.
            BuilderBindingUtility.ClearUxmlBindings(currentVisualElement);

            var context = new CreationContext(null, attributeOverridesRanges, null, null);
            traits.Init(visualElement, vea, context);
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

                    // Transitions are composed of dynamic elements which can add/remove themselves in the fieldList
                    // when the style is refreshed, so we take a copy of the list to ensure we do not iterate and
                    // mutate the list at the same time.
                    var tempFieldList = ListPool<VisualElement>.Get();
                    try
                    {
                        tempFieldList.AddRange(fieldList);
                        foreach (var field in tempFieldList)
                            m_StyleFields.RefreshStyleField(styleName, field);
                    }
                    finally
                    {
                        ListPool<VisualElement>.Release(tempFieldList);
                    }
                }

                m_LocalStylesSection.UpdateStyleCategoryFoldoutOverrides();
            }
            else
            {
                RefreshUI();
            }
        }

        public VisualElement FindFieldAtPath(string propertyPath)
        {
            const string k_StylePrefix = "style.";

            if (propertyPath.StartsWith(k_StylePrefix))
            {
                return FindStyleField(BuilderNameUtilities.ConvertStyleCSharpNameToUssName(propertyPath.Substring(k_StylePrefix.Length)));
            }
            else
            {
                return FindAttributeField(attributeSection.GetRemapCSPropertyToAttributeName(propertyPath));
            }
        }

        public VisualElement FindAttributeField(string propName)
        {
            bool IsFieldElement(VisualElement ve)
            {
                var attribute = ve.GetLinkedAttributeDescription();
                return attribute?.name == propName;
            }

            VisualElement field;
            if (propName is "data-source" or "data-source-type" or "data-source-path")
                field = m_HeaderSection.m_DataSourceAndPathView.fieldsContainer.Query().Where(IsFieldElement);
            else
                field = attributeSection.root.Query().Where(IsFieldElement);

            return field;
        }

        VisualElement FindTransitionField(string fieldPath)
        {
            var openBracketIndex = fieldPath.IndexOf('[');
            var closeBracketIndex = fieldPath.IndexOf(']');
            var transitionIndexStr = fieldPath.Substring(openBracketIndex + 1, (closeBracketIndex - openBracketIndex - 1));
            var transitionIndex = int.Parse(transitionIndexStr);
            var fieldName = fieldPath.Substring(closeBracketIndex + 2);
            var transitionListView = this.Q<TransitionsListView>();
            var transitionView = transitionListView[transitionIndex];

            return transitionView.Q(fieldName);
        }

        public VisualElement FindStyleField(string styleName)
        {
            VisualElement field = null;

            if (styleName.StartsWith("transitions"))
                field = FindTransitionField(styleName);
            else
                field = styleFields.m_StyleFields[styleName].First();

            return field;
        }

        void OnFirstDisplay(GeometryChangedEvent evt)
        {
            if (m_PreviewWindow != null)
                OpenPreviewWindow();
            else
            {
                UpdatePreviewHeight(m_PreviewDefaultHeight);
                // Needed to stop the TwoPaneSplitView from calling its own GeometryChangedEvent callback (OnSizeChange)
                // which would change the pane's layout to have the cached height when reopening the builder.
                evt.StopImmediatePropagation();
            }

            UpdateSelectorPreviewsVisibility();
            m_SplitView.UnregisterCallback<GeometryChangedEvent>(OnFirstDisplay);
        }

        private void OnSizeChange(GeometryChangedEvent evt)
        {
            var hideToggle = !showingPreview && m_PreviewWindow == null;
            m_SelectorPreview.backgroundToggle.style.display = hideToggle ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnDragLineChange(PointerUpEvent evt)
        {
            var previewHeight = m_SplitView.fixedPane.resolvedStyle.height;
            var isSingleClick = (previewHeight == m_CachedPreviewHeight || previewHeight == m_PreviewMinHeight)
                                && Math.Abs(m_SplitView.m_Resizer.delta) <= 5;

            if (isSingleClick && evt.button == (int) MouseButton.LeftMouse)
            {
                TogglePreviewInInspector();
                return;
            }

            if (!showingPreview) return;
            m_CachedPreviewHeight = previewHeight;
        }

        internal void ReattachPreview()
        {
            var previewPane = this.Q<BuilderPane>("inspector-selector-preview");
            m_SplitView.fixedPaneDimension = m_PreviewDefaultHeight;
            previewPane.Add(m_SelectorPreview);
            previewPane.toolbar.Add(m_SelectorPreview.backgroundToggle);
            m_PreviewWindow = null;
            UpdateSelectorPreviewsVisibility();
        }

        void UpdateSelectorPreviewsVisibility()
        {
            var currElementIsSelector = m_CurrentVisualElement != null &&
                                        BuilderSharedStyles.IsSelectorElement(m_CurrentVisualElement);
            if (m_PreviewWindow == null)
            {
                if (currElementIsSelector)
                {
                    m_SplitView.UnCollapse();
                    m_SelectorPreview.style.display = DisplayStyle.Flex;
                }
                else
                    m_SplitView.CollapseChild(1);
            }
            else if (currElementIsSelector)
            {
                m_SelectorPreview.style.display = DisplayStyle.Flex;
                // hiding empty state message
                m_PreviewWindow.idleMessage.style.display = DisplayStyle.None;
            }
            else
            {
                m_SelectorPreview.style.display = DisplayStyle.None;
                // showing empty state message
                m_PreviewWindow.idleMessage.style.display = DisplayStyle.Flex;
            }
        }

        internal void TogglePreviewInInspector()
        {
            if (showingPreview)
            {
                m_CachedPreviewHeight = m_SplitView.fixedPane.resolvedStyle.height;
                UpdatePreviewHeight(m_PreviewMinHeight);
            }
            else
            {
                UpdatePreviewHeight(m_CachedPreviewHeight);
            }
        }

        void UpdatePreviewHeight(float newHeight)
        {
            var draglineAnchor = m_SplitView.Q("unity-dragline-anchor");

            m_SplitView.fixedPane.style.height = newHeight;
            m_SplitView.fixedPaneDimension = newHeight;
            draglineAnchor.style.top = m_SplitView.resolvedStyle.height - newHeight;
        }

        internal void OpenPreviewWindow()
        {
            m_PreviewWindow = BuilderInspectorPreviewWindow.ShowWindow();
            m_SplitView.CollapseChild(1);
        }

        public void ReloadPreviewWindow(BuilderInspectorPreviewWindow window)
        {
            m_PreviewWindow = window;
            m_SplitView.CollapseChild(1);
        }

        private void RegisterDraglineInteraction()
        {
            m_SplitView.Q("unity-dragline-anchor").RegisterCallback<PointerUpEvent>(OnDragLineChange);

            m_SplitView.Q("unity-dragline").RegisterCallback<MouseUpEvent>(e =>
            {
                if (e.button == (int) MouseButton.RightMouse)
                {
                    OpenPreviewWindow();
                    // stops the context menu from opening
                    e.StopImmediatePropagation();
                }
            });
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_PreviewWindow != null)
            {
                previewWindow.Close();
            }
        }
    }
}
