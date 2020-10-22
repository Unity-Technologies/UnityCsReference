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
        FilterOperator filterOperator { get; }
        string ToString();
    }

    internal struct FilterOperationGeneratorData
    {
        public FilterOperator op;
        public string filterValue;
        public string paramValue;
        public IParseResult filterValueParseResult;
        public StringComparison globalStringComparison;
        public IFilterOperationGenerator generator;
    }

    internal interface IFilterOperationGenerator
    {
        IFilterOperation GenerateOperation<TData, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors);
        IFilterOperation GenerateOperation<TData, TParam, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TParam, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors);
    }

    internal class FilterOperationGenerator<TFilterRhs> : IFilterOperationGenerator
    {
        public IFilterOperation GenerateOperation<TData, TFilterLhs>(FilterOperationGeneratorData data, Filter<TData, TFilterLhs> filter, int operatorIndex, ICollection<QueryError> errors)
        {
            var stringComparisonOptions = filter.overrideStringComparison ? filter.stringComparison : data.globalStringComparison;
            Func<TData, TFilterRhs, bool> operation = (o, filterValue) => false;

            var handlerFound = false;
            var typedHandler = data.op.GetHandler<TFilterLhs, TFilterRhs>();
            if (typedHandler != null)
            {
                operation = (o, filterValue) => typedHandler(filter.GetData(o), filterValue, stringComparisonOptions);
                handlerFound = true;
            }

            if (!handlerFound)
            {
                var genericHandler = data.op.GetHandler<object, object>();
                if (genericHandler != null)
                {
                    operation = (o, filterValue) => genericHandler(filter.GetData(o), filterValue, stringComparisonOptions);
                    handlerFound = true;
                }
            }

            if (!handlerFound)
            {
                var error = $"No handler of type ({typeof(TFilterLhs)}, {typeof(TFilterRhs)}) or (object, object) found for operator {data.op.token}";
                errors.Add(new QueryError(operatorIndex, data.op.token.Length, error));
                return null;
            }

            var filterOperation = new FilterOperation<TData, TFilterLhs, TFilterRhs>(filter, data.op, data.filterValue, operation);
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
            var stringComparisonOptions = filter.overrideStringComparison ? filter.stringComparison : data.globalStringComparison;

            var handlerFound = false;
            var typedHandler = data.op.GetHandler<TFilterLhs, TFilterRhs>();
            if (typedHandler != null)
            {
                operation = (o, p, filterValue) => typedHandler(filter.GetData(o, p), filterValue, stringComparisonOptions);
                handlerFound = true;
            }

            if (!handlerFound)
            {
                var genericHandler = data.op.GetHandler<object, object>();
                if (genericHandler != null)
                {
                    operation = (o, p, filterValue) => genericHandler(filter.GetData(o, p), filterValue, stringComparisonOptions);
                    handlerFound = true;
                }
            }

            if (!handlerFound)
            {
                var error = $"No handler of type ({typeof(TFilterLhs)}, {typeof(TFilterRhs)}) or (object, object) found for operator {data.op.token}";
                errors.Add(new QueryError(operatorIndex, data.op.token.Length, error));
                return null;
            }

            var filterOperation = new FilterOperation<TData, TParam, TFilterLhs, TFilterRhs>(filter, data.op, data.filterValue, data.paramValue, operation);
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
        public FilterOperator filterOperator { get; }

        protected BaseFilterOperation(IFilter filter, FilterOperator filterOperator, string filterValue)
        {
            this.filter = filter;
            this.filterOperator = filterOperator;
            this.filterValueString = filterValue;
        }

        public abstract bool Match(TData obj);

        public new virtual string ToString()
        {
            return $"{filterName}{filterOperator.token}{filterValueString}";
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

        public FilterOperation(Filter<TData, TFilterLhs> filter, FilterOperator filterOperator, string filterValue, Func<TData, TFilterRhs, bool> operation)
            : base(filter, filterOperator, filterValue)
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
                throw new Exception($"{ex.Message}: Failed to execute {filterOperator} handler with {obj} and {filterValueString}\r\n{ex.StackTrace}");
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

        public FilterOperation(Filter<TData, TParam, TFilterLhs> filter, FilterOperator filterOperator, string filterValue, Func<TData, TParam, TFilterRhs, bool> operation)
            : base(filter, filterOperator, filterValue)
        {
            this.m_Operation = operation;
            m_ParamValue = null;
            m_Param = default;
        }

        public FilterOperation(Filter<TData, TParam, TFilterLhs> filter, FilterOperator filterOperator, string filterValue, string paramValue, Func<TData, TParam, TFilterRhs, bool> operation)
            : base(filter, filterOperator, filterValue)
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
                throw new Exception($"{ex.Message}: Failed to execute {filterOperator} handler with {obj} and {filterValueString}\r\n{ex.StackTrace}");
            }
        }

        public override string ToString()
        {
            var paramString = string.IsNullOrEmpty(m_ParamValue) ? "" : $"({m_ParamValue})";
            return $"{filterName}{paramString}{filterOperator.token}{filterValueString}";
        }

        public void SetFilterValue(TFilterRhs value)
        {
            m_FilterValue = value;
        }
    }
}
