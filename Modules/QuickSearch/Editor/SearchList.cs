// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    using ItemsById = SortedDictionary<string, SearchItem>;
    using ItemsByScore = SortedDictionary<int, SortedDictionary<string, SearchItem>>;
    using ItemsByProvider = SortedDictionary<int, SortedDictionary<int, SortedDictionary<string, SearchItem>>>;

    /// <summary>
    /// A search list represents a collection of search results that is filled
    /// </summary>
    public interface ISearchList : IList<SearchItem>, IDisposable
    {
        /// <summary>
        /// Indicates if the search request is still running and might return more results asynchronously.
        /// </summary>
        bool pending { get; }

        /// <summary>
        /// Any valid search context that is used to track async search request. It can be null.
        /// </summary>
        SearchContext context { get; }

        /// <summary>
        /// Yields search item until the search query is finished.
        /// Nullified items can be returned while the search request is pending.
        /// </summary>
        /// <returns>List of search items. Items can be null and must be discarded</returns>
        IEnumerable<SearchItem> Fetch();

        /// <summary>
        /// Add new items to the search list.
        /// </summary>
        /// <param name="items">List of items to be added</param>
        void AddItems(IEnumerable<SearchItem> items);

        /// <summary>
        /// Insert new search items in the current list.
        /// </summary>
        /// <param name="index">Index where the items should be inserted.</param>
        /// <param name="items">Items to be inserted.</param>
        void InsertRange(int index, IEnumerable<SearchItem> items);

        /// <summary>
        /// Return a subset of items.
        /// </summary>
        /// <param name="skipCount"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<SearchItem> GetRange(int skipCount, int count);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="selector"></param>
        /// <returns></returns>
        IEnumerable<TResult> Select<TResult>(Func<SearchItem, TResult> selector);
    }

    abstract class BaseSearchList : ISearchList, IList
    {
        private bool m_Disposed = false;

        public SearchContext context { get; private set; }

        public bool pending
        {
            get
            {
                if (context == null)
                    return false;
                return context.searchInProgress;
            }
        }

        public abstract int Count { get; }
        public virtual bool IsReadOnly => true;

        bool IList.IsFixedSize => false;
        bool ICollection.IsSynchronized => true;
        object ICollection.SyncRoot => context;

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                if (value is SearchItem item)
                    this[index] = item;
            }
        }

        public abstract SearchItem this[int index] { get; set; }

        public BaseSearchList()
        {
            context = null;
        }

        public BaseSearchList(SearchContext searchContext, bool trackAsyncItems = true)
        {
            context = searchContext;
            if (trackAsyncItems)
                context.asyncItemReceived += OnAsyncItemsReceived;
        }

        ~BaseSearchList()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract IEnumerable<SearchItem> Fetch();
        public abstract void AddItems(IEnumerable<SearchItem> items);

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (context != null)
                    context.asyncItemReceived -= OnAsyncItemsReceived;

                m_Disposed = true;
            }
        }

        private void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            AddItems(items);
        }

        public IEnumerable<TResult> Select<TResult>(Func<SearchItem, TResult> selector)
        {
            return Fetch().Where(item =>
            {
                context.Tick();
                return item != null;
            }).Select(item => selector(item));
        }

        public virtual void InsertRange(int index, IEnumerable<SearchItem> items) { throw new NotSupportedException(); }
        public virtual IEnumerable<SearchItem> GetRange(int skipCount, int count) { throw new NotSupportedException(); }
        public virtual int IndexOf(SearchItem item) { throw new NotSupportedException(); }
        public virtual void Insert(int index, SearchItem item) { throw new NotSupportedException(); }
        public virtual void RemoveAt(int index) { throw new NotSupportedException(); }
        public virtual void Add(SearchItem item) { throw new NotSupportedException(); }
        public virtual void Clear() { throw new NotSupportedException(); }
        public virtual bool Contains(SearchItem item) { throw new NotSupportedException(); }
        public virtual void CopyTo(SearchItem[] array, int arrayIndex) { throw new NotSupportedException(); }
        public virtual bool Remove(SearchItem item) { throw new NotSupportedException(); }
        public abstract IEnumerator<SearchItem> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IList.Add(object value)
        {
            Add(value as SearchItem ?? throw new ArgumentNullException(nameof(value)));
            return Count-1;
        }

        bool IList.Contains(object value)
        {
            return Contains(value as SearchItem ?? throw new ArgumentNullException(nameof(value)));
        }

        int IList.IndexOf(object value)
        {
            return IndexOf(value as SearchItem ?? throw new ArgumentNullException(nameof(value)));
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, value as SearchItem ?? throw new ArgumentNullException(nameof(value)));
        }

        void IList.Remove(object value)
        {
            Remove(value as SearchItem ?? throw new ArgumentNullException(nameof(value)));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            CopyTo(array as SearchItem[] ?? throw new ArgumentNullException(nameof(array)), index);
        }
    }

    class SortedSearchList : BaseSearchList
    {
        private class IdComparer : Comparer<string>
        {
            public override int Compare(string x, string y)
            {
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }

        private ItemsByProvider m_Data = new ItemsByProvider();
        private Dictionary<string, Tuple<int, int>> m_LUT = new Dictionary<string, Tuple<int, int>>();
        private bool m_TemporaryUnordered = false;
        private List<SearchItem> m_UnorderedItems = new List<SearchItem>();
        private int m_Count = 0;

        public override int Count => m_Count;

        public override SearchItem this[int index]
        {
            get => ElementAt(index);
            set => throw new NotSupportedException();
        }

        public SortedSearchList(SearchContext searchContext) : base(searchContext)
        {
            Clear();
        }

        public void FromEnumerable(IEnumerable<SearchItem> items)
        {
            Clear();
            AddItems(items);
        }

        public SearchItem ElementAt(int index)
        {
            var it = GetEnumerator();
            while (it.MoveNext() && index > 0)
            {
                index--;
            }

            return it.Current;
        }

        public override IEnumerable<SearchItem> Fetch()
        {
            if (context == null)
                throw new Exception("Fetch can only be used if the search list was created with a search context.");

            while (context.searchInProgress)
            {
                if (context.Tick())
                    yield return null;
            }

            var it = GetEnumerator();
            while (it.MoveNext())
            {
                if (it.Current != null)
                    yield return it.Current;
            }
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            if (context.subset != null)
                items = items.Intersect(context.subset);

            foreach (var item in items)
            {
                bool shouldAdd = true;
                if (m_LUT.TryGetValue(item.id, out Tuple<int, int> alreadyContainedValues))
                {
                    if (item.provider.priority >= alreadyContainedValues.Item1 &&
                        item.score >= alreadyContainedValues.Item2)
                        shouldAdd = false;

                    if (shouldAdd)
                    {
                        m_Data[alreadyContainedValues.Item1][alreadyContainedValues.Item2].Remove(item.id);
                        m_LUT.Remove(item.id);
                        --m_Count;
                    }
                }

                if (!shouldAdd)
                    continue;

                if (!m_Data.TryGetValue(item.provider.priority, out var itemsByScore))
                {
                    itemsByScore = new ItemsByScore();
                    m_Data.Add(item.provider.priority, itemsByScore);
                }

                if (!itemsByScore.TryGetValue(item.score, out var itemsById))
                {
                    itemsById = new ItemsById(new IdComparer());
                    itemsByScore.Add(item.score, itemsById);
                }

                itemsById.Add(item.id, item);
                m_LUT.Add(item.id, new Tuple<int, int>(item.provider.priority, item.score));
                ++m_Count;
            }
        }

        public override void Clear()
        {
            m_Data.Clear();
            m_LUT.Clear();
            m_Count = 0;
            m_TemporaryUnordered = false;
            m_UnorderedItems.Clear();
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            if (m_TemporaryUnordered)
            {
                foreach (var item in m_UnorderedItems)
                {
                    yield return item;
                }
            }

            foreach (var itemsByPriority in m_Data)
            {
                foreach (var itemsByScore in itemsByPriority.Value)
                {
                    foreach (var itemsById in itemsByScore.Value)
                    {
                        yield return itemsById.Value;
                    }
                }
            }
        }

        public override IEnumerable<SearchItem> GetRange(int skipCount, int count)
        {
            int skipped = 0;
            int counted = 0;
            foreach (var item in this)
            {
                if (skipped < skipCount)
                {
                    ++skipped;
                    continue;
                }

                if (counted >= count)
                    yield break;

                yield return item;
                ++counted;
            }
        }

        public override void InsertRange(int index, IEnumerable<SearchItem> items)
        {
            if (!m_TemporaryUnordered)
            {
                m_TemporaryUnordered = true;
                m_UnorderedItems = this.ToList();
            }

            var tempList = items.ToList();
            m_UnorderedItems.InsertRange(index, tempList);
            m_Count += tempList.Count;
        }

        public override void Add(SearchItem item)
        {
            AddItems(new[] {item});
        }

        public override void CopyTo(SearchItem[] array, int arrayIndex)
        {
            int i = arrayIndex;
            var it = GetEnumerator();
            while (it.MoveNext())
                array[i++] = it.Current;
        }

        public override bool Contains(SearchItem item)
        {
            if (m_TemporaryUnordered)
            {
                if (m_UnorderedItems.Contains(item))
                    return true;
            }

            foreach (var itemsByPriority in m_Data)
            {
                foreach (var itemsByScore in itemsByPriority.Value)
                {
                    if (itemsByScore.Value.ContainsValue(item))
                        return true;
                }
            }

            return false;
        }

        public override bool Remove(SearchItem item)
        {
            bool removed = false;
            if (m_TemporaryUnordered)
                removed = m_UnorderedItems.Remove(item);

            foreach (var itemsByPriority in m_Data)
            {
                foreach (var itemsByScore in itemsByPriority.Value)
                {
                    if (itemsByScore.Value.Remove(item.id))
                        return true;
                }
            }

            return removed;
        }
    }

    interface IGroup
    {
        string id { get; }
        string name { get; }
        string type { get; }
        int count { get; }
        int priority { get; }
        IEnumerable<SearchItem> items { get; }

        SearchItem ElementAt(int index);
        int IndexOf(SearchItem item);
        bool Add(SearchItem item);
    }

    class GroupedSearchList : BaseSearchList, ISearchList, IGroup
    {
        class Group : IGroup
        {
            public string id { get; private set; }
            public string name { get; private set; }
            public string type { get; private set; }
            public IEnumerable<SearchItem> items => m_Items;
            public int count => m_Items.Count;

            class SortByScoreComparer : IComparer<SearchItem>
            {
                public int Compare(SearchItem x, SearchItem y)
                {
                    int c = x.score.CompareTo(y.score);
                    if (c != 0)
                        return c;
                    return string.CompareOrdinal(x.id, y.id);
                }
            }

            static readonly SortByScoreComparer sortByScoreComparer = new SortByScoreComparer();

            public int priority { get; set; }

            private List<SearchItem> m_Items;
            private HashSet<int> m_IdHashes;

            public Group(string id, string type, string name, int priority = int.MaxValue)
            {
                this.id = id;
                this.name = name;
                this.type = type;
                this.priority = priority;
                m_Items = new List<SearchItem>();
                m_IdHashes = new HashSet<int>();
            }

            public SearchItem ElementAt(int index)
            {
                return m_Items.ElementAt(index);
            }

            public bool Add(SearchItem item)
            {
                bool added = true;
                var itemHash = item.GetHashCode();
                if (m_IdHashes.Contains(itemHash))
                {
                    var startIndex = m_Items.BinarySearch(item, sortByScoreComparer);
                    if (startIndex < 0)
                        startIndex = ~startIndex;
                    var itemIndex = m_Items.IndexOf(item, Math.Max(startIndex - 1, 0));
                    if (itemIndex >= 0 && item.score < m_Items[itemIndex].score)
                    {
                        m_Items.RemoveAt(itemIndex);
                        m_IdHashes.Remove(itemHash);
                        added = false;
                    }
                    else
                        return false;
                }

                var insertAt = m_Items.BinarySearch(item, sortByScoreComparer);
                if (insertAt < 0)
                {
                    insertAt = ~insertAt;
                    m_Items.Insert(insertAt, item);
                    m_IdHashes.Add(itemHash);
                    return added;
                }

                return false;
            }

            public int IndexOf(SearchItem item)
            {
                return m_Items.IndexOf(item);
            }

            public override string ToString()
            {
                return $"{id} ({m_Items.Count})";
            }
        }

        private int m_TotalCount = 0;
        private int m_CurrentGroupIndex = -1;
        private string m_CurrentGroupId;
        private readonly List<IGroup> m_Groups = new List<IGroup>();

        public override int Count => UseAll() ? m_TotalCount : m_Groups[m_CurrentGroupIndex].count;
        public int TotalCount => m_TotalCount;

        string IGroup.id => "all";
        string IGroup.name => "All";
        string IGroup.type => "all";
        int IGroup.priority => int.MinValue + 1;
        int IGroup.count => m_TotalCount;
        IEnumerable<SearchItem> IGroup.items => GetAll();

        public override SearchItem this[int index]
        {
            get => ElementAt(index);
            set => throw new NotSupportedException();
        }

        private bool UseAll()
        {
            return m_CurrentGroupIndex == -1 || m_Groups.Count == 0;
        }

        public override int IndexOf(SearchItem item)
        {
            if (!UseAll())
                return m_Groups[m_CurrentGroupIndex].IndexOf(item);

            int index = 0;
            foreach (var e in GetAll())
            {
                if (e == item)
                    return index;
                index++;
            }

            return -1;
        }

        public SearchItem ElementAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException($"Failed to access item {index} within {Count}/{TotalCount} items", nameof(index));

            if (UseAll())
            {
                foreach (var g in m_Groups)
                {
                    if (index < g.count)
                        return g.ElementAt(index);

                    index -= g.count;
                }
            }
            else
                return m_Groups[m_CurrentGroupIndex].ElementAt(index);

            return null;
        }

        public GroupedSearchList(SearchContext searchContext)
            : base(searchContext, false)
        {
            Clear();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public int GetGroupCount(bool showAll = true)
        {
            return m_Groups.Count + (showAll ? 1 : 0);
        }

        public IGroup GetGroupById(string groupId)
        {
            return m_Groups.Find(group => group.id == groupId);
        }

        internal IEnumerable<IGroup> GetGroupByType(string groupType)
        {
            return m_Groups.Where(group => group.type == groupType);
        }

        public IEnumerable<IGroup> EnumerateGroups(bool showAll = true)
        {
            if (showAll && m_Groups.Count > 1)
                yield return this;
            foreach (var g in m_Groups)
                yield return g;
        }

        public IEnumerable<SearchItem> GetAll()
        {
            return m_Groups.SelectMany(g => g.items);
        }

        public override IEnumerable<SearchItem> Fetch()
        {
            if (context == null)
                throw new Exception("Fetch can only be used if the search list was created with a search context.");

            while (context.searchInProgress)
                yield return null;

            if (UseAll())
            {
                var it = GetEnumerator();
                while (it.MoveNext())
                {
                    if (it.Current != null)
                        yield return it.Current;
                }
            }
            else
            {
                foreach (var item in m_Groups[m_CurrentGroupIndex].items)
                    yield return item;
            }
        }

        private void AddDefaultGroups()
        {
            var defaultGroups = context.providers
                .Where(p => p.showDetailsOptions.HasFlag(ShowDetailsOptions.DefaultGroup))
                .Select(p => new Group(p.id, p.type, p.name, p.priority));
            m_Groups.AddRange(defaultGroups);
            m_Groups.Sort((lhs, rhs) => lhs.priority.CompareTo(rhs.priority));
        }

        public IGroup AddGroup(SearchProvider searchProvider)
        {
            var itemGroup = m_Groups.Find(g => string.Equals(g.id, searchProvider.id, StringComparison.Ordinal));
            if (itemGroup != null)
                return itemGroup;
            itemGroup = new Group(searchProvider.id, searchProvider.type, searchProvider.name, searchProvider.priority);
            m_Groups.Add(itemGroup);
            m_Groups.Sort((lhs, rhs) => lhs.priority.CompareTo(rhs.priority));
            if (!string.IsNullOrEmpty(m_CurrentGroupId))
                m_CurrentGroupIndex = m_Groups.FindIndex(g => g.id == m_CurrentGroupId);
            return itemGroup;
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            foreach (var item in items)
            {
                var itemGroup = AddGroup(item.provider);
                if (itemGroup.Add(item))
                    m_TotalCount++;
            }

            // Restore current group if possible
            if (m_CurrentGroupIndex == -1 && !string.IsNullOrEmpty(m_CurrentGroupId))
                RestoreCurrentGroup(m_CurrentGroupId);
        }

        public override void Clear()
        {
            m_CurrentGroupIndex = -1;
            m_TotalCount = 0;
            m_Groups.Clear();
            AddDefaultGroups();
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            if (UseAll())
                return GetAll().GetEnumerator();

            return m_Groups[m_CurrentGroupIndex].items.GetEnumerator();
        }

        public override void Add(SearchItem item)
        {
            AddItems(new[] { item });
        }

        public override void CopyTo(SearchItem[] array, int arrayIndex)
        {
            int i = arrayIndex;
            var it = GetEnumerator();
            while (it.MoveNext())
                array[i++] = it.Current;
        }

        public string currentGroup
        {
            get
            {
                return m_CurrentGroupId ?? (this as IGroup)?.id;
            }

            set
            {
                if (value == null || !RestoreCurrentGroup(value))
                {
                    m_CurrentGroupId = value;
                    m_CurrentGroupIndex = -1;
                }
            }
        }

        private bool RestoreCurrentGroup(string groupId)
        {
            m_CurrentGroupIndex = m_Groups.FindIndex(g => g.id == groupId);
            if (m_CurrentGroupIndex != -1)
                m_CurrentGroupId = groupId;
            return m_CurrentGroupIndex != -1;
        }

        SearchItem IGroup.ElementAt(int index)
        {
            return ElementAt(index);
        }

        bool IGroup.Add(SearchItem item)
        {
            throw new NotSupportedException();
        }

        int IGroup.IndexOf(SearchItem item)
        {
            throw new NotSupportedException();
        }

        internal int GetItemCount(IEnumerable<string> activeProviderTypes)
        {
            int queryItemCount = 0;

            if (activeProviderTypes == null || !activeProviderTypes.Any())
                return TotalCount;

            foreach (var providerType in activeProviderTypes)
            {
                var groupsWithType = GetGroupByType(providerType);
                if (groupsWithType == null || !groupsWithType.Any())
                    continue;

                foreach (var group in groupsWithType)
                    queryItemCount += group.count;
            }

            return queryItemCount;
        }
    }

    class AsyncSearchList : BaseSearchList, ISearchList
    {
        private readonly List<SearchItem> m_UnorderedItems;

        public override int Count => m_UnorderedItems.Count;

        public override SearchItem this[int index]
        {
            get => m_UnorderedItems[index];
            set => m_UnorderedItems[index] = value;
        }

        public AsyncSearchList(SearchContext searchContext) : base(searchContext)
        {
            m_UnorderedItems = new List<SearchItem>();
        }

        public override void Add(SearchItem item)
        {
            m_UnorderedItems.Add(item);
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            m_UnorderedItems.AddRange(context.subset != null ? items.Intersect(context.subset) : items);
        }

        public override void Clear()
        {
            m_UnorderedItems.Clear();
        }

        public override bool Contains(SearchItem item)
        {
            return m_UnorderedItems.Contains(item);
        }

        public override void CopyTo(SearchItem[] array, int arrayIndex)
        {
            m_UnorderedItems.CopyTo(array, arrayIndex);
        }

        public override IEnumerable<SearchItem> Fetch()
        {
            if (context == null)
                throw new Exception("Fetch can only be used if the search list was created with a search context.");

            int i = 0;
            var nextCount = m_UnorderedItems.Count;
            for (; i < nextCount; ++i)
                yield return m_UnorderedItems[i];

            while (context.searchInProgress)
            {
                if (nextCount < m_UnorderedItems.Count)
                {
                    nextCount = m_UnorderedItems.Count;
                    for (; i < nextCount; ++i)
                        yield return m_UnorderedItems[i];
                }
                else
                {
                    if (context.Tick())
                        yield return null; // Wait for more items...
                }
            }

            for (; i < m_UnorderedItems.Count; ++i)
                yield return m_UnorderedItems[i];
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            return Fetch().GetEnumerator();
        }

        public override void InsertRange(int index, IEnumerable<SearchItem> items)
        {
            foreach (var item in items.Where(e => e != null))
                SearchItem.Insert(m_UnorderedItems, item, SearchItem.DefaultComparer);
        }

        public override bool Remove(SearchItem item)
        {
            return m_UnorderedItems.Remove(item);
        }

        public override IEnumerable<SearchItem> GetRange(int skipCount, int count)
        {
            return m_UnorderedItems.GetRange(skipCount, count);
        }
    }

    class ConcurrentSearchList : BaseSearchList
    {
        readonly ConcurrentBag<SearchItem> m_UnorderedItems;

        bool m_SearchStarted;
        bool m_GetItemsDone;

        public ConcurrentSearchList(SearchContext searchContext) : base(searchContext)
        {
            m_UnorderedItems = new ConcurrentBag<SearchItem>();
            searchContext.sessionStarted += SearchContextOnsessionStarted;
            m_SearchStarted = searchContext.searchInProgress;
        }

        protected override void Dispose(bool disposing)
        {
            context.sessionStarted -= SearchContextOnsessionStarted;
            base.Dispose(disposing);
        }

        void SearchContextOnsessionStarted(SearchContext obj)
        {
            m_SearchStarted = true;
        }

        public void GetItemsDone()
        {
            m_GetItemsDone = true;
        }

        public override void Add(SearchItem item)
        {
            m_UnorderedItems.Add(item);
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            var addedItems = context.subset != null ? items.Intersect(context.subset) : items;
            foreach (var searchItem in addedItems)
            {
                m_UnorderedItems.Add(searchItem);
            }
        }

        public override bool Contains(SearchItem item)
        {
            return m_UnorderedItems.Contains(item);
        }

        public override void CopyTo(SearchItem[] array, int arrayIndex)
        {
            m_UnorderedItems.CopyTo(array, arrayIndex);
        }

        public override int Count => m_UnorderedItems.Count;

        public override SearchItem this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override IEnumerable<SearchItem> Fetch()
        {
            if (context == null)
                throw new Exception("Fetch can only be used if the search list was created with a search context.");

            while (!m_SearchStarted)
            {
                yield return null;
            }

            while (context.searchInProgress || !m_GetItemsDone)
            {
                if (m_UnorderedItems.TryTake(out var searchItem))
                {
                    yield return searchItem;
                }
                else
                {
                    yield return null; // Wait for more items...
                }
            }

            while (!m_UnorderedItems.IsEmpty)
            {
                if (m_UnorderedItems.TryTake(out var searchItem))
                    yield return searchItem;
            }
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            return Fetch().GetEnumerator();
        }
    }

    static class SearchListExtension
    {
        public static IEnumerable<IEnumerable<SearchItem>> Batch(this IEnumerable<SearchItem> source, int batchSize)
        {
            using (var enumerator = source.GetEnumerator())
                while (enumerator.MoveNext())
                    yield return YieldBatchElements(enumerator, batchSize - 1);
        }

        private static IEnumerable<SearchItem> YieldBatchElements(IEnumerator<SearchItem> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
                yield return source.Current;
        }
    }
}
