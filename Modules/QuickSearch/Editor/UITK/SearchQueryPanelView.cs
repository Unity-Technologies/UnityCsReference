// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    // TODO: Handle external saved query events (like name change from project browser)
    readonly struct SearchQuerySource
    {
        public int id { get; }
        public string name { get; }
        public string tooltip { get; }
        public Func<IEnumerable<ISearchQuery>> getter { get; }
        public Action saveSearchQuery { get; }
        public ISearchQueryItemHandler searchQueryItemHandler { get; }

        public SearchQuerySource(int id, string name, string tooltip, Func<IEnumerable<ISearchQuery>> getter, ISearchQueryItemHandler itemHandler, Action saveSearchQuery)
        {
            this.id = id;
            this.name = name;
            this.tooltip = tooltip;
            this.getter = getter;
            this.searchQueryItemHandler = itemHandler;
            this.saveSearchQuery = saveSearchQuery;
        }
    }

    class SearchQueryComparer : IComparer<ISearchQuery>
    {
        public SearchQuerySortOrder sortOrder { get; set; }

        public int Compare(ISearchQuery x, ISearchQuery y)
        {
            switch (sortOrder)
            {
                case SearchQuerySortOrder.AToZ:
                    return SortAlpha(x, y);
                case SearchQuerySortOrder.ZToA:
                    return SortAlphaDesc(x, y);
                case SearchQuerySortOrder.CreationTime:
                    return SortCreationTime(x, y);
                case SearchQuerySortOrder.MostRecentlyUsed:
                    return SortLastUsedTime(x, y);
                case SearchQuerySortOrder.ItemCount:
                    return SortItemCount(x, y);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static int SortAlpha(ISearchQuery a, ISearchQuery b)
        {
            return a.displayName.CompareTo(b.displayName);
        }

        static int SortAlphaDesc(ISearchQuery a, ISearchQuery b)
        {
            return -SortAlpha(a, b);
        }

        static int SortCreationTime(ISearchQuery a, ISearchQuery b)
        {
            // Most recent is on top.
            var compare =  -(a.creationTime.CompareTo(b.creationTime));
            if (compare != 0)
                return compare;

            return SortAlpha(a, b);
        }

        static int SortLastUsedTime(ISearchQuery a, ISearchQuery b)
        {
            // Queries with no last used time are sorted by name.
            if (a.lastUsedTime == 0 && b.lastUsedTime == 0)
                return SortAlpha(a, b);

            // Most recently used is on top.
            var compare = -(a.lastUsedTime.CompareTo(b.lastUsedTime));
            if (compare != 0)
                return compare;
            return SortAlpha(a, b);
        }

        static int SortItemCount(ISearchQuery a, ISearchQuery b)
        {
            // Queries with no item count are sorted by name.
            if (a.itemCount == -1 && b.itemCount == -1)
                return SortAlpha(a, b);

            // Query with the most items is on top.
            var compare = -(a.itemCount.CompareTo(b.itemCount));
            if (compare != 0)
                return compare;
            return SortAlpha(a, b);
        }
    }

    class SearchQueryPanelView : SearchElement, ISearchPanel
    {
        bool m_FilterSearchQuery;
        VisualElement m_FilterSearchQueryButton;
        string m_CurrentSearchFilter;
        ToolbarSearchField m_SearchField;
        VisualElement m_Header;
        SearchQueryListView[] m_ListViews;
        SearchQueryListView m_FilteringListView;
        ScrollView m_ScrollView;
        List<SearchQuerySource> m_SearchQuerySources;
        ISearchQuery m_LastSelectedQuery;
        SearchQueryComparer m_QueryComparer = new SearchQueryComparer();

        const string k_BaseUserLabel = "User";
        const string k_BaseProjectLabel = "Project";
        const int k_FilterListId = -1;
        static readonly int k_UserQueryId = GetListViewId(k_BaseUserLabel);
        static readonly int k_ProjectQueryId = GetListViewId(k_BaseProjectLabel);
        static readonly string k_SearchesLabel = L10n.Tr("Searches");
        static readonly string k_UserQueryLabel = L10n.Tr(k_BaseUserLabel);
        static readonly string k_ProjectQueryLabel = L10n.Tr(k_BaseProjectLabel);
        static readonly string k_UserTooltip = L10n.Tr("Your saved searches available for all Unity projects on this machine.");
        static readonly string k_ProjectTooltip = L10n.Tr("Shared searches available for all contributors on this project.");

        static readonly string k_FilterSearchButtonTooltip = L10n.Tr("Filter Searches");
        static readonly string k_SortButtonTooltip = L10n.Tr("Change Searches sorting order");

        public static readonly string ussClassName = "search-query-panel";
        public static readonly string headerClassName = ussClassName.WithUssElement("header");
        public static readonly string iconClassName = ussClassName.WithUssElement("icon");
        public static readonly string scrollViewClassName = ussClassName.WithUssElement("scroll-view");
        public static readonly string iconClickableClassName = iconClassName.WithUssModifier("clickable");
        public static readonly string headerButtonRowClassName = headerClassName.WithUssElement("button-row");
        public static readonly string searchQueryIconClassName = headerClassName.WithUssElement("icon");
        public static readonly string filterButtonClassName = headerClassName.WithUssElement("filter");
        public static readonly string sortButtonClassName = headerClassName.WithUssElement("sort");
        public static readonly string searchFieldClassName = headerClassName.WithUssElement("search-field");

        internal ISearchQuery selectedQuery
        {
            get => m_LastSelectedQuery;
            set => m_LastSelectedQuery = value;
        }

        public SearchQueryPanelView(string name, ISearchView viewModel, params string[] classes)
            : base(name, viewModel, classes)
        {
            AddToClassList(ussClassName);

            m_Header = MakeHeader();
            Add(m_Header);
            m_ScrollView = new ScrollView(ScrollViewMode.Vertical);
            m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_ScrollView.AddToClassList(scrollViewClassName);

            m_SearchQuerySources = new List<SearchQuerySource>
            {
                new SearchQuerySource(k_UserQueryId, k_UserQueryLabel, k_UserTooltip, () => SearchQuery.userQueries, new SearchUserQueryListViewItemHandler(), SaveUserQuery),
                new SearchQuerySource(k_ProjectQueryId, k_ProjectQueryLabel, k_ProjectTooltip, () => SearchQueryAsset.savedQueries, new SearchProjectQueryListViewItemHandler(), SaveProjectQuery)
            };

            m_ListViews = m_SearchQuerySources.Select(source => CreateSearchQueryListView(source)).ToArray();
            foreach (var searchQueryListView in m_ListViews)
                m_ScrollView.Add(searchQueryListView);

            // Filter has no header, so no need to localize it.
            m_FilteringListView = CreateSearchQueryListView(k_FilterListId, "Filter", null, new ISearchQuery[] { }, BindAnyItem, null,  false);

            Add(m_ScrollView);
            RegisterCallback<MouseDownEvent>(HandleMouseDownEvent);

            UpdateQueriesFilterState(m_FilterSearchQuery);
            SortQueries(SearchSettings.savedSearchesSortOrder);
        }

        void BindAnyItem(ISearchQuery query, SearchQueryListViewItem item)
        {
            int sourceId = -1;
            if (query is SearchQuery)
                sourceId = k_UserQueryId;
            else if (query is SearchQueryAsset)
                sourceId = k_ProjectQueryId;
            var source = GetSearchQuerySource(sourceId);

            item.BindItem(query, source.searchQueryItemHandler);
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            SetActiveQuery(m_ViewModel.state.activeQuery);
            SetExpandedState(SearchSettings.expandedQueries);

            OnAll(SearchEvent.UserQueryAdded, HandleUserQueriesChanged);
            OnAll(SearchEvent.UserQueryRemoved, HandleUserQueriesChanged);
            OnAll(SearchEvent.ProjectQueryAdded, HandleProjectQueriesChanged);
            OnAll(SearchEvent.ProjectQueryRemoved, HandleProjectQueriesChanged);
            OnAll(SearchEvent.SearchQueryChanged, HandleSearchQueryChanged);
            On(SearchEvent.ActiveQueryChanged, HandleActiveQueryChanged);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.UserQueryAdded, HandleUserQueriesChanged);
            Off(SearchEvent.UserQueryRemoved, HandleUserQueriesChanged);
            Off(SearchEvent.ProjectQueryAdded, HandleProjectQueriesChanged);
            Off(SearchEvent.ProjectQueryRemoved, HandleProjectQueriesChanged);
            Off(SearchEvent.SearchQueryChanged, HandleSearchQueryChanged);
            Off(SearchEvent.ActiveQueryChanged, HandleActiveQueryChanged);

            base.OnDetachFromPanel(evt);
        }

        void HandleSearchQueryChanged(ISearchEvent evt)
        {
            var changedQuery = evt.GetArgument<ISearchQuery>(0);
            if (IsListViewDisplayed(m_FilteringListView))
            {
                if (!IsQueryNameMatchingFilter(changedQuery.displayName))
                {
                    m_FilteringListView.RemoveItem(changedQuery);
                    UpdateListViewSelection(m_FilteringListView, m_LastSelectedQuery);
                }
                else
                {
                    UpdateListViewVisualState(m_FilteringListView, m_LastSelectedQuery);
                }
                return;
            }

            UpdateAllListViewsVisualState(m_LastSelectedQuery);
        }

        void HandleActiveQueryChanged(ISearchEvent evt)
        {
            var activeQuery = evt.GetArgument<ISearchQuery>(0);
            SetActiveQuery(activeQuery);
        }

        void SetActiveQuery(ISearchQuery activeQuery)
        {
            if (activeQuery == m_LastSelectedQuery)
                return;
            m_LastSelectedQuery = activeQuery;

            ClearListViewsSelection();

            if (IsListViewDisplayed(m_FilteringListView))
            {
                UpdateListViewSelection(m_FilteringListView, m_LastSelectedQuery);
                return;
            }

            var listView = activeQuery switch
            {
                SearchQuery => GetListView(k_UserQueryId),
                SearchQueryAsset => GetListView(k_ProjectQueryId),
                _ => null
            };
            if (listView == null)
                return;

            UpdateListViewSelection(listView, m_LastSelectedQuery);
        }

        void HandleProjectQueriesChanged(ISearchEvent evt)
        {
            var source = GetSearchQuerySource(k_ProjectQueryId);
            RebuildListViewItemsFromSource(source);

            HandleViewQueryAdded(evt, SearchEvent.ProjectQueryAdded, source, true);
        }

        void HandleUserQueriesChanged(ISearchEvent evt)
        {
            var source = GetSearchQuerySource(k_UserQueryId);
            RebuildListViewItemsFromSource(source);

            HandleViewQueryAdded(evt, SearchEvent.UserQueryAdded, source, true);
        }

        void HandleViewQueryAdded(ISearchEvent evt, string eventName, SearchQuerySource source, bool waitForGeometryChanged)
        {
            if (!IsEventFromSameView(evt) || evt.eventName != eventName)
                return;

            var listView = GetListView(source.id);
            if (!IsListViewDisplayed(m_FilteringListView))
            {
                if (!listView.GetExpandedState())
                {
                    listView.SetExpandedState(true, true);
                    waitForGeometryChanged = true;
                }
            }

            if (!evt.HasArgument(0))
                return;
            var query = evt.GetArgument<ISearchQuery>(0);
            if (waitForGeometryChanged)
            {
                var scrollViewContainer = m_ScrollView.Q(null, "unity-scroll-view__content-container");
                scrollViewContainer?.RegisterFireAndForgetCallback<GeometryChangedEvent>(evt => FocusSearchQuery(query));
            }
            else
                FocusSearchQuery(query);
        }

        SearchQuerySource GetSearchQuerySource(int sourceId)
        {
            return m_SearchQuerySources.FirstOrDefault(source => source.id == sourceId);
        }

        void RebuildListViewItemsFromSource(SearchQuerySource source)
        {
            // If we are filtering, update the filtering list, but also
            // update the real list view.
            if (IsListViewDisplayed(m_FilteringListView))
            {
                RebuildFilteringListViewFromAllSources();
            }

            var listView = GetListView(source.id);
            if (listView == null)
                return;
            listView.SetItemSource(source.getter(), true);
            UpdateListViewVisualState(listView, m_LastSelectedQuery);
        }

        void RebuildFilteringListViewFromAllSources()
        {
            if (string.IsNullOrEmpty(m_CurrentSearchFilter))
                return;
            var filteredItems = m_SearchQuerySources
                .SelectMany(source => source.getter?.Invoke() ?? new ISearchQuery[] { })
                .Where(query => IsQueryNameMatchingFilter(query.displayName));
            m_FilteringListView.SetItemSource(filteredItems, true);
            UpdateListViewVisualState(m_FilteringListView, m_LastSelectedQuery);
        }

        bool IsQueryNameMatchingFilter(string queryName)
        {
            return queryName.IndexOf(m_CurrentSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void SaveProjectQuery()
        {
            Emit(SearchEvent.SaveProjectQuery);
        }

        void SaveUserQuery()
        {
            Emit(SearchEvent.SaveUserQuery);
        }

        SearchQueryListView CreateSearchQueryListView(SearchQuerySource source)
        {
            return CreateSearchQueryListView(source.id, source.name, source.tooltip, source.getter.Invoke(), (query, item) => item.BindItem(query, source.searchQueryItemHandler), source.saveSearchQuery, true);
        }

        SearchQueryListView CreateSearchQueryListView(int id, string name, string tooltip, IEnumerable<ISearchQuery> itemSource, Action<ISearchQuery, SearchQueryListViewItem> bindItem, Action saveQuery, bool visibleHeader)
        {
            var listView = new SearchQueryListView(id, name, tooltip, itemSource, visibleHeader, GetListViewNameFromSourceName(name), m_ViewModel);
            listView.itemSelected = HandleSearchQueryListViewItemSelected;
            listView.saveSearchQuery = saveQuery;
            listView.bindItem = bindItem;
            listView.expandStateChanged = HandleExpandedStateChanged;
            return listView;
        }

        void HandleExpandedStateChanged(SearchQueryListView listView, bool expanded)
        {
            var expandedIds = m_ListViews.Where(lv => lv.GetExpandedState()).Select(lv => lv.id).ToArray();
            SearchSettings.expandedQueries = expandedIds;
        }

        void SetExpandedState(int[] expandedIds)
        {
            foreach (var listView in m_ListViews)
                listView.SetExpandedState(expandedIds.Contains(listView.id), false);
        }

        void HandleMouseDownEvent(MouseDownEvent evt)
        {
            if (m_Header == null || m_ScrollView == null)
                return;
            var elementTarget = evt.target as VisualElement;
            if (elementTarget != this)
                return;
            m_LastSelectedQuery = null;
            ClearListViewsSelection();
        }

        void HandleSearchQueryListViewItemSelected(SearchQueryListView listViewSource, ISearchQuery itemSelected)
        {
            if (itemSelected == m_LastSelectedQuery)
                return;

            m_LastSelectedQuery = itemSelected;
            ClearListViewsSelection(listViewSource);
            Emit(SearchEvent.ExecuteSearchQuery, itemSelected);
        }

        void ClearListViewsSelection(params SearchQueryListView[] exceptions)
        {
            foreach (var listView in m_ListViews.Append(m_FilteringListView).Except(exceptions))
            {
                listView.ClearSelection();
            }
        }

        VisualElement MakeHeader()
        {
            var headerElement = new VisualElement();
            headerElement.style.flexDirection = FlexDirection.Column;
            headerElement.AddToClassList(headerClassName);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.AddToClassList(headerButtonRowClassName);

            var iconElement = Create<VisualElement>(null, iconClassName, searchQueryIconClassName);
            var label = Create<Label>(null);
            label.text = k_SearchesLabel;

            m_FilterSearchQueryButton = Create<VisualElement>(null, iconClassName, iconClickableClassName, filterButtonClassName);
            m_FilterSearchQueryButton.tooltip = k_FilterSearchButtonTooltip;
            m_FilterSearchQueryButton.RegisterCallback<MouseUpEvent>(HandleFilterClicked);

            var sortElement = Create<VisualElement>(null, iconClassName, iconClickableClassName, sortButtonClassName);
            sortElement.tooltip = k_SortButtonTooltip;
            sortElement.RegisterCallback<MouseDownEvent>(HandleSortClicked);

            buttonRow.Add(iconElement);
            buttonRow.Add(label);
            buttonRow.Add(m_FilterSearchQueryButton);
            buttonRow.Add(sortElement);
            headerElement.Add(buttonRow);

            var textField = new ToolbarSearchField();
            textField.AddToClassList(searchFieldClassName);
            textField.RegisterValueChangedCallback(HandleSearchFieldValueChanged);
            m_SearchField = textField;
            headerElement.Add(textField);

            return headerElement;
        }

        void HandleFilterClicked(MouseUpEvent evt)
        {
            if (m_FilterSearchQueryButton == null)
                return;
            UpdateQueriesFilterState(!m_FilterSearchQuery);
        }

        void UpdateQueriesFilterState(bool enabled)
        {
            m_FilterSearchQuery = enabled;
            if (m_FilterSearchQueryButton != null)
                m_FilterSearchQueryButton.EnableInClassList(filterButtonClassName.WithUssModifier("opened"), enabled);
            if (m_SearchField != null && m_Header != null)
            {
                var textFieldExists = m_Header.Contains(m_SearchField);
                if (enabled && !textFieldExists)
                {
                    m_Header.Add(m_SearchField);
                    m_SearchField.Focus();
                }
                else if (!enabled && textFieldExists)
                {
                    m_SearchField.value = string.Empty;
                    // This seems to prevent the ChangeEvent from happening
                    // So trigger an update manually
                    m_SearchField.RemoveFromHierarchy();
                    UpdateSavedQueriesFiltering(string.Empty);
                }
            }
        }

        void HandleSortClicked(MouseDownEvent evt)
        {
            var dropDownMenu = DropdownUtility.CreateDropdown();

            var position = evt.localMousePosition;
            var sortButton = evt.target as VisualElement;
            var worldPosition = sortButton.LocalToWorld(position);

            var currentSortingOrder = SearchSettings.savedSearchesSortOrder;
            var enumData = EnumDataUtility.GetCachedEnumData(typeof(SearchQuerySortOrder));
            var options = EditorGUI.EnumNamesCache.GetEnumTypeLocalizedGUIContents(typeof(SearchQuerySortOrder), enumData);
            for (var i = 0; i < options.Length; ++i)
            {
                var sortOrder = (SearchQuerySortOrder)i;
                dropDownMenu.AddItem(options[i].text, sortOrder == currentSortingOrder, () => SortQueries(sortOrder));
            }
            dropDownMenu.DropDown(new Rect(worldPosition, Vector2.one), sortButton);
        }

        internal void SortQueries(SearchQuerySortOrder sortOrder)
        {
            if (sortOrder != SearchSettings.savedSearchesSortOrder)
            {
                SearchSettings.savedSearchesSortOrder = sortOrder;
                SearchSettings.Save();
            }

            m_QueryComparer.sortOrder = sortOrder;
            if (IsListViewDisplayed(m_FilteringListView))
                UpdateListViewVisualState(m_FilteringListView, m_LastSelectedQuery);
            else
                UpdateAllListViewsVisualState(m_LastSelectedQuery);
        }

        void HandleSearchFieldValueChanged(ChangeEvent<string> evt)
        {
            UpdateSavedQueriesFiltering(evt.newValue);
        }

        void UpdateSavedQueriesFiltering(string searchQuery)
        {
            m_CurrentSearchFilter = searchQuery;
            if (string.IsNullOrEmpty(searchQuery))
            {
                if (!IsListViewDisplayed(m_FilteringListView))
                    return;
                m_FilteringListView.RemoveFromHierarchy();
                m_FilteringListView.ClearSelection();
                foreach (var searchQueryListView in m_ListViews)
                {
                    m_ScrollView.Add(searchQueryListView);
                    UpdateListViewVisualState(searchQueryListView, m_LastSelectedQuery);
                }
            }
            else
            {
                if (!IsListViewDisplayed(m_FilteringListView))
                {
                    foreach (var searchQueryListView in m_ListViews)
                    {
                        searchQueryListView.RemoveFromHierarchy();
                    }
                    m_ScrollView.Add(m_FilteringListView);
                }

                RebuildFilteringListViewFromAllSources();
            }
        }

        bool IsListViewDisplayed(SearchQueryListView listView)
        {
            return m_ScrollView != null && m_ScrollView.Contains(listView);
        }

        void UpdateAllListViewsVisualState(ISearchQuery selectedItem)
        {
            foreach (var searchQueryListView in m_ListViews)
                UpdateListViewVisualState(searchQueryListView, m_LastSelectedQuery);
        }

        void UpdateListViewVisualState(SearchQueryListView listView, ISearchQuery selectedItem)
        {
            listView.SortBy(m_QueryComparer);
            UpdateListViewSelection(listView, selectedItem);
        }

        static void UpdateListViewSelection(SearchQueryListView listView, ISearchQuery selectedItem)
        {
            listView.ClearSelection();
            listView.SetSelectedItem(selectedItem);
        }

        static string GetListViewNameFromSourceName(string name)
        {
            return $"SearchQueryListView-{name}";
        }

        SearchQueryListView GetListView(int sourceId)
        {
            return m_ListViews.FirstOrDefault(lv => lv.id == sourceId);
        }

        internal static int GetListViewId(string nonLocalizedName)
        {
            return $"SearchQueryCategoryTreeViewItem_{nonLocalizedName}".GetHashCode();
        }

        void FocusSearchQuery(ISearchQuery query)
        {
            var listView = m_ListViews.Append(m_FilteringListView).FirstOrDefault(lv => IsListViewDisplayed(lv) && lv.IsItemInList(query));
            var element = listView?.GetSearchElementForSearchQuery(query);
            if (element == null)
                return;

            if (element.geometryRealized)
                m_ScrollView.ScrollTo(element);
            else
                element.RegisterFireAndForgetCallback<GeometryChangedEvent>(evt => m_ScrollView.ScrollTo(element));
        }
    }
}
