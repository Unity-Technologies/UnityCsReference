// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    class AggregateEnumerable<T> : IQueryEnumerable<T>
    {
        IEnumerable<T> m_Enumerable;
        Func<IEnumerable<T>, IEnumerable<T>> m_Aggregator;

        public AggregateEnumerable(IEnumerable<T> enumerable, Func<IEnumerable<T>, IEnumerable<T>> aggregator)
        {
            m_Enumerable = enumerable;
            m_Aggregator = aggregator;
        }

        public void SetPayload(IEnumerable<T> payload)
        {}

        public IEnumerator<T> GetEnumerator()
        {
            return m_Aggregator(m_Enumerable).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.Aggregator)]
    class AggregateEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors)
        {
            if (root.leaf || root.children == null || root.children.Count != 1)
            {
                errors.Add(new QueryError(root.token.position, "Aggregator node must have a child."));
                return null;
            }

            var aggregatorNode = root as AggregatorNode;
            if (!(aggregatorNode?.aggregator is NestedQueryAggregator<T> handler))
            {
                errors.Add(new QueryError(root.token.position, "Aggregator node does not have the proper aggregator set."));
                return null;
            }
            var childEnumerable = EnumerableCreator.Create(root.children[0], engine, errors);
            return new AggregateEnumerable<T>(childEnumerable, handler.aggregator);
        }
    }
}
