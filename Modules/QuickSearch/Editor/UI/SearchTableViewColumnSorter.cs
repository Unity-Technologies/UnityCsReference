// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class BaseSearchListComparer : ISearchListComparer
    {
        protected virtual bool UseProviderPriority => true;

        public virtual int Compare(SearchItem x, SearchItem y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null) return 2;
            if (y == null) return -2;

            var compareFavoriteStateResult = CompareFavoriteState(x, y);
            if (compareFavoriteStateResult != 0)
                return compareFavoriteStateResult;

            var xa = x.options.HasAny(SearchItemOptions.CustomAction);
            var ya = y.options.HasAny(SearchItemOptions.CustomAction);

            var p = UseProviderPriority ? CompareByProviderPriority(x, y) : 0;
            if (xa && ya)
            {
                if (UseProviderPriority && p != 0)
                    return p;
                return BaseCompare(x, y);
            }

            if (xa) return -3;
            if (ya) return 3;

            if (UseProviderPriority && p != 0)
                return p;
            return CompareItems(x, y);
        }

        public int CompareFavoriteState(SearchItem x, SearchItem y)
        {
            var xIsFavorite = SearchSettings.searchItemFavorites.Contains(x.id);
            var yIsFavorite = SearchSettings.searchItemFavorites.Contains(y.id);

            if (xIsFavorite && yIsFavorite)
                return 0;

            if (!xIsFavorite && !yIsFavorite)
                return 0;

            if (xIsFavorite)
                return -1;

            return 1;
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }

        public static int BaseCompare(in SearchItem x, in SearchItem y)
        {
            int c = x.score.CompareTo(y.score);
            if (c != 0)
                return c;
            return string.CompareOrdinal(x.id, y.id);
        }

        public static int CompareByProviderPriority(in SearchItem x, in SearchItem y)
        {
            var px = x.provider;
            var py = y.provider;

            if (px == null && py == null) return 0;
            if (px == null) return 1;
            if (py == null) return -1;
            return px.priority.CompareTo(py.priority);
        }

        public virtual int CompareItems(in SearchItem x, in SearchItem y)
        {
            return BaseCompare(x, y);
        }

        protected static int DefaultCompare(object lhsv, object rhsv, bool sortAscending = true)
        {
            var sortOrder = sortAscending ? 1 : -1;

            if (lhsv is string s)
                return string.Compare(s, rhsv?.ToString(), StringComparison.OrdinalIgnoreCase) * sortOrder;
            if (lhsv is IComparable c)
                return Comparer.Default.Compare(c, rhsv) * sortOrder;
            return Comparer.Default.Compare(lhsv?.GetHashCode() ?? 0, rhsv?.GetHashCode() ?? 0) * sortOrder;
        }
    }

    class SortByScoreComparer : BaseSearchListComparer
    {
    }

    class SortByLabelComparer : BaseSearchListComparer
    {
        public override int CompareItems(in SearchItem x, in SearchItem y)
        {
            var compareFavoriteStateResult = CompareFavoriteState(x, y);
            if (compareFavoriteStateResult != 0)
                return compareFavoriteStateResult;

            var xl = x.GetLabel(x.context);
            var yl = y.GetLabel(y.context);
            int c = string.Compare(xl, yl, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
                return c;
            return base.CompareItems(x, y);
        }
    }

    class SortByNameComparer : SortBySelector
    {
        public SortByNameComparer()
            : base(new SearchSelector(new Regex("name"), 0, null, true, SelectName, "Name", cacheable: true))
        {

        }

        private static object SelectName(SearchSelectorArgs args)
        {
            var item = args.current;
            if (string.CompareOrdinal(item.provider.id, "asset") == 0)
            {
                if (item.data is Providers.AssetProvider.AssetMetaInfo info)
                {
                    if (!string.IsNullOrEmpty(info.path))
                        return System.IO.Path.GetFileName(info.path);

                    return info.obj?.name;
                }
            }
            else if (string.CompareOrdinal(item.provider.id, "scene") == 0)
            {
                var go = item.ToObject<UnityEngine.GameObject>();
                if (!go)
                    return null;
                return go.name;
            }

            return item.GetLabel(item.context);
        }
    }

    class SortByDescriptionComparer : BaseSearchListComparer
    {
        public override int CompareItems(in SearchItem x, in SearchItem y)
        {
            var compareFavoriteStateResult = CompareFavoriteState(x, y);
            if (compareFavoriteStateResult != 0)
                return compareFavoriteStateResult;

            var xl = x.GetDescription(x.context);
            var yl = y.GetDescription(y.context);
            int c = string.Compare(xl, yl, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
                return c;
            return base.CompareItems(x, y);
        }
    }

    class SortBySelector : BaseSearchListComparer
    {
        private readonly SearchSelector selector;

        public SortBySelector(SearchSelector selector)
        {
            this.selector = selector;
        }

        public override int CompareItems(in SearchItem x, in SearchItem y)
        {
            var compareFavoriteStateResult = CompareFavoriteState(x, y);
            if (compareFavoriteStateResult != 0)
                return compareFavoriteStateResult;

            var xs = selector.select(new SearchSelectorArgs(default, x));
            var ys = selector.select(new SearchSelectorArgs(default, y));

            if (xs == null && ys == null)
                return 0;

            if (xs == null) return 2;
            if (ys == null) return -2;

            var c = DefaultCompare(xs, ys, sortAscending: true);
            if (c != 0) return c;
            return base.CompareItems(x, y);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(selector.label, base.GetHashCode());
        }
    }

    class SearchTableViewColumnSorter : BaseSearchListComparer
    {
        readonly struct CD
        {
            public readonly SortDirection direction;
            public readonly SearchColumn.CompareEntry comparer;
            public readonly SearchColumn column;

            public CD(SearchColumn column, SortDirection direction)
            {
                this.direction = direction;
                this.column = column;
                this.comparer = column.comparer;
            }
        }

        readonly struct ValueKey : IEquatable<ValueKey>
        {
            public readonly ulong itemKey;
            public readonly int columnKey;

            public ValueKey(ulong itemKey, int columnKey)
            {
                this.itemKey = itemKey;
                this.columnKey = columnKey;
            }

            public override string ToString() => $"{columnKey} > {itemKey}";
            public override int GetHashCode() => HashCode.Combine(itemKey, columnKey);
            public override bool Equals(object other) => other is ValueKey l && Equals(l);
            public bool Equals(ValueKey other) => columnKey == other.columnKey && itemKey == other.itemKey;
        }

        protected override bool UseProviderPriority => m_Comparers.Count == 0;
        private readonly List<CD> m_Comparers = new List<CD>();
        private readonly Dictionary<ValueKey, object> m_ValueCache = new Dictionary<ValueKey, object>();

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var comp in m_Comparers)
                hash = HashCode.Combine(hash, comp.column.path, comp.direction);
            return hash;
        }

        public override int CompareItems(in SearchItem x, in SearchItem y)
        {
            if (m_Comparers.Count == 0)
                return BaseCompare(x, y);

            foreach (var comp in m_Comparers)
            {
                var lhs = new SearchColumnEventArgs(x, x.context, comp.column);
                var rhs = new SearchColumnEventArgs(y, y.context, comp.column);
                lhs.value = GetValue(lhs);
                rhs.value = GetValue(rhs);

                if (lhs.value == null && rhs.value == null)
                    continue;

                if (lhs.value == null) return 2;
                if (rhs.value == null) return -2;

                var compareArgs = new SearchColumnCompareArgs(lhs, rhs, comp.direction == SortDirection.Ascending);
                var cc = (comp.comparer ?? DefaultCompare)(compareArgs);
                if (cc != 0)
                    return cc;
            }

            // We come here if, for all comparers, both values were null or equal.
            // So try one more time with BaseCompare.
            var p = CompareByProviderPriority(x, y);
            if (p != 0)
                return p;
            return BaseCompare(x, y);
        }

        object GetValue(in SearchColumnEventArgs args)
        {
            var key = new ValueKey(args.item.id.GetHashCode64(), args.column.path.GetHashCode());
            if (m_ValueCache.TryGetValue(key, out var value))
                return value;
            value = args.column.getter(args);
            m_ValueCache[key] = value;
            return value;
        }

        int DefaultCompare(SearchColumnCompareArgs args)
        {
            var lhsv = args.lhs.value;
            var rhsv = args.rhs.value;
            return DefaultCompare(lhsv, rhsv, args.sortAscending);
        }

        public void Add(SearchColumn column, SortDirection direction)
        {
            m_Comparers.Add(new CD(column, direction));
        }
    }
}
