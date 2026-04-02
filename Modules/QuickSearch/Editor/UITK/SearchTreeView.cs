// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [VisibleToOtherModules]
    sealed class SearchTreeView : SearchElement, IResultView
    {
        enum UpdateMode
        {
            Update,
            UpdateIncremental,
            UpdateIncrementalTimed
        }

        enum UpdateStage
        {
            Handler,
            Hierarchy,
            Count,
        }

        readonly Unity.Hierarchy.Hierarchy m_Hierarchy;
        readonly HierarchyView m_HierarchyView;
        readonly HierarchySearchItemHandler m_SearchItemHandler;
        UpdateStage m_CurrentUpdateStage;
        readonly Stopwatch m_UpdateTimer = new();
        readonly SearchResultViewGlobalEventHandler m_GlobalEventHandler;
        readonly SearchResultViewDragHandler m_DragHandler;

        public static readonly string BaseViewUssClassName = "search-tree-view";
        public static readonly string SearchTreeViewItemUssClassName = "search-tree-view-item";
        public static readonly string SearchTreeViewRowClassName = "search-tree-view-row";
        public static readonly string SearchTreeViewItemButtonContainerClassName = SearchTreeViewItemUssClassName.WithUssElement("button-container");
        public static readonly string SearchTreeViewItemButtonContainerDisabledClassName = SearchTreeViewItemButtonContainerClassName.WithUssModifier("disabled");
        public static readonly string MoreActionButtonClassName = SearchTreeViewItemUssClassName.WithUssElement("more-action-button");

        internal const string ResultViewId = "SearchTreeView";
        public string ViewId => ResultViewId;
        bool IResultView.ShowNoResultMessage => true;
        public bool UpdateNeeded => m_SearchItemHandler.UpdateNeeded || m_HierarchyView.UpdateNeeded;
        public event IResultView.SelectionChangedEventHandler SelectionChanged;
        public event IResultView.PopulateItemsContextMenuHandler PopulateItemsContextMenu;

        public HierarchyView HierarchyView => m_HierarchyView;
        public HierarchySearchItemHandler SearchItemHandler => m_SearchItemHandler;

        public SearchTreeView(ISearchView viewModel)
            : base("SearchTreeView", viewModel, BaseViewUssClassName)
        {
            m_Hierarchy = new Unity.Hierarchy.Hierarchy();
            m_HierarchyView = new HierarchyView();
            m_HierarchyView.SetSourceHierarchy(m_Hierarchy, HierarchyNodeFlags.Expanded);
            m_HierarchyView.ListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            m_HierarchyView.ListView.selectionType = viewModel.multiselect ? SelectionType.Multiple : SelectionType.Single;

            m_SearchItemHandler = m_Hierarchy.GetOrCreateNodeTypeHandler<HierarchySearchItemHandler>();

            m_GlobalEventHandler = new SearchResultViewGlobalEventHandler(
                this,
                m_HierarchyView.ListView,
                null,
                GetCurrentIndex,
                GetItemCount,
                SetSelectedIndex,
                SelectionContains,
                AddToSelection,
                RemoveFromSelection,
                Frame,
                GetVisibleItemCount,
                null);

            m_DragHandler = new SearchResultViewDragHandler(m_ViewModel, this)
            {
                CanStartDrag = CanStartDrag,
                GetDraggedItem = GetDraggedItem,
                StartDrag = StartDrag,
            };

            // If there is no search in progress, it means we are creating the view after the search has completed
            // so we can directly allow sorting. Otherwise, we will allow sorting once the search is completed.
            if (!context.searchInProgress)
                m_SearchItemHandler.CanSort = true;
            m_SearchItemHandler.Setup(viewModel);

            Add(m_HierarchyView);
        }

        public void Dispose()
        {
            m_HierarchyView.Dispose();
            m_Hierarchy.Dispose();
        }

        public static SearchTreeView Create(ISearchView viewModel)
        {
            return new SearchTreeView(viewModel);
        }

        public static Texture2D FetchIcon()
        {
            return EditorGUIUtility.LoadIconRequired("UnityEditor.SceneHierarchyWindow");
        }

        public static SearchResultViewDescriptor GetDescriptor()
        {
            return new SearchResultViewDescriptor(ResultViewId, Create, FetchIcon,
                (float)DisplayMode.Table + 1,
                description: "Tree View",
                buttonClassName: "search-statusbar__tree-mode-button");
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            m_HierarchyView.FlagsChanged += OnHierarchyViewFlagsChanged;
            m_HierarchyView.PopulateContextMenu += OnHierarchyPopulateContextMenu;
            m_SearchItemHandler.ItemDoubleClicked += OnSearchItemDoubleClicked;
            m_GlobalEventHandler.RegisterGlobalEventHandlers();
            m_DragHandler.RegisterDragCallbacks();
            OnAll(SearchEvent.ItemFavoriteStateChanged, OnFavoriteStateChanged);

            this.RegisterFireAndForgetCallback<GeometryChangedEvent>(OnFirstGeometryChangedEvent);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_HierarchyView.FlagsChanged -= OnHierarchyViewFlagsChanged;
            m_HierarchyView.PopulateContextMenu -= OnHierarchyPopulateContextMenu;
            m_SearchItemHandler.ItemDoubleClicked -= OnSearchItemDoubleClicked;
            m_GlobalEventHandler.UnregisterGlobalEventHandler();
            m_DragHandler.UnregisterDragCallbacks();
            Off(SearchEvent.ItemFavoriteStateChanged, OnFavoriteStateChanged);

            base.OnDetachFromPanel(evt);
        }

        #region IResultView
        public void Refresh(RefreshFlags refreshFlags = RefreshFlags.Default)
        {
            if (refreshFlags.HasAny(RefreshFlags.ItemsChanged))
            {
                m_SearchItemHandler.IntegrateNewSearchItems();
            }
            else if (refreshFlags.HasAny(RefreshFlags.QueryStarted))
            {
                m_SearchItemHandler.CanSort = false;
                m_SearchItemHandler.RebuildHierarchy();
            }
            else if (refreshFlags.HasAny(RefreshFlags.QueryCompleted))
            {
                m_SearchItemHandler.CanSort = true;
            }
        }

        public void OnGroupChanged(string prevGroupId, string newGroupId)
        {
            m_SearchItemHandler.RebuildHierarchy();
        }

        public void OnItemSourceChanged(ISearchList itemSource)
        {
            m_SearchItemHandler.RebuildHierarchy();
        }

        public void AddSaveQueryMenuItems(SearchContext context, GenericMenu menu)
        {
            // Nothing to do
        }

        public void UpdateView()
        {
            // Don't do while(Update...){} because the SearchItemHandler could get stuck waiting for
            // missing parents.
            DoUpdate(UpdateMode.Update, TimeSpan.Zero);
        }

        public bool UpdateViewIncremental()
        {
            return DoUpdate(UpdateMode.UpdateIncremental, TimeSpan.Zero);
        }

        public bool UpdateViewIncrementalTimed(TimeSpan timeLimit)
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

        public void SetSearchItemComparer(IComparer<SearchItem> searchItemComparer)
        {
            m_SearchItemHandler.SetSearchItemComparer(searchItemComparer);
        }

        // Selection change coming from outside the view
        public void SetSelectionWithoutNotify(SearchSelection selection)
        {
            if (selection == null)
                return;

            // Updating the selection requires the entire hierarchy to be up to date, otherwise we might not find the corresponding nodes for the selected items
            m_SearchItemHandler.Update();
            m_HierarchyView.Update();

            m_HierarchyView.ViewModel.BeginFlagsChange();
            HierarchyNode firstSelection = HierarchyNode.Null;
            foreach (var selectedItem in selection)
            {
                if (m_SearchItemHandler.TryGetNode(selectedItem, out var node))
                {
                    m_HierarchyView.ViewModel.SetFlags(in node, HierarchyNodeFlags.Selected);
                    if (firstSelection == HierarchyNode.Null)
                        firstSelection = node;
                }
            }
            m_HierarchyView.ViewModel.EndFlagsChangeWithoutNotify();

            if (firstSelection != HierarchyNode.Null)
                m_HierarchyView.Frame(in firstSelection);
        }

        public int ComputeVisibleItemCapacity(float size, float height)
        {
            return (int)(height / GetItemHeight()) + 10;
        }
        #endregion

        void OnHierarchyViewFlagsChanged(HierarchyView view, HierarchyNodeFlags flags)
        {
            if ((flags & HierarchyNodeFlags.Selected) != 0)
            {
                var selectedCount = m_HierarchyView.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
                using var pooledArray = new RentSpanUnmanaged<int>(selectedCount);

                var index = 0;
                foreach (var node in m_HierarchyView.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
                {
                    if (m_SearchItemHandler.TryGetSearchItem(in node, out var searchItem))
                    {
                        var selectedIndex = m_ViewModel.results.IndexOf(searchItem);
                        if (selectedIndex != -1)
                            pooledArray.Span[index] = selectedIndex;
                        else
                            pooledArray.Span[index] = SearchSelection.InvalidIndex;
                    }
                    else
                    {
                        pooledArray.Span[index] = SearchSelection.InvalidIndex;
                    }

                    ++index;
                }
                SelectionChanged?.Invoke(pooledArray.Span);
            }
        }

        void OnHierarchyPopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
        {
            if (m_SearchItemHandler.TryGetSearchItem(in item.Node, out var searchItem) &&
                !m_SearchItemHandler.IsBuiltinParentSearchItem(searchItem))
            {
                PopulateItemsContextMenu?.Invoke(searchItem, menu);
            }
        }

        void OnSearchItemDoubleClicked(SearchItem searchItem)
        {
            // We care about the selected items, not only the item that is double-clicked.
            m_ViewModel.ExecuteAction(null, m_ViewModel.selection.ToArray(), true);
        }

        float GetItemHeight()
        {
            return m_HierarchyView.ListView.fixedItemHeight;
        }

        bool DoUpdate(UpdateMode updateMode, TimeSpan timeLimit)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UpdateHandlerByMode(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (mode)
                {
                    case UpdateMode.Update:
                        m_SearchItemHandler.Update();
                        break;
                    case UpdateMode.UpdateIncremental:
                        m_SearchItemHandler.UpdateIncremental();
                        break;
                    case UpdateMode.UpdateIncrementalTimed:
                        m_SearchItemHandler.UpdateIncrementalTimed(timeLimit);
                        break;
                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void UpdateHierarchyViewByMode(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (mode)
                {
                    case UpdateMode.Update:
                        m_HierarchyView.Update();
                        break;
                    case UpdateMode.UpdateIncremental:
                        m_HierarchyView.UpdateIncremental();
                        break;
                    case UpdateMode.UpdateIncrementalTimed:
                        m_HierarchyView.UpdateIncrementalTimed(timeLimit.TotalMilliseconds);
                        break;
                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void DoUpdateStage(UpdateMode mode, TimeSpan timeLimit)
            {
                switch (m_CurrentUpdateStage)
                {
                    case UpdateStage.Handler:
                        UpdateHandlerByMode(mode, timeLimit);
                        break;
                    case UpdateStage.Hierarchy:
                        UpdateHierarchyViewByMode(mode, timeLimit);
                        break;
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
            DoUpdateStage(updateMode, timeLimit);

            // Always increment the stage, otherwise we could wait a long time before the hierarchy gets updated.
            IncrementUpdateStage();

            return UpdateNeeded;
        }

        #region Global Event Handler
        int GetCurrentIndex()
        {
            return m_HierarchyView.ListView.selection.maxIndex;
        }

        int GetItemCount()
        {
            return m_HierarchyView.ViewModel.Count;
        }

        void SetSelectedIndex(int newSelectedIndex)
        {
            m_HierarchyView.ListView.selection.selectedIndex = newSelectedIndex;
        }

        bool SelectionContains(int index)
        {
            return m_HierarchyView.ListView.selection.ContainsIndex(index);
        }

        void AddToSelection(ReadOnlySpan<int> newSelection)
        {
            m_HierarchyView.ListView.selection.AddRange(newSelection);
        }

        void RemoveFromSelection(int index)
        {
            m_HierarchyView.ListView.selection.Remove(index);
        }

        void Frame(int index)
        {
            m_HierarchyView.ListView.ScrollToItem(index);
        }

        int GetVisibleItemCount()
        {
            return m_HierarchyView.ListView.m_DisplayedList.Count;
        }
        #endregion

        // The drag and drop mechanism from the HierarchyView is incompatible with the
        // drag API of the Search Providers. Therefore, we have to handle the drag
        // manually
        #region Drag Support

        bool CanStartDrag(PointerDownEvent evt)
        {
            var searchItem = GetDraggedItem(evt);
            return searchItem?.provider.startDrag != null;
        }

        SearchItem GetDraggedItem(PointerDownEvent evt)
        {
            var nodeIndex = m_HierarchyView.GetIndexFromWorldPosition(evt.position);
            if (nodeIndex < 0 || nodeIndex >= m_HierarchyView.ViewModel.Count)
                return null;

            var currentNode = m_HierarchyView.ViewModel[nodeIndex];
            return m_SearchItemHandler.TryGetSearchItem(in currentNode, out var searchItem) ? searchItem : null;
        }

        void StartDrag(SearchItem searchItem)
        {
            DragAndDrop.PrepareStartDrag();
            searchItem.provider.startDrag(searchItem, context);
        }

        #endregion

        void OnFavoriteStateChanged(ISearchEvent evt)
        {
            var id = string.Empty;
            if (evt.argumentCount == 1)
                id = (string)evt.GetArgument(0);
            else
                return;

            m_SearchItemHandler.UpdateSearchItemFavoriteState(id);
        }

        void OnFirstGeometryChangedEvent(GeometryChangedEvent evt)
        {
            // Update selection just in case it was already modified
            // Mostly to frame the selection properly
            SetSelectionWithoutNotify(m_ViewModel.selection);
        }
    }
}
