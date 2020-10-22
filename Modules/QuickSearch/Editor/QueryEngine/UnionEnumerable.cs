// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    class UnionEnumerable<T> : IQueryEnumerable<T>
    {
        IEnumerable<T> m_First;
        IEnumerable<T> m_Second;

        public UnionEnumerable(IEnumerable<T> first, IEnumerable<T> second)
        {
            m_First = first;
            m_Second = second;
        }

        public void SetPayload(IEnumerable<T> payload)
        {}

        public IEnumerator<T> GetEnumerator()
        {
            return m_First.Union(m_Second).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.Union)]
    class UnionEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors)
        {
            if (root.leaf || root.children == null || root.children.Count != 2)
            {
                errors.Add(new QueryError(root.token.position, "Union node must have two children."));
                return null;
            }

            var firstEnumerable = EnumerableCreator.Create(root.children[0], engine, errors);
            var secondEnumerable = EnumerableCreator.Create(root.children[1], engine, errors);
            if (firstEnumerable == null || secondEnumerable == null)
            {
                errors.Add(new QueryError(root.token.position, "Could not create enumerables from Union node's children."));
            }

            return new UnionEnumerable<T>(firstEnumerable, secondEnumerable);
        }
    }
}
