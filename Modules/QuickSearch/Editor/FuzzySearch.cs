// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define QUICKSEARCH_DEBUG
using System;
using System.Collections.Generic;


namespace UnityEditor.Search
{
    /// <summary>
    /// Utility class to perform matching against query text using a fuzzy search algorithm.
    /// </summary>
    public static class FuzzySearch
    {
        struct ScoreIndx
        {
            public int i;
            public int score;
            public int prev_mi;
        }

        class FuzzyMatchData : IDisposable
        {
            public List<ScoreIndx>[] matches_indx;
            public bool[,] matchData;

            private FuzzyMatchData(int strN, int patternN)
            {
                matchData = new bool[strN, patternN];

                matches_indx = new List<ScoreIndx>[patternN];
                for (var k = 0; k < patternN; k++)
                {
                    matches_indx[k] = new List<ScoreIndx>(8);
                }
            }

            public void Dispose()
            {
            }

            public static FuzzyMatchData Request(int strN, int patternN)
            {
                return new FuzzyMatchData(strN, patternN);
            }
        }

        internal struct ScopedProfiler : IDisposable
        {
            public ScopedProfiler(string name)
            {
            }

            public ScopedProfiler(string name, UnityEngine.Object targetObject)
            {
            }

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Performs a fuzzy search on a string to see if it matches a pattern.
        /// </summary>
        /// <param name="pattern">Pattern that we try to match the source string</param>
        /// <param name="origin">String we are looking into for a match</param>
        /// <param name="outScore">Score of the match. A higher score means the pattern is a better match for the string.</param>
        /// <param name="matches">List of indices in the source string where a match was found.</param>
        /// <returns>Returns true if a match was found</returns>
        public static bool FuzzyMatch(string pattern, string origin, ref long outScore, List<int> matches = null)
        {
            int str_n;
            int pattern_n;
            int str_start;

            string str;
            using (new ScopedProfiler("[FM] Init"))
            {
                outScore = -100000;
                matches?.Clear();

                if (string.IsNullOrEmpty(origin)) return false;

                if (string.IsNullOrEmpty(pattern)) return true;

                str = origin.ToLowerInvariant();
                pattern_n = pattern.Length;

                // find [str_start..str_end) that contains pattern's first and last letter
                str_start = 0;
                var str_end = str.Length - 1;

                var pattern_first_lower = pattern[0];
                var pattern_end_lower = pattern[pattern.Length - 1];

                for (; str_start < str.Length; ++str_start)
                    if (pattern_first_lower == str[str_start])
                        break;

                for (; str_end >= 0; --str_end)
                    if (pattern_end_lower == str[str_end])
                        break;
                ++str_end;

                str_n = str_end - str_start;

                // str subset is shorter than pattern
                if (str_n < pattern_n)
                    return false;

                // do check that pattern is fully inside [str_start..str_end)
                var pattern_i = 0;
                var str_i = str_start;
                while (pattern_i < pattern_n && str_i < str_end)
                {
                    if (pattern[pattern_i] == str[str_i])
                        ++pattern_i;
                    ++str_i;
                }

                if (pattern_i < pattern_n)
                    return false;
            }

            using (new ScopedProfiler("[FM] Body"))
            using (var d = FuzzyMatchData.Request(str_n, pattern_n))
            {
                var str_n_minus_pattern_n_plus_1 = str_n - pattern_n + 1;

                var prev_min_i = 0;

                using (var _3 = new ScopedProfiler("[FM] Match loop"))
                    for (var j = 0; j < pattern_n; ++j)
                    {
                        var min_i = str_n + 1;
                        var first_match = true;

                        for (int i = Math.Max(j, prev_min_i), end_i = str_n_minus_pattern_n_plus_1 + j; i < end_i; ++i)
                        {
                            // Skip existing <> tags
                            if (str[i] == '<')
                            {
                                for (; i < end_i; ++i)
                                {
                                    if (str[i] == '>')
                                        break;
                                }
                            }

                            var si = i + str_start;

                            var match = false;

                            if (pattern[j] == str[si])
                                match = true;

                            if (i >= d.matchData.GetLength(0) || j >= d.matchData.GetLength(1))
                                return false;
                            d.matchData[i, j] = match;

                            if (match)
                            {
                                if (first_match)
                                {
                                    min_i = i;
                                    first_match = false;
                                }

                                d.matches_indx[j].Add(new ScoreIndx { i = i, score = 1, prev_mi = -1 });
                            }
                        }

                        if (first_match)
                            return false; // no match for pattern[j]

                        prev_min_i = min_i;
                    }


                const int sequential_bonus = 75;            // bonus for adjacent matches
                const int separator_bonus = 30;             // bonus if match occurs after a separator
                const int camel_bonus = 30;                 // bonus if match is uppercase and prev is lower or symbol
                const int first_letter_bonus = 35;          // bonus if the first letter is matched
                const int leading_letter_penalty = -5;      // penalty applied for every letter in str before the first match
                const int max_leading_letter_penalty = -15; // maximum penalty for leading letters
                const int unmatched_letter_penalty = -1;    // penalty for every letter that doesn't matter
                int unmatched = str_n - (matches?.Count ?? 0);

                // find best score
                using (new ScopedProfiler("[FM] Best score 0"))
                    for (var mi = 0; mi < d.matches_indx[0].Count; ++mi)
                    {
                        var i = d.matches_indx[0][mi].i;
                        var si = str_start + i;
                        var s = 100 + unmatched_letter_penalty * unmatched;

                        var penalty = leading_letter_penalty * si;
                        if (penalty < max_leading_letter_penalty)
                            penalty = max_leading_letter_penalty;

                        s += penalty;
                        if (si == 0)
                        {
                            s += first_letter_bonus;
                        }
                        else
                        {
                            var currOrigI = origin[si];
                            var prevOrigI = origin[si - 1];

                            if (char.IsUpper(currOrigI) && char.IsUpper(prevOrigI) == false)
                                s += camel_bonus;
                            else if (prevOrigI == '_' || prevOrigI == ' ')
                                s += separator_bonus;
                        }

                        d.matches_indx[0][mi] = new ScoreIndx
                        {
                            i = i,
                            score = s,
                            prev_mi = -1
                        };
                    }

                using (new ScopedProfiler("[FM] Best score 1..pattern_n"))
                    for (var j = 1; j < pattern_n; ++j)
                    {
                        for (var mi = 0; mi < d.matches_indx[j].Count; ++mi)
                        {
                            var match = d.matches_indx[j][mi];

                            var si = str_start + d.matches_indx[j][mi].i;

                            var currOrigI = origin[si];
                            var prevOrigI = origin[si - 1];

                            if (char.IsUpper(currOrigI) && char.IsUpper(prevOrigI) == false)
                                match.score += camel_bonus;
                            else if (prevOrigI == '_' || prevOrigI == ' ')
                                match.score += separator_bonus;

                            // select from prev
                            var best_pmi = 0;

                            var best_score = -1;
                            for (var pmi = 0; pmi < d.matches_indx[j - 1].Count; ++pmi)
                            {
                                var prev_i = d.matches_indx[j - 1][pmi].i;
                                if (prev_i >= match.i)
                                    break;

                                var pmi_score = d.matches_indx[j - 1][pmi].score;

                                if (prev_i == match.i - 1)
                                    pmi_score += sequential_bonus;

                                if (best_score < pmi_score)
                                {
                                    best_score = pmi_score;
                                    best_pmi = pmi;
                                }
                            }

                            match.score += best_score;
                            match.prev_mi = best_pmi;

                            d.matches_indx[j][mi] = match;
                        }
                    }

                var best_mi = 0;
                var max_j = pattern_n - 1;
                for (var mi = 1; mi < d.matches_indx[max_j].Count; ++mi)
                {
                    if (d.matches_indx[max_j][best_mi].score < d.matches_indx[max_j][mi].score)
                        best_mi = mi;
                }

                var bestScore = d.matches_indx[max_j][best_mi];

                outScore = bestScore.score;
                if (matches != null)
                {
                    using (new ScopedProfiler("[FM] Matches calc"))
                    {
                        matches.Capacity = pattern_n;
                        matches.Add(bestScore.i + str_start);
                        {
                            var mi = bestScore.prev_mi;
                            for (var j = pattern_n - 2; j >= 0; --j)
                            {
                                matches.Add(d.matches_indx[j][mi].i + str_start);
                                mi = d.matches_indx[j][mi].prev_mi;
                            }
                        }
                        matches.Reverse();
                    }
                }

                return true;
            }
        }
    }

    internal static class RichTextFormatter
    {
        static readonly char[] cache_result = new char[1024];

        /// <summary>
        /// Color for matching text when using fuzzy search.
        /// </summary>
        public static string HighlightColorTag = EditorGUIUtility.isProSkin ? "<color=#FF6100>" : "<color=#EE4400>";

        /// <summary>
        /// Color for special tags when using fuzzy search.
        /// </summary>
        public static string HighlightColorTagSpecial = EditorGUIUtility.isProSkin ? "<color=#FF6100>" : "<color=#BB1100>";

        public static string FormatSuggestionTitle(string title, List<int> matches)
        {
            return FormatSuggestionTitle(title, matches, HighlightColorTag, HighlightColorTagSpecial);
        }

        public static string FormatSuggestionTitle(string title, List<int> matches, string selectedTextColorTag, string specialTextColorTag)
        {
            const string closingTag = "</color>";
            int openCharCount = specialTextColorTag.Length;
            int closingCharCount = closingTag.Length;

            var N = title.Length + matches.Count * (closingCharCount + openCharCount);
            var MN = matches.Count;

            var result = cache_result;
            if (N > cache_result.Length)
                result = new char[N];

            int t_i = 0;
            int t_j = 0;
            int t_k = 0;
            string tag = null;
            var needToClose = false;

            for (int guard = 0; guard < N; ++guard)
            {
                if (tag == null && needToClose == false && t_k < MN) // find tag for t_i
                {
                    var indx = matches[t_k];
                    if (indx == t_i || indx == -t_i)
                    {
                        tag = (indx < 0) ? specialTextColorTag : selectedTextColorTag;
                        ++t_k;
                    }
                }

                if (tag != null)
                {
                    result[guard] = tag[t_j++];

                    if (t_j >= tag.Length)
                    {
                        if (tag != closingTag)
                            needToClose = true;

                        tag = null;
                        t_j = 0;
                    }
                }
                else
                {
                    result[guard] = title[Math.Min(t_i++, title.Length - 1)];

                    if (needToClose)
                    {
                        tag = closingTag;
                        needToClose = false;
                    }
                }
            }

            return new string(result, 0, N);
        }
    }
}
