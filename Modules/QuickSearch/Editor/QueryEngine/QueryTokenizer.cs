// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEditor.Search
{
    enum ParseState
    {
        Matched,
        NoMatch,
        ParseError
    }

    static class QueryRegexValues
    {
        // To match a regex at a specific index, use \\G and Match(input, startIndex)
        public static readonly Regex k_PhraseRx = new Regex("\\G!?\\\".*?\\\"");

        public const string k_FilterOperatorsInnerPattern = "[^\\w\\s-{}()\"\\[\\].,/|\\`]+";
        public static readonly string k_FilterOperatorsPattern = $"({k_FilterOperatorsInnerPattern})";
        public static readonly string k_PartialFilterOperatorsPattern = $"(?<op>{k_FilterOperatorsInnerPattern})?";

        public const string k_FilterNamePattern = "\\G([\\w.]+)";
        public static readonly string k_PartialFilterNamePattern = $"\\G(?<name>[\\w.]+(?=\\(|(?:{k_FilterOperatorsInnerPattern})))";

        public const string k_FilterFunctionPattern = "(\\([^\\(\\)]+\\))?";
        public const string k_PartialFilterFunctionPattern = "(?<f1>\\((?!\\S*\\s+\\))[^\\(\\)\\s]*\\)?)?(?<f2>(f1)|\\([^\\(\\)]*\\))?";

        public const string k_FilterValuePattern = "(\\\".*?\\\"|([a-zA-Z0-9]*?)(?:\\{(?:(?<c>\\{)|[^{}]+|(?<-c>\\}))*(?(c)(?!))\\})|[^\\s{}]+)";
        public const string k_PartialFilterValuePattern = "(?<value>(?(op)(\\\".*?\\\"|([a-zA-Z0-9]*?)(?:\\{(?:(?<c>\\{)|[^{}]+|(?<-c>\\}))*(?(c)(?!))\\})|[^\\s{}]+)))?";

        public static readonly Regex k_WordRx = new Regex("\\G!?\\S+");
        public static readonly Regex k_NestedQueryRx = new Regex("\\G([a-zA-Z0-9]*?)(\\{(?:(?<c>\\{)|[^{}]+|(?<-c>\\}))*(?(c)(?!))\\})");
    }

    abstract class QueryTokenizer<TUserData>
    {
        Regex m_FilterRx = new Regex(QueryRegexValues.k_FilterNamePattern +
            QueryRegexValues.k_FilterFunctionPattern +
            QueryRegexValues.k_FilterOperatorsPattern +
            QueryRegexValues.k_FilterValuePattern, RegexOptions.Compiled);

        static readonly List<string> k_CombiningToken = new List<string>
        {
            "and",
            "or",
            "not",
            "-"
        };

        protected delegate int TokenMatcher(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched);
        protected delegate bool TokenConsumer(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);

        protected List<UnityEditor.Tuple<TokenMatcher, TokenConsumer>> m_TokenConsumers;

        protected QueryTokenizer()
        {
            // The order of regex in this list is important. Keep it like that unless you know what you are doing!
            m_TokenConsumers = new List<UnityEditor.Tuple<TokenMatcher, TokenConsumer>>
            {
                new UnityEditor.Tuple<TokenMatcher, TokenConsumer>(MatchEmpty, ConsumeEmpty),
                new UnityEditor.Tuple<TokenMatcher, TokenConsumer>(MatchGroup, ConsumeGroup),
                new UnityEditor.Tuple<TokenMatcher, TokenConsumer>(MatchCombiningToken, ConsumeCombiningToken),
                new UnityEditor.Tuple<TokenMatcher, TokenConsumer>(MatchNestedQuery, ConsumeNestedQuery),
                new UnityEditor.Tuple<TokenMatcher, TokenConsumer>(MatchFilter, ConsumeFilter),
                new UnityEditor.Tuple<TokenMatcher, TokenConsumer>(MatchWord, ConsumeWord)
            };
        }

        public void BuildFilterRegex(List<string> operators)
        {
            var innerOperatorsPattern = $"{string.Join("|", operators)}";
            var filterOperatorsPattern = $"({innerOperatorsPattern})";
            m_FilterRx = new Regex(QueryRegexValues.k_FilterNamePattern + QueryRegexValues.k_FilterFunctionPattern + filterOperatorsPattern + QueryRegexValues.k_FilterValuePattern, RegexOptions.Compiled);
        }

        public ParseState Parse(string text, int startIndex, int endIndex, ICollection<QueryError> errors, TUserData userData)
        {
            var index = startIndex;
            while (index < endIndex)
            {
                var matched = false;
                foreach (var t in m_TokenConsumers)
                {
                    var matcher = t.Item1;
                    var consumer = t.Item2;
                    var matchLength = matcher(text, index, endIndex, errors, out var sv, out var match, out var consumerMatched);
                    if (!consumerMatched)
                        continue;
                    var consumed = consumer(text, index, index + matchLength, sv, match, errors, userData);
                    if (!consumed)
                    {
                        return ParseState.ParseError;
                    }
                    index += matchLength;
                    matched = true;
                    break;
                }

                if (!matched)
                {
                    errors.Add(new QueryError {index = index, reason = $"Error parsing string. No token could be deduced at {index}"});
                    return ParseState.NoMatch;
                }
            }

            return ParseState.Matched;
        }

        static int MatchEmpty(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            var currentIndex = startIndex;
            var lengthMatched = 0;
            matched = false;
            while (currentIndex < endIndex && QueryEngineUtils.IsWhiteSpaceChar(text[currentIndex]))
            {
                ++currentIndex;
                ++lengthMatched;
                matched = true;
            }

            sv = text.GetStringView(startIndex, startIndex + lengthMatched);
            match = null;
            return lengthMatched;
        }

        static int MatchCombiningToken(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            match = null;
            sv = text.GetStringView();
            var totalUsableLength = endIndex - startIndex;

            foreach (var combiningToken in k_CombiningToken)
            {
                var tokenLength = combiningToken.Length;
                if (tokenLength > totalUsableLength)
                    continue;

                sv = text.GetWordView(startIndex);
                if (sv == combiningToken)
                {
                    matched = true;
                    return sv.Length;
                }
            }

            matched = false;
            return -1;
        }

        int MatchFilter(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = m_FilterRx.Match(text, startIndex, endIndex - startIndex);

            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        int MatchWord(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = QueryRegexValues.k_PhraseRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
                match = QueryRegexValues.k_WordRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        int MatchGroup(string text, int groupStartIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = null;
            if (groupStartIndex >= text.Length || text[groupStartIndex] != '(')
            {
                matched = false;
                return -1;
            }

            matched = true;
            if (groupStartIndex < 0 || groupStartIndex >= text.Length)
            {
                errors.Add(new QueryError {index = 0, reason = $"A group should have been found but index was {groupStartIndex}"});
                return -1;
            }

            var charConsumed = 0;

            var parenthesisCounter = 1;
            var groupEndIndex = groupStartIndex + 1;
            for (; groupEndIndex < text.Length && parenthesisCounter > 0; ++groupEndIndex)
            {
                if (text[groupEndIndex] == '(')
                    ++parenthesisCounter;
                else if (text[groupEndIndex] == ')')
                    --parenthesisCounter;
            }

            // Because of the final ++groupEndIndex, decrement the index
            --groupEndIndex;

            if (parenthesisCounter != 0)
            {
                errors.Add(new QueryError {index = groupStartIndex, reason = $"Unbalanced parenthesis"});
                return -1;
            }

            charConsumed = groupEndIndex - groupStartIndex + 1;
            sv = text.GetStringView(groupStartIndex + 1, groupStartIndex + charConsumed - 1);
            return charConsumed;
        }

        int MatchNestedQuery(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = QueryRegexValues.k_NestedQueryRx.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        protected abstract bool ConsumeEmpty(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeCombiningToken(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeFilter(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeWord(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeGroup(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeNestedQuery(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
    }
}
