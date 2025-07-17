// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using UnityEditor.UIElements;

namespace UnityEditor.Search
{
    class SearchQueryTreeViewItem : SearchElement
    {
        const string k_UssClassName = "search-query-tree-panel-item";
        const string k_ListItemClassName = "search-query-treeview-item";
        const string k_UssTreeViewClassName = "search-query-tree-panel";

        static readonly string k_NameLabelClassName = k_ListItemClassName.WithUssElement("label");

        internal static readonly string CountLabelClassName = k_ListItemClassName.WithUssElement("count");
        internal static readonly string HeaderClassName = k_UssTreeViewClassName.WithUssElement("header");

        RenamableLabel m_Label;
        VisualElement m_Icon;
        Label m_CountLabel;
        Action<SearchQueryTreeViewItem, ContextualMenuPopulateEvent> m_ActivateContextualMenu;

        bool m_IsRenaming;

        public ISearchQueryNodeHandler Handler { get; set; }
        public SearchQueryNodeData Data { get; set; }

        public bool Selected { get; set; }
        public string Text => m_Label.text;

        public SearchQueryTreeViewItem(ISearchView viewModel, Action<SearchQueryTreeViewItem, ContextualMenuPopulateEvent> menuHandler = null, params string[] classes)
            : base(nameof(SearchQueryTreeViewItem), viewModel, classes)
        {
            m_ActivateContextualMenu = menuHandler;

            AddToClassList(k_ListItemClassName);

            m_Label = new RenamableLabel();
            m_Label.AddToClassList(k_NameLabelClassName);
            m_Label.renameFinished += HandleRenameFinished;

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(SearchQueryPanelTreeView.IconClassName);

            m_CountLabel = new Label();
            m_CountLabel.AddToClassList(CountLabelClassName);
            m_CountLabel.pickingMode = PickingMode.Ignore;

            style.flexDirection = FlexDirection.Row;

            Add(m_Icon);
            Add(m_Label);
            Add(m_CountLabel);

            IManipulator contextualMenuManipulator = new ContextualMenuManipulator(HandleContextualMenu);
            this.AddManipulator(contextualMenuManipulator);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        public void Bind(Texture2D icon, string label, int count = -1)
        {
            OnAll(SearchEvent.SearchQueryItemCountUpdated, HandleSearchQueryItemCountUpdated);

            m_Icon.style.display = icon == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (icon != null)
                m_Icon.style.backgroundImage = new StyleBackground(icon);

            m_Label.text = label;

            m_CountLabel.style.display = count < 0 ? DisplayStyle.None : DisplayStyle.Flex;
            m_CountLabel.text = count.ToString();
        }

        public void Unbind()
        {
            Off(SearchEvent.SearchQueryItemCountUpdated, HandleSearchQueryItemCountUpdated);
        }

        protected void HandleContextualMenu(ContextualMenuPopulateEvent evt)
            => m_ActivateContextualMenu?.Invoke(this, evt);

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.clickCount != 1 || evt.button != 0)
                return;

            if (!Data.ValidQuery)
                return;

            if (!Selected)
            {
                Selected = true;
            }
            else if (Selected  && !m_IsRenaming && SupportsRename)
            {
                Rename();
                evt.StopImmediatePropagation();
                focusController.IgnoreEvent(evt);
            }
        }

        public bool SupportsRename
        {
            get
            {
                if (Handler == null)
                    return true;
                var query = Handler.GetQuery(Data.QueryId) ?? Handler.GetQuery(Data.TreeId);
                if (query == null)
                    return true;
                return Handler.SupportsRename(query);
            }
        }

        public void Rename()
        {
            if (!SupportsRename)
                return;

            m_Label.StartRename();
            m_IsRenaming = true;
        }

        protected void HandleRenameFinished(string newName)
        {
            if (!SupportsRename)
                return;
            Handler?.Rename(this, newName);
            m_IsRenaming = false;
        }

        void HandleSearchQueryItemCountUpdated(ISearchEvent evt)
        {
            var guid = evt.GetArgument<string>(0);
            if (!Data.QueryId.Equals(guid, StringComparison.Ordinal))
                return;
            var count = evt.GetArgument<int>(1);
            UpdateSearchQueryItemCount(count);
        }

        void UpdateSearchQueryItemCount(int itemCount)
        {
            var hidden = itemCount < 0;
            m_CountLabel.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;

            if (hidden)
                return;

            var formattedCount = Utils.FormatCount(Convert.ToUInt64(itemCount));
            m_CountLabel.text = string.Format(formattedCount);
        }
    }

    class SearchQueryPanelTreeView : SearchElement
    {
        internal const string UssClassName = "search-query-tree-panel";
        internal static readonly string HeaderClassName = UssClassName.WithUssElement("header");
        internal static readonly string HeaderButtonRowClassName = HeaderClassName.WithUssElement("button-row");
        internal static readonly string SearchQueryIconClassName = HeaderClassName.WithUssElement("icon");
        internal static readonly string FilterButtonClassName = HeaderClassName.WithUssElement("filter");
        internal static readonly string SearchFieldClassName = HeaderClassName.WithUssElement("search-field");

        internal static readonly string IconClassName = UssClassName.WithUssElement("icon");
        internal static readonly string IconClickableClassName = IconClassName.WithUssModifier("clickable");

        static readonly string k_FilterSearchButtonTooltip = L10n.Tr("Filter Searches");
        static readonly string k_SearchesLabel = L10n.Tr("Searches");

        VisualElement m_Header;
        ToolbarSearchField m_SearchField;

        VisualElement m_FilterSearchQueryButton;
        bool m_FilterSearchQuery;
        string m_CurrentSearchFilter;

        ISearchQuery m_LastSelectedQuery;

        TreeView m_TreeView;

        internal Dictionary<string,ISearchQueryNodeHandler> Handlers { get; }
        internal Dictionary<int, SearchQueryTreeViewItem> TreeViewItems { get; } = new ();
        List<TreeViewItemData<SearchQueryNodeData>> TreeRoots
        {
            get
            {
                var rootItems = new List<TreeViewItemData<SearchQueryNodeData>>();
                foreach (var handler in Handlers.Values)
                {
                    if (handler.RootItem != null)
                        rootItems.Add(handler.RootItem.Value);
                }
                return rootItems;
            }
        }

        internal ISearchQuery SelectedQuery => m_LastSelectedQuery;

        public SearchQueryPanelTreeView(string name, ISearchView viewModel, params string[] classes)
            : base(name, viewModel, classes)
        {
            m_Header = MakeHeader();
            Add(m_Header);

            m_TreeView = CreateSearchQueryTreeView();
            Add(m_TreeView);

            Handlers = new Dictionary<string, ISearchQueryNodeHandler>();

            var queryTreeConfig = viewModel.state.queryTreeConfig;
            if (queryTreeConfig == null || queryTreeConfig.NodeSources == null || queryTreeConfig.NodeSources.Length == 0)
            {
                queryTreeConfig = SearchQueryTreeConfig.CreateDefault();
            }

            foreach (var nodeSource in queryTreeConfig.NodeSources)
            {
                if (nodeSource == null || nodeSource.handler == null)
                    continue;
                var handler = nodeSource.handler.Invoke();
                if (handler == null)
                    continue;
                Handlers.Add(handler.Name, handler);
            }

            RebuildTreeViewFromAllSources();

            UpdateQueriesFilterState(false, false);
        }

        public static TreeViewItemData<SearchQueryNodeData> CreateItemData(int itemId, SearchQueryNodeData data, List<TreeViewItemData<SearchQueryNodeData>> children = null)
        {
            return new TreeViewItemData<SearchQueryNodeData>(itemId, data, children);
        }

        public static TreeViewItemData<SearchQueryNodeData> CreateItemData(bool isRoot, int handlerId, string name, string queryId = "", int treeId = -1, List<TreeViewItemData<SearchQueryNodeData>> children = null)
        {
            return new TreeViewItemData<SearchQueryNodeData>(treeId, new SearchQueryNodeData(isRoot, handlerId, name, queryId, treeId), children);
        }

        public static TreeViewItemData<SearchQueryNodeData> CreateItemData(int handlerId, string name, string queryId = "", int treeId = -1)
        {
            return new TreeViewItemData<SearchQueryNodeData>(treeId, new SearchQueryNodeData(false, handlerId, name, queryId, treeId), null);
        }

        public static TreeViewItemData<SearchQueryNodeData> CreateItemData(bool isRoot, int handlerId, string name, List<TreeViewItemData<SearchQueryNodeData>> children = null)
        {
            return new TreeViewItemData<SearchQueryNodeData>(handlerId, new SearchQueryNodeData(isRoot, handlerId, name, string.Empty, -1), children);
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            UpdateExpandedState();
            SetActiveQuery(viewState.activeQuery);
            if (viewState.activeQuery != null)
            {
                var treeId = viewState.activeQuery.GetTreeId();
                m_TreeView.ScrollToItemById(treeId);
            }

            RegisterCallback<KeyDownEvent>(HandleKeyDown);

            OnAll(SearchEvent.ActiveQueryChanged, HandleActiveQueryChanged);

            OnAll(SearchEvent.UserQueryAdded, HandleUserQueriesAdded);
            OnAll(SearchEvent.UserQueryChanged, HandleUserQueriesChanged);
            OnAll(SearchEvent.UserQueryRemoved, HandleUserQueriesRemoved);

            OnAll(SearchEvent.ProjectQueryAdded, HandleProjectQueriesAdded);
            OnAll(SearchEvent.ProjectQueryChanged, HandleProjectQueriesChanged);
            OnAll(SearchEvent.ProjectQueryRemoved, HandleProjectQueriesRemoved);
            OnAll(SearchEvent.PostProcessProjectQueryAdded, HandleProjectQueriesAdded);
            OnAll(SearchEvent.PostProcessProjectQueryMoved, HandleProjectQueriesMoved);
            OnAll(SearchEvent.PostProcessProjectQueryRemoved, HandlePostProcessProjectQueriesRemoved);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(HandleKeyDown);

            Off(SearchEvent.ActiveQueryChanged, HandleActiveQueryChanged);

            Off(SearchEvent.UserQueryAdded, HandleUserQueriesAdded);
            Off(SearchEvent.UserQueryChanged, HandleUserQueriesChanged);
            Off(SearchEvent.UserQueryRemoved, HandleUserQueriesRemoved);

            Off(SearchEvent.ProjectQueryAdded, HandleProjectQueriesAdded);
            Off(SearchEvent.ProjectQueryChanged, HandleProjectQueriesChanged);
            Off(SearchEvent.ProjectQueryRemoved, HandleProjectQueriesRemoved);
            Off(SearchEvent.PostProcessProjectQueryAdded, HandleProjectQueriesAdded);
            Off(SearchEvent.PostProcessProjectQueryMoved, HandleProjectQueriesMoved);
            Off(SearchEvent.PostProcessProjectQueryRemoved, HandlePostProcessProjectQueriesRemoved);

            base.OnDetachFromPanel(evt);
        }

        TreeView CreateSearchQueryTreeView()
        {
            var treeView = new TreeView();
            treeView.bindItem = BindItem;
            treeView.unbindItem = UnbindItem;
            treeView.makeItem = MakeItem;
            treeView.selectionType = SelectionType.Single;
            treeView.selectedIndicesChanged += HandleItemsSelected;
            treeView.itemExpandedChanged += HandleItemExpanded;
            return treeView;
        }

        void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.F2)
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (m_TreeView.selectedIndex < 0)
                return;

            if (!TreeViewItems.TryGetValue(m_TreeView.selectedIndex, out var selectedItem))
                return;

            if (selectedItem.Data.ValidQuery)
                selectedItem.Rename();
        }

        void HandleItemsSelected(IEnumerable<int> indices)
        {
            if (indices != null && indices.GetCount() > 0)
            {
                var itemIndex = SearchQueryPanelTreeUtils.GetFirstElement(indices);
                var rootElement = m_TreeView.GetRootElementForIndex(itemIndex);
                if (rootElement == null)
                    return;

                var selectedItem = rootElement.Q<SearchQueryTreeViewItem>();
                foreach (var item in TreeViewItems.Values)
                {
                    if (item != selectedItem)
                        item.Selected = false;
                }

                if (m_LastSelectedQuery != null && m_LastSelectedQuery.GetTreeId() == selectedItem.Data.TreeId)
                    return;

                m_LastSelectedQuery = selectedItem.Handler.GetQuery(selectedItem.Data.TreeId);
                selectedItem.Handler.ActivateItem(m_TreeView, selectedItem);
            }
        }

        SearchQueryTreeViewItem MakeItem()
        {
            return new SearchQueryTreeViewItem(m_ViewModel, HandleContextualMenu);
        }

        void BindItem(VisualElement e, int index)
        {
            var item = (SearchQueryTreeViewItem)e;
            var data = m_TreeView.GetItemDataForIndex<SearchQueryNodeData>(index);

            ISearchQueryNodeHandler handler = null;
            foreach (var h in Handlers.Values)
            {
                if (h.HandlerId == data.HandlerId)
                {
                    handler = h;
                    break;
                }
            }

            item.Data = data;
            item.Handler = handler;

            var rootElement = m_TreeView.GetRootElementForIndex(index);
            if (item.Data.IsRoot)
                rootElement.AddToClassList(SearchQueryTreeViewItem.HeaderClassName);
            else
                rootElement.RemoveFromClassList(SearchQueryTreeViewItem.HeaderClassName);

            handler?.BindItem(m_TreeView, item, index);
            TreeViewItems[index] = item;
        }

        void UnbindItem(VisualElement e, int index)
        {
            var item = (SearchQueryTreeViewItem)e;
            item.Unbind();

            TreeViewItems.Remove(index);
        }

        VisualElement MakeHeader()
        {
            var headerElement = new VisualElement();
            headerElement.style.flexDirection = FlexDirection.Column;
            headerElement.AddToClassList(HeaderClassName);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.AddToClassList(HeaderButtonRowClassName);

            var iconElement = Create<VisualElement>(null, IconClassName, SearchQueryIconClassName);
            var label = Create<Label>(null);
            label.text = k_SearchesLabel;

            m_FilterSearchQueryButton = Create<VisualElement>(null, IconClassName, IconClickableClassName, FilterButtonClassName);
            m_FilterSearchQueryButton.tooltip = k_FilterSearchButtonTooltip;
            m_FilterSearchQueryButton.RegisterCallback<MouseUpEvent>(HandleFilterClicked);

            buttonRow.Add(iconElement);
            buttonRow.Add(label);
            buttonRow.Add(m_FilterSearchQueryButton);
            headerElement.Add(buttonRow);

            var textField = new ToolbarSearchField();
            textField.AddToClassList(SearchFieldClassName);
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

        void UpdateQueriesFilterState(bool enabled, bool rebuildTree = true)
        {
            m_FilterSearchQuery = enabled;
            m_FilterSearchQueryButton?.EnableInClassList(FilterButtonClassName.WithUssModifier("opened"), enabled);

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
                }

                UpdateSavedQueriesFiltering(string.Empty, rebuildTree);

                if (!enabled)
                    // Reselect last selected query when disabling the filter
                    UpdateTreeViewSelection(m_LastSelectedQuery);
            }
        }

        void HandleSearchFieldValueChanged(ChangeEvent<string> evt)
        {
            UpdateSavedQueriesFiltering(evt.newValue);
        }

        void UpdateSavedQueriesFiltering(string searchQuery, bool rebuildTree = true)
        {
            if (m_CurrentSearchFilter == searchQuery)
                return;

            m_CurrentSearchFilter = searchQuery;

            if (rebuildTree)
            {
                RebuildTreeViewFromAllSources();
                UpdateExpandedState();
            }
        }

        void RebuildRoots(string searchFilter = "")
        {
            foreach (var handler in Handlers.Values)
            {
                handler.BuildRoots(context, searchFilter);
            }
        }

        void RebuildTreeViewFromAllSources()
        {
            RebuildRoots(m_CurrentSearchFilter);
            RefreshTreeView();
        }

        void RefreshTreeView()
        {
            m_TreeView.SetRootItems(TreeRoots);
            m_TreeView.Rebuild();
            UpdateExpandedState();
        }

        void HandleItemExpanded(TreeViewExpansionChangedArgs args)
        {
            if (m_FilterSearchQuery)
                // Do not save expanded state when filtering
                return;

            var expandedItems = new HashSet<int>(SearchSettings.expandedQueries);
            if (args.isExpanded)
                expandedItems.Add(args.id);
            else
                expandedItems.Remove(args.id);

            SearchSettings.expandedQueries = new int[expandedItems.Count];
            expandedItems.CopyTo(SearchSettings.expandedQueries);
        }

        void UpdateExpandedState()
        {
            if (m_FilterSearchQuery)
            {
                // When filtering, expand all available roots
                m_TreeView.ExpandAll();
                return;
            }

            if (SearchSettings.expandedQueries == null)
                return;

            var expandedItems = SearchSettings.expandedQueries;

            foreach (var expandedId in expandedItems)
                m_TreeView.ExpandItem(expandedId);
        }

        void SetActiveQuery(ISearchQuery query)
        {
            if (query == m_LastSelectedQuery)
                return;

            UpdateTreeViewSelection(query);
        }

        void UpdateTreeViewSelection(ISearchQuery query)
        {
            if (query == null || !SearchQueryPanelTreeUtils.IsQueryNameMatchingFilter(m_CurrentSearchFilter, query.displayName))
            {
                m_TreeView.ClearSelection();
                m_LastSelectedQuery = null;
                return;
            }

            var treeId = query.GetTreeId();
            m_LastSelectedQuery = query;
            m_TreeView.SetSelectionById(treeId);
        }

        void HandleContextualMenu(SearchQueryTreeViewItem item, ContextualMenuPopulateEvent evt)
        {
            item.Handler.PopulateContextualMenu(m_TreeView, context, item, evt.menu);
        }

        void HandleActiveQueryChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState == viewState)
            {
                var query = evt.GetArgument<ISearchQuery>(0);
                SetActiveQuery(query);
            }
        }

        void HandleUserQueriesAdded(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.UserQueriesLabel, out var userQueryHandler))
                return;

            var queries = SearchQueryPanelTreeUtils.ParseQueries(evt);
            if (queries.GetCount() > 0)
            {
                foreach (var query in queries)
                    userQueryHandler.AddQuery(query);

                RefreshTreeView();
            }
        }

        void HandleUserQueriesChanged(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.UserQueriesLabel, out var userQueryHandler))
                return;

            var queries = SearchQueryPanelTreeUtils.ParseQueries(evt);
            if (queries.GetCount() > 0)
            {
                foreach (var query in queries)
                    userQueryHandler.UpdateQuery(query);

                RefreshTreeView();
            }
        }

        void HandleUserQueriesRemoved(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.UserQueriesLabel, out var userQueryHandler))
                return;

            var queries = SearchQueryPanelTreeUtils.ParseQueries(evt);
            if (queries.GetCount() > 0)
            {
                foreach (var query in queries)
                {
                    userQueryHandler.RemoveQuery(query);
                    m_TreeView.TryRemoveItem(query.GetTreeId(), false);
                }

                RefreshTreeView();
            }
        }

        void HandleProjectQueriesAdded(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.ProjectQueriesLabel, out var projectQueryHandler))
                return;

            var queries = SearchQueryPanelTreeUtils.ParseQueries(evt);
            if (queries.GetCount() > 0)
            {
                foreach (var query in queries)
                    projectQueryHandler.AddQuery(query);

                RefreshTreeView();
            }
        }

        void HandleProjectQueriesChanged(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.ProjectQueriesLabel, out var projectQueryHandler))
                return;

            var queries = SearchQueryPanelTreeUtils.ParseQueries(evt);
            if (queries.GetCount() > 0)
            {
                foreach (var query in queries)
                    projectQueryHandler.UpdateQuery(query);

                RefreshTreeView();
            }
        }

        void HandleProjectQueriesMoved(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.ProjectQueriesLabel, out var projectQueryHandler))
                return;

            var queryPaths = SearchQueryPanelTreeUtils.ParseQueryPaths(evt);
            if (queryPaths.GetCount() > 0)
            {
                var queries = new List<SearchQueryAsset>();
                foreach (var queryPath in queryPaths)
                {
                    var query = AssetDatabase.LoadAssetAtPath<SearchQueryAsset>(queryPath);
                    if (query != null)
                    {
                        queries.Add(query);
                    }
                }

                foreach (var query in queries)
                    projectQueryHandler.UpdateQuery(query);

                RefreshTreeView();
            }
        }

        void HandleProjectQueriesRemoved(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.ProjectQueriesLabel, out var projectQueryHandler))
                return;

            var queryIds = SearchQueryPanelTreeUtils.ParseQueryIds(evt);
            if (queryIds.GetCount() > 0)
            {
                foreach (var queryId in queryIds)
                {
                    var queryIdHash = HashingUtils.GetHashCode(queryId);
                    projectQueryHandler.RemoveQuery(queryIdHash);
                }

                RefreshTreeView();
            }
        }

        void HandlePostProcessProjectQueriesRemoved(ISearchEvent evt)
        {
            if (!Handlers.TryGetValue(SearchQueryTreeConfig.ProjectQueriesLabel, out var queryHandler))
                return;

            var queryPaths = SearchQueryPanelTreeUtils.ParseQueryPaths(evt);
            if (queryPaths.GetCount() > 0 && queryHandler is ProjectSearchQueryTreeNodeHandler projectQueryHandler)
            {
                foreach (var queryPath in queryPaths)
                {
                    var removedId = projectQueryHandler.RemoveQuery(queryPath);
                    if (removedId != null && viewState.activeQuery != null && viewState.activeQuery.guid == removedId)
                        viewState.activeQuery = null;
                }

                RefreshTreeView();
            }
        }
    }
}
