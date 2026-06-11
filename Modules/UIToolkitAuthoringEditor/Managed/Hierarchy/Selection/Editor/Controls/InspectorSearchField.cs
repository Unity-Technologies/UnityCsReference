// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal sealed partial class InspectorSearchField : VisualElement, IShortcutContext
{
    [Flags]
    enum SearchFilter
    {
        All      = 1 << 0,
        Overrides = 1 << 1,
        Variable  = 1 << 2,
        Binding   = 1 << 3
    }

    public const string UssClass = "unity-inspector-search-field";
    public const string FilterContainerUssClass = UssClass + "__filter-container";
    public const string FilterLabelUssClass = UssClass + "__filter-label";
    public const string EmptyStateUssClass = UssClass + "__empty-state";
    public const string EmptyStateLabelUssClass = UssClass + "__empty-state__label";
    public static readonly UniqueStyleString HiddenClass = new("search-result--hidden");

    private readonly ToolbarSearchField m_SearchField;
    private readonly VisualElement m_FilterContainer;
    private readonly ToggleButtonGroup m_FilterTypeButtons;
    private readonly VisualElement m_EmptyStateContainer;
    private readonly Label m_EmptyStateLabel;
    private readonly Button m_ClearFilterButton;
    private VisualElement m_SearchContainer;
    private SearchFilter m_ActiveFilters = SearchFilter.All;

    private static readonly SearchFilter[] k_AllFilters = (SearchFilter[])Enum.GetValues(typeof(SearchFilter));
    private static readonly int k_EnumLength = k_AllFilters.Length;

    private static string s_PersistentSearch = string.Empty;
    private static SearchFilter s_PersistentFilter = SearchFilter.All;

    public bool active { get; set; }

    public VisualElement SearchContainer
    {
        get => m_SearchContainer;
        set
        {
            m_SearchContainer = value;
            m_SearchContainer?.Add(m_EmptyStateContainer);
        }
    }

    internal ToolbarSearchField toolbarSearchField => m_SearchField;

    public InspectorSearchField()
    {
        AddToClassList(UssClass);

        m_SearchField = new ToolbarSearchField();
        m_SearchField.placeholderText = "Filter attributes and properties";
        m_SearchField.RegisterValueChangedCallback(OnSearchChanged);
        m_SearchField.style.flexGrow = 1;

        // Create the filter container with toggle button group
        m_FilterContainer = new VisualElement().WithClassList(FilterContainerUssClass);

        var typeLabel = new Label("Filter:").WithClassList(FilterLabelUssClass);

        m_FilterTypeButtons = new ToggleButtonGroup();
        m_FilterTypeButtons.allowEmptySelection = false;
        m_FilterTypeButtons.RegisterValueChangedCallback(OnFilterTypeChanged);

        // Add buttons for each enum value
        foreach (SearchFilter filter in k_AllFilters)
        {
            var button = new Button { text = filter.ToString() };
            // Support multi selection with shift
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.modifiers == EventModifiers.Shift)
                {
                    m_ActiveFilters |= filter;
                    m_FilterTypeButtons.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(m_ActiveFilters, k_EnumLength));
                    s_PersistentFilter = m_ActiveFilters;
                }
            });
            m_FilterTypeButtons.Add(button);
        }

        // Set "All" as the default selection
        m_FilterTypeButtons.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(SearchFilter.All, k_EnumLength));

        m_FilterContainer.Add(typeLabel);
        m_FilterContainer.Add(m_FilterTypeButtons);

        // Create empty state container
        m_EmptyStateContainer = new VisualElement() { name = EmptyStateUssClass }.WithClassList(EmptyStateUssClass);

        m_EmptyStateLabel = new Label().WithClassList(EmptyStateLabelUssClass);

        m_ClearFilterButton = new Button(OnClearFilterClicked);
        m_ClearFilterButton.text = "Clear Filter";

        m_EmptyStateContainer.Add(m_EmptyStateLabel);
        m_EmptyStateContainer.Add(m_ClearFilterButton);

        // Restore persisted search state from previous selection
        if (!string.IsNullOrEmpty(s_PersistentSearch))
            m_SearchField.SetValueWithoutNotify(s_PersistentSearch);
        if (s_PersistentFilter != SearchFilter.All) {
            m_ActiveFilters = s_PersistentFilter;
            m_FilterTypeButtons.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(m_ActiveFilters, k_EnumLength));
        }

        Add(m_SearchField);
        Add(m_FilterContainer);
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
            {
                if (attachToPanelEvent.destinationPanel == null)
                    return;
                ShortcutIntegration.instance.contextManager.RegisterToolContext(this);
                break;
            }
            case DetachFromPanelEvent detachFromPanelEvent:
            {
                if (detachFromPanelEvent.originPanel == null)
                    return;
                ShortcutIntegration.instance.contextManager.DeregisterToolContext(this);
                break;
            }
            case FocusInEvent:
                if (!string.IsNullOrEmpty(m_SearchField.value) || !m_ActiveFilters.HasFlag(SearchFilter.All))
                    ApplyCurrentFilter();
                break;
        }
        base.HandleEventBubbleUp(evt);
    }

    internal void ResetSearch()
    {
        s_PersistentSearch = string.Empty;
        s_PersistentFilter = SearchFilter.All;
        m_ActiveFilters = SearchFilter.All;
        m_SearchField.SetValueWithoutNotify(string.Empty);
        m_FilterTypeButtons.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(SearchFilter.All, k_EnumLength));
        ClearSearch();
    }

    [Shortcut("UI Inspector/Find", typeof(InspectorSearchField), KeyCode.F, ShortcutModifiers.Action)]
    static void OnFind(ShortcutArguments args)
    {
        var searchField = args.context as InspectorSearchField;
        searchField?.toolbarSearchField.Focus();
    }

    private void OnFilterTypeChanged(ChangeEvent<ToggleButtonGroupState> evt)
    {
        m_ActiveFilters = StateToFlags(evt.newValue);

        s_PersistentFilter = m_ActiveFilters;
        ApplyCurrentFilter();
    }

    private static SearchFilter StateToFlags(ToggleButtonGroupState state)
    {
        SearchFilter result = 0;
        foreach (var index in state.GetActiveOptions(stackalloc int[state.length]))
            result |= (SearchFilter)(1 << index);
        return result;
    }

    private void OnSearchChanged(ChangeEvent<string> evt)
    {
        s_PersistentSearch = evt.newValue;
        ApplyCurrentFilter();
    }

    internal void ApplyCurrentFilter()
    {
        if (m_SearchContainer == null)
            return;

        var searchText = m_SearchField.value;

        if (string.IsNullOrEmpty(searchText) && m_ActiveFilters.HasFlag(SearchFilter.All))
        {
            ClearSearch();
        }
        else
        {
            ApplySearch(searchText);
        }
    }

    public void ClearSearch()
    {
        if (m_SearchContainer == null) return;
        var rows = m_SearchContainer.Query<OverrideRow>().Build();
        foreach (var row in rows)
        {
            if (IsInHeader(row))
                continue;
            row.EnableInClassList(HiddenClass, false);
        }

        m_EmptyStateContainer.style.display = DisplayStyle.None;
    }

    public void ApplySearch(string str)
    {
        var hasSearchText = !string.IsNullOrEmpty(str);
        var filterIsAll = m_ActiveFilters.HasFlag(SearchFilter.All);

        var rows = m_SearchContainer.Query<OverrideRow>().Build();
        int visibleCount = 0;
        var expandedFoldouts = HashSetPool<OverrideFoldout>.Get();
        try
        {
            foreach (var row in rows)
            {
                if (IsInHeader(row))
                    continue;

                bool shouldShow;

                if (!hasSearchText)
                {
                    shouldShow = filterIsAll || RowMatchesActiveFilters(row);
                }
                else
                {
                    bool matchesSearch = false;
                    foreach (var property in row.trackedProperties)
                    {
                        if (property.Contains(str, StringComparison.OrdinalIgnoreCase))
                        {
                            matchesSearch = true;
                            break;
                        }
                    }

                    // Overrides + search: the matching property name must be in the per-property override map.
                    shouldShow = matchesSearch && (filterIsAll || RowMatchesActiveFiltersWithSearch(row, str));
                }

                if (shouldShow)
                {
                    row.EnableInClassList(HiddenClass, false);
                    if (row is OverrideFoldout foldout)
                        foldout.value = true;
                    ShowAllParentFoldouts(row, expandedFoldouts);
                    visibleCount++;
                }
                else
                {
                    row.EnableInClassList(HiddenClass, true);
                }
            }
        }
        finally
        {
            HashSetPool<OverrideFoldout>.Release(expandedFoldouts);
        }

        // Show or hide empty state based on results
        if (visibleCount == 0 && (hasSearchText || !filterIsAll))
        {
            UpdateEmptyState(str);
            m_EmptyStateContainer.style.display = DisplayStyle.Flex;
        }
        else
        {
            m_EmptyStateContainer.style.display = DisplayStyle.None;
        }
    }

    // Returns true if the row matches all currently active filters.
    private bool RowMatchesActiveFilters(OverrideRow row)
    {
        if (m_ActiveFilters.HasFlag(SearchFilter.Overrides) && !row.ClassListContains(row.GetIsOverriddenClassName())) return false;
        if (m_ActiveFilters.HasFlag(SearchFilter.Variable) && !RowHasVariable(row)) return false;
        if (m_ActiveFilters.HasFlag(SearchFilter.Binding) && !RowHasBinding(row)) return false;
        return true;
    }

    // Returns true if the row matches all active filters when combined with a search term.
    private bool RowMatchesActiveFiltersWithSearch(OverrideRow row, string str)
    {
        if (m_ActiveFilters.HasFlag(SearchFilter.Overrides) && !row.HasMatchingOverriddenProperty(str)) return false;
        if (m_ActiveFilters.HasFlag(SearchFilter.Variable) && !RowHasVariable(row)) return false;
        if (m_ActiveFilters.HasFlag(SearchFilter.Binding) && !RowHasBinding(row)) return false;
        return true;
    }

    private void UpdateEmptyState(string searchTerm)
    {
        string filterDescription;
        if (m_ActiveFilters == 0 || m_ActiveFilters.HasFlag(SearchFilter.All))
        {
            filterDescription = "results";
        }
        else
        {
            filterDescription = (m_ActiveFilters.HasFlag(SearchFilter.Overrides),
                                 m_ActiveFilters.HasFlag(SearchFilter.Variable),
                                 m_ActiveFilters.HasFlag(SearchFilter.Binding)) switch
            {
                (true,  false, false) => "overrides",
                (false, true,  false) => "variables",
                (false, false, true)  => "bindings",
                (true,  true,  false) => "overrides and variables",
                (true,  false, true)  => "overrides and bindings",
                (false, true,  true)  => "variables and bindings",
                _                     => "overrides, variables, and bindings"
            };
        }

        var displayText = !string.IsNullOrEmpty(searchTerm)
            ? $"No {filterDescription} for \"{searchTerm}\"."
            : $"No {filterDescription}.";

        m_EmptyStateLabel.text = $"{displayText}\n\nLooking for a custom property? It may need to be enabled for search.";
    }

    private static bool RowHasVariable(OverrideRow row)
    {
        if (row is OverrideFoldout foldout)
            return foldout.name == StyleRuleInspector.VariablesFoldoutName;
        return row.ClassListContains(StylePropertyBinding.k_VariableFieldUssClassName);
    }

    private static bool RowHasBinding(OverrideRow row) => row is not OverrideFoldout &&
        row.ClassListContains(StylePropertyBinding.k_BoundFieldUssClassName) ||
        row.ClassListContains(UxmlAttributeFieldDecorator.s_BoundFieldUssClassName);

    private void OnClearFilterClicked()
    {
        m_SearchField.value = string.Empty;
        m_FilterTypeButtons.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(SearchFilter.All, k_EnumLength));
        m_ActiveFilters = SearchFilter.All;

        s_PersistentSearch = m_SearchField.value;
        s_PersistentFilter = m_ActiveFilters;

        ClearSearch();
    }

    private static bool IsInHeader(VisualElement element) =>
        element.GetFirstAncestorOfType<UISelectionObjectHeader>() != null;

    private void ShowAllParentFoldouts(VisualElement element, HashSet<OverrideFoldout> expandedFoldouts)
    {
        var parent = element.parent;
        while (parent != null)
        {
            if (parent is OverrideFoldout foldout)
            {
                if (expandedFoldouts.Add(foldout))
                {
                    foldout.EnableInClassList(HiddenClass, false);
                    foldout.value = true;
                }
                else
                {
                    // Foldout already expanded, no need to traverse further up
                    break;
                }
            }
            parent = parent.parent;
        }
    }
}
