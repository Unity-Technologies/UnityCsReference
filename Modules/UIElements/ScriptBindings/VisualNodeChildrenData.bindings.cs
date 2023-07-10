// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements;

/// <summary>
/// The <see cref="VisualNodeChildrenData"/> is a blittable struct that matches the internal hierarchy children.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 32)]
readonly unsafe struct VisualNodeChildrenData : IEnumerable<VisualNodeHandle>
{
    public struct Enumerator : IEnumerator<VisualNodeHandle>
    {
        VisualNodeHandle* m_Ptr;

        int m_Count;
        int m_Index;

        public Enumerator(VisualNodeHandle* ptr, int count)
        {
            m_Ptr = ptr;
            m_Count = count;
            m_Index = -1;
        }

        public bool MoveNext()
        {
            return ++m_Index < m_Count;
        }

        public void Reset()
        {
            m_Index = -1;
        }

        public VisualNodeHandle Current => *(m_Ptr + m_Index);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            m_Ptr = null;
        }
    }

    [FieldOffset(0)] readonly VisualNodeChildrenFixed m_Fixed;
    [FieldOffset(0)] readonly VisualNodeChildrenAlloc m_Alloc;

    /// <summary>
    /// Returns the number of children this node has.
    /// </summary>
    /// <returns>The number of children.</returns>
    public int Count => m_Alloc.IsCreated ? m_Alloc.Count : m_Fixed.Count;

    /// <summary>
    /// Gets the child at the specified index.
    /// </summary>
    /// <param name="index">The index to get.</param>
    /// <returns>The child handle.</returns>
    public VisualNodeHandle this[int index] => m_Alloc.IsCreated ? m_Alloc[index] : m_Fixed[index];

    /// <summary>
    /// Gets the child at the specified index.
    /// </summary>
    /// <param name="index">The index to get.</param>
    /// <returns>The child handle.</returns>
    public VisualNodeHandle ElementAt(int index)
    {
        return m_Alloc.IsCreated ? m_Alloc[index] : m_Fixed[index];
    }

    /// <summary>
    /// Gets the unsafe pointer to the children buffer.
    /// </summary>
    /// <returns>The pointer to the children buffer.</returns>
    public VisualNodeHandle* GetUnsafePtr()
    {
        fixed (VisualNodeChildrenData* ptr = &this)
            return (VisualNodeHandle*) ptr;
    }

    public Enumerator GetEnumerator()
        => new Enumerator(GetUnsafePtr(), Count);

    IEnumerator<VisualNodeHandle> IEnumerable<VisualNodeHandle>.GetEnumerator()
        => new Enumerator(GetUnsafePtr(), Count);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(GetUnsafePtr(), Count);
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
readonly unsafe struct VisualNodeChildrenFixed
{
    const int k_VisualNodeChildrenFixedCapacity = 4;

    [FieldOffset(0)]
    readonly VisualNodeHandle __Child0;

    [FieldOffset(8)]
    readonly VisualNodeHandle __Child1;

    [FieldOffset(16)]
    readonly VisualNodeHandle __Child2;

    [FieldOffset(24)]
    readonly VisualNodeHandle __Child3;

    /// <summary>
    /// Gets the number of children.
    /// </summary>
    /// <returns>The number of children.</returns>
    public int Count
    {
        get
        {
            fixed (VisualNodeHandle* ptr = &__Child0)
            {
                var c = 0;

                for (; c < k_VisualNodeChildrenFixedCapacity; c++)
                {
                    if ((ptr + c)->Id == 0)
                        return c;
                }

                return c;
            }
        }
    }

    /// <summary>
    /// Gets the child by index.
    /// </summary>
    /// <param name="index">The child index.</param>
    /// <exception cref="IndexOutOfRangeException">The given index is out of range.</exception>
    public VisualNodeHandle this[int index]
    {
        get
        {
            if ((uint) index >= k_VisualNodeChildrenFixedCapacity)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (VisualNodeHandle* ptr = &__Child0)
                return *(ptr + index);
        }
    }

    /// <summary>
    /// Gets the unsafe pointer to the children buffer.
    /// </summary>
    /// <returns>The pointer to the children buffer.</returns>
    public VisualNodeHandle* GetUnsafePtr()
    {
        fixed (VisualNodeHandle* ptr = &__Child0)
            return ptr;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
readonly unsafe struct VisualNodeChildrenAlloc
{
    const int k_VisualNodeChildrenIsAllocBit = 1 << 31;

    [FieldOffset(0)]
    readonly IntPtr m_Ptr;

    [FieldOffset(8)]
    readonly int m_Size;

    [FieldOffset(12)]
    readonly int m_Capacity;

    [FieldOffset(16)]
    readonly int m_Reserved;

    /// <summary>
    /// Returns true if the dynamic children array is allocated.
    /// </summary>
    public bool IsCreated => (m_Reserved & k_VisualNodeChildrenIsAllocBit) != 0;

    /// <summary>
    /// Gets the number of children.
    /// </summary>
    public int Count => m_Size;

    /// <summary>
    /// Gets the child by index.
    /// </summary>
    /// <param name="index">The child index.</param>
    /// <exception cref="IndexOutOfRangeException">The given index is out of range.</exception>
    public VisualNodeHandle this[int index]
    {
        get
        {
            if ((uint) index >= m_Size)
                throw new IndexOutOfRangeException(nameof(index));

            return *(GetUnsafePtr() + index);
        }
    }

    /// <summary>
    /// Gets the unsafe pointer to the children buffer.
    /// </summary>
    /// <returns>The pointer to the children buffer.</returns>
    public VisualNodeHandle* GetUnsafePtr() => (VisualNodeHandle*) m_Ptr.ToPointer();
}
