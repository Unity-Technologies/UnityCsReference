// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    class NativeList<T> : IDisposable where T : struct
    {
        NativeArray<T> m_NativeArray;
        int m_Count;

        public NativeList(int initialCapacity)
        {
            Debug.Assert(initialCapacity > 0);
            m_NativeArray = new NativeArray<T>(initialCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        void Expand(int newLength)
        {
            var newArray = new NativeArray<T>(newLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            var dst = newArray.Slice(0, m_Count);
            dst.CopyFrom(m_NativeArray);
            m_NativeArray.Dispose();
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

        public int Count => m_Count;

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
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
