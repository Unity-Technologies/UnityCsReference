// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    class NestedQueryEnumerable<T> : IQueryEnumerable<T>
    {
        IEnumerable<T> m_NestedQueryEnumerable;

        public bool fastYielding { get; }

        public NestedQueryEnumerable(IEnumerable<T> nestedQueryEnumerable, bool fastYielding)
        {
            m_NestedQueryEnumerable = nestedQueryEnumerable;
            this.fastYielding = fastYielding;
        }

        public void SetPayload(IEnumerable<T> payload)
        {}

        public IEnumerator<T> GetEnumerator()
        {
            return m_NestedQueryEnumerable.GetEnumerator();
        }

        public IEnumerator<T> FastYieldingEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.NestedQuery)]
    class NestedQueryEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            var nestedQueryNode = root as NestedQueryNode;
            var nestedQueryHandler = nestedQueryNode?.nestedQueryHandler as NestedQueryHandler<T>;
            if (nestedQueryHandler == null)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "There is no handler set for nested queries."));
                return null;
            }

            var nestedEnumerable = nestedQueryHandler.handler(nestedQueryNode.identifier, nestedQueryNode.associatedFilter);
            if (nestedEnumerable == null)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "Could not create enumerable from nested query handler."));
            }
            return new NestedQueryEnumerable<T>(nestedEnumerable, fastYielding);
        }
    }
}
