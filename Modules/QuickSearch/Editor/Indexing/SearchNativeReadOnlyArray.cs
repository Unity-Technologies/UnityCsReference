// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEditor.Search
{
    // This class is missing many safety checks to keep it as lightweight as possible.
    // Safety checks are also already done in the NativeArray itself.
    struct SearchNativeReadOnlyArray<T> : IReadOnlyList<T>, IDisposable where T : struct
    {
        NativeArray<T> m_BackingArray;

        public NativeArray<T> BackingArray => m_BackingArray;
        public int Count => m_BackingArray.Length;
        public Allocator Allocator => m_BackingArray.m_AllocatorLabel;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BackingArray[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_BackingArray[index] = value;
        }

        public SearchNativeReadOnlyArray(int count, Allocator allocator)
        {
            m_BackingArray = new NativeArray<T>(count, allocator); ;
        }

        public SearchNativeReadOnlyArray(NativeArray<T>.ReadOnly array, Allocator allocator)
        {
            m_BackingArray = new NativeArray<T>(array.Length, allocator);
            NativeArray<T>.Copy(array, m_BackingArray, array.Length);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_BackingArray.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (m_BackingArray.IsCreated)
                m_BackingArray.Dispose();
        }

        public Span<T> AsSpan()
        {
            return m_BackingArray.AsSpan();
        }

        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return m_BackingArray.AsReadOnlySpan();
        }
    }
}
