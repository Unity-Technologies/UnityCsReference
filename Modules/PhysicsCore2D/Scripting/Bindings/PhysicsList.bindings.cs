// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.U2D.Physics
{
    static partial class Scripting2D
    {
        /// <summary>
        /// Internal growable native-backed list used by PhysicsCore2D in place of NativeList.
        /// Avoids the Unity.Collections container plumbing (safety handles, AllocatorManager, Burst attributes)
        /// while keeping the same allocation primitive used by <see cref="PhysicsBuffer"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PhysicsList<T> : IDisposable
            where T : unmanaged
        {
            T* m_Ptr;
            int m_Length;
            int m_Capacity;
            Allocator m_Allocator;

            /// <undoc/>
            public PhysicsList(Allocator allocator) : this(1, allocator) { }

            /// <undoc/>
            public PhysicsList(int initialCapacity, Allocator allocator)
            {
                CheckAllocator(allocator);

                m_Ptr = null;
                m_Length = 0;
                m_Capacity = 0;
                m_Allocator = allocator;

                if (initialCapacity > 0)
                    Allocate(initialCapacity);
            }

            /// <undoc/>
            public int Length
            {
                get => m_Length;
                set => Resize(value);
            }

            /// <undoc/>
            public int Capacity
            {
                get => m_Capacity;
                set => SetCapacity(value);
            }

            /// <undoc/>
            public bool IsCreated => m_Ptr != null;

            /// <undoc/>
            public bool IsEmpty => m_Ptr == null || m_Length == 0;

            /// <undoc/>
            public ref T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckIndexInRange(index);
                    return ref UnsafeUtility.ArrayElementAsRef<T>(m_Ptr, index);
                }
            }

            /// <undoc/>
            public void Add(in T value)
            {
                CheckCreated();

                if (m_Length == m_Capacity)
                    Grow();

                m_Ptr[m_Length++] = value;
            }

            /// <undoc/>
            public void Clear() => m_Length = 0;

            /// <undoc/>
            public void Resize(int length)
            {
                CheckCreated();
                CheckNonNegative(length);

                if (length > m_Capacity)
                    SetCapacity(length);

                m_Length = length;
            }

            /// <undoc/>
            public void SetCapacity(int capacity)
            {
                CheckCreated();
                CheckNonNegative(capacity);

                if (capacity == m_Capacity)
                    return;

                if (capacity == 0)
                {
                    UnsafeUtility.FreeTracked(m_Ptr, m_Allocator);
                    m_Ptr = null;
                    m_Capacity = 0;
                    m_Length = 0;
                    return;
                }

                var newPtr = (T*)UnsafeUtility.MallocTracked(
                    (long)capacity * sizeof(T),
                    UnsafeUtility.AlignOf<T>(),
                    m_Allocator,
                    0);

                var copyCount = m_Length < capacity ? m_Length : capacity;
                if (copyCount > 0)
                    UnsafeUtility.MemCpy(newPtr, m_Ptr, (long)copyCount * sizeof(T));

                UnsafeUtility.FreeTracked(m_Ptr, m_Allocator);
                m_Ptr = newPtr;
                m_Capacity = capacity;

                if (m_Length > m_Capacity)
                    m_Length = m_Capacity;
            }

            /// <summary>
            /// Transfers ownership of the backing buffer to a new <see cref="NativeArray{T}"/>.
            /// The list is left empty (<see cref="IsCreated"/> becomes false) and its <see cref="Dispose"/> becomes a no-op.
            /// The returned array is sized exactly to <see cref="Length"/>; any trailing capacity is freed before transfer.
            /// The caller becomes responsible for disposing the returned array.
            /// </summary>
            public NativeArray<T> ToNativeArray()
            {
                if (m_Ptr == null || m_Length == 0)
                {
                    Dispose();
                    return new NativeArray<T>();
                }

                // Trim trailing capacity so the NativeArray's FreeTracked frees the exact size that was allocated.
                if (m_Length != m_Capacity)
                    SetCapacity(m_Length);

                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_Ptr, m_Length, m_Allocator);
                var safetyHandle = (m_Allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safetyHandle);

                // Relinquish ownership. Do NOT call FreeTracked - the NativeArray owns the buffer now.
                m_Ptr = null;
                m_Length = 0;
                m_Capacity = 0;
                m_Allocator = Allocator.None;

                return array;
            }

            /// <undoc/>
            public Span<T>.Enumerator GetEnumerator() => new Span<T>(m_Ptr, m_Length).GetEnumerator();

            /// <summary>
            /// Sort the list in place using the specified comparer.
            /// For default ascending order on <see cref="IComparable{T}"/> element types, pass <c>default(PhysicsList&lt;T&gt;.DefaultComparer&lt;T&gt;)</c>.
            /// </summary>
            public void Sort<TComparer>(TComparer comparer)
                where TComparer : IComparer<T>
            {
                CheckCreated();
                IntroSort(m_Ptr, m_Length, comparer);
            }

            /// <summary>
            /// Default ascending comparer for element types that implement <see cref="IComparable{TElement}"/>.
            /// Use with <see cref="Sort{TComparer}(TComparer)"/> as <c>list.Sort(default(PhysicsList&lt;TElement&gt;.DefaultComparer&lt;TElement&gt;))</c>.
            /// </summary>
            public readonly struct DefaultComparer<TElement> : IComparer<TElement>
                where TElement : IComparable<TElement>
            {
                /// <undoc/>
                public int Compare(TElement x, TElement y) => x.CompareTo(y);
            }

            /// <undoc/>
            public void Dispose()
            {
                if (m_Ptr == null)
                    return;

                UnsafeUtility.FreeTracked(m_Ptr, m_Allocator);
                m_Ptr = null;
                m_Length = 0;
                m_Capacity = 0;
                m_Allocator = Allocator.None;
            }

            void Allocate(int capacity)
            {
                m_Ptr = (T*)UnsafeUtility.MallocTracked(
                    (long)capacity * sizeof(T),
                    UnsafeUtility.AlignOf<T>(),
                    m_Allocator,
                    0);
                m_Capacity = capacity;
                m_Length = 0;
            }

            void Grow()
            {
                var newCapacity = m_Capacity == 0 ? 4 : m_Capacity * 2;
                SetCapacity(newCapacity);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckIndexInRange(int index)
            {
                if ((uint)index >= (uint)m_Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range [0, {m_Length}).");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            static void CheckAllocator(Allocator allocator)
            {
                if (allocator <= Allocator.None)
                    throw new ArgumentException("Allocator must be a valid allocator.", nameof(allocator));
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckCreated()
            {
                if (m_Ptr == null)
                    throw new ObjectDisposedException(nameof(PhysicsList<T>));
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            static void CheckNonNegative(int value)
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be non-negative.");
            }

            // Introsort: quicksort + insertion sort for small partitions + heapsort depth-limit fallback.
            // Lifted from Unity.Collections.NativeSortExtension.IntroSortStruct (NativeSort.cs lines 971-1139).
            // Identical algorithm, identical thresholds, identical performance characteristics to NativeList.Sort.

            const int k_IntrosortSizeThreshold = 16;

            static void IntroSort<TComparer>(T* array, int length, TComparer comp)
                where TComparer : IComparer<T>
            {
                if (length < 2)
                    return;
                IntroSortRecursive(array, 0, length - 1, 2 * Log2Floor(length), comp);
            }

            static void IntroSortRecursive<TComparer>(T* array, int lo, int hi, int depth, TComparer comp)
                where TComparer : IComparer<T>
            {
                while (hi > lo)
                {
                    int partitionSize = hi - lo + 1;
                    if (partitionSize <= k_IntrosortSizeThreshold)
                    {
                        if (partitionSize == 1)
                            return;
                        if (partitionSize == 2)
                        {
                            SwapIfGreater(array, lo, hi, comp);
                            return;
                        }
                        if (partitionSize == 3)
                        {
                            SwapIfGreater(array, lo, hi - 1, comp);
                            SwapIfGreater(array, lo, hi, comp);
                            SwapIfGreater(array, hi - 1, hi, comp);
                            return;
                        }

                        InsertionSort(array, lo, hi, comp);
                        return;
                    }

                    if (depth == 0)
                    {
                        HeapSort(array, lo, hi, comp);
                        return;
                    }
                    depth--;

                    int p = Partition(array, lo, hi, comp);
                    IntroSortRecursive(array, p + 1, hi, depth, comp);
                    hi = p - 1;
                }
            }

            static void InsertionSort<TComparer>(T* array, int lo, int hi, TComparer comp)
                where TComparer : IComparer<T>
            {
                for (int i = lo; i < hi; i++)
                {
                    int j = i;
                    T t = array[i + 1];
                    while (j >= lo && comp.Compare(t, array[j]) < 0)
                    {
                        array[j + 1] = array[j];
                        j--;
                    }
                    array[j + 1] = t;
                }
            }

            static int Partition<TComparer>(T* array, int lo, int hi, TComparer comp)
                where TComparer : IComparer<T>
            {
                int mid = lo + ((hi - lo) / 2);
                SwapIfGreater(array, lo, mid, comp);
                SwapIfGreater(array, lo, hi, comp);
                SwapIfGreater(array, mid, hi, comp);

                T pivot = array[mid];
                Swap(array, mid, hi - 1);
                int left = lo, right = hi - 1;

                while (left < right)
                {
                    while (left < hi && comp.Compare(pivot, array[++left]) > 0)
                    {
                    }

                    while (right > left && comp.Compare(pivot, array[--right]) < 0)
                    {
                    }

                    if (left >= right)
                        break;

                    Swap(array, left, right);
                }

                Swap(array, left, hi - 1);
                return left;
            }

            static void HeapSort<TComparer>(T* array, int lo, int hi, TComparer comp)
                where TComparer : IComparer<T>
            {
                int n = hi - lo + 1;

                for (int i = n / 2; i >= 1; i--)
                    Heapify(array, i, n, lo, comp);

                for (int i = n; i > 1; i--)
                {
                    Swap(array, lo, lo + i - 1);
                    Heapify(array, 1, i - 1, lo, comp);
                }
            }

            static void Heapify<TComparer>(T* array, int i, int n, int lo, TComparer comp)
                where TComparer : IComparer<T>
            {
                T val = array[lo + i - 1];
                while (i <= n / 2)
                {
                    int child = 2 * i;

                    if (child < n && comp.Compare(array[lo + child - 1], array[lo + child]) < 0)
                        child++;

                    if (comp.Compare(array[lo + child - 1], val) < 0)
                        break;

                    array[lo + i - 1] = array[lo + child - 1];
                    i = child;
                }

                array[lo + i - 1] = val;
            }

            static void Swap(T* array, int lhs, int rhs)
            {
                T tmp = array[lhs];
                array[lhs] = array[rhs];
                array[rhs] = tmp;
            }

            static void SwapIfGreater<TComparer>(T* array, int lhs, int rhs, TComparer comp)
                where TComparer : IComparer<T>
            {
                if (lhs != rhs && comp.Compare(array[lhs], array[rhs]) > 0)
                    Swap(array, lhs, rhs);
            }

            static int Log2Floor(int value)
            {
                int r = 0;
                while ((value >>= 1) != 0)
                    r++;
                return r;
            }
        }
    }
}
