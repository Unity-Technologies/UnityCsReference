// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements;

[StructLayout(LayoutKind.Explicit, Size = 32)]
readonly unsafe struct VisualNodeClassData : IEnumerable<int>
{
    public struct Enumerator : IEnumerator<int>
    {
        int* m_Ptr;

        int m_Count;
        int m_Index;

        public Enumerator(int* ptr, int count)
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

        public int Current => *(m_Ptr + m_Index);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            m_Ptr = null;
        }
    }

    [FieldOffset(0)] readonly VisualNodeClassDataFixed m_Fixed;
    [FieldOffset(0)] readonly VisualNodeClassDataAlloc m_Alloc;

    /// <summary>
    /// Returns the number of children this node has.
    /// </summary>
    /// <returns>The number of children.</returns>
    public int Count => m_Alloc.IsCreated ? m_Alloc.Count : m_Fixed.Count;

    /// <summary>
    /// Gets the number of children.
    /// </summary>
    /// <returns>The number of children.</returns>
    public int this[int index] => m_Alloc.IsCreated ? m_Alloc[index] : m_Fixed[index];

    /// <summary>
    /// Gets the unsafe pointer to the children buffer.
    /// </summary>
    /// <returns>The pointer to the children buffer.</returns>
    public int* GetUnsafePtr()
    {
        fixed (VisualNodeClassData* ptr = &this)
            return (int*) ptr;
    }

    public Enumerator GetEnumerator()
        => new Enumerator(GetUnsafePtr(), Count);

    IEnumerator<int> IEnumerable<int>.GetEnumerator()
        => new Enumerator(GetUnsafePtr(), Count);

    IEnumerator IEnumerable.GetEnumerator()
        => new Enumerator(GetUnsafePtr(), Count);
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
readonly unsafe struct VisualNodeClassDataFixed
{
    const int k_VisualNodeClassDataFixedCapacity = 8;

    [FieldOffset(0)]  readonly int __Child0;
    [FieldOffset(4)]  readonly int __Child1;
    [FieldOffset(8)]  readonly int __Child2;
    [FieldOffset(12)] readonly int __Child3;
    [FieldOffset(16)] readonly int __Child4;
    [FieldOffset(20)] readonly int __Child5;
    [FieldOffset(24)] readonly int __Child6;
    [FieldOffset(28)] readonly int __Child7;

    /// <summary>
    /// Gets the number of children.
    /// </summary>
    /// <returns>The number of children.</returns>
    public int Count
    {
        get
        {
            fixed (int* ptr = &__Child0)
            {
                var c = 0;

                for (; c < k_VisualNodeClassDataFixedCapacity; c++)
                {
                    if (*(ptr + c) == 0)
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
    public int this[int index]
    {
        get
        {
            if ((uint) index >= k_VisualNodeClassDataFixedCapacity)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (int* ptr = &__Child0)
                return *(ptr + index);
        }
    }

    /// <summary>
    /// Gets the unsafe pointer to the children buffer.
    /// </summary>
    /// <returns>The pointer to the children buffer.</returns>
    public int* GetUnsafePtr()
    {
        fixed (int* ptr = &__Child0)
            return ptr;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
readonly unsafe struct VisualNodeClassDataAlloc
{
    const int k_VisualNodeClassDataIsAllocBit = 1 << 31;

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
    public bool IsCreated => (m_Reserved & k_VisualNodeClassDataIsAllocBit) != 0;

    /// <summary>
    /// Gets the number of children.
    /// </summary>
    public int Count => m_Size;

    /// <summary>
    /// Gets the child by index.
    /// </summary>
    /// <param name="index">The child index.</param>
    /// <exception cref="IndexOutOfRangeException">The given index is out of range.</exception>
    public int this[int index]
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
    public int* GetUnsafePtr() => (int*) m_Ptr.ToPointer();
}
