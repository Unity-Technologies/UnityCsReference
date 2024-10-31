// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    enum FilterOperatorType
    {
        Contains,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Lesser,
        LesserOrEqual,
        Custom
    }

    readonly struct FilterOperatorContext
    {
        public readonly IQueryEngineFilter filter;
        public readonly QueryFilterOperator op;
        public readonly StringView filterValue;
        public readonly StringView paramValue;

        public FilterOperatorContext(IQueryEngineFilter filter, QueryFilterOperator op, StringView filterValue, StringView paramValue)
        {
            this.filter = filter;
            this.op = op;
            this.filterValue = filterValue;
            this.paramValue = paramValue;
        }
    }

    interface IFilterHandlerDelegate
    {
        bool Invoke(FilterOperatorContext ctx, object ev, object fv, StringComparison sc);
    }

    class FilterHandlerDelegate<TLhs, TRhs> : IFilterHandlerDelegate
    {
        public Func<FilterOperatorContext, TLhs, TRhs, StringComparison, bool> handler { get; }

        public FilterHandlerDelegate(Func<FilterOperatorContext, TLhs, TRhs, StringComparison, bool> handler)
        {
            this.handler = handler;
        }

        public bool Invoke(FilterOperatorContext ctx, object ev, object fv, StringComparison sc)
        {
            return handler(ctx, (TLhs)ev, (TRhs)fv, sc);
        }
    }

    readonly struct FilterOperatorTypes : IEquatable<FilterOperatorTypes>
    {
        public readonly Type leftHandSideType;
        public readonly Type rightHandSideType;

        public FilterOperatorTypes(Type lhs, Type rhs)
        {
            leftHandSideType = lhs;
            rightHandSideType = rhs;
        }

        public bool Equals(FilterOperatorTypes other)
        {
            return leftHandSideType == other.leftHandSideType && rightHandSideType == other.rightHandSideType;
        }

        public override bool Equals(object obj)
        {
            return obj is FilterOperatorTypes other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((leftHandSideType != null ? leftHandSideType.GetHashCode() : 0) * 397) ^ (rightHandSideType != null ? rightHandSideType.GetHashCode() : 0);
            }
        }
    }

    /// <summary>
    /// A QueryFilterOperator defines a boolean operator between a value returned by a filter and an operand inputted in the search query.
    /// </summary>
    public readonly struct QueryFilterOperator
    {
        readonly IQueryEngineImplementation m_EngineImplementation;

        /// <summary>
        /// The operator identifier.
        /// </summary>
        public string token { get; }

        internal readonly FilterOperatorType type;

        internal Dictionary<Type, Dictionary<Type, IFilterHandlerDelegate>> handlers { get; }

        /// <summary>
        /// Indicates if this <see cref="QueryFilterOperator"/> is valid.
        /// </summary>
        public bool valid => !string.IsNullOrEmpty(token);

        internal static QueryFilterOperator invalid = new QueryFilterOperator(null, FilterOperatorType.Custom, null);

        internal QueryFilterOperator(string token, FilterOperatorType type, IQueryEngineImplementation engine)
        {
            this.token = token;
            this.type = type;
            handlers = new Dictionary<Type, Dictionary<Type, IFilterHandlerDelegate>>();
            m_EngineImplementation = engine;
        }

        /// <summary>
        /// Adds a custom filter operator handler.
        /// </summary>
        /// <typeparam name="TFilterVariable">The operator's left hand side type. This is the type returned by a filter handler.</typeparam>
        /// <typeparam name="TFilterConstant">The operator's right hand side type.</typeparam>
        /// <param name="handler">Callback to handle the operation. Takes a TFilterVariable (value returned by the filter handler, will vary for each element) and a TFilterConstant (right hand side value of the operator, which is constant), and returns a boolean indicating if the filter passes or not.</param>
        public QueryFilterOperator AddHandler<TFilterVariable, TFilterConstant>(Func<TFilterVariable, TFilterConstant, bool> handler)
        {
            return AddHandler((FilterOperatorContext _, TFilterVariable l, TFilterConstant r, StringComparison _) => handler(l, r));
        }

        /// <summary>
        /// Adds a custom filter operator handler.
        /// </summary>
        /// <typeparam name="TFilterVariable">The operator's left hand side type. This is the type returned by a filter handler.</typeparam>
        /// <typeparam name="TFilterConstant">The operator's right hand side type.</typeparam>
        /// <param name="handler">Callback to handle the operation. Takes a TFilterVariable (value returned by the filter handler, will vary for each element), a TFilterConstant (right hand side value of the operator, which is constant), a StringComparison option and returns a boolean indicating if the filter passes or not.</param>
        public QueryFilterOperator AddHandler<TFilterVariable, TFilterConstant>(Func<TFilterVariable, TFilterConstant, StringComparison, bool> handler)
        {
            return AddHandler((FilterOperatorContext _, TFilterVariable l, TFilterConstant r, StringComparison sc) => handler(l, r, sc));
        }

        internal QueryFilterOperator AddHandler<TLhs, TRhs>(Func<FilterOperatorContext, TLhs, TRhs, bool> handler)
        {
            return AddHandler((FilterOperatorContext ctx, TLhs l, TRhs r, StringComparison _) => handler(ctx, l, r));
        }

        internal QueryFilterOperator AddHandler<TLhs, TRhs>(Func<FilterOperatorContext, TLhs, TRhs, StringComparison, bool> handler)
        {
            if (!valid)
                return this;

            var leftHandSideType = typeof(TLhs);
            var rightHandSideType = typeof(TRhs);

            if (!handlers.ContainsKey(typeof(TLhs)))
                handlers.Add(typeof(TLhs), new Dictionary<Type, IFilterHandlerDelegate>());
            var filterHandlerDelegate = new FilterHandlerDelegate<TLhs, TRhs>(handler);

            var handlersByLeftHandSideType = handlers[leftHandSideType];
            handlersByLeftHandSideType[rightHandSideType] = filterHandlerDelegate;
            m_EngineImplementation.AddFilterOperationGenerator<TRhs>();
            // Enums are user defined but still simple enough to generate a parse function for them.
            if (typeof(TRhs).IsEnum)
            {
                m_EngineImplementation.AddDefaultEnumTypeParser<TRhs>();
            }
            return this;
        }

        internal Func<FilterOperatorContext, TLhs, TRhs, StringComparison, bool> GetHandler<TLhs, TRhs>()
        {
            if (!valid)
                return null;
            var lhsType = typeof(TLhs);
            var rhsType = typeof(TRhs);
            if (handlers.TryGetValue(lhsType, out var handlersByLeftHandSideType))
            {
                if (handlersByLeftHandSideType.TryGetValue(rhsType, out var filterHandlerDelegate))
                    return ((FilterHandlerDelegate<TLhs, TRhs>)filterHandlerDelegate).handler;
            }
            return null;
        }

        internal IFilterHandlerDelegate GetHandler(Type leftHandSideType, Type rightHandSideType)
        {
            if (!valid)
                return null;
            if (handlers.TryGetValue(leftHandSideType, out var handlersByLeftHandSideType))
            {
                if (handlersByLeftHandSideType.TryGetValue(rightHandSideType, out var filterHandlerDelegate))
                    return filterHandlerDelegate;
            }
            return null;
        }

        internal static FilterOperatorType GetType(string op)
        {
            if (string.Equals(op, ":", StringComparison.Ordinal))
                return FilterOperatorType.Contains;
            if (string.Equals(op, "=", StringComparison.Ordinal))
                return FilterOperatorType.Equal;
            if (string.Equals(op, "!=", StringComparison.Ordinal))
                return FilterOperatorType.NotEqual;
            if (string.Equals(op, "<", StringComparison.Ordinal))
                return FilterOperatorType.Lesser;
            if (string.Equals(op, "<=", StringComparison.Ordinal))
                return FilterOperatorType.LesserOrEqual;
            if (string.Equals(op, ">", StringComparison.Ordinal))
                return FilterOperatorType.Greater;
            if (string.Equals(op, ">=", StringComparison.Ordinal))
                return FilterOperatorType.GreaterOrEqual;
            return FilterOperatorType.Custom;
        }
    }
}
