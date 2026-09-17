// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Jobs;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An unordered, expandable set of unique values.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeHashSetDebuggerTypeProxy<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct UnsafeHashSet<T>
        : INativeDisposable
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged, IEquatable<T>
    {
        internal HashMapHelper<T> m_Data;

        /// <summary>
        /// Initializes and returns an instance of NativeParallelHashSet.
        /// </summary>
        /// <param name="initialCapacity">The number of values that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHashSet(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = default;
            m_Data.Init(initialCapacity, 0, HashMapHelper<T>.kMinCapacity, allocator);
        }

        /// <summary>
        /// Whether this set is empty.
        /// </summary>
        /// <value>True if this set is empty or if the set has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || m_Data.IsEmpty;
        }

        /// <summary>
        /// Returns the current number of values in this set.
        /// </summary>
        /// <returns>The current number of values in this set.</returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.Count;
        }

        /// <summary>
        /// The number of values that fit in the current allocation.
        /// </summary>
        /// <value>The number of values that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than current capacity.</param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => m_Data.Capacity;
            set => m_Data.Resize(value);
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = HashMapHelper<T>.kMaxCapacity;

        /// <summary>
        /// Whether this set has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this set has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.IsCreated;
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            m_Data.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this set.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this set.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new UnsafeDisposeJob { Ptr = m_Data.Ptr, Allocator = m_Data.Allocator }.Schedule(inputDeps);
            m_Data.Ptr = null;

            return jobHandle;
        }

        /// <summary>
        /// Removes all values.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_Data.Clear();
        }

        /// <summary>
        /// Adds a new item.
        /// If the item is already present in the collection, this method <b>should</b> throw, but currently does not!
        /// Instead, it returns false without replacing said item.
        /// </summary>
        /// <remarks>
        /// Prefer <see cref="TryAdd(T)"/> if your intent is to allow graceful failure, as this API will eventually become more strict (and begin to throw).
        /// </remarks>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present.</returns>
        public bool Add(T item)
        {
            return -1 != m_Data.TryAdd(item);
        }

        /// <summary>
        /// Attempts to add a new item. Only succeeds if the item was not present.
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present (thus was successfully added).</returns>
        public bool TryAdd(T item)
        {
            return -1 != m_Data.TryAdd(item);
        }

        /// <summary>
        /// Removes a particular value.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>True if the value was present.</returns>
        public bool Remove(T item)
        {
            return -1 != m_Data.TryRemove(item);
        }

        /// <summary>
        /// Returns true if a particular value is present.
        /// </summary>
        /// <param name="item">The value to check for.</param>
        /// <returns>True if the value was present.</returns>
        public bool Contains(T item)
        {
            return -1 != m_Data.Find(item);
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess() => m_Data.TrimExcess();

        /// <summary>
        /// Returns an array with a copy of this set's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of the set's values.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            return m_Data.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the values of this set.
        /// </summary>
        /// <returns>An enumerator over the values of this set.</returns>
        public Enumerator GetEnumerator()
        {
            fixed (HashMapHelper<T>* data = &m_Data)
            {
                return new Enumerator { m_Enumerator = new HashMapHelper<T>.Enumerator(data) };
            }
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// An enumerator over the values of a set.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first value.
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal HashMapHelper<T>.Enumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value.
            /// </summary>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Enumerator.Reset();

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Enumerator.m_Data->Keys[m_Enumerator.m_Index];
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            object IEnumerator.Current => Current;
        }

        /// <summary>
        /// Returns a readonly version of this UnsafeHashMap instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the UnsafeHashMap it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref m_Data);
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeHashSet. Does not have its own allocated storage.
        /// </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public struct ReadOnly
            : IEnumerable<T>
        {
            internal HashMapHelper<T> m_Data;

            internal ReadOnly(ref HashMapHelper<T> data)
            {
                m_Data = data;
            }

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.IsCreated;
            }

            /// <summary>
            /// Whether this hash set is empty.
            /// </summary>
            /// <value>True if this hash set is empty or if the set has not been constructed.</value>
            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.IsEmpty;
            }

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.Count;
            }

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.Capacity;
            }

            /// <summary>
            /// Returns true if a particular value is present.
            /// </summary>
            /// <param name="item">The item to look up.</param>
            /// <returns>True if the item was present.</returns>
            public readonly bool Contains(T item)
            {
                return -1 != m_Data.Find(item);
            }

            /// <summary>
            /// Returns an array with a copy of this set's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of the set's values.</returns>
            public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_Data.GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public readonly Enumerator GetEnumerator()
            {
                fixed (HashMapHelper<T>* data = &m_Data)
                {
                    return new Enumerator { m_Enumerator = new HashMapHelper<T>.Enumerator(data) };
                }
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }

    sealed internal class UnsafeHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
        HashMapHelper<T> Data;

        public UnsafeHashSetDebuggerTypeProxy(UnsafeHashSet<T> data)
        {
            Data = data.m_Data;
        }

        public List<T> Items
        {
            get
            {
                var result = new List<T>();
                using (var keys = Data.GetKeyArray(Allocator.Temp))
                {
                    for (var k = 0; k < keys.Length; ++k)
                    {
                        result.Add(keys[k]);
                    }
                }

                return result;
            }
        }
    }
}
