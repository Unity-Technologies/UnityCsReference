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
        NativeArray<T> m_CurrentPage;
        int m_CurrentPageCount;

        public NativePagedList(int poolCapacity)
        {
            Debug.Assert(poolCapacity > 0);
            k_PoolCapacity = Mathf.NextPowerOfTwo(poolCapacity);
        }

        public void Add(ref T data)
        {
            // Add to the current page if there is still room
            if (m_CurrentPageCount < m_CurrentPage.Length)
            {
                m_CurrentPage[m_CurrentPageCount++] = data;
                return;
            }

            int newPageSize = m_Pages.Count > 0 ? m_CurrentPage.Length << 1 : k_PoolCapacity;
            m_CurrentPage = new NativeArray<T>(newPageSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Pages.Add(m_CurrentPage);

            m_CurrentPage[0] = data;
            m_CurrentPageCount = 1;
        }

        public void Add(T data) { Add(ref data); }

        List<NativeSlice<T>> m_Enumerator = new List<NativeSlice<T>>(8);
        public List<NativeSlice<T>> GetPages()
        {
            m_Enumerator.Clear();

            if (m_Pages.Count > 0)
            {
                int last = m_Pages.Count - 1;
                for(int i = 0 ; i < last ; ++i)
                    m_Enumerator.Add(m_Pages[i]);

                // Last page might not be full
                if(m_CurrentPageCount > 0)
                    m_Enumerator.Add(m_CurrentPage.Slice(0, m_CurrentPageCount));
            }

            return m_Enumerator;
        }

        public void Reset()
        {
            if (m_Pages.Count > 1)
            {
                // Keep first page alive
                m_CurrentPage = m_Pages[0];

                // Trim excess
                for (int i = 1; i < m_Pages.Count; ++i)
                    m_Pages[i].Dispose();
                m_Pages.Clear();
                m_Pages.Add(m_CurrentPage);
            }

            m_CurrentPageCount = 0;
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
                m_CurrentPageCount = 0;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
