// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    /// <summary>
    /// Structure that holds a view on a string, with a specified range of [startIndex, endIndex[.
    /// </summary>
    readonly ref struct StringView
    {
        readonly string m_BaseString;
        readonly int m_StartIndex;
        readonly int m_EndIndex;

        public int Length => m_EndIndex - m_StartIndex;

        public StringView(string baseString)
        {
            m_BaseString = baseString;
            m_StartIndex = 0;
            m_EndIndex = baseString.Length;
        }

        public StringView(string baseString, int startIndex, int endIndex)
        {
            if (startIndex < 0 || startIndex >= baseString.Length)
                throw new ArgumentException("Index out of string range", nameof(startIndex));
            if (endIndex < 0 || endIndex > baseString.Length)
                throw new ArgumentException("Index out of string range", nameof(endIndex));
            m_BaseString = baseString;
            m_StartIndex = startIndex;
            m_EndIndex = endIndex;
        }

        public char this[int index] => m_BaseString[m_StartIndex + index];

        public bool Equals(string other, StringComparison comparisonOptions = StringComparison.OrdinalIgnoreCase)
        {
            if (other.Length != Length)
                return false;

            return string.Compare(m_BaseString, m_StartIndex, other, 0, Length, comparisonOptions) == 0;
        }

        public override bool Equals(object other)
        {
            return other is string s && Equals(s);
        }

        public static bool operator==(StringView lhs, string rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(StringView lhs, string rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            var hc = 0;
            for (var i = m_StartIndex; i < m_EndIndex; ++i)
                hc ^= m_BaseString[i].GetHashCode();
            return hc;
        }

        public override string ToString()
        {
            return m_BaseString.Substring(m_StartIndex, Length);
        }

        public static bool IsNullOrEmpty(StringView sv)
        {
            if (sv.m_BaseString == null)
                return true;
            return sv.Length == 0;
        }

        public static bool IsNullOrWhiteSpace(StringView sv)
        {
            if (IsNullOrEmpty(sv))
                return true;
            for (var i = 0; i < sv.Length; ++i)
            {
                if (!char.IsWhiteSpace(sv[i]))
                    return false;
            }
            return true;
        }
    }

    static class StringExtensions
    {
        static readonly char[] k_OneLetterWords = new char[] { ':', '-', '!' };
        static readonly char[] k_WordSplitters = new char[] { '(', ')', '{', '}', '[', ']', ':', '-' };

        public static StringView GetStringView(this string baseString, int startIndex, int endIndex)
        {
            return new StringView(baseString, startIndex, endIndex);
        }

        public static StringView GetStringView(this string baseString)
        {
            return new StringView(baseString);
        }

        public static StringView GetWordView(this string baseString, int startIndex)
        {
            if (startIndex < 0 || startIndex >= baseString.Length)
                throw new ArgumentException("Index out of string range", nameof(startIndex));

            var i = startIndex;
            if (Array.IndexOf(k_OneLetterWords, baseString[i]) != -1)
                return new StringView(baseString, startIndex, i + 1);

            var lod = char.IsLetterOrDigit(baseString[i++]);
            while (i < baseString.Length)
            {
                if (Array.IndexOf(k_WordSplitters, baseString[i]) != -1)
                    break;

                if (lod == char.IsLetterOrDigit(baseString[i]))
                    i++;
                else
                    break;
            }
            return new StringView(baseString, startIndex, i);
        }
    }
}
