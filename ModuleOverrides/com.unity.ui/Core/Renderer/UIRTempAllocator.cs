// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    // This collection is designed to allocate ranges of memory. The ranges are allocated within the pool,
    // within the excess pages, or as dedicated pages if their size is excessive. On reset, only the pool is
    // preserved: all excess pages and dedicated pages are pruned.
    class TempAllocator<T> : IDisposable where T : struct
    {
        struct Page
        {
            public NativeArray<T> array;
            public int used;
        }

        readonly int m_ExcessMinCapacity;
        readonly int m_ExcessMaxCapacity;

        Page m_Pool;
        List<Page> m_Excess;
        int m_NextExcessSize;

        public TempAllocator(int poolCapacity, int excessMinCapacity, int excessMaxCapacity)
        {
            Debug.Assert(poolCapacity >= 1);
            Debug.Assert(excessMinCapacity >= 1);
            Debug.Assert(excessMinCapacity <= excessMaxCapacity);

            m_ExcessMinCapacity = excessMinCapacity;
            m_ExcessMaxCapacity = excessMaxCapacity;
            m_NextExcessSize = m_ExcessMinCapacity;

            m_Pool = new Page();
            m_Pool.array = new NativeArray<T>(poolCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Excess = new List<Page>(8);
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
                ReleaseExcess();
                m_Pool.array.Dispose();
                m_Pool.used = 0;
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public NativeSlice<T> Alloc(int count)
        {
            Debug.Assert(!disposed);

            // Look at the pool first since its capacity is supposed to be sufficient most of the time
            int nextCount = m_Pool.used + count;
            if (nextCount <= m_Pool.array.Length)
            {
                NativeSlice<T> slice = m_Pool.array.Slice(m_Pool.used, count);
                m_Pool.used = nextCount;
                return slice;
            }

            // Very large allocs get a dedicated page
            if (count > m_ExcessMaxCapacity)
            {
                var p = new Page
                {
                    array = new NativeArray<T>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                    used = count
                };
                m_Excess.Add(p);
                return p.array.Slice(0, count);
            }

            // Reverse search
            for (int i = m_Excess.Count - 1; i >= 0; --i)
            {
                Page p = m_Excess[i];
                nextCount = p.used + count;
                if (nextCount <= p.array.Length)
                {
                    NativeSlice<T> slice = p.array.Slice(p.used, count);
                    p.used = nextCount;
                    m_Excess[i] = p;
                    return slice;
                }
            }

            // Create a new excess page
            {
                while (count > m_NextExcessSize)
                    m_NextExcessSize <<= 1;

                var p = new Page
                {
                    array = new NativeArray<T>(m_NextExcessSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                    used = count
                };
                m_Excess.Add(p);
                m_NextExcessSize = Mathf.Min(m_NextExcessSize << 1, m_ExcessMaxCapacity);

                return p.array.Slice(0, count);
            }
        }

        public void Reset()
        {
            ReleaseExcess();
            m_Pool.used = 0;
            m_NextExcessSize = m_ExcessMinCapacity;
        }

        void ReleaseExcess()
        {
            foreach (Page p in m_Excess)
                p.array.Dispose();
            m_Excess.Clear();
        }

        public struct Statistics
        {
            public PageStatistics pool;
            public PageStatistics[] excess;
        }

        public struct PageStatistics
        {
            public int size;
            public int used;
        }

        public Statistics GatherStatistics()
        {
            var stats = new Statistics
            {
                pool = new PageStatistics
                {
                    size = m_Pool.array.Length,
                    used = m_Pool.used
                },
                excess = new PageStatistics[m_Excess.Count]
            };

            for (int i = 0; i < m_Excess.Count; ++i)
            {
                stats.excess[i] = new PageStatistics()
                {
                    size = m_Excess[i].array.Length,
                    used = m_Excess[i].used
                };
            }

            return stats;
        }
    }
}
