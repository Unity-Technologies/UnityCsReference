// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

namespace Unity.Hierarchy.Editor
{
    sealed class HierarchySearchView : ISearchView
    {
        SearchViewState m_ViewState;
        HierarchyWindow m_SearchWindow;
        HierarchySearchProvider m_SearchProvider;

        public ISearchList results { get; set; }
        public SearchContext context => m_ViewState.context;
        public SearchViewState state => m_ViewState;
        internal HierarchyWindow Window => m_SearchWindow;

        public HierarchySearchView(HierarchyWindow window)
        {
            m_SearchWindow = window;
            m_ViewState = SearchViewState.LoadDefaults();
            m_SearchProvider = new HierarchySearchProvider(window.View);
            m_ViewState.context = SearchService.CreateContext(new[] { m_SearchProvider }, "");
            results = new SortedSearchList(m_ViewState.context);
            context.searchView = this;
        }

        void ISearchView.SetSelection(params int[] selection)
        {
            // Used by SearchField to clear selection when query changes.
        }

        void ISearchView.SetSearchText(string searchText, TextCursorPlacement moveCursor)
        {
            ((ISearchView)this).SetSearchText(searchText, moveCursor, 0);
        }

        void ISearchView.SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
        {
            if (string.Equals(context.searchText.Trim(), searchText.Trim(), StringComparison.Ordinal))
                return;

            context.searchText = searchText;

            m_SearchWindow.SetSearchText(context.searchQuery);
        }

        IEnumerable<SearchQueryError> ISearchView.GetAllVisibleErrors()
        {
            yield break;
        }

        // Used by queryBuilder editors to position editing window
        Rect ISearchView.position => m_SearchWindow.position;

        void ISearchView.Repaint()
        {

        }
        string ISearchView.currentGroup { get => null; set { } }

        bool ISearchView.IsPicker()
        {
            return false;
        }

        #region NotImplemented
        SearchSelection ISearchView.selection => throw new NotSupportedException();
        float ISearchView.itemIconSize { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        DisplayMode ISearchView.displayMode => throw new NotSupportedException();

        bool ISearchView.multiselect { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        bool ISearchView.searchInProgress => throw new NotSupportedException();

        Action<SearchItem, bool> ISearchView.selectCallback => throw new NotSupportedException();

        Func<SearchItem, bool> ISearchView.filterCallback => throw new NotSupportedException();

        Action<SearchItem> ISearchView.trackingCallback => throw new NotSupportedException();

        int ISearchView.totalCount => throw new NotSupportedException();

        bool ISearchView.syncSearch { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        SearchPreviewManager ISearchView.previewManager => throw new NotSupportedException();

        void ISearchView.AddSelection(params int[] selection)
        {
            throw new NotSupportedException();
        }

        void ISearchView.Refresh(RefreshFlags reason)
        {
            throw new NotSupportedException();
        }

        void ISearchView.ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch)
        {
            throw new NotSupportedException();
        }

        void ISearchView.ExecuteSelection()
        {
            throw new NotSupportedException();
        }

        void ISearchView.ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition)
        {
            throw new NotSupportedException();
        }

        void ISearchView.SelectSearch()
        {
            throw new NotSupportedException();
        }

        void ISearchView.FocusSearch()
        {
            throw new NotSupportedException();
        }

        void ISearchView.SetColumns(IEnumerable<SearchColumn> columns)
        {
            throw new NotSupportedException();
        }

        EntityId ISearchView.GetViewId()
        {
            throw new NotSupportedException();
        }

        IEnumerable<IGroup> ISearchView.EnumerateGroups()
        {
            throw new NotSupportedException();
        }

        void ISearchView.SetupColumns(IList<SearchField> fields)
        {
            throw new NotSupportedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotSupportedException();
        }

        void ISearchView.Focus()
        {
            throw new NotSupportedException();
        }

        void ISearchView.Close()
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
