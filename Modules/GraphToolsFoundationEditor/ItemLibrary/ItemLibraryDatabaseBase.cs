// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// ItemLibrary Database base class
    /// Provides ways to index, filter and search a collection of library items
    /// </summary>
    [Serializable]
    abstract class ItemLibraryDatabaseBase
    {
        /// <summary>
        /// The filter currently being applied to the Database.
        /// </summary>
        public ItemLibraryFilter CurrentFilter { get; private set; }

        /// <summary>
        /// Contains every item in the database after it has been indexed.
        /// </summary>
        public IReadOnlyList<ItemLibraryItem> IndexedItems => m_IndexedItems;

        /// <summary>
        /// The filename to use to store serialized database json file
        /// </summary>
        protected const string k_SerializedJsonFile = "/SerializedDatabase.json";

        /// <summary>
        /// Whether or not to use parallel tasks to compute various operations such as indexing and filtering
        /// </summary>
        protected const bool k_UseParallelTasks = true;

        /// <summary>
        /// The Maximum number of filter results to cache.
        /// </summary>
        protected const int k_MaxNumFilterCache = 5;

        [SerializeField]
        List<ItemLibraryItem> m_IndexedItems;

        (ItemLibraryFilter filter, List<ItemLibraryItem> filteredItems)[] m_FilterCache;

        [SerializeField]
        int m_OldestCacheIndex;

        IEnumerable<ItemLibraryItem> m_UnindexedItems;

        Task[] m_ParallelTasks = new Task[Environment.ProcessorCount];
        List<ItemLibraryItem>[] m_ParallelLists = new List<ItemLibraryItem>[Environment.ProcessorCount];

        /// <summary>
        /// Instantiates an empty database
        /// </summary>
        protected ItemLibraryDatabaseBase()
            : this(new List<ItemLibraryItem>())
        {
        }

        internal struct SearchData_Internal
        {
            public long Score;
            public string MatchedString;
            public List<int> MatchedIndices;
        }

        internal Dictionary<ItemLibraryItem, SearchData_Internal> LastSearchData_Internal { get; } = new Dictionary<ItemLibraryItem, SearchData_Internal>();

        /// <summary>
        /// Instantiates a database with items that need to be indexed.
        /// </summary>
        /// <param name="unindexedItems">Items needing to be indexed.</param>
        protected ItemLibraryDatabaseBase(IEnumerable<ItemLibraryItem> unindexedItems)
        {
            m_UnindexedItems = unindexedItems;
        }

        /// <summary>
        /// Sets the filter to be used for future search.
        /// </summary>
        /// <param name="filter">The filter to use.</param>
        public void SetCurrentFilter(ItemLibraryFilter filter)
        {
            CurrentFilter = filter;
        }

        /// <summary>
        /// Searches the dabatase for matching items.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <returns>Items matching the search query as a list.</returns>
        public List<ItemLibraryItem> Search(string query)
        {
            return SearchAsEnumerable(query).ToList();
        }

        /// <summary>
        /// Searches the dababase for matching items.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <returns>Items matching the search query.</returns>
        public IEnumerable<ItemLibraryItem> SearchAsEnumerable(string query)
        {
            IndexIfNeeded();
            if (m_IndexedItems != null)
            {
                var filteredItems = FilterAndCacheItems(CurrentFilter, m_IndexedItems);
                LastSearchData_Internal.Clear();
                return PerformSearch(query, filteredItems);
            }

            return Enumerable.Empty<ItemLibraryItem>();
        }

        /// <summary>
        /// Indexes the database unless IndexedItems is not empty.
        /// Called by every call to `Search` but can be called manually ahead of time for convenience.
        /// </summary>
        public void IndexIfNeeded()
        {
            m_IndexedItems ??= PerformIndex(m_UnindexedItems);
        }

        /// <summary>
        /// Indexes database items. Get children items, get costly data if needed.
        /// Indexed items are stored in IndexItems.
        /// </summary>
        /// <param name="itemsToIndex">Items to index</param>
        /// <param name="estimateIndexSize">Estimate of the number of items, helps avoid reallocations.</param>
        /// <returns>A list of items that have been indexed</returns>
        public virtual List<ItemLibraryItem> PerformIndex(IEnumerable<ItemLibraryItem> itemsToIndex, int estimateIndexSize = -1)
        {
            if (estimateIndexSize < 0)
            {
                estimateIndexSize = itemsToIndex is IList<ItemLibraryItem> list ? list.Count : 0;
            }
            var indexedItems = new List<ItemLibraryItem>(estimateIndexSize);
            foreach (var item in itemsToIndex)
                AddItemToIndex(item, indexedItems);
            return indexedItems;
        }

        /// <summary>
        /// Applies a filter to a collection of items to only select some of them.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="items">The items to filter.</param>
        /// <returns>The list of items with the filter applied.</returns>
        public virtual List<ItemLibraryItem> PerformFilter(ItemLibraryFilter filter, IReadOnlyList<ItemLibraryItem> items)
        {
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (k_UseParallelTasks && items.Count > 100)
                return FilterMultiThreaded(filter, items);
            return FilterSingleThreaded(filter, items);
        }

        /// <summary>
        /// Calls PerformFilter and cache its result per filter.
        /// </summary>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="items">The items to filter.</param>
        /// <returns>The list of items with the filter applied.</returns>
        List<ItemLibraryItem> FilterAndCacheItems(ItemLibraryFilter filter, IReadOnlyList<ItemLibraryItem> items)
        {
            if (filter == null)
                return items.ToList();

            m_FilterCache = m_FilterCache ?? new(ItemLibraryFilter filter, List<ItemLibraryItem> filteredItems)[k_MaxNumFilterCache];
            var cachedItems = m_FilterCache.FirstOrDefault(tu => tu.filter == filter).filteredItems;

            if (cachedItems == null)
            {
                cachedItems = PerformFilter(filter, items);
                m_FilterCache[m_OldestCacheIndex] = (filter, cachedItems);
                m_OldestCacheIndex = (m_OldestCacheIndex + 1) % m_FilterCache.Length;
            }

            return cachedItems;
        }

        /// <summary>
        /// Performs a search on a collection of items that has already been indexed and filtered.
        /// </summary>
        /// <param name="query">Search query, e.g. keyword representing items to search for.</param>
        /// <param name="filteredItems">The list of indexed items to search in.</param>
        /// <returns>A list of items matching the search query.</returns>
        public abstract IEnumerable<ItemLibraryItem> PerformSearch(string query, IReadOnlyList<ItemLibraryItem> filteredItems);

        List<ItemLibraryItem> FilterSingleThreaded(ItemLibraryFilter filter, IReadOnlyList<ItemLibraryItem> items)
        {
            var result = new List<ItemLibraryItem>(items.Count);

            foreach (var item in items)
            {
                if (!filter.Match(item))
                    continue;

                result.Add(item);
            }

            return result;
        }

        List<ItemLibraryItem> FilterMultiThreaded(ItemLibraryFilter filter, IReadOnlyList<ItemLibraryItem> items)
        {
            var result = new List<ItemLibraryItem>();
            var tasks = m_ParallelTasks;
            var lists = m_ParallelLists;
            var count = tasks.Length;
            var itemsPerTask = (int)Math.Ceiling(items.Count / (float)count);

            for (var i = 0; i < count; i++)
            {
                var i1 = i;
                tasks[i] = Task.Run(() =>
                {
                    lists[i1] = new List<ItemLibraryItem>();

                    for (var j = 0; j < itemsPerTask; j++)
                    {
                        var index = j + itemsPerTask * i1;
                        if (index >= items.Count)
                            break;

                        var item = items[index];
                        if (!filter.Match(item))
                            continue;

                        lists[i1].Add(item);
                    }
                });
            }

            Task.WaitAll(tasks);

            for (var i = 0; i < count; i++)
            {
                result.AddRange(lists[i]);
            }

            return result;
        }

        /// <summary>
        /// Internal helper to overwrite Id after deserializing
        /// </summary>
        /// <param name="newId"></param>
        internal void OverwriteId_Internal(int newId)
        {
            Id_Internal = newId;
        }

        internal int Id_Internal { get; private set; }

        /// <summary>
        /// Saves a database to a file.
        /// </summary>
        /// <param name="databaseDirectory">Directory where the file should be stored.</param>
        public void SerializeToDirectory(string databaseDirectory)
        {
            if (databaseDirectory == null)
                return;
            if (!Directory.Exists(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);
            IndexIfNeeded();
            var serializedData = EditorJsonUtility.ToJson(this, true);
            var writer = new StreamWriter(databaseDirectory + k_SerializedJsonFile, false);
            writer.Write(serializedData);
            writer.Close();
        }

        /// <summary>
        /// Index an item in the database.
        /// Includes computing data related to the item that might be expensive, and discovering children items.
        /// </summary>
        /// <param name="item">The item to index.</param>
        /// <param name="indexedItems">The list to add indexed item to.</param>
        protected void AddItemToIndex(ItemLibraryItem item, List<ItemLibraryItem> indexedItems)
        {
            item.Build();
            indexedItems.Add(item);
        }
    }
}
