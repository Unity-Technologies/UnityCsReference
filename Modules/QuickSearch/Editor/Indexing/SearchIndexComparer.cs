// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    internal enum SearchIndexOperator
    {
        Contains,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,

        DoNotCompareScore,

        None
    }

    class SearchIndexComparer : IComparer<SearchIndexEntry>, IEqualityComparer<SearchIndexEntry>
    {
        public SearchIndexOperator op { get; private set; }

        public SearchIndexComparer(SearchIndexOperator op = SearchIndexOperator.Contains)
        {
            this.op = op;
        }

        // TODO: Setting SearchIndexOperator.DoNotCompareScore messes number comparisons
        public int Compare(SearchIndexEntry item1, SearchIndexEntry item2)
        {
            var c = ((byte)item1.type).CompareTo((byte)item2.type);
            if (c != 0)
                return c;
            c = item1.crc.CompareTo(item2.crc);
            if (c != 0)
                return c;

            if (item1.type == SearchIndexEntry.Type.Number)
            {
                double eps = 0.0;
                if (op == SearchIndexOperator.Less)
                    eps = -Utils.DOUBLE_EPSILON;
                else if (op == SearchIndexOperator.Greater)
                    eps = Utils.DOUBLE_EPSILON;
                else if (op == SearchIndexOperator.Equal || op == SearchIndexOperator.Contains || op == SearchIndexOperator.GreaterOrEqual || op == SearchIndexOperator.LessOrEqual)
                {
                    if (Utils.Approximately(item1.number, item2.number))
                        return 0;
                }
                c = item1.number.CompareTo(item2.number + eps);
            }
            else
                c = item1.key.CompareTo(item2.key);
            if (c != 0 || op == SearchIndexOperator.DoNotCompareScore)
                return c;

            if (item2.score == int.MaxValue || item1.score == int.MaxValue)
                return 0;
            return item1.score.CompareTo(item2.score);
        }

        public bool Equals(SearchIndexEntry x, SearchIndexEntry y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(SearchIndexEntry obj)
        {
            return obj.GetHashCode();
        }
    }

    struct SearchIndexEntryExactComparer : IComparer<SearchIndexEntry>
    {
        public int Compare(SearchIndexEntry x, SearchIndexEntry y)
        {
            var keyComparison = x.key.CompareTo(y.key);
            if (keyComparison != 0) return keyComparison;
            var crcComparison = x.crc.CompareTo(y.crc);
            if (crcComparison != 0) return crcComparison;
            var typeComparison = ((byte)x.type).CompareTo((byte)y.type);
            return typeComparison;
        }
    }
}
