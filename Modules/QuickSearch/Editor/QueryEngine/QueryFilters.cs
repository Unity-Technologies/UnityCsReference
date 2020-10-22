// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEditor.Search
{
    internal interface IFilter
    {
        string token { get; }
        IEnumerable<string> supportedFilters { get; }
        Type type { get; }
        bool SupportsValue(string value);
        bool paramFilter { get; }
        Type paramType { get; }
        bool resolver { get; }
        StringComparison stringComparison { get; }
        bool overrideStringComparison { get; }
        IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors);

        INestedQueryHandlerTransformer queryHandlerTransformer { get; }
        void SetNestedQueryTransformer<TNestedQueryData, TRhs>(Func<TNestedQueryData, TRhs> transformer);
    }

    internal abstract class BaseFilter<TFilter> : IFilter
    {
        public string token { get; protected set; }
        public IEnumerable<string> supportedFilters { get; protected set; }
        public Type type => typeof(TFilter);
        public virtual bool paramFilter => false;
        public virtual Type paramType => typeof(object);

        public bool resolver { get; protected set; }

        public StringComparison stringComparison { get; }
        public bool overrideStringComparison { get; }

        public INestedQueryHandlerTransformer queryHandlerTransformer { get; private set; }

        protected BaseFilter(string token, IEnumerable<string> supportedOperatorTypes, bool resolver)
        {
            this.token = token;
            supportedFilters = supportedOperatorTypes ?? new string[] {};
            this.resolver = resolver;
            overrideStringComparison = false;
        }

        protected BaseFilter(string token, IEnumerable<string> supportedOperatorTypes, bool resolver, StringComparison stringComparison)
        {
            this.token = token;
            supportedFilters = supportedOperatorTypes ?? new string[] {};
            this.resolver = resolver;
            this.stringComparison = stringComparison;
            overrideStringComparison = true;
        }

        public bool SupportsValue(string value)
        {
            var converter = TypeDescriptor.GetConverter(type);
            return converter.IsValid(value);
        }

        public abstract IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors);

        public void SetNestedQueryTransformer<TNestedQueryData, TRhs>(Func<TNestedQueryData, TRhs> transformer)
        {
            if (transformer == null)
                return;
            queryHandlerTransformer = new NestedQueryHandlerTransformer<TNestedQueryData, TRhs>(transformer);
        }
    }

    internal class Filter<TData, TFilter> : BaseFilter<TFilter>
    {
        private Func<TData, TFilter> m_GetDataCallback;
        private Func<TData, string, TFilter, bool> m_FilterResolver;

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TFilter> getDataCallback)
            : base(token, supportedOperatorType, false)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TFilter> getDataCallback, StringComparison stringComparison)
            : base(token, supportedOperatorType, false, stringComparison)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, string, TFilter, bool> resolver)
            : base(token, supportedOperatorType, true)
        {
            m_FilterResolver = resolver;
        }

        public TFilter GetData(TData o)
        {
            return m_GetDataCallback(o);
        }

        public bool Resolve(TData data, FilterOperator op, TFilter value)
        {
            if (!resolver)
                return false;
            return m_FilterResolver(data, op.token, value);
        }

        public override IFilterOperation GenerateOperation(FilterOperationGeneratorData data, int operatorIndex, ICollection<QueryError> errors)
        {
            if (resolver)
            {
                // ReSharper disable once ConvertToLocalFunction
                Func<TData, TFilter, bool> operation = (o, fv) => Resolve(o, data.op, fv);
                var filterOperation = new FilterOperation<TData, TFilter, TFilter>(this, data.op, data.filterValue, operation);
                var filterValue = ((ParseResult<TFilter>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
                return filterOperation;
            }
            else
                return data.generator.GenerateOperation(data, this, operatorIndex, errors);
        }
    }

    internal class Filter<TData, TParam, TFilter> : BaseFilter<TFilter>
    {
        public override bool paramFilter => true;
        public override Type paramType => typeof(TParam);

        private Func<TData, TParam, TFilter> m_GetDataCallback;
        private Func<TData, TParam, string, TFilter, bool> m_FilterResolver;
        private Func<string, TParam> m_ParameterTransformer;

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback)
            : base(token, supportedOperatorType, false)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, StringComparison stringComparison)
            : base(token, supportedOperatorType, false, stringComparison)
        {
            m_GetDataCallback = getDataCallback;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, Func<string, TParam> parameterTransformer)
            : base(token, supportedOperatorType, false)
        {
            m_GetDataCallback = getDataCallback;
            m_ParameterTransformer = parameterTransformer;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, TFilter> getDataCallback, Func<string, TParam> parameterTransformer, StringComparison stringComparison)
            : base(token, supportedOperatorType, false, stringComparison)
        {
            m_GetDataCallback = getDataCallback;
            m_ParameterTransformer = parameterTransformer;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, string, TFilter, bool> resolver)
            : base(token, supportedOperatorType, true)
        {
            m_FilterResolver = resolver;
        }

        public Filter(string token, IEnumerable<string> supportedOperatorType, Func<TData, TParam, string, TFilter, bool> resolver, Func<string, TParam> parameterTransformer)
            : base(token, supportedOperatorType, true)
        {
            m_FilterResolver = resolver;
            m_ParameterTransformer = parameterTransformer;
        }

        public TFilter GetData(TData o, TParam p)
        {
            return m_GetDataCallback(o, p);
        }

        public bool Resolve(TData data, TParam param, FilterOperator op, TFilter value)
        {
            if (!resolver)
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
            if (resolver)
            {
                // ReSharper disable once ConvertToLocalFunction
                Func<TData, TParam, TFilter, bool> operation = (o, param, fv) => Resolve(o, param, data.op, fv);

                var filterOperation = new FilterOperation<TData, TParam, TFilter, TFilter>(this, data.op, data.filterValue, data.paramValue, operation);
                var filterValue = ((ParseResult<TFilter>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
                return filterOperation;
            }
            else
                return data.generator.GenerateOperation(data, this, operatorIndex, errors);
        }
    }

    internal class DefaultFilter<TData> : Filter<TData, string>
    {
        public DefaultFilter(string token, Func<TData, string, string, string, bool> handler)
            : base(token, null, (o, op, value) => handler(o, token, op, value))
        {}
    }

    internal class DefaultParamFilter<TData> : Filter<TData, string, string>
    {
        public DefaultParamFilter(string token, Func<TData, string, string, string, string, bool> handler)
            : base(token, null, (o, param, op, value) => handler(o, token, param, op, value))
        {}
    }
}
