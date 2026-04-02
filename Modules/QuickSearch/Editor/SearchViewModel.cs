// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    // Implement most of the ISearchView without bounding a UI View
    [Serializable]
    class SearchViewModel : ISearchView
    {
        const int k_ResetSelectionIndex = -1;

        GroupedSearchList m_FilteredItems;
        SearchSelection m_SearchItemSelection;
        int m_DelayedCurrentSelection = k_ResetSelectionIndex;
        List<int> m_Selection;
        List<SearchItem> m_ItemsTempList = new();
        SearchViewState m_ViewState;

        #region ISearchView Properties
        public SearchSelection selection
        {
            get
            {
                if (m_SearchItemSelection == null)
                    m_SearchItemSelection = new SearchSelection(m_Selection, m_FilteredItems);
                return m_SearchItemSelection;
            }
        }

        public ISearchList results => m_FilteredItems;

        public SearchContext context => m_ViewState.context;

        public SearchViewState state
        {
            get => m_ViewState;
            set
            {
                m_ViewState = value;
                multiselect = m_ViewState.context?.options.HasAny(SearchFlags.Multiselect) ?? false;
                SetSelection(Array.Empty<int>());
            }
        }

        public string currentGroup
        {
            get => m_FilteredItems.currentGroup;
            set
            {
                m_ViewState.groupChanged?.Invoke(context, value, currentGroup);

                var selectedItems = SelectionToArray(selection);
                var newSelectedIndices = new int[selectedItems.Length];

                m_ViewState.group = value;
                m_FilteredItems.currentGroup = value;

                // By default no refresh of content. Is it ok to assume this?
                // RefreshContent(RefreshFlags.GroupChanged);

                for (var i = 0; i < selectedItems.Length; ++i)
                {
                    var selectedItem = selectedItems[i];
                    newSelectedIndices[i] = m_FilteredItems.IndexOf(selectedItem);
                }
                SetSelection(true, newSelectedIndices, true);
            }
        }
        public float itemIconSize
        {
            get => m_ViewState.itemIconSize;
            set => m_ViewState.itemIconSize = value;
        }

        public string currentResultViewId
        {
            get => m_ViewState.resultViewDescriptorList.CurrentViewId;
            set => m_ViewState.SetResultView(value);
        }

        public DisplayMode displayMode => SearchUtils.GetDisplayModeFromItemSize(m_ViewState.itemIconSize);

        public bool multiselect { get; set; }

        public Rect position { get; set; }

        public bool searchInProgress => m_ViewState.context.searchInProgress;

        // Call when an item is activated (double clicked on).
        public Action<SearchItem, bool> selectCallback
        {
            get => m_ViewState.selectHandler;
            set => m_ViewState.selectHandler = value;
        }

        // Called to further filter the items results.
        public Func<SearchItem, bool> filterCallback
        {
            get => m_ViewState.filterHandler;
            set => m_ViewState.filterHandler = value;
        }

        // Called when selection changed.
        public Action<SearchItem> trackingCallback
        {
            get => m_ViewState.trackingHandler;
            set => m_ViewState.trackingHandler = value;
        }

        public int totalCount => m_FilteredItems.TotalCount;

        public Action<GenericMenu, SearchItem> addToItemContextualMenu;
        public Action<SearchAction, SearchItem[], bool> executeAction;
        #endregion

        #region Internal properties
        bool ISearchView.syncSearch { get; set; }
        SearchPreviewManager ISearchView.previewManager => null;
        internal SearchResultViewDescriptorList resultViewDescriptorList => m_ViewState.resultViewDescriptorList;
        #endregion

        #region State Change Notification
        public event Action<SearchViewModel, string> queryChanged;
        #endregion

        public SearchViewModel(SearchViewState state)
        {
            m_ViewState = state;
            m_FilteredItems = new GroupedSearchList(context);
            m_FilteredItems.currentGroup = state.group;
            m_Selection = new();
            m_SearchItemSelection = null;
        }

        #region ISearchView Methods
        public virtual void AddSelection(params int[] selection)
        {
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

        public virtual void Dispose()
        {
            // Nothing to do;
        }

        public virtual void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = false)
        {
            if (executeAction != null)
            {
                executeAction(action, items, endSearch);
                return;
            }

            if (items == null || items.Length == 0)
                return;

            var item = items[^1];
            if (item == null)
                return;

            if (selectCallback != null && items.Length > 0)
            {
                selectCallback(items[0], false);
            }
            else
            {
                action ??= GetDefaultAction(selection, items);

                if (endSearch)
                    EditorApplication.delayCall -= DelayTrackSelection;

                if (action?.handler != null && items.Length == 1)
                    action.handler(item);
                else if (action?.execute != null)
                    action.execute(items);
                else
                    action?.handler?.Invoke(item);
            }
        }

        public virtual void ExecuteSelection()
        {
            ExecuteAction(GetDefaultAction(selection, selection), SelectionToArray(selection), endSearch: false);
        }

        public virtual void Refresh(RefreshFlags reason = RefreshFlags.Default)
        {
            RefreshItems();
        }

        public virtual void RefreshItems(Action<IEnumerable<SearchItem>> incomingItems = null, Action refreshDone = null)
        {
            m_FilteredItems.Clear();
            SearchService.Request(state.context, (c, items) =>
            {
                if (incomingItems == null)
                {
                    m_FilteredItems.AddItems(FilterItems(items));
                }
                else
                {
                    m_ItemsTempList.AddRange(FilterItems(items));
                    m_FilteredItems.AddItems(m_ItemsTempList);
                    incomingItems?.Invoke(m_ItemsTempList);
                    m_ItemsTempList.Clear();
                }
            }, _ => refreshDone?.Invoke());
        }

        public virtual void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.MoveLineEnd)
        {
            queryChanged?.Invoke(this, searchText);
            context.searchText = searchText;
            RefreshItems();
        }

        public virtual void SetSelection(params int[] newSelection)
        {
            SetSelection(true, newSelection);
        }

        public void ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition)
        {
            var menu = new GenericMenu();

            if (addToItemContextualMenu != null)
                addToItemContextualMenu?.Invoke(menu, item);
            else
                AddToItemContextualMenu(menu, item);

            if (contextualActionPosition == default)
                menu.ShowAsContext();
            else
                menu.DropDown(contextualActionPosition);
        }

        public virtual void AddToItemContextualMenu(GenericMenu menu, SearchItem item)
        {
            var shortcutIndex = 0;
            var useSelection = false;
            if (context == null)
                return;

            foreach (var selectedItem in context.selection)
            {
                if (selectedItem != null && string.Equals(selectedItem.id, item.id, StringComparison.OrdinalIgnoreCase))
                {
                    useSelection = true;
                    break;
                }
            }

            var currentSelection = useSelection ? context.selection : new SearchSelection([item]);
            foreach (var action in item.provider.actions)
            {
                var actionEnabled = action.enabled?.Invoke(currentSelection) ?? true;
                if (!actionEnabled)
                    continue;

                var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                switch (shortcutIndex)
                {
                    case 0:
                        itemName += " _enter";
                        break;
                    case 1:
                        itemName += " _&enter";
                        break;
                }

                menu.AddItem(new GUIContent(itemName, action.content.image), false, () => ExecuteAction(action, SelectionToArray(selection)));
                ++shortcutIndex;
            }

            menu.AddSeparator("");
            if (SearchSettings.searchItemFavorites.Contains(item.id))
                menu.AddItem(new GUIContent("Remove from Favorites"), false, () => SearchSettings.RemoveItemFavorite(item));
            else
                menu.AddItem(new GUIContent("Add to Favorites"), false, () => SearchSettings.AddItemFavorite(item));
        }

        IEnumerable<IGroup> ISearchView.EnumerateGroups()
        {
            return EnumerateGroups(!m_ViewState.hideAllGroup);
        }

        IEnumerable<SearchQueryError> ISearchView.GetAllVisibleErrors()
        {
            // var visibleProviders = ((ISearchView)this).EnumerateGroups().Select(g => g.id).ToArray();
            var visibleProviders = new HashSet<string>();
            foreach (var group in ((ISearchView)this).EnumerateGroups())
            {
                visibleProviders.Add(group.id);
            }

            var defaultProvider = SearchService.GetDefaultProvider();
            foreach (var e in context.GetAllErrors())
            {
                if (visibleProviders.Contains(e.provider.type) || e.provider.type == defaultProvider.type)
                    yield return e;
            }
        }

        EntityId ISearchView.GetViewId()
        {
            return EntityId.None;
        }
        #endregion

        #region ISearchView Unsupported
        public virtual void Focus()
        {
            // Nothing by default: not tied to UI
        }

        public virtual void FocusSearch()
        {
            // Nothing by default: not tied to UI
        }

        public virtual bool IsPicker()
        {
            return false;
        }

        public virtual void Repaint()
        {
            // Nothing by default: not tied to UI => should be MarkDirtyRepaint
        }

        public virtual void SelectSearch()
        {
            // Nothing by default: not tied to UI => should be same thing as Focus.
        }

        public virtual void Close()
        {
            throw new NotSupportedException();
        }

        public virtual void SetColumns(IEnumerable<SearchColumn> columns)
        {
            throw new NotSupportedException();
        }

        void ISearchView.SetupColumns(IList<SearchField> fields)
        {
            throw new NotSupportedException();
        }

        public virtual void SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            throw new NotSupportedException("Cursor cannot be set for this control.");
        }
        #endregion

        #region Utility functions. Taken mostly from internal SearchView.
        public static SearchItem[] SelectionToArray(SearchSelection selection)
        {
            var selectionArray = new SearchItem[selection.Count];
            var index = 0;
            foreach (var item in selection)
            {
                selectionArray[index++] = item;
            }
            return selectionArray;
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

        internal static SearchAction GetDefaultAction(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            var provider = GetProviderOfInterest(selection, items);
            if (provider.actions?.Count > 0)
                return provider.actions[0];
            return null;
        }

        internal static SearchAction GetSecondaryAction(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            var provider = GetProviderOfInterest(selection, items);
            if (provider.actions?.Count > 1)
                return provider.actions[1];
            if (provider.actions?.Count > 0)
                return provider.actions[0];
            return null;
        }

        internal static SearchProvider GetProviderOfInterest(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            if (items != null)
            {
                using var it = items.GetEnumerator();
                if (it.MoveNext())
                    return it.Current?.provider;
            }

            if (selection != null && selection.Count > 0)
            {
                return selection.First().provider;
            }
            return null;
        }

        internal IEnumerable<IGroup> EnumerateGroups(bool showTheAllGroupTab)
        {
            var groups = m_FilteredItems.EnumerateGroups(showTheAllGroupTab);
            foreach (var group in groups)
            {
                if (!showTheAllGroupTab || !string.Equals(group.id, "default", StringComparison.Ordinal))
                    yield return group;
            }
        }

        internal IGroup GetGroupById(string groupId)
        {
            return m_FilteredItems.GetGroupById(groupId);
        }

        IEnumerable<SearchItem> FilterItems(IEnumerable<SearchItem> items)
        {
            if (filterCallback == null)
            {
                foreach (var item in items)
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in items)
                {
                    if (filterCallback(item))
                        yield return item;
                }
            }
        }
        #endregion

        #region User Overridable
        protected bool IsItemValid(int index)
        {
            if (index < 0 || index >= m_FilteredItems.Count)
                return false;
            return true;
        }

        protected void SetSelection(bool trackSelection, int[] newSelection, bool forceChange = false)
        {
            if (!multiselect && newSelection.Length > 1)
                newSelection = [newSelection[^1]];

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
                m_ViewState.selectedIds = selectedIds.ToArray();
                if (trackSelection)
                    TrackSelection(lastIndexAdded);
            }
        }

        protected void TrackSelection(int currentSelection)
        {
            if (trackingCallback == null)
                return;

            m_DelayedCurrentSelection = currentSelection;
            EditorApplication.delayCall -= DelayTrackSelection;
            EditorApplication.delayCall += DelayTrackSelection;
        }

        protected void DelayTrackSelection()
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
        #endregion
    }
}
