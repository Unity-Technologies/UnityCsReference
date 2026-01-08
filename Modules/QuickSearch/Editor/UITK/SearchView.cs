// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEngine.Pool;
using UnityEngine.Search;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Search
{
    class SearchView : VisualElement, ISearchView, ISearchElement
    {
        const int k_ResetSelectionIndex = -1;
        internal const double resultViewUpdateThrottleDelay = 0.05d;

        private EntityId m_ViewId;
        private bool m_Disposed = false;
        private SearchViewState m_ViewState;
        private IResultView m_ResultView;
        internal IResultView resultView => m_ResultView;
        protected GroupedSearchList m_FilteredItems;
        private SearchSelection m_SearchItemSelection;
        private readonly List<int> m_Selection = new List<int>();
        private int m_DelayedCurrentSelection = k_ResetSelectionIndex;
        private bool m_SyncSearch;
        private SearchPreviewManager m_PreviewManager;
        private int m_TextureCacheSize;
        IEnumerator<SearchItem> m_FetchRequestEnumerator;

        // UITK
        private VisualElement m_ResultViewContainer;

        public SearchViewState state => m_ViewState;
        public float itemSize { get => viewState.itemSize; set => SetItemSize(value); }
        public SearchViewState viewState => m_ViewState;
        public Rect position => worldBound;
        public ISearchList results => m_FilteredItems;
        public SearchContext context => m_ViewState.context;

        public DisplayMode displayMode => GetDisplayMode();
        float ISearchView.itemIconSize { get => itemSize; set => itemSize = value; }
        Action<SearchItem, bool> ISearchView.selectCallback => m_ViewState.selectHandler;
        Func<SearchItem, bool> ISearchView.filterCallback => m_ViewState.filterHandler;
        public Action<SearchItem> trackingCallback => m_ViewState.trackingHandler;

        public bool multiselect
        {
            get => m_ViewState.context.options.HasAny(SearchFlags.Multiselect);
            set
            {
                if (value)
                    m_ViewState.context.options |= SearchFlags.Multiselect;
                else
                    m_ViewState.context.options &= ~SearchFlags.Multiselect;
            }
        }

        public SearchSelection selection
        {
            get
            {
                if (m_SearchItemSelection == null)
                    m_SearchItemSelection = new SearchSelection(m_Selection, m_FilteredItems);
                return m_SearchItemSelection;
            }
        }

        public string currentGroup
        {
            get => m_FilteredItems.currentGroup;
            set
            {
                var prevGroup = currentGroup;
                viewState.groupChanged?.Invoke(context, value, currentGroup);

                var tempSelection = ArrayPool<int>.Shared.Rent(m_Selection.Count);
                using var _ = ListPool<SearchItem>.Get(out var tempSelectedItems);
                for (var i = 0; i < m_Selection.Count; ++i)
                {
                    tempSelectedItems.Add(m_FilteredItems[m_Selection[i]]);
                }

                m_FilteredItems.currentGroup = value;
                viewState.group = m_FilteredItems.currentGroup;

                resultView?.OnGroupChanged(prevGroup, value);

                if (m_SyncSearch && value != null)
                    NotifySyncSearch(currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.SyncSearch);

                RefreshContent(RefreshFlags.GroupChanged);

                var actualCount = 0;
                for (var i  = 0; i < tempSelectedItems.Count; ++i)
                {
                    var selectedItem = tempSelectedItems[i];
                    var newIndex = m_FilteredItems.IndexOf(selectedItem);
                    if (newIndex >= 0)
                    {
                        tempSelection[actualCount] = newIndex;
                        actualCount++;
                    }
                }
                SetSelection(true, tempSelection.AsSpan(0, actualCount), true);
                ArrayPool<int>.Shared.Return(tempSelection);
            }
        }

        SearchPreviewManager ISearchView.previewManager => previewManager;
        internal SearchPreviewManager previewManager => m_PreviewManager;

        bool ISearchView.syncSearch { get => syncSearch; set => syncSearch = value; }
        internal bool syncSearch
        {
            get => m_SyncSearch;
            set
            {
                if (value == m_SyncSearch)
                    return;

                m_SyncSearch = value;
                if (value)
                    NotifySyncSearch(currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.StartSession);
                else
                    NotifySyncSearch(currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.EndSession);
            }
        }

        public bool hideHelpers { get; set; }

        int ISearchView.totalCount => totalCount;
        public int totalCount => m_FilteredItems.TotalCount;

        public IEnumerable<SearchItem> items => m_FilteredItems;
        public bool searchInProgress => context.searchInProgress;

        public SearchView(SearchViewState viewState, EntityId viewId)
        {
            using (new EditorPerformanceTracker("SearchView.ctor"))
            {
                m_ViewId = viewId;
                m_ViewState = viewState;
                m_PreviewManager = new SearchPreviewManager();

                context.searchView = context.searchView ?? this;
                m_FilteredItems = new GroupedSearchList(context, GetDefaultSearchListComparer());
                m_FilteredItems.currentGroup = viewState.group;
                viewState.itemSize = viewState.itemSize == 0 ? GetDefaultItemSize() : viewState.itemSize;
                hideHelpers = m_ViewState.HasFlag(SearchViewFlags.DisableQueryHelpers);
                style.flexGrow = 1f;
                UpdateView();

                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        public void Reset()
        {
            if (context == (m_FilteredItems?.context))
            {
                m_FilteredItems.Clear();
            }
            else
            {
                m_FilteredItems?.Dispose();
                m_FilteredItems = new GroupedSearchList(context, GetDefaultSearchListComparer());
                m_FilteredItems.currentGroup = viewState.group;
                m_ResultView?.OnItemSourceChanged(m_FilteredItems);
            }
            ClearSelection();
        }

        private ISearchListComparer GetDefaultSearchListComparer()
        {
            if (context?.searchView?.IsPicker() ?? false)
                return new SortByNameComparer();
            return null;
        }

        public void Refresh(RefreshFlags reason = RefreshFlags.Default)
        {
            // TODO FetchItemProperties (DOTSE-1994): remove this case and always refresh.
            if (reason == RefreshFlags.DisplayModeChanged)
            {
                RefreshContent(reason);
            }
            else
            {
                FetchItems();
            }
        }

        public void FetchItems()
        {
            using var tracker = new EditorPerformanceTracker("SearchView.FetchItems");

            if (m_SyncSearch)
                NotifySyncSearch(currentGroup, UnityEditor.SearchService.SearchService.SyncSearchEvent.SyncSearch);

            context.ClearErrors();
            m_FilteredItems.Clear();
            m_FetchRequestEnumerator?.Dispose();
            ClearSelection();

            var hostWindow = this.GetSearchHostWindow();
            if (hostWindow != null)
                OnIncomingItems(context, hostWindow.FetchItems());

            if (context.options.HasAny(SearchFlags.Debug))
                Debug.Log($"[{context.sessionId}] Running query {context.searchText}");
            RefreshContent(RefreshFlags.QueryStarted, false);
            m_FetchRequestEnumerator = SearchService.RequestEnumerator(context);
        }

        public void Update()
        {
            if (m_FetchRequestEnumerator == null)
                return;

            using var _ = ListPool<SearchItem>.Get(out var tempItems);
            while (m_FetchRequestEnumerator.MoveNext())
            {
                if (m_FetchRequestEnumerator.Current != null)
                    tempItems.Add(m_FetchRequestEnumerator.Current);
            }

            AddToFilteredItems(tempItems);
            m_FilteredItems.SortAllGroups();
            RefreshContent(RefreshFlags.ItemsChanged);

            m_FetchRequestEnumerator.Dispose();
            m_FetchRequestEnumerator = null;
            OnQueryRequestFinished(context);
        }

        public bool UpdateIncremental()
        {
            if (m_FetchRequestEnumerator == null)
                return false;

            if (m_FetchRequestEnumerator.MoveNext())
            {
                var current = m_FetchRequestEnumerator.Current;
                if (current != null)
                {
                    if (OnIncomingItem_NoRefresh(context, current))
                        RefreshContent(RefreshFlags.ItemsChanged);
                }
                return true;
            }

            m_FilteredItems.SortAllGroups();
            RefreshContent(RefreshFlags.ItemsChanged);

            m_FetchRequestEnumerator.Dispose();
            m_FetchRequestEnumerator = null;
            OnQueryRequestFinished(context);
            return false;
        }

        public bool UpdateIncrementalTimed(TimeSpan timeLimit)
        {
            if (m_FetchRequestEnumerator == null)
                return false;

            var timeStamp = Stopwatch.GetTimestamp();
            var needsRefresh = false;
            while (m_FetchRequestEnumerator.MoveNext())
            {
                if (m_FetchRequestEnumerator.Current != null)
                    needsRefresh |= OnIncomingItem_NoRefresh(context, m_FetchRequestEnumerator.Current);
                var elapsed = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - timeStamp);
                if (elapsed > timeLimit)
                {
                    if (needsRefresh)
                        RefreshContent(RefreshFlags.ItemsChanged);
                    return true;
                }
            }

            m_FilteredItems.SortAllGroups();
            RefreshContent(RefreshFlags.ItemsChanged);

            m_FetchRequestEnumerator.Dispose();
            m_FetchRequestEnumerator = null;
            OnQueryRequestFinished(context);
            return false;
        }

        public override string ToString() => context.searchText;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                AssetPreview.DeletePreviewTextureManagerByID(m_ViewId);
                m_ViewState.context?.Dispose();
                m_ResultView?.Dispose();
            }

            m_Disposed = true;
        }

        EntityId ISearchView.GetViewId()
        {
            return m_ViewId;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdatePreviewManagerCacheSize();
        }

        internal void UpdateViewAndEmitDisplayModeChange()
        {
            if (!UpdateView())
            {
                // Still report item size changes even if the view didn't change
                EmitDisplayModeChanged();
            }
        }

        private void SetItemSize(float value)
        {
            if (viewState.itemSize == value)
                return;

            viewState.itemSize = value;
            UpdateViewAndEmitDisplayModeChange();
        }

        private void EmitDisplayModeChanged()
        {
            m_ResultView?.Refresh(RefreshFlags.DisplayModeChanged);
            Dispatcher.Emit(SearchEvent.DisplayModeChanged, new SearchEventPayload(this));
        }

        private bool UpdateView()
        {
            using (new EditorPerformanceTracker("SearchView.UpdateView"))
            {
                IResultView nextView = null;
                if (results.Count == 0 && displayMode != DisplayMode.Table)
                {
                    if (!m_ResultView?.showNoResultMessage ?? false)
                        return false;

                    if (m_ResultView is not SearchEmptyView)
                        nextView = new SearchEmptyView(this, viewState.flags);
                }
                else
                {
                    if (itemSize <= 32f)
                    {
                        if (!(m_ResultView is SearchListView))
                            nextView = new SearchListView(this);
                    }
                    else if (itemSize >= (float)DisplayMode.Table)
                    {
                        if (!(m_ResultView is SearchTableView))
                            nextView = new SearchTableView(this);
                    }
                    else
                    {
                        if (!(m_ResultView is SearchGridView))
                            nextView = new SearchGridView(this);
                    }
                }

                if (nextView == null)
                    return false;

                m_ResultView = nextView;
                UpdatePreviewManagerCacheSize();

                if (m_ResultViewContainer != null)
                    m_ResultViewContainer.RemoveFromHierarchy();

                m_ResultViewContainer = m_ResultView as VisualElement;
                if (m_ResultViewContainer == null)
                    throw new NotSupportedException("Result view must be implemented using UTIK");

                m_ResultViewContainer.style.flexGrow = 1f;
                Add(m_ResultViewContainer);

                EmitDisplayModeChanged();
                return true;
            }
        }

        private void UpdatePreviewManagerCacheSize()
        {
            var width = worldBound.width;
            var height = worldBound.height;
            if (width <= 0 || float.IsNaN(width) || height <= 0 || float.IsNaN(height))
                return;

            // Note: We approximate how many items could be displayed in the current Rect. We cannot rely on the ResultView to have
            // an exact list of visibleItems since get updated AFTER our resize handler and we need to update the Cache size so preview are properly generated.

            var potentialVisibleItems = Mathf.Min(m_ResultView.ComputeVisibleItemCapacity(width, height), results.Count);
            var newTextureCacheSize = Mathf.Max(potentialVisibleItems * 2 + 30, 128);
            if (potentialVisibleItems == 0 || newTextureCacheSize <= m_TextureCacheSize)
                return;

            m_PreviewManager.poolSize = newTextureCacheSize;
            m_TextureCacheSize = newTextureCacheSize;
            AssetPreview.SetPreviewTextureCacheSize(m_TextureCacheSize, ((ISearchView)this).GetViewId());
        }

        private DisplayMode GetDisplayMode()
        {
            if (itemSize <= (float)DisplayMode.Compact)
                return DisplayMode.Compact;
            if (itemSize <= (float)DisplayMode.List)
                return DisplayMode.List;
            if (itemSize >= (float)DisplayMode.Table)
                return DisplayMode.Table;
            return DisplayMode.Grid;
        }

        private float GetDefaultItemSize()
        {
            if (viewState.flags.HasAny(SearchViewFlags.CompactView))
                return 1f;

            if (viewState.flags.HasAny(SearchViewFlags.GridView))
                return (float)DisplayMode.Grid;

            if (viewState.flags.HasAny(SearchViewFlags.TableView))
                return (float)DisplayMode.Table;

            return viewState.itemSize;
        }

        public void RefreshContent(RefreshFlags flags, bool updateView = true)
        {
            using (new EditorPerformanceTracker("SearchView.RefreshContent"))
            {
                if (updateView)
                {
                    UpdateView();
                    m_ResultView?.Refresh(flags);
                }

                if (context.debug)
                    Debug.Log($"[{searchInProgress}] Refresh {flags} for query \"{context.searchText}\": {m_ResultView}");

                Dispatcher.Emit(SearchEvent.RefreshContent, new SearchEventPayload(this, flags, context.searchText));
            }
        }

        private void AddToFilteredItems(IEnumerable<SearchItem> items)
        {
            if (m_ViewState.filterHandler != null)
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                items = items.Where(item => m_ViewState.filterHandler(item));
#pragma warning restore RS0030
            }
            m_FilteredItems.AddItems(items);
        }

        private void OnIncomingItems(SearchContext context, IEnumerable<SearchItem> items)
        {
            var countBefore = m_FilteredItems.TotalCount;
            AddToFilteredItems(items);
            if (m_FilteredItems.TotalCount != countBefore)
                RefreshContent(RefreshFlags.ItemsChanged);
        }

        bool OnIncomingItem_NoRefresh(SearchContext context, SearchItem item)
        {
            var countBefore = m_FilteredItems.TotalCount;
            if (m_ViewState.filterHandler != null && !m_ViewState.filterHandler(item))
                return false;
            m_FilteredItems.AddItem(item);
            return m_FilteredItems.TotalCount != countBefore;
        }

        private void OnQueryRequestFinished(SearchContext context)
        {
            UpdateSelectionFromIds();
            Utils.CallDelayed(() => RefreshContent(RefreshFlags.QueryCompleted));
        }

        private void UpdateSelectionFromIds()
        {
            var selectionSynced = selection.SyncSelectionIfInvalid();
            if (viewState.selectedIds.Length == 0 || selection.Count != 0)
            {
                if (selectionSynced)
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    SetSelection(trackSelection: false, selection.indexes.ToArray());
#pragma warning restore RS0030
                return;
            }

            var indexesToSelect = new List<int>(viewState.selectedIds.Length);
            for (int index = 0; index < results.Count; index++)
            {
                var item = results[index];
                if (Array.IndexOf(viewState.selectedIds, item.GetEntityId()) != -1)
                {
                    indexesToSelect.Add(index);
                    if (indexesToSelect.Count == viewState.selectedIds.Length)
                        break;
                }
            }

            if (indexesToSelect.Count > 0)
                SetSelection(trackSelection: false, indexesToSelect.ToArray());
        }

        public void SetSelection(params int[] selection)
        {
            SetSelection(true, selection);
        }

        private bool IsItemValid(int index)
        {
            if (index < 0 || index >= m_FilteredItems.Count)
                return false;
            return true;
        }

        private void SetSelection(bool trackSelection, ReadOnlySpan<int> newSelection, bool forceChange = false)
        {
            if (!multiselect && newSelection.Length > 1)
                newSelection = newSelection.Slice(newSelection.Length - 1, 1);

            var selectedIds = new List<EntityId>();
            var lastIndexAdded = k_ResetSelectionIndex;

            m_Selection.Clear();
            m_SearchItemSelection = null;
            foreach (var idx in newSelection)
            {
                if (!IsItemValid(idx))
                    continue;

                selectedIds.Add(m_FilteredItems[idx].GetEntityId());
                m_Selection.Add(idx);
                lastIndexAdded = idx;
            }

            if (lastIndexAdded != k_ResetSelectionIndex || forceChange)
            {
                m_SearchItemSelection = null;
                viewState.selectedIds = selectedIds.ToArray();
                if (trackSelection)
                    TrackSelection(lastIndexAdded);

                Dispatcher.Emit(SearchEvent.SelectionHasChanged, new SearchEventPayload(this));
            }
        }

        private void TrackSelection(int currentSelection)
        {
            if (!SearchSettings.trackSelection)
                return;

            m_DelayedCurrentSelection = currentSelection;
            EditorApplication.delayCall -= DelayTrackSelection;
            EditorApplication.delayCall += DelayTrackSelection;
        }

        public void SetItems(IEnumerable<SearchItem> newItems)
        {
            var tempSelection = ArrayPool<int>.Shared.Rent(m_Selection.Count);
            using var _ = ListPool<SearchItem>.Get(out var tempSelectedItems);
            for (var i = 0; i < m_Selection.Count; ++i)
            {
                tempSelectedItems.Add(m_FilteredItems[m_Selection[i]]);
            }

            m_SearchItemSelection = null;
            m_FilteredItems.Clear();
            m_FilteredItems.AddItems(newItems);
            if (!string.IsNullOrEmpty(context.filterId))
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                m_FilteredItems.AddGroup(context.providers.First());
#pragma warning restore RS0030

            var actualCount = 0;
            for (var i = 0; i < tempSelectedItems.Count; ++i)
            {
                var selectedItem = tempSelectedItems[i];
                var newIndex = m_FilteredItems.IndexOf(selectedItem);
                if (newIndex >= 0)
                {
                    tempSelection[actualCount] = newIndex;
                    actualCount++;
                }
            }
            SetSelection(trackSelection: false, tempSelection.AsSpan(0, actualCount));
            ArrayPool<int>.Shared.Return(tempSelection);
        }

        internal void DelayTrackSelection()
        {
            if (m_FilteredItems.Count == 0)
                return;

            if (!IsItemValid(m_DelayedCurrentSelection))
                return;

            var selectedItem = m_FilteredItems[m_DelayedCurrentSelection];
            if (trackingCallback == null)
                selectedItem?.provider?.trackSelection?.Invoke(selectedItem, context);
            else
                trackingCallback(selectedItem);

            m_DelayedCurrentSelection = k_ResetSelectionIndex;
        }

        public void ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition)
        {
            if (IsPicker())
                return;

            SearchAnalytics.SendEvent(viewState.sessionId, SearchAnalytics.GenericEventType.QuickSearchShowActionMenu, item.provider.id);
            var menu = new GenericMenu();
            var shortcutIndex = 0;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var useSelection = context?.selection?.Any(e => string.Equals(e.id, item.id, StringComparison.OrdinalIgnoreCase)) ?? false;
#pragma warning restore RS0030
            var currentSelection = useSelection ? context.selection : new SearchSelection(new[] { item });
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var action in item.provider.actions.Where(a => a.enabled?.Invoke(currentSelection) ?? true))
#pragma warning restore RS0030
            {
                var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                if (shortcutIndex == 0)
                    itemName += " _enter";
                else if (shortcutIndex == 1)
                    itemName += " _&enter";

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                menu.AddItem(new GUIContent(itemName, action.content.image), false, () => ExecuteAction(action, currentSelection.ToArray()));
#pragma warning restore RS0030
                ++shortcutIndex;
            }

            menu.AddSeparator("");
            if (SearchSettings.searchItemFavorites.Contains(item.id))
                menu.AddItem(new GUIContent("Remove from Favorites"), false, () => SearchSettings.RemoveItemFavorite(item));
            else
                menu.AddItem(new GUIContent("Add to Favorites"), false, () => SearchSettings.AddItemFavorite(item));

            if (contextualActionPosition == default)
                menu.ShowAsContext();
            else
              menu.DropDown(contextualActionPosition);
        }

        internal static SearchAction GetSelectAction(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var provider = (items ?? selection).First().provider;
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectAction = provider.actions.FirstOrDefault(a => string.Equals(a.id, "select", StringComparison.Ordinal));
#pragma warning restore RS0030
            if (selectAction == null)
            {
                selectAction = GetDefaultAction(selection, items);
            }
            return selectAction;
        }

        internal static SearchAction GetDefaultAction(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var provider = (items ?? selection).First().provider;
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return provider.actions.FirstOrDefault();
#pragma warning restore RS0030
        }

        internal static SearchAction GetSecondaryAction(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var provider = (items ?? selection).First().provider;
#pragma warning restore RS0030
            return provider.actions.Count > 1 ? provider.actions[1] : GetDefaultAction(selection, items);
        }

        void ISearchView.ExecuteSelection()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            ExecuteAction(GetDefaultAction(selection, selection), selection.ToArray(), endSearch: false);
#pragma warning restore RS0030
        }

        public void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = false)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var item = items.LastOrDefault();
#pragma warning restore RS0030
            if (item == null)
                return;

            if (m_ViewState.selectHandler != null && items.Length > 0)
            {
                m_ViewState.selectHandler(items[0], false);
                m_ViewState.selectHandler = null;
                if (IsPicker())
                    endSearch = true;
            }
            else
            {
                if (action == null)
                    action = GetDefaultAction(selection, items);

                SendSearchEvent(item, action);
                if (endSearch)
                    EditorApplication.delayCall -= DelayTrackSelection;

                if (action?.handler != null && items.Length == 1)
                    action.handler(item);
                else if (action?.execute != null)
                    action.execute(items);
                else
                    action?.handler?.Invoke(item);
            }

            var searchWindow = this.GetHostWindow() as SearchWindow;
            if (searchWindow != null && endSearch && (action?.closeWindowAfterExecution ?? true) && !searchWindow.context.options.HasFlag(SearchFlags.Dockable))
                searchWindow.CloseSearchWindow();
        }

        private void SendSearchEvent(SearchItem item, SearchAction action = null)
        {
            var evt = new SearchAnalytics.SearchEvent();
            if (item != null)
                evt.Success(item, action);

            if (evt.success)
            {
                evt.Done();
            }
            evt.searchText = context.searchText;
            evt.useQueryBuilder = viewState.queryBuilderEnabled;
            SearchAnalytics.SendSearchEvent(evt, context);
        }

        public void SetSearchText(string searchText, TextCursorPlacement moveCursor)
        {
            if (string.Equals(context.searchText, searchText, StringComparison.Ordinal))
                return;
            var isEquivalent = string.Equals(context.searchText.Trim(), searchText.Trim(), StringComparison.Ordinal);
            context.searchText = searchText;

            // Don't trigger a refresh if the text is equivalent (i.e. only added new trailing spaces)
            if (!isEquivalent)
                Refresh(RefreshFlags.ItemsChanged);
        }

        void ISearchView.SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            throw new NotSupportedException("Cursor cannot be set for this control.");
        }

        public void AddSelection(params int[] selection)
        {
            if (!multiselect && m_Selection.Count == 1)
                throw new Exception("Multi selection is not allowed.");

            foreach (var idx in selection)
            {
                if (!IsItemValid(idx))
                    continue;

                if (m_Selection.Contains(idx))
                {
                    m_Selection.Remove(idx);
                }
                else
                {
                    m_Selection.Add(idx);
                }
            }

            SetSelection(true, m_Selection.ToArray());
        }

        void ClearSelection()
        {
            m_Selection.Clear();
            m_SearchItemSelection = null;
        }

        void ISearchView.FocusSearch() => m_ResultViewContainer.Focus();
        void ISearchView.SelectSearch() => m_ResultViewContainer.Focus();

        void ISearchView.Repaint() => MarkDirtyRepaint();

        void ISearchView.Close() => throw new NotSupportedException("Cannot close search view element. Close the host window instead.");
        void ISearchView.SetColumns(IEnumerable<SearchColumn> columns) => throw new NotSupportedException();

        public int GetItemCount(IEnumerable<string> providerIds)
        {
            return m_FilteredItems.GetItemCount(providerIds);
        }

        public IEnumerable<IGroup> EnumerateGroups()
        {
            return EnumerateGroups(!viewState.hideAllGroup);
        }

        public IEnumerable<IGroup> EnumerateGroups(bool showAll)
        {
            var groups = m_FilteredItems.EnumerateGroups(showAll);
            if (showAll)
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                groups = groups.Where(g => !string.Equals(g.id, "default", StringComparison.Ordinal));
#pragma warning restore RS0030
            return groups;
        }

        public IGroup GetGroupById(string groupId)
        {
            return m_FilteredItems.GetGroupById(groupId);
        }

        public int IndexOf(SearchItem item)
        {
            return m_FilteredItems.IndexOf(item);
        }

        public bool Add(SearchItem item)
        {
            if (m_FilteredItems.Contains(item))
                return false;
            m_FilteredItems.Add(item);
            return true;
        }

        IEnumerable<SearchQueryError> ISearchView.GetAllVisibleErrors() => GetAllVisibleErrors();
        internal IEnumerable<SearchQueryError> GetAllVisibleErrors()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var visibleProviders = EnumerateGroups().Select(g => g.id).ToArray();
#pragma warning restore RS0030
            var defaultProvider = SearchService.GetDefaultProvider();
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return context.GetAllErrors().Where(e => visibleProviders.Contains(e.provider.type) || e.provider.type == defaultProvider.type);
#pragma warning restore RS0030
        }

        public bool IsPicker()
        {
            var window = this.GetSearchHostWindow();
            return window != null && window.IsPicker();
        }

        IEnumerable<IGroup> ISearchView.EnumerateGroups()
        {
            return EnumerateGroups();
        }

        private void NotifySyncSearch(string groupId, UnityEditor.SearchService.SearchService.SyncSearchEvent evt)
        {
            var syncViewId = groupId;
            switch (groupId)
            {
                case "asset":
                    syncViewId = typeof(ProjectSearchEngine).FullName;
                    break;
                case "scene":
                    syncViewId = typeof(SceneSearchEngine).FullName;
                    break;
            }
            UnityEditor.SearchService.SearchService.NotifySyncSearchChanged(evt, syncViewId, context.searchQuery);
        }

        public void SetupColumns(IList<SearchField> fields)
        {
            if (m_ResultView is SearchTableView tableView)
                tableView.SetupColumns(fields);
        }

        void ISearchView.SetupColumns(IList<SearchField> fields)
        {
            SetupColumns(fields);
        }
    }
}
