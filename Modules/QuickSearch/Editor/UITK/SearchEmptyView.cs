// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchEmptyView : SearchElement, IResultView
    {
        const string k_All = "all";
        public static readonly string ussClassName = "search-empty-view";
        public static readonly string headerClassName = ussClassName.WithUssElement("header");
        public static readonly string noResultsRowContainerClassName = ussClassName.WithUssElement("no-results-row-container");
        public static readonly string noResultsClassName = ussClassName.WithUssElement("no-results");
        public static readonly string noResultsHelpClassName = ussClassName.WithUssElement("no-results-help");
        public static readonly string queryRowClassName = ussClassName.WithUssElement("query-row");
        public static readonly string builderClassName = ussClassName.WithUssElement("builder");
        public static readonly string descriptionClassName = ussClassName.WithUssElement("description");
        public static readonly string helperBackgroundClassName = ussClassName.WithUssElement("helper-background");

        public static readonly Texture2D recentSearchesIcon = EditorGUIUtility.FindTexture("UndoHistory");

        private bool m_Disposed;
        private QueryBuilder m_Areas;
        private VisualElement m_QueriesContainer;

        float IResultView.itemSize => m_ViewModel.itemIconSize;
        Rect IResultView.rect => worldBound;
        bool IResultView.showNoResultMessage => true;

        public bool hideHelpers { get; set; }

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

        public SearchEmptyView(ISearchView viewModel, bool hideHelpers = false)
            : base("SearchEmptyView", viewModel, ussClassName)
        {
            this.hideHelpers = hideHelpers;

            BuildView();
        }

        private void BuildView()
        {
            Clear();

            if (hideHelpers)
                AddNoResultLabel(k_NoResultsLabel);
            else if (m_ViewModel.searchInProgress)
                AddNoResultLabel(k_SearchInProgressLabel);
            else if (!context.empty)
                BuildNoResultsTips();
            else
                BuildQueryHelpers();

            EnableInClassList(helperBackgroundClassName, hideHelpers || context.empty);
        }

        private void BuildQueryHelpers()
        {
            Add(CreateHeader(k_NarrowYourSearchLabel));
            Add(CreateProviderHelpers(viewState.queryBuilderEnabled));

            m_QueriesContainer = new VisualElement();
            m_QueriesContainer.name = "QueryHelpersContainer";
            BuildSearches();

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

            searches.UpdateTitle();
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

            if (qd.query != null && this.GetHostWindow() is ISearchQueryView sqv)
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
            var isAll = k_All == currentAreaFilterId;
            if (isAll)
                return queries;
            var currentProvider = GetActiveHelperProviders(blockMode).FirstOrDefault(p => p.filterId == currentAreaFilterId);
            return queries.Where(q =>
            {
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
            var allArea = new QueryAreaBlock(m_Areas, k_All, string.Empty);
            allArea.RegisterCallback<ClickEvent>(OnProviderClicked);
            m_Areas.AddBlock(allArea);

            foreach (var p in GetActiveHelperProviders(blockMode))
            {
                var providerBlock = new QueryAreaBlock(m_Areas, p);
                providerBlock.RegisterCallback<ClickEvent>(OnProviderClicked);
                m_Areas.AddBlock(providerBlock);
            }
            m_Areas.@readonly = true;
            foreach (var b in m_Areas.blocks)
                b.tooltip = string.Format(k_AreaTooltipFormat, b.value);

            foreach (var b in m_Areas.EnumerateBlocks())
                providersContainer.Add(b.CreateGUI());

            return providersContainer;
        }

        private void OnProviderClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not QueryAreaBlock area)
                return;

            if (evt.clickCount == 2 && !string.IsNullOrEmpty(area.filterId))
            {
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchHelperWidgetExecuted, viewState.queryBuilderEnabled ? "queryBuilder" : "text", "doubleClick");
                var query = CreateQuery(area.ToString());
                ExecuteQuery(query);
            }
            else
            {
                var filterId = string.IsNullOrEmpty(area.filterId) ? area.value : area.filterId;
                SearchSettings.helperWidgetCurrentArea = filterId;
                BuildSearches();
            }

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private static ISearchQuery CreateQuery(string queryStr)
        {
            var q = new SearchQuery() { searchText = queryStr };
            q.viewState.itemSize = SearchSettings.itemIconSize;
            return q;
        }

        private void ExecuteQuery(ISearchQuery query)
        {
            if(this.GetHostWindow() is ISearchQueryView sqv)
                sqv.ExecuteSearchQuery(query);
        }

        private IEnumerable<SearchProvider> GetActiveHelperProviders(bool blockMode)
        {
            var allProviders = m_ViewModel?.context?.GetProviders() ?? SearchService.GetActiveProviders();
            var generalProviders = allProviders.Where(p => !p.isExplicitProvider);
            var explicitProviders = allProviders.Where(p => p.isExplicitProvider);
            var providers = generalProviders.Concat(explicitProviders);

            if (!blockMode)
                return providers;

            var builtinSearches = SearchTemplateAttribute.GetAllQueries();
            return providers.Where(p => p.id != "expression" && (p.fetchPropositions != null ||
                    builtinSearches.Any(sq => sq.searchText.StartsWith(p.filterId) || sq.GetProviderIds().Any(pid => p.id == pid))));
        }

        private static Label CreateHeader(in string text)
        {
            var header = CreateLabel(text, null, headerClassName);
            return header;
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            On(SearchEvent.FilterToggled, BuildView);
            On(SearchEvent.RefreshBuilder, BuildView);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.FilterToggled, BuildView);
            Off(SearchEvent.RefreshBuilder, BuildView);

            base.OnDetachFromPanel(evt);
        }

        private void BuildView(ISearchEvent evt)
        {
            BuildView();
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

            if (m_ViewModel.totalCount == 0 || provider == null)
            {
                if (string.IsNullOrEmpty(context.searchQuery?.Trim()))
                {
                    AddNoResultLabel(k_NoResultsFoundLabel);
                    AddNoResultQueryPropositions(2);
                }
                else
                {
                    AddNoResultLabel(string.Format(k_NoResultsFoundQueryFormat, context.searchQuery));

                    if (anyNonReady)
                        AddNoResultHelpLabel(k_IndexingInProgressLabel);
                    else
                        AddNoResultHelpLabel(k_TrySomethingElseLabel);
                }
            }
            else
            {
                AddNoResultLabel(string.Format(k_NoResultsInProviderFormat, provider.name));
                if (anyNonReady)
                    AddNoResultHelpLabel(k_IndexingInProgressLabel);
                else
                    AddNoResultHelpLabel(k_SelectAnotherTabLabel);
            }

            if (!anyNonReady)
            {
                if (anyWithNoPropertyIndexing)
                    AddNoResultHelpLabelWithButton(k_IndexesPropertiesLabel, k_OpenIndexManagerLabel, () => OpenIndexManager());
                else if (allDisabled)
                    AddNoResultHelpLabelWithButton(k_IndexesDisabledLabel, k_OpenIndexManagerLabel, () => OpenIndexManager());
            }

            if (!context.wantsMore)
                AddNoResultHelpLabelWithButton(k_ShowMoreResultsLabel, k_TurnItOnLabel, () => ToggleWantsMore());

            if (!context.showPackages)
                AddNoResultHelpLabelWithButton(k_ShowPackagesLabel, k_TurnItOnLabel, () => TogglePackages());
        }

        void AddNoResultQueryPropositions(int maxQueryCount)
        {
            var emptyFilterId = string.IsNullOrEmpty(context.filterId);
            if (emptyFilterId)
            {
                AddNoResultHelpLabel(k_AddFilterOrKeywordLabel);
                return;
            }

            var searches = new QueryHelperSearchGroup(viewState.queryBuilderEnabled, k_SearchesLabel);
            PopulateSearches(searches);
            var filteredQueries = GetFilteredQueries(searches.queries, context.filterId, viewState.queryBuilderEnabled)
                .Take(maxQueryCount).ToArray();
            if (filteredQueries.Length == 0)
            {
                AddNoResultHelpLabel(k_AddFilterOrKeywordLabel);
                return;
            }

            AddNoResultHelpLabel(k_TryAQueryLabel);
            var container = Create("SearchEmptyViewQueryPropositions", noResultsRowContainerClassName);
            PopulateSearchHelpers(filteredQueries, container);

            Add(container);
        }

        private void AddNoResultLabel(in string text)
        {
            var label = CreateLabel(text, null, PickingMode.Ignore, noResultsClassName);
            Add(label);
        }

        private void AddNoResultHelpLabel(in string text)
        {
            var label = CreateLabel(text, null, PickingMode.Ignore, noResultsHelpClassName);
            Add(label);
        }

        private void AddNoResultHelpLabelWithButton(in string text, in string buttonText, Action onClickedCallback)
        {
            var container = Create("SearchEmptyViewAction", noResultsRowContainerClassName);
            container.style.flexDirection = FlexDirection.Row;
            var label = CreateLabel(text, null, PickingMode.Ignore, noResultsHelpClassName);
            container.Add(label);
            var button = new Button(onClickedCallback)
            {
                text = buttonText
            };
            container.Add(button);
            Add(container);
        }

        void IResultView.Refresh(RefreshFlags flags)
        {
            BuildView();
        }

        void IResultView.OnGroupChanged(string prevGroupId, string newGroupId)
        {
            BuildView();
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
