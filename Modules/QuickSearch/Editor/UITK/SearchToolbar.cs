// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchToolbar : UIElements.Toolbar, ISearchElement
    {
        private static readonly string openSaveSearchesIconTooltip = L10n.Tr("Toggle Saved Searches Panel");

        public static readonly string queryBuilderIconTooltip = L10n.Tr("Toggle Query Builder Mode");

        public static readonly string previewInspectorButtonTooltip = L10n.Tr("Toggle Inspector Panel");

        public static readonly string saveQueryButtonTooltip = L10n.Tr("Save search query as an asset.");

        public static readonly string pressToFilterTooltip = L10n.Tr("Press Tab \u21B9 to filter");

        protected readonly ISearchView m_ViewModel;
        protected readonly Toggle m_SearchQueryToggle;
        protected readonly Toggle m_QueryBuilderToggle;
        protected readonly Toggle m_InspectorToggle;
        protected readonly SearchFieldElement m_SearchField;
        private readonly ToolbarMenu s_SaveQueryDropdown;

        public virtual SearchContext context => m_ViewModel.context;
        public virtual SearchViewState viewState => m_ViewModel.state;

        public SearchFieldElement searchField => m_SearchField;
        internal QueryBuilder queryBuilder => m_SearchField.queryBuilder;

        public new static readonly string ussClassName = "search-toolbar";
        public static readonly string buttonClassName = ussClassName.WithUssElement("button");
        public static readonly string dropdownClassName = ussClassName.WithUssElement("dropdown");
        public static readonly string savedSearchesButtonClassName = ussClassName.WithUssElement("saved-searches-button");
        public static readonly string queryBuilderButtonClassName = ussClassName.WithUssElement("query-builder-button");
        public static readonly string inspectorButtonClassName = ussClassName.WithUssElement("inspector-button");
        public static readonly string saveQueryButtonClassName = ussClassName.WithUssElement("save-query-button");

        public SearchToolbar(string name, ISearchView viewModel)
        {
            this.name = name;
            m_ViewModel = viewModel;

            AddToClassList(ussClassName);

            var window = viewModel as SearchWindow;

            // Search query panel toggle
            if (viewState.hasQueryPanel)
            {
                m_SearchQueryToggle = SearchElement.CreateToolbarToggle(
                    "SearchQueryPanelToggle",
                    (evt) => UpdateToggleTooltipEvent(evt, m_SearchQueryToggle, openSaveSearchesIconTooltip, SearchWindow.toggleSavedSearchesPanelShortcutId),
                    viewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel),
                    OnToggleSearchQueryPanel,
                    buttonClassName,
                    savedSearchesButtonClassName);
                Add(m_SearchQueryToggle);
            }

            // Query builder toggle
            if (viewState.hasQueryBuilderToggle)
            {
                m_QueryBuilderToggle = SearchElement.CreateToolbarToggle(
                    "SearchQueryBuilderToggle",
                    (evt) => UpdateToggleTooltipEvent(evt, m_QueryBuilderToggle, queryBuilderIconTooltip, SearchWindow.toggleQueryBuilderModeShortcutId),
                    viewState.queryBuilderEnabled,
                    OnToggleQueryBuilder,
                    buttonClassName,
                    queryBuilderButtonClassName);
                Add(m_QueryBuilderToggle);
            }

            // Search field
            m_SearchField = new SearchFieldElement(nameof(SearchFieldElement), viewModel, useSearchGlobalEventHandler:true);
            Add(m_SearchField);

            // Save query dropdown
            if (viewState.hasQueryPanel)
            {
                s_SaveQueryDropdown = SearchElement.Create<ToolbarMenu, MouseUpEvent>("SearchSaveQueryMenu", OnSaveQueryDropdown, buttonClassName, dropdownClassName, saveQueryButtonClassName);
                Add(s_SaveQueryDropdown);
                UpdateSaveQueryButton();
            }

            // Details view panel toggle.
            if (viewState.flags.HasNone(SearchViewFlags.DisableInspectorPreview))
            {
                m_InspectorToggle = SearchElement.CreateToolbarToggle(
                    "SearchInspectorToggle",
                    (evt) => UpdateToggleTooltipEvent(evt, m_InspectorToggle, previewInspectorButtonTooltip, SearchWindow.toggleInspectorPanelShortcutId),
                    viewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview),
                    OnToggleInspector,
                    buttonClassName,
                    inspectorButtonClassName);
                Add(m_InspectorToggle);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void UpdateSaveQueryButton()
        {
            if (s_SaveQueryDropdown == null)
                return;

            s_SaveQueryDropdown.SetEnabled(!context.empty);
        }

        private void OnToggleQueryBuilder(ChangeEvent<bool> evt)
        {
            if (m_ViewModel is SearchWindow window)
                window.ToggleQueryBuilder();

            m_SearchField.ToggleQueryBuilder();
        }

        private void OnToggleSearchQueryPanel(ChangeEvent<bool> evt)
        {
            if (m_ViewModel is SearchWindow window)
                window.TogglePanelView(SearchViewFlags.OpenLeftSidePanel);
        }

        private void OnToggleInspector(ChangeEvent<bool> evt)
        {
            if (m_ViewModel is SearchWindow window)
                window.TogglePanelView(SearchViewFlags.OpenInspectorPreview);
        }

        private void OnSaveQuery(ISearchQueryView window)
        {
            var saveQueryMenu = new GenericMenu();
            if (m_ViewModel.state.activeQuery != null)
            {
                saveQueryMenu.AddItem(new GUIContent($"Save {m_ViewModel.state.activeQuery.displayName}"), false, window.SaveActiveSearchQuery);
                saveQueryMenu.AddSeparator("");
            }

            AddSaveQueryMenuItems(window, saveQueryMenu);
            saveQueryMenu.ShowAsContext();
        }

        private void OnSaveQueryDropdown(MouseUpEvent evt)
        {
            if (m_ViewModel is ISearchQueryView window)
                OnSaveQuery(window);
        }

        private void AddSaveQueryMenuItems(ISearchQueryView window, GenericMenu saveQueryMenu)
        {
            saveQueryMenu.AddItem(new GUIContent("Save User"), false, window.SaveUserSearchQuery);
            saveQueryMenu.AddItem(new GUIContent("Save Project..."), false, window.SaveProjectSearchQuery);
            if (!string.IsNullOrEmpty(context.searchText))
            {
                saveQueryMenu.AddSeparator("");
                saveQueryMenu.AddItem(new GUIContent("Clipboard"), false, () => SaveQueryToClipboard(context.searchText));
            }
        }

        private void SaveQueryToClipboard(in string query)
        {
            var trimmedQuery = Utils.TrimText(query);
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, trimmedQuery);
            EditorGUIUtility.systemCopyBuffer = Utils.TrimText(trimmedQuery);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Dispatcher.On(SearchEvent.ViewStateUpdated, OnViewFlagsUpdated);
            Dispatcher.On(SearchEvent.SearchTextChanged, OnSearchQueryChanged);
            Dispatcher.On(SearchEvent.SearchContextChanged, OnSearchQueryChanged);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Dispatcher.Off(SearchEvent.ViewStateUpdated, OnViewFlagsUpdated);
            Dispatcher.Off(SearchEvent.SearchTextChanged, OnSearchQueryChanged);
            Dispatcher.Off(SearchEvent.SearchContextChanged, OnSearchQueryChanged);
        }

        private void OnSearchQueryChanged(ISearchEvent evt)
        {
            UpdateSaveQueryButton();
        }

        private void OnViewFlagsUpdated(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;

            m_QueryBuilderToggle?.SetValueWithoutNotify(evt.sourceViewState.queryBuilderEnabled);
            m_SearchQueryToggle?.SetValueWithoutNotify(evt.sourceViewState.flags.HasAny(SearchViewFlags.OpenLeftSidePanel));
            m_InspectorToggle?.SetValueWithoutNotify(evt.sourceViewState.flags.HasAny(SearchViewFlags.OpenInspectorPreview));
        }

        void UpdateToggleTooltipEvent(TooltipEvent evt, Toggle toggle, string baseTooltip, string shortcutId)
        {
            ShortcutBinding shortcutBinding = ShortcutBinding.empty;

            if (m_ViewModel is SearchWindow sw)
                shortcutBinding = sw.GetShortcutBinding(shortcutId);
            else
                shortcutBinding = Utils.GetShortcutBinding(shortcutId);

            evt.tooltip = shortcutBinding.Equals(ShortcutBinding.empty) ? baseTooltip : $"{baseTooltip} ({shortcutBinding})";
            evt.rect = toggle.worldBound;
        }
    }
}
