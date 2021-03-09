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
    }

    class QueryEngineParserData
    {
        public List<IQueryNode> expressionNodes;
        public NodesToStringPosition nodesToStringPosition;
        public ICollection<string> tokens;

        public QueryEngineParserData(NodesToStringPosition nodesToStringPosition, ICollection<string> tokens)
        {
            expressionNodes = new List<IQueryNode>();
            this.nodesToStringPosition = nodesToStringPosition;
            this.tokens = tokens;
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

    sealed class QueryEngineImpl<TData> : QueryTokenizer<QueryEngineParserData>, IQueryEngineImplementation
    {
        Dictionary<string, IFilter> m_Filters = new Dictionary<string, IFilter>();
        Func<TData, string, string, string, bool> m_DefaultFilterHandler;
        Func<TData, string, string, string, string, bool> m_DefaultParamFilterHandler;
        Dictionary<string, FilterOperator> m_FilterOperators = new Dictionary<string, FilterOperator>();
        List<ITypeParser> m_TypeParsers = new List<ITypeParser>();
        Dictionary<Type, ITypeParser> m_DefaultTypeParsers = new Dictionary<Type, ITypeParser>();
        Dictionary<Type, IFilterOperationGenerator> m_FilterOperationGenerators = new Dictionary<Type, IFilterOperationGenerator>();

        Regex m_PartialFilterRx = new Regex(QueryRegexValues.k_PartialFilterNamePattern +
            QueryRegexValues.k_PartialFilterFunctionPattern +
            QueryRegexValues.k_PartialFilterOperatorsPattern +
            QueryRegexValues.k_PartialFilterValuePattern, RegexOptions.Compiled);

        static readonly Dictionary<string, Func<IQueryNode>> k_CombiningTokenGenerators = new Dictionary<string, Func<IQueryNode>>
        {
            {"and", () => new AndNode()},
            {"or", () => new OrNode()},
            {"not", () => new NotNode()},
            {"-", () => new NotNode()}
        };

        enum DefaultOperator
        {
            Contains,
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        }

        readonly struct FilterCreationParams
        {
            public readonly string token;
            public readonly string[] supportedOperators;
            public readonly bool overridesGlobalComparisonOptions;
            public readonly StringComparison comparisonOptions;
            public readonly bool useParameterTransformer;
            public readonly string parameterTransformerFunction;
            public readonly Type parameterTransformerAttributeType;

            public FilterCreationParams(
                string token, string[] supportedOperators, bool overridesGlobalComparison,
                StringComparison comparisonOptions, bool useParameterTransformer,
                string parameterTransformerFunction, Type parameterTransformerAttributeType)
            {
                this.token = token;
                this.supportedOperators = supportedOperators;
                this.overridesGlobalComparisonOptions = overridesGlobalComparison;
                this.comparisonOptions = comparisonOptions;
                this.useParameterTransformer = useParameterTransformer;
                this.parameterTransformerFunction = parameterTransformerFunction;
                this.parameterTransformerAttributeType = parameterTransformerAttributeType;
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

        public QueryEngineImpl()
            : this(new QueryValidationOptions())
        {}

        public QueryEngineImpl(QueryValidationOptions validationOptions)
        {
            this.validationOptions = validationOptions;

            // Default operators
            AddOperator(":", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.Contains, ":"))
                .AddHandler((string ev, string fv, StringComparison sc) => ev?.IndexOf(fv, sc) >= 0)
                .AddHandler((int ev, int fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1)
                .AddHandler((float ev, float fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1)
                .AddHandler((double ev, double fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1);
            AddOperator("=", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.Equal, "="))
                .AddHandler((int ev, int fv) => ev == fv)
                .AddHandler((float ev, float fv) => Math.Abs(ev - fv) < Mathf.Epsilon)
                .AddHandler((double ev, double fv) => Math.Abs(ev - fv) < double.Epsilon)
                .AddHandler((bool ev, bool fv) => ev == fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Equals(ev, fv, sc));
            AddOperator("!=", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.NotEqual, "!="))
                .AddHandler((int ev, int fv) => ev != fv)
                .AddHandler((float ev, float fv) => Math.Abs(ev - fv) >= Mathf.Epsilon)
                .AddHandler((double ev, double fv) => Math.Abs(ev - fv) >= double.Epsilon)
                .AddHandler((bool ev, bool fv) => ev != fv)
                .AddHandler((string ev, string fv, StringComparison sc) => !string.Equals(ev, fv, sc));
            AddOperator("<", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.Less, "<"))
                .AddHandler((int ev, int fv) => ev < fv)
                .AddHandler((float ev, float fv) => ev < fv)
                .AddHandler((double ev, double fv) => ev < fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) < 0);
            AddOperator(">", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.Greater, ">"))
                .AddHandler((int ev, int fv) => ev > fv)
                .AddHandler((float ev, float fv) => ev > fv)
                .AddHandler((double ev, double fv) => ev > fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) > 0);
            AddOperator("<=", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.LessOrEqual, "<="))
                .AddHandler((int ev, int fv) => ev <= fv)
                .AddHandler((float ev, float fv) => ev <= fv)
                .AddHandler((double ev, double fv) => ev <= fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) <= 0);
            AddOperator(">=", false)
                .AddHandler((object ev, object fv, StringComparison sc) => CompareObjects(ev, fv, sc, DefaultOperator.GreaterOrEqual, ">="))
                .AddHandler((int ev, int fv) => ev >= fv)
                .AddHandler((float ev, float fv) => ev >= fv)
                .AddHandler((double ev, double fv) => ev >= fv)
                .AddHandler((string ev, string fv, StringComparison sc) => string.Compare(ev, fv, sc) >= 0);

            BuildFilterRegex();
            BuildDefaultTypeParsers();

            // Insert consumer before the word consumer
            m_TokenConsumers.Insert(m_TokenConsumers.Count - 1, new QueryTokenHandler(MatchPartialFilter, ConsumePartialFilter));
        }

        bool CompareObjects(object ev, object fv, StringComparison sc, DefaultOperator op, string opToken)
        {
            if (ev == null || fv == null)
                return false;

            var evt = ev.GetType();
            var fvt = fv.GetType();

            if (m_FilterOperators.TryGetValue(opToken, out var operators))
            {
                var opHandler = operators.GetHandler(evt, fvt);
                if (opHandler != null)
                    return opHandler.Invoke(ev, fv, sc);
            }

            if (evt != fvt)
                return false;

            if (ev is string evs && fv is string fvs)
            {
                switch (op)
                {
                    case DefaultOperator.Contains: return evs.IndexOf(fvs, sc) != -1;
                    case DefaultOperator.Equal: return evs.Equals(fvs, sc);
                    case DefaultOperator.NotEqual: return !evs.Equals(fvs, sc);
                    case DefaultOperator.Greater: return string.Compare(evs, fvs, sc) > 0;
                    case DefaultOperator.GreaterOrEqual: return string.Compare(evs, fvs, sc) >= 0;
                    case DefaultOperator.Less: return string.Compare(evs, fvs, sc) < 0;
                    case DefaultOperator.LessOrEqual: return string.Compare(evs, fvs, sc) <= 0;
                }

                return false;
            }

            switch (op)
            {
                case DefaultOperator.Contains: return ev.ToString().IndexOf(fv.ToString(), sc) >= 0;
                case DefaultOperator.Equal: return ev.Equals(fv);
                case DefaultOperator.NotEqual: return !ev.Equals(fv);
                case DefaultOperator.Greater: return Comparer<object>.Default.Compare(ev, fv) > 0;
                case DefaultOperator.GreaterOrEqual: return Comparer<object>.Default.Compare(ev, fv) >= 0;
                case DefaultOperator.Less: return Comparer<object>.Default.Compare(ev, fv) < 0;
                case DefaultOperator.LessOrEqual: return Comparer<object>.Default.Compare(ev, fv) <= 0;
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

        public void RemoveFilter(string token)
        {
            if (!m_Filters.ContainsKey(token))
            {
                Debug.LogWarning($"No filter found for \"{token}\".");
                return;
            }
            m_Filters.Remove(token);
        }

        public FilterOperator AddOperator(string op)
        {
            return AddOperator(op, true);
        }

        FilterOperator AddOperator(string op, bool rebuildFilterRegex)
        {
            if (m_FilterOperators.ContainsKey(op))
                return m_FilterOperators[op];
            var filterOperator = new FilterOperator(op, this);
            m_FilterOperators.Add(op, filterOperator);
            if (rebuildFilterRegex)
                BuildFilterRegex();
            return filterOperator;
        }

        public FilterOperator GetOperator(string op)
        {
            return m_FilterOperators.ContainsKey(op) ? m_FilterOperators[op] : null;
        }

        public void AddOperatorHandler<TLhs, TRhs>(string op, Func<TLhs, TRhs, bool> handler)
        {
            AddOperatorHandler<TLhs, TRhs>(op, (ev, fv, sc) => handler(ev, fv));
        }

        public void AddOperatorHandler<TLhs, TRhs>(string op, Func<TLhs, TRhs, StringComparison, bool> handler)
        {
            if (!m_FilterOperators.ContainsKey(op))
                return;
            m_FilterOperators[op].AddHandler(handler);

            // Enums are user defined but still simple enough to generate a parse function for them.
            if (typeof(TRhs).IsEnum && !m_DefaultTypeParsers.ContainsKey(typeof(TRhs)))
            {
                AddDefaultEnumTypeParser<TRhs>();
            }
        }

        public void SetDefaultFilter(Func<TData, string, string, string, bool> handler)
        {
            m_DefaultFilterHandler = handler;
        }

        public void SetDefaultParamFilter(Func<TData, string, string, string, string, bool> handler)
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

        public void SetsearchWordMatcher(Func<string, bool, StringComparison, string, bool> wordMatcher)
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

        IQueryNode BuildGraphRecursively(string text, int startIndex, int endIndex, ICollection<QueryError> errors, ICollection<string> tokens, NodesToStringPosition nodesToStringPosition)
        {
            var userData = new QueryEngineParserData(nodesToStringPosition, tokens);
            var result = Parse(text, startIndex, endIndex, errors, userData);
            if (result != ParseState.Matched)
                return null;

            InsertAndIfNecessary(userData.expressionNodes, nodesToStringPosition);
            var rootNode = CombineNodesToTree(userData.expressionNodes, errors, nodesToStringPosition);
            return rootNode;
        }

        internal BuildGraphResult BuildGraph(string text, ICollection<QueryError> errors, ICollection<string> tokens)
        {
            if (text == null)
                return new BuildGraphResult(new QueryGraph(null), new QueryGraph(null));

            var nodesToStringPosition = new NodesToStringPosition();
            var evaluationRootNode = BuildGraphRecursively(text, 0, text.Length, errors, tokens, nodesToStringPosition);

            // Make a copy of the query graph for the stock query graph
            IQueryNode queryRootNode = null;
            if (evaluationRootNode != null)
            {
                if (!(evaluationRootNode is ICopyableNode copyableRoot))
                    throw new Exception($"Root node should be an instance of {nameof(ICopyableNode)}.");
                queryRootNode = copyableRoot.Copy();
            }

            // Final simplification
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

        protected override bool ConsumeEmpty(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            return true;
        }

        protected override bool ConsumeCombiningToken(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var token = sv.ToString();
            if (!k_CombiningTokenGenerators.TryGetValue(token, out var generator))
                return false;
            var newNode = generator();
            newNode.token = new QueryToken(token, startIndex);
            userData.nodesToStringPosition.Add(newNode, new QueryToken(startIndex, sv.Length));
            userData.expressionNodes.Add(newNode);
            userData.tokens.Add(token);
            return true;
        }

        protected override bool ConsumeFilter(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var node = CreateFilterToken(match.Value, match, startIndex, errors);
            if (node == null)
                return false;

            node.token = new QueryToken(match.Value, startIndex);
            userData.nodesToStringPosition.Add(node, new QueryToken(startIndex, match.Length));
            userData.expressionNodes.Add(node);
            userData.tokens.Add(match.Value);
            return true;
        }

        int MatchPartialFilter(string text, int startIndex, int endIndex, ICollection<QueryError> errors, out StringView sv, out Match match, out bool matched)
        {
            sv = text.GetStringView();
            match = m_PartialFilterRx.Match(text, startIndex, endIndex - startIndex);

            if (!match.Success)
            {
                matched = false;
                return -1;
            }

            matched = true;
            return match.Length;
        }

        bool ConsumePartialFilter(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var node = CreatePartialFilterToken(match.Value, match, startIndex, errors);
            if (node == null)
                return false;

            node.token = new QueryToken(match.Value, startIndex);
            userData.nodesToStringPosition.Add(node, new QueryToken(startIndex, match.Length));
            userData.expressionNodes.Add(node);
            userData.tokens.Add(match.Value);
            return true;
        }

        protected override bool ConsumeWord(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            if (validationOptions.validateFilters && searchDataCallback == null)
            {
                errors.Add(new QueryError(startIndex, match.Length, "Cannot use a search word without setting the search data callback."));
                return false;
            }

            var node = CreateWordExpressionNode(match.Value);
            if (node == null)
                return false;

            node.token = new QueryToken(match.Value, startIndex);
            userData.nodesToStringPosition.Add(node, new QueryToken(startIndex, match.Length));
            userData.expressionNodes.Add(node);
            if (!node.skipped)
                userData.tokens.Add(match.Value);
            return true;
        }

        protected override bool ConsumeGroup(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            var groupNode = BuildGraphRecursively(text, startIndex + 1, endIndex - 1, errors, userData.tokens, userData.nodesToStringPosition);
            if (groupNode == null)
                return false;

            userData.expressionNodes.Add(groupNode);
            return true;
        }

        protected override bool ConsumeNestedQuery(string text, int startIndex, int endIndex, StringView sv, Match match, ICollection<QueryError> errors, QueryEngineParserData userData)
        {
            if (validationOptions.validateFilters && !validationOptions.skipNestedQueries)
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

            var nestedQueryAggregator = match.Groups[1].Value;
            var queryString = match.Groups[2].Value;

            var nestedQueryString = queryString.Substring(1, queryString.Length - 2);
            nestedQueryString = nestedQueryString.Trim();
            var nestedQueryNode = new NestedQueryNode(nestedQueryString, m_NestedQueryHandler);
            nestedQueryNode.token = new QueryToken(startIndex + (string.IsNullOrEmpty(nestedQueryAggregator) ? 0 : nestedQueryAggregator.Length), queryString.Length);
            userData.nodesToStringPosition.Add(nestedQueryNode, nestedQueryNode.token);
            if (validationOptions.skipNestedQueries)
            {
                nestedQueryNode.skipped = true;
                userData.expressionNodes.Add(nestedQueryNode);
                return true;
            }

            userData.tokens.Add(match.Value);

            if (string.IsNullOrEmpty(nestedQueryAggregator))
                userData.expressionNodes.Add(nestedQueryNode);
            else
            {
                var lowerAggregator = nestedQueryAggregator.ToLowerInvariant();
                if (!m_NestedQueryAggregators.TryGetValue(lowerAggregator, out var aggregator) && validationOptions.validateFilters)
                {
                    errors.Add(new QueryError(startIndex, nestedQueryAggregator.Length, $"Unknown nested query aggregator \"{nestedQueryAggregator}\""));
                    return false;
                }

                var aggregatorNode = new AggregatorNode(nestedQueryAggregator, aggregator);
                aggregatorNode.token = new QueryToken(startIndex, nestedQueryAggregator.Length + nestedQueryNode.token.length);
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

        IQueryNode CreateFilterToken(string token, Match match, int index, ICollection<QueryError> errors)
        {
            if (match.Groups.Count != 7) // There is 6 groups plus the balancing group
            {
                errors.Add(new QueryError(index, token.Length, $"Could not parse filter block \"{token}\"."));
                return null;
            }

            var filterType = match.Groups[1].Value;
            var filterParam = match.Groups[2].Value;
            var filterOperator = match.Groups[3].Value;
            var filterValue = match.Groups[4].Value;
            var nestedQueryAggregator = match.Groups[5].Value;

            var filterTypeIndex = match.Groups[1].Index;
            var filterOperatorIndex = match.Groups[3].Index;
            var filterValueIndex = match.Groups[4].Index;

            if (!string.IsNullOrEmpty(filterParam))
            {
                // Trim () around the group
                filterParam = filterParam.Trim('(', ')');
            }

            if (!m_Filters.TryGetValue(filterType, out var filter))
            {
                // When skipping unknown filter, just return a noop. The graph will get simplified later.
                if (validationOptions.skipUnknownFilters)
                {
                    return new FilterNode(filterType, filterOperator, filterValue, filterParam, token) {skipped = true};
                }

                if (!HasDefaultHandler(!string.IsNullOrEmpty(filterParam)) && validationOptions.validateFilters)
                {
                    errors.Add(new QueryError(filterTypeIndex, filterType.Length, $"Unknown filter \"{filterType}\"."));
                    return null;
                }
                if (string.IsNullOrEmpty(filterParam))
                    filter = new DefaultFilter<TData>(filterType, m_DefaultFilterHandler ?? ((o, s, fo, value) => false));
                else
                    filter = new DefaultParamFilter<TData>(filterType, m_DefaultParamFilterHandler ?? ((o, s, param, fo, value) => false));
            }

            if (!m_FilterOperators.ContainsKey(filterOperator))
            {
                errors.Add(new QueryError(filterOperatorIndex, filterOperator.Length, $"Unknown filter operator \"{filterOperator}\"."));
                return null;
            }
            var op = m_FilterOperators[filterOperator];

            if (filter.supportedFilters.Any() && !filter.supportedFilters.Any(filterOp => filterOp.Equals(op.token)))
            {
                errors.Add(new QueryError(filterOperatorIndex, filterOperator.Length, $"The operator \"{op.token}\" is not supported for this filter."));
                return null;
            }

            if (QueryEngineUtils.IsPhraseToken(filterValue))
                filterValue = filterValue.Trim('"');

            IQueryNode filterNode;

            if (QueryEngineUtils.IsNestedQueryToken(filterValue))
            {
                if (validationOptions.skipNestedQueries)
                {
                    return new InFilterNode(filter, op, filterValue, filterParam, token) {skipped = true};
                }

                if (validationOptions.validateFilters && m_NestedQueryHandler == null)
                {
                    errors.Add(new QueryError(filterValueIndex, filterValue.Length, $"Cannot use a nested query without setting the handler first."));
                    return null;
                }

                filterNode = new InFilterNode(filter, op, filterValue, filterParam, token);

                var startIndex = filterValue.IndexOf('{') + 1;
                filterValue = filterValue.Substring(startIndex, filterValue.Length - startIndex - 1);
                filterValue = filterValue.Trim();
                var nestedQueryNode = new NestedQueryNode(filterValue, filterType, m_NestedQueryHandler);
                nestedQueryNode.token = new QueryToken(filterValue, filterValueIndex);

                if (!string.IsNullOrEmpty(nestedQueryAggregator))
                {
                    var lowerAggregator = nestedQueryAggregator.ToLowerInvariant();
                    if (!m_NestedQueryAggregators.TryGetValue(lowerAggregator, out var aggregator) && validationOptions.validateFilters)
                    {
                        errors.Add(new QueryError(filterValueIndex, nestedQueryAggregator.Length, $"Unknown nested query aggregator \"{nestedQueryAggregator}\""));
                        return null;
                    }

                    var aggregatorNode = new AggregatorNode(nestedQueryAggregator, aggregator);
                    aggregatorNode.token = new QueryToken(nestedQueryAggregator, filterValueIndex);

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
                filterNode = new FilterNode(filter, op, filterValue, filterParam, token);
            }

            return filterNode;
        }

        IQueryNode CreatePartialFilterToken(string token, Match match, int index, ICollection<QueryError> errors)
        {
            var filterType = match.Groups["name"].Value;
            var filterParam = match.Groups["f1"].Value;
            if (string.IsNullOrEmpty(filterParam))
                filterParam = match.Groups["f2"].Value;
            var filterOperator = match.Groups["op"].Value;
            var filterValue = match.Groups["value"].Value;

            var filterTypeIndex = match.Groups["name"].Index;
            var filterOperatorIndex = match.Groups["op"].Index;

            if (!string.IsNullOrEmpty(filterParam))
            {
                // Trim () around the group
                filterParam = filterParam.Trim('(', ')');
            }

            if (validationOptions.skipIncompleteFilters)
            {
                return new FilterNode(filterType, filterOperator, filterValue, filterParam, token) {skipped = true};
            }

            if (!m_Filters.TryGetValue(filterType, out var filter))
            {
                // When skipping unknown filter, just return a noop. The graph will get simplified later.
                if (validationOptions.skipUnknownFilters)
                {
                    return new FilterNode(filterType, filterOperator, filterValue, filterParam, token) { skipped = true };
                }

                if (!HasDefaultHandler(!string.IsNullOrEmpty(filterParam)) && validationOptions.validateFilters)
                {
                    errors.Add(new QueryError(filterTypeIndex, filterType.Length, $"Unknown filter \"{filterType}\"."));
                    return null;
                }
                if (string.IsNullOrEmpty(filterParam))
                    filter = new DefaultFilter<TData>(filterType, m_DefaultFilterHandler ?? ((o, s, fo, value) => false));
                else
                    filter = new DefaultParamFilter<TData>(filterType, m_DefaultParamFilterHandler ?? ((o, s, param, fo, value) => false));
            }

            FilterOperator op = null;
            if (!string.IsNullOrEmpty(filterOperator))
            {
                if (!m_FilterOperators.ContainsKey(filterOperator))
                {
                    errors.Add(new QueryError(filterOperatorIndex, filterOperator.Length, $"Unknown filter operator \"{filterOperator}\"."));
                    return null;
                }
                op = m_FilterOperators[filterOperator];

                if (filter.supportedFilters.Any() && !filter.supportedFilters.Any(filterOp => filterOp.Equals(op.token)))
                {
                    errors.Add(new QueryError(filterOperatorIndex, filterOperator.Length, $"The operator \"{op.token}\" is not supported for this filter."));
                    return null;
                }
            }

            if (QueryEngineUtils.IsPhraseToken(filterValue))
                filterValue = filterValue.Trim('"');

            var filterNode = new FilterNode(filter, op, filterValue, filterParam, token);
            errors.Add(new QueryError(index, token.Length, $"The filter \"{filterType}\" is incomplete."));

            return filterNode;
        }

        private bool HasDefaultHandler(bool useParamFilter)
        {
            return useParamFilter ? m_DefaultParamFilterHandler != null : m_DefaultFilterHandler != null;
        }

        public IParseResult ParseFilterValue(string filterValue, IFilter filter, FilterOperator op, out Type filterValueType)
        {
            var foundValueType = false;
            IParseResult parseResult = null;
            filterValueType = filter.type;

            // Filter resolver only support values of the same type as the filter in their resolver
            if (filter.resolver)
            {
                return ParseSpecificType(filterValue, filter.type);
            }

            // Check custom parsers first
            foreach (var typeParser in m_TypeParsers)
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

        IParseResult ParseSpecificType(string filterValue, Type type)
        {
            foreach (var typeParser in m_TypeParsers)
            {
                if (type != typeParser.type)
                    continue;

                return typeParser.Parse(filterValue);
            }

            return GenerateParseResultForType(filterValue, type);
        }

        IQueryNode CreateWordExpressionNode(string token)
        {
            var isExact = token.StartsWith("!");
            if (isExact)
                token = token.Remove(0, 1);
            if (QueryEngineUtils.IsPhraseToken(token))
                token = token.Trim('"');

            if (searchWordTransformerCallback != null)
                token = searchWordTransformerCallback(token);

            var searchNode = new SearchNode(token, isExact);
            if (string.IsNullOrEmpty(token))
                searchNode.skipped = true;
            return searchNode;
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

                // Otherwise, remove ourselves from our parent
                node.parent.children.Remove(node);
            }
            else
            {
                var children = combinedNode.children.ToArray();
                for (var i = 0; i < children.Length; ++i)
                {
                    RemoveSkippedNodes(ref children[i], errors, nodesToStringPosition);
                }

                // If we have become a leaf, remove ourselves from our parent
                if (combinedNode.leaf)
                {
                    if (combinedNode.parent != null)
                        combinedNode.parent.children.Remove(node);
                    else
                        node = null;

                    return;
                }

                // If we are a node with a single child and not a leaf, nothing to do.
                if (combinedNode.type == QueryNodeType.Not || combinedNode.type == QueryNodeType.Where || combinedNode.type == QueryNodeType.Aggregator)
                    return;

                // If we are an And or an Or and children count is still 2, nothing to do
                if (combinedNode.children.Count == 2)
                    return;

                // If we have no parent, we are the root so the remaining child must take our place
                if (combinedNode.parent == null)
                {
                    node = combinedNode.children[0];
                    return;
                }

                // Otherwise, replace ourselves with the remaining child in our parent child list
                var index = combinedNode.parent.children.IndexOf(combinedNode);
                if (index == -1)
                {
                    var t = nodesToStringPosition[node];
                    errors.Add(new QueryError(t.position, $"Node {combinedNode.type} not found in its parent's children list."));
                    return;
                }

                combinedNode.parent.children[index] = combinedNode.children[0];
            }
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

        void BuildFilterRegex()
        {
            var sortedOperators = m_FilterOperators.Keys.Select(Regex.Escape).ToList();
            sortedOperators.Sort((s, s1) => s1.Length.CompareTo(s.Length));
            BuildFilterRegex(sortedOperators);

            var innerOperatorsPattern = $"{string.Join("|", sortedOperators)}";
            var partialOperatorsPattern = $"(?<op>{innerOperatorsPattern})?";
            var partialFilterNamePattern = $"\\G(?<name>[\\w.]+(?=\\(|(?:{innerOperatorsPattern})))";
            m_PartialFilterRx = new Regex(partialFilterNamePattern + QueryRegexValues.k_PartialFilterFunctionPattern + partialOperatorsPattern + QueryRegexValues.k_PartialFilterValuePattern);
        }

        IParseResult GenerateParseResultForType(string value, Type type)
        {
            // Check if we have a default type parser before doing the expensive reflection call
            if (m_DefaultTypeParsers.ContainsKey(type))
            {
                var parser = m_DefaultTypeParsers[type];
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
            AddDefaultTypeParser(s => int.TryParse(s, out var value) ? new ParseResult<int>(true, value) : ParseResult<int>.none);
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

        void AddDefaultEnumTypeParser<T>()
        {
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
                AddFilter(filter.token, filter);
            }
        }

        IFilter CreateFilterFromFilterAttribute<TFilterAttribute, TTransformerAttribute>(MethodInfo mi)
            where TFilterAttribute : QueryEngineFilterAttribute
            where TTransformerAttribute : QueryEngineParameterTransformerAttribute
        {
            var attr = mi.GetCustomAttributes(typeof(TFilterAttribute), false).Cast<TFilterAttribute>().First();
            var filterToken = attr.token;
            var stringComparison = attr.overridesStringComparison ? attr.comparisonOptions : globalStringComparison;
            var supportedOperators = attr.supportedOperators;
            var creationParams = new FilterCreationParams(
                filterToken,
                supportedOperators,
                attr.overridesStringComparison,
                stringComparison,
                attr.useParamTransformer,
                attr.paramTransformerFunction,
                typeof(TTransformerAttribute)
            );

            try
            {
                var inputParams = mi.GetParameters();
                if (inputParams.Length == 0)
                {
                    Debug.LogWarning($"Filter method {mi.Name} should have at least one input parameter.");
                    return null;
                }

                var objectParam = inputParams[0];
                if (objectParam.ParameterType != typeof(TData))
                {
                    Debug.LogWarning($"Parameter {objectParam.Name}'s type of filter method {mi.Name} must be {typeof(TData)}.");
                    return null;
                }
                var returnType = mi.ReturnType;

                // Basic filter
                if (inputParams.Length == 1)
                {
                    return CreateFilterForMethodInfo(creationParams, mi, false, false, returnType);
                }

                // Filter function
                if (inputParams.Length == 2)
                {
                    var filterParamType = inputParams[1].ParameterType;
                    return CreateFilterForMethodInfo(creationParams, mi, true, false, filterParamType, returnType);
                }

                // Filter resolver
                if (inputParams.Length == 3)
                {
                    var operatorType = inputParams[1].ParameterType;
                    var filterValueType = inputParams[2].ParameterType;
                    if (operatorType != typeof(string))
                    {
                        Debug.LogWarning($"Parameter {inputParams[1].Name}'s type of filter method {mi.Name} must be {typeof(string)}.");
                        return null;
                    }

                    if (returnType != typeof(bool))
                    {
                        Debug.LogWarning($"Return type of filter method {mi.Name} must be {typeof(bool)}.");
                        return null;
                    }

                    return CreateFilterForMethodInfo(creationParams, mi, false, true, filterValueType);
                }

                // Filter function resolver
                if (inputParams.Length == 4)
                {
                    var filterParamType = inputParams[1].ParameterType;
                    var operatorType = inputParams[2].ParameterType;
                    var filterValueType = inputParams[3].ParameterType;
                    if (operatorType != typeof(string))
                    {
                        Debug.LogWarning($"Parameter {inputParams[1].Name}'s type of filter method {mi.Name} must be {typeof(string)}.");
                        return null;
                    }

                    if (returnType != typeof(bool))
                    {
                        Debug.LogWarning($"Return type of filter method {mi.Name} must be {typeof(bool)}.");
                        return null;
                    }

                    return CreateFilterForMethodInfo(creationParams, mi, true, true, filterParamType, filterValueType);
                }

                Debug.LogWarning($"Error while creating filter {filterToken}. Parameter count mismatch.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while creating filter {filterToken}. {ex.Message}");
                return null;
            }
        }

        IFilter CreateFilterForMethodInfo(FilterCreationParams creationParams, MethodInfo mi, bool filterFunction, bool resolver, params Type[] methodTypes)
        {
            var methodName = $"CreateFilter{(filterFunction ? "Function" : "")}{(resolver ? "Resolver" : "")}ForMethodInfoTyped";
            var thisClassType = typeof(QueryEngineImpl<TData>);
            var method = thisClassType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            var typedMethod = method.MakeGenericMethod(methodTypes);
            return typedMethod.Invoke(this, new object[] { creationParams, mi }) as IFilter;
        }

        IFilter CreateFilterForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TFilter>), mi) as Func<TData, TFilter>;
            if (creationParams.overridesGlobalComparisonOptions)
                return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, creationParams.comparisonOptions);
            return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc);
        }

        IFilter CreateFilterResolverForMethodInfoTyped<TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, string, TFilter, bool>), mi) as Func<TData, string, TFilter, bool>;
            return new Filter<TData, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc);
        }

        IFilter CreateFilterFunctionForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TParam, TFilter>), mi) as Func<TData, TParam, TFilter>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                if (creationParams.overridesGlobalComparisonOptions)
                    return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc, creationParams.comparisonOptions);
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc);
            }

            if (creationParams.overridesGlobalComparisonOptions)
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, creationParams.comparisonOptions);
            return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc);
        }

        IFilter CreateFilterFunctionResolverForMethodInfoTyped<TParam, TFilter>(FilterCreationParams creationParams, MethodInfo mi)
        {
            var methodFunc = Delegate.CreateDelegate(typeof(Func<TData, TParam, string, TFilter, bool>), mi) as Func<TData, TParam, string, TFilter, bool>;

            if (creationParams.useParameterTransformer)
            {
                var parameterTransformerFunc = GetParameterTransformerFunction<TParam>(mi, creationParams.parameterTransformerFunction, creationParams.parameterTransformerAttributeType);
                return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc, parameterTransformerFunc);
            }
            return new Filter<TData, TParam, TFilter>(creationParams.token, creationParams.supportedOperators, methodFunc);
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
    }

    /// <summary>
    /// A QueryEngine defines how to build a query from an input string.
    /// It can be customized to support custom filters and operators.
    /// </summary>
    /// <typeparam name="TData">The filtered data type.</typeparam>
    public class QueryEngine<TData>
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

        /// <summary>
        /// Add a new custom filter.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and returns an object of type TFilter.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TFilter>(string token, Func<TData, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, getDataFunc);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and returns an object of type TFilter.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TFilter>(string token, Func<TData, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, stringComparison);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="getDataFunc">Callback used to get the object that is used in the filter. Takes an object of type TData and TParam, and returns an object of type TFilter.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="stringComparison">String comparison options.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, TFilter> getDataFunc, Func<string, TParam> parameterTransformer, StringComparison stringComparison, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, getDataFunc, parameterTransformer, stringComparison);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TFilter>(string token, Func<TData, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TFilter>(token, supportedOperatorType, filterResolver);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, an object of type TParam, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, string, TFilter, bool> filterResolver, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver);
            m_Impl.AddFilter(token, filter);
        }

        /// <summary>
        /// Add a new custom filter function with a custom resolver. Useful when you wish to handle all operators yourself.
        /// </summary>
        /// <typeparam name="TParam">The type of the constant parameter passed to the function.</typeparam>
        /// <typeparam name="TFilter">The type of the data that is compared by the filter.</typeparam>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="filterResolver">Callback used to handle any operators for this filter. Takes an object of type TData, an object of type TParam, the operator token and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        /// <param name="parameterTransformer">Callback used to convert a string to the type TParam. Used when parsing the query to convert what is passed to the function into the correct format.</param>
        /// <param name="supportedOperatorType">List of supported operator tokens. Null for all operators.</param>
        public void AddFilter<TParam, TFilter>(string token, Func<TData, TParam, string, TFilter, bool> filterResolver, Func<string, TParam> parameterTransformer, string[] supportedOperatorType = null)
        {
            var filter = new Filter<TData, TParam, TFilter>(token, supportedOperatorType, filterResolver, parameterTransformer);
            m_Impl.AddFilter(token, filter);
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
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <remarks>You will get a warning if you try to remove a non existing filter.</remarks>
        public void RemoveFilter(string token)
        {
            m_Impl.RemoveFilter(token);
        }

        /// <summary>
        /// Add a custom filter operator.
        /// </summary>
        /// <param name="op">The operator identifier.</param>
        public void AddOperator(string op)
        {
            m_Impl.AddOperator(op);
        }

        internal FilterOperator GetOperator(string op)
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

        /// <summary>
        /// Add a type parser that parse a string and returns a custom type. Used
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
            m_Impl.SetDefaultFilter(handler);
        }

        /// <summary>
        /// Set the default filter handler for function filters that were not registered.
        /// </summary>
        /// <param name="handler">Callback used to handle the function filter. Takes an object of type TData, the filter identifier, the parameter, the operator and the filter value, and returns a boolean indicating if the filter passed or not.</param>
        public void SetDefaultParamFilter(Func<TData, string, string, string, string, bool> handler)
        {
            m_Impl.SetDefaultParamFilter(handler);
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
            m_Impl.SetsearchWordMatcher(wordMatcher);
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
        /// Parse a query string into a Query operation. This Query operation can then be used to filter any data set of type TData.
        /// </summary>
        /// <param name="text">The query input string.</param>
        /// <param name="useFastYieldingQueryHandler">Set to true to get a query that will yield null results for elements that don't pass the query, instead of only the elements that pass the query.</param>
        /// <returns>Query operation of type TData.</returns>
        public Query<TData> Parse(string text, bool useFastYieldingQueryHandler = false)
        {
            var handlerFactory = new DefaultQueryHandlerFactory<TData>(this, useFastYieldingQueryHandler);
            return BuildQuery(text, handlerFactory, (evalGraph, queryGraph, errors, tokens, handler) => new Query<TData>(text, evalGraph, queryGraph, errors, tokens, handler));
        }

        /// <summary>
        /// Parse a query string into a Query operation. This Query operation can then be used to filter any data set of type TData.
        /// </summary>
        /// <typeparam name="TQueryHandler">Type of the underlying query handler. See IQueryHandler.</typeparam>
        /// <typeparam name="TPayload">Type of the payload that the query handler receives.</typeparam>
        /// <param name="text">The query input string.</param>
        /// <param name="queryHandlerFactory">A factory object that creates query handlers of type TQueryHandler. See IQueryHandlerFactory.</param>
        /// <returns>Query operation of type TData and TPayload.</returns>
        public Query<TData, TPayload> Parse<TQueryHandler, TPayload>(string text, IQueryHandlerFactory<TData, TQueryHandler, TPayload> queryHandlerFactory)
            where TQueryHandler : IQueryHandler<TData, TPayload>
            where TPayload : class
        {
            return BuildQuery(text, queryHandlerFactory, (evalGraph, queryGraph, errors, tokens, handler) => new Query<TData, TPayload>(text, evalGraph, queryGraph, errors, tokens, handler));
        }

        TQuery BuildQuery<TQuery, TQueryHandler, TPayload>(string text, IQueryHandlerFactory<TData, TQueryHandler, TPayload> queryHandlerFactory, Func<QueryGraph, QueryGraph, ICollection<QueryError>, ICollection<string>, TQueryHandler, TQuery> queryCreator)
            where TQueryHandler : IQueryHandler<TData, TPayload>
            where TPayload : class
            where TQuery : Query<TData, TPayload>
        {
            var errors = new List<QueryError>();
            var tokens = new List<string>();
            var result = m_Impl.BuildGraph(text, errors, tokens);
            return queryCreator(result.evaluationGraph, result.queryGraph, errors, tokens, queryHandlerFactory.Create(result.evaluationGraph, errors));
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
        /// <param name="filterToken">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
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

        internal IParseResult ParseFilterValue(string filterValue, IFilter filter, FilterOperator op, out Type filterValueType)
        {
            return m_Impl.ParseFilterValue(filterValue, filter, op, out filterValueType);
        }

        internal IFilterOperationGenerator GetGeneratorForType(Type type)
        {
            return m_Impl.GetGeneratorForType(type);
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
