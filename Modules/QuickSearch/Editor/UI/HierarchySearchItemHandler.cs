// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Hierarchy;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    sealed class HierarchySearchItemHandler : HierarchyNodeTypeHandler, IHierarchyEditorNodeTypeHandler
    {
        enum UpdateMode
        {
            Update,
            UpdateIncremental,
            UpdateIncrementalTimed
        }

        enum UpdateStage
        {
            MissingParents,
            Sorting,
            Count
        }

        const string k_TokenSeparatedParentProviderSuffix = "_TokenSeparatedParent";
        static readonly string k_ParentCycleErrorFormat = L10n.Tr("Parent cycle detected for SearchItem \"{0}\".");

        ISearchView m_ViewModel;
        readonly SearchItemHierarchyNodeMap m_SearchItemHierarchyNodeMap = new();
        readonly List<SearchItem> m_ItemsMissingParents = new();
        readonly Dictionary<SearchProvider, SearchProvider> m_TokenSeparatedParentProviders = new();
        SearchItemHierarchySorting m_HierarchySorting;
        UpdateStage m_CurrentUpdateStage = UpdateStage.MissingParents;
        HashSet<SearchItem> m_VisitedForCycleSet = new();

        // We can only sort when all the results have been obtained in order to have the same behavior as the other views.
        bool m_CanSort;

        // Stopwatches used for timed updates
        readonly Stopwatch m_UpdateTimer = new();
        readonly Stopwatch m_MissingParentTimer = new();

        public SearchContext Context => m_ViewModel.context;

        public bool UpdateNeeded => m_ItemsMissingParents.Count > 0 || SortingNeeded;
        public bool SortingNeeded => m_HierarchySorting.UpdateNeeded && CanSort;

        public bool CanSort
        {
            get => m_CanSort;
            set
            {
                if (value != m_CanSort)
                    RestartSorting();
                m_CanSort = value;
            }
        }

        public event Action<SearchItem> ItemDoubleClicked;

        public void Setup(ISearchView viewModel)
        {
            m_ViewModel = viewModel;
            RebuildHierarchy();
        }

        #region HierarchyNodeTypeHandler
        protected override void Initialize()
        {
            m_HierarchySorting = new SearchItemHierarchySorting(Hierarchy, CommandList, m_SearchItemHierarchyNodeMap);
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override string GetNodeTypeName()
        {
            return nameof(HierarchySearchItemHandler);
        }

        /// <summary>
        /// Called when the hierarchy node type handler is bound to a hierarchy view.
        /// Typically used to add stylesheets or classes to the <see cref="HierarchyView.StyleContainer"/>.
        /// </summary>
        /// <param name="view">The hierarchy view.</param>
        protected override void OnBindView(HierarchyView view) { }

        /// <summary>
        /// Called when the hierarchy node type handler is unbound from a hierarchy view.
        /// </summary>
        /// <param name="view">The hierarchy view.</param>
        protected override void OnUnbindView(HierarchyView view) { }

        /// <summary>
        /// Called whenever a hierarchy view item is bound to a hierarchy view.
        /// Typically used to set up the item with the necessary data and styles.
        /// </summary>
        /// <param name="item">The hierarchy view item.</param>
        protected override void OnBindItem(HierarchyViewItem item)
        {
            if (!m_SearchItemHierarchyNodeMap.TryGetSearchItem(in item.Node, out var searchItem))
                return;

            item.RowContainer.AddToClassList(SearchTreeView.SearchTreeViewRowClassName);
            item.AddToClassList(SearchTreeView.SearchTreeViewItemUssClassName);
            item.RightCustomContainer.AddToClassList(SearchTreeView.SearchTreeViewItemButtonContainerClassName);

            // If the item is a builtin parent, the button container should not be visible since they don't have any actions
            // and can't be favorite.
            item.RightCustomContainer.EnableInClassList(SearchTreeView.SearchTreeViewItemButtonContainerDisabledClassName, IsBuiltinParentSearchItem(searchItem));

            var tex = searchItem.GetThumbnail(Context);
            item.Icon.style.backgroundImage = tex;

            if (!m_ViewModel.IsPicker())
            {
                var moreActionButton =
                    item.RightCustomContainer.Q<SearchViewItemButtonWithContext>(name: SearchViewItem.moreActionButtonName);
                if (moreActionButton == null)
                {
                    moreActionButton = new SearchViewItemButtonWithContext(SearchViewItem.moreActionButtonName,
                        string.Empty,
                        SearchViewItem.moreActionsTooltip,
                        OnActionDropdownClicked,
                        SearchElement.baseIconButtonClassName,
                        SearchTreeView.MoreActionButtonClassName);
                    item.RightCustomContainer.Add(moreActionButton);
                }
                moreActionButton.BoundItem = searchItem;
            }

            var favoriteButton =
                item.RightCustomContainer.Q<SearchViewItemButtonWithContext>(name: SearchViewItem.searchFavoriteButtonName);
            if (favoriteButton == null)
            {
                favoriteButton = new SearchViewItemButtonWithContext(SearchViewItem.searchFavoriteButtonName,
                    string.Empty,
                    SearchViewItem.searchFavoriteButtonTooltip,
                    OnFavoriteButtonClicked,
                    SearchElement.baseIconButtonClassName,
                    SearchViewItem.searchFavoriteButtonClassName);
                item.RightCustomContainer.Add(favoriteButton);
            }

            favoriteButton.BoundItem = searchItem;
            UpdateFavoriteImage(favoriteButton, searchItem);
        }

        /// <summary>
        /// Called whenever a hierarchy view item is unbound from a hierarchy view.
        /// </summary>
        /// <param name="item">The hierarchy view item.</param>
        protected override void OnUnbindItem(HierarchyViewItem item) { }
        #endregion

        #region IHierarchyEditorNodeTypeHandler
        public bool CanCut(HierarchyView view)
        {
            return false;
        }

        public bool OnCut(HierarchyView view)
        {
            throw new NotImplementedException();
        }

        public bool CanCopy(HierarchyView view)
        {
            return false;
        }

        public bool OnCopy(HierarchyView view)
        {
            throw new NotImplementedException();
        }

        public bool CanPaste(HierarchyView view)
        {
            return false;
        }

        public bool OnPaste(HierarchyView view)
        {
            throw new NotImplementedException();
        }

        public bool CanPasteAsChild(HierarchyView view)
        {
            return false;
        }

        public bool OnPasteAsChild(HierarchyView view, bool keepWorldPos)
        {
            throw new NotImplementedException();
        }

        public bool CanSetName(HierarchyView view, in HierarchyNode node)
        {
            return false;
        }

        public bool OnSetName(HierarchyView view, in HierarchyNode node, string name)
        {
            throw new NotImplementedException();
        }

        public string GetDisplayName(HierarchyView view, in HierarchyNode node)
        {
            if (m_SearchItemHierarchyNodeMap.TryGetSearchItem(node, out var searchItem))
                return searchItem.GetLabel(Context);
            return "Unknown Item";
        }

        public bool CanDuplicate(HierarchyView view)
        {
            return false;
        }

        public bool OnDuplicate(HierarchyView view)
        {
            throw new NotImplementedException();
        }

        public bool CanDelete(HierarchyView view)
        {
            return false;
        }

        public bool OnDelete(HierarchyView view)
        {
            throw new NotImplementedException();
        }

        public bool CanFindReferences(HierarchyView view)
        {
            return false;
        }

        public bool OnFindReferences(HierarchyView view)
        {
            throw new NotImplementedException();
        }

        public bool CanDoubleClick(HierarchyView view, in HierarchyNode node)
        {
            return true;
        }

        public bool OnDoubleClick(HierarchyView view, in HierarchyNode node)
        {
            if (m_SearchItemHierarchyNodeMap.TryGetSearchItem(in node, out var searchItem))
                ItemDoubleClicked?.Invoke(searchItem);
            return true;
        }

        public void GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip)
        {
            // No tooltip.
        }

        public void PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
        {
            // Do nothing.
        }

        public bool AcceptParent(HierarchyView view, in HierarchyNode parent)
        {
            return false;
        }

        public bool AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            return false;
        }

        public bool CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes)
        {
            return false;
        }

        public void OnStartDrag(in HierarchyViewDragAndDropSetupData data)
        {
            throw new NotImplementedException();
        }

        public DragVisualMode CanDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            return DragVisualMode.Rejected;
        }

        public DragVisualMode OnDrop(in HierarchyViewDragAndDropHandlingData data)
        {
            throw new NotImplementedException();
        }
        #endregion

        public bool TryGetNode(SearchItem searchItem, out HierarchyNode node)
        {
            return m_SearchItemHierarchyNodeMap.TryGetNode(searchItem, out node);
        }

        public bool TryGetSearchItem(in HierarchyNode node, out SearchItem searchItem)
        {
            return m_SearchItemHierarchyNodeMap.TryGetSearchItem(in node, out searchItem);
        }

        public bool IsBuiltinParentSearchItem(SearchItem searchItem)
        {
            var itemProvider = searchItem?.provider;
            if (itemProvider == null)
                return false;
            return itemProvider.id.EndsWith(k_TokenSeparatedParentProviderSuffix, StringComparison.Ordinal);
        }

        public void SetSearchItemComparer(IComparer<SearchItem> comparer)
        {
            m_HierarchySorting.SetSearchItemComparer(comparer);
        }

        public void UpdateSearchItemFavoriteState(string searchItemId)
        {
            // We don't know if this item is visible or not, so
            // dirty the hierarchy. It will trigger a rebinding of the items and update the favorite button state accordingly.
            CommandList.SetDirty();
        }

        public void Update()
        {
            // Don't do while(Update...){} because m_ItemsMissingParents could be non-emptiable at this point.
            // It could take multiple frames to receive the missing parents.
            DoUpdate(UpdateMode.Update, TimeSpan.Zero);
        }

        public bool UpdateIncremental()
        {
            return DoUpdate(UpdateMode.UpdateIncremental, TimeSpan.Zero);
        }

        public bool UpdateIncrementalTimed(TimeSpan timeLimit)
        {
            while (true)
            {
                m_UpdateTimer.Restart();
                if (!DoUpdate(UpdateMode.UpdateIncrementalTimed, timeLimit))
                    return false; // Update completed

                timeLimit -= m_UpdateTimer.Elapsed;
                if (timeLimit <= TimeSpan.Zero)
                    return true; // Timed out
            }
        }

        public void IntegrateNewSearchItems()
        {
            if (m_ViewModel == null)
                return;

            Hierarchy.Reserve(m_ViewModel.results.Count);
            foreach (var searchItem in m_ViewModel.results)
            {
                CreateNodesForSearchItem(searchItem);
            }
        }

        public void RebuildHierarchy()
        {
            Hierarchy.Clear();
            ClearMappings();
            RestartSorting();
            IntegrateNewSearchItems();
        }

        void CreateNodesForSearchItem(SearchItem searchItem)
        {
            if (!m_SearchItemHierarchyNodeMap.TryGetNode(searchItem, out var node))
            {
                CommandList.Add(Hierarchy.Root, out node);
                m_SearchItemHierarchyNodeMap.Add(searchItem, in node);

                var parentDescriptor = searchItem.GetParentDescriptor(Context);
                if (!string.IsNullOrEmpty(parentDescriptor.Id))
                {
                    switch (parentDescriptor.Type)
                    {
                        case SearchItemParentType.SearchItemId when m_SearchItemHierarchyNodeMap.TryGetNode(parentDescriptor.Id, out var parentNode):
                            TrySetParent(searchItem, parentDescriptor, node, parentNode);
                            break;
                        case SearchItemParentType.SearchItemId:
                            m_ItemsMissingParents.Add(searchItem);
                            break;
                        case SearchItemParentType.TokenSeparatedId:
                            BuildTokenSeparatedParentHierarchyFromSearchItem(searchItem, in node);
                            break;
                        default:
                            throw new NotImplementedException(parentDescriptor.Type.ToString());
                    }
                }

                RestartSorting();
            }
        }

        void ClearMappings()
        {
            m_SearchItemHierarchyNodeMap.Clear();
            m_ItemsMissingParents.Clear();
            m_VisitedForCycleSet.Clear();
        }

        bool UpdateItemsMissingParents()
        {
            if (m_ItemsMissingParents.Count == 0)
                return false;

            for (var i = m_ItemsMissingParents.Count - 1; i >= 0; --i)
            {
                var searchItem = m_ItemsMissingParents[i];
                var parentDescriptor = searchItem.GetParentDescriptor(Context);
                if (m_SearchItemHierarchyNodeMap.TryGetNode(parentDescriptor.Id, out var parentNode) &&
                    m_SearchItemHierarchyNodeMap.TryGetNode(searchItem, out var node))
                {
                    TrySetParent(searchItem, parentDescriptor, in node, in parentNode);
                    m_ItemsMissingParents.RemoveAt(i);
                    RestartSorting();
                }
            }

            return m_ItemsMissingParents.Count > 0;
        }

        bool UpdateItemsMissingParentsIncremental()
        {
            if (m_ItemsMissingParents.Count == 0)
                return false;

            var index = m_ItemsMissingParents.Count - 1;
            var searchItem = m_ItemsMissingParents[index];
            var parentDescriptor = searchItem.GetParentDescriptor(Context);
            if (m_SearchItemHierarchyNodeMap.TryGetNode(parentDescriptor.Id, out var parentNode) &&
                m_SearchItemHierarchyNodeMap.TryGetNode(searchItem, out var node))
            {
                TrySetParent(searchItem, parentDescriptor, in node, in parentNode);
                m_ItemsMissingParents.RemoveAt(index);
                RestartSorting();
            }

            return m_ItemsMissingParents.Count > 0;
        }

        bool UpdateItemsMissingParentsIncrementalTimed(TimeSpan timeLimit)
        {
            if (m_ItemsMissingParents.Count == 0)
                return false;

            m_MissingParentTimer.Restart();
            for (var i = m_ItemsMissingParents.Count - 1; i >= 0; --i)
            {
                var searchItem = m_ItemsMissingParents[i];
                var parentDescriptor = searchItem.GetParentDescriptor(Context);
                if (m_SearchItemHierarchyNodeMap.TryGetNode(parentDescriptor.Id, out var parentNode) &&
                    m_SearchItemHierarchyNodeMap.TryGetNode(searchItem, out var node))
                {
                    TrySetParent(searchItem, parentDescriptor, in node, in parentNode);
                    m_ItemsMissingParents.RemoveAt(i);
                    RestartSorting();
                }

                if (m_MissingParentTimer.Elapsed >= timeLimit)
                    break;
            }

            return m_ItemsMissingParents.Count > 0;
        }

        bool DoUpdate(UpdateMode updateMode, TimeSpan timeLimit)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateMissingParentsByMode(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (mode)
                {
                    case UpdateMode.Update:
                        return UpdateItemsMissingParents();
                    case UpdateMode.UpdateIncremental:
                        return UpdateItemsMissingParentsIncremental();
                    case UpdateMode.UpdateIncrementalTimed:
                        return UpdateItemsMissingParentsIncrementalTimed(timeLimit);
                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateSortingByMode(UpdateMode mode, TimeSpan timeLimit)
            {
                if (!m_CanSort)
                    return false;

                switch (mode)
                {
                    case UpdateMode.Update:
                        m_HierarchySorting.Update();
                        return false;
                    case UpdateMode.UpdateIncremental:
                        return m_HierarchySorting.UpdateIncremental();
                    case UpdateMode.UpdateIncrementalTimed:
                        return m_HierarchySorting.UpdateIncrementalTimed(timeLimit);
                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool DoUpdateStage(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (m_CurrentUpdateStage)
                {
                    case UpdateStage.MissingParents:
                        return UpdateMissingParentsByMode(mode, timeLimit);
                    case UpdateStage.Sorting:
                        return UpdateSortingByMode(mode, timeLimit);
                    default:
                        throw new NotImplementedException(m_CurrentUpdateStage.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IncrementUpdateStage()
            {
                m_CurrentUpdateStage = (UpdateStage)(((int)m_CurrentUpdateStage + 1) % ((int)UpdateStage.Count));
            }

            // Execute the current stage
            var callAgain = DoUpdateStage(updateMode, timeLimit);

            if (!callAgain)
                IncrementUpdateStage();

            return callAgain || UpdateNeeded;
        }

        void BuildTokenSeparatedParentHierarchyFromSearchItem(SearchItem searchItem, in HierarchyNode node)
        {
            if (m_ViewModel == null || Context == null)
                return;

            var parentDescriptor = searchItem.GetParentDescriptor(Context);
            if (parentDescriptor.Type != SearchItemParentType.TokenSeparatedId)
                return;

            var parentProvider = GetOrCreateTokenSeparatedParentProvider(searchItem.provider);
            using var _ = ListPool<StringView>.Get(out var substrings);
            searchItem.GetParentsTokenSeparatedIds(Context, substrings);

            // Iterate through each split to find or create parent nodes.
            // Do it in reverse order to go from leaf to root.
            var currentNode = node;
            for (var i = substrings.Count - 1; i >= 0; --i)
            {
                var parentLabel = substrings[i];
                // The id of the parent node is the substring of the leaf parent's complete id from the start to the end of the current label.
                var parentId = new StringView(parentLabel.baseString, 0, parentLabel.endIndex);
                var grandParentId = i > 0 ? new StringView(substrings[i - 1].baseString, 0, substrings[i - 1].endIndex) : StringView.nil;
                var parentNode = GetOrCreateParentNodeFromDescriptor(parentProvider, parentId, parentLabel, grandParentId, in currentNode);

                // Move up the hierarchy
                currentNode = parentNode;
            }
        }

        HierarchyNode GetOrCreateParentNodeFromDescriptor(SearchProvider parentProvider, StringView parentId, StringView parentLabel, StringView grandParentId, in HierarchyNode childNode)
        {
            if (!m_SearchItemHierarchyNodeMap.TryGetNode(parentId, out var parentNode))
            {
                CommandList.Add(Hierarchy.Root, out parentNode);
                var parentSearchItem = parentProvider.CreateItem(Context, parentId.ToString(), parentLabel.ToString(), null, null, null);
                if (grandParentId.valid)
                {
                    parentSearchItem.SetParentDescriptor(new SearchItemParentDescriptor(grandParentId.ToString(), SearchItemParentType.TokenSeparatedId));
                }
                m_SearchItemHierarchyNodeMap.Add(parentSearchItem, in parentNode);
            }

            CommandList.SetParent(in childNode, in parentNode);
            return parentNode;
        }

        SearchProvider GetOrCreateTokenSeparatedParentProvider(SearchProvider childProvider)
        {
            if (!m_TokenSeparatedParentProviders.TryGetValue(childProvider, out var tokenSeparatedProvider))
            {
                tokenSeparatedProvider = new SearchProvider(childProvider + k_TokenSeparatedParentProviderSuffix, childProvider.name + " (Token Separated Parent)")
                {
                    priority = childProvider.priority,
                    fetchLabel = (item, context) => item.label ?? item.id,
                    fetchThumbnail = (item, context) => item.thumbnail,
                };
                m_TokenSeparatedParentProviders[childProvider] = tokenSeparatedProvider;
            }
            return tokenSeparatedProvider;
        }

        void RestartSorting()
        {
            m_HierarchySorting.Reset();

            // If we were doing sorting, go back to the missing parents stage.
            m_CurrentUpdateStage = UpdateStage.MissingParents;
        }

        static void OnFavoriteButtonClicked(SearchViewItemButtonWithContext button, SearchItem searchItem)
        {
            if (button == null || searchItem == null)
                return;

            if (SearchSettings.searchItemFavorites.Contains(searchItem.id))
                SearchSettings.RemoveItemFavorite(searchItem);
            else
                SearchSettings.AddItemFavorite(searchItem);
            UpdateFavoriteImage(button, searchItem);
        }

        static void UpdateFavoriteImage(SearchViewItemButtonWithContext button, SearchItem searchItem)
        {
            if (button == null || searchItem == null)
                return;

            if (SearchSettings.searchItemFavorites.Contains(searchItem.id))
            {
                button.tooltip = SearchViewItem.searchFavoriteOnButtonTooltip;
                button.SetActivePseudoState(true);
            }
            else
            {
                button.tooltip = SearchViewItem.searchFavoriteButtonTooltip;
                button.SetActivePseudoState(false);
            }
        }

        void OnActionDropdownClicked(SearchViewItemButtonWithContext button, SearchItem searchItem)
        {
            if (button == null || searchItem == null)
                return;
            m_ViewModel.ShowItemContextualMenu(searchItem, default);
        }

        bool IsParentCycleDetected(SearchItem firstItem, SearchItemParentDescriptor parentDescriptor)
        {
            using var _ = ListPool<SearchItem>.Get(out var visitedItems);
            visitedItems.Add(firstItem);
            while (true)
            {
                // We only support checking for cycles with parents that are other SearchItems.
                // Besides, it is unlikely that there would be a cycle with a token separated
                // parent since they are generated based on the id of the child SearchItem and not shared between items.
                if (parentDescriptor.Type != SearchItemParentType.SearchItemId)
                {
                    SetSearchItemsAsVisited(visitedItems);
                    return false;
                }

                // If there is no parent, we have reached the root and no cycle was detected.
                if (string.IsNullOrEmpty(parentDescriptor.Id))
                {
                    SetSearchItemsAsVisited(visitedItems);
                    return false;
                }

                // If there is no mapping for the parent, we might get it later, so bail out for now.
                // Do not mark the visited items as completely visited, since the parenting is not fully complete.
                if (!m_SearchItemHierarchyNodeMap.TryGetNode(parentDescriptor.Id, out var parentNode))
                    return false;
                if (parentNode == HierarchyNode.Null)
                    return false;
                if (!m_SearchItemHierarchyNodeMap.TryGetSearchItem(in parentNode, out var parentItem))
                    return false;

                visitedItems.Add(parentItem);

                // If the parent item is the same as the first item, we have a cycle.
                if (parentItem.Equals(firstItem))
                {
                    SetSearchItemsAsVisited(visitedItems);
                    return true;
                }

                // If we know that the parent item has already been visited during a previous cycle check,
                // it means that it doesn't have a cycle and we can stop checking further up the hierarchy.
                if (m_VisitedForCycleSet.Contains(parentItem))
                {
                    SetSearchItemsAsVisited(visitedItems);
                    return false;
                }

                parentDescriptor = parentItem.GetParentDescriptor(Context);
            }
        }

        void SetSearchItemsAsVisited(List<SearchItem> items)
        {
            foreach (var searchItem in items)
            {
                m_VisitedForCycleSet.Add(searchItem);
            }
        }

        void TrySetParent(SearchItem item, SearchItemParentDescriptor parentDescriptor, in HierarchyNode node, in HierarchyNode parentNode)
        {
            if (IsParentCycleDetected(item, parentDescriptor))
            {
                UnityEngine.Debug.LogError(string.Format(k_ParentCycleErrorFormat, item));
                return;
            }

            CommandList.SetParent(in node, in parentNode);
        }
    }
}
