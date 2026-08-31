// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UnityEditor.TextCore.Text
{
    // The subset recipe character set format: canonical lowercase hex code point ranges,
    // e.g. "20-7e,4e00-9fff,1f600". Consecutive code points collapse into ranges so a recipe
    // stays compact instead of enumerating every character.
    internal static class UnicodeRanges
    {
        internal static string FromCharacters(string characters)
        {
            if (string.IsNullOrEmpty(characters))
                return string.Empty;

            var codePoints = new SortedSet<uint>();
            for (int i = 0; i < characters.Length; i += char.IsSurrogatePair(characters, i) ? 2 : 1)
                codePoints.Add((uint)char.ConvertToUtf32(characters, i));

            return FromCodePoints(codePoints);
        }

        internal static string FromCodePoints(IEnumerable<uint> codePoints)
        {
            var sorted = codePoints as SortedSet<uint> ?? new SortedSet<uint>(codePoints);
            var builder = new StringBuilder();
            uint first = 0, last = 0;
            bool open = false;

            foreach (uint c in sorted)
            {
                if (open && c == last + 1)
                {
                    last = c;
                    continue;
                }
                if (open)
                    AppendRange(builder, first, last);
                first = last = c;
                open = true;
            }
            if (open)
                AppendRange(builder, first, last);

            return builder.ToString();
        }

        static void AppendRange(StringBuilder builder, uint first, uint last)
        {
            if (builder.Length > 0)
                builder.Append(',');
            builder.Append(first.ToString("x"));
            if (last != first)
                builder.Append('-').Append(last.ToString("x"));
        }

        internal static int CountCodePoints(string ranges)
        {
            if (string.IsNullOrEmpty(ranges))
                return 0;

            int count = 0;
            foreach (var token in ranges.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParseToken(token, out uint first, out uint last))
                    count += (int)(last - first + 1);
            }
            return count;
        }

        // Returns null when the expansion would exceed maxLength UTF-16 units.
        internal static string ToCharacters(string ranges, int maxLength)
        {
            if (string.IsNullOrEmpty(ranges))
                return string.Empty;

            var builder = new StringBuilder();
            foreach (uint c in EnumerateCodePoints(ranges))
            {
                builder.Append(char.ConvertFromUtf32((int)c));
                if (builder.Length > maxLength)
                    return null;
            }
            return builder.ToString();
        }

        // Yields only valid scalar values; surrogates and out-of-range entries are skipped.
        internal static IEnumerable<uint> EnumerateCodePoints(string ranges)
        {
            if (string.IsNullOrEmpty(ranges))
                yield break;

            foreach (var token in ranges.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!TryParseToken(token, out uint first, out uint last))
                    continue;

                for (uint c = first; c <= last; c++)
                {
                    if (c is (>= 0xD800 and <= 0xDFFF) or > 0x10FFFF)
                        continue;
                    yield return c;
                }
            }
        }

        static bool TryParseToken(string token, out uint first, out uint last)
        {
            last = 0;
            var bounds = token.Split('-');
            if (!uint.TryParse(bounds[0], NumberStyles.HexNumber, null, out first))
                return false;

            last = first;
            if (bounds.Length == 1)
                return true;
            return bounds.Length == 2
                && uint.TryParse(bounds[1], NumberStyles.HexNumber, null, out last)
                && last >= first;
        }
    }
}
