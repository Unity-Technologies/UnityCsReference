// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    class IntersectionEnumerable<T> : IQueryEnumerable<T>
    {
        IEnumerable<T> m_First;
        IEnumerable<T> m_Second;

        public IntersectionEnumerable(IEnumerable<T> first, IEnumerable<T> second)
        {
            m_First = first;
            m_Second = second;
        }

        public void SetPayload(IEnumerable<T> payload)
        {}

        public IEnumerator<T> GetEnumerator()
        {
            return m_First.Intersect(m_Second).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.Intersection)]
    class IntersectionEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors)
        {
            if (root.leaf || root.children == null || root.children.Count != 2)
            {
                errors.Add(new QueryError(root.token.position, "Intersection node must have two children."));
                return null;
            }

            var firstEnumerable = EnumerableCreator.Create(root.children[0], engine, errors);
            var secondEnumerable = EnumerableCreator.Create(root.children[1], engine, errors);
            if (firstEnumerable == null || secondEnumerable == null)
            {
                errors.Add(new QueryError(root.token.position, "Could not create enumerables from Intersection node's children."));
            }

            return new IntersectionEnumerable<T>(firstEnumerable, secondEnumerable);
        }
    }
}
