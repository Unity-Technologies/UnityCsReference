// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Collections;

namespace UnityEditor.Search
{
    // SearchIndexEntry enumerator that takes multiple pre-sorted
    // list of SearchIndexEntry and enumerates the combined sorted result.
    // Based on the MinHeap algorithm from https://www.geeksforgeeks.org/merge-k-sorted-arrays/
    class SearchIndexEntryHeapEnumerator : IEnumerator<SearchIndexEntry>
    {
        class MinHeapNode
        {
            public SearchIndexEntry element; // The element to be stored

            // Index of the array from
            // which the element is taken.
            public int arrayIndex;

            // Index of the next element
            // to be picked from the array.
            public int nextElementIndex;

            public MinHeapNode(SearchIndexEntry element, int arrayIndex, int nextElementIndex)
            {
                this.element = element;
                this.arrayIndex = arrayIndex;
                this.nextElementIndex = nextElementIndex;
            }
        };

        MinHeapNode[] m_Heap; // Array of elements in heap
        int m_HeapSize; // Current number of elements in min heap
        IReadOnlyList<IReadOnlyList<SearchIndexEntry>> m_Enumerables;
        SearchIndexEntry m_Current;
        int m_TotalResults;
        int m_CurrentResultCount;
        IComparer<SearchIndexEntry> m_Comparer;

        // All enumerables must be non empty
        public SearchIndexEntryHeapEnumerator(IReadOnlyList<IReadOnlyList<SearchIndexEntry>> enumerables, IComparer<SearchIndexEntry> comparer)
        {
            m_Enumerables = enumerables;
            m_Comparer = comparer;
            Init();
        }

        void Init()
        {
            MinHeapNode[] hArr = new MinHeapNode[m_Enumerables.Count];
            int resultSize = 0;
            for (int i = 0; i < m_Enumerables.Count; i++)
            {
                MinHeapNode node = new MinHeapNode(m_Enumerables[i][0], i, 1);
                hArr[i] = node;
                resultSize += m_Enumerables[i].Count;
            }

            m_TotalResults = resultSize;
            m_CurrentResultCount = 0;
            CreateHeap(hArr, m_Enumerables.Count);
        }

        void CreateHeap(MinHeapNode[] a, int size)
        {
            m_HeapSize = size;
            m_Heap = a;
            int i = (m_HeapSize - 1) / 2;
            while (i >= 0)
            {
                MinHeapify(i);
                i--;
            }
        }

        MinHeapNode GetRoot()
        {
            if (m_HeapSize <= 0)
            {
                throw new InvalidOperationException("SearchIndexer MinHeap sort underflow.");
            }
            return m_Heap[0];
        }

        // A recursive method to heapify a subtree
        // with the root at given index. This method
        // assumes that the subtrees are already heapified.
        void MinHeapify(int i)
        {
            int l = LeftIndex(i);
            int r = RightIndex(i);
            int smallest = i;

            if (l < m_HeapSize && m_Comparer.Compare(m_Heap[l].element, m_Heap[i].element) < 0)
                smallest = l;

            if (r < m_HeapSize && m_Comparer.Compare(m_Heap[r].element, m_Heap[smallest].element) < 0)
                smallest = r;

            if (smallest != i)
            {
                Swap(m_Heap, i, smallest);
                MinHeapify(smallest);
            }
        }

        // Get the index of the left child of the node at index i.
        static int LeftIndex(int i) { return (2 * i + 1); }

        // Get the index of the right child of the node at index i.
        static int RightIndex(int i) { return (2 * i + 2); }

        // Replace the root with a new node and heapify the new root.
        void ReplaceRoot(MinHeapNode root)
        {
            m_Heap[0] = root;
            MinHeapify(0);
        }

        // A utility function to swap two min heap nodes
        static void Swap(MinHeapNode[] heap, int i, int j)
        {
            MinHeapNode temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }

        public bool MoveNext()
        {
            if (m_CurrentResultCount >= m_TotalResults)
                return false;

            m_CurrentResultCount++;

            MinHeapNode root = GetRoot();
            m_Current = root.element;

            // Find the next element that will
            // replace current root of heap.
            // The next element belongs to same
            // array as the current root.
            if (root.nextElementIndex < m_Enumerables[root.arrayIndex].Count)
                root.element = m_Enumerables[root.arrayIndex][root.nextElementIndex++];

            // If root was the last element of its array
            else
                root.element = SearchIndexEntry.MaxEntry;

            // Replace root with next element of array
            ReplaceRoot(root);

            return true;
        }

        public void Reset()
        {
            Init();
        }

        public SearchIndexEntry Current => m_Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        { }
    }
}
