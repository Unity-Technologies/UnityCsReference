// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace Unity.GraphToolkit
{
    /// <summary>
    /// A read-only view over a <see cref="HashSet{T}"/>. Exposes the set as an
    /// <see cref="IReadOnlyCollection{T}"/> without letting callers mutate it by downcasting, the way
    /// <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}"/> does for lists.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the set.</typeparam>
    class ReadOnlyHashSet<T> : IReadOnlyCollection<T>
    {
        readonly HashSet<T> m_Set;

        public ReadOnlyHashSet(HashSet<T> set)
        {
            m_Set = set;
        }

        public int Count => m_Set.Count;

        public bool Contains(T item) => m_Set.Contains(item);

        // Returns the struct enumerator to keep foreach allocation-free.
        public HashSet<T>.Enumerator GetEnumerator() => m_Set.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => m_Set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Set.GetEnumerator();
    }
}
