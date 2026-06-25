// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Unity.Burst.Editor
{
    internal struct SearchCriteria
    {
        internal string filter;
        internal bool isCaseSensitive;
        internal bool isWholeWords;
        internal bool isRegex;

        internal SearchCriteria(string keyword, bool caseSensitive, bool wholeWord, bool regex)
        {
            filter = keyword;
            isCaseSensitive = caseSensitive;
            isWholeWords = wholeWord;
            isRegex = regex;
        }

        internal bool Equals(SearchCriteria obj) =>
            filter == obj.filter && isCaseSensitive == obj.isCaseSensitive && isWholeWords == obj.isWholeWords && isRegex == obj.isRegex;

        public override bool Equals(object obj) =>
            obj is SearchCriteria other && Equals(other);

        public override int GetHashCode() => base.GetHashCode();
    }

    internal static class BurstStringSearch
    {
        /// <summary>
        /// Gets index of line end in given string, both absolute and relative to start of line.
        /// </summary>
        /// <param name="str">String to search in.</param>
        /// <param name="line">Line to get end index of.</param>
        /// <returns>(absolute line end index of string, line end index relative to line start).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Argument must be greater than 0 and less than or equal to number of lines in
        /// <paramref name="str" />.
        /// </exception>
        internal static (int total, int relative) GetEndIndexOfPlainLine (string str, int line)
        {
            var lastIdx = -1;
            var newIdx = -1;

            for (var i = 0; i <= line; i++)
            {
                lastIdx = newIdx;
                newIdx = str.IndexOf('\n', lastIdx + 1);

                if (newIdx == -1 && i < line)
                {
                    throw new ArgumentOutOfRangeException(nameof(line),
                        "Argument must be greater than 0 and less than or equal to number of lines in str.");
                }
            }
            lastIdx++;
            return newIdx != -1 ? (newIdx, newIdx - lastIdx) : (str.Length - 1, str.Length - 1 - lastIdx);
        }

        /// <summary>
        /// Gets index of line end in given string, both absolute and relative to start of line.
        /// Adjusts the index so color tags are not included in relative index.
        /// </summary>
        /// <param name="str">String to search in.</param>
        /// <param name="line">Line to find end of in string.</param>
        /// <returns>(absolute line end index of string, line end index relative to line start adjusted for color tags).</returns>
        internal static (int total, int relative) GetEndIndexOfColoredLine(string str, int line)
        {
            var (total, relative) = GetEndIndexOfPlainLine(str, line);
            return RemoveColorTagFromIdx(str, total, relative);
        }

        /// <summary>
        /// Adjusts index of color tags on line.
        /// </summary>
        /// <remarks>Assumes that <see cref="tidx"/> is index of something not a color tag.</remarks>
        /// <param name="str">String containing the indexes.</param>
        /// <param name="tidx">Total index of line end.</param>
        /// <param name="ridx">Relative index of line end.</param>
        /// <returns>(<see cref="tidx"/>, <see cref="ridx"/>) adjusted for color tags on line.</returns>
        private static (int total, int relative) RemoveColorTagFromIdx(string str, int tidx, int ridx)
        {
            var lineStartIdx = tidx - ridx;
            var colorTagFiller = 0;

            var tmp = str.LastIndexOf("</color", tidx);
            var lastWasStart = true;
            var colorTagStart = str.LastIndexOf("<color=", tidx);

            if (tmp > colorTagStart)
            {
                // color tag end was closest
                lastWasStart = false;
                colorTagStart = tmp;
            }

            while (colorTagStart != -1 && colorTagStart >= lineStartIdx)
            {
                var colorTagEnd = str.IndexOf('>', colorTagStart);
                // +1 as the index is zero based.
                colorTagFiller += colorTagEnd - colorTagStart + 1;

                if (lastWasStart)
                {
                    colorTagStart = str.LastIndexOf("</color", colorTagStart);
                    lastWasStart = false;
                }
                else
                {
                    colorTagStart = str.LastIndexOf("<color=", colorTagStart);
                    lastWasStart = true;
                }
            }
            return (tidx - colorTagFiller, ridx - colorTagFiller);
        }

        /// <summary>
        /// Finds the zero indexed line number of given <see cref="matchIdx"/>.
        /// </summary>
        /// <param name="str">String to search in.</param>
        /// <param name="matchIdx">Index to find line number of.</param>
        /// <returns>Line number of given index in string.</returns>
        internal static int FindLineNr(string str, int matchIdx)
        {
            var lineNr = 0;
            var idxn = str.IndexOf('\n');

            while (idxn != -1 && idxn < matchIdx)
            {
                lineNr++;
                idxn = str.IndexOf('\n', idxn + 1);
            }

            return lineNr;
        }

        /// <summary>
        /// Finds first match of <see cref="criteria"/> in given string.
        /// </summary>
        /// <param name="str">String to search in.</param>
        /// <param name="criteria">Search options.</param>
        /// <param name="regx">Used when <see cref="criteria"/> specifies regex search.</param>
        /// <param name="startIdx">Index to start the search at.</param>
        /// <returns>(start index of match, length of match)</returns>
        internal static (int idx, int length) FindMatch(string str, SearchCriteria criteria, Regex regx, int startIdx = 0)
        {
            var idx = -1;
            var len = 0;

            if (criteria.isRegex)
            {
                // regex will have the appropriate options in it if isCaseSensitive or/and isWholeWords is true.
                var res = regx.Match(str, startIdx);

                if (res.Success) (idx, len) = (res.Index, res.Length);
            }
            else if (criteria.isWholeWords)
            {
                (idx, len) = (IndexOfWholeWord(str, startIdx, criteria.filter, criteria.isCaseSensitive
                    ? StringComparison.InvariantCulture
                    : StringComparison.InvariantCultureIgnoreCase), criteria.filter.Length);
            }
            else
            {
                unsafe
                {
                    fixed (char* source = str)
                    {
                        fixed (char* target = criteria.filter)
                        {
                            (idx, len) = (
                                IndexOfCustom(source, str.Length, target, criteria.filter.Length, startIdx, criteria.isCaseSensitive)
                                , criteria.filter.Length);
                        }
                    }
                }
            }

            return (idx, len);
        }

        internal static List<(int idx, int length)> FindAllMatches(string str, SearchCriteria criteria, Regex regx,
            int startIdx = 0)
        {
            var retVal = new List<(int, int)>();

            if (criteria.isRegex)
            {
                var res = regx.Matches(str, startIdx);

                foreach (Match match in res)
                {
                    retVal.Add((match.Index, match.Length));
                }
            }
            else if (criteria.isWholeWords)
            {
                retVal.AddRange(IndexOfWholeWordAll(str, startIdx, criteria.filter,
                    criteria.isCaseSensitive
                        ? StringComparison.InvariantCulture
                        : StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                unsafe
                {
                    fixed (char* source = str)
                    {
                        fixed (char* target = criteria.filter)
                        {
                            retVal.AddRange(FindAllIndices(source, str.Length, target, criteria.filter.Length, startIdx, criteria.isCaseSensitive));
                        }
                    }
                }
            }

            return retVal;
        }

        private static char ToUpper(char c) => c - 97U > 25U ? c : (char)(c - 32U);

        private static unsafe int ScanForFilterInsensitive(char* str, char* filter, int flen, int i)
        {
            int j = 0;
            while (j < flen && ToUpper(str[i + j]) == ToUpper(filter[j]))
            {
                j++;
            }
            return j;
        }

        private static unsafe int ScanForFilter(char* str, char* filter, int flen, int i)
        {
            int j = 0;
            while (j < flen && str[i + j] == filter[j])
            {
                j++;
            }
            return j;
        }

        private static unsafe List<(int, int)> FindAllIndices(char* str, int len, char* filter, int flen, int startIdx, bool caseSensitive)
        {
            var retVal = new List<(int,int)>();
            if (len < flen) { return retVal; }

            int stop = len - flen;
            if (caseSensitive)
            {
                for (int i = startIdx; i < stop; i++)
                {
                    if (ScanForFilter(str, filter, flen, i) == flen)
                    {
                        retVal.Add((i, flen));
                        i += flen - 1;
                    }
                }
            }
            else
            {
                for (int i = startIdx; i < stop; i++)
                {
                    if (ScanForFilterInsensitive(str, filter, flen, i) == flen)
                    {
                        retVal.Add((i, flen));
                        i += flen-1;
                    }
                }
            }
            return retVal;
        }


        /// <summary>
        /// Finds index of first occurence of <see cref="filter"/> in <see cref="str"/>.
        /// </summary>
        /// <param name="str">String to search through</param>
        /// <param name="len">Length of <see cref="str"/></param>
        /// <param name="filter">Needle to find</param>
        /// <param name="flen">Lenght of <see cref="filter"/></param>
        /// <param name="startIdx">Index to start search from</param>
        /// <param name="caseSensitive">Whether search ignore casing</param>
        /// <returns>index of first match or -1</returns>
        private static unsafe int IndexOfCustom(char* str, int len, char* filter, int flen, int startIdx, bool caseSensitive)
        {
            if (len < flen) { return -1; }

            int stop = len - flen;
            if (caseSensitive)
            {
                for (int i = startIdx; i < stop; i++)
                {
                    if (ScanForFilter(str, filter, flen, i) == flen)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = startIdx; i < stop; i++)
                {
                    if (ScanForFilterInsensitive(str, filter, flen, i) == flen)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds index of <see cref="filter"/> matching for whole words.
        /// </summary>
        /// <param name="str">String to search in.</param>
        /// <param name="startIdx">Index to start search from.</param>
        /// <param name="filter">Key to search for.</param>
        /// <param name="opt">Options for string comparison.</param>
        /// <returns>Index of match or -1.</returns>
        private static int IndexOfWholeWord(string str, int startIdx, string filter, StringComparison opt)
        {
            const string wholeWordMatch = @"\w";

            var j = startIdx;
            var filterLen = filter.Length;
            var strLen = str.Length;
            while (j < strLen && (j = str.IndexOf(filter, j, opt)) >= 0)
            {
                var noPrior = true;
                if (j != 0)
                {
                    var frontBorder = str[j - 1];
                    noPrior = !Regex.IsMatch(frontBorder.ToString(), wholeWordMatch);
                }

                var noAfter = true;
                if (j + filterLen != strLen)
                {
                    var endBorder = str[j + filterLen];
                    noAfter = !Regex.IsMatch(endBorder.ToString(), wholeWordMatch);
                }

                if (noPrior && noAfter) return j;

                j++;
            }
            return -1;
        }


        private static List<(int idx, int len)> IndexOfWholeWordAll(string str, int startIdx, string filter, StringComparison opt)
        {
            const string wholeWordMatch = @"\w";
            var retVal = new List<(int, int)>();

            var j = startIdx;
            var filterLen = filter.Length;
            var strLen = str.Length;
            while (j < strLen && (j = str.IndexOf(filter, j, opt)) >= 0)
            {
                var noPrior = true;
                if (j != 0)
                {
                    var frontBorder = str[j - 1];
                    noPrior = !Regex.IsMatch(frontBorder.ToString(), wholeWordMatch);
                }

                var noAfter = true;
                if (j + filterLen != strLen)
                {
                    var endBorder = str[j + filterLen];
                    noAfter = !Regex.IsMatch(endBorder.ToString(), wholeWordMatch);
                }

                if (noPrior && noAfter)
                {
                    retVal.Add((j, filterLen));
                    j += filterLen - 1;
                }

                j++;
            }
            return retVal;
        }
    }
}
