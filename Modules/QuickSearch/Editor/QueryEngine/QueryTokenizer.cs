// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public const string k_BaseWordPattern = "[\\w.]+";
        public const string k_FilterOperatorsInnerPattern = "[^\\w\\s-{}()\"\\[\\].,/|\\`]+";
        // This is a balanced group for {} with an optional aggregator name
        public const string k_NestedFilterPattern = "(?<agg>[a-zA-Z0-9]*?)(?<nested>\\{(?>(?<c>\\{)|[^{}]+|(?<-c>\\}))*(?(c)(?!))\\})";
        public static readonly string k_FilterOperatorsPattern = $"(?<op>{k_FilterOperatorsInnerPattern})";
        public static readonly string k_PartialFilterOperatorsPattern = $"(?<op>{k_FilterOperatorsInnerPattern})?";

        public static readonly string k_FilterNamePattern = $"\\G(?<name>{k_BaseWordPattern})";
        // To begin matching a partial filter, the name of the filter must be followed by either "(" or an operator
        public static readonly string k_PartialFilterNamePattern = $"\\G(?<name>{k_BaseWordPattern})(?=\\(|(?:{k_FilterOperatorsInnerPattern}))";

        public const string k_FilterFunctionPattern = "(?<f1>\\([^\\(\\)]+\\))?";
        // This matches either a parenthesis opened with no space like "(asd", a close parenthesis with no space like "(asd)", or a close parenthesis with spaces like "( asd )"
        public const string k_PartialFilterFunctionPattern = "(?<f1>\\((?!\\S*(?:\\s+\\S*)+\\))[^\\(\\)\\s]*\\)?)?(?<f2>(?(f1)|\\([^\\(\\)]*\\)))?";

        public static readonly string k_FilterValuePattern = $"(?<value>{k_NestedFilterPattern}|[^\\s{{}}]+)";
        public static readonly string k_PartialFilterValuePattern = $"(?<value>(?(op)({k_NestedFilterPattern}|[^\\s{{}}]+)))?";

        public static readonly Regex k_BaseFilterNameRegex = new Regex(k_BaseWordPattern, RegexOptions.Compiled);
        public static readonly Regex k_WordRx = new Regex("\\G!?\\S+", RegexOptions.Compiled);
        public static readonly Regex k_NestedQueryRx = new Regex($"\\G{k_NestedFilterPattern}", RegexOptions.Compiled);
    }

    [Flags]
    enum QueryTextDelimiterEscapeOptions
    {
        EscapeNone = 0,
        EscapeOpening = 1,
        EscapeClosing = 1 << 1,
        EscapeAll = EscapeOpening | EscapeClosing
    }

    static class QueryTextDelimiterEscapeOptionsExtensions
    {
        public static bool HasAny(this QueryTextDelimiterEscapeOptions flags, QueryTextDelimiterEscapeOptions f) => (flags & f) != 0;
        public static bool HasAll(this QueryTextDelimiterEscapeOptions flags, QueryTextDelimiterEscapeOptions all) => (flags & all) == all;
    }

    readonly struct QueryTextDelimiter : IEquatable<QueryTextDelimiter>
    {
        internal const string k_QueryHandlerDataKey = "delimiterHashCode";
        internal const string k_WordBoundaryPattern = "(?=\\s|$)";

        public readonly string openingToken;
        public readonly string closingToken;
        public readonly QueryTextDelimiterEscapeOptions escapeOptions;
        public bool valid => !string.IsNullOrEmpty(openingToken) && !string.IsNullOrEmpty(closingToken);

        public readonly string escapedOpeningToken;
        public readonly string escapedClosingToken;

        public static QueryTextDelimiter invalid = new QueryTextDelimiter(null, null, QueryTextDelimiterEscapeOptions.EscapeNone);

        public QueryTextDelimiter(string openingToken, string closingToken)
            : this(openingToken, closingToken, QueryTextDelimiterEscapeOptions.EscapeAll)
        {}

        public QueryTextDelimiter(string openingToken)
            : this(openingToken, k_WordBoundaryPattern, QueryTextDelimiterEscapeOptions.EscapeOpening)
        {}

        public QueryTextDelimiter(string openingToken, string closingToken, QueryTextDelimiterEscapeOptions escapeOptions)
        {
            this.escapeOptions = escapeOptions;
            this.openingToken = openingToken;
            this.closingToken = closingToken;

            escapedOpeningToken = EscapeToken(openingToken, QueryTextDelimiterEscapeOptions.EscapeOpening, escapeOptions);
            escapedClosingToken = EscapeToken(closingToken, QueryTextDelimiterEscapeOptions.EscapeClosing, escapeOptions);
        }

        public bool Equals(QueryTextDelimiter other)
        {
            return openingToken == other.openingToken && closingToken == other.closingToken;
        }

        public override int GetHashCode()
        {
            return openingToken.GetHashCode() ^ closingToken.GetHashCode();
        }

        public override string ToString()
        {
            return $"({openingToken}, {closingToken})";
        }

        static string EscapeToken(string token, QueryTextDelimiterEscapeOptions requiredEscapeOptions, QueryTextDelimiterEscapeOptions currentEscapeOptions)
        {
            if (currentEscapeOptions.HasAll(requiredEscapeOptions))
                return Regex.Escape(token);
            return token;
        }
    }

    abstract class QueryTokenizer<TUserData>
    {
        const string k_QuoteHandlerKey = "quoteHandler";
        const string k_CommentHandlerKey = "commentHandler";
        const string k_ToggleHandlerKey = "toggleHandler";

        protected enum TokenHandlerPriority
        {
            Empty = 0,
            Group = 100,
            Combining = 200,
            Nested = 300,
            FilterWithQuotes = 350,
            Filter = 400,
            Toggle = 500,
            Comment = 600,
            Phrase = 650,
            Word = 700
        }

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
        protected delegate bool TokenConsumer(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);

        protected struct QueryTokenHandler
        {
            public TokenMatcher matcher;
            public TokenConsumer consumer;
            public int priority;
            public int id;
            public Dictionary<string, object> userData;

            public QueryTokenHandler(TokenMatcher matcher, TokenConsumer consumer, int id, int priority)
            {
                this.matcher = matcher;
                this.consumer = consumer;
                this.priority = priority;
                this.id = id;
                userData = new Dictionary<string, object>();
            }

            public QueryTokenHandler(TokenMatcher matcher, TokenConsumer consumer, TokenHandlerPriority priority)
                : this(matcher, consumer, 0, (int)priority)
            {}

            public QueryTokenHandler(TokenMatcher matcher, TokenConsumer consumer, int priority)
                : this(matcher, consumer, 0, priority)
            {}

            public QueryTokenHandler(TokenMatcher matcher, TokenConsumer consumer, int id, TokenHandlerPriority priority)
                : this(matcher, consumer, id, (int)priority)
            {}

            public QueryTokenHandler(int id, int priority)
                : this(null, null, id, priority)
            {}

            public void AddOrUpdateUserData(string key, object value)
            {
                userData[key] = value;
            }
        }

        protected List<QueryTokenHandler> m_TokenConsumers;
        protected List<QueryTextDelimiter> m_QuoteDelimiters;
        protected List<QueryTextDelimiter> m_CommentDelimiters;
        protected List<QueryTextDelimiter> m_ToggleDelimiters;

        protected QueryTokenizer()
        {
            m_TokenConsumers = new List<QueryTokenHandler>
            {
                new QueryTokenHandler(MatchEmpty, ConsumeEmpty, TokenHandlerPriority.Empty),
                new QueryTokenHandler(MatchGroup, ConsumeGroup, TokenHandlerPriority.Group),
                new QueryTokenHandler(MatchCombiningToken, ConsumeCombiningToken, TokenHandlerPriority.Combining),
                new QueryTokenHandler(MatchNestedQuery, ConsumeNestedQuery, TokenHandlerPriority.Nested),
                new QueryTokenHandler(MatchFilter, ConsumeFilter, TokenHandlerPriority.Filter),
                new QueryTokenHandler(MatchWord, ConsumeWord, TokenHandlerPriority.Word)
            };
            m_QuoteDelimiters = new List<QueryTextDelimiter>
            {
                new QueryTextDelimiter("\"", "\"")
            };
            m_CommentDelimiters = new List<QueryTextDelimiter>();
            m_ToggleDelimiters = new List<QueryTextDelimiter>
            {
                new QueryTextDelimiter("+")
            };
            AddCustomPhraseTokenHandler(0, (int)TokenHandlerPriority.Phrase, BuildPhraseRegex(m_QuoteDelimiters[0]), m_QuoteDelimiters[0], false);
            AddCustomFilterWithQuotesTokenHandler(0, (int)TokenHandlerPriority.FilterWithQuotes, m_QuoteDelimiters[0], null, false);
            AddCustomToggleTokenHandler(0, (int)TokenHandlerPriority.Toggle, BuildTextBlockRegex(m_ToggleDelimiters[0]), m_ToggleDelimiters[0], false);
            m_TokenConsumers.Sort(QueryTokenHandlerComparer);
        }

        public virtual void AddQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            AddQuoteDelimiter(in textDelimiter, null);
        }

        public virtual void AddQuoteDelimiter(in QueryTextDelimiter textDelimiter, IEnumerable<string> operators, bool sort = true)
        {
            AddTextBlockDelimiter(in textDelimiter, m_QuoteDelimiters, delimiter =>
            {
                // We need a new visitor for Phrases and Filters
                AddCustomPhraseTokenHandler(0, (int)TokenHandlerPriority.Phrase, BuildPhraseRegex(in delimiter), in delimiter, false);
                AddCustomFilterWithQuotesTokenHandler(0, (int)TokenHandlerPriority.FilterWithQuotes, in delimiter, operators, false);
            }, sort);
        }

        public virtual void RemoveQuoteDelimiter(in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            RemoveTextBlockDelimiter(in textDelimiter, m_QuoteDelimiters, k_QuoteHandlerKey, sort);
        }

        public virtual void AddCommentDelimiter(in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            AddTextBlockDelimiter(in textDelimiter, m_CommentDelimiters, delimiter =>
            {
                AddCustomCommentTokenHandler(0, (int)TokenHandlerPriority.Comment, BuildTextBlockRegex(in delimiter), in delimiter, false);
            }, sort);
        }

        public virtual void RemoveCommentDelimiter(in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            RemoveTextBlockDelimiter(in textDelimiter, m_CommentDelimiters, k_CommentHandlerKey, sort);
        }

        public virtual void AddToggleDelimiter(in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            AddTextBlockDelimiter(in textDelimiter, m_ToggleDelimiters, delimiter =>
            {
                AddCustomToggleTokenHandler(0, (int)TokenHandlerPriority.Toggle, BuildTextBlockRegex(in delimiter), in delimiter, false);
            }, sort);
        }

        public virtual void RemoveToggleDelimiter(in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            RemoveTextBlockDelimiter(in textDelimiter, m_ToggleDelimiters, k_ToggleHandlerKey, sort);
        }

        void AddTextBlockDelimiter(in QueryTextDelimiter textDelimiter, IList<QueryTextDelimiter> currentDelimiters, Action<QueryTextDelimiter> addDelimiterTokenHandlers, bool sort = true)
        {
            if (TextBlockDelimiterExist(in textDelimiter, currentDelimiters))
                return;

            addDelimiterTokenHandlers?.Invoke(textDelimiter);
            currentDelimiters.Add(textDelimiter);

            if (sort)
                SortQueryTokenHandlers();
        }

        void RemoveTextBlockDelimiter(in QueryTextDelimiter textDelimiter, IEnumerable<QueryTextDelimiter> currentDelimiters, string typeKey, bool sort = true)
        {
            if (!TextBlockDelimiterExist(in textDelimiter, currentDelimiters))
                return;

            var hashCode = textDelimiter.GetHashCode();
            RemoveQueryTokenHandlers(handler =>
            {
                if (!handler.userData.ContainsKey(typeKey) || !handler.userData.TryGetValue(QueryTextDelimiter.k_QueryHandlerDataKey, out var data))
                    return false;
                return (int)data == hashCode;
            });

            if (sort)
                SortQueryTokenHandlers();
        }

        protected void AddCustomPhraseTokenHandler(int handlerId, int priority, Regex matchRx, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            var queryTokenHandler = new QueryTokenHandler(handlerId, priority)
            {
                matcher = (string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched) => MatchText(matchRx, text, startIndex, endIndex, errors, out sv, out match, out matched),
                consumer = ConsumeWord
            };
            AddCustomQuoteTokenHandler(queryTokenHandler, in textDelimiter, sort);
        }

        protected void AddCustomCommentTokenHandler(int handlerId, int priority, Regex matchRx, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            var queryTokenHandler = new QueryTokenHandler(handlerId, priority)
            {
                matcher = (string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched) => MatchText(matchRx, text, startIndex, endIndex, errors, out sv, out match, out matched),
                consumer = ConsumeComment
            };
            ConfigureCommentTokenHandler(queryTokenHandler, in textDelimiter);
            AddQueryTokenHandler(queryTokenHandler, sort);
        }

        protected void AddCustomToggleTokenHandler(int handlerId, int priority, Regex matchRx, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            var queryTokenHandler = new QueryTokenHandler(handlerId, priority)
            {
                matcher = (string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched) => MatchText(matchRx, text, startIndex, endIndex, errors, out sv, out match, out matched),
                consumer = ConsumeToggle
            };
            ConfigureToggleTokenHandler(queryTokenHandler, in textDelimiter);
            AddQueryTokenHandler(queryTokenHandler, sort);
        }

        protected void AddCustomFilterWithQuotesTokenHandler(int handlerId, int priority, in QueryTextDelimiter textDelimiter, IEnumerable<string> operators, bool sort = true)
        {
            var customFilterRegex = BuildFilterRegex(QueryRegexValues.k_FilterNamePattern, BuildFilterValuePatternFromQuoteDelimiter(in textDelimiter), operators);
            var queryTokenHandler = new QueryTokenHandler(handlerId, priority)
            {
                matcher = (string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched) => MatchFilter(customFilterRegex, text, startIndex, endIndex, errors, out sv, out match, out matched),
                consumer = ConsumeFilter
            };
            AddCustomQuoteTokenHandler(queryTokenHandler, in textDelimiter, sort);
        }

        protected void AddCustomQuoteTokenHandler(QueryTokenHandler handler, in QueryTextDelimiter textDelimiter, bool sort)
        {
            ConfigureQuoteTokenHandler(handler, in textDelimiter);
            AddQueryTokenHandler(handler, sort);
        }

        protected void ConfigureQuoteTokenHandler(QueryTokenHandler handler, in QueryTextDelimiter textDelimiter)
        {
            ConfigureTextBlockTokenHandler(handler, in textDelimiter, k_QuoteHandlerKey);
        }

        protected void ConfigureCommentTokenHandler(QueryTokenHandler handler, in QueryTextDelimiter textDelimiter)
        {
            ConfigureTextBlockTokenHandler(handler, in textDelimiter, k_CommentHandlerKey);
        }

        protected void ConfigureToggleTokenHandler(QueryTokenHandler handler, in QueryTextDelimiter textDelimiter)
        {
            ConfigureTextBlockTokenHandler(handler, in textDelimiter, k_ToggleHandlerKey);
        }

        protected void ConfigureTextBlockTokenHandler(QueryTokenHandler handler, in QueryTextDelimiter textDelimiter, string typeKey)
        {
            handler.AddOrUpdateUserData(QueryTextDelimiter.k_QueryHandlerDataKey, textDelimiter.GetHashCode());
            handler.AddOrUpdateUserData(typeKey, true);
        }

        public bool QuoteDelimiterExist(in QueryTextDelimiter textDelimiter)
        {
            return TextBlockDelimiterExist(in textDelimiter, m_QuoteDelimiters);
        }

        public bool CommentDelimiterExist(in QueryTextDelimiter textDelimiter)
        {
            return TextBlockDelimiterExist(in textDelimiter, m_CommentDelimiters);
        }

        public bool ToggleDelimiterExist(in QueryTextDelimiter textDelimiter)
        {
            return TextBlockDelimiterExist(in textDelimiter, m_ToggleDelimiters);
        }

        public bool TextBlockDelimiterExist(in QueryTextDelimiter textDelimiter, IEnumerable<QueryTextDelimiter> currentDelimiters)
        {
            foreach (var delimiterTokens in currentDelimiters)
            {
                if (delimiterTokens.Equals(textDelimiter))
                    return true;
            }

            return false;
        }

        public bool IsPhraseToken(string token)
        {
            if (token.Length < 2)
                return false;
            var startIndex = token[0] == '!' ? 1 : 0;
            var sv = token.GetStringView(startIndex);
            return m_QuoteDelimiters.Any(tokens => sv.StartsWith(tokens.openingToken) && sv.EndsWith(tokens.closingToken));
        }

        public StringView RemoveQuotes(in StringView token)
        {
            if (token.length < 2)
                return token;

            foreach (var quoteDelimiter in m_QuoteDelimiters)
            {
                if (token.length < (quoteDelimiter.openingToken.Length + quoteDelimiter.closingToken.Length))
                    continue;

                if (token.StartsWith(quoteDelimiter.openingToken) && token.EndsWith(quoteDelimiter.closingToken))
                {
                    return token.Substring(quoteDelimiter.openingToken.Length, token.length - quoteDelimiter.openingToken.Length - quoteDelimiter.closingToken.Length);
                }
            }

            return token;
        }

        protected void AddQueryTokenHandler(QueryTokenHandler handler, bool sort = true)
        {
            m_TokenConsumers.Add(handler);

            if (sort)
                SortQueryTokenHandlers();
        }

        protected void RemoveQueryTokenHandlers(int id)
        {
            RemoveQueryTokenHandlers(handler => handler.id == id);
        }

        protected void RemoveQueryTokenHandlers(Func<QueryTokenHandler, bool> predicate)
        {
            m_TokenConsumers.RemoveAll(handler => predicate(handler));
        }

        protected void SortQueryTokenHandlers()
        {
            m_TokenConsumers.Sort(QueryTokenHandlerComparer);
        }

        static int QueryTokenHandlerComparer(QueryTokenHandler x, QueryTokenHandler y)
        {
            return x.priority.CompareTo(y.priority);
        }

        public void RebuildAllBasicFilterRegex(IEnumerable<string> operators, bool sort = true)
        {
            // Rebuild default filter regex
            BuildFilterRegex(operators);

            // Rebuild all token handlers for filters with quotes
            RemoveQueryTokenHandlers(handler => handler.priority == (int)TokenHandlerPriority.FilterWithQuotes);
            foreach (var quoteDelimiter in m_QuoteDelimiters)
            {
                AddCustomFilterWithQuotesTokenHandler(0, (int)TokenHandlerPriority.FilterWithQuotes, in quoteDelimiter, operators, false);
            }

            if (sort)
                SortQueryTokenHandlers();
        }

        public void BuildFilterRegex(IEnumerable<string> operators)
        {
            m_FilterRx = BuildFilterRegex(QueryRegexValues.k_FilterNamePattern, QueryRegexValues.k_FilterValuePattern, operators);
        }

        public static Regex BuildFilterRegex(string filterNamePattern, IEnumerable<string> operators)
        {
            return BuildFilterRegex(filterNamePattern, QueryRegexValues.k_FilterValuePattern, operators);
        }

        public static Regex BuildFilterRegex(string filterNamePattern, string filterValuePattern, IEnumerable<string> operators)
        {
            var filterOperatorsPattern = BuildOperatorsPattern(operators);
            return new Regex(filterNamePattern + QueryRegexValues.k_FilterFunctionPattern + filterOperatorsPattern + filterValuePattern, RegexOptions.Compiled);
        }

        public static Regex BuildBooleanFilterRegex(string filterNamePattern)
        {
            return new Regex(filterNamePattern + QueryRegexValues.k_FilterFunctionPattern, RegexOptions.Compiled);
        }

        public static Regex BuildPartialFilterRegex(string filterNamePattern, IEnumerable<string> operators)
        {
            return BuildPartialFilterRegex(filterNamePattern, QueryRegexValues.k_PartialFilterValuePattern, operators);
        }

        public static Regex BuildPartialFilterRegex(string filterNamePattern, string filterValuePattern, IEnumerable<string> operators)
        {
            var partialOperatorsPattern = BuildOperatorsPattern(operators) + "?";
            return new Regex(filterNamePattern + QueryRegexValues.k_PartialFilterFunctionPattern + partialOperatorsPattern + filterValuePattern);
        }

        public static string BuildFilterNamePatternFromToken(string token)
        {
            token = Regex.Escape(token);
            return BuildBasicFilterNamePattern(token);
        }

        static string BuildBasicFilterNamePattern(string token)
        {
            return $"\\G(?<name>{token})";
        }

        public static string BuildFilterNamePatternFromToken(Regex token)
        {
            var tokenString = token.ToString().GetStringView();
            if (tokenString.StartsWith("\\G"))
                tokenString = tokenString.Substring(2, tokenString.length - 2);

            var capturedGroup = GetCapturedGroup(tokenString);
            if (!capturedGroup.valid || capturedGroup.length <= 2)
                return BuildBasicFilterNamePattern(tokenString.ToString());

            var capturePattern = GetCapturePattern(capturedGroup);

            var sb = new StringBuilder(tokenString.length);
            if (!tokenString.StartsWith("\\G"))
                sb.Append("\\G");
            for (var i = 0; i < capturedGroup.startIndex; ++i)
                sb.Append(tokenString[i]);
            sb.Append($"(?<name>{capturePattern})");
            for (var i = capturedGroup.endIndex; i < tokenString.length; ++i)
                sb.Append(tokenString[i]);

            return sb.ToString();
        }

        static StringView GetCapturedGroup(StringView tokenString)
        {
            var startIndex = -1;
            var nestedLevel = 0;

            for (var i = 0; i < tokenString.length; ++i)
            {
                if (IsEscaped(tokenString, i))
                    continue;

                // We can only get capture group that we can name or are already named.
                if (startIndex == -1 && IsValidCaptureOpener(tokenString, i))
                {
                    startIndex = i;
                }

                if (startIndex != -1 && tokenString[i] == '(')
                    nestedLevel++;

                if (startIndex != -1 && IsValidCaptureCloser(tokenString, i))
                {
                    --nestedLevel;
                    if (nestedLevel == 0)
                    {
                        var endIndex = i + 1;
                        return tokenString.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }

            return StringView.nil;
        }

        static StringView GetCapturePattern(StringView captureGroup)
        {
            // We suppose the captureGroup was validated with IsValidCaptureOpener and IsValidCaptureClose

            if (!captureGroup.valid)
                return StringView.nil;
            if (!captureGroup.StartsWith('(') || !captureGroup.EndsWith(')'))
                return StringView.nil;
            if (captureGroup.length == 2)
                return StringView.empty;
            if (captureGroup[1] != '?')
                return captureGroup.Substring(1, captureGroup.length - 2);


            for (var i = 2; i < captureGroup.length - 1; ++i)
            {
                // End of the capture name
                if (captureGroup[i] == '>')
                    return captureGroup.Substring(i + 1, captureGroup.length - i - 2);
            }

            return StringView.nil;
        }

        static bool IsEscaped(StringView tokenString, int index)
        {
            if (index < 1 || index >= tokenString.length)
                return false;
            if (tokenString[index - 1] == '\\')
                return true;
            return false;
        }

        static bool IsValidCaptureOpener(StringView tokenString, int index)
        {
            if (index < 0 || index >= tokenString.length)
                return false;
            if (tokenString[index] != '(')
                return false;
            if (IsEscaped(tokenString, index))
                return false;
            if (index < tokenString.length - 1 && tokenString[index + 1] != '?')
                return true;
            index = index + 2;
            if (index >= tokenString.length)
                return false;
            if (tokenString[index] != '<')
                return false;
            ++index;
            if (index >= tokenString.length)
                return false;
            if (tokenString[index] == '=' || tokenString[index] == '!')
                return false;

            for (; index < tokenString.length; ++index)
            {
                if (tokenString[index] == '-')
                    return false;
                if (tokenString[index] == '>')
                    return true;
            }

            return false;
        }

        static bool IsValidCaptureCloser(StringView tokenString, int index)
        {
            if (index < 0 || index >= tokenString.length)
                return false;
            if (tokenString[index] != ')')
                return false;
            if (IsEscaped(tokenString, index))
                return false;
            return true;
        }

        public static string BuildPartialFilterNamePatternFromToken(string token, IEnumerable<string> operators)
        {
            var filterNamePattern = BuildFilterNamePatternFromToken(token);
            var innerOperatorsPattern = BuildInnerOperatorsPattern(operators);
            return $"{filterNamePattern}(?=\\(|(?:{innerOperatorsPattern}))";
        }

        public static string BuildPartialFilterNamePatternFromToken(Regex token, IEnumerable<string> operators)
        {
            var filterNamePattern = BuildFilterNamePatternFromToken(token);
            var innerOperatorsPattern = BuildInnerOperatorsPattern(operators);

            return $"{filterNamePattern}(?=\\(|(?:{innerOperatorsPattern}))";
        }

        public static Regex BuildPhraseRegex(in QueryTextDelimiter textDelimiter)
        {
            return new Regex(BuildTextBlockRegexPattern(in textDelimiter, "\\G!?", string.Empty));
        }

        public static Regex BuildTextBlockRegex(in QueryTextDelimiter textDelimiter)
        {
            return new Regex(BuildTextBlockRegexPattern(in textDelimiter, "\\G", string.Empty));
        }

        public static string BuildFilterValuePatternFromQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            return BuildTextBlockRegexPattern(in textDelimiter, "(?<value>", ")");
        }

        public static string BuildPartialFilterValuePatternFromQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            return BuildTextBlockRegexPattern(in textDelimiter, "(?<value>(?(op)(", ")))?");
        }

        static string BuildTextBlockRegexPattern(in QueryTextDelimiter textDelimiter, string prefix, string suffix)
        {
            return $"{prefix}{textDelimiter.escapedOpeningToken}(?<inner>.*?){textDelimiter.escapedClosingToken}{suffix}";
        }

        public static string BuildOperatorsPattern(IEnumerable<string> operators)
        {
            var innerOperatorsPattern = BuildInnerOperatorsPattern(operators);
            return $"(?<op>{innerOperatorsPattern})";
        }

        public static string BuildInnerOperatorsPattern(IEnumerable<string> operators)
        {
            if (operators == null)
                return QueryRegexValues.k_FilterOperatorsInnerPattern;

            var sortedOperators = operators.Select(Regex.Escape).ToList();
            sortedOperators.Sort((s, s1) => s1.Length.CompareTo(s.Length));
            return $"{string.Join("|", sortedOperators)}";
        }

        public ParseState Parse(string text, int startIndex, int endIndex, ICollection<QueryError> errors, TUserData userData)
        {
            var index = startIndex;
            while (index < endIndex)
            {
                var matched = false;
                foreach (var t in m_TokenConsumers)
                {
                    var matchLength = t.matcher(text, index, endIndex, errors, out var sv, out var match, out var consumerMatched);
                    if (!consumerMatched)
                        continue;
                    var consumed = t.consumer(text, index, index + matchLength, in sv, match, errors, userData);
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
                    errors.Add(new QueryError { index = index, reason = $"Error parsing string. No token could be deduced at {index}" });
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
            sv = text.GetWordView(startIndex);
            var totalUsableLength = endIndex - startIndex;

            foreach (var combiningToken in k_CombiningToken)
            {
                var tokenLength = combiningToken.Length;
                if (tokenLength > totalUsableLength)
                    continue;

                if (sv.Equals(combiningToken, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    return sv.length;
                }
            }

            matched = false;
            return -1;
        }

        int MatchFilter(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            return MatchFilter(m_FilterRx, text, startIndex, endIndex, errors, out sv, out match, out matched);
        }

        protected static int MatchFilter(Regex filterRx, string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = filterRx.Match(text, startIndex, endIndex - startIndex);

            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        static int MatchWord(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            return MatchText(QueryRegexValues.k_WordRx, text, startIndex, endIndex, errors, out sv, out match, out matched);
        }

        static int MatchText(Regex textRegex, string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = textRegex.Match(text, startIndex, endIndex - startIndex);
            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        static int MatchGroup(string text, int groupStartIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
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
                errors.Add(new QueryError { index = 0, reason = $"A group should have been found but index was {groupStartIndex}" });
                return -1;
            }

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

            var charConsumed = groupEndIndex - groupStartIndex + 1;
            if (parenthesisCounter != 0)
            {
                errors.Add(new QueryError { index = groupStartIndex, length = 1, reason = $"Unbalanced parentheses" });
                return -1;
            }

            sv = text.GetStringView(groupStartIndex, groupStartIndex + charConsumed);
            var innerSv = text.GetStringView(groupStartIndex + 1, groupStartIndex + charConsumed - 1);
            if (innerSv.IsNullOrWhiteSpace())
            {
                errors.Add(new QueryError { index = groupStartIndex, length = charConsumed, reason = $"Empty group" });
                return -1;
            }

            return charConsumed;
        }

        static int MatchNestedQuery(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
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

        protected abstract bool ConsumeEmpty(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeCombiningToken(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeFilter(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeWord(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeComment(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeToggle(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeGroup(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
        protected abstract bool ConsumeNestedQuery(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, TUserData userData);
    }
}
