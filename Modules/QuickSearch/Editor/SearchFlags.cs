// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    /// <summary>
    /// Various search options used to fetch items.
    /// </summary>
    [Flags]
    public enum SearchFlags
    {
        /// <summary>
        /// No specific search options.
        /// </summary>
        None = 0,

        /// <summary>
        /// Search items are fetch synchronously.
        /// </summary>
        Synchronous = 1 << 0,

        /// <summary>
        /// Fetch items will be sorted by the search service.
        /// </summary>
        Sorted = 1 << 1,

        /// <summary>
        /// Send the first items asynchronously
        /// </summary>
        FirstBatchAsync = 1 << 2,

        /// <summary>
        /// Sets the search to search for all results.
        /// </summary>
        WantsMore = 1 << 3,

        /// <summary>
        /// Adding debugging info while looking for results,
        /// </summary>
        Debug = 1 << 4,

        /// <summary>
        /// Prevent the search to use any indexing
        /// </summary>
        NoIndexing = 1 << 5,

        /// <summary>
        /// Default Search Flag
        /// </summary>
        Default = Sorted,

        /// <summary>
        /// Always show query errors even when there are results available.
        /// </summary>
        ShowErrorsWithResults = 1 << 24,

        /// <summary>
        /// Search View Flags
        /// </summary>
        SaveFilters = 1 << 25,

        /// <summary>
        /// Open QuickSearch reusing an existing window if any.
        /// </summary>
        ReuseExistingWindow = 1 << 26,

        /// <summary>
        /// Specify that a QuickSearch window list view supports multi-selection.
        /// </summary>
        Multiselect = 1 << 27,

        /// <summary>
        /// Specify that a QuickSearch window is dockable instead of being a modal popup window.
        /// </summary>
        Dockable = 1 << 28,

        /// <summary>
        /// Focus the search query when opening QuickSearch.
        /// </summary>
        FocusContext = 1 << 29,

        /// <summary>
        /// Hide all QuickSearch side panels.
        /// </summary>
        HidePanels = 1 << 30,

        /// <summary>
        /// Default options when opening a QuickSearch window.
        /// </summary>
        OpenDefault = SaveFilters | Multiselect | Dockable,
        /// <summary>
        /// Default options when opening a QuickSearch using the global shortcut.
        /// </summary>
        OpenGlobal = OpenDefault | ReuseExistingWindow,
        /// <summary>
        /// Options when opening QuickSearch in contextual mode (with only a few selected providers enabled).
        /// </summary>
        OpenContextual = Multiselect | Dockable | FocusContext,
        /// <summary>
        /// Options when opening QuickSearch as an Object Picker.
        /// </summary>
        OpenPicker = FocusContext | HidePanels | WantsMore
    }

    static class SearchFlagsExtensions
    {
        public static bool HasAny(this SearchFlags flags, SearchFlags f) => (flags & f) != 0;
        public static bool HasAll(this SearchFlags flags, SearchFlags all) => (flags & all) == all;

        public static bool HasAny(this ShowDetailsOptions flags, ShowDetailsOptions f) => (flags & f) != 0;
        public static bool HasAll(this ShowDetailsOptions flags, ShowDetailsOptions all) => (flags & all) == all;

        public static bool HasAny(this SearchItemOptions flags, SearchItemOptions f) => (flags & f) != 0;
        public static bool HasAll(this SearchItemOptions flags, SearchItemOptions all) => (flags & all) == all;

        public static bool HasAny(this FetchPreviewOptions flags, FetchPreviewOptions f) => (flags & f) != 0;
        public static bool HasAll(this FetchPreviewOptions flags, FetchPreviewOptions all) => (flags & all) == all;
    }
}
