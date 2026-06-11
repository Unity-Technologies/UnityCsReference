// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    // This collection is designed to allocate ranges of unmanaged elements. The ranges are allocated within the
    // pool, within the excess pages, or as dedicated pages if their size is excessive. On reset, only the pool
    // is preserved: all excess pages and dedicated pages are pruned. Internally the backing storage is a pool of
    // bytes; each allocation is aligned to the natural alignment of T.
    class TempAllocator : IDisposable
    {
        static readonly MemoryLabel k_MemoryLabel = new (nameof(UIElements), "Renderer.TempAllocator");
        struct Page
        {
            public NativeArray<byte> array;
            public int used;
        }

        readonly int m_ExcessMinCapacity;
        readonly int m_ExcessMaxCapacity;

        Page m_Pool;
        List<Page> m_Excess;
        List<Page> m_Dedicated; // Always full
        int m_NextExcessSize;

        static class StaticSafetyIds<T> where T : struct
        {
            public static int id;
        }

        static void InitStaticSafetyId<T>(ref AtomicSafetyHandle handle) where T : struct
        {
            if (StaticSafetyIds<T>.id == 0)
                StaticSafetyIds<T>.id = AtomicSafetyHandle.NewStaticSafetyId<NativeSlice<T>>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, StaticSafetyIds<T>.id);
        }

        List<AtomicSafetyHandle> m_SafetyHandles = new();

        /// <param name="poolCapacity">Size of the persistent alloc</param>
        /// <param name="excessMinCapacity">Minimum size of the first excess page (actual size can be larger if the current alloc is larger than this minimum)</param>
        /// <param name="excessMaxCapacity">Maximum size of an excess page. Also defines the threshold from which dedicated pages are allocated for large allocs.</param>
        public TempAllocator(int poolCapacity, int excessMinCapacity, int excessMaxCapacity)
        {
            Debug.Assert(poolCapacity >= 1);
            Debug.Assert(excessMinCapacity >= 1);
            Debug.Assert(excessMinCapacity <= excessMaxCapacity);

            m_ExcessMinCapacity = excessMinCapacity;
            m_ExcessMaxCapacity = excessMaxCapacity;
            m_NextExcessSize = m_ExcessMinCapacity;

            m_Pool = new Page();
            m_Pool.array = new NativeArray<byte>(poolCapacity, k_MemoryLabel, NativeArrayOptions.UninitializedMemory);
            m_Excess = new List<Page>(8);
            m_Dedicated = new List<Page>(4);
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
                Reset();
                m_Pool.array.Dispose();
                m_Pool.used = 0;
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int AlignUp(int value, int alignmentPo2) => (value + alignmentPo2 - 1) & ~(alignmentPo2 - 1);

        public NativeSlice<T> Alloc<T>(int count) where T : unmanaged
        {
            Debug.Assert(!disposed);

            if (count <= 0)
                return new NativeSlice<T>();

            int byteCount = count * UnsafeUtility.SizeOf<T>();
            NativeSlice<T> slice;

            if (byteCount <= m_ExcessMaxCapacity)
            {
                int alignment = UnsafeUtility.AlignOf<T>();
                slice = DoSubAlloc(byteCount, alignment).SliceConvert<T>();
                var safety = AtomicSafetyHandle.Create();
                InitStaticSafetyId<T>(ref safety);
                NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, safety);
                m_SafetyHandles.Add(safety);
            }
            else
            {
                // No per-slice safety handle: the slice inherits the dedicated NativeArray's safety
                slice = AllocDedicated(byteCount).SliceConvert<T>();
            }

            return slice;
        }

        NativeSlice<byte> DoSubAlloc(int byteCount, int alignment)
        {
            Debug.Assert(!disposed);

            // Look at the pool first since its capacity is supposed to be sufficient most of the time
            int poolStart = AlignUp(m_Pool.used, alignment);
            int poolEnd = poolStart + byteCount;
            if (poolEnd <= m_Pool.array.Length)
            {
                NativeSlice<byte> slice = m_Pool.array.Slice(poolStart, byteCount);
                m_Pool.used = poolEnd;
                return slice;
            }

            // Reverse search
            for (int i = m_Excess.Count - 1; i >= 0; --i)
            {
                Page p = m_Excess[i];
                int start = AlignUp(p.used, alignment);
                int end = start + byteCount;
                if (end <= p.array.Length)
                {
                    NativeSlice<byte> slice = p.array.Slice(start, byteCount);
                    p.used = end;
                    m_Excess[i] = p;
                    return slice;
                }
            }

            // Create a new excess page
            {
                while (byteCount > m_NextExcessSize)
                    m_NextExcessSize <<= 1;

                var p = new Page
                {
                    array = new NativeArray<byte>(m_NextExcessSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                    used = byteCount
                };
                m_Excess.Add(p);
                m_NextExcessSize = Mathf.Min(m_NextExcessSize << 1, m_ExcessMaxCapacity);

                return p.array.Slice(0, byteCount);
            }
        }

        NativeSlice<byte> AllocDedicated(int byteCount)
        {
            var p = new Page
            {
                array = new NativeArray<byte>(byteCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
                used = byteCount
            };
            m_Dedicated.Add(p);
            return p.array.Slice(0, byteCount);
        }

        public void Reset()
        {
            ReleaseExcess();
            ReleaseDedicated();
            m_Pool.used = 0;
            m_NextExcessSize = m_ExcessMinCapacity;
            for (int i = 0; i < m_SafetyHandles.Count; ++i)
            {
                var safety = m_SafetyHandles[i];
                AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
                AtomicSafetyHandle.Release(safety);
            }

            m_SafetyHandles.Clear();
        }

        void ReleaseExcess()
        {
            foreach (Page p in m_Excess)
                p.array.Dispose();
            m_Excess.Clear();
        }

        void ReleaseDedicated()
        {
            foreach (Page p in m_Dedicated)
                p.array.Dispose();
            m_Dedicated.Clear();
        }

        public struct Statistics
        {
            public PageStatistics pool;
            public PageStatistics[] excess;
            public PageStatistics[] dedicated;
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
                excess = new PageStatistics[m_Excess.Count],
                dedicated = new PageStatistics[m_Dedicated.Count]
            };

            for (int i = 0; i < m_Excess.Count; ++i)
            {
                stats.excess[i] = new PageStatistics()
                {
                    size = m_Excess[i].array.Length,
                    used = m_Excess[i].used
                };
            }

            for (int i = 0; i < m_Dedicated.Count; ++i)
            {
                stats.dedicated[i] = new PageStatistics()
                {
                    size = m_Dedicated[i].array.Length,
                    used = m_Dedicated[i].used
                };
            }

            return stats;
        }
    }
}
