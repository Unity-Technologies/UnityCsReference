// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Search
{
    /// <summary>
    /// Interface for Query filters.
    /// </summary>
    public interface IQueryEngineFilter
    {
        /// <summary>
        /// The identifier of the filter. Typically what precedes the operator in a filter (for example, "id" in "id>=2").
        /// </summary>
        string token { get; }

        /// <summary>
        /// The regular expression that matches the filter. Matches what precedes the operator in a filter (for example, "id" in "id>=2").
        /// </summary>
        Regex regexToken { get; }

        /// <summary>
        /// Indicates if the filter uses a regular expression token or not.
        /// </summary>
        bool usesRegularExpressionToken { get; }

        /// <summary>
        /// List of supported operators.
        /// </summary>
        IEnumerable<string> supportedOperators { get; }

        /// <summary>
        /// The type of the data that is compared by the filter.
        /// </summary>
        Type type { get; }

        /// <summary>
        /// Indicates if the filter uses a parameter.
        /// </summary>
        bool usesParameter { get; }

        /// <summary>
        /// The type of the constant parameter passed to the filter.
        /// </summary>
        Type parameterType { get; }

        /// <summary>
        /// Indicates if the filter uses a resolver function.
        /// </summary>
        bool usesResolver { get; }

        /// <summary>
        /// The string comparison options of the filter.
        /// </summary>
        StringComparison stringComparison { get; }

        /// <summary>
        /// Indicates if the filter overrides the global string comparison options.
        /// </summary>
        bool overridesStringComparison { get; }

        /// <summary>
        /// Collection of <see cref="QueryFilterOperator"/>s specific for the filter.
        /// </summary>
        IReadOnlyDictionary<string, QueryFilterOperator> operators { get; }

        /// <summary>
        /// Additional information specific to the filter.
        /// </summary>
        IReadOnlyDictionary<string, string> metaInfo { get; }

        /// <summary>
        /// Set the filter's nested query transformer function. This function takes the result of a nested query and extract the necessary data to compare with the filter.
        /// </summary>
        /// <typeparam name="TNestedQueryData">The type of data returned by the nested query.</typeparam>
        /// <typeparam name="TRhs">The type expected on the right hand side of the filter.</typeparam>
        /// <param name="transformer">The transformer function.</param>
        void SetNestedQueryTransformer<TNestedQueryData, TRhs>(Func<TNestedQueryData, TRhs> transformer);

        /// <summary>
        /// Add a type parser specific to the filter that parse a string and returns a custom type. Used
        /// by custom operator handlers.
        /// </summary>
        /// <typeparam name="TFilterConstant">The type of the parsed operand that is on the right hand side of the operator.</typeparam>
        /// <param name="parser">Callback used to determine if a string can be converted into TFilterConstant. Takes a string and returns a <see cref="ParseResult{TFilterConstant}"/> object. This contains the success flag, and the actual converted value if it succeeded.</param>
        /// <returns>The current <see cref="IQueryEngineFilter"/>.</returns>
        IQueryEngineFilter AddTypeParser<TFilterConstant>(Func<string, ParseResult<TFilterConstant>> parser);

        /// <summary>
        /// Add a custom filter operator specific to the filter. If the operator already exists, the existing operator is returned.
        /// </summary>
        /// <param name="op">The operator identifier.</param>
        /// <returns>The added <see cref="QueryFilterOperator"/>.</returns>
        QueryFilterOperator AddOperator(string op);

        /// <summary>
        /// Remove a custom operator specific to the filter.
        /// </summary>
        /// <param name="op">The operator identifier.</param>
        /// <returns>The current <see cref="IQueryEngineFilter"/>.</returns>
        IQueryEngineFilter RemoveOperator(string op);

        /// <summary>
        /// Add additional information specific to the filter.
        /// </summary>
        /// <param name="key">The key of the information.</param>
        /// <param name="value">The value of the information.</param>
        /// <returns>The current <see cref="IQueryEngineFilter"/>.</returns>
        IQueryEngineFilter AddOrUpdateMetaInfo(string key, string value);

        /// <summary>
        /// Remove information on the filter.
        /// </summary>
        /// <param name="key">The key of the information.</param>
        /// <returns>The current <see cref="IQueryEngineFilter"/>.</returns>
        IQueryEngineFilter RemoveMetaInfo(string key);

        /// <summary>
        /// Removes all additional information specific to the filter.
        /// </summary>
        /// <returns>The current <see cref="IQueryEngineFilter"/>.</returns>
        IQueryEngineFilter ClearMetaInfo();
    }

    interface IFilter : IQueryEngineFilter
    {
        IEnumerable<ITypeParser> typeParsers { get; }
        IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors);
        INestedQueryHandlerTransformer nestedQueryHandlerTransformer { get; }
    }

    abstract class BaseFilter<TFilter, TEngineType> : IFilter
    {
        public string token { get; }
        public Regex regexToken { get; }
        public virtual bool usesRegularExpressionToken => false;
        public IEnumerable<string> supportedOperators { get; protected set; }
        public Type type => typeof(TFilter);
        public virtual bool usesParameter => false;
        public virtual Type parameterType => typeof(object);

        public bool usesResolver { get; protected set; }

        public StringComparison stringComparison { get; }
        public bool overridesStringComparison { get; }

        public INestedQueryHandlerTransformer nestedQueryHandlerTransformer { get; private set; }

        protected QueryEngineImpl<TEngineType> m_QueryEngine;

        List<ITypeParser> m_TypeParsers = new List<ITypeParser>();
        public IEnumerable<ITypeParser> typeParsers => m_TypeParsers;

        Dictionary<string, QueryFilterOperator> m_FilterOperators = new Dictionary<string, QueryFilterOperator>();
        public IReadOnlyDictionary<string, QueryFilterOperator> operators => m_FilterOperators;

        Dictionary<string, string> m_MetaInfo = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> metaInfo => m_MetaInfo;

        protected BaseFilter(string token, IEnumerable<string> supportedOperatorTypes, bool resolver, QueryEngineImpl<TEngineType> queryEngine)
        {
            this.token = token;
            supportedOperators = supportedOperatorTypes ?? new string[] {};
            usesResolver = resolver;
            overridesStringComparison = false;
            m_QueryEngine = queryEngine;
        }

        protected BaseFilter(string token, IEnumerable<string> supportedOperatorTypes, bool resolver, StringComparison stringComparison, QueryEngineImpl<TEngineType> queryEngine)
            : this(token, supportedOperatorTypes, resolver, queryEngine)
        {
            this.stringComparison = stringComparison;
            overridesStringComparison = true;
        }

        protected BaseFilter(Regex token, IEnumerable<string> supportedOperatorTypes, bool resolver, QueryEngineImpl<TEngineType> queryEngine)
            : this(string.Empty, supportedOperatorTypes, resolver, queryEngine)
        {
            regexToken = token;
            ValidateRegexToken();
            BuildFilterMatchers();
        }

        protected BaseFilter(Regex token, IEnumerable<string> supportedOperatorTypes, bool resolver, StringComparison stringComparison, QueryEngineImpl<TEngineType> queryEngine)
            : this(string.Empty, supportedOperatorTypes, resolver, stringComparison, queryEngine)
        {
            regexToken = token;
            ValidateRegexToken();
            BuildFilterMatchers();
        }

        public abstract IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors);

        public void SetNestedQueryTransformer<TNestedQueryData, TRhs>(Func<TNestedQueryData, TRhs> transformer)
        {
            if (transformer == null)
                return;
            nestedQueryHandlerTransformer = new NestedQueryHandlerTransformer<TNestedQueryData, TRhs>(transformer);
        }

        public IQueryEngineFilter AddTypeParser<TFilterConstant>(Func<string, ParseResult<TFilterConstant>> parser)
        {
            m_TypeParsers.Add(new TypeParser<TFilterConstant>(parser));
            m_QueryEngine.AddFilterOperationGenerator<TFilterConstant>();
            return this;
        }

        public QueryFilterOperator AddOperator(string op)
        {
            if (m_FilterOperators.ContainsKey(op))
                return m_FilterOperators[op];
            var filterOperator = new QueryFilterOperator(op, m_QueryEngine);
            m_FilterOperators.Add(op, filterOperator);

            BuildFilterMatchers();

            return filterOperator;
        }

        public IQueryEngineFilter RemoveOperator(string op)
        {
            if (!m_FilterOperators.ContainsKey(op))
                return this;
            m_FilterOperators.Remove(op);
            BuildFilterMatchers();
            return this;
        }

        public IQueryEngineFilter AddOrUpdateMetaInfo(string key, string value)
        {
            m_MetaInfo[key] = value;
            return this;
        }

        public IQueryEngineFilter RemoveMetaInfo(string key)
        {
            m_MetaInfo.Remove(key);
            return this;
        }

        public IQueryEngineFilter ClearMetaInfo()
        {
            m_MetaInfo.Clear();
            return this;
        }

        void BuildFilterMatchers()
        {
            m_QueryEngine.BuildFilterMatchers(this);
        }

        void ValidateRegexToken()
        {
            // Group 0 is the match, group 1 if the first group
            if (regexToken.GetGroupNumbers().Length != 2)
                Debug.LogWarning($"Filter regex \"{regexToken}\" must have exactly one capturing group.");
        }

        public override int GetHashCode()
        {
            if (usesRegularExpressionToken)
                return regexToken.GetHashCode();
            return token.GetHashCode();
        }
    }

    class Filter<TData, TFilter> : BaseFilter<TFilter, TData>
    {
        Func<TData, TFilter> m_GetDataCallback;
        Func<TData, string, TFilter, bool> m_FilterResolver;

        public Filter(string token, IEnumerable<string> supportedOperatorType, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = data => default;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TFilter> getDataCallback, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TFilter> getDataCallback, StringComparison stringComparison, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, stringComparison, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, string, TFilter, bool> resolver, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, true, queryEngine)
        {
            m_FilterResolver = resolver;
        }

        public TFilter GetData(TData o)
        {
            return m_GetDataCallback(o);
        }

        public bool Resolve(TData data, in QueryFilterOperator op, TFilter value)
        {
            if (!usesResolver)
                return false;
            return m_FilterResolver(data, op.token, value);
        }

        public override IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors)
        {
            if (usesResolver)
            {
                // ReSharper disable once ConvertToLocalFunction
                Func<TData, TFilter, bool> operation = (o, fv) => Resolve(o, in data.op, fv);
                var filterOperation = new FilterOperation<TData, TFilter, TFilter>(this, in data.op, data.filterValue, operation);
                var filterValue = ((ParseResult<TFilter>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
                return filterOperation;
            }
            else
                return data.generator.GenerateOperation(data, this, operatorIndex, errors);
        }
    }

    class Filter<TData, TParam, TFilter> : BaseFilter<TFilter, TData>
    {
        public override bool usesParameter => true;
        public override Type parameterType => typeof(TParam);

        Func<TData, TParam, TFilter> m_GetDataCallback;
        Func<TData, TParam, string, TFilter, bool> m_FilterResolver;
        Func<string, TParam> m_ParameterTransformer;

        public Filter(string token, IEnumerable<string> supportedOperatorType, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = (data, param) => default;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, StringComparison stringComparison, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, stringComparison, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, Func<string, TParam> parameterTransformer, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
            m_ParameterTransformer = parameterTransformer;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, Func<string, TParam> parameterTransformer, StringComparison stringComparison, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, stringComparison, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
            m_ParameterTransformer = parameterTransformer;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, string, TFilter, bool> resolver, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, true, queryEngine)
        {
            m_FilterResolver = resolver;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, string, TFilter, bool> resolver, Func<string, TParam> parameterTransformer, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, true, queryEngine)
        {
            m_FilterResolver = resolver;
            m_ParameterTransformer = parameterTransformer;
        }

        public TFilter GetData(TData o, TParam p)
        {
            return m_GetDataCallback(o, p);
        }

        public bool Resolve(TData data, TParam param, in QueryFilterOperator op, TFilter value)
        {
            if (!usesResolver)
                return false;
            return m_FilterResolver(data, param, op.token, value);
        }

        public TParam TransformParameter(string param)
        {
            if (m_ParameterTransformer != null)
                return m_ParameterTransformer(param);

            return Utils.ConvertValue<TParam>(param);
        }

        public override IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors)
        {
            if (usesResolver)
            {
                // ReSharper disable once ConvertToLocalFunction
                Func<TData, TParam, TFilter, bool> operation = (o, param, fv) => Resolve(o, param, in data.op, fv);

                var filterOperation = new FilterOperation<TData, TParam, TFilter, TFilter>(this, in data.op, data.filterValue, data.paramValue, operation);
                var filterValue = ((ParseResult<TFilter>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
                return filterOperation;
            }
            else
                return data.generator.GenerateOperation(data, this, operatorIndex, errors);
        }
    }

    class RegexFilter<TData, TFilter> : BaseFilter<TFilter, TData>
    {
        Func<TData, string, TFilter> m_GetDataCallback;
        Func<TData, string, string, TFilter, bool> m_FilterResolver;

        public override bool usesRegularExpressionToken => true;

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = (data, s) => default;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TFilter> getDataCallback, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TFilter> getDataCallback, StringComparison stringComparison, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, stringComparison, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, string, TFilter, bool> resolver, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, true, queryEngine)
        {
            m_FilterResolver = resolver;
        }

        public TFilter GetData(TData o, string filterId)
        {
            return m_GetDataCallback(o, filterId);
        }

        public bool Resolve(TData data, string filterId, in QueryFilterOperator op, TFilter value)
        {
            if (!usesResolver)
                return false;
            return m_FilterResolver(data, filterId, op.token, value);
        }

        public override IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors)
        {
            if (usesResolver)
            {
                // ReSharper disable once ConvertToLocalFunction
                Func<TData, TFilter, bool> operation = (o, fv) => Resolve(o, data.filterName, in data.op, fv);
                var filterOperation = new FilterOperation<TData, TFilter, TFilter>(this, in data.op, data.filterValue, operation);
                var filterValue = ((ParseResult<TFilter>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
                return filterOperation;
            }
            else
                return data.generator.GenerateOperation(data, this, operatorIndex, errors);
        }
    }

    class RegexFilter<TData, TParam, TFilter> : BaseFilter<TFilter, TData>
    {
        public override bool usesRegularExpressionToken => true;
        public override bool usesParameter => true;
        public override Type parameterType => typeof(TParam);

        Func<TData, string, TParam, TFilter> m_GetDataCallback;
        Func<TData, string, TParam, string, TFilter, bool> m_FilterResolver;
        Func<string, TParam> m_ParameterTransformer;

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = (data, s, p) => default;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TParam, TFilter> getDataCallback, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TParam, TFilter> getDataCallback, StringComparison stringComparison, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, stringComparison, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TParam, TFilter> getDataCallback, Func<string, TParam> parameterTransformer, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
            m_ParameterTransformer = parameterTransformer;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TParam, TFilter> getDataCallback, Func<string, TParam> parameterTransformer, StringComparison stringComparison, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, false, stringComparison, queryEngine)
        {
            m_GetDataCallback = getDataCallback;
            m_ParameterTransformer = parameterTransformer;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TParam, string, TFilter, bool> resolver, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, true, queryEngine)
        {
            m_FilterResolver = resolver;
        }

        public RegexFilter(Regex token, IEnumerable<string> supportedOperatorType, Func<TData, string, TParam, string, TFilter, bool> resolver, Func<string, TParam> parameterTransformer, QueryEngineImpl<TData> queryEngine)
            : base(token, supportedOperatorType, true, queryEngine)
        {
            m_FilterResolver = resolver;
            m_ParameterTransformer = parameterTransformer;
        }

        public TFilter GetData(TData o, string filterId, TParam p)
        {
            return m_GetDataCallback(o, filterId, p);
        }

        public bool Resolve(TData data, string filterId, TParam param, in QueryFilterOperator op, TFilter value)
        {
            if (!usesResolver)
                return false;
            return m_FilterResolver(data, filterId, param, op.token, value);
        }

        public TParam TransformParameter(string param)
        {
            if (m_ParameterTransformer != null)
                return m_ParameterTransformer(param);

            return Utils.ConvertValue<TParam>(param);
        }

        public override IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors)
        {
            if (usesResolver)
            {
                // ReSharper disable once ConvertToLocalFunction
                Func<TData, TParam, TFilter, bool> operation = (o, param, fv) => Resolve(o, data.filterName, param, in data.op, fv);

                var filterOperation = new FilterOperation<TData, TParam, TFilter, TFilter>(this, in data.op, data.filterValue, data.paramValue, operation);
                var filterValue = ((ParseResult<TFilter>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
                return filterOperation;
            }
            else
                return data.generator.GenerateOperation(data, this, operatorIndex, errors);
        }
    }

    class DefaultFilter<TData> : Filter<TData, string>
    {
        public DefaultFilter(string token, Func<TData, string, string, string, bool> handler, QueryEngineImpl<TData> queryEngine)
            : base(token, null, (o, op, value) => handler(o, token, op, value), queryEngine)
        {}
    }

    class DefaultParamFilter<TData> : Filter<TData, string, string>
    {
        public DefaultParamFilter(string token, Func<TData, string, string, string, string, bool> handler, QueryEngineImpl<TData> queryEngine)
            : base(token, null, (o, param, op, value) => handler(o, token, param, op, value), queryEngine)
        {}
    }
}
