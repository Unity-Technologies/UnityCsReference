// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;

namespace UnityEditor.Experimental.GraphView
{
    // Matches templates while QuickSearch's index isn't ready. Handles simple queries only.
    class TemplateQueryMatcher
    {
        enum Op { Word, Equals, Contains }

        struct Term
        {
            public Op op;
            public string key;
            public string value;
        }

        readonly List<Term> m_Terms = new List<Term>();

        public bool IsMatchAll => m_Terms.Count == 0;

        // Area is based on the template's location and not raised through regular search terms.
        public string Area { get; private set; }

        public static TemplateQueryMatcher Parse(string query, string toolKey)
        {
            var matcher = new TemplateQueryMatcher();
            if (string.IsNullOrWhiteSpace(query))
                return matcher;

            foreach (var token in Tokenize(query))
            {
                if (TryParseArea(token, out var area))
                    matcher.Area = area;
                else if (TryParseTerm(token, toolKey, out var term))
                    matcher.m_Terms.Add(term);
            }
            return matcher;
        }

        public bool Matches(Dictionary<string, List<string>> document)
        {
            foreach (var term in m_Terms)
            {
                if (!MatchTerm(term, document))
                    return false;
            }
            return true;
        }

        static bool MatchTerm(Term term, Dictionary<string, List<string>> document)
        {
            if (term.op == Op.Word)
                return MatchWord(term.value, document);

            if (!document.TryGetValue(term.key, out var values))
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

        static bool MatchWord(string word, Dictionary<string, List<string>> document)
        {
            foreach (var entry in document)
            {
                if (!GraphViewIndexerExtension.IsWordSearchable(entry.Key))
                    continue;
                foreach (var value in entry.Value)
                {
                    foreach (var component in Search.SearchUtils.SplitEntryComponents(value, Search.SearchUtils.entrySeparators))
                    {
                        if (component.StartsWith(word, System.StringComparison.Ordinal))
                            return true;
                    }
                }
            }
            return false;
        }

        static bool TryParseTerm(string token, string toolKey, out Term term)
        {
            term = default;
            if (string.IsNullOrEmpty(token))
                return false;

            var separatorIndex = FirstSeparator(token, out var op);
            if (separatorIndex < 0)
            {
                // and/or/grouping aren't handled here; the index handles those once ready.
                var word = Unquote(token).ToLowerInvariant();
                if (word.Length == 0 || word == "and" || word == "or")
                    return false;
                term = new Term { op = Op.Word, value = word };
                return true;
            }

            var key = token.Substring(0, separatorIndex).ToLowerInvariant();
            var value = Unquote(token.Substring(separatorIndex + 1)).ToLowerInvariant();
            if (value.Length == 0)
                return false;

            // Type/area filters are handled elsewhere; ignore them.
            if (key == "t" || key == "a" || key == "area")
                return false;

            // 'l'/'label' is shorthand for the template's asset labels.
            if (key == "l" || key == "label")
                key = $"{toolKey}.label";

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
            if (key != "a" && key != "area")
                return false;

            area = Unquote(token.Substring(separatorIndex + 1)).ToLowerInvariant();
            return area.Length > 0;
        }

        static int FirstSeparator(string token, out Op op)
        {
            op = Op.Contains;
            var eq = token.IndexOf('=');
            var colon = token.IndexOf(':');

            // A quoted value can contain ':' or '='; only separators before the first quote count.
            var quote = token.IndexOf('"');
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
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 2);
            return s;
        }

        // Splits on whitespace, keeping double-quoted spans together.
        static List<string> Tokenize(string query)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;
            foreach (var c in query)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
                tokens.Add(sb.ToString());
            return tokens;
        }
    }
}
