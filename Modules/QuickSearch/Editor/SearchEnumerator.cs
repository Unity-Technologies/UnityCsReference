// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_STACKED_ENUMERATOR_DISPOSING
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;


namespace UnityEditor.Search
{
    internal class SearchEnumerator<T> : IEnumerator<T>
    {
        private const int k_MaxStackDepth = 32;

        private readonly Stack<IEnumerator> m_ItemsEnumerator = new Stack<IEnumerator>();

        private static readonly bool k_IsNullable = default(T) == null;


        public int Count => m_ItemsEnumerator.Count;

        public Stack<IEnumerator> enumeratorStack => m_ItemsEnumerator;

        public SearchEnumerator()
        {}


        public SearchEnumerator(object itemEnumerator)
        {
            if (itemEnumerator == this)
                throw new ArgumentException($"SearchEnumerator cannot contain itself.", nameof(itemEnumerator));

            if (itemEnumerator is IEnumerable enumerable)
                m_ItemsEnumerator.Push(enumerable.GetEnumerator());
            else if (itemEnumerator is IEnumerator enumerator)
                m_ItemsEnumerator.Push(enumerator);
            else
                throw new ArgumentException($"Parameter {nameof(itemEnumerator)} is not an IEnumerable or IEnumerator.", nameof(itemEnumerator));
        }

        public bool NextItem(out T nextItem)
        {
            var advanced = MoveNext();
            nextItem = Current;
            return advanced;
        }

        private void ValidateStack()
        {
            Assert.IsFalse(m_ItemsEnumerator.Count > k_MaxStackDepth, "Possible stack overflow detected.");
        }

        public bool MoveNext()
        {
            while (true)
            {
                bool atEnd;
                Current = default;

                if (m_ItemsEnumerator.Count == 0)
                    return false;

                var currentIterator = m_ItemsEnumerator.Peek();
                if (currentIterator == null)
                    return false;

                atEnd = !currentIterator.MoveNext();
                if (atEnd)
                {
                    m_ItemsEnumerator.Pop();
                    continue;
                }


                // Test IEnumerable before IEnumerator
                if (currentIterator.Current is IEnumerable enumerable)
                {
                    m_ItemsEnumerator.Push(enumerable.GetEnumerator());
                    ValidateStack();
                    continue;
                }
                if (currentIterator.Current is IEnumerator enumerator)
                {
                    m_ItemsEnumerator.Push(enumerator);
                    ValidateStack();
                    continue;
                }

                // If we have a nullable type and the value is null, consider it
                // as a valid value.
                if (k_IsNullable && currentIterator.Current == null)
                {
                    return true;
                }

                if (currentIterator.Current is T current)
                    Current = current;
                else
                    throw new InvalidCastException($"Cannot cast \"{currentIterator.Current?.GetType()}\" to type {typeof(T)}.");

                return true;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public T Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {

            using (new RaceConditionDetector(this))
            {
                foreach (var enumerator in m_ItemsEnumerator)
                {
                    if (enumerator is IDisposable disposable)
                        disposable.Dispose();
                }
                m_ItemsEnumerator.Clear();
            }
        }
    }

    internal class SearchEnumerator : SearchEnumerator<object>
    {
        public SearchEnumerator() {}

        public SearchEnumerator(object itemEnumerator)
            : base(itemEnumerator)
        {}
    }
}
