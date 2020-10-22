// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEditor.Search
{
    internal interface IFilterHandlerDelegate
    {
        bool Invoke(object ev, object fv, StringComparison sc);
    }

    internal class FilterHandlerDelegate<TLhs, TRhs> : IFilterHandlerDelegate
    {
        public Func<TLhs, TRhs, StringComparison, bool> handler { get; }

        public FilterHandlerDelegate(Func<TLhs, TRhs, StringComparison, bool> handler)
        {
            this.handler = handler;
        }

        public bool Invoke(object ev, object fv, StringComparison sc)
        {
            return handler((TLhs)ev, (TRhs)fv, sc);
        }
    }

    internal readonly struct FilterOperatorTypes : IEquatable<FilterOperatorTypes>
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

    internal class FilterOperator
    {
        private IQueryEngineImplementation m_EngineImplementation;

        public string token { get; }
        public Dictionary<Type, Dictionary<Type, IFilterHandlerDelegate>> handlers { get; }

        public FilterOperator(string token, IQueryEngineImplementation engine)
        {
            this.token = token;
            handlers = new Dictionary<Type, Dictionary<Type, IFilterHandlerDelegate>>();
            m_EngineImplementation = engine;
        }

        public FilterOperator AddHandler<TLhs, TRhs>(Func<TLhs, TRhs, bool> handler)
        {
            return AddHandler((TLhs l, TRhs r, StringComparison sc) => handler(l, r));
        }

        public FilterOperator AddHandler<TLhs, TRhs>(Func<TLhs, TRhs, StringComparison, bool> handler)
        {
            var leftHandSideType = typeof(TLhs);
            var rightHandSideType = typeof(TRhs);

            if (!handlers.ContainsKey(typeof(TLhs)))
                handlers.Add(typeof(TLhs), new Dictionary<Type, IFilterHandlerDelegate>());
            var filterHandlerDelegate = new FilterHandlerDelegate<TLhs, TRhs>(handler);

            var handlersByLeftHandSideType = handlers[leftHandSideType];
            if (handlersByLeftHandSideType.ContainsKey(rightHandSideType))
                handlersByLeftHandSideType[rightHandSideType] = filterHandlerDelegate;
            else
                handlersByLeftHandSideType.Add(rightHandSideType, filterHandlerDelegate);
            m_EngineImplementation.AddFilterOperationGenerator<TRhs>();
            return this;
        }

        public Func<TLhs, TRhs, StringComparison, bool> GetHandler<TLhs, TRhs>()
        {
            var lhsType = typeof(TLhs);
            var rhsType = typeof(TRhs);
            if (handlers.TryGetValue(lhsType, out var handlersByLeftHandSideType))
            {
                if (handlersByLeftHandSideType.TryGetValue(rhsType, out var filterHandlerDelegate))
                    return ((FilterHandlerDelegate<TLhs, TRhs>)filterHandlerDelegate).handler;
            }
            return null;
        }

        public IFilterHandlerDelegate GetHandler(Type leftHandSideType, Type rightHandSideType)
        {
            if (handlers.TryGetValue(leftHandSideType, out var handlersByLeftHandSideType))
            {
                if (handlersByLeftHandSideType.TryGetValue(rightHandSideType, out var filterHandlerDelegate))
                    return filterHandlerDelegate;
            }
            return null;
        }
    }
}
