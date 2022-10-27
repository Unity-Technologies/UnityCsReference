// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Contains databases and preferences used to perform a search.
    /// </summary>
    class ItemLibraryLibrary_Internal
    {
        const string k_DefaultContext = "UnknownItemLibraryContext";

        /// <summary>
        /// Adapter used in this library to customize the searching interface.
        /// </summary>
        public IItemLibraryAdapter Adapter { get; }

        /// <summary>
        /// Sorting function used to organize items.
        /// </summary>
        Comparison<ItemLibraryItem> SortComparison => Adapter.SortComparison;

        /// <summary>
        /// Associates style names to category paths.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to certain categories.</remarks>
        public IReadOnlyDictionary<string, string> CategoryPathStyleNames => Adapter?.CategoryPathStyleNames;

        /// <summary>
        /// Sets the visibility of the preview panel in the user preferences.
        /// </summary>
        /// <param name="visible">Whether or not the preview panel should be visible.</param>
        public void SetPreviewPanelVisibility(bool visible)
        {
            Preferences_Internal.PreviewVisibility = visible;
        }

        /// <summary>
        /// Gets the visibility of the preview panel from the user preferences.
        /// </summary>
        /// <returns>True if the panel should be visible, false otherwise.</returns>
        public bool IsPreviewPanelVisible()
        {
            return Adapter.HasDetailsPanel && Preferences_Internal.PreviewVisibility;
        }

        readonly List<ItemLibraryDatabaseBase> m_Databases;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryLibrary_Internal"/> class.
        /// </summary>
        /// <param name="items">The items to search.</param>
        /// <param name="title">The title for the search window.</param>
        /// <param name="libraryName">The name of the tool using the library. Used to separate preferences.</param>
        /// <param name="context">The name of the context using the library. Used to separate preferences.</param>
        public ItemLibraryLibrary_Internal(IEnumerable<ItemLibraryItem> items, string title, string libraryName = null, string context = null)
            : this(items, new ItemLibraryAdapter(title,  libraryName), context: context)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryLibrary_Internal"/> class.
        /// </summary>
        /// <param name="items">The items to search.</param>
        /// <param name="adapter">An adapter specifying several preferences for the search.</param>
        /// <param name="context">The name of the context using the library. Used to separate preferences.</param>
        public ItemLibraryLibrary_Internal(IEnumerable<ItemLibraryItem> items, IItemLibraryAdapter adapter, string context = null)
            : this(new List<ItemLibraryDatabaseBase> { new ItemLibraryDatabase(items.ToList()) }, adapter, context: context)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryLibrary_Internal"/> class.
        /// </summary>
        /// <param name="database">A database of items to search.</param>
        /// <param name="title">The title for the search window.</param>
        /// <param name="libraryName">The name of the tool using the library. Used to separate preferences.</param>
        /// <param name="context">The name of the context using the library. Used to separate preferences.</param>
        public ItemLibraryLibrary_Internal(ItemLibraryDatabaseBase database, string title, string libraryName = null, string context = null)
            : this(new List<ItemLibraryDatabaseBase> { database }, title, libraryName: libraryName, context: context)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryLibrary_Internal"/> class.
        /// </summary>
        /// <param name="databases">The databases of items to search.</param>
        /// <param name="title">The title for the search window.</param>
        /// <param name="filter">A filter to ignore certain items.</param>
        /// <param name="libraryName">The name of the tool using the library. Used to separate preferences.</param>
        /// <param name="context">The name of the context using the library. Used to separate preferences.</param>
        public ItemLibraryLibrary_Internal(IEnumerable<ItemLibraryDatabaseBase> databases, string title, ItemLibraryFilter filter = null, string libraryName = null, string context = null)
            : this(databases, title, new ItemLibraryAdapter(title, libraryName), filter, context)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryLibrary_Internal"/> class.
        /// </summary>
        /// <param name="database">A database of items to search.</param>
        /// <param name="adapter">An adapter specifying several preferences for the search.</param>
        /// <param name="filter">A filter to ignore certain items.</param>
        /// <param name="context">The name of the context using the library. Used to separate preferences.</param>
        public ItemLibraryLibrary_Internal(ItemLibraryDatabaseBase database, IItemLibraryAdapter adapter = null, ItemLibraryFilter filter = null, string context = null)
            : this(new List<ItemLibraryDatabaseBase> { database }, adapter, filter, context)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLibraryLibrary_Internal"/> class.
        /// </summary>
        /// <param name="databases">The databases of items to search.</param>
        /// <param name="adapter">An adapter specifying several preferences for the search.</param>
        /// <param name="filter">A filter to ignore certain items.</param>
        /// <param name="context">The name of the context using the library. Used to separate preferences.</param>
        public ItemLibraryLibrary_Internal(IEnumerable<ItemLibraryDatabaseBase> databases, IItemLibraryAdapter adapter = null, ItemLibraryFilter filter = null, string context = null)
            : this(databases, null, adapter, filter, context)
        {}

        internal ItemLibraryPreferences_Internal Preferences_Internal { get; }

        Dictionary<string, ItemLibraryItem> m_ItemsByPath;
        List<ItemLibraryItem> m_CachedFavorites;

        /// <summary>
        /// A list of the favorite <see cref="ItemLibraryItem"/> in this library.
        /// </summary>
        public IReadOnlyList<ItemLibraryItem> CurrentFavorites => m_CachedFavorites;

        /// <summary>
        /// Tests whether a <see cref="ItemLibraryItem"/> is a favorite or not.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns><c>true</c> if the item is favorite, <c>false</c> otherwise.</returns>
        public bool IsFavorite(ItemLibraryItem item)
        {
            return Preferences_Internal.GetFavorites().Contains(item.FullName);
        }

        /// <summary>
        /// Sets or unsets a <see cref="ItemLibraryItem"/> as favorite.
        /// </summary>
        /// <param name="item">The item to set or unset as favorite.</param>
        /// <param name="setFavorite">Set to <c>true</c> to set as favorite, <c>false</c> to remove from favorites.</param>
        public void SetFavorite(ItemLibraryItem item, bool setFavorite = true)
        {
            Preferences_Internal.SetFavorite(item.FullName, setFavorite);
            if (setFavorite)
                m_CachedFavorites.Add(item);
            else
                m_CachedFavorites.Remove(item);
        }

        /// <summary>
        /// Tests whether a category is collapsed or not.
        /// </summary>
        /// <param name="categoryView">The category to test.</param>
        /// <returns><c>true</c> if the category is collapsed, <c>false</c> otherwise.</returns>
        internal bool IsCollapsed_Internal(ICategoryView_Internal categoryView)
        {
            return Preferences_Internal.GetCollapsedCategories().Contains(categoryView.Path);
        }

        /// <summary>
        /// Sets a category as collapsed or expanded.
        /// </summary>
        /// <param name="categoryView">The category to collapse or expand.</param>
        /// <param name="setCollapsed">Set to <c>true</c> to collapse, <c>false</c> to expand.</param>
        internal void SetCollapsed_Internal(ICategoryView_Internal categoryView, bool setCollapsed = true)
        {
            Preferences_Internal.SetCollapsed(categoryView.Path, setCollapsed);
        }

        /// <summary>
        /// Clears all favorites.
        /// </summary>
        public void ClearFavorites()
        {
            Preferences_Internal.ClearFavorites();
            m_CachedFavorites.Clear();
        }

        ItemLibraryLibrary_Internal(IEnumerable<ItemLibraryDatabaseBase> databases, string title, IItemLibraryAdapter adapter, ItemLibraryFilter filter, string context = null)
        {
            m_Databases = new List<ItemLibraryDatabaseBase>();
            var databaseId = 0;
            foreach (var database in databases)
            {
                // This is needed for sorting items between databases.
                database.OverwriteId_Internal(databaseId);
                databaseId++;
                database.SetCurrentFilter(filter);

                m_Databases.Add(database);
            }

            Adapter = adapter ?? new ItemLibraryAdapter(title);
            Preferences_Internal = new ItemLibraryPreferences_Internal(Adapter.LibraryName, string.IsNullOrEmpty(context) ? k_DefaultContext : context);
        }

        Dictionary<ItemLibraryItem, ItemLibraryDatabaseBase.SearchData_Internal> m_LastSearchDataPerItem = new Dictionary<ItemLibraryItem, ItemLibraryDatabaseBase.SearchData_Internal>();
        long m_LastMaxScore;

        ItemLibraryItem GetMaxInLastSearch(IReadOnlyList<ItemLibraryItem> searchResults)
        {
            var maxScore = long.MinValue;
            ItemLibraryItem result = null;
            foreach (var item in searchResults)
            {
                if (m_LastSearchDataPerItem.TryGetValue(item, out var data) && data.Score > maxScore)
                {
                    maxScore = data.Score;
                    result = item;
                }
            }

            return result;
        }

        /// <summary>
        /// Searches for items using a text query.
        /// </summary>
        /// <param name="query">The query to match items with.</param>
        /// <returns>A possibly empty collection of items matching the query.</returns>
        public IEnumerable<ItemLibraryItem> Search(string query)
        {
            var results = new List<ItemLibraryItem>();
            m_LastSearchDataPerItem.Clear();

            query = query.ToLower();
            m_LastMaxScore = long.MinValue;
            foreach (var database in m_Databases)
            {
                var localResults = database.Search(query);
                foreach (var kv in database.LastSearchData_Internal)
                {
                    m_LastSearchDataPerItem.Add(kv.Key, kv.Value);
                }

                var bestInDb = GetMaxInLastSearch(localResults);
                var localMaxScore = bestInDb != null ? m_LastSearchDataPerItem[bestInDb].Score : long.MinValue;
                if (localMaxScore > m_LastMaxScore)
                {
                    m_LastMaxScore = localMaxScore;
                    // skip the highest scored item in the local results and
                    // insert it back as the first item. The first item should always be
                    // the highest scored item. The order of the other items does not matter
                    // because they will be reordered to recreate the tree properly.
                    if (results.Count > 0)
                    {
                        // backup previous best result
                        results.Add(results[0]);
                        // replace it with the new best result
                        results[0] = localResults[0];
                        // add remaining results at the end
                        results.AddRange(localResults.Skip(1));
                    }
                    else // best result will be the first item
                        results.AddRange(localResults);
                }
                else // no new best result just append everything
                {
                    results.AddRange(localResults);
                }
            }

            if (string.IsNullOrEmpty(query))
            {
                results.Sort((a, b) =>
                {
                    var priorityDelta = a.Priority - b.Priority;
                    if (priorityDelta == 0 && SortComparison != null)
                        return SortComparison(a, b);
                    return priorityDelta;
                });
            }
            else
            {
                int Comparison(ItemLibraryItem a, ItemLibraryItem b) => (int)(m_LastSearchDataPerItem[b].Score - m_LastSearchDataPerItem[a].Score);
                results.Sort(Comparison);
            }

            if (m_ItemsByPath == null)
            {
                m_ItemsByPath = new Dictionary<string, ItemLibraryItem>();
                var allItems = string.IsNullOrEmpty(query) ? results : Search("");
                m_ItemsByPath = allItems.ToDictionary(r => r.FullName, r => r);

                m_CachedFavorites = Preferences_Internal.GetFavorites()
                    .Select(path => m_ItemsByPath.TryGetValue(path, out var item) ? item : null)
                    .Where(i => i != null)
                    .ToList();
            }

            return results;
        }
    }
}
