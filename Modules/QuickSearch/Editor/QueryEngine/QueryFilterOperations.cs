// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    internal interface IFilterOperation
    {
        string filterName { get; }
        string filterValueString { get; }
        string filterParams { get; }
        IFilter filter { get; }
        QueryFilterOperator queryFilterOperator { get; }
        string ToString();
    }

    internal struct FilterOperationGeneratorData
    {
        public QueryFilterOperator op;
        public string filterName;
        public string filterValue;
        public string paramValue;
        public IParseResult filterValueParseResult;
        public StringComparison globalStringComparison;
        public IFilterOperationGenerator generator;
    }

    internal interface IFilterOperationGenerator
    {
        IFilterOperation GenerateOperation<TData, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors);
        IFilterOperation GenerateOperation<TData, TFilterLhs>(FilterOperationGeneratorData data, RegexFilter<TData, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors);
        IFilterOperation GenerateOperation<TData, TParam, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TParam, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors);
        IFilterOperation GenerateOperation<TData, TParam, TFilterLhs>(FilterOperationGeneratorData data, RegexFilter<TData, TParam, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors);
    }

    internal class FilterOperationGenerator<TFilterRhs> : IFilterOperationGenerator
    {
        public IFilterOperation GenerateOperation<TData, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors)
        {
            var stringComparisonOptions = filter.overridesStringComparison ? filter.stringComparison : data.globalStringComparison;
            var filterOperatorContext = new FilterOperatorContext(filter);
            Func<TData, TFilterRhs, bool> operation = (o, filterValue) => false;

            var handlerFound = false;
            var typedHandler = data.op.GetHandler<TFilterLhs, TFilterRhs>();
            if (typedHandler != null)
            {
                operation = (o, filterValue) => typedHandler(filterOperatorContext, filter.GetData(o), filterValue, stringComparisonOptions);
                handlerFound = true;
            }

            if (!handlerFound)
            {
                var genericHandler = data.op.GetHandler<object, object>();
                if (genericHandler != null)
                {
                    operation = (o, filterValue) => genericHandler(filterOperatorContext, filter.GetData(o), filterValue, stringComparisonOptions);
                    handlerFound = true;
                }
            }

            if (!handlerFound)
            {
                var error = $"No handler of type ({typeof(TFilterLhs)}, {typeof(TFilterRhs)}) or (object, object) found for operator {data.op.token}";
                errors.Add(new QueryError(operatorIndex, data.op.token.Length, error));
                return null;
            }

            var filterOperation = new FilterOperation<TData, TFilterLhs, TFilterRhs>(filter, in data.op, data.filterValue, operation);
            if (data.filterValueParseResult != null)
            {
                var filterValue = ((ParseResult<TFilterRhs>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
            }

            return filterOperation;
        }

        public IFilterOperation GenerateOperation<TData, TFilterLhs>(FilterOperationGeneratorData data, RegexFilter<TData, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors)
        {
            var stringComparisonOptions = filter.overridesStringComparison ? filter.stringComparison : data.globalStringComparison;
            var filterOperatorContext = new FilterOperatorContext(filter);
            Func<TData, TFilterRhs, bool> operation = (o, filterValue) => false;

            var handlerFound = false;
            var typedHandler = data.op.GetHandler<TFilterLhs, TFilterRhs>();
            if (typedHandler != null)
            {
                operation = (o, filterValue) => typedHandler(filterOperatorContext, filter.GetData(o, data.filterName), filterValue, stringComparisonOptions);
                handlerFound = true;
            }

            if (!handlerFound)
            {
                var genericHandler = data.op.GetHandler<object, object>();
                if (genericHandler != null)
                {
                    operation = (o, filterValue) => genericHandler(filterOperatorContext, filter.GetData(o, data.filterName), filterValue, stringComparisonOptions);
                    handlerFound = true;
                }
            }

            if (!handlerFound)
            {
                var error = $"No handler of type ({typeof(TFilterLhs)}, {typeof(TFilterRhs)}) or (object, object) found for operator {data.op.token}";
                errors.Add(new QueryError(operatorIndex, data.op.token.Length, error));
                return null;
            }

            var filterOperation = new FilterOperation<TData, TFilterLhs, TFilterRhs>(filter, in data.op, data.filterValue, operation);
            if (data.filterValueParseResult != null)
            {
                var filterValue = ((ParseResult<TFilterRhs>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
            }

            return filterOperation;
        }

        public IFilterOperation GenerateOperation<TData, TParam, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TParam, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors)
        {
            Func<TData, TParam, TFilterRhs, bool> operation = (o, p, fv) => false;
            var stringComparisonOptions = filter.overridesStringComparison ? filter.stringComparison : data.globalStringComparison;
            var filterOperatorContext = new FilterOperatorContext(filter);

            var handlerFound = false;
            var typedHandler = data.op.GetHandler<TFilterLhs, TFilterRhs>();
            if (typedHandler != null)
            {
                operation = (o, p, filterValue) => typedHandler(filterOperatorContext, filter.GetData(o, p), filterValue, stringComparisonOptions);
                handlerFound = true;
            }

            if (!handlerFound)
            {
                var genericHandler = data.op.GetHandler<object, object>();
                if (genericHandler != null)
                {
                    operation = (o, p, filterValue) => genericHandler(filterOperatorContext, filter.GetData(o, p), filterValue, stringComparisonOptions);
                    handlerFound = true;
                }
            }

            if (!handlerFound)
            {
                var error = $"No handler of type ({typeof(TFilterLhs)}, {typeof(TFilterRhs)}) or (object, object) found for operator {data.op.token}";
                errors.Add(new QueryError(operatorIndex, data.op.token.Length, error));
                return null;
            }

            var filterOperation = new FilterOperation<TData, TParam, TFilterLhs, TFilterRhs>(filter, in data.op, data.filterValue, data.paramValue, operation);
            if (data.filterValueParseResult != null)
            {
                var filterValue = ((ParseResult<TFilterRhs>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
            }

            return filterOperation;
        }

        public IFilterOperation GenerateOperation<TData, TParam, TFilterLhs>(FilterOperationGeneratorData data, RegexFilter<TData, TParam, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors)
        {
            Func<TData, TParam, TFilterRhs, bool> operation = (o, p, fv) => false;
            var stringComparisonOptions = filter.overridesStringComparison ? filter.stringComparison : data.globalStringComparison;
            var filterOperatorContext = new FilterOperatorContext(filter);

            var handlerFound = false;
            var typedHandler = data.op.GetHandler<TFilterLhs, TFilterRhs>();
            if (typedHandler != null)
            {
                operation = (o, p, filterValue) => typedHandler(filterOperatorContext, filter.GetData(o, data.filterName, p), filterValue, stringComparisonOptions);
                handlerFound = true;
            }

            if (!handlerFound)
            {
                var genericHandler = data.op.GetHandler<object, object>();
                if (genericHandler != null)
                {
                    operation = (o, p, filterValue) => genericHandler(filterOperatorContext, filter.GetData(o, data.filterName, p), filterValue, stringComparisonOptions);
                    handlerFound = true;
                }
            }

            if (!handlerFound)
            {
                var error = $"No handler of type ({typeof(TFilterLhs)}, {typeof(TFilterRhs)}) or (object, object) found for operator {data.op.token}";
                errors.Add(new QueryError(operatorIndex, data.op.token.Length, error));
                return null;
            }

            var filterOperation = new FilterOperation<TData, TParam, TFilterLhs, TFilterRhs>(filter, in data.op, data.filterValue, data.paramValue, operation);
            if (data.filterValueParseResult != null)
            {
                var filterValue = ((ParseResult<TFilterRhs>)data.filterValueParseResult).parsedValue;
                filterOperation.SetFilterValue(filterValue);
            }

            return filterOperation;
        }
    }

    internal abstract class BaseFilterOperation<TData> : IFilterOperation
    {
        public string filterName => filter.token;
        public string filterValueString { get; }
        public virtual string filterParams => null;
        public IFilter filter { get; }
        public QueryFilterOperator queryFilterOperator { get; }

        protected BaseFilterOperation(IFilter filter, in QueryFilterOperator queryFilterOperator, string filterValue)
        {
            this.filter = filter;
            this.queryFilterOperator = queryFilterOperator;
            this.filterValueString = filterValue;
        }

        public abstract bool Match(TData obj);

        public new virtual string ToString()
        {
            return $"{filterName}{queryFilterOperator.token}{filterValueString}";
        }
    }

    interface IDynamicFilterOperation<in TFilterRhs>
    {
        void SetFilterValue(TFilterRhs value);
    }

    internal class FilterOperation<TData, TFilterLhs, TFilterRhs> : BaseFilterOperation<TData>, IDynamicFilterOperation<TFilterRhs>
    {
        TFilterRhs m_FilterValue;
        Func<TData, TFilterRhs, bool> m_Operation;

        public FilterOperation(Filter<TData, TFilterLhs> filter, in QueryFilterOperator queryFilterOperator, string filterValue, Func<TData, TFilterRhs, bool> operation)
            : base(filter, in queryFilterOperator, filterValue)
        {
            this.m_Operation = operation;
        }

        public FilterOperation(RegexFilter<TData, TFilterLhs> filter, in QueryFilterOperator queryFilterOperator, string filterValue, Func<TData, TFilterRhs, bool> operation)
            : base(filter, in queryFilterOperator, filterValue)
        {
            this.m_Operation = operation;
        }

        public override bool Match(TData obj)
        {
            try
            {
                return m_Operation(obj, m_FilterValue);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}: Failed to execute {queryFilterOperator} handler with {obj} and {filterValueString}\r\n{ex.StackTrace}");
            }
        }

        public void SetFilterValue(TFilterRhs value)
        {
            m_FilterValue = value;
        }
    }

    internal class FilterOperation<TData, TParam, TFilterLhs, TFilterRhs> : BaseFilterOperation<TData>, IDynamicFilterOperation<TFilterRhs>
    {
        string m_ParamValue;
        TFilterRhs m_FilterValue;
        Func<TData, TParam, TFilterRhs, bool> m_Operation;
        TParam m_Param;

        public override string filterParams => m_ParamValue;

        public FilterOperation(Filter<TData, TParam, TFilterLhs> filter, in QueryFilterOperator queryFilterOperator, string filterValue, Func<TData, TParam, TFilterRhs, bool> operation)
            : base(filter, in queryFilterOperator, filterValue)
        {
            this.m_Operation = operation;
            m_ParamValue = null;
            m_Param = default;
        }

        public FilterOperation(RegexFilter<TData, TParam, TFilterLhs> filter, in QueryFilterOperator queryFilterOperator, string filterValue, Func<TData, TParam, TFilterRhs, bool> operation)
            : base(filter, in queryFilterOperator, filterValue)
        {
            this.m_Operation = operation;
            m_ParamValue = null;
            m_Param = default;
        }

        public FilterOperation(Filter<TData, TParam, TFilterLhs> filter, in QueryFilterOperator queryFilterOperator, string filterValue, string paramValue, Func<TData, TParam, TFilterRhs, bool> operation)
            : base(filter, in queryFilterOperator, filterValue)
        {
            this.m_Operation = operation;
            m_ParamValue = paramValue;
            m_Param = string.IsNullOrEmpty(paramValue) ? default : filter.TransformParameter(paramValue);
        }

        public FilterOperation(RegexFilter<TData, TParam, TFilterLhs> filter, in QueryFilterOperator queryFilterOperator, string filterValue, string paramValue, Func<TData, TParam, TFilterRhs, bool> operation)
            : base(filter, in queryFilterOperator, filterValue)
        {
            this.m_Operation = operation;
            m_ParamValue = paramValue;
            m_Param = string.IsNullOrEmpty(paramValue) ? default : filter.TransformParameter(paramValue);
        }

        public override bool Match(TData obj)
        {
            try
            {
                return m_Operation(obj, m_Param, m_FilterValue);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}: Failed to execute {queryFilterOperator} handler with {obj} and {filterValueString}\r\n{ex.StackTrace}");
            }
        }

        public override string ToString()
        {
            var paramString = string.IsNullOrEmpty(m_ParamValue) ? "" : $"({m_ParamValue})";
            return $"{filterName}{paramString}{queryFilterOperator.token}{filterValueString}";
        }

        public void SetFilterValue(TFilterRhs value)
        {
            m_FilterValue = value;
        }
    }
}
