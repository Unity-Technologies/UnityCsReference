// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchEmptyView : SearchElement, IResultView
    {
        public static readonly string ussClassName = "search-empty-view";
        public static readonly string headerClassName = ussClassName.WithUssElement("header");
        public static readonly string noResultsRowContainerClassName = ussClassName.WithUssElement("no-results-row-container");
        public static readonly string noResultsClassName = ussClassName.WithUssElement("no-results");
        public static readonly string noResultsHelpClassName = ussClassName.WithUssElement("no-results-help");
        public static readonly string queryRowClassName = ussClassName.WithUssElement("query-row");
        public static readonly string builderClassName = ussClassName.WithUssElement("builder");
        public static readonly string descriptionClassName = ussClassName.WithUssElement("description");
        public static readonly string helperBackgroundClassName = ussClassName.WithUssElement("helper-background");

        public static readonly Texture2D recentSearchesIcon = EditorGUIUtility.LoadIconRequired("UndoHistory");

        private bool m_Disposed;
        private QueryBuilder m_Areas;
        private VisualElement m_QueriesContainer;
        private SearchEmptyViewMode m_DisplayMode;

        float IResultView.itemSize => m_ViewModel.itemIconSize;
        Rect IResultView.rect => worldBound;
        bool IResultView.showNoResultMessage => true;

        enum SearchEmptyViewMode
        {
            None,
            HideHelpersNoResult,
            SearchInProgress,
            NoResult,
            NoResultWithTips,
            QueryHelpersText,
            QueryHelpersBlock
        }

        public SearchViewFlags searchViewFlags { get; set; }

        int IResultView.ComputeVisibleItemCapacity(float width, float height)
        {
            return 0;
        }

        static readonly string k_NoResultsLabel = L10n.Tr("No Results.");
        static readonly string k_SearchInProgressLabel = L10n.Tr("Search in progress...");
        static readonly string k_NarrowYourSearchLabel = L10n.Tr("Narrow your search");
        static readonly string k_SearchesLabel = L10n.Tr("Searches");
        static readonly string k_AreaTooltipFormat = L10n.Tr("Double click to search in: {0}");
        static readonly string k_NoResultsFoundLabel = L10n.Tr("No results found.");
        static readonly string k_NoResultsFoundQueryFormat = L10n.Tr("No results found for <b>{0}</b>.");
        static readonly string k_IndexingInProgressLabel = L10n.Tr("Indexing is still in progress.");
        static readonly string k_TrySomethingElseLabel = L10n.Tr("Try something else?");
        static readonly string k_NoResultsInProviderFormat = L10n.Tr("There is no result in {0}.");
        static readonly string k_SelectAnotherTabLabel = L10n.Tr("Select another search tab?");
        static readonly string k_IndexesPropertiesLabel = L10n.Tr("Some indexes don't have properties enabled.");
        static readonly string k_OpenIndexManagerLabel = L10n.Tr("Open Index Manager?");
        static readonly string k_IndexesDisabledLabel = L10n.Tr("All indexes are disabled.");
        static readonly string k_ShowMoreResultsLabel = L10n.Tr("Show more results is off.");
        static readonly string k_TurnItOnLabel = L10n.Tr("Turn it on?");
        static readonly string k_ShowPackagesLabel = L10n.Tr("Show Packages is off.");
        static readonly string k_AddFilterOrKeywordLabel = L10n.Tr("The search query is empty. Try adding some filters or keywords.");
        static readonly string k_TryAQueryLabel = L10n.Tr("The search query is empty. Try one of these queries:");

        Label m_NoResultLabel;
        VisualElement m_QueryPropositionContainer;
        Label m_NoResultsHelpLabel;
        VisualElement m_IndexHelpLabel;
        VisualElement m_WantsMoreLabel;
        VisualElement m_ShowPackagesLabel;

        public SearchEmptyView(ISearchView viewModel, SearchViewFlags flags)
            : base("SearchEmptyView", viewModel, ussClassName)
        {
            searchViewFlags = flags;

            BuildView();
        }

        public void Update()
        {
            if (GetDisplayMode() == SearchEmptyViewMode.NoResultWithTips && !context.empty)
            {
                BuildView(true);
            }
        }

        public void UpdateView()
        {
            BuildView();
        }

        private SearchEmptyViewMode GetDisplayMode()
        {
            if (searchViewFlags.HasFlag(SearchViewFlags.DisableQueryHelpers))
                return SearchEmptyViewMode.HideHelpersNoResult;

            if (m_ViewModel.searchInProgress)
                return SearchEmptyViewMode.SearchInProgress;

            if (!QueryEmpty())
            {
                return searchViewFlags.HasFlag(SearchViewFlags.DisableNoResultTips) ?
                    SearchEmptyViewMode.NoResult :
                    SearchEmptyViewMode.NoResultWithTips;
            }

            if (viewState.queryBuilderEnabled)
                return SearchEmptyViewMode.QueryHelpersBlock;

            return SearchEmptyViewMode.QueryHelpersText;
        }

        private bool QueryEmpty()
        {
            return string.IsNullOrEmpty(context.searchText);
        }

        private void BuildView(bool forceBuild = false)
        {
            var displayMode = GetDisplayMode();
            if (!forceBuild && displayMode == m_DisplayMode)
            {
                UpdateViewMinimal();
                return;
            }
            m_DisplayMode = displayMode;

            ClearAllNoResultsTipsHelpers();
            Clear();

            switch(m_DisplayMode)
            {
                case SearchEmptyViewMode.HideHelpersNoResult:
                    AddOrUpdateNoResultLabel(ref m_NoResultLabel, k_NoResultsLabel);
                    break;
                case SearchEmptyViewMode.NoResult:
                    AddOrUpdateNoResultLabel(ref m_NoResultLabel, k_NoResultsLabel);
                    break;
                case SearchEmptyViewMode.NoResultWithTips:
                    BuildNoResultsTips();
                    break;
                case SearchEmptyViewMode.SearchInProgress:
                    AddOrUpdateNoResultLabel(ref m_NoResultLabel, k_SearchInProgressLabel);
                    break;
                default:
                    BuildQueryHelpers();
                    break;
            }

            EnableInClassList(helperBackgroundClassName, m_DisplayMode == SearchEmptyViewMode.HideHelpersNoResult || QueryEmpty());
        }

        void UpdateViewMinimal()
        {
            if (m_DisplayMode != SearchEmptyViewMode.NoResultWithTips)
                return;
            BuildNoResultsTips();
        }

        private void BuildQueryHelpers()
        {
            m_QueriesContainer = new VisualElement();
            m_QueriesContainer.name = "QueryHelpersContainer";
            if (GetActiveHelperProviders(viewState.queryBuilderEnabled).Count() > 0)
            {
                Add(CreateHeader(k_NarrowYourSearchLabel));
                Add(CreateProviderHelpers(viewState.queryBuilderEnabled));
            }
            else
            {
                BuildSearches();
            }

            Add(m_QueriesContainer);
        }

        private void BuildSearches()
        {
            m_QueriesContainer.Clear();

            var searches = new QueryHelperSearchGroup(viewState.queryBuilderEnabled, k_SearchesLabel);
            PopulateSearches(searches);
            var searchesHeader = CreateHeader(k_SearchesLabel);

            m_QueriesContainer.Add(searchesHeader);
            m_QueriesContainer.Add(CreateSearchHelpers(viewState.queryBuilderEnabled, searches));

            searchesHeader.text = searches.title.text;
        }

        private void PopulateSearches(QueryHelperSearchGroup searches)
        {
            foreach (var q in SearchTemplateAttribute.GetAllQueries())
                searches.Add(q, QueryHelperSearchGroup.QueryType.Template, SearchQuery.GetIcon(q));

            foreach (var q in SearchQueryAsset.savedQueries.Cast<ISearchQuery>().Concat(SearchQuery.userQueries).Where(q => q.isSearchTemplate))
                searches.Add(q, QueryHelperSearchGroup.QueryType.Template, SearchQuery.GetIcon(q));

            foreach (var a in EnumerateUniqueRecentSearches().Take(5))
                searches.Add(a, QueryHelperSearchGroup.QueryType.Recent, recentSearchesIcon);
        }

        private VisualElement CreateSearchHelpers(bool blockMode, QueryHelperSearchGroup searches)
        {
            var container = new ScrollView();
            container.style.flexDirection = FlexDirection.Column;

            var currentAreaFilterId = SearchSettings.helperWidgetCurrentArea;
            var filteredQueries = GetFilteredQueries(searches.queries, currentAreaFilterId, blockMode); ;
            PopulateSearchHelpers(filteredQueries, container);
            searches.UpdateTitle(filteredQueries.Count());
            container.Children().LastOrDefault()?.AddToClassList("last-child");
            return container;
        }

        private void PopulateSearchHelpers(IEnumerable<QueryHelperSearchGroup.QueryData> filteredQueries, VisualElement container)
        {
            foreach (var qd in filteredQueries)
            {
                var rowContainer = new VisualElement();
                rowContainer.AddToClassList(queryRowClassName);
                rowContainer.Add(new Image() { image = qd.icon.image, tooltip = qd.icon.tooltip, pickingMode = PickingMode.Ignore });

                if (qd.builder != null)
                {
                    var builderContainer = new VisualElement();
                    builderContainer.pickingMode = PickingMode.Ignore;
                    builderContainer.AddToClassList(builderClassName);
                    foreach (var b in qd.builder.EnumerateBlocks())
                    {
                        var be = b.CreateGUI();
                        be.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);
                        be.pickingMode = PickingMode.Ignore;
                        builderContainer.Add(be);
                    }
                    rowContainer.Add(builderContainer);
                }
                else
                {
                    rowContainer.Add(CreateLabel(qd.searchText, PickingMode.Ignore));
                }

                rowContainer.Add(CreateLabel(qd.description, PickingMode.Ignore, descriptionClassName));
                rowContainer.userData = qd;

                rowContainer.RegisterCallback<ClickEvent>(OnQueryHelperClicked);

                container.Add(rowContainer);
            }
        }

        private void OnQueryHelperClicked(ClickEvent evt)
        {
            if (evt.target is not VisualElement ve || ve.userData is not QueryHelperSearchGroup.QueryData qd)
                return;

            if (qd.query != null && this.GetSearchHostWindow() is ISearchQueryView sqv)
                ExecuteQuery(qd.query);
            else
                m_ViewModel.SetSearchText(qd.searchText);
        }

        private bool IsFilteredQuery(ISearchQuery query, SearchProvider provider)
        {
            if (provider == null)
                return false;

            if (query.searchText.StartsWith(provider.filterId))
                return true;

            var queryProviders = query.GetProviderIds().ToArray();
            return queryProviders.Length == 1 && queryProviders[0] == provider.id;
        }

        private IEnumerable<QueryHelperSearchGroup.QueryData> GetFilteredQueries(IEnumerable<QueryHelperSearchGroup.QueryData> queries, string currentAreaFilterId, bool blockMode)
        {
            var activeProviders = GetActiveHelperProviders(blockMode);
            var isAll = GroupedSearchList.allGroupId == currentAreaFilterId;
            if (isAll)
            {
                // Keep only query matching one of the active providers.
                return queries.Where(q => activeProviders.Any(p => IsFilteredQuery(q.query, p)));
            }
            var currentProvider = activeProviders.FirstOrDefault(p => p.filterId == currentAreaFilterId);
            return queries.Where(q =>
            {
                // Keep query matching THE selected provider.
                if (q.type == QueryHelperSearchGroup.QueryType.Recent)
                    return q.searchText.StartsWith(currentAreaFilterId, StringComparison.Ordinal);
                return IsFilteredQuery(q.query, currentProvider);
            });
        }

        private IEnumerable<string> EnumerateUniqueRecentSearches()
        {
            var recentSearches = SearchSettings.recentSearches.ToList();
            for (var i = 0; i < recentSearches.Count(); ++i)
            {
                var a = recentSearches[i];
                yield return a;

                for (var j = i + 1; j < recentSearches.Count();)
                {
                    var b = recentSearches[j];
                    if (a.StartsWith(b) || Utils.LevenshteinDistance(a, b, false) < 9)
                    {
                        recentSearches.RemoveAt(j);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        private VisualElement CreateProviderHelpers(bool blockMode = true)
        {
            var providersContainer = new VisualElement();
            providersContainer.style.flexDirection = FlexDirection.Row;
            providersContainer.style.flexWrap = Wrap.Wrap;
            providersContainer.style.flexShrink = 0f;

            m_Areas = new QueryBuilder(string.Empty);
            var allArea = new QueryAreaBlock(m_Areas, GroupedSearchList.allGroupId, string.Empty);
            allArea.RegisterCallback<MouseDownEvent> (OnProviderClicked);
            m_Areas.AddBlock(allArea);

            var providers = SearchUtils.SortProvider(GetActiveHelperProviders(blockMode));
            foreach (var p in providers)
            {
                var providerBlock = new QueryAreaBlock(m_Areas, p);
                providerBlock.RegisterCallback<MouseDownEvent>(OnProviderClicked);
                m_Areas.AddBlock(providerBlock);
            }
            m_Areas.@readonly = true;
            foreach (var b in m_Areas.blocks)
                b.tooltip = string.Format(k_AreaTooltipFormat, b.value);

            foreach (var b in m_Areas.EnumerateBlocks())
            {
                if (b is QueryAreaBlock area && GetFilterId(area) == SearchSettings.helperWidgetCurrentArea)
                {
                    b.selected = true;
                }
                providersContainer.Add(b.CreateGUI());
            }

            if (!m_Areas.selectedBlocks.Any())
            {
                allArea.selected = true;
                SetCurrentArea(allArea);
            }
            else
            {
                BuildSearches();
            }

            return providersContainer;
        }

        private void OnProviderClicked(MouseDownEvent evt)
        {
            if (evt.currentTarget is not QueryAreaBlock area)
                return;

            if (evt.clickCount == 2 && !string.IsNullOrEmpty(area.filterId))
            {
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchHelperWidgetExecuted, viewState.queryBuilderEnabled ? "queryBuilder" : "text", "doubleClick");
                var query = CreateQuery(area.ToString());
                ExecuteQuery(query);
            }
            else if (SearchSettings.helperWidgetCurrentArea != GetFilterId(area))
            {
                SetCurrentArea(area);
            }

            evt.StopPropagation();
        }

        private void SetCurrentArea(QueryAreaBlock area)
        {
            var filterId = GetFilterId(area);
            SearchSettings.helperWidgetCurrentArea = filterId;
            BuildSearches();
        }

        private static string GetFilterId(QueryAreaBlock area)
        {
            return string.IsNullOrEmpty(area.filterId) ? area.value : area.filterId;
        }

        private static ISearchQuery CreateQuery(string queryStr)
        {
            var q = new SearchQuery() { searchText = queryStr };
            q.isTextOnlyQuery = true;
            q.viewState.itemSize = SearchSettings.itemIconSize;
            return q;
        }

        private void ExecuteQuery(ISearchQuery query)
        {
            if(this.GetSearchHostWindow() is ISearchQueryView sqv)
                sqv.ExecuteSearchQuery(query);
        }

        private IEnumerable<SearchProvider> GetActiveHelperProviders(bool blockMode)
        {
            var allProviders = m_ViewModel?.context?.GetProviders() ?? SearchService.Providers;
            if (!blockMode)
                return allProviders;
            var filtered = allProviders.Where(p => p.id != "expression");
            return filtered;
        }

        private static Label CreateHeader(in string text)
        {
            var header = CreateLabel(text, null, headerClassName);
            return header;
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            On(SearchEvent.FilterToggled, ForceRebuildView);
            On(SearchEvent.RefreshBuilder, BuildView);
            On(SearchEvent.SearchIndexesChanged, BuildView);
            On(SearchEvent.SearchTextChanged, BuildView);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.FilterToggled, ForceRebuildView);
            Off(SearchEvent.RefreshBuilder, BuildView);
            Off(SearchEvent.SearchIndexesChanged, BuildView);
            Off(SearchEvent.SearchTextChanged, BuildView);

            base.OnDetachFromPanel(evt);
        }

        private void BuildView(ISearchEvent evt)
        {
            BuildView();
        }

        void ForceRebuildView(ISearchEvent evt)
        {
            BuildView(true);
        }

        private void BuildNoResultsTips()
        {
            var provider = SearchService.GetProvider(m_ViewModel.currentGroup);
            var anyNonReady = false;
            var anyWithNoPropertyIndexing = false;
            var allDisabled = true;
            if (!context.noIndexing)
            {
                foreach (var database in SearchDatabase.EnumerateAll())
                {
                    if (database.settings.options.disabled)
                        continue;
                    allDisabled = false;
                    if (!database.ready)
                        anyNonReady = true;
                    if (!database.settings.options.properties)
                        anyWithNoPropertyIndexing = true;
                }
            }
            else
                allDisabled = false;

            var structureChanged = false;
            if (m_ViewModel.totalCount == 0 || provider == null)
            {
                if (string.IsNullOrEmpty(context.searchQuery?.Trim()))
                {
                    structureChanged |= ClearNoResultsTipsHelper(ref m_NoResultsHelpLabel);
                    structureChanged |= AddOrUpdateNoResultLabel(ref m_NoResultLabel, k_NoResultsFoundLabel);
                    structureChanged |= AddOrUpdateNoResultQueryPropositions(ref m_QueryPropositionContainer, 2);
                }
                else
                {
                    structureChanged |= ClearNoResultsTipsHelper(ref m_QueryPropositionContainer);
                    structureChanged |= AddOrUpdateNoResultLabel(ref m_NoResultLabel, string.Format(k_NoResultsFoundQueryFormat, context.searchQuery));

                    if (anyNonReady)
                        structureChanged |= AddOrUpdateNoResultHelpLabel(ref m_NoResultsHelpLabel, k_IndexingInProgressLabel);
                    else
                        structureChanged |= AddOrUpdateNoResultHelpLabel(ref m_NoResultsHelpLabel, k_TrySomethingElseLabel);
                }
            }
            else
            {
                structureChanged |= ClearNoResultsTipsHelper(ref m_QueryPropositionContainer);
                structureChanged |= AddOrUpdateNoResultLabel(ref m_NoResultLabel, string.Format(k_NoResultsInProviderFormat, provider.name));
                if (anyNonReady)
                    structureChanged |= AddOrUpdateNoResultHelpLabel(ref m_NoResultsHelpLabel, k_IndexingInProgressLabel);
                else
                    structureChanged |= AddOrUpdateNoResultHelpLabel(ref m_NoResultsHelpLabel, k_SelectAnotherTabLabel);
            }

            if (!anyNonReady)
            {
                if (anyWithNoPropertyIndexing)
                    structureChanged |= AddOrUpdateNoResultHelpLabelWithButton(ref m_IndexHelpLabel, k_IndexesPropertiesLabel, k_OpenIndexManagerLabel, () => OpenIndexManager());
                else if (allDisabled)
                    structureChanged |= AddOrUpdateNoResultHelpLabelWithButton(ref m_IndexHelpLabel, k_IndexesDisabledLabel, k_OpenIndexManagerLabel, () => OpenIndexManager());
                else
                    structureChanged |= ClearNoResultsTipsHelper(ref m_IndexHelpLabel);
            }

            if (!context.wantsMore)
                structureChanged |= AddOrUpdateNoResultHelpLabelWithButton(ref m_WantsMoreLabel, k_ShowMoreResultsLabel, k_TurnItOnLabel, () => ToggleWantsMore());
            else
                structureChanged |= ClearNoResultsTipsHelper(ref m_WantsMoreLabel);

            if (!context.showPackages)
                structureChanged |= AddOrUpdateNoResultHelpLabelWithButton(ref m_ShowPackagesLabel, k_ShowPackagesLabel, k_TurnItOnLabel, () => TogglePackages());
            else
                structureChanged |= ClearNoResultsTipsHelper(ref m_ShowPackagesLabel);

            if (structureChanged)
            {
                SetSortOrder(m_NoResultLabel, 0);
                SetSortOrder(m_QueryPropositionContainer, 1);
                SetSortOrder(m_NoResultsHelpLabel, 2);
                SetSortOrder(m_IndexHelpLabel, 3);
                SetSortOrder(m_WantsMoreLabel, 4);
                SetSortOrder(m_ShowPackagesLabel, 5);
                Sort((elementA, elementB) =>
                {
                    if (elementA == null && elementB == null)
                        return 0;
                    if (elementA == null)
                        return -1;
                    if (elementB == null)
                        return 1;
                    var sortOrderA = elementA.userData as int? ?? 0;
                    var sortOrderB = elementB.userData as int? ?? 0;
                    return sortOrderA.CompareTo(sortOrderB);
                });
            }
        }

        void SetSortOrder(VisualElement element, int sortOrder)
        {
            if (element == null)
                return;
            element.userData = sortOrder;
        }

        // This needs to be generic as converting from ref Label to ref VisualElement does not work.
        static bool ClearNoResultsTipsHelper<T>(ref T helper)
            where T : VisualElement
        {
            if (helper == null)
                return false;
            helper.RemoveFromHierarchy();
            helper = null;
            return true;
        }

        void ClearAllNoResultsTipsHelpers()
        {
            ClearNoResultsTipsHelper(ref m_NoResultLabel);
            ClearNoResultsTipsHelper(ref m_NoResultsHelpLabel);
            ClearNoResultsTipsHelper(ref m_IndexHelpLabel);
            ClearNoResultsTipsHelper(ref m_WantsMoreLabel);
            ClearNoResultsTipsHelper(ref m_ShowPackagesLabel);
            ClearNoResultsTipsHelper(ref m_QueryPropositionContainer);
        }

        bool AddOrUpdateNoResultQueryPropositions(ref VisualElement container, int maxQueryCount)
        {
            container?.RemoveFromHierarchy();

            var emptyFilterId = string.IsNullOrEmpty(context.filterId);
            if (emptyFilterId)
            {
                Label label = null;
                AddOrUpdateNoResultHelpLabel(ref label, k_AddFilterOrKeywordLabel);
                container = label;
                return true;
            }

            var searches = new QueryHelperSearchGroup(viewState.queryBuilderEnabled, k_SearchesLabel);
            PopulateSearches(searches);
            var filteredQueries = GetFilteredQueries(searches.queries, context.filterId, viewState.queryBuilderEnabled)
                .Take(maxQueryCount).ToArray();
            if (filteredQueries.Length == 0)
            {
                Label label = null;
                AddOrUpdateNoResultHelpLabel(ref label, k_AddFilterOrKeywordLabel);
                container = label;
                return true;
            }

            container = new VisualElement();
            Label tryAQueryLabel = null;
            AddOrUpdateNoResultHelpLabel(ref tryAQueryLabel, k_TryAQueryLabel, container);
            var innerContainer = Create("SearchEmptyViewQueryPropositions", noResultsRowContainerClassName);
            PopulateSearchHelpers(filteredQueries, innerContainer);
            container.Add(innerContainer);

            Add(container);
            return true;
        }

        private bool AddOrUpdateNoResultLabel(ref Label label, in string text)
        {
            if (label != null)
            {
                label.text = text;
                return false;
            }

            label = CreateLabel(text, null, PickingMode.Ignore, noResultsClassName);
            Add(label);
            return true;
        }

        private bool AddOrUpdateNoResultHelpLabel(ref Label label, in string text, VisualElement container = null)
        {
            if (label != null)
            {
                label.text = text;
                return false;
            }

            label = CreateLabel(text, null, PickingMode.Ignore, noResultsHelpClassName);

            if (container != null)
                container.Add(label);
            else
                Add(label);
            return true;
        }

        private bool AddOrUpdateNoResultHelpLabelWithButton(ref VisualElement container, in string text, in string buttonText, Action onClickedCallback)
        {
            if (container != null)
            {
                var innerLabel = container.Q<Label>();
                innerLabel.text = text;
                return false;
            }

            container = Create("SearchEmptyViewAction", noResultsRowContainerClassName);
            container.style.flexDirection = FlexDirection.Row;
            var label = CreateLabel(text, null, PickingMode.Ignore, noResultsHelpClassName);
            container.Add(label);
            var button = new Button(onClickedCallback)
            {
                text = buttonText
            };
            container.Add(button);
            Add(container);
            return true;
        }

        void IResultView.Refresh(RefreshFlags flags)
        {
            if (flags.HasAny(RefreshFlags.QueryCompleted | RefreshFlags.ItemsChanged | RefreshFlags.StructureChanged))
                BuildView();
        }

        void IResultView.OnGroupChanged(string prevGroupId, string newGroupId)
        {
            BuildView();
        }

        void IResultView.OnItemSourceChanged(ISearchList itemSource)
        {
            // Nothing to do
        }

        void IResultView.AddSaveQueryMenuItems(SearchContext context, GenericMenu menu)
        {
            // Nothing to do
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
                m_Disposed = true;
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static void OpenIndexManager()
        {
            IndexManager.OpenWindow();
        }

        private void ToggleWantsMore()
        {
            Emit(SearchEvent.ToggleWantsMore);
        }

        private void TogglePackages()
        {
            Emit(SearchEvent.TogglePackages);
        }
    }
}
