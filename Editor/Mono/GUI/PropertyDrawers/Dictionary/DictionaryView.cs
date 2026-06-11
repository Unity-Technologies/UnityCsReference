// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEditor.UIElements;
using UnityEngine.UIElements.Internal;
using UnityEditor;

namespace UnityEngine.UIElements
{

/// <summary>
/// Self-contained ListView specialization for editing a serialized
/// <see cref="Dictionary{TKey, TValue}"/> field. Owns the foldout header
/// (via <see cref="BaseListView.showFoldoutHeader"/>), the +/- footer
/// (via <see cref="BaseListView.showAddRemoveFooter"/>), the two-column
/// "Key | Value" header with its draggable resizer, sort + duplicate
/// detection state, and all per-property data needed to render and mutate
/// the dictionary.
///
/// Bind by setting <see cref="BindableElement.bindingPath"/> (typically
/// from <c>DictionaryDrawer.CreatePropertyGUI</c>) and letting the
/// inspector's tree-wide <c>Bind(SerializedObject)</c> walk dispatch a
/// <c>SerializedPropertyBindEvent</c> here. The view intercepts that event
/// the same way <see cref="PropertyField"/> does, so the default
/// <see cref="BaseListView"/> binding never runs and the dictionary owns
/// its own makeItem/bindItem/itemsSource pipeline. Do not call
/// <see cref="BindableElement.BindProperty(SerializedProperty)"/> on this
/// view: when the view has no panel yet (e.g. inside
/// <c>CreatePropertyGUI</c>) <c>BindProperty</c> queues a deferred
/// <c>BindingRequest</c> that re-walks the tree on the next panel update
/// and re-dispatches the bind event, running the rebuild path twice.
/// </summary>
internal class DictionaryView : ListView
{
    // All USS classes added by the dictionary view live under the `unity-dictionary-view`
    // block so they're easy to distinguish from classes inherited from BaseListView,
    // ListView, PropertyField, BaseField, etc. Sub-element names use the BEM `__element`
    // suffix and modifiers use `--modifier`. The styles live in the builtin
    // UnityEngine.UIElements.uss in the "Dictionary View" section.
    public new static readonly string ussClassName = "unity-dictionary-view";
    public static readonly string headerAndListContainerUssClassName = ussClassName + "__header-and-list";

    static readonly ProfilerMarker s_BuildMarker = new("DictDrawer.Build", MarkerFlags.VerbosityExternal);
    static readonly ProfilerMarker s_BindListItemMarker = new("DictDrawer.BindListItem", MarkerFlags.VerbosityExternal);
    static readonly ProfilerMarker s_UnbindListItemMarker = new("DictDrawer.UnbindListItem", MarkerFlags.VerbosityExternal);

    static readonly string k_DrawerFieldClass = ussClassName + "__field";
    static readonly string k_DrawerFieldKeyClass = ussClassName + "__field--key";
    static readonly string k_DrawerFieldValueClass = ussClassName + "__field--value";
    static readonly string k_ListViewClass = ussClassName + "__list-view";
    static readonly string k_ListViewFocusedClass = ussClassName + "__list-view--focused";
    static readonly string k_ListHeaderClass = ussClassName + "__list-header";
    static readonly string k_ListHeaderKeyClass = ussClassName + "__list-header__key";
    static readonly string k_ListHeaderValueClass = ussClassName + "__list-header__value";
    static readonly string k_RowClass = ussClassName + "__row";
    static readonly string k_HelpBoxClass = ussClassName + "__helpbox";
    static readonly string k_HelpBoxDuplicatesClass = ussClassName + "__helpbox--duplicates";
    static readonly string k_HelpBoxSelectDuplicateClass = ussClassName + "__helpbox__select-duplicate";
    static readonly string k_HeaderSpacerClass = ussClassName + "__header-spacer";
    static readonly string k_ToggleLabelClass = ussClassName + "__toggle-label";
    static readonly string k_EmptyLabelClass = ussClassName + "__empty-label";
    static readonly string k_HeaderInfoClass = ussClassName + "__header-info";
    static readonly string k_DuplicateKeyIconClass = ussClassName + "__duplicate-key-icon";
    static readonly string k_SelectionIndicatorClass = ussClassName + "__selection-indicator";
    static readonly string k_ColumnResizerClass = ussClassName + "__column-resizer";
    static readonly string k_ColumnResizerLineClass = ussClassName + "__column-resizer__line";

    SerializedProperty m_DictionaryFieldProperty;
    SerializedProperty m_ArrayProperty;
    Hash128 m_StateCacheKey;

    readonly Foldout m_Foldout;
    Label m_HeaderInfoLabel;
    readonly VisualElement m_HeaderAndListContainer;
    VisualElement m_ListHeader;
    ColumnResizer m_ColumnResizer;
    VisualElement m_KeyHeader;
    Label m_ValueLabel;
    VisualElement m_SortIndicator;

    HelpBox m_MultiEditHelpBox;
    HelpBox m_DuplicatesHelpBox;

    readonly HashSet<int> m_DuplicateEntryIndices = new();

    bool m_SortScheduled;
    bool m_SortAscending = true;
    DictionaryDrawer.SortedIndexMap m_SortedIndexMap = DictionaryDrawer.SortedIndexMap.Empty;
    // Hash of the keys at the time we last produced m_SortedIndexMap.
    // Lets the TrackPropertyValue callback skip sort scheduling when only a
    // value changed (the hash is invariant under value-only edits).
    ulong m_HashOfKeys;

    List<int> m_ItemsSource;
    readonly List<int> m_SelectedArrayIndicesScratch = new List<int>();
    readonly List<int> m_RestoredDisplayIndicesScratch = new List<int>();

    bool m_IsBound;

    int displayedItemCount => m_SortedIndexMap.Length;

    public Foldout foldout => m_Foldout;

    IReadOnlyList<int> SelectedDisplayIndices => selectedIndicesList;

    // Are we in a state where it is safe to re-sort right now?
    // Sorting rebuilds list items and would steal focus from any in-flight text edit, so
    // both the TrackPropertyValue fast path and the deferred SortIfNeeded path
    // bail out (and reschedule) when this returns false.
    bool IsReadyToSortByKey() => EditorInteractionMonitor.IsReadyToApplyDeferredChanges(panel?.focusController);

    int DisplayToArrayIndex(int displayIndex) => m_SortedIndexMap.ToArrayIndex(displayIndex);

    int ArrayIndexToDisplayIndex(int arrayIndex) => m_SortedIndexMap.ToDisplayIndex(arrayIndex);

    public DictionaryView()
    {
        AddToClassList(ussClassName);
        AddToClassList(k_ListViewClass);
        reorderable = false;
        selectionType = SelectionType.Multiple;
        // Must be set before showFoldoutHeader, otherwise SetupArraySizeField
        // would create the size field while building the foldout header.
        showBoundCollectionSize = false;
        virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        showAlternatingRowBackgrounds = AlternatingRowBackground.All;
        showBorder = false;

        // Triggers AddFoldout() in BaseListView, which parents scrollView under the foldout.
        showFoldoutHeader = true;
        m_Foldout = this.Q<Foldout>(className: BaseListView.foldoutHeaderUssClassName);

        // Re-parent scrollView under our header-and-list container so the rounded
        // outline encloses just the column header + scrollView; the footer added
        // below stays a sibling beneath the framed container.
        scrollView.RemoveFromHierarchy();
        m_HeaderAndListContainer = new VisualElement { name = headerAndListContainerUssClassName };
        m_HeaderAndListContainer.AddToClassList(headerAndListContainerUssClassName);
        m_HeaderAndListContainer.Add(scrollView);
        m_Foldout.contentContainer.Add(m_HeaderAndListContainer);

        // EnableFooter adds the footer to m_Foldout.contentContainer when the
        // foldout exists, placing it as a sibling after m_HeaderAndListContainer.
        showAddRemoveFooter = true;

        autoSelectNewItemOnAdd = false;

        makeNoneElement = MakeEmptyElement;
        makeItem = MakeListItem;
        bindItem = BindListItem;
        unbindItem = UnbindListItem;
        destroyItem = DestroyListItem;
        onAdd = _ => OnAddClicked();
        onRemove = _ => OnRemoveClicked();
        selectionChanged += OnSelectionChanged;
    }

    [EventInterest(typeof(SerializedPropertyBindEvent))]
    protected override void HandleEventBubbleUp(EventBase evt)
    {
        base.HandleEventBubbleUp(evt);

        if (evt is SerializedPropertyBindEvent bindEvent)
        {
            RebuildFromProperty(bindEvent.bindProperty);
            evt.StopPropagation();
        }
    }

    // Tears down any prior bound state and rebuilds the view against the new
    // property. Resolves key/value types and the optional DictionaryHeaderAttribute
    // from the property's reflected FieldInfo, builds the column header, sort
    // scheduler, and duplicate tracking, and installs makeItem/bindItem/onAdd/onRemove
    // on the list. Only called from HandleEventBubbleUp's SerializedPropertyBindEvent
    // handler; external callers bind via bindingPath + the inspector's tree walk.
    void RebuildFromProperty(SerializedProperty property)
    {
        using var _ = s_BuildMarker.Auto();

        if (property == null)
            return;

        ResetBoundState();

        m_DictionaryFieldProperty = property.Copy();
        m_StateCacheKey = DictionaryDrawer.ComputeStateCacheKey(m_DictionaryFieldProperty.propertyPath);

        var cachedState = DictionaryDrawer.GetCachedState(m_StateCacheKey);
        if (cachedState != null)
        {
            m_SortAscending = cachedState.sortAscending;
        }

        // Foldout's bindingPath drives open/closed state via SerializedProperty.isExpanded,
        // so the user's collapse/expand survives rebuilds and domain reloads.
        m_Foldout.text = m_DictionaryFieldProperty.displayName;
        m_Foldout.bindingPath = m_DictionaryFieldProperty.propertyPath;
        headerTitle = m_DictionaryFieldProperty.displayName;

        if (DictionaryDrawer.IsEditingMultipleObjects(m_DictionaryFieldProperty))
        {
            // Multi-object editing isn't supported (entries are sorted by key,
            // so a given row may correspond to different entries across
            // targets). Show a HelpBox inside the foldout content and skip
            // list setup; tests still find foldout via Q<Foldout>().
            ShowMultiEditHelpBox();
            m_IsBound = true;
            return;
        }

        var arrayProp = m_DictionaryFieldProperty.Copy();
        arrayProp.Next(true);
        m_ArrayProperty = arrayProp;

        BuildDuplicatesHelpBox(m_Foldout.contentContainer);
        BuildFoldoutHeaderInfoLabel();
        SetTwoColumnHeader(BuildColumnHeader());

        RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
        RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        RegisterCallback<KeyDownEvent>(OnDictionaryKeyDown);
        RegisterCallback<FocusInEvent>(OnFocusIn);
        RegisterCallback<FocusOutEvent>(OnFocusOut);

        this.TrackPropertyValue(m_DictionaryFieldProperty, OnTrackedPropertyChanged);

        RebuildSortedIndices();
        RefreshListView();
        UpdateHeaderInfo();
        UpdateRemoveButtonState();

        m_IsBound = true;
    }

    void ResetBoundState()
    {
        if (!m_IsBound)
            return;

        // Tear down everything RebuildFromProperty installs so a rebuild for a
        // different property starts from a clean slate. The foldout itself stays
        // since its bindingPath gets reassigned below.
        if (m_MultiEditHelpBox != null)
        {
            m_MultiEditHelpBox.RemoveFromHierarchy();
            m_MultiEditHelpBox = null;
        }
        // Multi-edit hides the framed list area and footer; restore visibility
        // here so a subsequent non-multi Bind shows them again.
        m_HeaderAndListContainer.style.display = DisplayStyle.Flex;
        var listFooter = m_Foldout.Q(className: BaseListView.footerUssClassName);
        if (listFooter != null)
            listFooter.style.display = DisplayStyle.Flex;
        if (m_DuplicatesHelpBox != null)
        {
            m_DuplicatesHelpBox.RemoveFromHierarchy();
            m_DuplicatesHelpBox = null;
        }
        if (m_ListHeader != null)
        {
            m_ListHeader.RemoveFromHierarchy();
            m_ListHeader = null;
        }
        if (m_HeaderInfoLabel != null)
        {
            m_HeaderInfoLabel.RemoveFromHierarchy();
            m_HeaderInfoLabel = null;
        }

        makeItem = null;
        bindItem = null;
        unbindItem = null;
        destroyItem = null;
        makeNoneElement = null;
        onAdd = null;
        onRemove = null;
        selectionChanged -= OnSelectionChanged;
        UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
        UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        UnregisterCallback<KeyDownEvent>(OnDictionaryKeyDown);
        UnregisterCallback<FocusInEvent>(OnFocusIn);
        UnregisterCallback<FocusOutEvent>(OnFocusOut);

        if (m_DictionaryFieldProperty != null)
            this.UntrackPropertyValue(m_DictionaryFieldProperty, OnTrackedPropertyChanged);

        m_DictionaryFieldProperty = null;
        m_ArrayProperty = null;

        m_DuplicateEntryIndices.Clear();
        m_ItemsSource = null;
        itemsSource = null;
        m_SortedIndexMap = DictionaryDrawer.SortedIndexMap.Empty;
        m_HashOfKeys = 0;
        m_SortScheduled = false;
        m_ColumnResizer = null;
        m_KeyHeader = null;
        m_ValueLabel = null;
        m_SortIndicator = null;
        m_IsBound = false;
    }

    void SetTwoColumnHeader(VisualElement header)
    {
        m_HeaderAndListContainer.Insert(0, header);
    }

    void ShowMultiEditHelpBox()
    {
        if (m_MultiEditHelpBox == null)
        {
            m_MultiEditHelpBox = new HelpBox(DictionaryDrawer.Texts.MultiEditUnsupportedMessage, HelpBoxMessageType.Info);
            m_MultiEditHelpBox.AddToClassList(k_HelpBoxClass);
        }
        if (m_MultiEditHelpBox.parent != m_Foldout.contentContainer)
            m_Foldout.contentContainer.Add(m_MultiEditHelpBox);

        // Hide the framed list/header area + footer; the foldout owns just the
        // multi-edit hint while still respecting the user's collapse/expand state.
        m_HeaderAndListContainer.style.display = DisplayStyle.None;
        var listFooter = m_Foldout.Q(className: BaseListView.footerUssClassName);
        if (listFooter != null)
            listFooter.style.display = DisplayStyle.None;
    }

    void OnSelectionChanged(IEnumerable<object> _)
    {
        UpdateRemoveButtonState();
    }

    void OnTrackedPropertyChanged(object _, SerializedProperty __)
    {
        if (m_ArrayProperty == null || !m_ArrayProperty.isValid)
            return;

        int newSize = m_ArrayProperty.arraySize;
        if (newSize != displayedItemCount)
        {
            RebuildSortedIndicesAndRefresh();
            UpdateHeaderInfo();
            return;
        }

        // Always schedule; SortIfNeeded owns the single keys-hash check
        // that decides whether the change actually warrants a re-sort. Routing
        // value-only edits through the same scheduler coalesces rapid-fire
        // changes (e.g. dragging a slider) to one hash computation per frame
        // instead of one per change.
        CheckIfKeysChangedAndSortIfNeeded();

        // While a key is being edited (or any other editor interaction is in
        // flight) the pending sort can't run yet — keep duplicate markers in
        // sync so the user sees live feedback as they type. Sorting itself
        // would yank the focused field out of their hands.
        if (!IsReadyToSortByKey() && UpdateDuplicateIndicesOnly())
        {
            UpdateHeaderInfo();
            UpdateDuplicateKeyIconsOnVisibleItems();
        }
    }

    void BuildFoldoutHeaderInfoLabel()
    {
        var toggle = m_Foldout.Q<Toggle>();
        if (toggle == null)
            return;

        var toggleLabel = toggle.Q<Label>();
        if (toggleLabel != null)
            toggleLabel.AddToClassList(k_ToggleLabelClass);

        var spacer = new VisualElement();
        spacer.AddToClassList(k_HeaderSpacerClass);

        m_HeaderInfoLabel = new Label();
        m_HeaderInfoLabel.AddToClassList(k_HeaderInfoClass);

        toggle.Add(spacer);
        toggle.Add(m_HeaderInfoLabel);
    }

    void BuildDuplicatesHelpBox(VisualElement parent)
    {
        // The duplicates helpbox starts hidden via .unity-dictionary-view__helpbox--duplicates
        // (display: none in USS); UpdateHeaderInfo flips style.display when the
        // duplicate count goes non-zero.
        m_DuplicatesHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
        m_DuplicatesHelpBox.AddToClassList(k_HelpBoxClass);
        m_DuplicatesHelpBox.AddToClassList(k_HelpBoxDuplicatesClass);

        var selectButton = new Button(OnSelectFirstDuplicateClicked)
        {
            text = DictionaryDrawer.Texts.SelectFirstDuplicateButtonLabel
        };
        selectButton.AddToClassList(k_HelpBoxSelectDuplicateClass);
        m_DuplicatesHelpBox.Add(selectButton);

        parent.Add(m_DuplicatesHelpBox);
    }

    void OnSelectFirstDuplicateClicked()
    {
        int firstDisplayIndex = DictionaryDrawer.FindFirstDuplicateDisplayIndex(m_DuplicateEntryIndices, m_SortedIndexMap);
        if (firstDisplayIndex < 0)
            return;

        SetSelection(firstDisplayIndex);
        ScrollToItem(firstDisplayIndex);
        FocusContentContainer();
    }

    VisualElement BuildColumnHeader()
    {
        var fieldInfo = ScriptAttributeUtility.GetFieldInfoAndStaticTypeFromProperty(m_DictionaryFieldProperty, out _);
        DictionaryDrawer.GetHeaderLabels(fieldInfo, out var keyLabelText, out var valueLabelText, out var attributeFraction);

        var header = new VisualElement();
        header.AddToClassList(k_ListHeaderClass);
        m_ListHeader = header;

        m_KeyHeader = new VisualElement();
        m_KeyHeader.AddToClassList(k_ListHeaderKeyClass);
        m_KeyHeader.AddToClassList(MultiColumnHeaderColumn.sortableUssClassName);
        UpdateSortIndicatorClass();

        // keyLabel is styled via .unity-dictionary-view__list-header__key > .unity-text-element
        // (ellipsis overflow + flex grow/shrink), so no inline styles needed here.
        var keyLabel = new Label(keyLabelText);
        m_KeyHeader.Add(keyLabel);

        m_SortIndicator = new VisualElement();
        m_SortIndicator.AddToClassList(MultiColumnHeaderColumnSortIndicator.ussClassName);
        var sortArrow = new VisualElement();
        sortArrow.AddToClassList(MultiColumnHeaderColumnSortIndicator.arrowUssClassName);
        m_SortIndicator.Add(sortArrow);
        m_KeyHeader.Add(m_SortIndicator);

        m_KeyHeader.RegisterCallback<ClickEvent>(OnKeyHeaderClicked);

        m_ValueLabel = new Label(valueLabelText);
        m_ValueLabel.AddToClassList(k_ListHeaderValueClass);
        m_ValueLabel.pickingMode = PickingMode.Ignore;

        header.Add(m_KeyHeader);
        m_ColumnResizer = new ColumnResizer(
            header, m_DictionaryFieldProperty.propertyPath, attributeFraction, UpdateColumnWidths);
        header.Add(m_ColumnResizer.BuildElement());
        header.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (Mathf.Abs(evt.oldRect.width - evt.newRect.width) > 0.5f)
                UpdateColumnWidths();
        });
        header.Add(m_ValueLabel);
        header.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction(DictionaryDrawer.Texts.ResetToDefaultsLabel,
                _ => ResetToDefaults(),
                _ => DictionaryDrawer.HasCachedState(m_StateCacheKey) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }));

        return header;
    }

    VisualElement MakeListItem()
    {
        var container = new VisualElement();
        container.AddToClassList("unity-list-view__reorderable-item__container");
        container.AddToClassList(k_RowClass);
        container.name = "dict-element";

        var duplicateKeyIcon = new VisualElement();
        duplicateKeyIcon.AddToClassList(k_DuplicateKeyIconClass);

        var keyContainer = new VisualElement { name = "key-container" };
        keyContainer.AddToClassList(k_DrawerFieldClass);
        keyContainer.AddToClassList(k_DrawerFieldKeyClass);
        keyContainer.AddToClassList("unity-inspector-element");
        keyContainer.AddToClassList("unity-inspector-main-container");

        var valueContainer = new VisualElement { name = "value-container" };
        valueContainer.AddToClassList(k_DrawerFieldClass);
        valueContainer.AddToClassList(k_DrawerFieldValueClass);
        valueContainer.AddToClassList("unity-inspector-element");
        valueContainer.AddToClassList("unity-inspector-main-container");

        // The splitter line color is set inline from SharedStyles.k_RowsSplitColor
        // so the IMGUI and UITK drawers share a single SkinnedColor definition
        // rather than duplicating the values into USS as well. Everything else
        // about the split (border-left-width, padding) is in InspectorWindow.uss
        // on the --key/--value modifier classes.
        valueContainer.style.borderLeftColor = DictionaryDrawer.SharedStyles.k_RowsSplitColor.color;

        ApplyKeyColumnWidth(keyContainer);

        // Build the row's two PropertyFields up-front and parent them under the
        // key/value containers. They start unbound — BindListItem just calls
        // BindProperty on them every time the row is reused, hitting the fast
        // rebind path in PropertyField.ResetInternal so the child field tree is
        // not torn down and rebuilt per row.
        var keyField = new PropertyField(property: null, label: string.Empty, showFirstFoldoutHeader: false);
        var valueField = new PropertyField(property: null, label: string.Empty, showFirstFoldoutHeader: false);
        keyContainer.Add(keyField);
        valueContainer.Add(valueField);

        var selectionIndicator = new VisualElement();
        selectionIndicator.AddToClassList(k_SelectionIndicatorClass);
        selectionIndicator.pickingMode = PickingMode.Ignore;

        container.Add(duplicateKeyIcon);
        container.Add(keyContainer);
        container.Add(valueContainer);
        container.Add(selectionIndicator);

        container.RegisterCallback<PointerDownEvent>(OnRowPointerDown, TrickleDown.TrickleDown);

        return container;
    }

    void BindListItem(VisualElement element, int displayIndex)
    {
        using var _ = s_BindListItemMarker.Auto();

        if (!TryGetArrayIndexForBinding(element, displayIndex, out int arrayIndex))
            return;

        ApplyAlternatingBackground(element, displayIndex);

        var arrayElement = m_ArrayProperty.GetArrayElementAtIndex(arrayIndex);
        DictionaryDrawer.GetKeyAndValueProperties(arrayElement, out var keyProp, out var valueProp);

        var keyContainer = element.Q<VisualElement>("key-container");
        var valueContainer = element.Q<VisualElement>("value-container");
        ApplyKeyColumnWidth(keyContainer);

        // The row's PropertyFields were created in MakeListItem; rebinding flips
        // their target property without recreating the child field tree. When the
        // key/value lookup unexpectedly returns null (e.g. corrupted entry), the
        // stale binding is dropped via Unbind() instead of leaving the previous
        // row's data visible.
        RebindCellField(keyContainer, keyProp);
        UpdateDuplicateKeyIconVisibility(element, arrayIndex);
        RebindCellField(valueContainer, valueProp);
    }

    static void RebindCellField(VisualElement container, SerializedProperty property)
    {
        var field = container.Q<PropertyField>();
        if (field == null)
            return;
        if (property != null)
            field.BindProperty(property);
        else
            field.Unbind();
    }

    void UnbindListItem(VisualElement element, int displayIndex)
    {
        using var _ = s_UnbindListItemMarker.Auto();
        UnbindRowPropertyFields(element);
    }

    static void UnbindRowPropertyFields(VisualElement element)
    {
        UnbindCellField(element.Q<VisualElement>("key-container"));
        UnbindCellField(element.Q<VisualElement>("value-container"));
    }

    static void UnbindCellField(VisualElement container)
    {
        var field = container?.Q<PropertyField>();
        field?.Unbind();
    }

    static void DestroyListItem(VisualElement element)
    {
        UnbindRowPropertyFields(element);
    }

    bool TryGetArrayIndexForBinding(VisualElement element, int displayIndex, out int arrayIndex)
    {
        arrayIndex = -1;
        if (displayIndex < 0 || displayIndex >= displayedItemCount)
            return false;

        element.userData = displayIndex;
        arrayIndex = DisplayToArrayIndex(displayIndex);
        return true;
    }

    // Updates the only row-cell property that changes at runtime: the key
    // column width (driven by the resizer fraction). Everything else about the
    // cell layout — flex grow/shrink, min-width, padding, border width,
    // alignment — is defined statically in UnityEngine.UIElements.uss.
    void ApplyKeyColumnWidth(VisualElement keyContainer)
    {
        float keyWidth = GetHeaderSplitLineX();
        keyContainer.style.flexBasis = keyWidth;
        keyContainer.style.width = keyWidth;
    }

    // Paint the alternating row background inline rather than relying on the
    // framework's .unity-collection-view__item--alternative-background class.
    // The inline style.backgroundColor overrides every CSS rule, including the
    // framework's .unity-collection-view__item:hover:enabled and
    // unity-collection-view:focus:enabled .unity-collection-view__item--selected
    // rules, so the row stays at this color through hover and selection. The
    // selected-state outline is communicated separately by
    // .unity-dictionary-view__selection-indicator (see UnityEngine.UIElements.uss).
    static void ApplyAlternatingBackground(VisualElement element, int displayIndex)
    {
        element.style.backgroundColor = displayIndex % 2 != 0
            ? DictionaryDrawer.SharedStyles.k_AlternatingRowColor.color
            : Color.clear;
    }

    static VisualElement MakeEmptyElement()
    {
        var label = new Label(DictionaryDrawer.Texts.EmptyDictionaryLabel);
        label.AddToClassList(k_EmptyLabelClass);
        return label;
    }

    void OnFocusIn(FocusInEvent evt)
    {
        SetFocusedClass(IsListViewKeyboardNavTarget(evt.target as VisualElement));
    }

    void OnFocusOut(FocusOutEvent evt)
    {
        // The matching FocusInEvent that follows would set the correct state
        // anyway, but FocusIn doesn't fire when focus moves to a different
        // panel (e.g. clicking another EditorWindow), so reflect the loss of
        // ListView keyboard focus eagerly here too.
        var nextFocus = evt.relatedTarget as VisualElement;
        SetFocusedClass(IsInDrawerSubtree(nextFocus) && IsListViewKeyboardNavTarget(nextFocus));
    }

    bool IsInDrawerSubtree(VisualElement element)
    {
        return element != null && element.FindCommonAncestor(this) == this;
    }

    // The selection outline only goes blue when the ListView itself owns
    // keyboard focus (arrow keys navigate rows). Once focus drops into a
    // PropertyField inside a row those keys belong to the field, so we
    // signal that with the inactive (grey) outline. The ListView delegates
    // focus to its scrollView's contentContainer (the only focusable leaf
    // in the BaseVerticalCollectionView hierarchy), so that's what we
    // compare evt.target against.
    bool IsListViewKeyboardNavTarget(VisualElement target)
    {
        if (target == null)
            return false;
        if (target == this)
            return true;

        var listScrollView = scrollView;
        return listScrollView != null && (target == listScrollView || target == listScrollView.contentContainer);
    }

    void SetFocusedClass(bool focused)
    {
        EnableInClassList(k_ListViewFocusedClass, focused);
    }

    void FocusContentContainer()
    {
        scrollView?.contentContainer?.Focus();
    }

    void OnRowPointerDown(PointerDownEvent evt)
    {
        if (!(evt.currentTarget is VisualElement row) || !(row.userData is int displayIndex))
            return;

        var selectionSnapshot = new HashSet<int>(SelectedDisplayIndices);
        schedule.Execute(() =>
        {
            if (new HashSet<int>(SelectedDisplayIndices).SetEquals(selectionSnapshot))
                SetSelection(displayIndex);
        });
    }

    // Header columns mirror what ApplyKeyColumnWidth does for row cells: only
    // the resizer-driven width is dynamic, so we just push it onto flex-basis
    // and width on both header columns. All other layout (flex grow/shrink,
    // min-width, padding-left/right around the resizer) lives in USS.
    void ApplyHeaderColumnLayout()
    {
        if (m_ColumnResizer == null || m_KeyHeader == null || m_ValueLabel == null || m_ListHeader == null)
            return;

        float headerContentWidth = m_ListHeader.contentRect.width;
        float keyWidth = GetHeaderSplitLineX();
        float valueWidth = Mathf.Max(0f, headerContentWidth - keyWidth - DictionaryDrawer.k_VerticalSplitterWidth);

        m_KeyHeader.style.flexBasis = keyWidth;
        m_KeyHeader.style.width = keyWidth;

        m_ValueLabel.style.flexBasis = valueWidth;
        m_ValueLabel.style.width = valueWidth;
    }

    float GetHeaderSplitLineX()
    {
        if (m_ColumnResizer == null || m_ListHeader == null)
            return 0f;

        float splitAreaWidth = Mathf.Max(0f, m_ListHeader.contentRect.width - DictionaryDrawer.k_VerticalSplitterWidth);
        return Mathf.Round(DictionaryDrawer.GetKeyColumnPixelWidth(m_ColumnResizer.KeyColumnFraction, splitAreaWidth));
    }

    void UpdateColumnWidths()
    {
        ApplyHeaderColumnLayout();

        if (scrollView?.contentContainer == null)
            return;

        foreach (var row in scrollView.contentContainer.Children())
        {
            var keyContainer = row.Q<VisualElement>("key-container");
            if (keyContainer != null)
                ApplyKeyColumnWidth(keyContainer);
        }
    }

    // Marks that the keys may have changed and queues a single deferred pass.
    // Calling this again before the pass runs has no extra effect. The actual
    // decision (hash check + interaction gate) lives in SortIfNeeded, which
    // itself returns without doing any work if the keys' content hash hasn't
    // actually moved.
    void CheckIfKeysChangedAndSortIfNeeded()
    {
        if (m_SortScheduled)
            return;
        ScheduleSort(0);
    }

    void ScheduleSort(long delayMs)
    {
        m_SortScheduled = true;
        var item = schedule.Execute(SortIfNeeded);
        if (delayMs > 0)
            item.StartingIn(delayMs);
    }

    // Builds the sorted order once and reuses it.
    // If the keys did not actually change we don't want to pay the
    // cost of sorting as this can be expensive for a large dictionary
    // If the order matches what we already have we skip the (expensive) ListView rebuild and only refresh the
    // duplicate markers, which is much cheaper for large dictionaries.
    void SortIfNeeded()
    {
        // If the user is interacting with the Editor we wait sorting to prevent disrupting the workflow
        if (!IsReadyToSortByKey())
        {
            ScheduleSort(DictionaryDrawer.k_SortRetryDelayMs);
            return;
        }

        m_SortScheduled = false;

        var newHashOfKeys = DictionaryDrawer.GetKeysContentHash(m_ArrayProperty);
        if (newHashOfKeys == m_HashOfKeys)
            return;

        var updatedSortedIndexMap = DictionaryDrawer.SortedIndexMap.Build(m_ArrayProperty, m_SortAscending);
        m_HashOfKeys = newHashOfKeys;

        if (m_SortedIndexMap.DisplayOrderEquals(updatedSortedIndexMap))
        {
            if (UpdateDuplicateIndicesOnly())
            {
                UpdateHeaderInfo();
                UpdateDuplicateKeyIconsOnVisibleItems();
            }
            return;
        }

        var selectedArrayIndices = GetSelectedArrayIndices();
        m_SortedIndexMap = updatedSortedIndexMap;
        RefreshListView();
        RestoreSelectionByArrayIndices(selectedArrayIndices);
        FocusContentContainer();
    }

    void OnKeyHeaderClicked(ClickEvent evt)
    {
        m_SortAscending = !m_SortAscending;
        UpdateSortIndicatorClass();
        DictionaryDrawer.UpdateCachedState(m_StateCacheKey, state => state.sortAscending = m_SortAscending);
        RebuildSortedIndicesAndRefresh();
    }

    void ResetToDefaults()
    {
        DictionaryDrawer.ClearCachedState(m_StateCacheKey);

        m_SortAscending = true;
        UpdateSortIndicatorClass();

        m_ColumnResizer?.ResetToDefaultFraction();
        RebuildSortedIndicesAndRefresh();
    }

    void UpdateSortIndicatorClass()
    {
        m_KeyHeader.EnableInClassList(MultiColumnHeaderColumn.sortedAscendingUssClassName, m_SortAscending);
        m_KeyHeader.EnableInClassList(MultiColumnHeaderColumn.sortedDescendingUssClassName, !m_SortAscending);
    }

    void UpdateDuplicateKeyIconVisibility(VisualElement rowElement, int arrayIndex)
    {
        var icon = rowElement?.Q<VisualElement>(className: k_DuplicateKeyIconClass);
        if (icon == null)
            return;

        if (DictionaryKeyUtility.GetMarkerKind(arrayIndex, m_DuplicateEntryIndices) != DictionaryKeyUtility.KeyMarkerKind.None)
        {
            // Icon, size, and top-offset all live in USS on
            // .unity-dictionary-view__duplicate-key-icon; we only flip display + tooltip
            // from C# based on duplicate-state.
            icon.style.display = DisplayStyle.Flex;
            icon.tooltip = DictionaryDrawer.Texts.DuplicateMarkerTooltip;
        }
        else
        {
            icon.style.display = DisplayStyle.None;
            icon.tooltip = null;
        }
    }

    // Thin wrapper around the shared in-place refresh so call sites stay
    // self-documenting at the UITK layer.
    bool UpdateDuplicateIndicesOnly()
        => DictionaryDrawer.TryRefreshDuplicateIndicesInto(m_DictionaryFieldProperty, m_DuplicateEntryIndices);

    void UpdateDuplicateKeyIconsOnVisibleItems()
    {
        var content = scrollView.contentContainer;
        foreach (var wrapper in content.Children())
        {
            var dictElement = wrapper.Q("dict-element");
            if (dictElement != null && dictElement.userData is int displayIndex && displayIndex < displayedItemCount)
                UpdateDuplicateKeyIconVisibility(dictElement, DisplayToArrayIndex(displayIndex));
        }
    }

    void OnValidateCommand(ValidateCommandEvent evt)
    {
        if (evt.commandName == "Duplicate" && SelectedDisplayIndices.Count == 1)
            evt.StopPropagation();
    }

    void OnExecuteCommand(ExecuteCommandEvent evt)
    {
        if (evt.commandName == "Duplicate" && SelectedDisplayIndices.Count == 1)
        {
            OnAddClicked();
            evt.StopPropagation();
        }
    }

    // Renamed from OnKeyDown to avoid hiding BaseVerticalCollectionView.OnKeyDown,
    // which is a public method on the base class that the binding system calls
    // through the registered key-event path. Keeping a distinct name lets the
    // base behavior continue to run unchanged while we layer in our F-key
    // scroll-to-selection shortcut.
    void OnDictionaryKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.F && !evt.ctrlKey && !evt.commandKey && !evt.altKey
            && SelectedDisplayIndices.Count > 0)
        {
            ScrollToItem(SelectedDisplayIndices[0]);
            evt.StopPropagation();
        }
    }

    void OnAddClicked()
    {
        var selected = SelectedDisplayIndices;
        int singleSelectedDisplayIndex = selected.Count == 1 ? selected[0] : -1;
        int lastIndex = DictionaryDrawer.InsertOrDuplicateSelectedEntry(
            m_ArrayProperty, m_SortedIndexMap, singleSelectedDisplayIndex);

        RebuildSortedIndices();
        RefreshListView();
        int newDisplayIndex = ArrayIndexToDisplayIndex(lastIndex);
        SetSelection(newDisplayIndex);
        FocusContentContainer();
        schedule.Execute(() => ScrollToItem(newDisplayIndex));

        UpdateHeaderInfo();
        UpdateRemoveButtonState();
    }

    void RebuildSortedIndices()
    {
        m_SortedIndexMap = DictionaryDrawer.SortedIndexMap.Build(m_ArrayProperty, m_SortAscending);
        m_HashOfKeys = DictionaryDrawer.GetKeysContentHash(m_ArrayProperty);
    }

    void RebuildSortedIndicesAndRefresh()
    {
        var selectedArrayIndices = GetSelectedArrayIndices();
        RebuildSortedIndices();
        RefreshListView();
        RestoreSelectionByArrayIndices(selectedArrayIndices);
        FocusContentContainer();
    }

    List<int> GetSelectedArrayIndices()
    {
        m_SelectedArrayIndicesScratch.Clear();
        foreach (var displayIndex in SelectedDisplayIndices)
        {
            if (displayIndex >= 0 && displayIndex < displayedItemCount)
                m_SelectedArrayIndicesScratch.Add(DisplayToArrayIndex(displayIndex));
        }
        return m_SelectedArrayIndicesScratch;
    }

    // O(k log k) for k selected entries: previously O(n*k) because every
    // display row called arrayIndices.Contains(...) on the selection list.
    // The reverse map in m_SortedIndexMap turns each lookup into O(1); we
    // sort the resulting display indices so SetSelection / ScrollToItem
    // get the same ascending order they used to receive when iterating
    // displayIndex from 0 to displayedItemCount.
    void RestoreSelectionByArrayIndices(List<int> arrayIndices)
    {
        if (arrayIndices.Count == 0)
            return;
        m_RestoredDisplayIndicesScratch.Clear();
        foreach (var arrayIdx in arrayIndices)
        {
            if (m_SortedIndexMap.ContainsArrayIndex(arrayIdx))
                m_RestoredDisplayIndicesScratch.Add(m_SortedIndexMap.ToDisplayIndex(arrayIdx));
        }
        if (m_RestoredDisplayIndicesScratch.Count > 0)
        {
            m_RestoredDisplayIndicesScratch.Sort();
            SetSelection(m_RestoredDisplayIndicesScratch);
            ScrollToItem(m_RestoredDisplayIndicesScratch[0]);
        }
    }

    void RefreshListView()
    {
        UpdateDuplicateIndicesOnly();
        UpdateHeaderInfo();
        UpdateListViewItemsSource(displayedItemCount);

        RefreshItems();
    }

    // Keeps m_ItemsSource sized to arraySize and wired up as the
    // ListView's itemsSource. ListView requires an IList of the right Count;
    // we own and reuse a single List<int> across rebuilds so structural
    // changes only pay for the delta (append on growth, RemoveRange on shrink)
    // instead of allocating a fresh list every time.
    void UpdateListViewItemsSource(int arraySize)
    {
        if (m_ItemsSource == null)
        {
            m_ItemsSource = new List<int>(arraySize);
            itemsSource = m_ItemsSource;
        }
        for (int i = m_ItemsSource.Count; i < arraySize; i++)
            m_ItemsSource.Add(i);
        if (m_ItemsSource.Count > arraySize)
            m_ItemsSource.RemoveRange(arraySize, m_ItemsSource.Count - arraySize);
    }

    void UpdateHeaderInfo()
    {
        int itemCount = m_ArrayProperty.arraySize;
        int duplicateCount = m_DuplicateEntryIndices.Count;
        bool hasDuplicates = duplicateCount > 0;

        if (m_HeaderInfoLabel != null)
        {
            string text = DictionaryDrawer.Texts.GetItemCountText(itemCount);
            if (hasDuplicates)
                text += DictionaryDrawer.Texts.GetDuplicateCountText(duplicateCount);
            m_HeaderInfoLabel.text = text;
        }
        if (m_DuplicatesHelpBox != null)
        {
            if (hasDuplicates)
            {
                m_DuplicatesHelpBox.text = DictionaryDrawer.Texts.GetDuplicatesHelpBoxText(duplicateCount);
                m_DuplicatesHelpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_DuplicatesHelpBox.style.display = DisplayStyle.None;
            }
        }
    }

    void OnRemoveClicked()
    {
        var selected = SelectedDisplayIndices;
        int newSelectedDisplayIndex = selected.Count == 1 ? selected[0] : -1;

        bool removed = DictionaryDrawer.RemoveEntriesAtDisplayIndices(
            m_ArrayProperty, selected, m_SortedIndexMap);
        if (!removed)
            return;

        RebuildSortedIndices();
        RefreshListView();
        UpdateHeaderInfo();

        int newSize = displayedItemCount;
        if (newSize <= 0 || newSelectedDisplayIndex < 0)
            ClearSelection();
        else
            SetSelection(Mathf.Min(newSelectedDisplayIndex, newSize - 1));

        FocusContentContainer();
        UpdateRemoveButtonState();
    }

    void UpdateRemoveButtonState()
    {
        // BaseListView.allowRemove drives the built-in remove button's enabled
        // state through UpdateRemoveButton(), which honors both this flag and
        // the item count — same UX as the previous hand-built footer.
        allowRemove = SelectedDisplayIndices.Count > 0;
    }

    /// <summary>
    /// Draggable column resizer placed between key and value header labels.
    /// Manages the key-column fraction, persists manual overrides via
    /// <see cref="StateCache{T}"/>, and notifies the owner when the fraction
    /// changes.
    /// </summary>
    sealed class ColumnResizer
    {
        // Visible 1px line width. The 20px hit area and its negative left
        // offset that centers it on the line are defined in USS
        // (.unity-dictionary-view__column-resizer); only the line width is needed
        // here, for the drag-start width calculation.
        const float k_ResizerLineWidth = 1f;

        // In-memory, editor-process lifetime. Key: same Hash128 as s_StateCache (normalized path).
        // Holds the set of ColumnResizer instances across which fraction changes propagate.
        // Eviction: per-resizer in OnDetachFromPanel; entry removed when its list empties.
        // Registration is gated on m_IsPartOfList, so top-level dictionaries never enter this table.
        static readonly Dictionary<Hash128, List<ColumnResizer>> s_LinkedResizers = new();

        readonly VisualElement m_Header;
        readonly Hash128 m_StateCacheKey;
        readonly Action m_OnFractionChanged;
        readonly bool m_IsPartOfList;
        readonly float m_AttributeFraction;

        float m_KeyColumnFraction;
        bool m_IsDragging;
        float m_DragStartFraction;
        float m_DragStartX;
        float m_DragStartHeaderContentWidth;

        public float KeyColumnFraction => m_KeyColumnFraction;

        public ColumnResizer(VisualElement header, string propertyPath, float attributeFraction, Action onFractionChanged)
        {
            m_Header = header;
            m_OnFractionChanged = onFractionChanged;
            m_IsPartOfList = Regex.IsMatch(propertyPath, @"\[\d+\]");
            m_AttributeFraction = attributeFraction;
            m_StateCacheKey = DictionaryDrawer.ComputeStateCacheKey(propertyPath);
            m_KeyColumnFraction = DictionaryDrawer.GetActiveKeyColumnFraction(m_StateCacheKey, attributeFraction);
        }

        public VisualElement BuildElement()
        {
            // Line + interaction layout (size, position, cursor) is defined in
            // UnityEngine.UIElements.uss; the line color is the only piece that
            // needs to be set inline, since it's a SkinnedColor.
            var line = new VisualElement();
            line.AddToClassList(k_ColumnResizerLineClass);
            line.style.backgroundColor = DictionaryDrawer.SharedStyles.k_ResizerColor.color;

            var interaction = new VisualElement();
            interaction.AddToClassList(k_ColumnResizerClass);
            line.Add(interaction);

            interaction.RegisterCallback<PointerDownEvent>(OnPointerDown);
            interaction.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            interaction.RegisterCallback<PointerUpEvent>(OnPointerUp);
            interaction.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            if (m_IsPartOfList)
            {
                line.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                line.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            return line;
        }

        public void ResetToDefaultFraction()
        {
            m_KeyColumnFraction = m_AttributeFraction;
            m_OnFractionChanged?.Invoke();
            if (m_IsPartOfList)
                NotifyLinkedResizers();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!s_LinkedResizers.TryGetValue(m_StateCacheKey, out var list))
            {
                list = new List<ColumnResizer>();
                s_LinkedResizers[m_StateCacheKey] = list;
            }
            list.Add(this);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (s_LinkedResizers.TryGetValue(m_StateCacheKey, out var list))
            {
                list.Remove(this);
                if (list.Count == 0)
                    s_LinkedResizers.Remove(m_StateCacheKey);
            }
        }

        void NotifyLinkedResizers()
        {
            if (!s_LinkedResizers.TryGetValue(m_StateCacheKey, out var list) || list.Count <= 1)
                return;

            foreach (var resizer in list)
            {
                if (resizer == this)
                    continue;
                resizer.m_KeyColumnFraction = m_KeyColumnFraction;
                resizer.m_OnFractionChanged?.Invoke();
            }
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            var resizer = evt.currentTarget as VisualElement;
            resizer.CapturePointer(evt.pointerId);
            m_IsDragging = true;
            m_DragStartFraction = m_KeyColumnFraction;
            m_DragStartX = evt.position.x;
            m_DragStartHeaderContentWidth = Mathf.Max(1f, m_Header.contentRect.width - k_ResizerLineWidth);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_IsDragging || m_DragStartHeaderContentWidth <= 0)
                return;

            float deltaX = evt.position.x - m_DragStartX;
            float newFraction = m_DragStartFraction + deltaX / m_DragStartHeaderContentWidth;
            m_KeyColumnFraction = DictionaryDrawer.ClampDraggedKeyColumnFraction(newFraction, m_DragStartHeaderContentWidth);
            m_OnFractionChanged?.Invoke();
            if (m_IsPartOfList)
                NotifyLinkedResizers();
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!m_IsDragging)
                return;

            var resizer = evt.currentTarget as VisualElement;
            resizer.ReleasePointer(evt.pointerId);
            m_IsDragging = false;

            DictionaryDrawer.UpdateCachedState(m_StateCacheKey, state => state.keyColumnFractionSetByUser = m_KeyColumnFraction);
            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            m_IsDragging = false;
        }
    }
}

} // end of namespace
