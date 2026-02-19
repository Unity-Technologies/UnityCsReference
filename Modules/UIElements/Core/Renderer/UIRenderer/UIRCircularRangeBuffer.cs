// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    // This class implements a circular buffer that can allocate and free variable sized ranges. Ranges are
    // always allocated contiguously, and freed in the same order (although frees can be split or merged).
    //
    // Internally, the buffer is made of pages that grow by a factor of 2x when the current page doesn't have
    // enough room to satisfy an allocation request. Previous pages are kept until all their data has been freed.
    // This ensures that no copies are needed when growing the buffer.
    //
    // When wrapping occurs and there isn't enough contiguous space at the end for an allocation, elements at
    // the end are "skipped" (wasted). These skipped elements remain unusable until the elements allocated
    // before that region have been freed.
    class CircularRangeBuffer<T> : IDisposable where T : unmanaged
    {
        public static class Testing
        {
            public static int GetAllocHead(CircularRangeBuffer<T> buffer) => buffer.m_AllocHead;
            public static int GetFreeHead(CircularRangeBuffer<T> buffer) => buffer.m_FreeHead;
            public static int GetCount(CircularRangeBuffer<T> buffer) => buffer.m_Count;
            public static int GetWasted(CircularRangeBuffer<T> buffer) => buffer.m_Wasted;
            public static int GetCapacity(CircularRangeBuffer<T> buffer) => buffer.m_Capacity;
            public static int GetPreviousPageCount(CircularRangeBuffer<T> buffer) => buffer.m_PreviousPages.Count;
            public static NativeArray<T> GetCurrentPage(CircularRangeBuffer<T> buffer) => buffer.m_CurrentPage;
            public static bool GetDisposed(CircularRangeBuffer<T> buffer) => buffer.disposed;
        }

        class PageInfo
        {
            public NativeArray<T> array;
            public int count;
        }

        Queue<PageInfo> m_PreviousPages = new();

        NativeArray<T> m_CurrentPage;

        // Information for the current page only
        int m_AllocHead; // Next index to allocate at
        int m_FreeHead;  // Next index to free at
        int m_Count;     // Number of allocated elements (excludes wasted)
        int m_Wasted;    // Number of skipped elements at the end when wrapping
        int m_Capacity;

        public CircularRangeBuffer(int capacity)
        {
            capacity = Mathf.Max(capacity, 1);
            capacity = Mathf.NextPowerOfTwo(capacity);
            CreateNewPage(capacity);
        }

        void CreateNewPage(int capacity)
        {
            m_CurrentPage = new NativeArray<T>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_AllocHead = 0;
            m_FreeHead = 0;
            m_Count = 0;
            m_Wasted = 0;
            m_Capacity = capacity;
        }

        NativeSlice<T> AllocateFromNewPage(int count)
        {
            // Move current page to previous pages so it can eventually be disposed.
            m_PreviousPages.Enqueue(new PageInfo { array = m_CurrentPage, count = m_Count });

            // Determine the required capacity of the next page
            int capacity = m_CurrentPage.Length << 1; // Double

            // If still not enough to support 'count' elements, round up to next power of two
            if (count > capacity)
                capacity = Mathf.NextPowerOfTwo(count);

            CreateNewPage(capacity);

            // Allocate within the new page
            m_AllocHead = count;
            m_Count = count;

            return m_CurrentPage.Slice(0, count);
        }

        public NativeSlice<T> Allocate(int count)
        {
            Debug.Assert(!disposed && count >= 0);

            if (count == 0)
                return new NativeSlice<T>();

            int available = m_Capacity - m_Count - m_Wasted;
            if (count < available)
            {
                // There is enough room but maybe not contiguous

                // Main Case: enough room ahead without wrapping
                int nextAllocHead = m_AllocHead + count;
                if (nextAllocHead <= m_Capacity)
                {
                    NativeSlice<T> slice = m_CurrentPage.Slice(m_AllocHead, count);
                    m_AllocHead = nextAllocHead == m_Capacity ? 0 : nextAllocHead;
                    m_Count += count;
                    return slice;
                }

                // Edge Case: enough room with wrapping (alloc head is ahead of free head)
                if (m_AllocHead >= m_FreeHead && count <= m_FreeHead)
                {
                    m_Wasted = m_Capacity - m_AllocHead;
                    m_AllocHead = count;
                    m_Count += count;
                    return m_CurrentPage.Slice(0, count);
                }
            }

            // Rare Case: not enough room, allocate a new page
            return AllocateFromNewPage(count);
        }

        public void Free(int count)
        {
            Debug.Assert(!disposed && count >= 0);

            // Deallocate from previous pages first
            while (m_PreviousPages.Count > 0 && count > 0)
            {
                PageInfo page = m_PreviousPages.Peek();
                if (page.count <= count)
                {
                    // We can consume the entire page. Dispose and dequeue.
                    count -= page.count;
                    m_PreviousPages.Dequeue();
                    page.array.Dispose();
                }
                else
                {
                    // The page has more than enough to satisfy the deallocation.
                    page.count -= count;
                    return;
                }
            }

            if (count == 0)
                return;

            Debug.Assert(count <= m_Count);

            int actualCapacity = m_Capacity - m_Wasted;
            int nextFreeHead = m_FreeHead + count;

            if (nextFreeHead <= actualCapacity)
            {
                // Main Case: no wrapping
                m_FreeHead = nextFreeHead == actualCapacity ? 0 : nextFreeHead;
                m_Count -= count;

                // If we crossed the wasted boundary, reclaim it
                if (m_Wasted > 0 && nextFreeHead == actualCapacity)
                    m_Wasted = 0;
            }
            else
            {
                // Edge case: wrapping, which also implies dealing with the wasted
                m_FreeHead = nextFreeHead - actualCapacity;
                m_Count -= count;
                m_Wasted = 0;
            }

            if (m_Count == 0)
            {
                // Buffer is now empty, reset the state
                m_AllocHead = 0;
                m_FreeHead = 0;
                m_Wasted = 0;
            }
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (PageInfo page in m_PreviousPages)
                    page.array.Dispose();
                m_CurrentPage.Dispose();

                m_CurrentPage = new NativeArray<T>();
                m_Capacity = 0;
                m_PreviousPages.Clear();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion
    }
}
