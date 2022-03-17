// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Search
{
    delegate void CountLengthToEndOfBlock(string baseString, int[] counts, int startIndex, int endIndex);

    /// <summary>
    /// Structure that holds a view on a subset string, starting with a specified range of [startIndex, endIndex[.
    /// The subset keeps the order unchanged.
    /// </summary>
    struct SubsetStringView : IStringView
    {
        readonly string m_BaseString;
        readonly int m_BaseStringLength;

        List<int> m_Indexes;
        readonly int[] m_Counts;

        public static readonly SubsetStringView Empty = new SubsetStringView(string.Empty);

        public bool valid => m_BaseString != null;
        public string baseString => m_BaseString;
        public int length => m_Indexes?.Count ?? 0;

        public char this[int index] => m_BaseString[m_Indexes[index]];

        public SubsetStringView(string baseString)
            : this(baseString, 0, baseString.Length, CountLengthToNextWhiteSpace)
        {}

        public SubsetStringView(string baseString, CountLengthToEndOfBlock counter)
            : this(baseString, 0, baseString.Length, counter)
        {}

        public SubsetStringView(string baseString, int startIndex)
            : this(baseString, startIndex, baseString.Length, CountLengthToNextWhiteSpace)
        {}

        public SubsetStringView(string baseString, int startIndex, CountLengthToEndOfBlock counter)
            : this(baseString, startIndex, baseString.Length, counter)
        {}

        public SubsetStringView(string baseString, int startIndex, int endIndex)
            : this(baseString, startIndex, endIndex, CountLengthToNextWhiteSpace)
        {}

        public SubsetStringView(string baseString, int startIndex, int endIndex, CountLengthToEndOfBlock counter)
        {
            if (startIndex < 0 || (!string.IsNullOrEmpty(baseString) && startIndex >= baseString.Length) || (string.IsNullOrEmpty(baseString) && startIndex != 0))
                throw new ArgumentException("Index out of string range", nameof(startIndex));
            if (endIndex < 0 || (!string.IsNullOrEmpty(baseString) && endIndex > baseString.Length) || (string.IsNullOrEmpty(baseString) && endIndex != 0))
                throw new ArgumentException("Index out of string range", nameof(endIndex));
            if (endIndex < startIndex)
                throw new ArgumentException("Incorrect range specified. End index should not be smaller than start index.", nameof(endIndex));

            m_BaseString = baseString;
            m_BaseStringLength = baseString?.Length ?? 0;

            var length = endIndex - startIndex;
            m_Counts = new int[length];
            m_Indexes = new List<int>(length);

            for (var i = startIndex; i < endIndex; ++i)
            {
                m_Indexes.Add(i);
            }

            counter(baseString, m_Counts, startIndex, endIndex);
        }

        SubsetStringView(string baseString, List<int> indexes, int[] counts)
        {
            m_BaseString = baseString;
            m_BaseStringLength = baseString?.Length ?? 0;
            m_Indexes = indexes ?? throw new ArgumentNullException(nameof(indexes));
            m_Counts = counts ?? throw new ArgumentNullException(nameof(counts));
        }

        /// <summary>
        /// Replaces the block of text in the range [startIndex, endIndex[ for the range [newStartIndex, newEndIndex[.
        /// </summary>
        /// <param name="startIndex">First index of the block to replace.</param>
        /// <param name="endIndex">Index after the last character to replace.</param>
        /// <param name="newStartIndex">First index of the new range.</param>
        /// <param name="newEndIndex">Index after the last character of the new range.</param>
        /// TODO: Add a parameter to support specifying if the indexes are on the original string or the string view
        public void ReplaceWithSubset(int startIndex, int endIndex, int newStartIndex, int newEndIndex)
        {
            if (startIndex < 0 || startIndex >= m_BaseStringLength ||
                endIndex < 0 || endIndex > m_BaseStringLength || endIndex <= startIndex ||
                newStartIndex < startIndex || newStartIndex > endIndex ||
                newEndIndex <= startIndex || newEndIndex > endIndex || newEndIndex < newStartIndex)
                return;

            var removeRangeStartIndex = -1;
            for (var i = 0; i < length; ++i)
            {
                var currentIndex = m_Indexes[i];
                if (currentIndex < startIndex)
                    continue;

                if (currentIndex < newStartIndex && removeRangeStartIndex == -1)
                    removeRangeStartIndex = i;

                if (currentIndex == newStartIndex && removeRangeStartIndex != -1)
                {
                    var count = i - removeRangeStartIndex;
                    m_Indexes.RemoveRange(removeRangeStartIndex, count);
                    i -= count;
                    removeRangeStartIndex = -1;
                }

                if (currentIndex < newEndIndex)
                    continue;

                if (currentIndex >= newEndIndex && removeRangeStartIndex == -1)
                    removeRangeStartIndex = i;

                if (currentIndex >= endIndex)
                {
                    if (removeRangeStartIndex != -1)
                    {
                        var count = i - removeRangeStartIndex;
                        m_Indexes.RemoveRange(removeRangeStartIndex, count);
                        removeRangeStartIndex = -1;
                    }
                    break;
                }
            }

            // If endIndex and newEndIndex are = length, the loop will breakout before
            // we can remove anything.
            if (removeRangeStartIndex != -1)
            {
                var count = length - removeRangeStartIndex;
                m_Indexes.RemoveRange(removeRangeStartIndex, count);
            }
        }

        public int LengthToEndOfBlock(int index)
        {
            var realIndex = m_Indexes[index];
            return m_Counts[realIndex];
        }

        public SubsetStringView Substring(int start)
        {
            if (start >= length)
                return Empty;

            return Substring(start, length - start);
        }

        IStringView IStringView.Substring(int start)
        {
            return Substring(start);
        }

        public SubsetStringView Substring(int start, int length)
        {
            if (start < 0 || start >= this.length)
                throw new ArgumentException("Index out of string range", nameof(start));

            var end = start + length;
            if (end > this.length)
                throw new ArgumentException("Index out of string range", nameof(length));

            var indexes = new List<int>(length);
            for (var i = start; i < end; ++i)
            {
                indexes.Add(m_Indexes[i]);
            }
            return new SubsetStringView(m_BaseString, indexes, m_Counts);
        }

        IStringView IStringView.Substring(int start, int length)
        {
            return Substring(start, length);
        }

        public SubsetStringView Trim(params char[] chrs)
        {
            FindTrimStartEnd(0, length, chrs, out var start, out var end);

            var indexes = new List<int>(end - start);
            for (var i = start; i < end; ++i)
            {
                indexes.Add(m_Indexes[i]);
            }
            return new SubsetStringView(m_BaseString, indexes, m_Counts);
        }

        IStringView IStringView.Trim(params char[] chrs)
        {
            return Trim(chrs);
        }

        void FindTrimStartEnd(int localStart, int localEnd, char[] chrs, out int trimStart, out int trimEnd)
        {
            trimStart = localStart;
            trimEnd = localEnd;
            for (; trimStart < localEnd;)
            {
                var globalIndex = m_Indexes[trimStart];
                var c = baseString[globalIndex];
                if ((chrs != null && chrs.Length > 0 && Array.IndexOf(chrs, c) != -1) || char.IsWhiteSpace(c))
                    trimStart++;
                else
                    break;
            }

            for (; trimEnd > trimStart;)
            {
                var globalIndex = m_Indexes[trimEnd - 1];
                var c = baseString[globalIndex];
                if ((chrs != null && chrs.Length > 0 && Array.IndexOf(chrs, c) != -1) || char.IsWhiteSpace(c))
                    trimEnd--;
                else
                    break;
            }
        }

        public int IndexOf(IStringView other, StringComparison sc = StringComparison.Ordinal)
        {
            if (!valid || !other.valid)
                return -1;
            if (length < other.length)
                return -1;

            int foundStartIndex = -1;
            int otherIndex = 0;
            for (var i = 0; i < length && otherIndex < other.length; ++i)
            {
                if (!StringView.Compare(this[i], other[otherIndex], sc))
                {
                    if (foundStartIndex > -1)
                    {
                        foundStartIndex = -1;
                        otherIndex = 0;
                    }
                }
                else
                {
                    if (foundStartIndex == -1)
                        foundStartIndex = i;
                    otherIndex++;
                }
            }

            if (otherIndex != other.length)
                return -1;
            return foundStartIndex;
        }

        public int IndexOf(SubsetStringView other, StringComparison sc = StringComparison.Ordinal)
        {
            return IndexOf(this, other, sc);
        }

        public int IndexOf(string other, StringComparison sc = StringComparison.Ordinal)
        {
            // Use a StringView here, we don't want to allocate for nothing.
            return IndexOf(this, new StringView(other), sc);
        }

        public int IndexOf(char other, StringComparison sc = StringComparison.Ordinal)
        {
            return IndexOf(this, other, sc);
        }

        static int IndexOf(SubsetStringView source, SubsetStringView other, StringComparison sc)
        {
            if (!source.valid || !other.valid)
                return -1;
            if (source.length < other.length)
                return -1;

            int foundStartIndex = -1;
            int otherIndex = 0;
            for (var i = 0; i < source.length && otherIndex < other.length; ++i)
            {
                if (!StringView.Compare(source[i], other[otherIndex], sc))
                {
                    if (foundStartIndex > -1)
                    {
                        foundStartIndex = -1;
                        otherIndex = 0;
                    }
                }
                else
                {
                    if (foundStartIndex == -1)
                        foundStartIndex = i;
                    otherIndex++;
                }
            }

            if (otherIndex != other.length)
                return -1;
            return foundStartIndex;
        }

        static int IndexOf(SubsetStringView source, StringView other, StringComparison sc)
        {
            if (!source.valid || !other.valid)
                return -1;
            if (source.length < other.length)
                return -1;

            int foundStartIndex = -1;
            int otherIndex = 0;
            for (var i = 0; i < source.length && otherIndex < other.length; ++i)
            {
                if (!StringView.Compare(source[i], other[otherIndex], sc))
                {
                    if (foundStartIndex > -1)
                    {
                        foundStartIndex = -1;
                        otherIndex = 0;
                    }
                }
                else
                {
                    if (foundStartIndex == -1)
                        foundStartIndex = i;
                    otherIndex++;
                }
            }

            if (otherIndex != other.length)
                return -1;
            return foundStartIndex;
        }

        static int IndexOf(SubsetStringView source, char other, StringComparison sc)
        {
            if (!source.valid)
                return -1;

            for (var i = 0; i < source.length; ++i)
            {
                if (StringView.Compare(source[i], other, sc))
                    return i;
            }

            return -1;
        }

        public int LastIndexOf(IStringView other, StringComparison sc = StringComparison.Ordinal)
        {
            if (length < other.length)
                return -1;

            int otherIndex = other.length - 1;
            for (var i = length - 1; i >= 0 && otherIndex >= 0; --i)
            {
                if (!StringView.Compare(this[i], other[otherIndex], sc))
                {
                    otherIndex = other.length - 1;
                }
                else
                {
                    if (otherIndex == 0)
                        return i;
                    otherIndex--;
                }
            }

            return -1;
        }

        public int LastIndexOf(SubsetStringView other, StringComparison sc = StringComparison.Ordinal)
        {
            return LastIndexOf(this, other, sc);
        }

        public int LastIndexOf(string other, StringComparison sc = StringComparison.Ordinal)
        {
            // Use a StringView here, we don't want to allocate for nothing.
            return LastIndexOf(this, new StringView(other), sc);
        }

        public int LastIndexOf(char other, StringComparison sc = StringComparison.Ordinal)
        {
            return LastIndexOf(this, other, sc);
        }

        static int LastIndexOf(SubsetStringView source, SubsetStringView other, StringComparison sc)
        {
            if (source.length < other.length)
                return -1;

            int otherIndex = other.length - 1;
            for (var i = source.length - 1; i >= 0 && otherIndex >= 0; --i)
            {
                if (!StringView.Compare(source[i], other[otherIndex], sc))
                {
                    otherIndex = other.length - 1;
                }
                else
                {
                    if (otherIndex == 0)
                        return i;
                    otherIndex--;
                }
            }

            return -1;
        }

        static int LastIndexOf(SubsetStringView source, StringView other, StringComparison sc)
        {
            if (source.length < other.length)
                return -1;

            int otherIndex = other.length - 1;
            for (var i = source.length - 1; i >= 0 && otherIndex >= 0; --i)
            {
                if (!StringView.Compare(source[i], other[otherIndex], sc))
                {
                    otherIndex = other.length - 1;
                }
                else
                {
                    if (otherIndex == 0)
                        return i;
                    otherIndex--;
                }
            }

            return -1;
        }

        static int LastIndexOf(SubsetStringView source, char other, StringComparison sc)
        {
            if (!source.valid)
                return -1;

            for (var i = source.length - 1; i >= 0; --i)
            {
                if (StringView.Compare(source[i], other, sc))
                    return i;
            }

            return -1;
        }

        public bool StartsWith(char c, StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (length == 0)
                return false;
            return StringView.Compare(this[0], c, stringComparison);
        }

        public bool StartsWith(string v, StringComparison sc = StringComparison.Ordinal)
        {
            if (v.Length > length)
                return false;

            for (var i = 0; i < v.Length; ++i)
                if (!StringView.Compare(this[i], v[i], sc))
                    return false;

            return true;
        }

        public bool StartsWith(IStringView v, StringComparison sc = StringComparison.Ordinal)
        {
            if (v.length > length)
                return false;

            for (var i = 0; i < v.length; ++i)
                if (!StringView.Compare(this[i], v[i], sc))
                    return false;

            return true;
        }

        public bool EndsWith(char c, StringComparison sc = StringComparison.Ordinal)
        {
            if (length == 0)
                return false;
            return StringView.Compare(this[length - 1], c, sc);
        }

        public bool EndsWith(string v, StringComparison sc = StringComparison.Ordinal)
        {
            if (v.Length > length)
                return false;

            for (var i = 0; i < v.Length; ++i)
                if (!StringView.Compare(this[length - v.Length + i], v[i], sc))
                    return false;

            return true;
        }

        public bool EndsWith(IStringView v, StringComparison sc = StringComparison.Ordinal)
        {
            if (v.length > length)
                return false;

            for (var i = 0; i < v.length; ++i)
                if (!StringView.Compare(this[length - v.length + i], v[i], sc))
                    return false;

            return true;
        }

        public bool Contains(char c, StringComparison ordinal = StringComparison.Ordinal)
        {
            for (var i = 0; i < length; ++i)
                if (StringView.Compare(this[i], c, ordinal))
                    return true;
            return false;
        }

        public bool Contains(SubsetStringView s, StringComparison ordinal = StringComparison.Ordinal)
        {
            return IndexOf(s) != -1;
        }

        public bool Contains(IStringView s, StringComparison ordinal = StringComparison.Ordinal)
        {
            return IndexOf(s) != -1;
        }

        public bool Contains(string s, StringComparison ordinal = StringComparison.Ordinal)
        {
            return IndexOf(s) != -1;
        }

        public override bool Equals(object other)
        {
            if (other is string o)
                return Equals(o);
            if (other is SubsetStringView v)
                return Equals(v);
            if (other is IStringView sv)
                return Equals(sv);
            return false;
        }

        public bool Equals(IStringView other, StringComparison comparisonOptions = StringComparison.OrdinalIgnoreCase)
        {
            if (other is SubsetStringView sv)
                return Equals(sv, comparisonOptions);

            if (other?.length != length)
                return false;

            for (var i = 0; i < length; ++i)
            {
                if (!StringView.Compare(this[i], other[i], comparisonOptions))
                    return false;
            }

            return true;
        }

        public bool Equals(string other, StringComparison comparisonOptions = StringComparison.OrdinalIgnoreCase)
        {
            if (other?.Length != length)
                return false;

            for (var i = 0; i < length; ++i)
            {
                if (!StringView.Compare(this[i], other[i], comparisonOptions))
                    return false;
            }

            return true;
        }

        public bool Equals(SubsetStringView other, StringComparison comparisonOptions = StringComparison.OrdinalIgnoreCase)
        {
            if (other.length != length)
                return false;

            for (var i = 0; i < length; ++i)
            {
                if (!StringView.Compare(this[i], other[i], comparisonOptions))
                    return false;
            }

            return true;
        }

        public override string ToString()
        {
            if (!valid)
                return string.Empty;
            if (length == 0)
                return string.Empty;
            if (length == m_BaseStringLength)
                return m_BaseString;
            var sb = new StringBuilder(m_Indexes.Count);
            foreach (var index in m_Indexes)
            {
                sb.Append(m_BaseString[index]);
            }

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            var hc = 0;
            foreach (var index in m_Indexes)
                hc ^= m_BaseString[index].GetHashCode();
            return hc;
        }

        public IEnumerator<char> GetEnumerator()
        {
            foreach (var index in m_Indexes)
                yield return m_BaseString[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool operator==(SubsetStringView lhs, SubsetStringView rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(SubsetStringView lhs, SubsetStringView rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator==(SubsetStringView lhs, string rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(SubsetStringView lhs, string rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator==(string lhs, SubsetStringView rhs)
        {
            return rhs.Equals(lhs);
        }

        public static bool operator!=(string lhs, SubsetStringView rhs)
        {
            return !rhs.Equals(lhs);
        }

        public static bool operator==(SubsetStringView lhs, IStringView rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(SubsetStringView lhs, IStringView rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static bool operator==(IStringView lhs, SubsetStringView rhs)
        {
            return rhs.Equals(lhs);
        }

        public static bool operator!=(IStringView lhs, SubsetStringView rhs)
        {
            return !rhs.Equals(lhs);
        }

        public static implicit operator bool(SubsetStringView sv)
        {
            return !sv.IsNullOrEmpty();
        }

        public static void CountLengthToNextWhiteSpace(string baseString, int[] counts, int startIndex, int endIndex)
        {
            var length = endIndex - startIndex;
            var count = 0;
            for (int index = length - 1, sourceIndex = endIndex - 1; index >= 0; --index, --sourceIndex)
            {
                if (char.IsWhiteSpace(baseString[sourceIndex]))
                    count = 0;
                else
                    ++count;
                counts[index] = count;
            }
        }
    }

    static partial class StringExtensions
    {
        public static bool IsNullOrEmpty(this SubsetStringView sv)
        {
            if (!sv.valid)
                return true;
            return sv.length == 0;
        }

        public static bool IsNullOrWhiteSpace(this SubsetStringView sv)
        {
            if (sv.IsNullOrEmpty())
                return true;
            for (var i = 0; i < sv.length; ++i)
            {
                if (!char.IsWhiteSpace(sv[i]))
                    return false;
            }
            return true;
        }

        public static SubsetStringView GetSubsetStringView(this StringView sv)
        {
            return new SubsetStringView(sv.baseString, sv.startIndex, sv.endIndex);
        }
    }
}
