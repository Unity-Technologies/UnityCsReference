// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.Search;

namespace UnityEditor.Experimental.GraphView
{
    // Implement most of the ISearchView without bounding a UI View
    [Serializable]
    internal class TemplateSearchViewModel : ISearchView
    {
        const int k_ResetSelectionIndex = -1;

        private GroupedSearchList m_FilteredItems;
        private SearchSelection m_SearchItemSelection;
        private int m_DelayedCurrentSelection = k_ResetSelectionIndex;
        private List<int> m_Selection;
        private SearchViewState m_ViewState;

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
            get { return m_ViewState; }
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
                var prevGroup = currentGroup;
                viewState.groupChanged?.Invoke(context, value, currentGroup);

                var selectedItems = m_SearchItemSelection != null ? new List<SearchItem>(m_SearchItemSelection) : new List<SearchItem>();
                var newSelectedIndices = new int[selectedItems.Count];

                viewState.group = value;
                m_FilteredItems.currentGroup = value;

                // By default no refresh of content. Is it ok to assume this?
                // RefreshContent(RefreshFlags.GroupChanged);

                for (var i = 0; i < selectedItems.Count; ++i)
                {
                    var selectedItem = selectedItems[i];
                    newSelectedIndices[i] = m_FilteredItems.IndexOf(selectedItem);
                }
                SetSelection(true, newSelectedIndices, true);
            }
        }
        public float itemIconSize { get; set; }

        public string currentResultViewId { get; set; }

        public DisplayMode displayMode { get; set; }

        public bool multiselect { get; set; }

        public Rect position { get; set; }

        public bool searchInProgress => m_ViewState.context.searchInProgress;

        public Action<SearchItem, bool> selectCallback { get; set; }

        public Func<SearchItem, bool> filterCallback { get; set; }

        public Action<SearchItem> trackingCallback { get; set; }

        int ISearchView.totalCount => m_FilteredItems.TotalCount;

        bool ISearchView.syncSearch { get; set; }

        SearchPreviewManager ISearchView.previewManager => null;

        public SearchViewState viewState => m_ViewState;

        public EntityId viewId { get; set; }
        public Action<GenericMenu, SearchItem> addToItemContextualMenu;
        public Action<SearchAction, SearchItem[], bool> executeAction;
        #endregion

        #region State Change Notification
        public event Action<TemplateSearchViewModel, string> queryChanged;
        #endregion

        public TemplateSearchViewModel(SearchViewState state)
        {
            m_ViewState = state;
            m_FilteredItems = new GroupedSearchList(context);
            m_FilteredItems.currentGroup = viewState.group;
            m_Selection = new();
            m_SearchItemSelection = null;
        }

        #region ISearchView Methods
        public virtual void AddSelection(params int[] selection)
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

            if (items.Length == 0)
                return;
            var item = items[^1];
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
            var selectionCopy = new SearchItem[selection.Count];
            var i = 0;
            foreach (var x in selection)
                selectionCopy[i++] = x;
            ExecuteAction(GetDefaultAction(selection, selection), selectionCopy, endSearch: false);
        }

        public virtual void Refresh(RefreshFlags reason = RefreshFlags.Default)
        {
            RefreshItems();
        }

        public virtual void RefreshItems(Action<IEnumerable<SearchItem>> incomingItems = null, Action refreshDone = null)
        {
            // TODO ViewModel:

            m_FilteredItems.Clear();
            Search.SearchService.Request(state.context, (c, items) =>
            {
                m_FilteredItems.AddItems(items);
                incomingItems?.Invoke(items);
            }, _ => refreshDone?.Invoke());
        }

        public virtual void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.MoveLineEnd)
        {
            queryChanged?.Invoke(this, searchText);
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
            bool useSelection = false;
            if (context?.selection != null)
            {
                foreach (var e in context.selection)
                {
                    if (string.Equals(e.id, item.id, StringComparison.OrdinalIgnoreCase))
                    {
                        useSelection = true;
                        break;
                    }
                }
            }

            var currentSelection = useSelection ? context.selection : new SearchSelection(new[] { item });
            foreach (var action in item.provider.actions)
            {
                bool isEnabled = action.enabled?.Invoke(currentSelection) ?? true;
                if (isEnabled)
                {
                    var itemName = !string.IsNullOrWhiteSpace(action.content.text) ? action.content.text : action.content.tooltip;
                    if (shortcutIndex == 0)
                        itemName += " _enter";
                    else if (shortcutIndex == 1)
                        itemName += " _&enter";

                    var currentSelectionArray = new SearchItem[currentSelection.Count];
                    var index = 0;
                    foreach (var x in currentSelection)
                        currentSelectionArray[index++] = x;

                    menu.AddItem(new GUIContent(itemName, action.content.image), false, () => ExecuteAction(action, currentSelectionArray));
                    ++shortcutIndex;
                }
            }


            menu.AddSeparator("");
            if (SearchSettings.searchItemFavorites.Contains(item.id))
                menu.AddItem(new GUIContent("Remove from Favorites"), false, () => SearchSettings.RemoveItemFavorite(item));
            else
                menu.AddItem(new GUIContent("Add to Favorites"), false, () => SearchSettings.AddItemFavorite(item));
        }

        IEnumerable<IGroup> ISearchView.EnumerateGroups()
        {
            return EnumerateGroups(!viewState.hideAllGroup);
        }

        IEnumerable<SearchQueryError> ISearchView.GetAllVisibleErrors()
        {
            var visibleProviders = new List<string>();
            foreach (var group in ((ISearchView)this).EnumerateGroups())
            {
                visibleProviders.Add(group.id);
            }

            var defaultProvider = Search.SearchService.GetDefaultProvider();
            foreach (var e in context.GetAllErrors())
            {
                if (visibleProviders.Contains(e.provider.type) || e.provider.type == defaultProvider.type)
                {
                    yield return e;
                }
            }
        }

        EntityId ISearchView.GetViewId()
        {
            return viewId;
        }
#endregion

        #region BaseSearchView Unsupported
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
            foreach (var item in (items ?? selection))
            {
                foreach (var action in item.provider.actions)
                {
                    return action;
                }
            }
            return null;
        }

        internal static SearchAction GetSecondaryAction(SearchSelection selection, IEnumerable<SearchItem> items)
        {
            foreach (var item in (items ?? selection))
            {
                var provider = item.provider;
                if (provider.actions.Count > 1)
                {
                    return provider.actions[1];
                }
                return GetDefaultAction(selection, items);
            }

            return null;
        }


        protected bool IsItemValid(int index)
        {
            if (index < 0 || index >= m_FilteredItems.Count)
                return false;
            return true;
        }

        protected void SetSelection(bool trackSelection, int[] selection, bool forceChange = false)
        {
            if (!multiselect && selection.Length > 1)
                selection = new int[] { selection[selection.Length - 1] };

            var selectedIds = new List<EntityId>();
            var lastIndexAdded = k_ResetSelectionIndex;

            m_Selection.Clear();
            m_SearchItemSelection = null;
            foreach (var idx in selection)
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

        internal IEnumerable<IGroup> EnumerateGroups(bool showAll)
        {
            var groups = m_FilteredItems.EnumerateGroups(showAll);
            List<IGroup> filteredGroups = new List<IGroup>();

            if (showAll)
            {
                foreach (var g in groups)
                {
                    if (!string.Equals(g.id, "default", StringComparison.Ordinal))
                    {
                        filteredGroups.Add(g);
                    }
                }
                return filteredGroups;
            }

            return groups;
        }

        internal IGroup GetGroupById(string groupId)
        {
            return m_FilteredItems.GetGroupById(groupId);
        }
#endregion
    }
}
