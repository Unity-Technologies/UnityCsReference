// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace UnityEngine.UIElements.Unmanaged;

// TODO replace with NativeList when Collections land in trunk
struct UnmanagedTempList<T>(int capacity) : IDisposable
    where T : unmanaged
{
    NativeArray<T> m_NativeArray = new(capacity, Allocator.Temp);
    int m_Count;

    public void Add(T item)
    {
        if (m_Count == m_NativeArray.Length)
        {
            var newArray = new NativeArray<T>(m_NativeArray.Length * 2, Allocator.Temp);
            var slice = newArray.Slice(0, m_NativeArray.Length);
            slice.CopyFrom(m_NativeArray);
            m_NativeArray.Dispose();
            m_NativeArray = newArray;
        }
        m_NativeArray[m_Count++] = item;
    }

    public NativeSlice<T> Values => m_NativeArray.Slice(0, m_Count);
    public ReadOnlySpan<T> Span => m_NativeArray.AsReadOnlySpan().Slice(0, m_Count);

    public void Dispose()
    {
        m_NativeArray.Dispose();
    }
}
