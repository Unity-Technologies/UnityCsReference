// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    interface IStringView : IEnumerable<char>
    {
        bool valid { get; }
        string baseString { get; }
        int Length { get; }
        char this[int index] { get; }
        string ToString();

        IStringView Substring(int start);
        IStringView Substring(int start, int length);
        IStringView Trim(params char[] chrs);
        int IndexOf(IStringView other, StringComparison sc = StringComparison.Ordinal);
        int IndexOf(string other, StringComparison sc = StringComparison.Ordinal);
        int IndexOf(char other, StringComparison sc = StringComparison.Ordinal);
        int LastIndexOf(IStringView other, StringComparison sc = StringComparison.Ordinal);
        int LastIndexOf(string other, StringComparison sc = StringComparison.Ordinal);
        int LastIndexOf(char other, StringComparison sc = StringComparison.Ordinal);
        bool StartsWith(char c, StringComparison stringComparison = StringComparison.Ordinal);
        bool StartsWith(string v, StringComparison sc = StringComparison.Ordinal);
        bool StartsWith(IStringView v, StringComparison sc = StringComparison.Ordinal);
        bool EndsWith(char c, StringComparison sc = StringComparison.Ordinal);
        bool EndsWith(string v, StringComparison sc = StringComparison.Ordinal);
        bool EndsWith(IStringView v, StringComparison sc = StringComparison.Ordinal);
        bool Contains(char c, StringComparison ordinal = StringComparison.Ordinal);
        bool Contains(IStringView s, StringComparison ordinal = StringComparison.Ordinal);
        bool Contains(string s, StringComparison ordinal = StringComparison.Ordinal);

        bool Equals(IStringView other, StringComparison comparisonOptions = StringComparison.OrdinalIgnoreCase);
        bool Equals(string other, StringComparison comparisonOptions = StringComparison.OrdinalIgnoreCase);
    }

    static class IStringViewExtensions
    {
        public static bool IsNullOrEmpty(this IStringView sv)
        {
            if (sv == null || !sv.valid)
                return true;
            return sv.Length == 0;
        }

        public static bool IsNullOrWhiteSpace(this IStringView sv)
        {
            if (sv.IsNullOrEmpty())
                return true;
            for (var i = 0; i < sv.Length; ++i)
            {
                if (!char.IsWhiteSpace(sv[i]))
                    return false;
            }
            return true;
        }
    }
}
