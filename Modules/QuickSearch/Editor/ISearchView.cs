// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor.Search
{
    /// <summary>
    /// DisplayMode for a <see cref="ISearchView"/>
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>Unspecified ISearchView display mode</summary>
        None = 0,
        /// <summary>Display as a list view</summary>
        List = 32,
        /// <summary>Display as a Grid of icons of various size.</summary>
        Grid = 96,

        [ExcludeFromDocs]
        /// <summary>Maximum grid size</summary>
        Limit = 128,
        /// <summary>Table view used to bulk edit search results.</summary>
        Table = 129,
    }

    /// <summary>
    /// Where to place the cursor in the text are of a <see cref="ISearchView"/> (see <see cref="ISearchView.SetSearchText"/>).
    /// </summary>
    public enum TextCursorPlacement
    {
        /// <summary>Do not move the cursor.</summary>
        None,
        /// <summary>Move the cursor at the end of the line of text.</summary>
        MoveLineEnd,
        /// <summary>Move the cursor at the beginning of the line of text.</summary>
        MoveLineStart,
        /// <summary>Move the cursor the the end of the previous word.</summary>
        MoveToEndOfPreviousWord,
        /// <summary>Move the cursor the the start of the previous word.</summary>
        MoveToStartOfNextWord,
        /// <summary>Move the cursor one word to the left.</summary>
        MoveWordLeft,
        /// <summary>Move the cursor one word to the right.</summary>
        MoveWordRight,
        /// <summary>Move the cursor one word to the right for auto complete mode.</summary>
        MoveAutoComplete,
        /// <summary>Default cursor position (end of the line of text).</summary>
        Default = MoveLineEnd
    }

    [Flags]
    public enum RefreshFlags
    {
        None = 0,

        // Normal refresh
        Default = 1 << 0,

        // The structure of the current selection data has changed
        StructureChanged = 1 << 1,

        // The display mode or item size has changed
        DisplayModeChanged = 1 << 2,

        // The search item list has been updated
        ItemsChanged = 1 << 3,

        // The current item group has changed.
        GroupChanged = 1 << 4
    }

    /// <summary>
    /// Search view interface used by the search context to execute a few UI operations.
    /// </summary>
    public interface ISearchView : IDisposable
    {
        /// <summary>
        /// Returns the selected item in the view
        /// </summary>
        SearchSelection selection { get; }

        /// <summary>
        /// Return the list of all search results.
        /// </summary>
        ISearchList results { get; }

        /// <summary>
        /// Returns the current view search context
        /// </summary>
        SearchContext context { get; }

        /// <summary>
        /// Defines the size of items in the search view.
        /// </summary>
        float itemIconSize { get; set; }

        /// <summary>
        /// Indicates how the data is displayed in the UI.
        /// </summary>
        DisplayMode displayMode { get; }

        /// <summary>
        /// Allow multi-selection or not.
        /// </summary>
        bool multiselect { get; set; }

        /// <summary>
        /// Absolute coordinate of the search view
        /// </summary>
        Rect position { get; }

        /// <summary>
        /// Callback used to override the select behavior.
        /// </summary>
        Action<SearchItem, bool> selectCallback { get; }

        /// <summary>
        /// Callback used to filter items shown in the list.
        /// </summary>
        Func<SearchItem, bool> filterCallback { get; }

        /// <summary>
        /// Callback used to override the tracking behavior.
        /// </summary>
        Action<SearchItem> trackingCallback { get; }

        /// <summary>
        /// Update the search view with a new selection.
        /// </summary>
        /// <param name="selection">Array of item indices to select</param>
        void SetSelection(params int[] selection);

        /// <summary>
        /// Add new items to the current selection
        /// </summary>
        /// <param name="selection">Array of item indices to add to selection</param>
        void AddSelection(params int[] selection);

        /// <summary>
        /// Sets the search query text.
        /// </summary>
        /// <param name="searchText">Text to be displayed in the search view.</param>
        /// <param name="moveCursor">Where to place the cursor after having set the search text</param>
        void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.Default);
        void SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition);

        /// <summary>
        /// Make sure the search is now focused.
        /// </summary>
        void Focus();

        /// <summary>
        /// Triggers a refresh of the search view, re-fetching all the search items from enabled search providers.
        /// </summary>
        void Refresh(RefreshFlags reason = RefreshFlags.Default);

        /// <summary>
        /// Request the search view to repaint itself
        /// </summary>
        void Repaint();

        /// <summary>
        /// Execute a Search Action on a given list of items.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="items">Items to apply the action on.</param>
        /// <param name="endSearch">If true, executing this action will close the Quicksearch window.</param>
        void ExecuteAction(SearchAction action, SearchItem[] items, bool endSearch = true);

        /// <summary>
        /// Execute the default action of the active selection.
        /// </summary>
        void ExecuteSelection();

        /// <summary>
        /// Close the search view
        /// </summary>
        void Close();

        /// <summary>
        /// Show a contextual menu for the specified item.
        /// </summary>
        /// <param name="item">Item affected by the contextual menu.</param>
        /// <param name="contextualActionPosition">Where the menu should be drawn on screen (generally item position)</param>
        void ShowItemContextualMenu(SearchItem item, Rect contextualActionPosition);

        /// <summary>
        /// Request to focus and select the search field.
        /// </summary>
        void SelectSearch();

        /// <summary>
        /// Focus the search text field control.
        /// </summary>
        void FocusSearch();

        /// <summary>
        /// Set table view columns.
        /// </summary>
        internal void SetColumns(IEnumerable<SearchColumn> columns);
    }
}
