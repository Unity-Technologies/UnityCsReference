// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
unsafe struct LayoutList<T> : IDisposable
    where T : unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    struct Data
    {
        public int Capacity;
        public int Count;
        public T* Values;
    }

    readonly Allocator m_Allocator;
    Data* m_Data;

    public int Count => m_Data->Count;

    public bool IsCreated => null != m_Data;

    public ref T this[int index]
    {
        get
        {
            if ((uint)index > m_Data->Count)
                throw new ArgumentOutOfRangeException();

            return ref m_Data->Values[index];
        }
    }

    public LayoutList(int initialCapacity, Allocator allocator)
    {
        m_Allocator = allocator;
        m_Data = (Data*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Data>(), UnsafeUtility.AlignOf<Data>(), allocator);
        Assert.IsTrue(m_Data != null);
        UnsafeUtility.MemClear(m_Data, UnsafeUtility.SizeOf<Data>());
        ResizeCapacity(initialCapacity);
    }

    public void Dispose()
    {
        if (null == m_Data)
            return;

        if (m_Data->Values != null)
            UnsafeUtility.Free(m_Data->Values, m_Allocator);

        UnsafeUtility.Free(m_Data, m_Allocator);
        m_Data = null;
    }

    public void Insert(int index, T value)
    {
        if ((uint)index > m_Data->Count)
            throw new ArgumentOutOfRangeException();

        if (m_Data->Capacity == m_Data->Count)
            IncreaseCapacity();

        if (index < m_Data->Count)
        {
            // Shift elements to make space.
            UnsafeUtility.MemMove(m_Data->Values + index + 1, m_Data->Values + index, UnsafeUtility.SizeOf<T>() * (m_Data->Count - index));
        }

        m_Data->Values[index] = value;
        m_Data->Count++;
    }

    public int IndexOf(T value)
    {
        var count = m_Data->Count;

        var src = &value;
        var ptr = m_Data->Values;
        var size = UnsafeUtility.SizeOf<T>();

        for (var i = 0; i < count; i++, ptr++)
        {
            if (UnsafeUtility.MemCmp(ptr, src, size) == 0)
                return i;
        }

        return -1;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= m_Data->Count)
            throw new ArgumentOutOfRangeException();

        m_Data->Count--;

        UnsafeUtility.MemMove(m_Data->Values + index, m_Data->Values + index + 1, UnsafeUtility.SizeOf<T>() * (m_Data->Count - index));
        m_Data->Values[m_Data->Count] = default;
    }

    public void Clear()
    {
        m_Data->Count = 0;
    }

    void IncreaseCapacity()
    {
        EnsureCapacity(m_Data->Capacity * 2);
    }

    void EnsureCapacity(int capacity)
    {
        if (capacity <= m_Data->Capacity)
            return;

        ResizeCapacity(capacity);
    }

    void ResizeCapacity(int capacity)
    {
        Assert.IsTrue(capacity > 0);
        m_Data->Values = (T*)ResizeArray(m_Data->Values, m_Data->Capacity, capacity, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), m_Allocator);
        m_Data->Capacity = capacity;
    }

    static void* ResizeArray(void* fromPtr, long fromCount, long toCount, long size, int align, Allocator allocator)
    {
        Assert.IsTrue(toCount > 0);

        var toPtr = UnsafeUtility.Malloc(size * toCount, align, allocator);
        Assert.IsTrue(toPtr != null);

        if (fromCount <= 0)
            return toPtr;

        var countToCopy = toCount < fromCount ? toCount : fromCount;
        var bytesToCopy = countToCopy * size;

        UnsafeUtility.MemCpy(toPtr, fromPtr, bytesToCopy);
        UnsafeUtility.Free(fromPtr, allocator);

        return toPtr;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator : IEnumerator<T>
    {
        LayoutList<T> m_List;
        int m_Index;
        T m_Current;

        public T Current => m_Current;
        object IEnumerator.Current => m_Current;

        public Enumerator(LayoutList<T> list)
        {
            m_List = list;
            m_Index = 0;
            m_Current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (!m_List.IsCreated)
            {
                m_Current = default;
                return false;
            }

            if ((uint)m_Index >= m_List.Count)
            {
                m_Current = default;
                return false;
            }

            m_Current = m_List[m_Index];
            m_Index++;
            return true;
        }

        public void Reset()
        {
            m_Index = 0;
        }
    }
}
