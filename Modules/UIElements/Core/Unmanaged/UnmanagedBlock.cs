// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.Unmanaged;

/// <summary>
/// A buffer of uninitialized unmanaged data with adaptable <see cref="Capacity"/>.
/// </summary>
/// <typeparam name="T">The type of items in this collection. Can be any unmanaged data type.</typeparam>
/// <remarks>
/// Items in this collection are immediately available when Capacity is set.
/// This collection doesn't have a @@Count@@ or methods to Add or Remove items.
/// It's up to the user of the collection to track the state of the items in the addressable range.
/// </remarks>
unsafe struct UnmanagedBlock<T> : IDisposable where T : unmanaged
{
    private static readonly MemoryLabel k_MemoryLabel =
        new(nameof(UIElements), $"UnmanagedBlock<{typeof(T).Name}>", Allocator.Persistent);

    private T* m_Data;

    /// <summary>
    /// Gets a pointer view on the current storage of the items data.
    /// This pointer can get invalidated if Capacity is changed or Dispose is called.
    /// </summary>
    /// <returns>A pointer to a valid memory location of Capacity * sizeof(T) bytes.</returns>
    public T* GetUnsafePtr() => m_Data;

    private int m_Capacity;

    /// <summary>
    /// Increases or decreases the range of items immediately addressable in this collection.
    /// Newly available item indices are not initialized.
    /// Can potentially invalidate pointer values returned by <see cref="GetUnsafePtr"/> prior to setting this.
    /// </summary>
    public int Capacity
    {
        get => m_Capacity;
        set
        {
            if (m_Capacity == value) return;
            m_Capacity = value;
            m_Data = (T*)UnsafeUtility.Realloc(m_Data, m_Capacity * UnsafeUtility.SizeOf<T>(),
                UnsafeUtility.AlignOf<T>(), k_MemoryLabel);
        }
    }

    /// <summary>
    /// Gets or modifies the item stored at the given index.
    /// </summary>
    /// <param name="index">The offset from the beginning of this collection where the item data is located.</param>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref m_Data[index];
    }

    /// <summary>
    /// Creates a new collection of @@T@@'s capable of holding the given initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The number of immediately available item slots in this collection.</param>
    public UnmanagedBlock(int initialCapacity)
    {
        m_Data = (T*)UnsafeUtility.Malloc(initialCapacity * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(),
            k_MemoryLabel);
        m_Capacity = initialCapacity;
    }

    /// <summary>
    /// Frees memory associated with this collection.
    /// Accessing the collection after calling Dispose is not recommended.
    /// </summary>
    public void Dispose()
    {
        if (m_Data != null)
        {
            UnsafeUtility.Free(m_Data, k_MemoryLabel);
            m_Data = null;
            m_Capacity = 0;
        }
    }

    /// <summary>
    /// Returns a view to this collection's data.
    /// </summary>
    /// <param name="start">The starting offset into the data.</param>
    /// <param name="count">The number of elements in this view. Should not exceed the collection's Capacity.</param>
    /// <returns>A span of items overlapping the requested range, if available. Throws an IndexOutOfRangeException if range is not valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> ReadOnlySpan(int start, int count)
    {
        if (start < 0 || count < 0 || start + count > Capacity)
            throw new IndexOutOfRangeException();
        return new ReadOnlySpan<T>(m_Data + start, count);
    }

    /// <summary>
    /// Returns a view to this collection's data.
    /// </summary>
    /// <param name="start">The starting offset into the data.</param>
    /// <param name="count">The number of elements in this view. Should not exceed the collection's Capacity.</param>
    /// <returns>A span of items overlapping the requested range, if available. Throws an IndexOutOfRangeException if range is not valid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> Span(int start, int count)
    {
        if (start < 0 || count < 0 || start + count > Capacity)
            throw new IndexOutOfRangeException();
        return new Span<T>(m_Data + start, count);
    }
}
