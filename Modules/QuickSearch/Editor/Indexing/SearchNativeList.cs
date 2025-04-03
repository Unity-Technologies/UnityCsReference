// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_SEARCHNATIVELIST_DISPOSE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEditor.Search
{
    // This class is missing many safety checks to keep it as lightweight as possible.
    // Safety checks are also already done in the NativeArray itself.
    class SearchNativeList<T> : IList<T>, IReadOnlyList<T>, IDisposable where T : struct
    {
        NativeArray<T> m_BackingArray;

        public NativeArray<T> BackingArray => m_BackingArray;
        public int Capacity => m_BackingArray.Length;
        public int Count { get; private set; }
        public bool IsReadOnly => false;

        public Allocator Allocator => m_BackingArray.m_AllocatorLabel;

        // This accessor is not bound checked, and won't affect Count
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BackingArray[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_BackingArray[index] = value;
        }

        public SearchNativeList(int capacity, Allocator allocator)
        {
            m_BackingArray = new NativeArray<T>(capacity, allocator);
            Count = 0;
        }

        public NativeArray<T>.ReadOnly ToReadOnly()
        {
            var subArray = m_BackingArray.GetSubArray(0, Count);
            return subArray.AsReadOnly();
        }

        public void Dispose()
        {
            if (m_BackingArray.IsCreated)
                m_BackingArray.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            GrowIfNeeded(Count + 1);
            m_BackingArray[Count] = item;
            ++Count;
        }

        public void AddRange(IReadOnlyList<T> items)
        {
            GrowIfNeeded(Count + items.Count);
            for (var i = 0; i < items.Count; ++i)
            {
                m_BackingArray[Count] = items[i];
                ++Count;
            }
        }

        public void Clear()
        {
            Count = 0;
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int BinarySearch(in T item, IComparer<T> comparer)
        {
            if (Count <= 0)
                return ~0;
            return m_BackingArray.AsReadOnlySpan().Slice(0, Count).BinarySearch(item, comparer);
        }

        public void Sort(IComparer<T> comparer)
        {
            var span = m_BackingArray.AsSpan().Slice(0, Count);
            QuickSort(span, 0, span.Length - 1, comparer);
        }

        void GrowIfNeeded(int newCount)
        {
            if (newCount > Capacity)
                Grow(newCount * 2);
        }

        void Grow(int newCapacity)
        {
            if (newCapacity <= m_BackingArray.Length)
                return;

            var newBackingArray = new NativeArray<T>(newCapacity, m_BackingArray.m_AllocatorLabel);
            NativeArray<T>.Copy(m_BackingArray, newBackingArray, m_BackingArray.Length);
            m_BackingArray.Dispose();
            m_BackingArray = newBackingArray;
        }

        static void QuickSort(Span<T> input, int start, int end, IComparer<T> comparer)
        {
            var stack = new Stack<(int start, int end)>();
            stack.Push((start, end));

            while (stack.Count > 0)
            {
                var (innerStart, innerEnd) = stack.Pop();
                if (innerStart < innerEnd)
                {
                    var pivot = Partition(input, innerStart, innerEnd, comparer);

                    // Push in reverse order so that (start, pivot-1) gets picked up first
                    stack.Push((pivot+1, innerEnd));
                    stack.Push((innerStart, pivot-1));
                }
            }
        }

        static int Partition(Span<T> input, int start, int end, IComparer<T> comparer)
        {
            var pivot = input[end];
            var pIndex = start;

            for (var i = start; i < end; i++)
            {
                var c = comparer.Compare(input[i], pivot);
                if (c <= 0)
                {
                    (input[i], input[pIndex]) = (input[pIndex], input[i]);
                    pIndex++;
                }
            }

            (input[pIndex], input[end]) = (input[end], input[pIndex]);
            return pIndex;
        }
    }
}
