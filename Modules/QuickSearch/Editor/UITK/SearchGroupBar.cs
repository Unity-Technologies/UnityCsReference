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
    class SearchGroupTab : VisualElement, IGroup
    {
        public static readonly string TabStyleClassName = SearchGroupBar.ussClassName.WithUssElement("tab");
        public static readonly string EmptyTabStyleClassName = TabStyleClassName.WithUssModifier("empty");
        public static readonly string TabButtonStyleClassName = TabStyleClassName.WithUssElement("button");
        public static readonly string TabLabelClassName = TabStyleClassName.WithUssElement("label");

        public Label groupNameLabel;
        public Label groupCountLabel;

        private readonly IGroup m_Group;
        private SearchGroupBar m_GroupBar;

        public new string name => m_Group.name;
        public string id => m_Group.id;
        public string type => m_Group.type;
        public int count => m_Group.count;
        public int priority => m_Group.priority;
        bool IGroup.optional { get => m_Group.optional; set => m_Group.optional = value; }

        public IEnumerable<SearchItem> items => m_Group.items;

        internal SearchGroupTab(SearchGroupBar groupBar, IGroup group)
        {
            m_Group = group;
            m_GroupBar = groupBar;

            AddToClassList(TabStyleClassName);

            if (count == 0)
                AddToClassList(EmptyTabStyleClassName);

            groupNameLabel = new Label();
            groupNameLabel.AddToClassList(TabLabelClassName);
            groupNameLabel.enableRichText = true;
            Add(groupNameLabel);

            groupCountLabel = new Label();
            groupCountLabel.enableRichText = true;
            Add(groupCountLabel);

            RegisterCallback<MouseDownEvent>(OnSelectGroupTab);
        }

        private void OnSelectGroupTab(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                var allTabs = parent.Children();
                foreach (var tab in allTabs)
                    tab.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates |= PseudoStates.Checked;

                m_Group.Sort();
                m_GroupBar.ViewModel.currentGroup = id;
            }
            else if (evt.button == 1)
                m_GroupBar.ShowVisibilityFilters();
        }
        

        SearchItem IGroup.ElementAt(int index) => m_Group.ElementAt(index);
        public int IndexOf(SearchItem item) => m_Group.IndexOf(item);
        bool IGroup.Add(SearchItem item) => m_Group.Add(item);
        void IGroup.Sort() => m_Group.Sort();
        void IGroup.SortBy(ISearchListComparer comparer) => m_Group.SortBy(comparer);
        void IGroup.Clear() => m_Group.Clear();
    }

    class SearchGroupBar : SearchElement
    {
        private const float k_MinTabWidth = 80f;
        private const float m_TabMoreButtonWidth = 30f;
        private const int k_MinimumGroupVisible = 1;

        private Button m_SyncButton;
        private SearchGroupTab m_HiddenSelectedGroup;
        private readonly Button m_TabMoreButton;
        private readonly VisualElement m_TabsContainer;
        private readonly List<SearchGroupTab> m_HiddenGroups = new();
        private readonly VisualElement m_ShowMoreGroup;
        private readonly VisualElement m_ResultViewButtonContainer;
        private readonly List<Action> m_SearchEventOffs = new();

        internal ISearchView ViewModel => m_ViewModel;

        public static readonly string ussClassName = "search-groupbar";
        public static readonly string groupBarButtonClassName = ussClassName.WithUssElement("button");
        public static readonly string groupBarSortButtonClassName = ussClassName.WithUssElement("sort-button");
        public static readonly string groupBarMoreButtonClassName = ussClassName.WithUssElement("more-button");
        public static readonly string groupBarVisButtonClassName = ussClassName.WithUssElement("visibility-button");
        public static readonly string tabsContainerClassName = ussClassName.WithUssElement("tabs-container");
        public static readonly string moreGroupTabClassName = ussClassName.WithUssElement("more-group-tab");
        public static readonly string moreGroupTabVisibleClassName = moreGroupTabClassName.WithUssModifier("visible");
        public static readonly string resultViewButtonContainerClassName = ussClassName.WithUssElement("result-view-button-container");
        public static readonly string syncSearchButtonClassName = ussClassName.WithUssElement("sync-search-button");

        private static bool s_IsDarkTheme => EditorGUIUtility.isProSkin;
        private readonly string m_SortButtonTooltip = L10n.Tr("Sort current group results");
        private readonly string m_VisibilityFiltersTooltip = L10n.Tr("Determine which items should be shown.");
        private readonly string m_MoreProviderFiltersTooltip = L10n.Tr("Manage search view appearance.");
        private readonly string m_SyncSearchButtonTooltip = L10n.Tr("Synchronize search fields (Ctrl + K)");
        private readonly string m_SyncSearchOnButtonTooltip = L10n.Tr("Synchronize search fields (Ctrl + K)");
        private readonly string m_SyncSearchAllGroupTabTooltip = L10n.Tr("Choose a specific search tab (eg. Project) to enable synchronization.");
        private readonly string m_SyncSearchProviderNotSupportedTooltip = L10n.Tr("Search provider doesn't support synchronization");
        private readonly string m_SyncSearchViewNotEnabledTooltip = L10n.Tr("Search provider uses a search engine\nthat cannot be synchronized.\nSee Preferences -> Search.");
        private readonly string m_TabCountTextColorFormat = s_IsDarkTheme ? "<color=#7B7B7B>{0}</color>" : "<color=#6A6A6A>{0}</color>";

        internal SearchGroupBar(string name, ISearchView viewModel)
            : base(name, viewModel, ussClassName)
        {
            focusable = true;

            m_TabsContainer = new VisualElement() { name = "SearchTabsContainer" };
            m_TabsContainer.AddToClassList(tabsContainerClassName);
            m_TabsContainer.RegisterCallback<AttachToPanelEvent>(AttachTabs);
            m_TabsContainer.RegisterCallback<DetachFromPanelEvent>(DetachTabs);

            // This tab will have a dropdown to select the extra tabs
            m_TabMoreButton = new Button(ShowHiddenGroups);
            m_TabMoreButton.AddToClassList(groupBarButtonClassName);
            m_TabMoreButton.AddToClassList(SearchGroupTab.TabButtonStyleClassName);

            m_ShowMoreGroup = new VisualElement() { name = "SearchMoreGroupTab" };
            m_ShowMoreGroup.AddToClassList(moreGroupTabClassName);

            m_ResultViewButtonContainer = new VisualElement();
            m_ResultViewButtonContainer.AddToClassList(resultViewButtonContainerClassName);

            Add(m_TabsContainer);
            AddMoreButtons();
        }

        private void AttachTabs(AttachToPanelEvent evt)
        {
            BuildGroups();
            
            m_SearchEventOffs.AddRange(new []
            {
                On(SearchEvent.DisplayModeChanged, UpdateResultViewButton),
                On(SearchEvent.RefreshContent, OnRefreshed),
                On(SearchEvent.TogglePackages, HandleTogglePackages),
                On(SearchEvent.ToggleWantsMore, HandleToggleWantsMore),
            });
        }

        private void DetachTabs(DetachFromPanelEvent evt)
        {            
            m_SearchEventOffs?.ForEach(off => off());

            // Make sure to remove callbacks when Detaching from panel
            EditorApplication.tick -= AdjustTabs;
        }

        private void BuildGroups()
        {
            m_TabsContainer.Clear();

            foreach (var g in m_ViewModel.EnumerateGroups())
                m_TabsContainer.Add(new SearchGroupTab(this, g));
            m_TabsContainer.Add(m_ShowMoreGroup);

            AdjustTabs();
        }

        private void AdjustTabs()
        {
            EditorApplication.tick -= AdjustTabs;

            AdjustTabs(m_TabsContainer.resolvedStyle.width, m_TabsContainer.resolvedStyle.height);

            m_TabsContainer.UnregisterCallback<GeometryChangedEvent>(AdjustTabs);
            m_TabsContainer.RegisterCallback<GeometryChangedEvent>(AdjustTabs);

            UpdateResultViewButton();
        }

        private void AdjustTabs(GeometryChangedEvent evt)
        {
            AdjustTabs(evt.newRect.width, evt.newRect.height);
        }

        private void AdjustTabs(float barWidth, float barHeight)
        {
            // Compute the available max space for tabs
            var availableTabWidth = k_MinTabWidth + m_TabMoreButtonWidth;
            var tempBarWidth = barWidth;
            if (barWidth < availableTabWidth)
            {
                tempBarWidth -= m_TabMoreButtonWidth;
                availableTabWidth = tempBarWidth;
            }

            foreach (var c in m_TabsContainer.Children())
            {
                if (c is not SearchGroupTab tab)
                    continue;

                tab.groupNameLabel.text = tab.name;
                if (tab.count > 0)
                {
                    var formattedCount = Utils.FormatCount((ulong)tab.count);
                    tab.groupCountLabel.text = string.Format(m_TabCountTextColorFormat, formattedCount);
                }

                var maxgroupNameWidth = tempBarWidth < 200f ? tempBarWidth * 3/4 : 150f;
                var maxgroupCountWidth = tempBarWidth < 200f ? tempBarWidth * 1/4 : 50f;
                var groupNameSize = tab.groupNameLabel.MeasureTextSize(tab.groupNameLabel.text, maxgroupNameWidth, MeasureMode.AtMost, barHeight, MeasureMode.Exactly);
                var groupCountSize = tab.groupCountLabel.MeasureTextSize(tab.groupCountLabel.text, maxgroupCountWidth, MeasureMode.AtMost, barHeight, MeasureMode.Exactly);

                var tabSize = groupNameSize + groupCountSize;
                if (tabSize.x > availableTabWidth)
                    availableTabWidth = tabSize.x;
            }

            // Add some padding to available space
            availableTabWidth = (float)Math.Round((availableTabWidth + (double)m_TabMoreButtonWidth) / 2d, MidpointRounding.AwayFromZero) * 2f;

            SearchGroupTab hiddenSelectedGroup = null;
            m_HiddenGroups.Clear();

            // Resize tabs and hide the extra tabs
            var tabIndex = 0;
            var fitTabCount = Math.Max(Mathf.FloorToInt(barWidth / availableTabWidth), k_MinimumGroupVisible);

            var showMore = false;
            if (fitTabCount == m_TabsContainer.childCount - 1)
                m_HiddenSelectedGroup = null;

            if (fitTabCount < m_TabsContainer.childCount - 1)
            {
                fitTabCount--;
                showMore = true;
            }

            foreach (var c in m_TabsContainer.Children())
            {
                if (c is not SearchGroupTab tab)
                    continue;

                if (m_HiddenSelectedGroup != null && string.Equals(tab.id, m_HiddenSelectedGroup.id, StringComparison.Ordinal) && tabIndex >= fitTabCount)
                    m_HiddenGroups.Add(tab);

                ++tabIndex;
            }

            tabIndex = 0;
            foreach (var c in m_TabsContainer.Children())
            {
                if (c is not SearchGroupTab tab)
                    continue;
                tab.style.width = availableTabWidth;
                tab.groupNameLabel.style.maxWidth = availableTabWidth * 0.7f;
                tab.groupCountLabel.style.maxWidth = availableTabWidth * 0.3f;

                if (tabIndex < fitTabCount)
                {
                    tab.style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (!m_HiddenGroups.Contains(tab))
                        m_HiddenGroups.Add(tab);

                    tab.style.display = DisplayStyle.None;
                }

                if (string.Equals(tab.id, m_ViewModel.currentGroup, StringComparison.Ordinal))
                {
                    if (tabIndex >= fitTabCount)
                        hiddenSelectedGroup = tab;
                    tab.pseudoStates |= PseudoStates.Checked;
                }
                else
                    tab.pseudoStates &= ~PseudoStates.Checked;

                ++tabIndex;
            }

            if (hiddenSelectedGroup != null)
                m_HiddenSelectedGroup = hiddenSelectedGroup;

            m_ShowMoreGroup.Clear();
            if (showMore && m_HiddenGroups.Count > 0)
            {
                var moreSelectedGroup = hiddenSelectedGroup ?? m_HiddenGroups.FirstOrDefault();
                var expandedTab = new SearchGroupTab(this, moreSelectedGroup);

                expandedTab.groupNameLabel.text = expandedTab.name;
                if (!context.empty)
                {
                    var formattedCount = Utils.FormatCount((ulong)expandedTab.count);
                    expandedTab.groupCountLabel.text = string.Format(m_TabCountTextColorFormat, formattedCount);
                }

                var maxNameLabelWidth = (float)Math.Round(((availableTabWidth - m_TabMoreButtonWidth) * 0.7f) / 2d, MidpointRounding.AwayFromZero) * 2f;
                var maxCountLabelWidth = (float)Math.Round(((availableTabWidth - m_TabMoreButtonWidth) * 0.3f) / 2d, MidpointRounding.AwayFromZero) * 2f;
                expandedTab.style.width = availableTabWidth;
                expandedTab.groupNameLabel.style.maxWidth = maxNameLabelWidth;
                expandedTab.groupCountLabel.style.maxWidth = maxCountLabelWidth;

                expandedTab.Add(m_TabMoreButton);
                m_TabMoreButton.style.maxWidth = m_TabMoreButtonWidth;

                if (moreSelectedGroup == hiddenSelectedGroup)
                    expandedTab.pseudoStates |= PseudoStates.Checked;

                m_HiddenGroups.Remove(moreSelectedGroup);
                m_ShowMoreGroup.Add(expandedTab);
                m_ShowMoreGroup.EnableInClassList(moreGroupTabVisibleClassName, true);
            }
            else
            {
                m_ShowMoreGroup.EnableInClassList(moreGroupTabVisibleClassName, false);
            }

            var providerSupportsSync = m_ViewModel.state.GetProviderById(m_ViewModel.currentGroup)?.supportsSyncViewSearch ?? false;
            var searchViewSyncEnabled = providerSupportsSync && SearchUtils.SearchViewSyncEnabled(m_ViewModel.currentGroup);
            var supportsSync = providerSupportsSync && searchViewSyncEnabled;
            if (m_SyncButton != null)
            {
                if (supportsSync)
                    m_SyncButton.style.display = DisplayStyle.Flex;
                else
                    m_SyncButton.style.display = DisplayStyle.None;

                if (m_ViewModel.syncSearch)
                    m_SyncButton.pseudoStates |= PseudoStates.Active;
                else
                    m_SyncButton.pseudoStates &= PseudoStates.Active;
            }
        }

        private void AddMoreButtons()
        {
            UpdateResultViewButton();
            Add(m_ResultViewButtonContainer);

            AddSyncSearchButton();

            if (!m_ViewModel.state.isSimplePicker)
            {
                var sortButton = CreateButton("SearchSortGroup", m_SortButtonTooltip, ShowSortOptions, groupBarButtonClassName, groupBarSortButtonClassName);
                Add(sortButton);
            }
            

            var visButton = CreateButton("SearchVisibilityOptions", m_VisibilityFiltersTooltip, ShowVisibilityFilters, groupBarButtonClassName, groupBarVisButtonClassName);
            Add(visButton);

            if (!m_ViewModel.state.isSimplePicker)
            {
                var moreButton = CreateButton("SearchMoreOptions", m_MoreProviderFiltersTooltip, ShowViewFilters, groupBarButtonClassName, groupBarMoreButtonClassName);
                Add(moreButton);
            }
        }

        private void ShowSortOptions()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Sort By Rank"), false, () => SortGroup<SortByScoreComparer>());
            menu.AddItem(new GUIContent("Sort By Name"), false, () => SortGroup<SortByNameComparer>());
            menu.AddItem(new GUIContent("Sort By Label"), false, () => SortGroup<SortByLabelComparer>());
            menu.AddItem(new GUIContent("Sort By Description"), false, () => SortGroup<SortByDescriptionComparer>());

            foreach (var s in SelectorManager.selectors)
            {
                if (!s.printable)
                    continue;

                if (string.IsNullOrEmpty(s.provider) || string.CompareOrdinal(s.provider, m_ViewModel.currentGroup) == 0)
                {
                    var sortType = s.description ?? s.label;
                    menu.AddItem(new GUIContent($"More.../Sort By {ObjectNames.NicifyVariableName(sortType)}"), false, () => SortGroupBySelector(s));
                }
            }

            menu.ShowAsContext();
        }

        private void SortGroup<T>() where T: ISearchListComparer, new()
        {
            SortGroup(new T());
        }

        private void SortGroupBySelector(SearchSelector selector)
        {
            SortGroup(new SortBySelector(selector));
        }

        private void SortGroup(ISearchListComparer comparer)
        {
            m_ViewModel.results.SortBy(comparer);
            Dispatcher.Emit(SearchEvent.RefreshContent, new SearchEventPayload(this, RefreshFlags.ItemsChanged));
        }

        private void UpdateResultViewButton()
        {
            Emit(SearchEvent.RequestResultViewButtons, _ => m_ResultViewButtonContainer.Clear(), null, m_ResultViewButtonContainer);
        }

        private void UpdateResultViewButton(ISearchEvent evt)
        {
            UpdateResultViewButton();
        }

        private void SetSyncSearchView(bool sync)
        {
            var providerSupportsSync = m_ViewModel.state.GetProviderById(m_ViewModel.currentGroup)?.supportsSyncViewSearch ?? false;
            var searchViewSyncEnabled = providerSupportsSync && SearchUtils.SearchViewSyncEnabled(m_ViewModel.currentGroup);
            var supportsSync = providerSupportsSync && searchViewSyncEnabled;
            if (!supportsSync)
                return;

            if (sync)
                m_SyncButton.pseudoStates |= PseudoStates.Active;
            else
                m_SyncButton.pseudoStates &= PseudoStates.Active;

            m_ViewModel.syncSearch = sync;
            if (m_ViewModel.syncSearch)
                SearchAnalytics.SendEvent(viewState.sessionId, SearchAnalytics.GenericEventType.QuickSearchSyncViewButton, m_ViewModel.currentGroup);
            m_ViewModel.Refresh();
        }

        private void AddSyncSearchButton()
        {
            var providerSupportsSync = m_ViewModel.state.GetProviderById(m_ViewModel.currentGroup)?.supportsSyncViewSearch ?? false;
            var searchViewSyncEnabled = providerSupportsSync && SearchUtils.SearchViewSyncEnabled(m_ViewModel.currentGroup);
            var supportsSync = providerSupportsSync && searchViewSyncEnabled && !viewState.isPicker;
            if (!supportsSync)
                return;
            var syncButtonTooltip = m_ViewModel.currentGroup == GroupedSearchList.allGroupId ? m_SyncSearchAllGroupTabTooltip :
                !providerSupportsSync ? m_SyncSearchProviderNotSupportedTooltip :
                !searchViewSyncEnabled ? m_SyncSearchViewNotEnabledTooltip :
                m_ViewModel.syncSearch ? m_SyncSearchOnButtonTooltip : m_SyncSearchButtonTooltip;

            m_SyncButton = CreateButton("SyncSearchButton", syncButtonTooltip, () => SetSyncSearchView(!m_ViewModel.syncSearch), groupBarButtonClassName, syncSearchButtonClassName);
            if (m_ViewModel.syncSearch)
                m_SyncButton.pseudoStates |= PseudoStates.Active;
            else
                m_SyncButton.pseudoStates &= PseudoStates.Active;

            Add(m_SyncButton);
        }

        private void ShowHiddenGroups()
        {
            if (m_HiddenGroups == null || m_HiddenGroups.Count == 0)
                return;

            var groupMenu = new GenericMenu();
            foreach (var g in m_HiddenGroups)
            {
                var groupContent = new GUIContent($"{g.name} ({g.count})");
                groupMenu.AddItem(groupContent, false, () => SelectGroupTab(g));
            }
            groupMenu.ShowAsContext();
        }

        private void SelectGroupTab(SearchGroupTab groupTab)
        {
            m_ViewModel.currentGroup = groupTab.id;
        }

        private void OnRefreshed(ISearchEvent evt)
        {
            var flags = evt.GetArgument<RefreshFlags>(0);
            if ((flags & RefreshFlags.GroupChanged) != 0)
            {
                EditorApplication.tick -= AdjustTabs;
                EditorApplication.tick += AdjustTabs;
            }
            else if ((flags & RefreshFlags.QueryCompleted) != 0)
            {
                BuildGroups();
            }
        }

        internal void ShowVisibilityFilters()
        {
            var filterMenu = new GenericMenu();

            var wantsMoreContent = new GUIContent(L10n.Tr($"Show more results"));

            filterMenu.AddItem(new GUIContent(L10n.Tr($"Show Packages results")), context.options.HasAny(SearchFlags.Packages), TogglePackages);
            filterMenu.AddItem(wantsMoreContent, context?.wantsMore ?? false, ToggleWantsMore);

            if (!m_ViewModel.state.isSimplePicker)
            {
                var dbs = SearchDatabase.EnumerateAll().ToList();
                if (dbs.Count > 1)
                {
                    filterMenu.AddSeparator(string.Empty);
                    filterMenu.AddDisabledItem(new GUIContent("Indexes"));
                    filterMenu.AddSeparator(string.Empty);

                    foreach (var db in dbs)
                    {
                        filterMenu.AddItem(new GUIContent($"{db.name} ({db.settings.type} index)"), !db.settings.options.disabled, () => ToggleIndexEnabled(db));
                    }
                }
            }
            filterMenu.ShowAsContext();
        }

        internal void ShowViewFilters()
        {
            var filterMenu = new GenericMenu();
            if (m_ViewModel is ISearchWindow window)
                window.AddItemsToMenu(filterMenu);

            filterMenu.AddSeparator(string.Empty);
            filterMenu.AddDisabledItem(new GUIContent("Search Providers"));
            filterMenu.AddSeparator(string.Empty);
            if (m_ViewModel is ISearchWindow searchWindow)
                searchWindow.AddProvidersToMenu(filterMenu);
            else
            {
                foreach (var p in context.GetProviders())
                {
                    var filterContent = new GUIContent($"{p.name} ({p.filterId})");
                    filterMenu.AddDisabledItem(filterContent, context.IsEnabled(p.id));
                }
            }
            filterMenu.ShowAsContext();
        }

        void HandleToggleWantsMore(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;
            ToggleWantsMore();
        }

        void HandleTogglePackages(ISearchEvent evt)
        {
            if (evt.sourceViewState != viewState)
                return;
            TogglePackages();
        }

        internal void TogglePackages()
        {
            if (m_ViewModel.context.showPackages)
            {
                SearchSettings.defaultFlags &= ~SearchFlags.Packages;
                m_ViewModel.context.showPackages = false;
            }
            else
            {
                SearchSettings.defaultFlags |= SearchFlags.Packages;
                m_ViewModel.context.showPackages = true;
            }
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(SearchFlags.Packages), m_ViewModel.context?.wantsMore.ToString());
            Refresh(RefreshFlags.StructureChanged);
        }

        private void ToggleIndexEnabled(SearchDatabase db)
        {
            db.settings.options.disabled = !db.settings.options.disabled;
            if (m_ViewModel is SearchWindow window)
                window.Refresh(RefreshFlags.GroupChanged);
        }

        internal void ToggleWantsMore()
        {
            if (m_ViewModel.context.wantsMore)
            {
                SearchSettings.defaultFlags &= ~SearchFlags.WantsMore;
                m_ViewModel.context.wantsMore = false;
            }
            else
            {
                SearchSettings.defaultFlags |= SearchFlags.WantsMore;
                m_ViewModel.context.wantsMore = true;
            }
            SearchSettings.wantsMore = m_ViewModel.context?.wantsMore ?? false;
            SendEvent(SearchAnalytics.GenericEventType.PreferenceChanged, nameof(m_ViewModel.context.wantsMore), m_ViewModel.context?.wantsMore.ToString());
            Refresh(RefreshFlags.StructureChanged);
        }

        internal void Refresh(RefreshFlags flags)
        {
            if (m_ViewModel is SearchWindow window)
                window.Refresh(flags);
        }

        protected void SendEvent(SearchAnalytics.GenericEventType category, string name = null, string message = null, string description = null)
        {
            string id = null;
            if (m_ViewModel is SearchWindow window)
                id = window.windowId;
            SearchAnalytics.SendEvent(id, category, name, message, description);
        }
    }
}
