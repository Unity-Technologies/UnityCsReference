// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements.Unmanaged;

/// <summary>
/// Used for list-typed properties that don't have many variations overall.
/// The ref counting allows the Copy() and CopyFrom() methods to be O(1) and non-allocating,
/// at the cost of an extra 4 bytes and a bit of housekeeping on assignation.
/// </summary>
/// <remarks>
/// This struct always has size 8 regardless of if it's compiled for an x64 arch or not. This is
/// required to keep aligned with the native version that may or may not be compiled under the same
/// constaints than the managed side, so we need to keep the largest common size for predictability.
/// </remarks>
/// <typeparam name="T"></typeparam>
// See "Modules/UIElements/Core/Native/Unmanaged/UnmanagedRefCountedList.h"
[StructLayout(LayoutKind.Sequential, Size = 8)]
internal readonly unsafe struct UnmanagedRefCountedList<T>
    where T : unmanaged
{
    private static readonly MemoryLabel k_MemoryLabel = new(nameof(UnityEngine.UIElements), nameof(UnmanagedRefCountedList<T>), Allocator.Domain);
    public static UnmanagedRefCountedList<T> Empty => new();

    [StructLayout(LayoutKind.Sequential)]
    private struct Data
    {
        public const int k_Alignment = sizeof(int) * 2;

        public int Size;
        public int RefCount;

        // This field is just here to note where the items start in memory.
        // Multiple items will be allocated sequentially starting at this memory offset.
        // In native, this will be a T[] field, i.e. an inlined extensible array field.
        public T Items;
    }

    private readonly Data* m_Data;

    public ref T this[int index] => ref (&m_Data->Items)[index];

    public int Count => m_Data != null ? m_Data->Size : 0;
    public bool IsEmpty => m_Data == null;

    // Should be private, but needed by UnmanagedRefCountedListExtensions
    internal ref int UnsafeRefCount => ref m_Data->RefCount;
    internal int UnsafeCount => m_Data->Size;

    public ReadOnlySpan<T> ToReadOnlySpan()
    {
        return m_Data != null ? new ReadOnlySpan<T>(&m_Data->Items, m_Data->Size) : ReadOnlySpan<T>.Empty;
    }

    public List<T> ToList()
    {
        var result = new List<T>(Count);
        foreach (var item in this)
            result.Add(item);
        return result;
    }

    public List<TOther> ToList<TOther>(Func<T, TOther> convert)
    {
        var result = new List<TOther>();
        foreach (var item in this)
            result.Add(convert(item));
        return result;
    }

    public void CopyTo(ref List<T> other)
    {
        if (other == null) other = new (); else other.Clear();
        foreach (var item in this)
            other.Add(item);
    }

    public void CopyTo<TOther>(ref List<TOther> other, Func<T, TOther> convert)
    {
        if (other == null) other = new (); else other.Clear();
        foreach (var item in this)
            other.Add(convert(item));
    }

    /// <summary>
    /// Constructor for a non-empty list of a given size.
    /// </summary>
    /// <remarks>Items in this list are allocated but not initialized after this constructor is used.</remarks>
    /// <param name="size">A non-zero number of items.</param>
    internal UnmanagedRefCountedList(int size)
    {
        // Allocate sizeof(Data) + (n-1) * sizeof(T), because Data contains one T already
        var bytes = UnsafeUtility.SizeOf<Data>() + (size - 1) * UnsafeUtility.SizeOf<T>();
        m_Data = (Data*)UnsafeUtility.MallocTracked(bytes, Data.k_Alignment, k_MemoryLabel, 0);
        m_Data->Size = size;
        m_Data->RefCount = 1;
    }

    internal void UnsafeRelease()
    {
        if (--m_Data->RefCount == 0)
            UnsafeUtility.FreeTracked(m_Data, k_MemoryLabel);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly Data* m_Data;
        private int m_Index;

        public Enumerator(UnmanagedRefCountedList<T> list)
        {
            m_Data = list.m_Data;
            m_Index = -1;
        }
        public bool MoveNext()
        {
            m_Index++;
            return m_Data != null && m_Index < m_Data->Size;
        }

        public void Reset() { m_Index = -1; }

        public T Current => (&m_Data->Items)[m_Index];

        object IEnumerator.Current => Current;

        public void Dispose() { }
    }

    // Equality based on "same reference", not values-based. RefCount also not part of comparison.
    public static bool operator==(UnmanagedRefCountedList<T> a, UnmanagedRefCountedList<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator!=(UnmanagedRefCountedList<T> a, UnmanagedRefCountedList<T> b)
    {
        return !a.Equals(b);
    }

    public bool Equals(UnmanagedRefCountedList<T> other)
    {
        return m_Data == other.m_Data;
    }

    public override bool Equals(object obj)
    {
        return obj is UnmanagedRefCountedList<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)m_Data;
    }

    /// <undoc/>
    public static implicit operator StyleList<T>(UnmanagedRefCountedList<T> v)
    {
        return new StyleList<T>(v.ToList());
    }

    /// <undoc/>
    public static implicit operator ReadOnlySpan<T>(UnmanagedRefCountedList<T> v)
    {
        return v.ToReadOnlySpan();
    }
}

internal static class UnmanagedRefCountedListExtensions
{
    // Makes this list the only owner of the data and ensures it has room for count elements.
    private static void PrepareWrite<T>(ref this UnmanagedRefCountedList<T> self, int count)
        where T : unmanaged
    {
        if (count == 0)
        {
            Clear(ref self);
            return;
        }

        if (self.IsEmpty)
        {
            self = new(count);
            return;
        }

        if (self.UnsafeCount == count && self.UnsafeRefCount == 1)
            return;

        // Adjust size to copy in place
        self.UnsafeRelease();
        self = new(count);
    }

    public static void Clear<T>(ref this UnmanagedRefCountedList<T> self)
        where T : unmanaged
    {
        if (!self.IsEmpty) self.UnsafeRelease();
        self = UnmanagedRefCountedList<T>.Empty;
    }

    public static void CopyFrom<T>(ref this UnmanagedRefCountedList<T> self, UnmanagedRefCountedList<T> other)
        where T : unmanaged
    {
        // Don't do a "smart" copy for refCount==1, as individual properties are less likely to be
        // modified repeatedly like computed styles are.

        // Increment first, to avoid Dispose if self == other
        if (!other.IsEmpty) other.UnsafeRefCount++;
        if (!self.IsEmpty) self.UnsafeRelease();
        self = other;
    }

    //
    // Copy from/to managed lists.
    //

    public static void CopyFrom<T>(ref this UnmanagedRefCountedList<T> self, List<T> other)
        where T : unmanaged
    {
        var count = other?.Count ?? 0;
        self.PrepareWrite(count);
        for (int i = 0; i < count; i++)
            self[i] = other[i];
    }

    public static void CopyFrom<T, TOther>(ref this UnmanagedRefCountedList<T> self, List<TOther> other, Func<TOther, T> convert)
        where T : unmanaged
    {
        var count = other?.Count ?? 0;
        self.PrepareWrite(count);
        for (int i = 0; i < count; i++)
            self[i] = convert(other[i]);
    }

    public static void CopyFrom<T>(ref this UnmanagedRefCountedList<T> self, ReadOnlySpan<T> other)
        where T : unmanaged
    {
        var count = other.Length;
        self.PrepareWrite(count);
        for (int i = 0; i < count; i++)
            self[i] = other[i];
    }

    public static void CopyFrom(ref this UnmanagedRefCountedList<StylePropertyId> self, List<StylePropertyName> other) =>
        self.CopyFrom(other, name => name.id);

    public static void CopyTo(this UnmanagedRefCountedList<StylePropertyId> self, ref List<StylePropertyName> other) =>
        self.CopyTo(ref other, id => new StylePropertyName(id));

    public static List<StylePropertyName> ToManaged(this UnmanagedRefCountedList<StylePropertyId> self) =>
        self.ToList(id => new StylePropertyName(id));

    public static List<TimeValue> ToManaged(this UnmanagedRefCountedList<TimeValue> self) =>
        self.ToList();

    public static List<EasingFunction> ToManaged(this UnmanagedRefCountedList<EasingFunction> self) =>
        self.ToList();

    public static void CopyFrom(ref this UnmanagedRefCountedList<UnmanagedFilterFunction> self, List<FilterFunction> other) =>
        self.CopyFrom(other, f => f);

    public static void CopyTo(this UnmanagedRefCountedList<UnmanagedFilterFunction> self, ref List<FilterFunction> other) =>
        self.CopyTo(ref other, f => f);

    public static List<FilterFunction> ToManaged(this UnmanagedRefCountedList<UnmanagedFilterFunction> self) =>
        self.ToList(f => (FilterFunction)f);

    public static void CopyFrom(ref this UnmanagedRefCountedList<UnmanagedMaterialPropertyValue> self, List<MaterialPropertyValue> other) =>
        self.CopyFrom(other, mpv => mpv);

    public static void CopyTo<T>(this ReadOnlySpan<T> from, ref List<T> to)
    {
        if (to == null) to = new (); else to.Clear();
        foreach (var item in from)
            to.Add(item);
    }

    public static void CopyTo<T, TOther>(this ReadOnlySpan<T> from, ref List<TOther> to, Func<T, TOther> convert)
    {
        if (to == null) to = new (); else to.Clear();
        foreach (var item in from)
            to.Add(convert(item));
    }
}
