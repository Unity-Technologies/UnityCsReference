// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Experimental.GraphView
{
    // Matches templates while QuickSearch's index isn't ready.
    partial class TemplateQueryMatcher
    {
        internal const string k_FileNameKey = "filename";

        const string k_And = "and";
        const string k_Or = "or";
        const string k_Not = "not";
        const string k_TypeKey = "t";
        const string k_AreaShortKey = "a";
        const string k_AreaKey = "area";
        const string k_LabelShortKey = "l";
        const string k_LabelKey = "label";

        const char k_ExactPrefix = '!';
        const char k_NegationPrefix = '-';
        const char k_Quote = '"';
        const char k_GroupOpen = '(';
        const char k_GroupClose = ')';
        const char k_EqualsSeparator = '=';
        const char k_ContainsSeparator = ':';

        enum Op { Word, ExactWord, Equals, Contains }

        struct Term
        {
            public Op op;
            public string key;
            public string value;
        }

        Func<Dictionary<string, List<string>>, bool> m_Root;

        public bool IsMatchAll => m_Root == null;

        // Area is based on the template's location and not raised through regular search terms.
        public string Area { get; private set; }

        public static TemplateQueryMatcher Parse(string query, string toolKey)
        {
            var matcher = new TemplateQueryMatcher();
            if (string.IsNullOrWhiteSpace(query))
                return matcher;

            var parser = new Parser(Tokenize(query), toolKey, matcher);
            matcher.m_Root = parser.ParseQuery();
            return matcher;
        }

        public bool Matches(Dictionary<string, List<string>> document)
        {
            return m_Root == null || m_Root(document);
        }

        static bool MatchTerm(Term term, Dictionary<string, List<string>> document)
        {
            if (term.op == Op.Word || term.op == Op.ExactWord)
                return MatchWord(term.value, term.op == Op.ExactWord, document);

            if (term.key == k_FileNameKey || !document.TryGetValue(term.key, out var values))
                return false;

            foreach (var value in values)
            {
                if (term.op == Op.Equals && value == term.value)
                    return true;
                if (term.op == Op.Contains && value.Contains(term.value))
                    return true;
            }
            return false;
        }

        // Must stay in sync with GraphViewIndexerExtension.IndexWords.
        static bool MatchWord(string word, bool exact, Dictionary<string, List<string>> document)
        {
            foreach (var entry in document)
            {
                if (!GraphViewIndexerExtension.IsWordSearchable(entry.Key))
                    continue;
                var isFileName = entry.Key == k_FileNameKey;
                foreach (var value in entry.Value)
                {
                    if (isFileName && !exact && value.Contains(word))
                        return true;
                    if (MatchesWord(value, word, exact))
                        return true;
                    foreach (var component in Search.SearchUtils.SplitEntryComponents(value, Search.SearchUtils.entrySeparators))
                    {
                        if (MatchesWord(component, word, exact))
                            return true;
                    }
                }
            }
            return false;
        }

        static bool MatchesWord(string indexed, string word, bool exact) =>
            exact ? indexed == word : indexed.StartsWith(word, StringComparison.Ordinal);

        static bool TryParseTerm(string token, string toolKey, out Term term)
        {
            term = default;
            if (string.IsNullOrEmpty(token))
                return false;

            var separatorIndex = FirstSeparator(token, out var op);
            if (separatorIndex < 0)
            {
                var isExact = token[0] == k_ExactPrefix;
                if (isExact)
                    token = token.Substring(1);
                var isQuoted = token.Length >= 2 && token[0] == k_Quote && token[token.Length - 1] == k_Quote;
                var word = Unquote(token).ToLowerInvariant();
                if (word.Length == 0 || (!isQuoted && !isExact && (word == k_And || word == k_Or || word == k_Not)))
                    return false;
                term = new Term { op = isExact ? Op.ExactWord : Op.Word, value = word };
                return true;
            }

            var key = token.Substring(0, separatorIndex).ToLowerInvariant();
            var value = Unquote(token.Substring(separatorIndex + 1)).ToLowerInvariant();
            if (value.Length == 0)
                return false;

            // Type/area filters are handled elsewhere; ignore them.
            if (key == k_TypeKey || key == k_AreaShortKey || key == k_AreaKey)
                return false;

            // 'l'/'label' is shorthand for the template's asset labels.
            if (key == k_LabelShortKey || key == k_LabelKey)
                key = $"{toolKey}.{k_LabelKey}";

            term = new Term { op = op, key = key, value = value };
            return true;
        }

        static bool TryParseArea(string token, out string area)
        {
            area = null;
            var separatorIndex = FirstSeparator(token, out _);
            if (separatorIndex < 0)
                return false;

            var key = token.Substring(0, separatorIndex).ToLowerInvariant();
            if (key != k_AreaShortKey && key != k_AreaKey)
                return false;

            area = Unquote(token.Substring(separatorIndex + 1)).ToLowerInvariant();
            return area.Length > 0;
        }

        static int FirstSeparator(string token, out Op op)
        {
            op = Op.Contains;
            var eq = token.IndexOf(k_EqualsSeparator);
            var colon = token.IndexOf(k_ContainsSeparator);

            // A quoted value can contain ':' or '='; only separators before the first quote count.
            var quote = token.IndexOf(k_Quote);
            if (quote >= 0)
            {
                if (eq > quote) eq = -1;
                if (colon > quote) colon = -1;
            }

            if (eq < 0 && colon < 0)
                return -1;
            if (eq >= 0 && (colon < 0 || eq < colon))
            {
                op = Op.Equals;
                return eq;
            }
            op = Op.Contains;
            return colon;
        }

        static string Unquote(string s)
        {
            if (s.Length >= 2 && s[0] == k_Quote && s[s.Length - 1] == k_Quote)
                return s.Substring(1, s.Length - 2);
            if (s.Length > 0 && s[0] == k_Quote)
                return s.Substring(1);
            return s;
        }

        // Splits on whitespace and parentheses, keeping double-quoted spans together. Parentheses opened
        // inside a word (e.g. name=Noise(3D)) belong to that word.
        static List<string> Tokenize(string query)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            var wordGroupDepth = 0;

            void Flush()
            {
                if (sb.Length > 0)
                    tokens.Add(sb.ToString());
                sb.Clear();
                wordGroupDepth = 0;
            }

            foreach (var c in query)
            {
                if (c == k_Quote)
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
                else if (inQuotes)
                {
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    Flush();
                }
                else if (c == k_GroupOpen && sb.Length > 0 && !(sb.Length == 1 && sb[0] == k_NegationPrefix))
                {
                    ++wordGroupDepth;
                    sb.Append(c);
                }
                else if (c == k_GroupClose && wordGroupDepth > 0)
                {
                    --wordGroupDepth;
                    sb.Append(c);
                }
                else if (c == k_GroupOpen || c == k_GroupClose)
                {
                    Flush();
                    tokens.Add(c.ToString());
                }
                else
                {
                    sb.Append(c);
                }
            }
            Flush();
            return tokens;
        }
    }
}
