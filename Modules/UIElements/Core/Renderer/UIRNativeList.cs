// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    internal class NativeList<T> : IDisposable where T : struct
    {
        struct DeferredArray
        {
            public NativeArray<T> array;
            public int framesRemaining;
        }

        readonly MemoryLabel m_MemoryLabel;
        readonly int m_MaxQueuedFrameCount;
        NativeArray<T> m_NativeArray;
        int m_Count;
        List<DeferredArray> m_DeferredArrays;

        public NativeList(int initialCapacity, MemoryLabel allocLabel, int maxQueuedFrameCount = 0)
        {
            Debug.Assert(initialCapacity > 0);
            m_MemoryLabel = allocLabel;
            m_MaxQueuedFrameCount = maxQueuedFrameCount;
            m_NativeArray = new NativeArray<T>(initialCapacity, allocLabel, NativeArrayOptions.UninitializedMemory);
            m_DeferredArrays = maxQueuedFrameCount > 0 ? new List<DeferredArray>() : null;
        }

        public NativeList(int initialCapacity, MemoryLabel allocLabel, Allocator allocator, int maxQueuedFrameCount = 0)
        {
            Debug.Assert(initialCapacity > 0);
            m_MemoryLabel = allocLabel;
            m_MaxQueuedFrameCount = maxQueuedFrameCount;
            m_NativeArray = new NativeArray<T>(initialCapacity, allocator, NativeArrayOptions.UninitializedMemory);
            m_DeferredArrays = maxQueuedFrameCount > 0 ? new List<DeferredArray>() : null;
        }

        void Expand(int newLength)
        {
            var newArray = new NativeArray<T>(newLength, m_MemoryLabel, NativeArrayOptions.UninitializedMemory);
            if (m_Count > 0)
            {
                var dst = newArray.Slice(0, m_Count);
                dst.CopyFrom(m_NativeArray);
            }

            // Defer disposal if needed, otherwise dispose immediately
            if (m_MaxQueuedFrameCount > 0 && m_DeferredArrays != null)
            {
                m_DeferredArrays.Add(new DeferredArray
                {
                    array = m_NativeArray,
                    framesRemaining = m_MaxQueuedFrameCount
                });
            }
            else
            {
                m_NativeArray.Dispose();
            }

            m_NativeArray = newArray;
        }

        public void Add(ref T data)
        {
            if (m_Count == m_NativeArray.Length)
                Expand(m_NativeArray.Length << 1);

            m_NativeArray[m_Count++] = data;
        }

        public void Add(NativeSlice<T> src)
        {
            int required = m_Count + src.Length;
            if (m_NativeArray.Length < required)
                Expand(required << 1);

            var dst = m_NativeArray.Slice(m_Count, src.Length);
            dst.CopyFrom(src);

            m_Count += src.Length;
        }

        public void Clear()
        {
            m_Count = 0;
        }

        public NativeSlice<T> GetSlice(int start, int length)
        {
            return m_NativeArray.Slice(start, length);
        }

        public NativeArray<T> GetBuffer()
        {
            return m_NativeArray;
        }

        public int Count => m_Count;

        public void AdvanceFrame()
        {
            if (m_DeferredArrays == null || m_DeferredArrays.Count == 0)
                return;

            for (int i = m_DeferredArrays.Count - 1; i >= 0; --i)
            {
                var deferred = m_DeferredArrays[i];
                --deferred.framesRemaining;

                if (deferred.framesRemaining <= 0)
                {
                    // Safe to dispose now - render thread is done with this buffer
                    if (deferred.array.IsCreated)
                        deferred.array.Dispose();
                    m_DeferredArrays.RemoveAt(i);
                }
                else
                {
                    // Update the counter
                    m_DeferredArrays[i] = deferred;
                }
            }
        }

        // For testing
        internal int GetDeferredArrayCount()
        {
            return m_DeferredArrays?.Count ?? 0;
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
                m_NativeArray.Dispose();

                // Dispose all deferred arrays immediately on disposal
                if (m_DeferredArrays != null)
                {
                    foreach (var deferred in m_DeferredArrays)
                    {
                        if (deferred.array.IsCreated)
                            deferred.array.Dispose();
                    }
                    m_DeferredArrays.Clear();
                }
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
