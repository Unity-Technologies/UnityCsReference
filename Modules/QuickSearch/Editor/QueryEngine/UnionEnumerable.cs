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

        public bool fastYielding { get; }

        public UnionEnumerable(IEnumerable<T> first, IEnumerable<T> second, bool fastYielding)
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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return m_First.Union(m_Second).GetEnumerator();
#pragma warning restore RS0030
        }

        public IEnumerator<T> FastYieldingEnumerator()
        {
            var distinctFirstElements = new HashSet<T>();
            var distinctSecondElements = new HashSet<T>();
            foreach (var firstElement in m_First)
            {
                if (distinctFirstElements.Contains(firstElement))
                    yield return default;
                else
                {
                    distinctFirstElements.Add(firstElement);
                    yield return firstElement;
                }
            }

            foreach (var secondElement in m_Second)
            {
                if (distinctFirstElements.Contains(secondElement))
                {
                    yield return default;
                    continue;
                }

                if (distinctSecondElements.Contains(secondElement))
                {
                    yield return default;
                    continue;
                }

                distinctSecondElements.Add(secondElement);
                yield return secondElement;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [EnumerableCreator(QueryNodeType.Union)]
    class UnionEnumerableFactory : IQueryEnumerableFactory
    {
        public IQueryEnumerable<T> Create<T>(IQueryNode root, QueryEngine<T> engine, ICollection<QueryError> errors, bool fastYielding)
        {
            if (root.leaf || root.children == null || root.children.Count != 2)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "Union node must have two children."));
                return null;
            }

            var firstEnumerable = EnumerableCreator.Create(root.children[0], engine, errors, fastYielding);
            var secondEnumerable = EnumerableCreator.Create(root.children[1], engine, errors, fastYielding);
            if (firstEnumerable == null || secondEnumerable == null)
            {
                errors.Add(new QueryError(root.token.position, root.token.length, "Could not create enumerables from Union node's children."));
            }

            return new UnionEnumerable<T>(firstEnumerable, secondEnumerable, fastYielding);
        }
    }
}
