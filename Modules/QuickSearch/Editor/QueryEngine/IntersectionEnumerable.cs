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

        public bool fastYielding { get; }

        public IntersectionEnumerable(IEnumerable<T> first, IEnumerable<T> second, bool fastYielding)
        {
            m_First = first;
            m_Second = second;
            this.fastYielding = fastYielding;
        }

        public void SetPayload(IEnumerable<T> payload)
        {}

        public IEnumerator<T> GetEnumerator()
        {
            if (fastYielding)
                return FastYieldingEnumerator();
            return m_First.Intersect(m_Second).GetEnumerator();
        }

        public IEnumerator<T> FastYieldingEnumerator()
        {
            var distinctFirstElements = new HashSet<T>();
            var distinctSecondElements = new HashSet<T>(m_Second);
            foreach (var firstElement in m_First)
            {
                if (distinctFirstElements.Contains(firstElement))
                {
                    yield return default;
                    continue;
                }

                distinctFirstElements.Add(firstElement);
                if (distinctSecondElements.Contains(firstElement))
                    yield return firstElement;
                else
                    yield return default;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.Intersection)]
    class IntersectionEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYield)
        {
            if (root.leaf || root.children == null || root.children.Count != 2)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "Intersection node must have two children."));
                return null;
            }

            var firstEnumerable = EnumerableCreator.Create(root.children[0], engine, errors, fastYield);
            var secondEnumerable = EnumerableCreator.Create(root.children[1], engine, errors, fastYield);
            if (firstEnumerable == null || secondEnumerable == null)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "Could not create enumerables from Intersection node's children."));
            }

            return new IntersectionEnumerable<T>(firstEnumerable, secondEnumerable, fastYield);
        }
    }
}
