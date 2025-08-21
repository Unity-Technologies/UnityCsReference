// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    // Features of this collection:
    // a) An amount of pooling can be specified
    // b) Excess is trimmed on reset
    // c) No copies are involved when pages are created
    // d) Inner pages are exposed as NativeSlices
    // e) Pages grow by a factor of 2x
    // f) Elements can be added by ref or value
    class NativePagedList<T> : IDisposable where T : struct
    {
        readonly int k_PoolCapacity;

        List<NativeArray<T>> m_Pages = new List<NativeArray<T>>(8);
        NativeArray<T> m_LastPage;
        int m_CountInLastPage;
        readonly NativeArrayAllocator m_FirstPageAllocator;
        readonly NativeArrayAllocator m_OtherPagesAllocator;

        public NativePagedList(int poolCapacity, string profilerName, Allocator firstPageAllocator = Allocator.Persistent, Allocator otherPagesAllocator = Allocator.Persistent)
        {
            Debug.Assert(poolCapacity > 0);
            k_PoolCapacity = Mathf.NextPowerOfTwo(poolCapacity);
            m_FirstPageAllocator = new NativeArrayAllocator(profilerName, firstPageAllocator);
            m_OtherPagesAllocator = new NativeArrayAllocator(profilerName, otherPagesAllocator);
        }

        public void Add(ref T data)
        {
            // Add to the last page if there is still room
            if (m_CountInLastPage < m_LastPage.Length)
            {
                m_LastPage[m_CountInLastPage++] = data;
                return;
            }

            int newPageSize = m_Pages.Count > 0 ? m_LastPage.Length << 1 : k_PoolCapacity;
            NativeArrayAllocator allocator = m_Pages.Count == 0 ? m_FirstPageAllocator : m_OtherPagesAllocator;
            m_LastPage = allocator.CreateArray(newPageSize, NativeArrayOptions.UninitializedMemory);
            m_Pages.Add(m_LastPage);

            m_LastPage[0] = data;
            m_CountInLastPage = 1;
        }

        public void Add(T data) { Add(ref data); }

        // TODO: Implement page enumerator instead
        List<NativeSlice<T>> m_Enumerator = new List<NativeSlice<T>>(8);

        // Note: This code is not thread safe, using a page enumerator as in the TODO
        // should solve the multi-threading issue.
        public List<NativeSlice<T>> GetPages()
        {
            m_Enumerator.Clear();

            if (m_Pages.Count > 0)
            {
                int last = m_Pages.Count - 1;
                for (int i = 0; i < last; ++i)
                    m_Enumerator.Add(m_Pages[i]);

                // Last page might not be full
                if (m_CountInLastPage > 0)
                    m_Enumerator.Add(m_LastPage.Slice(0, m_CountInLastPage));
            }

            return m_Enumerator;
        }

        // This return the number of element added and not the current capacity of the list
        public int GetCount()
        {
            int count = m_CountInLastPage;
            for (int i = 0; i < m_Pages.Count - 1; ++i)
                count += m_Pages[i].Length;
            return count;
        }

        public void Reset()
        {
            if (m_Pages.Count > 1)
            {
                // Keep first page alive
                m_LastPage = m_Pages[0];

                // Trim excess
                for (int i = 1; i < m_Pages.Count; ++i)
                    m_Pages[i].Dispose();
                m_Pages.Clear();
                m_Pages.Add(m_LastPage);
            }

            m_CountInLastPage = 0;
        }

        // TempAllocator does not require labels, this struct helps to use the correct way to build a NativeArray depending on the allocator used.
        struct NativeArrayAllocator
        {
            Allocator m_Allocator;
            MemoryLabel m_MemoryLabel;

            public NativeArrayAllocator(string profilerName, Allocator allocator)
            {
                if (MemoryLabel.SupportsAllocator(allocator))
                {
                    m_Allocator = default;
                    m_MemoryLabel = new MemoryLabel(nameof(UIElements), profilerName, allocator);
                }
                else
                {
                    m_Allocator = allocator;
                    m_MemoryLabel = default;
                }
            }
            public NativeArray<T> CreateArray(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            {
                if (m_MemoryLabel.IsCreated)
                    return new NativeArray<T>(length, m_MemoryLabel, options);
                else
                    return new NativeArray<T>(length, m_Allocator, options);
            }
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                for (int i = 0; i < m_Pages.Count; ++i)
                    m_Pages[i].Dispose();
                m_Pages.Clear();
                m_CountInLastPage = 0;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        // Note: The related NativePagedList cannot be modified while the enumerator is being used. I will not correctly update its internal state to correctly
        // keep track of new added elements or reset list.
        public struct Enumerator
        {
            NativePagedList<T> m_NativePagedList;
            NativeArray<T> m_CurrentPage;

            int m_IndexInCurrentPage = 0;
            int m_IndexOfCurrentPage = 0;
            int m_CountInCurrentPage = 0;

            public Enumerator(NativePagedList<T> nativePagedList, int offset)
            {
                m_NativePagedList = nativePagedList;

                // This loop does NOT process the last page
                for (int i = 0; i < m_NativePagedList.m_Pages.Count - 1; i++)
                {
                    m_CountInCurrentPage = m_NativePagedList.m_Pages[i].Length;

                    if (offset >= m_CountInCurrentPage)
                    {
                        offset -= m_CountInCurrentPage;
                    }
                    else
                    {
                        m_IndexInCurrentPage = offset;
                        m_IndexOfCurrentPage = i;
                        m_CurrentPage = m_NativePagedList.m_Pages[m_IndexOfCurrentPage];
                        return;
                    }
                }

                // Process for the last page
                m_IndexOfCurrentPage = m_NativePagedList.m_Pages.Count - 1;
                m_CountInCurrentPage = m_NativePagedList.m_CountInLastPage;
                m_IndexInCurrentPage = offset;
                m_CurrentPage = m_NativePagedList.m_LastPage;
            }

            public bool HasNext()
            {
                return m_IndexInCurrentPage < m_CountInCurrentPage;
            }

            public T GetNext()
            {
                if (!HasNext())
                {
                    throw new InvalidOperationException("No more elements");
                }

                T result = m_CurrentPage[m_IndexInCurrentPage];
                ++m_IndexInCurrentPage;

                // Select next
                if (m_IndexInCurrentPage == m_CountInCurrentPage)
                {
                    m_IndexInCurrentPage = 0;
                    ++m_IndexOfCurrentPage;
                    int pageCount = m_NativePagedList.m_Pages.Count;

                    if (m_IndexOfCurrentPage < pageCount)
                    {
                        if (m_IndexOfCurrentPage < pageCount - 1)
                            m_CountInCurrentPage = m_NativePagedList.m_Pages[m_IndexOfCurrentPage].Length;
                        else
                            m_CountInCurrentPage = m_NativePagedList.m_CountInLastPage;
                    }
                    else
                    {
                        // Stay at the end of the paged list to return false on HasNext()
                        m_IndexOfCurrentPage = pageCount - 1;
                        m_CountInCurrentPage = m_NativePagedList.m_CountInLastPage;
                        m_IndexInCurrentPage = m_CountInCurrentPage;
                    }

                    m_CurrentPage = m_NativePagedList.m_Pages[m_IndexOfCurrentPage];
                }
                return result;
            }
        }
    }
}
