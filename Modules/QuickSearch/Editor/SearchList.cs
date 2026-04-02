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
    interface ISearchListComparer : IComparer<SearchItem>
    {

    }

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

        internal void SortBy(ISearchListComparer comparer);
    }

    abstract class BaseSearchList : ISearchList, IList
    {
        private bool m_Disposed = false;
        bool m_OwnsContext;

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
            : this(searchContext, trackAsyncItems, false)
        {}

        public BaseSearchList(SearchContext searchContext, bool trackAsyncItems, bool ownsContext)
        {
            context = searchContext;
            m_OwnsContext = ownsContext;
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

                if (m_OwnsContext)
                    context?.Dispose();

                m_Disposed = true;
            }
        }

        private void OnAsyncItemsReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            AddItems(items);
        }

        public IEnumerable<TResult> Select<TResult>(Func<SearchItem, TResult> selector)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return Fetch().Where(item =>
#pragma warning restore UA2001
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

        void ISearchList.SortBy(ISearchListComparer comparer)
        {
            SortBy(comparer);
        }

        public virtual void SortBy(ISearchListComparer comparer)
        {
            throw new NotSupportedException();
        }
    }

    internal enum SearchListSortingStrategy
    {
        Manual,
        OnAddItems,
        OnAddItemsThrottled
    }

    class SortedSearchList : BaseSearchList
    {
        private List<SearchItem> m_IncomingBatch;
        private HashSet<int> m_IdHashes = new HashSet<int>();
        private List<SearchItem> m_Items = new List<SearchItem>();
        private ISearchListComparer m_Comparer = new SortByScoreComparer();
        readonly SearchListSortingStrategy m_SortingStrategy;
        bool m_Sorted;
        static readonly TimeSpan k_ThrottleDelay = TimeSpan.FromSeconds(1);
        double m_ThrottleTimer;

        internal bool Sorted => m_Sorted;
        public override int Count => m_Items.Count;

        public override SearchItem this[int index]
        {
            get => m_Items[index];
            set => throw new NotSupportedException();
        }

        public SortedSearchList(SearchContext searchContext, SearchListSortingStrategy sortingStrategy = SearchListSortingStrategy.Manual) : base(searchContext)
        {
            Clear();
            m_SortingStrategy = sortingStrategy;
            m_ThrottleTimer = 0;
        }

        public void FromEnumerable(IEnumerable<SearchItem> items)
        {
            Clear();
            AddItems(items);
        }

        public SearchItem ElementAt(int index) => m_Items[index];

        public override int IndexOf(SearchItem item) => m_Items.IndexOf(item);

        public override IEnumerable<SearchItem> Fetch()
        {
            if (context == null)
                throw new Exception("Fetch can only be used if the search list was created with a search context.");

            bool trackIncomingItems = false;
            if (context.searchInProgress)
            {
                trackIncomingItems = true;
                m_IncomingBatch = new List<SearchItem>();
            }

            // m_Items could be modified while yielding, copy the data
            // since items can be inserted anywhere in the list.
            var itemsCopy = m_Items.ToArray();
            foreach (var item in itemsCopy)
                yield return item;

            int i = 0;
            var nextCount = 0;
            while (context.searchInProgress)
            {
                if (context.Tick())
                    yield return null;

                if (trackIncomingItems && nextCount < m_IncomingBatch.Count)
                {
                    // m_IncomingBatch could be modified while yielding items. Do not use foreach.
                    nextCount = m_IncomingBatch.Count;
                    for (; i < nextCount; ++i)
                        yield return m_IncomingBatch[i];
                }
            }

            if (trackIncomingItems)
            {
                for (; i < m_IncomingBatch.Count; ++i)
                    yield return m_IncomingBatch[i];
                m_IncomingBatch = null;
            }
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            foreach (var item in items)
                Add(item);

            if (!Sorted)
            {
                if (m_SortingStrategy == SearchListSortingStrategy.OnAddItems)
                {
                    Sort();
                }
                else if (m_SortingStrategy == SearchListSortingStrategy.OnAddItemsThrottled)
                {
                    if (Delayer.ThrottleTrigger(ref m_ThrottleTimer, k_ThrottleDelay))
                    {
                        Sort();
                    }
                }
            }
        }

        public override void Clear()
        {
            m_Items.Clear();
            m_IdHashes.Clear();
            m_IncomingBatch?.Clear();
            m_Sorted = true;
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            return Fetch().GetEnumerator();
        }

        public override IEnumerable<SearchItem> GetRange(int skipCount, int count)
        {
            return m_Items.GetRange(skipCount, count);
        }

        public override void InsertRange(int index, IEnumerable<SearchItem> items)
        {
            foreach (var e in items)
                Add(e);
        }

        private bool AddItem(in SearchItem item)
        {
            if (item == null)
                return false;

            var itemHash = item.GetHashCode();
            if (m_IdHashes.Contains(itemHash))
            {
                // TODO: We should revisit this. There is an inconsistency in how we deal with ordering and score.
                // If an item has the same id, a higher score (high score=sorted last) but sorted first based on the sorting method, it will not be replaced by the new item.
                // Which is ok if we consider that we want to keep the first result based on the sorting method. However,
                // if the existing item is sorted last, but has a lower score (lower score=sorted first), it will not be replaced and the new item will not be added, so the behavior
                // is not the same as the other case.

                var itemIndex = -1;
                if (m_Sorted)
                {
                    var startIndex = m_Items.BinarySearch(item, m_Comparer);
                    if (startIndex < 0)
                        startIndex = ~startIndex;
                    itemIndex = m_Items.IndexOf(item, Math.Max(startIndex - 1, 0));
                }
                else
                {
                    itemIndex = m_Items.IndexOf(item);
                }
                if (itemIndex >= 0 && item.score < m_Items[itemIndex].score)
                {
                    m_Items.RemoveAt(itemIndex);
                    m_IdHashes.Remove(itemHash);
                }
                else
                    return false;
            }

            m_Items.Add(item);
            m_IdHashes.Add(itemHash);
            m_Sorted = false;

            return true;
        }

        public override void Add(SearchItem item)
        {
            if (AddItem(item) && m_IncomingBatch != null)
                m_IncomingBatch.Add(item);
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
            return m_IdHashes.Contains(item.GetHashCode());
        }

        public override bool Remove(SearchItem item)
        {
            if (m_IdHashes.Remove(item.GetHashCode()))
                return m_Items.Remove(item);
            return false;
        }

        public void Sort()
        {
            if (m_Sorted)
                return;
            m_Items.Sort(m_Comparer);
            m_Sorted = true;
        }
    }

    interface IGroup
    {
        string id { get; }
        string name { get; }
        string type { get; }
        int count { get; }
        int priority { get; }
        bool optional { get; set; }
        IEnumerable<SearchItem> items { get; }

        SearchItem ElementAt(int index);
        int IndexOf(SearchItem item);
        bool Add(SearchItem item);
        void Sort();
        void SortBy(ISearchListComparer comparer);
        void Clear();
        void SetSearchListComparer(ISearchListComparer comparer);
    }

    class GroupedSearchList : BaseSearchList
    {
        internal class Group : IGroup
        {
            public string id { get; private set; }
            public string name { get; private set; }
            public string type { get; private set; }
            public bool optional { get; set; }
            public IEnumerable<SearchItem> items => m_Items;
            public int count => m_Items.Count;
            public bool sorted;

            public int priority { get; set; }

            private HashSet<int> m_IdHashes;
            private List<SearchItem> m_Items;
            private ISearchListComparer m_Comparer;

            public Group(string id, string type, string name, ISearchListComparer comparer, int priority = int.MaxValue)
            {
                this.id = id;
                this.name = name;
                this.type = type;
                this.priority = priority;
                this.optional = true;
                m_Items = new List<SearchItem>();
                m_IdHashes = new HashSet<int>();
                m_Comparer = comparer;
                sorted = true;
            }

            public void Clear()
            {
                m_Items.Clear();
                m_IdHashes.Clear();
                sorted = true;
            }

            public SearchItem ElementAt(int index)
            {
                return m_Items[index];
            }

            public bool Add(SearchItem item)
            {
                var itemHash = item.GetHashCode();
                if (m_IdHashes.Contains(itemHash))
                {
                    // TODO: We should revisit this. There is an inconsistency in how we deal with ordering and score.
                    // If an item has the same id, a higher score (high score=sorted last) but sorted first based on the sorting method, it will not be replaced by the new item.
                    // Which is ok if we consider that we want to keep the first result based on the sorting method. However,
                    // if the existing item is sorted last, but has a lower score (lower score=sorted first), it will not be replaced and the new item will not be added, so the behavior
                    // is not the same as the other case.

                    var itemIndex = -1;
                    if (sorted)
                    {
                        var startIndex = m_Items.BinarySearch(item, m_Comparer);
                        if (startIndex < 0)
                            startIndex = ~startIndex;
                        itemIndex = m_Items.IndexOf(item, Math.Max(startIndex - 1, 0));
                    }
                    else
                    {
                        itemIndex = m_Items.IndexOf(item);
                    }

                    if (itemIndex >= 0 && item.score < m_Items[itemIndex].score)
                    {
                        m_Items.RemoveAt(itemIndex);
                        m_IdHashes.Remove(itemHash);
                    }
                    else
                        return false;
                }

                m_Items.Add(item);
                m_IdHashes.Add(itemHash);
                sorted = false;

                return true;
            }

            public int IndexOf(SearchItem item)
            {
                return m_Items.IndexOf(item);
            }

            public override string ToString()
            {
                return $"{id} ({m_Items.Count})";
            }

            public void Sort()
            {
                if (m_Comparer == null)
                    return;

                m_Items.Sort(m_Comparer);
                sorted = true;
            }

            void IGroup.SortBy(ISearchListComparer comparer)
            {
                if (comparer == null)
                    return;

                if (comparer.GetHashCode() == m_Comparer.GetHashCode())
                    return;

                m_Comparer = comparer;
                m_Items.Sort(comparer);
                sorted = true;
            }

            public void SetSearchListComparer(ISearchListComparer comparer)
            {
                if (comparer.GetHashCode() == m_Comparer.GetHashCode())
                    return;
                m_Comparer = comparer;
                sorted = false;
            }
        }

        internal const string allGroupId = "all";
        private int m_CurrentGroupIndex = 0;
        private string m_CurrentGroupId;
        private readonly List<IGroup> m_Groups = new List<IGroup>();
        private ISearchListComparer m_SearchListComparer;
        readonly SearchListSortingStrategy m_SortingStrategy;
        double m_ThrottlerTimer;
        static readonly TimeSpan k_ThrottleDelay = TimeSpan.FromSeconds(1);

        public bool Sorted => m_Groups.TrueForAll(g => ((Group)g).sorted);
        public override int Count => m_Groups[m_CurrentGroupIndex >= 0 ? m_CurrentGroupIndex : 0].count;
        public int TotalCount => m_Groups[0].count;

        public ISearchListComparer SearchListComparer => m_SearchListComparer;

        IGroup CreateGroup(string id, string type, string name, ISearchListComparer comparer, int priority = int.MaxValue, bool optional = true)
        {
            return new Group(id, type, name, comparer, priority) { optional = optional };
        }

        public override SearchItem this[int index]
        {
            get => ElementAt(index);
            set => throw new NotSupportedException();
        }

        public GroupedSearchList(SearchContext searchContext, SearchListSortingStrategy sortingStrategy = SearchListSortingStrategy.Manual)
            : this(searchContext, null, sortingStrategy)
        {
        }

        public GroupedSearchList(SearchContext searchContext, ISearchListComparer searchListComparer, SearchListSortingStrategy sortingStrategy = SearchListSortingStrategy.Manual)
            : base(searchContext, false)
        {
            m_SortingStrategy = sortingStrategy;
            m_SearchListComparer = searchListComparer ?? new SortByScoreComparer();
            m_ThrottlerTimer = 0;
            Clear();
        }

        public override int IndexOf(SearchItem item)
        {
            return m_Groups[m_CurrentGroupIndex].IndexOf(item);
        }

        public SearchItem ElementAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException($"Failed to access item {index} within {Count}/{TotalCount} items", nameof(index));

            return m_Groups[m_CurrentGroupIndex].ElementAt(index);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public int GetGroupCount(bool showAll = true)
        {
            return m_Groups.Count + (showAll ? 0 : -1);
        }

        public IGroup GetGroupById(string groupId)
        {
            return m_Groups.Find(group => group.id == groupId);
        }

        internal IEnumerable<IGroup> GetGroupByType(string groupType)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_Groups.Where(group => group.type == groupType);
#pragma warning restore UA2001
        }

        public IEnumerable<IGroup> EnumerateGroups(bool showAll = true)
        {
            if (m_Groups.Count == 2 && m_Groups[0].count == m_Groups[1].count)
                yield return m_Groups[1];
            else
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var g in m_Groups.Skip(showAll ? 0 : 1).Where(g => !g.optional || g.count > 0))
#pragma warning restore UA2001
                    yield return g;
            }
        }

        public IEnumerable<SearchItem> GetAll()
        {
            return m_Groups[0].items;
        }

        public override IEnumerable<SearchItem> Fetch()
        {
            if (context == null)
                throw new Exception("Fetch can only be used if the search list was created with a search context.");

            while (context.searchInProgress)
            {
                yield return null;
                Dispatcher.ProcessOne();
            }

            foreach (var item in m_Groups[m_CurrentGroupIndex].items)
                yield return item;
        }

        private void AddDefaultGroups()
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var defaultGroups = context.providers
#pragma warning restore UA2001
                .Where(p => p.showDetailsOptions.HasFlag(ShowDetailsOptions.DefaultGroup))
                .Select(p => CreateGroup(p.id, p.type, p.name, m_SearchListComparer, p.priority, optional: false));
            m_Groups.AddRange(defaultGroups);
            m_Groups.Sort((lhs, rhs) => lhs.priority.CompareTo(rhs.priority));
        }

        public IGroup AddGroup(SearchProvider searchProvider)
        {
            var itemGroup = m_Groups.Find(g => string.Equals(g.id, searchProvider.id, StringComparison.Ordinal));
            if (itemGroup != null)
                return itemGroup;
            itemGroup = CreateGroup(searchProvider.id, searchProvider.type, searchProvider.name, m_SearchListComparer, searchProvider.priority);
            m_Groups.Add(itemGroup);
            m_Groups.Sort((lhs, rhs) => lhs.priority.CompareTo(rhs.priority));
            if (!string.IsNullOrEmpty(m_CurrentGroupId))
                m_CurrentGroupIndex = m_Groups.FindIndex(g => g.id == m_CurrentGroupId);
            if (m_CurrentGroupIndex == -1)
                m_CurrentGroupIndex = 0;
            return itemGroup;
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            foreach (var item in items)
                Add(item);

            SortOnAddItems();
            RestoreCurrentGroupIfNeeded();
        }

        public void AddItem(SearchItem item)
        {
            Add(item);

            SortOnAddItems();
            RestoreCurrentGroupIfNeeded();
        }

        public override void Clear()
        {
            if (m_Groups.Count == 0)
            {
                m_CurrentGroupIndex = 0;
                m_Groups.Add(CreateGroup(allGroupId, allGroupId, "All", m_SearchListComparer, int.MinValue));
                AddDefaultGroups();
            }
            else
            {
                foreach (var g in m_Groups)
                    g.Clear();
            }
        }

        public override IEnumerator<SearchItem> GetEnumerator()
        {
            return m_Groups[m_CurrentGroupIndex].items.GetEnumerator();
        }

        public override void Add(SearchItem item)
        {
            var itemGroup = AddGroup(item.provider);
            if (itemGroup.Add(item))
                m_Groups[0].Add(item);
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
                return m_CurrentGroupId ?? allGroupId;
            }

            set
            {
                if (value == null || !RestoreCurrentGroup(value))
                {
                    m_CurrentGroupId = value;
                    m_CurrentGroupIndex = 0;
                }
            }
        }

        internal IGroup GetCurrentGroup()
        {
            return m_Groups[m_CurrentGroupIndex];
        }

        private bool RestoreCurrentGroup(string groupId)
        {
            m_CurrentGroupIndex = m_Groups.FindIndex(g => g.id == groupId);
            if (m_CurrentGroupIndex != -1)
                m_CurrentGroupId = groupId;
            return m_CurrentGroupIndex != -1;
        }

        void RestoreCurrentGroupIfNeeded()
        {
            if (m_CurrentGroupIndex == -1 && !string.IsNullOrEmpty(m_CurrentGroupId))
                RestoreCurrentGroup(m_CurrentGroupId);
        }

        internal int GetItemCount(IEnumerable<string> activeProviderTypes)
        {
            int queryItemCount = 0;

#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (activeProviderTypes == null || !activeProviderTypes.Any())
#pragma warning restore UA2002
                return TotalCount;

            foreach (var providerType in activeProviderTypes)
            {
                var groupsWithType = GetGroupByType(providerType);
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (groupsWithType == null || !groupsWithType.Any())
#pragma warning restore UA2002
                    continue;

                foreach (var group in groupsWithType)
                    queryItemCount += group.count;
            }

            return queryItemCount;
        }

        public void Sort()
        {
            if (!((Group)m_Groups[m_CurrentGroupIndex]).sorted)
                m_Groups[m_CurrentGroupIndex].Sort();
        }

        public void SortAllGroups()
        {
            foreach (var group in m_Groups)
            {
                if (!((Group)group).sorted)
                    group.Sort();
            }
        }

        public override void SortBy(ISearchListComparer comparer)
        {
            if (comparer == m_SearchListComparer)
                return;

            foreach (var group in m_Groups)
            {
                group.SetSearchListComparer(comparer);
            }
            m_SearchListComparer = comparer;
            m_Groups[m_CurrentGroupIndex].Sort();
        }

        void SortOnAddItems()
        {
            if (!Sorted)
            {
                if (m_SortingStrategy == SearchListSortingStrategy.OnAddItems)
                {
                    SortAllGroups();
                }
                else if (m_SortingStrategy == SearchListSortingStrategy.OnAddItemsThrottled)
                {
                    if (Delayer.ThrottleTrigger(ref m_ThrottlerTimer, k_ThrottleDelay))
                    {
                        SortAllGroups();
                    }
                }
            }
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

        public AsyncSearchList(SearchContext searchContext, bool ownsContext) : base(searchContext, true, ownsContext)
        {
            m_UnorderedItems = new List<SearchItem>();
        }

        public override void Add(SearchItem item)
        {
            m_UnorderedItems.Add(item);
        }

        public override void AddItems(IEnumerable<SearchItem> items)
        {
            m_UnorderedItems.AddRange(items);
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
            m_UnorderedItems.InsertRange(index, items);
        }

        public override int IndexOf(SearchItem item)
        {
            return m_UnorderedItems.IndexOf(item);
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

        public ConcurrentSearchList(SearchContext searchContext, bool ownsContext) : base(searchContext, true, ownsContext)
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
            var addedItems = items;
            foreach (var searchItem in addedItems)
            {
                m_UnorderedItems.Add(searchItem);
            }
        }

        public override bool Contains(SearchItem item)
        {
            #pragma warning disable UA2007 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_UnorderedItems.Contains(item);
#pragma warning restore UA2007
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
