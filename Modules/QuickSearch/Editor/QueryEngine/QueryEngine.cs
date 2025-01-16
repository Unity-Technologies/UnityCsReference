// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using NodesToStringPosition = System.Collections.Generic.Dictionary<UnityEditor.Search.IQueryNode, UnityEditor.Search.QueryToken>;

namespace UnityEditor.Search
{
    interface IParseResult
    {
        bool success { get; }
    }

    interface ITypeParser
    {
        Type type { get; }
        IParseResult Parse(string value);
    }

    readonly struct TypeParser<T> : ITypeParser
    {
        readonly Func<string, ParseResult<T>> m_Parser;

        public Type type => typeof(T);

        public TypeParser(Func<string, ParseResult<T>> parser)
        {
            m_Parser = parser;
        }

        public IParseResult Parse(string value)
        {
            return m_Parser(value);
        }
    }

    /// <summary>
    /// A ParseResult holds the result of a parsing operation.
    /// </summary>
    /// <typeparam name="T">Type of the result of the parsing operation.</typeparam>
    public readonly struct ParseResult<T> : IParseResult
    {
        /// <summary>
        /// Flag indicating if the parsing succeeded or not.
        /// </summary>
        public bool success { get; }

        /// <summary>
        /// Actual result of the parsing.
        /// </summary>
        public readonly T parsedValue;

        /// <summary>
        /// Create a ParseResult.
        /// </summary>
        /// <param name="success">Flag indicating if the parsing succeeded or not.</param>
        /// <param name="value">Actual result of the parsing.</param>
        public ParseResult(bool success, T value)
        {
            this.success = success;
            this.parsedValue = value;
        }

        /// <summary>
        /// Default value when no ParsetResult are available.
        /// </summary>
        public static readonly ParseResult<T> none = new ParseResult<T>(false, default(T));
    }

    interface IQueryEngineImplementation
    {
        void AddFilterOperationGenerator<T>();
        void AddDefaultEnumTypeParser<T>();
    }

    /// <summary>
    /// Struct containing the available query validation options.
    /// </summary>
    public struct QueryValidationOptions
    {
        /// <summary>
        /// Boolean indicating if filters should be validated.
        /// </summary>
        public bool validateFilters;

        /// <summary>
        /// Boolean indicating if unknown filters should be skipped.
        /// If validateFilters is true and skipUnknownFilters is false, unknown filters will generate errors
        /// if no default handler is provided.
        /// </summary>
        public bool skipUnknownFilters;

        /// <summary>
        /// Boolean indicating if incomplete filters should be skipped.
        /// If skipIncompleteFilters is false, incomplete filters will generate errors when parsed.
        /// </summary>
        public bool skipIncompleteFilters;

        internal bool skipNestedQueries;
        internal bool validateSyntaxOnly;
    }

    class QueryEngineParserData
    {
        public List<IQueryNode> expressionNodes;
        public NodesToStringPosition nodesToStringPosition;
        public ICollection<string> tokens;
        public ICollection<QueryToggle> toggles;

        public QueryEngineParserData(NodesToStringPosition nodesToStringPosition, ICollection<string> tokens, ICollection<QueryToggle> toggles)
        {
            expressionNodes = new List<IQueryNode>();
            this.nodesToStringPosition = nodesToStringPosition;
            this.tokens = tokens;
            this.toggles = toggles;
        }
    }

    struct BuildGraphResult
    {
        public QueryGraph evaluationGraph;
        public QueryGraph queryGraph;

        public BuildGraphResult(QueryGraph evaluationGraph, QueryGraph queryGraph)
        {
            this.evaluationGraph = evaluationGraph;
            this.queryGraph = queryGraph;
        }
    }

    static class DictionaryExtensions
    {
        public static bool TryGetValueByStringView(this Dictionary<string, QueryFilterOperator> source, in StringView key, out QueryFilterOperator op)
        {
            var keys = source.Keys;
            foreach (var k in keys)
            {
                if (key.Equals(k))
                {
                    op = source[k];
                    return true;
                }
            }

            op = QueryFilterOperator.invalid;
            return false;
        }

        public static bool TryGetValueByStringView(this IReadOnlyDictionary<string, QueryFilterOperator> source, in StringView key, out QueryFilterOperator op)
        {
            var keys = source.Keys;
            foreach (var k in keys)
            {
                if (key.Equals(k))
                {
                    op = source[k];
                    return true;
                }
            }

            op = QueryFilterOperator.invalid;
            return false;
        }
    }

    sealed class QueryEngineImpl<TData> : QueryTokenizer<QueryEngineParserData>, IQueryEngineImplementation
    {
        Dictionary<string, IFilter> m_Filters = new Dictionary<string, IFilter>();
        DefaultQueryFilterHandlerBase<TData> m_DefaultFilterHandler;
        DefaultQueryFilterHandlerBase<TData> m_DefaultParamFilterHandler;
        Dictionary<string, QueryFilterOperator> m_FilterOperators = new Dictionary<string, QueryFilterOperator>();
        List<ITypeParser> m_TypeParsers = new List<ITypeParser>();
        Dictionary<Type, ITypeParser> m_DefaultTypeParsers = new Dictionary<Type, ITypeParser>();
        Dictionary<Type, IFilterOperationGenerator> m_FilterOperationGenerators = new Dictionary<Type, IFilterOperationGenerator>();

        Regex m_PartialFilterRx = new Regex(QueryRegexValues.k_PartialFilterNamePattern +
            QueryRegexValues.k_PartialFilterFunctionPattern +
            QueryRegexValues.k_PartialFilterOperatorsPattern +
            QueryRegexValues.k_PartialFilterValuePattern, RegexOptions.Compiled);

        Regex m_BooleanFilterRx = new Regex(QueryRegexValues.k_FilterNamePattern +
            QueryRegexValues.k_FilterFunctionPattern, RegexOptions.Compiled);

        static readonly Dictionary<string, Func<IQueryNode>> k_CombiningTokenGenerators = new Dictionary<string, Func<IQueryNode>>
        {
            {"and", () => new AndNode()},
            {"or", () => new OrNode()},
            {"not", () => new NotNode()},
            {"-", () => new NotNode()}
        };

        enum ExtendedTokenHandlerPriority
        {
            Boolean = TokenHandlerPriority.Filter + 5,
            Partial = Boolean + 10,
            PartialWithQuotes = Partial - 5
        }

        readonly struct FilterCreationParams
        {
            public readonly string token;
            public readonly Regex regexToken;
            public readonly bool useRegexToken;
            public readonly string[] supportedOperators;
            public readonly bool overridesGlobalComparisonOptions;
            public readonly StringComparison comparisonOptions;
            public readonly bool useParameterTransformer;
            public readonly string parameterTransformerFunction;
            public readonly Type parameterTransformerAttributeType;

            public FilterCreationParams(
                string token, Regex regexToken, bool useRegexToken, string[] supportedOperators, bool overridesGlobalComparison,
                StringComparison comparisonOptions, bool useParameterTransformer,
                string parameterTransformerFunction, Type parameterTransformerAttributeType)
            {
                this.token = token;
                this.regexToken = regexToken;
                this.useRegexToken = useRegexToken;
                this.supportedOperators = supportedOperators;
                this.overridesGlobalComparisonOptions = overridesGlobalComparison;
                this.comparisonOptions = comparisonOptions;
                this.useParameterTransformer = useParameterTransformer;
                this.parameterTransformerFunction = parameterTransformerFunction;
                this.parameterTransformerAttributeType = parameterTransformerAttributeType;
            }
        }

        readonly struct CreateFilterTokenArgs
        {
            public readonly StringView token;
            public readonly Match match;
            public readonly int index;
            public readonly ICollection<QueryError> errors;

            public readonly StringView filterType;
            public readonly StringView filterParam;
            public readonly StringView filterOperator;
            public readonly StringView filterValue;
            public readonly StringView nestedQueryAggregator;

            public readonly int filterTypeIndex;
            public readonly int filterOperatorIndex;
            public readonly int filterValueIndex;

            public CreateFilterTokenArgs(in StringView token, Match match, int index, ICollection<QueryError> errors, in StringView filterType, in StringView filterParam, in StringView filterOperator, in StringView filterValue, in StringView nestedQueryAggregator, int filterTypeIndex,
                                         int filterOperatorIndex, int filterValueIndex)
            {
                this.token = token;
                this.match = match;
                this.index = index;
                this.errors = errors;
                this.filterType = filterType;
                this.filterParam = filterParam;
                this.filterOperator = filterOperator;
                this.filterValue = filterValue;
                this.nestedQueryAggregator = nestedQueryAggregator;
                this.filterTypeIndex = filterTypeIndex;
                this.filterOperatorIndex = filterOperatorIndex;
                this.filterValueIndex = filterValueIndex;
            }
        }

        public Func<TData, IEnumerable<string>> searchDataCallback { get; private set; }
        public Func<string, string> searchWordTransformerCallback { get; private set; }
        public Func<string, bool, StringComparison, string, bool> searchWordMatcher { get; private set; }

        public QueryValidationOptions validationOptions { get; set; }

        public StringComparison globalStringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;
        public StringComparison searchDataStringComparison { get; private set; } = StringComparison.OrdinalIgnoreCase;
        public bool searchDataOverridesGlobalStringComparison { get; private set; }

        INestedQueryHandler m_NestedQueryHandler;
        Dictionary<string, INestedQueryAggregator> m_NestedQueryAggregators = new Dictionary<string, INestedQueryAggregator>();

        HashSet<int> m_CustomFilterTokenHandlers = new HashSet<int>();

        delegate int MatchFilterFunction(Regex filterRx, string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched);
        delegate IQueryNode CreateFilterFunction(IFilter filter, string text, in StringView token, Match match, int index, ICollection<QueryError> errors);
        delegate IQueryNode CreateFilterTokenFunction(string text, in StringView token, Match match, int index, ICollection<QueryError> errors);
        delegate IQueryNode CreateTextNodeFunction(string text, in StringView token, Match match, ICollection<QueryError> errors, QueryEngineParserData data);

        public QueryEngineImpl()
            : this(new QueryValidationOptions())
        {}

        public QueryEngineImpl(QueryValidationOptions validationOptions)
        {
            this.validationOptions = validationOptions;

            // Default operators
            AddOperator(":", FilterOperatorType.Contains, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.Contains, ":"))
                .AddHandler((string ev, string fv, StringComparison sc) => ev?.IndexOf(fv, sc) >= 0)
                .AddHandler((int ev, int fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1)
                .AddHandler((float ev, float fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1)
                .AddHandler((double ev, double fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1);
            AddOperator("=", FilterOperatorType.Equal, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.Equal, "="))
                .AddHandler((int ev, int fv) => ev == fv)
                .AddHandler((float ev, float fv) => Math.Abs(ev - fv) < Mathf.Epsilon)
                .AddHandler((double ev, double fv) => Math.Abs(ev - fv) < double.Epsilon)
                .AddHandler((bool ev, bool fv) => ev == fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Equals(ev, fv, sc));
            AddOperator("!=", FilterOperatorType.NotEqual, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.NotEqual, "!="))
                .AddHandler((int ev, int fv) => ev != fv)
                .AddHandler((float ev, float fv) => Math.Abs(ev - fv) >= Mathf.Epsilon)
                .AddHandler((double ev, double fv) => Math.Abs(ev - fv) >= double.Epsilon)
                .AddHandler((bool ev, bool fv) => ev != fv)
                .AddHandler((string ev, string fv, StringComparison sc) => !string.Equals(ev, fv, sc));
            AddOperator("<", FilterOperatorType.Lesser, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.Lesser, "<"))
                .AddHandler((int ev, int fv) => ev < fv)
                .AddHandler((float ev, float fv) => ev < fv)
                .AddHandler((double ev, double fv) => ev < fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) < 0);
            AddOperator(">", FilterOperatorType.Greater, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.Greater, ">"))
                .AddHandler((int ev, int fv) => ev > fv)
                .AddHandler((float ev, float fv) => ev > fv)
                .AddHandler((double ev, double fv) => ev > fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) > 0);
            AddOperator("<=", FilterOperatorType.LesserOrEqual, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.LesserOrEqual, "<="))
                .AddHandler((int ev, int fv) => ev <= fv)
                .AddHandler((float ev, float fv) => ev <= fv)
                .AddHandler((double ev, double fv) => ev <= fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) <= 0);
            AddOperator(">=", FilterOperatorType.GreaterOrEqual, false)
                .AddHandler((FilterOperatorContext ctx, object ev, object fv, StringComparison sc) => CompareObjects(ctx, ev, fv, sc, FilterOperatorType.GreaterOrEqual, ">="))
                .AddHandler((int ev, int fv) => ev >= fv)
                .AddHandler((float ev, float fv) => ev >= fv)
                .AddHandler((double ev, double fv) => ev >= fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) >= 0);

            BuildFilterRegex();
            BuildDefaultTypeParsers();

            // Insert boolean filter matcher/consumer after normal filter
            AddQueryTokenHandler(new QueryTokenHandler(MatchBooleanFilter, ConsumeBooleanFilter, (int)ExtendedTokenHandlerPriority.Boolean), false);

            // Insert partial filter matcher/consumer before the word consumer
            AddQueryTokenHandler(new QueryTokenHandler(MatchPartialFilter, ConsumePartialFilter, (int)ExtendedTokenHandlerPriority.Partial));
        }

        bool CompareObjects(FilterOperatorContext ctx, object ev, object fv, StringComparison sc, FilterOperatorType op, string opToken)
        {
            if (ev == null || fv == null)
                return false;

            var evt = ev.GetType();
            var fvt = fv.GetType();

            // Check filter operators first
            if (ctx.filter != null && ctx.filter.operators.TryGetValue(opToken, out var filterOperators))
            {
                var opHandler = filterOperators.GetHandler(evt, fvt);
                if (opHandler != null)
                    return opHandler.Invoke(ctx, ev, fv, sc);
            }

            if (m_FilterOperators.TryGetValue(opToken, out var operators))
            {
                var opHandler = operators.GetHandler(evt, fvt);
                if (opHandler != null)
                    return opHandler.Invoke(ctx, ev, fv, sc);
            }

            if (evt != fvt)
                return false;

            if (ev is string evs && fv is string fvs)
            {
                switch (op)
                {
                    case FilterOperatorType.Contains: return evs.IndexOf(fvs, sc) != -1;
                    case FilterOperatorType.Equal: return evs.Equals(fvs, sc);
                    case FilterOperatorType.NotEqual: return !evs.Equals(fvs, sc);
                    case FilterOperatorType.Greater: return string.Compare(evs, fvs, sc) > 0;
                    case FilterOperatorType.GreaterOrEqual: return string.Compare(evs, fvs, sc) >= 0;
                    case FilterOperatorType.Lesser: return string.Compare(evs, fvs, sc) < 0;
                    case FilterOperatorType.LesserOrEqual: return string.Compare(evs, fvs, sc) <= 0;
                }

                return false;
            }

            switch (op)
            {
                case FilterOperatorType.Contains: return ev.ToString().IndexOf(fv.ToString(), sc) >= 0;
                case FilterOperatorType.Equal: return ev.Equals(fv);
                case FilterOperatorType.NotEqual: return !ev.Equals(fv);
                case FilterOperatorType.Greater: return Comparer<object>.Default.Compare(ev, fv) > 0;
                case FilterOperatorType.GreaterOrEqual: return Comparer<object>.Default.Compare(ev, fv) >= 0;
                case FilterOperatorType.Lesser: return Comparer<object>.Default.Compare(ev, fv) < 0;
                case FilterOperatorType.LesserOrEqual: return Comparer<object>.Default.Compare(ev, fv) <= 0;
            }

            return false;
        }

        public void AddFilter(string token, IFilter filter)
        {
            if (m_Filters.ContainsKey(token))
            {
                Debug.LogWarning($"A filter for \"{token}\" already exists. Please remove it first before adding a new one.");
                return;
            }
            m_Filters.Add(token, filter);
        }

        public void AddFilter(Regex token, IFilter filter)
        {
            AddFilter(token.ToString(), filter);
        }

        public void AddFilter(IFilter filter)
        {
            if (filter.usesRegularExpressionToken)
                AddFilter(filter.regexToken, filter);
            else
                AddFilter(filter.token, filter);
        }

        public void RemoveFilter(string token)
        {
            if (!m_Filters.ContainsKey(token))
            {
                Debug.LogWarning($"No filter found for \"{token}\".");
                return;
            }

            var filter = m_Filters[token];
            var filterId = filter.GetHashCode();
            if (m_CustomFilterTokenHandlers.Contains(filterId))
                RemoveQueryTokenHandlers(filterId);
            m_Filters.Remove(token);
        }

        public void RemoveFilter(Regex token)
        {
            RemoveFilter(token.ToString());
        }

        public void RemoveFilter(IFilter filter)
        {
            if (filter.usesRegularExpressionToken)
                RemoveFilter(filter.regexToken);
            else
                RemoveFilter(filter.token);
        }

        public void ClearFilters()
        {
            foreach (var filterId in m_CustomFilterTokenHandlers)
            {
                RemoveQueryTokenHandlers(filterId);
            }
            m_Filters.Clear();
            m_CustomFilterTokenHandlers.Clear();
        }

        public IFilter GetFilter(string token)
        {
            if (!m_Filters.ContainsKey(token))
            {
                Debug.LogWarning($"No filter found for \"{token}\".");
                return null;
            }
            return m_Filters[token];
        }

        public IFilter GetFilter(Regex token)
        {
            return GetFilter(token.ToString());
        }

        public IEnumerable<IFilter> GetAllFilters()
        {
            return m_Filters.Values;
        }

        public bool HasFilter(string token)
        {
            return m_Filters.ContainsKey(token);
        }

        public bool HasFilter(Regex token)
        {
            return HasFilter(token.ToString());
        }

        public bool TryGetFilter(string token, out IFilter filter)
        {
            return m_Filters.TryGetValue(token, out filter);
        }

        public bool TryGetFilter(Regex token, out IFilter filter)
        {
            return TryGetFilter(token.ToString(), out filter);
        }

        public QueryFilterOperator AddOperator(string op)
        {
            return AddOperator(op, true);
        }

        QueryFilterOperator AddOperator(string op, bool rebuildFilterRegex)
        {
            // This is called only by users. If they try to add an existing default
            // operator, the default operator is returned and the type doesn't matter.
            return AddOperator(op, FilterOperatorType.Custom, rebuildFilterRegex);
        }

        QueryFilterOperator AddOperator(string op, FilterOperatorType type, bool rebuildFilterRegex)
        {
            if (m_FilterOperators.ContainsKey(op))
                return m_FilterOperators[op];
            var filterOperator = new QueryFilterOperator(op, type, this);
            m_FilterOperators.Add(op, filterOperator);
            if (rebuildFilterRegex)
                BuildFilterRegex();
            return filterOperator;
        }

        public void RemoveOperator(string op)
        {
            RemoveOperator(op, true);
        }

        void RemoveOperator(string op, bool rebuildFilterRegex)
        {
            if (!m_FilterOperators.ContainsKey(op))
                return;
            m_FilterOperators.Remove(op);
            if (rebuildFilterRegex)
                BuildFilterRegex();
        }

        public QueryFilterOperator GetOperator(string op)
        {
            return m_FilterOperators.ContainsKey(op) ? m_FilterOperators[op] : QueryFilterOperator.invalid;
        }

        public void AddOperatorHandler<TLhs, TRhs>(string op, Func<TLhs, TRhs, bool> handler)
        {
            AddOperatorHandler<TLhs, TRhs>(op, (ctx, ev, fv, sc) => handler(ev, fv));
        }

        public void AddOperatorHandler<TLhs, TRhs>(string op, Func<TLhs, TRhs, StringComparison, bool> handler)
        {
            AddOperatorHandler<TLhs, TRhs>(op, (ctx, ev, fv, sc) => handler(ev, fv, sc));
        }

        public void AddOperatorHandler<TLhs, TRhs>(string op, Func<FilterOperatorContext, TLhs, TRhs, bool> handler)
        {
            AddOperatorHandler<TLhs, TRhs>(op, (ctx, ev, fv, sc) => handler(ctx, ev, fv));
        }

        public void AddOperatorHandler<TLhs, TRhs>(string op, Func<FilterOperatorContext, TLhs, TRhs, StringComparison, bool> handler)
        {
            if (!m_FilterOperators.ContainsKey(op))
                return;
            m_FilterOperators[op].AddHandler(handler);
        }

        public void SetDefaultFilterHandler(DefaultQueryFilterHandlerBase<TData> handler)
        {
            m_DefaultFilterHandler = handler;
        }

        public void SetDefaultParamFilterHandler(DefaultQueryFilterHandlerBase<TData> handler)
        {
            m_DefaultParamFilterHandler = handler;
        }

        public void SetSearchDataCallback(Func<TData, IEnumerable<string>> getSearchDataCallback)
        {
            searchDataCallback = getSearchDataCallback;
            // Remove the override flag in case it was already set.
            searchDataOverridesGlobalStringComparison = false;
        }

        public void SetSearchDataCallback(Func<TData, IEnumerable<string>> getSearchDataCallback, StringComparison stringComparison)
        {
            SetSearchDataCallback(getSearchDataCallback);
            searchDataStringComparison = stringComparison;
            searchDataOverridesGlobalStringComparison = true;
        }

        public void SetSearchDataCallback(Func<TData, IEnumerable<string>> getSearchDataCallback, Func<string, string> searchWordTransformerCallback, StringComparison stringComparison)
        {
            SetSearchDataCallback(getSearchDataCallback, stringComparison);
            this.searchWordTransformerCallback = searchWordTransformerCallback;
        }

        public void SetSearchWordMatcher(Func<string, bool, StringComparison, string, bool> wordMatcher)
        {
            searchWordMatcher = wordMatcher;
        }

        public void AddTypeParser<TFilterConstant>(Func<string, ParseResult<TFilterConstant>> parser)
        {
            m_TypeParsers.Add(new TypeParser<TFilterConstant>(parser));
            AddFilterOperationGenerator<TFilterConstant>();
        }

        public void SetNestedQueryHandler<TNestedQueryData>(Func<string, string, IEnumerable<TNestedQueryData>> handler)
        {
            if (handler == null)
                return;
            m_NestedQueryHandler = new NestedQueryHandler<TNestedQueryData>(handler);

            // Reset default aggregators with the correct type
            m_NestedQueryAggregators = new Dictionary<string, INestedQueryAggregator>();
            AddNestedQueryAggregator<TNestedQueryData>("max", MaxAggregator<TNestedQueryData>.Aggregate);
            AddNestedQueryAggregator<TNestedQueryData>("min", MinAggregator<TNestedQueryData>.Aggregate);
            AddNestedQueryAggregator<TNestedQueryData>("first", FirstAggregator<TNestedQueryData>.Aggregate);
            AddNestedQueryAggregator<TNestedQueryData>("last", LastAggregator<TNestedQueryData>.Aggregate);
        }

        public void SetFilterNestedQueryTransformer<TNestedQueryData, TRhs>(string filterToken, Func<TNestedQueryData, TRhs> transformer)
        {
            if (m_Filters.TryGetValue(filterToken, out var filter))
            {
                filter.SetNestedQueryTransformer(transformer);
            }
        }

        public void AddNestedQueryAggregator<TNestedQueryData>(string token, Func<IEnumerable<TNestedQueryData>, IEnumerable<TNestedQueryData>> aggregator)
        {
            var lowerToken = token.ToLowerInvariant();
            if (m_NestedQueryAggregators.ContainsKey(lowerToken))
            {
                Debug.LogWarning($"An aggregator for \"{token}\" already exists. Please remove it first before adding a new one.");
                return;
            }

            m_NestedQueryAggregators.Add(lowerToken, new NestedQueryAggregator<TNestedQueryData>(aggregator));
        }

        public void RemoveNestedQueryAggregator<TNestedQuery>(string token)
        {
            var lowerToken = token.ToLowerInvariant();
            if (!m_NestedQueryAggregators.ContainsKey(lowerToken))
            {
                Debug.LogWarning($"No aggregator found for \"{token}\".");
                return;
            }
            m_NestedQueryAggregators.Remove(token);
        }

        public IFilterOperationGenerator GetGeneratorForType(Type type)
        {
            if (!m_FilterOperationGenerators.ContainsKey(type))
            {
                return null;
            }
            return m_FilterOperationGenerators[type];
        }

        public override void AddQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            AddQuoteDelimiter(textDelimiter, m_FilterOperators.Keys);
        }

        public void RemoveQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            RemoveQuoteDelimiter(textDelimiter, true);
        }

        IQueryNode BuildGraphRecursively(string text, int startIndex, int endIndex, ICollection<QueryError> errors, ICollection<string> tokens, ICollection<QueryToggle> toggles, NodesToStringPosition nodesToStringPosition)
        {
            var userData = new QueryEngineParserData(nodesToStringPosition, tokens, toggles);
            var result = Parse(text, startIndex, endIndex, errors, userData);
            if (result != ParseState.Matched)
                return null;

            InsertAndIfNecessary(userData.expressionNodes, nodesToStringPosition);
            var rootNode = CombineNodesToTree(userData.expressionNodes, errors, nodesToStringPosition);
            return rootNode;
        }

        internal BuildGraphResult BuildGraph(string text, ICollection<QueryError> errors, ICollection<string> tokens, ICollection<QueryToggle> toggles)
        {
            if (text == null)
                return new BuildGraphResult(new QueryGraph(null), new QueryGraph(null));

            var nodesToStringPosition = new NodesToStringPosition();
            var evaluationRootNode = BuildGraphRecursively(text, 0, text.Length, errors, tokens, toggles, nodesToStringPosition);

            // Make a copy of the query graph for the stock query graph
            IQueryNode queryRootNode = null;
            if (evaluationRootNode != null)
            {
                if (!(evaluationRootNode is ICopyableNode copyableRoot))
                    throw new Exception($"Root node should be an instance of {nameof(ICopyableNode)}.");
                queryRootNode = copyableRoot.Copy();
            }

            // When validating syntax only, set the evaluation node to null to prevent further errors.
            if (validationOptions.validateSyntaxOnly)
                evaluationRootNode = null;

            // Final simplification
            RemoveGroupNodes(ref evaluationRootNode, errors, nodesToStringPosition);
            RemoveSkippedNodes(ref evaluationRootNode, errors, nodesToStringPosition);

            evaluationRootNode = InsertWhereNode(evaluationRootNode, errors, nodesToStringPosition);

            // Validate the evaluationGraph at the final stage, not when building all the subgraphs
            // otherwise you will validate each nodes multiple times. A null node
            // is valid if we removed all noOp nodes.
            if (evaluationRootNode != null)
                ValidateGraph(evaluationRootNode, errors, nodesToStringPosition);

            if (evaluationRootNode != null && !IsEnumerableNode(evaluationRootNode))
            {
                errors.Add(new QueryError(evaluationRootNode.token.position, evaluationRootNode.token.length, "Only enumerable nodes can be at the root level"));
                evaluationRootNode.skipped = false;
            }

            return new BuildGraphResult(new QueryGraph(evaluationRootNode), new QueryGraph(queryRootNode));
        }

        protected override bool ConsumeEmpty(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return true;
        }

        protected override bool ConsumeCombiningToken(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var token = sv.ToString();
            IQueryNode newNode = null;

            foreach (var tokenGenerator in k_CombiningTokenGenerators)
            {
                if (sv.Equals(tokenGenerator.Key, StringComparison.OrdinalIgnoreCase))
                {
                    newNode = tokenGenerator.Value();
                    break;
                }
            }

            if (newNode == null)
            {
                errors.Add(new QueryError(startIndex, endIndex - startIndex, $"No node generator found for combining token \"{token}\"."));
                return false;
            }

            newNode.token = new QueryToken(in sv, startIndex);
            userData.nodesToStringPosition.Add(newNode, new QueryToken(startIndex, sv.length));
            userData.expressionNodes.Add(newNode);
            userData.tokens.Add(token);
            return true;
        }

        protected override bool ConsumeFilter(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return ConsumeFilter(text, startIndex, endIndex, in sv, match, errors, userData, CreateFilterToken);
        }

        int MatchBooleanFilter(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            return MatchBooleanFilter(m_BooleanFilterRx, text, startIndex, endIndex, errors, out sv, out match, out matched);
        }

        int MatchBooleanFilter(Regex booleanFilterRx, string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = booleanFilterRx.Match(text, startIndex, endIndex - startIndex);
            matched = false;

            if (!match.Success)
                return -1;

            if (match.Groups.Count < 2)
                return -1;

            var filterType = match.Groups[1].Value;
            if (!m_Filters.TryGetValue(filterType, out var filter))
                return -1;

            if (filter.type != typeof(bool))
                return -1;

            matched = true;
            return match.Length;
        }

        static int MatchKnownBooleanFilter(Regex booleanFilterRx, string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = booleanFilterRx.Match(text, startIndex, endIndex - startIndex);
            matched = false;

            if (!match.Success)
                return -1;

            if (match.Groups.Count < 2)
                return -1;

            matched = true;
            return match.Length;
        }

        bool ConsumeBooleanFilter(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return ConsumeFilter(text, startIndex, endIndex, in sv, match, errors, userData, CreateBooleanFilterToken);
        }

        int MatchPartialFilter(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            return MatchFilter(m_PartialFilterRx, text, startIndex, endIndex, errors, out sv, out match, out matched);
        }

        bool ConsumePartialFilter(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return ConsumeFilter(text, startIndex, endIndex, in sv, match, errors, userData, CreatePartialFilterToken);
        }

        static bool ConsumeFilter(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData, CreateFilterTokenFunction createFilterFunc)
        {
            var svToken = new StringView(text, startIndex, startIndex + match.Length);
            var node = createFilterFunc(text, in svToken, match, startIndex, errors);
            if (node == null)
                return false;

            node.token = new QueryToken(in svToken, startIndex);
            userData.nodesToStringPosition.Add(node, new QueryToken(startIndex, match.Length));
            userData.expressionNodes.Add(node);
            userData.tokens.Add(match.Value);
            return true;
        }

        protected override bool ConsumeWord(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            if (validationOptions.validateFilters && searchDataCallback == null && !validationOptions.validateSyntaxOnly)
            {
                errors.Add(new QueryError(startIndex, match.Length, "Cannot use a search word without setting the search data callback."));
                return false;
            }

            return ConsumeText(text, startIndex, endIndex, in sv, match, errors, userData, CreateWordExpressionNode);
        }

        protected override bool ConsumeComment(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return ConsumeText(text, startIndex, endIndex, in sv, match, errors, userData, CreateCommentNode);
        }

        protected override bool ConsumeToggle(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return ConsumeText(text, startIndex, endIndex, in sv, match, errors, userData, CreateToggleNode);
        }

        static bool ConsumeText(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData, CreateTextNodeFunction createNodeFunc)
        {
            var tokenSv = new StringView(text, startIndex, startIndex + match.Length);
            var node = createNodeFunc(text, in tokenSv, match, errors, userData);
            if (node == null)
                return false;

            node.token = new QueryToken(in tokenSv, startIndex);
            userData.nodesToStringPosition.Add(node, new QueryToken(startIndex, match.Length));
            userData.expressionNodes.Add(node);
            if (!node.skipped)
                userData.tokens.Add(match.Value);
            return true;
        }

        protected override bool ConsumeGroup(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var innerNode = BuildGraphRecursively(text, startIndex + 1, endIndex - 1, errors, userData.tokens, userData.toggles, userData.nodesToStringPosition);
            if (innerNode == null)
                return false;

            var token = new QueryToken(in sv, startIndex);
            var groupNode = new GroupNode(in sv) { token = token };
            groupNode.AddNode(innerNode);
            userData.expressionNodes.Add(groupNode);
            userData.nodesToStringPosition.Add(groupNode, token);
            return true;
        }

        protected override bool ConsumeNestedQuery(string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            if (validationOptions.validateFilters && !validationOptions.skipNestedQueries && !validationOptions.validateSyntaxOnly)
            {
                if (m_NestedQueryHandler == null)
                {
                    errors.Add(new QueryError(startIndex, match.Length, "Cannot use a nested query without setting the handler first."));
                    return false;
                }

                if (m_NestedQueryHandler.enumerableType != typeof(TData))
                {
                    errors.Add(new QueryError(startIndex, match.Length, "When not in a filter, a nested query must return the same type as the QueryEngine it is used in."));
                    return false;
                }
            }

            var aggregatorGroup = match.Groups["agg"];
            var nestedGroup = match.Groups["nested"];
            var nestedQueryAggregator = GetStringViewFromMatch(text, aggregatorGroup);
            var queryString = GetStringViewFromMatch(text, nestedGroup);

            var nestedQueryString = queryString.Substring(1, queryString.length - 2);
            nestedQueryString = nestedQueryString.Trim();
            var nestedQueryNode = new NestedQueryNode(in nestedQueryString, in queryString, m_NestedQueryHandler);
            nestedQueryNode.token = new QueryToken(startIndex + (nestedQueryAggregator.IsNullOrEmpty() ? 0 : nestedQueryAggregator.length), queryString.length);
            userData.nodesToStringPosition.Add(nestedQueryNode, nestedQueryNode.token);
            if (validationOptions.skipNestedQueries)
            {
                nestedQueryNode.skipped = true;
                userData.expressionNodes.Add(nestedQueryNode);
                return true;
            }

            userData.tokens.Add(match.Value);

            if (nestedQueryAggregator.IsNullOrEmpty())
                userData.expressionNodes.Add(nestedQueryNode);
            else
            {
                var lowerAggregator = nestedQueryAggregator.ToString().ToLowerInvariant();
                if (!m_NestedQueryAggregators.TryGetValue(lowerAggregator, out var aggregator) && validationOptions.validateFilters && !validationOptions.validateSyntaxOnly)
                {
                    errors.Add(new QueryError(startIndex, nestedQueryAggregator.length, $"Unknown nested query aggregator \"{nestedQueryAggregator}\""));
                    return false;
                }

                var aggregatorNode = new AggregatorNode(in nestedQueryAggregator, aggregator);
                aggregatorNode.token = new QueryToken(startIndex, nestedQueryAggregator.length + nestedQueryNode.token.length);
                userData.nodesToStringPosition.Add(aggregatorNode, aggregatorNode.token);
                aggregatorNode.children.Add(nestedQueryNode);
                nestedQueryNode.parent = aggregatorNode;
                userData.expressionNodes.Add(aggregatorNode);
            }

            return true;
        }

        static void InsertAndIfNecessary(List<IQueryNode> nodes, NodesToStringPosition nodesToStringPosition)
        {
            if (nodes.Count <= 1)
                return;

            for (var i = 0; i < nodes.Count - 1; ++i)
            {
                if (nodes[i] is CombinedNode cn && cn.leaf)
                    continue;
                if (nodes[i + 1] is CombinedNode nextCn && nextCn.leaf && nextCn.type != QueryNodeType.Not)
                    continue;

                var andNode = new AndNode();
                var previousNodePosition = nodesToStringPosition[nodes[i]];
                var nextNodePosition = nodesToStringPosition[nodes[i + 1]];
                var startPosition = previousNodePosition.position + previousNodePosition.length;
                var length = nextNodePosition.position - startPosition;
                nodesToStringPosition.Add(andNode, new QueryToken(startPosition, length));
                nodes.Insert(i + 1, andNode);
                andNode.token = new QueryToken("", startPosition, length);
                // Skip this new node
                ++i;
            }
        }

        IQueryNode CreateFilterToken(string text, in StringView token, Match match, int index, ICollection<QueryError> errors)
        {
            return CreateFilterToken(null, text, in token, match, index, errors);
        }

        IQueryNode CreateFilterToken(IFilter filter, string text, in StringView token, Match match, int index, ICollection<QueryError> errors)
        {
            // Mandatory groups
            var filterTypeGroup = match.Groups["name"];
            var filterOperatorGroup = match.Groups["op"];
            var filterValueGroup = match.Groups["value"];
            var filterParamGroup = match.Groups["f1"];
            var nestedQueryGroup = match.Groups["agg"];

            if (!filterTypeGroup.Success || !filterOperatorGroup.Success || !filterValueGroup.Success)
            {
                errors.Add(new QueryError(index, token.length, $"Could not parse filter block \"{token}\"."));
                return null;
            }

            var filterType = GetStringViewFromMatch(text, filterTypeGroup);
            var filterParam = GetStringViewFromMatch(text, filterParamGroup);
            var filterOperator = GetStringViewFromMatch(text, filterOperatorGroup);
            var filterValue = GetStringViewFromMatch(text, filterValueGroup);
            var nestedQueryAggregator = GetStringViewFromMatch(text, nestedQueryGroup);

            var filterTypeIndex = filterTypeGroup.Index;
            var filterOperatorIndex = filterOperatorGroup.Index;
            var filterValueIndex = filterValueGroup.Index;

            if (!filterParam.IsNullOrEmpty())
            {
                // Trim () around the group
                filterParam = filterParam.Trim('(', ')');
            }

            var args = new CreateFilterTokenArgs(in token, match, index, errors, in filterType, in filterParam, in filterOperator, in filterValue, in nestedQueryAggregator, filterTypeIndex, filterOperatorIndex, filterValueIndex);
            if (filter == null && !GetFilter(in args, out var queryNode, out filter))
            {
                return queryNode;
            }

            return CreateFilterToken(filter, in args);
        }

        IQueryNode CreateFilterToken(IFilter filter, in CreateFilterTokenArgs args)
        {
            if (!GetFilterOperator(filter, in args.filterOperator, out var op))
            {
                args.errors.Add(new QueryError(args.filterOperatorIndex, args.filterOperator.length, $"Unknown filter operator \"{args.filterOperator.ToString()}\"."));
                return null;
            }

            if (!validationOptions.validateSyntaxOnly && filter.supportedOperators.Any() && !filter.supportedOperators.Any(filterOp => filterOp.Equals(op.token)))
            {
                args.errors.Add(new QueryError(args.filterOperatorIndex, args.filterOperator.length, $"The operator \"{op.token}\" is not supported for this filter."));
                return null;
            }

            var rawFilterValue = args.filterValue;
            var filterValue = RemoveQuotes(in rawFilterValue);

            IQueryNode filterNode;

            if (QueryEngineUtils.IsNestedQueryToken(in filterValue))
            {
                if (validationOptions.skipNestedQueries)
                {
                    return new InFilterNode(filter, args.filterType, in op, args.filterOperator, filterValue, rawFilterValue, args.filterParam, args.token) { skipped = true };
                }

                if (validationOptions.validateFilters && m_NestedQueryHandler == null && !validationOptions.validateSyntaxOnly)
                {
                    args.errors.Add(new QueryError(args.filterValueIndex, args.filterValue.length, $"Cannot use a nested query without setting the handler first."));
                    return null;
                }

                filterNode = new InFilterNode(filter, args.filterType, in op, args.filterOperator, filterValue, rawFilterValue, args.filterParam, args.token);

                var startIndex = filterValue.IndexOf('{');
                var rawNestedQuery = filterValue.Substring(startIndex, filterValue.length - startIndex);
                filterValue = rawNestedQuery.Substring(1, rawNestedQuery.length - 2);
                filterValue = filterValue.Trim();
                var nestedQueryNode = new NestedQueryNode(in filterValue, in rawNestedQuery, in args.filterType, m_NestedQueryHandler);
                nestedQueryNode.token = new QueryToken(filterValue, args.filterValueIndex);

                if (!args.nestedQueryAggregator.IsNullOrEmpty())
                {
                    var lowerAggregator = args.nestedQueryAggregator.ToString().ToLowerInvariant();
                    if (!m_NestedQueryAggregators.TryGetValue(lowerAggregator, out var aggregator) && validationOptions.validateFilters && !validationOptions.validateSyntaxOnly)
                    {
                        args.errors.Add(new QueryError(args.filterValueIndex, args.nestedQueryAggregator.length, $"Unknown nested query aggregator \"{args.nestedQueryAggregator}\""));
                        return null;
                    }

                    var aggregatorNode = new AggregatorNode(args.nestedQueryAggregator, aggregator);
                    aggregatorNode.token = new QueryToken(args.nestedQueryAggregator, args.filterValueIndex);

                    filterNode.children.Add(aggregatorNode);
                    aggregatorNode.parent = filterNode;
                    aggregatorNode.children.Add(nestedQueryNode);
                    nestedQueryNode.parent = aggregatorNode;
                }
                else
                {
                    filterNode.children.Add(nestedQueryNode);
                    nestedQueryNode.parent = filterNode;
                }
            }
            else
            {
                filterNode = new FilterNode(filter, in args.filterType, in op, in args.filterOperator, in filterValue, in rawFilterValue, in args.filterParam, in args.token);
            }

            return filterNode;
        }

        IQueryNode CreateBooleanFilterToken(string text, in StringView token, Match match, int index, ICollection<QueryError> errors)
        {
            return CreateBooleanFilterToken(null, text, in token, match, index, errors);
        }

        IQueryNode CreateBooleanFilterToken(IFilter filter, string text, in StringView token, Match match, int index, ICollection<QueryError> errors)
        {
            if (match.Groups.Count != 3) // There is 3 groups
            {
                errors.Add(new QueryError(index, token.length, $"Could not parse filter block \"{token}\"."));
                return null;
            }

            var filterType = GetStringViewFromMatch(text, match.Groups["name"]);
            var filterParam = GetStringViewFromMatch(text, match.Groups["f1"]);
            var filterTypeIndex = match.Groups["name"].Index;

            if (!filterParam.IsNullOrEmpty())
            {
                // Trim () around the group
                filterParam = filterParam.Trim('(', ')');
            }

            var args = new CreateFilterTokenArgs(in token, match, index, errors, in filterType, in filterParam, in StringView.empty, in StringView.empty, in StringView.empty, filterTypeIndex, 0, 0);
            if (filter == null && !GetFilter(in args, out var queryNode, out filter))
            {
                return queryNode;
            }
            return CreateBooleanFilterToken(filter, in args);
        }

        IQueryNode CreateBooleanFilterToken(IFilter filter, in CreateFilterTokenArgs args)
        {
            // Boolean filters default to equal
            var filterOperator = "=".GetStringView();
            if (!GetFilterOperator(filter, in filterOperator, out var op))
            {
                args.errors.Add(new QueryError(args.index, args.token.length, $"Boolean filter not supported."));
                return null;
            }

            var operatorIdSv = filterOperator;
            var filterValueSv = new StringView("true");
            IQueryNode filterNode = new FilterNode(filter, in args.filterType, in op, in operatorIdSv, in filterValueSv, in filterValueSv, in args.filterParam, in args.token);
            return filterNode;
        }

        IQueryNode CreatePartialFilterToken(string text, in StringView token, Match match, int index, ICollection<QueryError> errors)
        {
            return CreatePartialFilterToken(null, text, in token, match, index, errors);
        }

        IQueryNode CreatePartialFilterToken(IFilter filter, string text, in StringView token, Match match, int index, ICollection<QueryError> errors)
        {
            var filterTypeGroup = match.Groups["name"];
            var filterOperatorGroup = match.Groups["op"];
            var filterValueGroup = match.Groups["value"];

            var filterType = GetStringViewFromMatch(text, filterTypeGroup);
            var filterParam = GetStringViewFromMatch(text, match.Groups["f1"]);
            if (filterParam.IsNullOrEmpty())
                filterParam = GetStringViewFromMatch(text, match.Groups["f2"]);
            var filterOperator = GetStringViewFromMatch(text, filterOperatorGroup);
            var filterValue = GetStringViewFromMatch(text, filterValueGroup);

            var filterTypeIndex = filterTypeGroup.Index;
            var filterOperatorIndex = filterOperatorGroup.Index;
            var filterValueIndex = filterValueGroup.Index;

            if (!filterParam.IsNullOrEmpty())
            {
                // Trim () around the group
                filterParam = filterParam.Trim('(', ')');
            }

            if (validationOptions.skipIncompleteFilters)
            {
                return new FilterNode(in filterType, in filterOperator, in filterValue, in filterValue, in filterParam, in token) { skipped = true };
            }

            var args = new CreateFilterTokenArgs(in token, match, index, errors, in filterType, in filterParam, in filterOperator, in filterValue, in StringView.empty, filterTypeIndex, filterOperatorIndex, filterValueIndex);
            if (filter == null && !GetFilter(in args, out var queryNode, out filter))
            {
                return queryNode;
            }
            return CreatePartialFilterToken(filter, in args);
        }

        IQueryNode CreatePartialFilterToken(IFilter filter, in CreateFilterTokenArgs args)
        {
            var op = QueryFilterOperator.invalid;
            if (!args.filterOperator.IsNullOrEmpty())
            {
                if (!GetFilterOperator(filter, in args.filterOperator, out op))
                {
                    args.errors.Add(new QueryError(args.filterOperatorIndex, args.filterOperator.length, $"Unknown filter operator \"{args.filterOperator}\"."));
                    return null;
                }

                var opToken = op.token;
                if (!validationOptions.validateSyntaxOnly && filter.supportedOperators.Any() && !filter.supportedOperators.Any(filterOp => filterOp.Equals(opToken)))
                {
                    args.errors.Add(new QueryError(args.filterOperatorIndex, args.filterOperator.length, $"The operator \"{op.token}\" is not supported for this filter."));
                    return null;
                }
            }

            var rawFilterValue = args.filterValue;
            var filterValue = RemoveQuotes(in rawFilterValue);

            var filterNode = new FilterNode(filter, in args.filterType, in op, in args.filterOperator, in filterValue, in rawFilterValue, in args.filterParam, in args.token);
            args.errors.Add(new QueryError(args.index, args.token.length, $"The filter \"{args.filterType}\" is incomplete."));

            return filterNode;
        }

        bool GetFilter(in CreateFilterTokenArgs args, out IQueryNode queryNode, out IFilter filter)
        {
            queryNode = null;
            var filterTypeStr = args.filterType.ToString();
            if (!m_Filters.TryGetValue(filterTypeStr, out filter))
            {
                // When skipping unknown filter, just return a noop. The graph will get simplified later.
                if (validationOptions.skipUnknownFilters)
                {
                    queryNode = new FilterNode(in args.filterType, in args.filterOperator, in args.filterValue, in args.filterValue, in args.filterParam, in args.token) { skipped = true };
                    return false;
                }

                if (!validationOptions.validateSyntaxOnly && !HasDefaultHandler(!args.filterParam.IsNullOrEmpty()) && validationOptions.validateFilters)
                {
                    args.errors.Add(new QueryError(args.filterTypeIndex, args.filterType.length, $"Unknown filter \"{filterTypeStr}\"."));
                    queryNode = null;
                    return false;
                }
                filter = CreateDefaultFilter(filterTypeStr, args.filterParam);
            }

            return true;
        }

        bool GetFilterOperator(IFilter filter, in StringView operatorToken, out QueryFilterOperator op)
        {
            op = QueryFilterOperator.invalid;
            if (filter.operators.TryGetValueByStringView(in operatorToken, out op))
                return true;
            if (m_FilterOperators.TryGetValueByStringView(in operatorToken, out op))
                return true;
            return op.valid;
        }

        IFilter CreateDefaultFilter(string filterToken, in StringView filterParam)
        {
            if (filterParam.IsNullOrEmpty())
                return m_DefaultFilterHandler?.CreateFilter(filterToken, this) ?? new DefaultFilterResolver<TData, string>(filterToken, (o, s, fo, value) => false, this);
            return m_DefaultParamFilterHandler?.CreateFilter(filterToken, this) ?? new DefaultParamFilterResolver<TData, string, string>(filterToken, (o, s, param, fo, value) => false, this);
        }

        private bool HasDefaultHandler(bool useParamFilter)
        {
            return useParamFilter ? m_DefaultParamFilterHandler != null : m_DefaultFilterHandler != null;
        }

        public IParseResult ParseFilterValue(string filterValue, IFilter filter, in QueryFilterOperator op, out Type filterValueType)
        {
            var foundValueType = false;
            IParseResult parseResult = null;
            filterValueType = filter.type;

            // Filter resolver only support values of the same type as the filter in their resolver
            if (filter.usesResolver)
            {
                return ParseSpecificType(filterValue, filter.type, filter);
            }

            // Check custom parsers first
            foreach (var typeParser in filter.typeParsers.Concat(m_TypeParsers))
            {
                parseResult = typeParser.Parse(filterValue);
                if (parseResult.success)
                {
                    filterValueType = typeParser.type;
                    foundValueType = true;
                    break;
                }
            }

            // If no custom type parsers managed to parse the string, try our default ones with the types of handlers available for the operator
            if (!foundValueType)
            {
                // OPTME: Not the prettiest bit of code, but we got rid of LINQ at least.
                var handlerTypes = new List<FilterOperatorTypes>();
                foreach (var handlersKey in op.handlers.Keys)
                {
                    if (handlersKey != filter.type)
                        continue;

                    foreach (var rhsType in op.handlers[handlersKey].Keys)
                    {
                        handlerTypes.Add(new FilterOperatorTypes(handlersKey, rhsType));
                    }
                }
                if (filter.type != typeof(object))
                {
                    foreach (var handlersKey in op.handlers.Keys)
                    {
                        if (handlersKey != typeof(object))
                            continue;

                        foreach (var rhsType in op.handlers[handlersKey].Keys)
                        {
                            handlerTypes.Add(new FilterOperatorTypes(handlersKey, rhsType));
                        }
                    }
                }
                var sortedHandlerTypes = new List<FilterOperatorTypes>();
                foreach (var handlerType in handlerTypes)
                {
                    if (handlerType.rightHandSideType != typeof(object))
                    {
                        sortedHandlerTypes.Add(handlerType);
                    }
                }
                foreach (var handlerType in handlerTypes)
                {
                    if (handlerType.rightHandSideType == typeof(object))
                    {
                        sortedHandlerTypes.Add(handlerType);
                    }
                }

                foreach (var opHandlerTypes in sortedHandlerTypes)
                {
                    var rhsType = opHandlerTypes.rightHandSideType;
                    parseResult = GenerateParseResultForType(filterValue, rhsType);
                    if (parseResult.success)
                    {
                        filterValueType = rhsType;
                        foundValueType = true;
                        break;
                    }
                }
            }

            // If we still didn't manage to parse the value, try with the type of the filter instead
            if (!foundValueType)
            {
                // Try one last time with the type of the filter instead
                parseResult = GenerateParseResultForType(filterValue, filter.type);
                if (parseResult.success)
                    filterValueType = filter.type;
            }

            return parseResult;
        }

        IParseResult ParseSpecificType(string filterValue, Type type, IFilter filter)
        {
            foreach (var typeParser in filter.typeParsers.Concat(m_TypeParsers))
            {
                if (type != typeParser.type)
                    continue;

                var result = typeParser.Parse(filterValue);
                if (result.success)
                    return result;
            }

            return GenerateParseResultForType(filterValue, type);
        }

        IQueryNode CreateWordExpressionNode(string text, in StringView token, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var localToken = token;
            var isExact = localToken.StartsWith("!");
            if (isExact)
                localToken = localToken.Substring(1, token.length - 1);
            var rawToken = localToken;
            localToken = RemoveQuotes(localToken);

            if (searchWordTransformerCallback != null)
                localToken = new StringView(searchWordTransformerCallback(localToken.ToString()));

            var searchNode = new SearchNode(in localToken, in rawToken, isExact);
            if (localToken.IsNullOrEmpty())
                searchNode.skipped = true;
            return searchNode;
        }

        static IQueryNode CreateCommentNode(string text, in StringView token, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return new CommentNode(in token);
        }

        static IQueryNode CreateToggleNode(string text, in StringView token, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var innerValue = GetStringViewFromMatch(text, match.Groups["inner"]);
            userData.toggles.Add(new QueryToggle(in token, in innerValue));
            return new ToggleNode(in token, in innerValue);
        }

        IQueryNode CombineNodesToTree(List<IQueryNode> expressionNodes, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            var count = expressionNodes.Count;
            if (count == 0)
                return null;

            CombineNotNodes(expressionNodes, errors, nodesToStringPosition);
            CombineAndOrNodes(QueryNodeType.And, expressionNodes, errors, nodesToStringPosition);
            CombineAndOrNodes(QueryNodeType.Or, expressionNodes, errors, nodesToStringPosition);

            var root = expressionNodes[0];
            TransformAndOrToIntersectUnion(ref root, errors, nodesToStringPosition);

            return root;
        }

        static void CombineNotNodes(List<IQueryNode> expressionNodes, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            var count = expressionNodes.Count;
            if (count == 0)
                return;

            for (var i = count - 1; i >= 0; --i)
            {
                var currentNode = expressionNodes[i];
                if (currentNode.type != QueryNodeType.Not)
                    continue;
                var nextNode = i < count - 1 ? expressionNodes[i + 1] : null;
                if (!(currentNode is CombinedNode combinedNode))
                    continue;
                if (!combinedNode.leaf)
                    continue;

                var t = nodesToStringPosition[currentNode];
                if (nextNode == null)
                {
                    errors.Add(new QueryError(t.position + t.length, $"Missing operand to combine with node {currentNode.type}."));
                }
                else
                {
                    combinedNode.AddNode(nextNode);
                    expressionNodes.RemoveAt(i + 1);
                }
            }
        }

        static void CombineAndOrNodes(QueryNodeType nodeType, List<IQueryNode> expressionNodes, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            var count = expressionNodes.Count;
            if (count == 0)
                return;

            for (var i = count - 1; i >= 0; --i)
            {
                var currentNode = expressionNodes[i];
                if (currentNode.type != nodeType)
                    continue;
                var nextNode = i < count - 1 ? expressionNodes[i + 1] : null;
                var previousNode = i > 0 ? expressionNodes[i - 1] : null;
                if (!(currentNode is CombinedNode combinedNode))
                    continue;
                if (!combinedNode.leaf)
                    continue;

                var t = nodesToStringPosition[currentNode];
                if (previousNode == null)
                {
                    errors.Add(new QueryError(t.position, t.length, $"Missing left-hand operand to combine with node {currentNode.type}."));
                }
                else
                {
                    combinedNode.AddNode(previousNode);
                    expressionNodes.RemoveAt(i - 1);
                    // Update current index
                    --i;
                }

                if (nextNode == null)
                {
                    errors.Add(new QueryError(t.position, t.length, $"Missing right-hand operand to combine with node {currentNode.type}."));
                }
                else
                {
                    combinedNode.AddNode(nextNode);
                    expressionNodes.RemoveAt(i + 1);
                }
            }
        }

        void TransformAndOrToIntersectUnion(ref IQueryNode root, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            if ((root.type != QueryNodeType.And && root.type != QueryNodeType.Or) || validationOptions.skipNestedQueries)
                return;

            var children = root.children.ToArray();
            for (var i = 0; i < children.Length; ++i)
            {
                TransformAndOrToIntersectUnion(ref children[i], errors, nodesToStringPosition);
            }

            var hasNestedQueries = false;
            var nestedQueriesCount = 0;
            foreach (var child in root.children)
            {
                switch (child.type)
                {
                    case QueryNodeType.NestedQuery:
                    case QueryNodeType.Intersection:
                    case QueryNodeType.Union:
                        hasNestedQueries = true;
                        ++nestedQueriesCount;
                        break;
                }
            }

            if (!hasNestedQueries)
                return;

            if (nestedQueriesCount != root.children.Count)
            {
                var t = nodesToStringPosition[root];
                errors.Add(new QueryError(t.position, t.length, $"Cannot mix root level nested query operations with regular operations."));
                return;
            }

            IQueryNode convertedNode;
            if (root.type == QueryNodeType.And)
                convertedNode = new IntersectionNode();
            else
                convertedNode = new UnionNode();
            var oldNodeToStringPosition = nodesToStringPosition[root];
            nodesToStringPosition.Add(convertedNode, oldNodeToStringPosition);
            convertedNode.token = root.token;

            convertedNode.children.AddRange(root.children);
            foreach (var child in convertedNode.children)
            {
                child.parent = convertedNode;
            }
            root.children.Clear();

            if (root.parent == null)
            {
                convertedNode.parent = null;
                root = convertedNode;
            }
            else
            {
                root.parent.children.Remove(root);
                root.parent.children.Add(convertedNode);
                convertedNode.parent = root.parent;
                root.parent = null;
            }
        }

        static void RemoveSkippedNodes(ref IQueryNode node, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            if (node == null)
                return;

            if (!(node is CombinedNode combinedNode))
            {
                // When not processing NoOp, nothing to do.
                if (!node.skipped)
                    return;

                // This is the root. Set it to null
                if (node.parent == null)
                {
                    node = null;
                    return;
                }

                if (node.parent is CombinedNode cn)
                {
                    // Otherwise, remove ourselves from our parent
                    cn.RemoveNode(node);
                }
            }
            else
            {
                var children = combinedNode.children.ToArray();
                for (var i = 0; i < children.Length; ++i)
                {
                    RemoveSkippedNodes(ref children[i], errors, nodesToStringPosition);
                }

                combinedNode.RemoveFromTreeIfPossible(ref node, errors);
            }
        }

        static void RemoveGroupNodes(ref IQueryNode node, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            if (node?.children == null)
                return;

            var children = node.children.ToArray();
            for (var i = 0; i < children.Length; ++i)
                RemoveGroupNodes(ref children[i], errors, nodesToStringPosition);

            if (!(node is GroupNode groupNode))
                return;

            groupNode.RemoveFromTreeIfPossible(ref node, errors);
        }

        static void ValidateGraph(IQueryNode root, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            if (root == null)
            {
                errors.Add(new QueryError(0, "Encountered a null node."));
                return;
            }
            var t = nodesToStringPosition[root];
            var position = t.position;
            var length = t.length;
            if (root is CombinedNode cn)
            {
                if (root.leaf)
                {
                    errors.Add(new QueryError(position, length, $"Node {root.type} is a leaf."));
                    return;
                }

                switch (root.type)
                {
                    case QueryNodeType.Not:
                    case QueryNodeType.Where:
                    case QueryNodeType.Group:
                        if (root.children.Count != 1)
                            errors.Add(new QueryError(position, length, $"Node {root.type} should have a child."));
                        break;
                    case QueryNodeType.And:
                    case QueryNodeType.Or:
                    case QueryNodeType.Union:
                    case QueryNodeType.Intersection:
                        if (root.children.Count != 2)
                            errors.Add(new QueryError(position, length, $"Node {root.type} should have 2 children."));
                        break;
                }

                foreach (var child in root.children)
                {
                    ValidateGraph(child, errors, nodesToStringPosition);
                }
            }
        }

        public void BuildFilterRegex()
        {
            RebuildAllBasicFilterRegex(m_FilterOperators.Keys, false);
            RebuildAllBasicPartialFilterRegex(m_FilterOperators.Keys, false);
            foreach (var filter in m_Filters.Values)
            {
                BuildFilterMatchers(filter, false);
            }
            SortQueryTokenHandlers();
        }

        IParseResult GenerateParseResultForType(string value, Type type)
        {
            // Check if we have a default type parser before doing the expensive reflection call
            if (m_DefaultTypeParsers.TryGetValue(type, out var parser))
            {
                return parser.Parse(value);
            }

            var thisClassType = typeof(QueryEngineImpl<TData>);
            var method = thisClassType.GetMethod("ParseData", BindingFlags.NonPublic | BindingFlags.Instance);
            var typedMethod = method.MakeGenericMethod(type);
            return typedMethod.Invoke(this, new object[] { value }) as IParseResult;
        }

        ParseResult<T> ParseData<T>(string value)
        {
            if (Utils.TryConvertValue(value, out T parsedValue))
            {
                // Last resort to get a operation generator if we don't have one yet
                AddFilterOperationGenerator<T>();

                return new ParseResult<T>(true, parsedValue);
            }

            return ParseResult<T>.none;
        }

        void BuildDefaultTypeParsers()
        {
            AddDefaultTypeParser(s =>
            {
                if (int.TryParse(s, out var value))
                    return new ParseResult<int>(true, value);
                if (double.TryParse(s, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var number))
                    return new ParseResult<int>(true, (int)number);

                return ParseResult<int>.none;
            });
            AddDefaultTypeParser(s => float.TryParse(s, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var value) ? new ParseResult<float>(true, value) : ParseResult<float>.none);
            AddDefaultTypeParser(s => bool.TryParse(s, out var value) ? new ParseResult<bool>(true, value) : ParseResult<bool>.none);
            AddDefaultTypeParser(s => double.TryParse(s, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var value) ? new ParseResult<double>(true, value) : ParseResult<double>.none);
        }

        void AddDefaultTypeParser<T>(Func<string, ParseResult<T>> parser)
        {
            if (m_DefaultTypeParsers.ContainsKey(typeof(T)))
            {
                Debug.LogWarning($"A default parser for type {typeof(T)} already exists.");
                return;
            }
            m_DefaultTypeParsers.Add(typeof(T), new TypeParser<T>(parser));
            AddFilterOperationGenerator<T>();
        }

        public void AddDefaultEnumTypeParser<T>()
        {
            if (m_DefaultTypeParsers.ContainsKey(typeof(T)))
                return;
            AddDefaultTypeParser(s =>
            {
                try
                {
                    var value = Enum.Parse(typeof(T), s, true);
                    return new ParseResult<T>(true, (T)value);
                }
                catch (Exception)
                {
                    return ParseResult<T>.none;
                }
            });
        }

        public void AddFilterOperationGenerator<T>()
        {
            if (m_FilterOperationGenerators.ContainsKey(typeof(T)))
                return;
            m_FilterOperationGenerators.Add(typeof(T), new FilterOperationGenerator<T>());
        }

        public void AddFiltersFromAttribute<TFilterAttribute, TTransformerAttribute>()
            where TFilterAttribute : QueryEngineFilterAttribute
            where TTransformerAttribute : QueryEngineParameterTransformerAttribute
        {
            var filters = TypeCache.GetMethodsWithAttribute<TFilterAttribute>()
                .Select(CreateFilterFromFilterAttribute<TFilterAttribute, TTransformerAttribute>)
                .Where(filter => filter != null);
            foreach (var filter in filters)
            {
                AddFilter(filter);
            }
        }

        IFilter CreateFilterFromFilterAttribute<TFilterAttribute, TTransformerAttribute>(MethodInfo mi)
            where TFilterAttribute : QueryEngineFilterAttribute
            where TTransformerAttribute : QueryEngineParameterTransformerAttribute
        {
            var attr = mi.GetCustomAttributes(typeof(TFilterAttribute), false).Cast<TFilterAttribute>().First();
            var creationParams = new FilterCreationParams(
                attr.token,
                new Regex(attr.token),
                attr.useRegularExpressionToken,
                attr.supportedOperators,
                attr.overridesStringComparison,
                attr.overridesStringComparison ? attr.comparisonOptions : globalStringComparison,
                attr.useParamTransformer,
                attr.paramTransformerFunction,
                typeof(TTransformerAttribute)
            );

            if (attr.useRegularExpressionToken)
                return CreateFilterFromFilterAttribute(mi, attr, creationParams, creationParams.regexToken.ToString(), typeof(TData), typeof(string));
            return CreateFilterFromFilterAttribute(mi, attr, creationParams, creationParams.token, typeof(TData));
        }

        IFilter CreateFilterFromFilterAttribute<TFilterAttribute>(MethodInfo mi, TFilterAttribute attr, FilterCreationParams creationParams, string filterId, params Type[] mandatoryTypes)
            where TFilterAttribute : QueryEngineFilterAttribute
        {
            try
            {
                var mandatoryParamCount = mandatoryTypes.Length;
                var inputParams = mi.GetParameters();
                if (inputParams.Length < mandatoryParamCount)
                {
                    Debug.LogWarning($"Filter method {mi.Name} should have at least {mandatoryParamCount} input parameter{(mandatoryParamCount > 1 ? "s" : "")}.");
                    return null;
                }

                for (var i = 0; i < mandatoryParamCount; ++i)
                {
                    var param = inputParams[i];
                    var expectedParamType = mandatoryTypes[i];
                    if (param.ParameterType != expectedParamType)
                    {
                        Debug.LogWarning($"Parameter \"{param.Name}\"'s type of filter method \"{mi.Name}\" must be \"{expectedParamType}\".");
                        return null;
                    }
                }

                var returnType = mi.ReturnType;
                if (returnType == typeof(void))
                {
                    Debug.LogWarning($"Return parameter type of filter method \"{mi.Name}\" cannot be void.");
                    return null;
                }

                // Basic filter
                if (inputParams.Length == mandatoryParamCount)
                {
                    return CreateFilterForMethodInfo(creationParams, mi, false, false, false, creationParams.useRegexToken, returnType);
                }

                // Filter function
                if (inputParams.Length == mandatoryParamCount + 1)
                {
                    var filterParamType = inputParams[mandatoryParamCount].ParameterType;
                    return CreateFilterForMethodInfo(creationParams, mi, true, false, false, creationParams.useRegexToken, filterParamType, returnType);
                }

                // Filter resolver
                if (inputParams.Length == mandatoryParamCount + 2)
                {
                    var operatorParam = inputParams[mandatoryParamCount];
                    var filterValueType = inputParams[mandatoryParamCount + 1].ParameterType;
                    var isOperatorStruct = operatorParam.ParameterType == typeof(QueryFilterOperator);
                    if (operatorParam.ParameterType != typeof(string) && !isOperatorStruct)
                    {
                        Debug.LogWarning($"Parameter \"{operatorParam.Name}\"'s type of filter method \"{mi.Name}\" must be \"{typeof(string)}\" or \"{typeof(QueryFilterOperator)}\".");
                        return null;
                    }

                    if (returnType != typeof(bool))
                    {
                        Debug.LogWarning($"Return type of filter method \"{mi.Name}\" must be \"{typeof(bool)}\".");
                        return null;
                    }

                    return CreateFilterForMethodInfo(creationParams, mi, false, true, isOperatorStruct, creationParams.useRegexToken, filterValueType);
                }

                // Filter function resolver
                if (inputParams.Length == mandatoryParamCount + 3)
                {
                    var filterParamType = inputParams[mandatoryParamCount].ParameterType;
                    var operatorParam = inputParams[mandatoryParamCount + 1];
                    var filterValueType = inputParams[mandatoryParamCount + 2].ParameterType;
                    var isOperatorStruct = operatorParam.ParameterType == typeof(QueryFilterOperator);
                    if (operatorParam.ParameterType != typeof(string) && !isOperatorStruct)
                    {
                        Debug.LogWarning($"Parameter \"{operatorParam.Name}\"'s type of filter method \"{mi.Name}\" must be \"{typeof(string)}\" or \"{typeof(QueryFilterOperator)}\".");
                        return null;
                    }

                    if (returnType != typeof(bool))
                    {
                        Debug.LogWarning($"Return type of filter method \"{mi.Name}\" must be \"{typeof(bool)}\".");
                        return null;
                    }

                    return CreateFilterForMethodInfo(creationParams, mi, true, true, isOperatorStruct, creationParams.useRegexToken, filterParamType, filterValueType);
                }

                Debug.LogWarning($"Error while creating filter \"{filterId}\". Parameter count mismatch.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while creating filter \"{filterId}\". {ex.Message}");
                return null;
            }
        }

        IFilter CreateFilterForMethodInfo(FilterCreationParams creationParams, MethodInfo mi, bool filterFunction, bool resolver, bool resolverStruct, bool useRegex, params Type[] methodTypes)
        {
            var methodName = $"Create{(useRegex ? "Regex" : "")}Filter{(filterFunction ? "Function" : "")}{(resolver ? $"{(resolverStruct ? "Struct" : "String")}Resolver" : "")}ForMethodInfoTyped";
            var thisClassType = typeof(QueryEngineImpl<TData>);
            var method = thisClassType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            var typedMethod = method.MakeGenericMethod(methodTypes);
            return typedMethod.Invoke(this, new object[] { creationParams, mi }) as IFilter;
        }

        IFilter CreateFilterForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TFilter>), mi) as Func<TData, TFilter>;
            if (creationParams.overridesGlobalComparisonOptions)
                return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, creationParams.comparisonOptions, this);
            return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateFilterStringResolverForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, TFilter, bool>), mi) as Func<TData, string, TFilter, bool>;
            return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateFilterStructResolverForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, QueryFilterOperator, TFilter, bool>), mi) as Func<TData, QueryFilterOperator, TFilter, bool>;
            return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateFilterFunctionForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TParam, TFilter>), mi) as Func<TData, TParam, TFilter>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                if (creationParams.overridesGlobalComparisonOptions)
                    return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, creationParams.comparisonOptions, this);
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, this);
            }

            if (creationParams.overridesGlobalComparisonOptions)
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, creationParams.comparisonOptions, this);
            return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateFilterFunctionStringResolverForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TParam, string, TFilter, bool>), mi) as Func<TData, TParam, string, TFilter, bool>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, this);
            }
            return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateFilterFunctionStructResolverForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TParam, QueryFilterOperator, TFilter, bool>), mi) as Func<TData, TParam, QueryFilterOperator, TFilter, bool>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, this);
            }
            return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateRegexFilterForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, TFilter>), mi) as Func<TData, string, TFilter>;
            if (creationParams.overridesGlobalComparisonOptions)
                return new RegexFilter<TData, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, creationParams.comparisonOptions, this);
            return new RegexFilter<TData, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateRegexFilterStringResolverForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, string, TFilter, bool>), mi) as Func<TData, string, string, TFilter, bool>;
            return new RegexFilter<TData, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateRegexFilterStructResolverForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, QueryFilterOperator, TFilter, bool>), mi) as Func<TData, string, QueryFilterOperator, TFilter, bool>;
            return new RegexFilter<TData, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateRegexFilterFunctionForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, TParam, TFilter>), mi) as Func<TData, string, TParam, TFilter>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                if (creationParams.overridesGlobalComparisonOptions)
                    return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, creationParams.comparisonOptions, this);
                return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, this);
            }

            if (creationParams.overridesGlobalComparisonOptions)
                return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, creationParams.comparisonOptions, this);
            return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateRegexFilterFunctionStringResolverForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, TParam, string, TFilter, bool>), mi) as Func<TData, string, TParam, string, TFilter, bool>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, this);
            }
            return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, this);
        }

        IFilter CreateRegexFilterFunctionStructResolverForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, TParam, QueryFilterOperator, TFilter, bool>), mi) as Func<TData, string, TParam, QueryFilterOperator, TFilter, bool>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, this);
            }
            return new RegexFilter<TData, TParam, TFilter>(creationParams.regexToken, creationParams.supportedOperators, methodFunc, this);
        }

        static Func<string, TParam> GetParameterTransformerFunction<TParam>(MethodInfo mi, string functionName, Type transformerAttributeType)
        {
            var transformerMethod = TypeCache.GetMethodsWithAttribute(transformerAttributeType)
                .Where(transformerMethodInfo =>
                {
                    var sameType = transformerMethodInfo.ReturnType == typeof(TParam);
                    var sameName = transformerMethodInfo.Name == functionName;
                    var paramInfos = transformerMethodInfo.GetParameters();
                    var sameParameter = paramInfos.Length == 1 && paramInfos[0].ParameterType == typeof(string);
                    return sameType && sameName && sameParameter;
                }).FirstOrDefault();
            if (transformerMethod == null)
            {
                Debug.LogWarning($"Filter function {mi.Name} uses a parameter transformer, but the function {functionName} was not found.");
                return null;
            }

            return Delegate.CreateDelegate(typeof(Func<string, TParam>), transformerMethod) as Func<string, TParam>;
        }

        static bool IsEnumerableNode(IQueryNode node)
        {
            if (node == null)
                return false;
            switch (node.type)
            {
                case QueryNodeType.Intersection:
                case QueryNodeType.Union:
                case QueryNodeType.NestedQuery:
                case QueryNodeType.Where:
                case QueryNodeType.Aggregator:
                    return true;
                default: return false;
            }
        }

        static IQueryNode InsertWhereNode(IQueryNode rootNode, ICollection<QueryError> errors, NodesToStringPosition nodesToStringPosition)
        {
            if (rootNode == null || IsEnumerableNode(rootNode))
                return rootNode;

            var whereNode = new WhereNode();
            nodesToStringPosition.Add(whereNode, new QueryToken(0, 0));
            whereNode.children.Add(rootNode);
            rootNode.parent = whereNode;
            return whereNode;
        }

        public void BuildFilterMatchers(IFilter filter, bool sort = true)
        {
            var filterId = filter.GetHashCode();

            if (m_CustomFilterTokenHandlers.Contains(filterId))
            {
                RemoveQueryTokenHandlers(filterId);
                m_CustomFilterTokenHandlers.Remove(filterId);
            }

            if (!FilterHasCustomTokenHandlers(filter))
                return;

            var operators = m_FilterOperators.Keys.Concat(filter.operators.Keys);

            AddCustomFilterTokenHandler(filter, filterId, operators, false);
            AddCustomPartialFilterTokenHandler(filter, filterId, operators, false);
            if (filter.type == typeof(bool))
            {
                AddCustomBooleanFilterTokenHandler(filter, filterId, false);
            }

            foreach (var quoteTokens in m_QuoteDelimiters)
            {
                AddFilterQuoteTokenHandlers(filter, filterId, operators, quoteTokens, false);
            }

            m_CustomFilterTokenHandlers.Add(filterId);

            if (sort)
                SortQueryTokenHandlers();
        }

        static bool FilterHasCustomTokenHandlers(IFilter filter)
        {
            return filter.usesRegularExpressionToken || filter.operators.Any();
        }

        void AddFilterQuoteTokenHandlers(IFilter filter, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            if (!FilterHasCustomTokenHandlers(filter))
                return;

            var filterId = filter.GetHashCode();
            var operators = m_FilterOperators.Keys.Concat(filter.operators.Keys);
            AddFilterQuoteTokenHandlers(filter, filterId, operators, in textDelimiter, sort);
        }

        void AddFilterQuoteTokenHandlers(IFilter filter, int filterId, IEnumerable<string> operators, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            if (!FilterHasCustomTokenHandlers(filter))
                return;

            AddCustomFilterWithQuotesTokenHandler(filter, filterId, operators, in textDelimiter, false);
            AddCustomPartialFilterWithQuotesTokenHandler(filter, filterId, operators, in textDelimiter, false);

            if (sort)
                SortQueryTokenHandlers();
        }

        void AddCustomFilterTokenHandler(IFilter filter, int filterId, IEnumerable<string> operators, bool sort = true)
        {
            Regex filterRx;
            if (filter.usesRegularExpressionToken)
                filterRx = BuildFilterRegex(BuildFilterNamePatternFromToken(filter.regexToken), operators);
            else
                filterRx = BuildFilterRegex(BuildFilterNamePatternFromToken(filter.token), operators);
            AddCustomTokenHandler(filter, filterId, (int)TokenHandlerPriority.Filter - 1, filterRx, MatchFilter, CreateFilterToken, sort);
        }

        void AddCustomFilterWithQuotesTokenHandler(IFilter filter, int filterId, IEnumerable<string> operators, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            Regex filterRx;
            if (filter.usesRegularExpressionToken)
                filterRx = BuildFilterRegex(BuildFilterNamePatternFromToken(filter.regexToken), BuildFilterValuePatternFromQuoteDelimiter(in textDelimiter), operators);
            else
                filterRx = BuildFilterRegex(BuildFilterNamePatternFromToken(filter.token), BuildFilterValuePatternFromQuoteDelimiter(in textDelimiter), operators);
            AddCustomTokenHandler(filter, filterId, (int)TokenHandlerPriority.FilterWithQuotes - 1, filterRx, MatchFilter, CreateFilterToken, in textDelimiter, sort);
        }

        void AddCustomBooleanFilterTokenHandler(IFilter filter, int filterId, bool sort = true)
        {
            Regex booleanRx;
            if (filter.usesRegularExpressionToken)
                booleanRx = BuildBooleanFilterRegex(BuildFilterNamePatternFromToken(filter.regexToken));
            else
                booleanRx = BuildBooleanFilterRegex(BuildFilterNamePatternFromToken(filter.token));
            AddCustomTokenHandler(filter, filterId, (int)ExtendedTokenHandlerPriority.Boolean - 1, booleanRx, MatchKnownBooleanFilter, CreateBooleanFilterToken, sort);
        }

        void AddCustomPartialFilterTokenHandler(IFilter filter, int filterId, IEnumerable<string> operators, bool sort = true)
        {
            Regex partialRx;
            if (filter.usesRegularExpressionToken)
                partialRx = BuildPartialFilterRegex(BuildPartialFilterNamePatternFromToken(filter.regexToken, operators), operators);
            else
                partialRx = BuildPartialFilterRegex(BuildPartialFilterNamePatternFromToken(filter.token, operators), operators);
            AddCustomTokenHandler(filter, filterId, (int)ExtendedTokenHandlerPriority.Partial - 1, partialRx, MatchFilter, CreatePartialFilterToken, sort);
        }

        void AddCustomPartialFilterWithQuotesTokenHandler(IFilter filter, int filterId, IEnumerable<string> operators, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            Regex partialRx;
            if (filter.usesRegularExpressionToken)
                partialRx = BuildPartialFilterRegex(BuildPartialFilterNamePatternFromToken(filter.regexToken, operators), BuildPartialFilterValuePatternFromQuoteDelimiter(in textDelimiter), operators);
            else
                partialRx = BuildPartialFilterRegex(BuildPartialFilterNamePatternFromToken(filter.token, operators), BuildPartialFilterValuePatternFromQuoteDelimiter(in textDelimiter), operators);
            AddCustomTokenHandler(filter, filterId, (int)ExtendedTokenHandlerPriority.PartialWithQuotes - 1, partialRx, MatchFilter, CreatePartialFilterToken, in textDelimiter, sort);
        }

        void AddCustomTokenHandler(IFilter filter, int filterId, int priority, Regex matchRx, MatchFilterFunction matchFilter, CreateFilterFunction createFilter, bool sort = true)
        {
            AddCustomTokenHandler(filter, filterId, priority, matchRx, matchFilter, createFilter, null, sort);
        }

        void AddCustomTokenHandler(IFilter filter, int filterId, int priority, Regex matchRx, MatchFilterFunction matchFilter, CreateFilterFunction createFilter, in QueryTextDelimiter textDelimiter, bool sort = true)
        {
            var hashCode = textDelimiter.GetHashCode();
            AddCustomTokenHandler(filter, filterId, priority, matchRx, matchFilter, createFilter, handler =>
            {
                handler.AddOrUpdateUserData(QueryTextDelimiter.k_QueryHandlerDataKey, hashCode);
                return handler;
            }, sort);
        }

        void AddCustomTokenHandler(IFilter filter, int filterId, int priority, Regex matchRx, MatchFilterFunction matchFilter, CreateFilterFunction createFilter, Func<QueryTokenHandler, QueryTokenHandler> customize, bool sort = true)
        {
            var queryTokenHandler = new QueryTokenHandler(filterId, priority)
            {
                matcher = (string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched) => matchFilter(matchRx, text, startIndex, endIndex, errors, out sv, out match, out matched),
                consumer = (string text, int startIndex, int endIndex, in StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData data) =>
                {
                    return ConsumeFilter(text, startIndex, endIndex, in sv, match, errors, data, (string t, in StringView sv, Match m, int index, ICollection<QueryError> e) => createFilter(filter, t, in sv, m, index, e));
                }
            };
            queryTokenHandler = customize?.Invoke(queryTokenHandler) ?? queryTokenHandler;
            AddQueryTokenHandler(queryTokenHandler, sort);
        }

        public override void AddQuoteDelimiter(in QueryTextDelimiter textDelimiter, IEnumerable<string> operators, bool sort = true)
        {
            base.AddQuoteDelimiter(in textDelimiter, operators, false);

            // Add a new token handler for partial filters
            AddCustomPartialFilterWithQuotesTokenHandler(0, (int)ExtendedTokenHandlerPriority.PartialWithQuotes, textDelimiter, operators, false);

            // Rebuild filter handlers for custom filter that have regex or operators
            foreach (var filter in m_Filters.Values)
            {
                AddFilterQuoteTokenHandlers(filter, textDelimiter, false);
            }

            if (sort)
                SortQueryTokenHandlers();
        }

        void AddCustomPartialFilterWithQuotesTokenHandler(int handlerId, int priority, in QueryTextDelimiter textDelimiter, IEnumerable<string> operators, bool sort = true)
        {
            AddCustomPartialFilterWithQuotesTokenHandler(handlerId, priority, BuildPartialFilterNamePatternFromToken(QueryRegexValues.k_BaseFilterNameRegex, operators), BuildPartialFilterValuePatternFromQuoteDelimiter(textDelimiter), textDelimiter, operators, sort);
        }

        void AddCustomPartialFilterWithQuotesTokenHandler(int handlerId, int priority, string filterNamePattern, string filterValuePattern, in QueryTextDelimiter textDelimiter, IEnumerable<string> operators, bool sort = true)
        {
            var partialFilterRegex = BuildPartialFilterRegex(filterNamePattern, filterValuePattern, operators);
            var queryTokenHandler = new QueryTokenHandler(handlerId, priority)
            {
                matcher = (string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched) => MatchFilter(partialFilterRegex, text, startIndex, endIndex, errors, out sv, out match, out matched),
                consumer = ConsumePartialFilter
            };
            AddCustomQuoteTokenHandler(queryTokenHandler, textDelimiter, sort);
        }

        void RebuildAllBasicPartialFilterRegex(IEnumerable<string> operators, bool sort = true)
        {
            m_PartialFilterRx = BuildPartialFilterRegex(BuildPartialFilterNamePatternFromToken(QueryRegexValues.k_BaseFilterNameRegex, operators), operators);

            // Rebuild all token handlers for partial filters with quotes
            RemoveQueryTokenHandlers(handler => handler.priority == (int)ExtendedTokenHandlerPriority.PartialWithQuotes);
            foreach (var queryQuoteTokens in m_QuoteDelimiters)
            {
                AddCustomPartialFilterWithQuotesTokenHandler(0, (int)ExtendedTokenHandlerPriority.PartialWithQuotes, queryQuoteTokens, operators, false);
            }

            if (sort)
                SortQueryTokenHandlers();
        }

        static StringView GetStringViewFromMatch(string text, Group matchGroup)
        {
            if (matchGroup.Length == 0)
                return StringView.empty;
            return new StringView(text, matchGroup.Index, matchGroup.Index + matchGroup.Length);
        }
    }

    /// <summary>
    /// A QueryEngine defines how to build a query from an input string.
    /// It can be customized to support custom filters and operators.
    /// </summary>
    /// <typeparam name="TData">The filtered data type.</typeparam>
    public partial class QueryEngine<TData>
    {
        QueryEngineImpl<TData> m_Impl;

        /// <summary>
        /// Get of set if the engine must validate filters when parsing the query. Defaults to true.
        /// </summary>
        public bool validateFilters
        {
            get => m_Impl.validationOptions.validateFilters;
            set
            {
                var options = m_Impl.validationOptions;
                options.validateFilters = value;
                m_Impl.validationOptions = options;
            }
        }
        /// <summary>
        /// Boolean indicating if unknown filters should be skipped.
        /// If validateFilters is true and skipUnknownFilters is false, unknown filters will generate errors
        /// if no default handler is provided.
        /// </summary>
        public bool skipUnknownFilters
        {
            get => m_Impl.validationOptions.skipUnknownFilters;
            set
            {
                var options = m_Impl.validationOptions;
                options.skipUnknownFilters = value;
                m_Impl.validationOptions = options;
            }
        }

        /// <summary>
        /// Boolean indicating if incomplete filters should be skipped.
        /// If skipIncompleteFilters is false, incomplete filters will generate errors when parsed.
        /// </summary>
        public bool skipIncompleteFilters
        {
            get => m_Impl.validationOptions.skipIncompleteFilters;
            set
            {
                var options = m_Impl.validationOptions;
                options.skipIncompleteFilters = value;
                m_Impl.validationOptions = options;
            }
        }

        internal bool skipNestedQueries
        {
            get => m_Impl.validationOptions.skipNestedQueries;
            set
            {
                var options = m_Impl.validationOptions;
                options.skipNestedQueries = value;
                m_Impl.validationOptions = options;
            }
        }

        internal bool validateSyntaxOnly
        {
            get => m_Impl.validationOptions.validateSyntaxOnly;
            set
            {
                var options = m_Impl.validationOptions;
                options.validateSyntaxOnly = value;
                m_Impl.validationOptions = options;
                m_Impl.BuildFilterRegex();
            }
        }

        /// <summary>
        /// Global string comparison options for word matching and filter handling (if not overridden).
        /// </summary>
        public StringComparison globalStringComparison => m_Impl.globalStringComparison;

        /// <summary>
        /// String comparison options for word/phrase matching.
        /// </summary>
        public StringComparison searchDataStringComparison => m_Impl.searchDataStringComparison;

        /// <summary>
        /// Indicates if word/phrase matching uses searchDataStringComparison or not.
        /// </summary>
        public bool searchDataOverridesStringComparison => m_Impl.searchDataOverridesGlobalStringComparison;

        /// <summary>
        /// The callback used to get the data to match to the search words.
        /// </summary>
        public Func<TData, IEnumerable<string>> searchDataCallback => m_Impl.searchDataCallback;

        /// <summary>
        /// The function used to match the search data against the search words.
        /// </summary>
        public Func<string, bool, StringComparison, string, bool> searchWordMatcher => m_Impl.searchWordMatcher;

        /// <summary>
        /// Construct a new QueryEngine.
        /// </summary>
        public QueryEngine()
        {
            m_Impl = new QueryEngineImpl<TData>(new QueryValidationOptions { validateFilters = true });
        }

        /// <summary>
        /// Construct a new QueryEngine.
        /// </summary>
        /// <param name="validateFilters">Indicates if the engine must validate filters when parsing the query.</param>
        public QueryEngine(bool validateFilters)
        {
            m_Impl = new QueryEngineImpl<TData>(new QueryValidationOptions { validateFilters = validateFilters });
        }

        /// <summary>
        /// Construct a new QueryEngine with the specified validation options.
        /// </summary>
        /// <param name="validationOptions">The validation options to use in this engine.</param>
        public QueryEngine(QueryValidationOptions validationOptions)
        {
            m_Impl = new QueryEngineImpl<TData>(validationOptions);
        }

        internal IQueryEngineFilter AddFilter(string token, string[] supportedOperators = null)
        {
            var filter = new Filter<TData, string>(token, supportedOperators, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter AddFilter(Regex token, string[] supportedOperators = null)
        {
            var filter = new RegexFilter<TData, string>(token, supportedOperators, m_Impl);
            m_Impl.AddFilter(filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and returns an object of type TFilter.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TFilter>(string token, Func<TData, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and returns an object of type TFilter.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TFilter>(string token, Func<TData, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TFilter>(string token, Func<TData, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        internal void AddFilter<TFilter>(string token, Func<TData, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, an object of type TParam, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        internal void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, an object of type TParam, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, string, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        internal void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, QueryFilterOperator, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the actual filter name that was matched and returns an object of type TFilter.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TFilter>(Regex token, Func<TData, string, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the actual filter name that was matched and returns an object of type TFilter.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TFilter>(Regex token, Func<TData, string, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the actual filter name that was matched, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the actual filter name that was matched, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the actual filter name that was matched, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the actual filter name that was matched, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, a string representing the actual filter name that was matched, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TFilter>(Regex token, Func<TData, string, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter AddFilter<TFilter>(Regex token, Func<TData, string, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter function with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, a string representing the actual filter name that was matched, an object of type TParam, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add a new custom filter function with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, a string representing the actual filter name that was matched, an object of type TParam, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. This list contains the supported operator tokens. Use null or an empty list to indicate that all operators are supported.</param>
        /// <returns>A <see cref="IQueryEngineFilter"/>.</returns>
        public IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, string, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter AddFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, QueryFilterOperator, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter(string token, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, string>(token, supportedOperatorType, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter(Regex token, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, string>(token, supportedOperatorType, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(string token, Func<TData, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(string token, Func<TData, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(string token, Func<TData, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(string token, Func<TData, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, string, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(string token, Func<TData, TParam, QueryFilterOperator, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(Regex token, Func<TData, string, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(Regex token, Func<TData, string, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, stringComparison, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(Regex token, Func<TData, string, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TFilter>(Regex token, Func<TData, string, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, QueryFilterOperator, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, string, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        internal IQueryEngineFilter SetFilter<TParam, TFilter>(Regex token, Func<TData, string, TParam, QueryFilterOperator, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new RegexFilter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer, m_Impl);
            m_Impl.AddFilter(token, filter);
            return filter;
        }

        /// <summary>
        /// Add all custom filters that are identified with the method attribute TAttribute.
        /// </summary>
        /// <typeparam name="TFilterAttribute">The type of the attribute of filters to fetch.</typeparam>
        /// <typeparam name="TTransformerAttribute">The attribute type for the parameter transformers associated with the filter attribute.</typeparam>
        public void AddFiltersFromAttribute<TFilterAttribute, TTransformerAttribute>()
            where TFilterAttribute : QueryEngineFilterAttribute
            where TTransformerAttribute : QueryEngineParameterTransformerAttribute
        {
            m_Impl.AddFiltersFromAttribute<TFilterAttribute, TTransformerAttribute>();
        }

        /// <summary>
        /// Remove a custom filter.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <remarks>You will get a warning if you try to remove a filter that does not exist.</remarks>
        public void RemoveFilter(string token)
        {
            m_Impl.RemoveFilter(token);
        }

        /// <summary>
        /// Remove a custom filter.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <remarks>You will get a warning if you try to remove a filter that does not exist.</remarks>
        public void RemoveFilter(Regex token)
        {
            m_Impl.RemoveFilter(token);
        }

        /// <summary>
        /// Remove a custom filter.
        /// </summary>
        /// <param name="filter">A <see cref="IQueryEngineFilter"/> that was returned by <see cref="AddFilter"/>.</param>
        public void RemoveFilter(IQueryEngineFilter filter)
        {
            m_Impl.RemoveFilter((IFilter)filter);
        }

        /// <summary>
        /// Removes all filters that were added on the <see cref="QueryEngine"/>.
        /// </summary>
        public void ClearFilters()
        {
            m_Impl.ClearFilters();
        }

        /// <summary>
        /// Get a filter by its token.
        /// </summary>
        /// <param name="token">The token used to create the filter.</param>
        /// <returns>Returns the <see cref="IQueryEngineFilter"/>, or null if it does not exist.</returns>
        internal IQueryEngineFilter GetFilter(string token)
        {
            return m_Impl.GetFilter(token);
        }

        /// <summary>
        /// Get a filter by its token.
        /// </summary>
        /// <param name="token">The token used to create the filter.</param>
        /// <returns>Returns the <see cref="IQueryEngineFilter"/>, or null if it does not exist.</returns>
        internal IQueryEngineFilter GetFilter(Regex token)
        {
            return m_Impl.GetFilter(token);
        }

        /// <summary>
        /// Get all filters added on this <see cref="QueryEngine"/>.
        /// </summary>
        /// <returns>An enumerable of <see cref="IQueryEngineFilter"/>.</returns>
        public IEnumerable<IQueryEngineFilter> GetAllFilters()
        {
            return m_Impl.GetAllFilters();
        }

        /// <summary>
        /// Indicates if a filter exists on the <see cref="QueryEngine"/>.
        /// </summary>
        /// <param name="token">The token used to create the filter.</param>
        /// <returns>Returns true if the filter exists, otherwise returns false.</returns>
        internal bool HasFilter(string token)
        {
            return m_Impl.HasFilter(token);
        }

        /// <summary>
        /// Indicates if a filter exists.
        /// </summary>
        /// <param name="token">The token used to create the filter.</param>
        /// <returns>Returns true if the filter exists, otherwise returns false.</returns>
        internal bool HasFilter(Regex token)
        {
            return m_Impl.HasFilter(token);
        }

        /// <summary>
        /// Try to get a filter by its token.
        /// </summary>
        /// <param name="token">The token used to create the filter.</param>
        /// <param name="filter">The existing <see cref="IQueryEngineFilter"/>, or null if it does not exist.</param>
        /// <returns>Returns true if the filter was retrieved or false if the filter does not exist.</returns>
        public bool TryGetFilter(string token, out IQueryEngineFilter filter)
        {
            var success =  m_Impl.TryGetFilter(token, out var engineFilter);
            filter = engineFilter;
            return success;
        }

        /// <summary>
        /// Try to get a filter by its token.
        /// </summary>
        /// <param name="token">The token used to create the filter.</param>
        /// <param name="filter">The existing <see cref="IQueryEngineFilter"/>, or null if it does not exist.</param>
        /// <returns>Returns true if the filter was retrieved or false if the filter does not exist.</returns>
        public bool TryGetFilter(Regex token, out IQueryEngineFilter filter)
        {
            var success =  m_Impl.TryGetFilter(token, out var engineFilter);
            filter = engineFilter;
            return success;
        }

        /// <summary>
        /// Add a global custom filter operator.
        /// </summary>
        /// <param name="op">The operator identifier.</param>
        public void AddOperator(string op)
        {
            m_Impl.AddOperator(op);
        }

        /// <summary>
        /// Removes a custom operator that was added on the <see cref="QueryEngine"/>.
        /// </summary>
        /// <param name="op">The operator identifier.</param>
        public void RemoveOperator(string op)
        {
            m_Impl.RemoveOperator(op);
        }

        /// <summary>
        /// Get a custom operator added on the <see cref="QueryEngine"/>.
        /// </summary>
        /// <param name="op">The operator identifier.</param>
        /// <returns>The global <see cref="QueryFilterOperator"/> or an invalid <see cref="QueryFilterOperator"/> if it does not exist.</returns>
        public QueryFilterOperator GetOperator(string op)
        {
            return m_Impl.GetOperator(op);
        }

        /// <summary>
        /// Add a custom filter operator handler.
        /// </summary>
        /// <typeparam name="TFilterVariable">The operator's left hand side type. This is the type returned by a filter handler.</typeparam>
        /// <typeparam name="TFilterConstant">The operator's right hand side type.</typeparam>
        /// <param name="op">The filter operator.</param>
        /// <param name="handler">Callback to handle the operation. Takes a TFilterVariable (value returned by the filter handler, will vary for each element) and a TFilterConstant (right hand side value of the operator, which is constant), and returns a boolean indicating if the filter passes or not.</param>
        public void AddOperatorHandler<TFilterVariable, TFilterConstant>(string op, Func<TFilterVariable, TFilterConstant, bool> handler)
        {
            m_Impl.AddOperatorHandler(op, handler);
        }

        /// <summary>
        /// Add a custom filter operator handler.
        /// </summary>
        /// <typeparam name="TFilterVariable">The operator's left hand side type. This is the type returned by a filter handler.</typeparam>
        /// <typeparam name="TFilterConstant">The operator's right hand side type.</typeparam>
        /// <param name="op">The filter operator.</param>
        /// <param name="handler">Callback to handle the operation. Takes a TFilterVariable (value returned by the filter handler, will vary for each element), a TFilterConstant (right hand side value of the operator, which is constant), a StringComparison option and returns a boolean indicating if the filter passes or not.</param>
        public void AddOperatorHandler<TFilterVariable, TFilterConstant>(string op, Func<TFilterVariable, TFilterConstant, StringComparison, bool> handler)
        {
            m_Impl.AddOperatorHandler(op, handler);
        }

        internal void AddOperatorHandler<TFilterVariable, TFilterConstant>(string op, Func<FilterOperatorContext, TFilterVariable, TFilterConstant, bool> handler)
        {
            m_Impl.AddOperatorHandler(op, handler);
        }

        internal void AddOperatorHandler<TFilterVariable, TFilterConstant>(string op, Func<FilterOperatorContext, TFilterVariable, TFilterConstant, StringComparison, bool> handler)
        {
            m_Impl.AddOperatorHandler(op, handler);
        }

        /// <summary>
        /// Add a global type parser that parses a string and returns a custom type. Used
        /// by custom operator handlers.
        /// </summary>
        /// <typeparam name="TFilterConstant">The type of the parsed operand that is on the right hand side of the operator.</typeparam>
        /// <param name="parser">Callback used to determine if a string can be converted into TFilterConstant. Takes a string and returns a ParseResult object. This contains the success flag, and the actual converted value if it succeeded.</param>
        public void AddTypeParser<TFilterConstant>(Func<string, ParseResult<TFilterConstant>> parser)
        {
            m_Impl.AddTypeParser(parser);
        }

        /// <summary>
        /// Set the default filter handler for filters that were not registered.
        /// </summary>
        /// <param name="handler">Callback used to handle the filter. Takes an object of type TData, the filter identifier, the operator and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        public void SetDefaultFilter(Func<TData, string, string, string, bool> handler)
        {
            m_Impl.SetDefaultFilterHandler(new DefaultQueryFilterResolverHandler<TData, string>(handler));
        }

        /// <summary>
        /// Set the default filter handler for filters that were not registered.
        /// </summary>
        /// <param name="handler">Callback used to handle the filter. Takes an object of type TData, the filter identifier, the operator and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="onCreateFilter">Callback called when a default filter is generated. Can be used to setup type parsers and operator handlers specific for this filter.</param>
        internal void SetDefaultFilter(Func<TData, string, string, string, bool> handler, Action<IFilter> onCreateFilter)
        {
            m_Impl.SetDefaultFilterHandler(new DefaultQueryFilterResolverHandler<TData, string>(handler, onCreateFilter));
        }

        /// <summary>
        /// Set the default filter handler for filters that were not registered.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the current filter id and returns an object of type TFilter.</param>
        internal void SetDefaultFilter<TFilter>(Func<TData, string, TFilter> getDataFunc)
        {
            m_Impl.SetDefaultFilterHandler(new DefaultQueryFilterHandler<TData, TFilter>(getDataFunc));
        }

        /// <summary>
        /// Set the default filter handler for filters that were not registered.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the current filter id and returns an object of type TFilter.</param>
        /// <param name="onCreateFilter">Callback called when a default filter is generated. Can be used to setup type parsers and operator handlers specific for this filter.</param>
        internal void SetDefaultFilter<TFilter>(Func<TData, string, TFilter> getDataFunc, Action<IFilter> onCreateFilter)
        {
            m_Impl.SetDefaultFilterHandler(new DefaultQueryFilterHandler<TData, TFilter>(getDataFunc, onCreateFilter));
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <param name="handler">Callback used to handle the function filter. Takes an object of type TData, the filter identifier, the parameter, the operator and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        public void SetDefaultParamFilter(Func<TData, string, string, string, string, bool> handler)
        {
            m_Impl.SetDefaultParamFilterHandler(new DefaultQueryParamFilterResolverHandler<TData, string, string>(handler));
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <param name="handler">Callback used to handle the function filter. Takes an object of type TData, the filter identifier, the parameter, the operator and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="onCreateFilter">Callback called when a default filter is generated. Can be used to setup type parsers and operator handlers specific for this filter.</param>
        internal void SetDefaultParamFilter(Func<TData, string, string, string, string, bool> handler, Action<IFilter> onCreateFilter)
        {
            m_Impl.SetDefaultParamFilterHandler(new DefaultQueryParamFilterResolverHandler<TData, string, string>(handler, onCreateFilter));
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the filter id, an object of type TParam, and returns an object of type TFilter.</param>
        internal void SetDefaultParamFilter<TParam, TFilter>(Func<TData, string, TParam, TFilter> getDataFunc)
        {
            m_Impl.SetDefaultParamFilterHandler(new DefaultQueryParamFilterHandler<TData, TParam, TFilter>(getDataFunc));
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the filter id, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="onCreateFilter">Callback called when a default filter is generated. Can be used to setup type parsers and operator handlers specific for this filter.</param>
        internal void SetDefaultParamFilter<TParam, TFilter>(Func<TData, string, TParam, TFilter> getDataFunc, Action<IFilter> onCreateFilter)
        {
            m_Impl.SetDefaultParamFilterHandler(new DefaultQueryParamFilterHandler<TData, TParam, TFilter>(getDataFunc, onCreateFilter));
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the filter id, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        internal void SetDefaultParamFilter<TParam, TFilter>(Func<TData, string, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer)
        {
            m_Impl.SetDefaultParamFilterHandler(new DefaultQueryParamFilterHandler<TData, TParam, TFilter>(getDataFunc, parameterTransformer));
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData, a string representing the filter id, an object of type TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="onCreateFilter">Callback called when a default filter is generated. Can be used to setup type parsers and operator handlers specific for this filter.</param>
        internal void SetDefaultParamFilter<TParam, TFilter>(Func<TData, string, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, Action<IFilter> onCreateFilter)
        {
            m_Impl.SetDefaultParamFilterHandler(new DefaultQueryParamFilterHandler<TData, TParam, TFilter>(getDataFunc, parameterTransformer, onCreateFilter));
        }

        /// <summary>
        /// Set the callback to be used to fetch the data that will be matched against the search words.
        /// </summary>
        /// <param name="getSearchDataCallback">Callback used to get the data to be matched against the search words. Takes an object of type TData and return an IEnumerable of strings.</param>
        public void SetSearchDataCallback(Func<TData, IEnumerable<string>> getSearchDataCallback)
        {
            m_Impl.SetSearchDataCallback(getSearchDataCallback);
        }

        /// <summary>
        /// Set the callback to be used to fetch the data that will be matched against the search words.
        /// </summary>
        /// <param name="getSearchDataCallback">Callback used to get the data to be matched against the search words. Takes an object of type TData and return an IEnumerable of strings.</param>
        /// <param name="stringComparison">String comparison options.</param>
        public void SetSearchDataCallback(Func<TData, IEnumerable<string>> getSearchDataCallback, StringComparison stringComparison)
        {
            m_Impl.SetSearchDataCallback(getSearchDataCallback, stringComparison);
        }

        /// <summary>
        /// Set the callback to be used to fetch the data that will be matched against the search words, plus a transformer on those words.
        /// </summary>
        /// <param name="getSearchDataCallback">Callback used to get the data to be matched against the search words. Takes an object of type TData and return an IEnumerable of strings.</param>
        /// <param name="searchWordTransformerCallback">Callback used to transform a search word during the query parsing. Useful when doing lowercase or uppercase comparison. Can return null or an empty string to remove the word from the query.</param>
        /// <param name="stringComparison">String comparison options.</param>
        public void SetSearchDataCallback(Func<TData, IEnumerable<string>> getSearchDataCallback, Func<string, string> searchWordTransformerCallback, StringComparison stringComparison)
        {
            m_Impl.SetSearchDataCallback(getSearchDataCallback, searchWordTransformerCallback, stringComparison);
        }

        /// <summary>
        /// Set the search word matching function to be used instead of the default one. Set to null to use the default.
        /// </summary>
        /// <param name="wordMatcher">The search word matching function. The first parameter is the search word. The second parameter is a boolean for exact match or not. The third parameter is the StringComparison options. The fourth parameter is an element of the array returned by the search data callback. The function returns true for a match or false for no match.</param>
        public void SetSearchWordMatcher(Func<string, bool, StringComparison, string, bool> wordMatcher)
        {
            m_Impl.SetSearchWordMatcher(wordMatcher);
        }

        /// <summary>
        /// Set global string comparison options. Used for word matching and filter handling (unless overridden by filter).
        /// </summary>
        /// <param name="stringComparison">String comparison options.</param>
        public void SetGlobalStringComparisonOptions(StringComparison stringComparison)
        {
            m_Impl.globalStringComparison = stringComparison;
        }

        /// <summary>
        /// Parse a query string into a ParsedQuery operation. This ParsedQuery operation can then be used to filter any data set of type TData.
        /// </summary>
        /// <param name="text">The query input string.</param>
        /// <param name="useFastYieldingQueryHandler">Set to true to get a query that will yield null results for elements that don't pass the query, instead of only the elements that pass the query.</param>
        /// <returns>ParsedQuery operation of type TData.</returns>
        public ParsedQuery<TData> ParseQuery(string text, bool useFastYieldingQueryHandler = false)
        {
            var handlerFactory = new DefaultQueryHandlerFactory<TData>(this, useFastYieldingQueryHandler);
            return BuildQuery(text, handlerFactory, (evalGraph, queryGraph, errors, tokens, toggles, handler) => new ParsedQuery<TData>(text, evalGraph, queryGraph, errors, tokens, toggles, handler));
        }

        /// <summary>
        /// Parse a query string into a ParsedQuery operation. This ParsedQuery operation can then be used to filter any data set of type TData.
        /// </summary>
        /// <typeparam name="TQueryHandler">Type of the underlying query handler. See IQueryHandler.</typeparam>
        /// <typeparam name="TPayload">Type of the payload that the query handler receives.</typeparam>
        /// <param name="text">The query input string.</param>
        /// <param name="queryHandlerFactory">A factory object that creates query handlers of type TQueryHandler. See IQueryHandlerFactory.</param>
        /// <returns>ParsedQuery operation of type TData and TPayload.</returns>
        public ParsedQuery<TData, TPayload> ParseQuery<TQueryHandler, TPayload>(string text, IQueryHandlerFactory<TData, TQueryHandler, TPayload> queryHandlerFactory)
            where TQueryHandler : IQueryHandler<TData, TPayload>
            where TPayload : class
        {
            return BuildQuery(text, queryHandlerFactory, (evalGraph, queryGraph, errors, tokens, toggles, handler) => new ParsedQuery<TData, TPayload>(text, evalGraph, queryGraph, errors, tokens, toggles, handler));
        }

        TQuery BuildQuery<TQuery, TQueryHandler, TPayload>(string text, IQueryHandlerFactory<TData, TQueryHandler, TPayload> queryHandlerFactory, Func<QueryGraph, QueryGraph, ICollection<QueryError>, ICollection<string>, ICollection<QueryToggle>, TQueryHandler, TQuery> queryCreator)
            where TQueryHandler : IQueryHandler<TData, TPayload>
            where TPayload : class
            where TQuery : ParsedQuery<TData, TPayload>
        {
            var errors = new List<QueryError>();
            var tokens = new List<string>();
            var toggles = new List<QueryToggle>();
            var result = m_Impl.BuildGraph(text, errors, tokens, toggles);
            return queryCreator(result.evaluationGraph, result.queryGraph, errors, tokens, toggles, queryHandlerFactory.Create(result.evaluationGraph, errors));
        }

        /// <summary>
        /// Set the function that will handle nested queries. Only one handler can be set.
        /// </summary>
        /// <typeparam name="TNestedQueryData">The type of data returned by the nested query.</typeparam>
        /// <param name="handler">The function that handles nested queries. It receives the nested query and the filter token on which the query is applied, and returns an IEnumerable.</param>
        public void SetNestedQueryHandler<TNestedQueryData>(Func<string, string, IEnumerable<TNestedQueryData>> handler)
        {
            m_Impl.SetNestedQueryHandler(handler);
        }

        /// <summary>
        /// Set a filter's nested query transformer function. This function takes the result of a nested query and extract the necessary data to compare with the filter.
        /// </summary>
        /// <typeparam name="TNestedQueryData">The type of data returned by the nested query.</typeparam>
        /// <typeparam name="TRhs">The type expected on the right hand side of the filter.</typeparam>
        /// <param name="filterToken">The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").</param>
        /// <param name="transformer">The transformer function.</param>
        public void SetFilterNestedQueryTransformer<TNestedQueryData, TRhs>(string filterToken, Func<TNestedQueryData, TRhs> transformer)
        {
            m_Impl.SetFilterNestedQueryTransformer(filterToken, transformer);
        }

        /// <summary>
        /// Add a new nested query aggregator. An aggregator is an operation that can be applied on a nested query to aggregate the results of the nested query according to a certain criteria.
        /// </summary>
        /// <param name="token">Name of the aggregator used when typing the query. This name will be lowercased when parsing the query to speedup the process.</param>
        /// <param name="aggregator">Aggregator function. Takes the results of the nested query, and returns an aggregate that contains any number of items.</param>
        /// <typeparam name="TNestedQueryData">The type of data returned by the nested query.</typeparam>
        public void AddNestedQueryAggregator<TNestedQueryData>(string token, Func<IEnumerable<TNestedQueryData>, IEnumerable<TNestedQueryData>> aggregator)
        {
            m_Impl.AddNestedQueryAggregator(token, aggregator);
        }

        internal IParseResult ParseFilterValue(string filterValue, IFilter filter, in QueryFilterOperator op, out Type filterValueType)
        {
            return m_Impl.ParseFilterValue(filterValue, filter, in op, out filterValueType);
        }

        internal IFilterOperationGenerator GetGeneratorForType(Type type)
        {
            return m_Impl.GetGeneratorForType(type);
        }

        internal void AddQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            m_Impl.AddQuoteDelimiter(textDelimiter);
        }

        internal void RemoveQuoteDelimiter(in QueryTextDelimiter textDelimiter)
        {
            m_Impl.RemoveQuoteDelimiter(textDelimiter);
        }

        internal void AddCommentDelimiter(in QueryTextDelimiter textDelimiter)
        {
            m_Impl.AddCommentDelimiter(textDelimiter);
        }

        internal void RemoveCommentDelimiter(in QueryTextDelimiter textDelimiter)
        {
            m_Impl.RemoveCommentDelimiter(textDelimiter);
        }

        internal void AddToggleDelimiter(in QueryTextDelimiter textDelimiter)
        {
            m_Impl.AddToggleDelimiter(textDelimiter);
        }

        internal void RemoveToggleDelimiter(in QueryTextDelimiter textDelimiter)
        {
            m_Impl.RemoveToggleDelimiter(textDelimiter);
        }
    }

    /// <summary>
    /// A QueryEngine defines how to build a query from an input string.
    /// It can be customized to support custom filters and operators.
    /// Default query engine of type object.
    /// </summary>
    public class QueryEngine : QueryEngine<object>
    {
        /// <summary>
        /// Construct a new QueryEngine.
        /// </summary>
        public QueryEngine()
        {}

        /// <summary>
        /// Construct a new QueryEngine.
        /// </summary>
        /// <param name="validateFilters">Indicates if the engine must validate filters when parsing the query.</param>
        public QueryEngine(bool validateFilters)
            : base(validateFilters)
        {}

        /// <summary>
        /// Construct a new QueryEngine with the specified validation options.
        /// </summary>
        /// <param name="validationOptions">The validation options to use in this engine.</param>
        public QueryEngine(QueryValidationOptions validationOptions)
            : base(validationOptions)
        {}
    }
}
