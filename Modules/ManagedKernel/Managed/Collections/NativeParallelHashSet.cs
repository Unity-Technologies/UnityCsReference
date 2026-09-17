// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Internal;
using Unity.Burst;
using System.Runtime.CompilerServices;

namespace Unity.Collections
{
    /// <summary>
    /// An unordered, expandable set of unique values.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(NativeParallelHashSetDebuggerTypeProxy<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct NativeParallelHashSet<T>
        : INativeDisposable
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged, IEquatable<T>
    {
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeParallelHashSet<T>>();

        internal NativeParallelHashMap<T, bool> m_Data;

        /// <summary>
        /// Initializes and returns an instance of NativeParallelHashSet.
        /// </summary>
        /// <param name="capacity">The number of values that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeParallelHashSet(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = new NativeParallelHashMap<T, bool>(capacity, allocator);
            CollectionHelper.SetStaticSafetyId<NativeParallelHashSet<T>>(ref m_Data.m_Safety, ref s_staticSafetyId.Data);
        }

        /// <summary>
        /// Whether this set is empty.
        /// </summary>
        /// <value>True if this set is empty or if the set has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.IsEmpty;
        }

        /// <summary>
        /// Returns the current number of values in this set.
        /// </summary>
        /// <returns>The current number of values in this set.</returns>
        public int Count() => m_Data.Count();

        /// <summary>
        /// The number of values that fit in the current allocation.
        /// </summary>
        /// <value>The number of values that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than current capacity.</param>
        /// <exception cref="InvalidOperationException">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => m_Data.Capacity;
            set => m_Data.Capacity = value;
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = NativeParallelHashMap<T, bool>.MaxCapacity;

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
        public void Dispose() => m_Data.Dispose();

        /// <summary>
        /// Creates and schedules a job that will dispose this set.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this set.</returns>
        public JobHandle Dispose(JobHandle inputDeps) => m_Data.Dispose(inputDeps);

        /// <summary>
        /// Removes all values.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear() => m_Data.Clear();

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
        public bool Add(T item) => m_Data.TryAdd(item, false);

        /// <summary>
        /// Attempts to add a new item. Only succeeds if the item was not present.
        /// </summary>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the value was not already present (thus was successfully added).</returns>
        public bool TryAdd(T item) => m_Data.TryAdd(item, false);

        /// <summary>
        /// Removes a particular value.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>True if the value was present.</returns>
        public bool Remove(T item) => m_Data.Remove(item);

        /// <summary>
        /// Returns true if a particular value is present.
        /// </summary>
        /// <param name="item">The value to check for.</param>
        /// <returns>True if the value was present.</returns>
        public bool Contains(T item) => m_Data.ContainsKey(item);

        /// <summary>
        /// Returns an array with a copy of this set's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of the set's values.</returns>
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyArray(allocator);

        /// <summary>
        /// Returns a parallel writer.
        /// </summary>
        /// <returns>A parallel writer.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Data = m_Data.AsParallelWriter();
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Data.m_Safety, ref ParallelWriter.s_staticSafetyId.Data);
            return writer;
        }

        /// <summary>
        /// A parallel writer for a NativeParallelHashSet.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a set.
        /// </remarks>
        [NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public struct ParallelWriter
        {
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
            internal NativeParallelHashMap<T, bool>.ParallelWriter m_Data;

            /// <summary>
            /// The number of values that fit in the current allocation.
            /// </summary>
            /// <value>The number of values that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.Capacity;
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
            /// <returns>True if the value is not already present.</returns>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public bool Add(T item) => m_Data.TryAdd(item, false);

            /// <summary>
            /// Adds a new item.
            /// If the item is already present in the collection, this method <b>should</b> throw, but currently does not!
            /// Instead, it returns false without replacing said item.
            /// </summary>
            /// <remarks>
            /// Prefer <see cref="TryAdd(T)"/> if your intent is to allow graceful failure, as this API will eventually become more strict (and begin to throw).
            /// </remarks>
            /// <param name="item">The value to add.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            /// <returns>True if the value is not already present.</returns>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public bool Add(T item, int threadIndexOverride) => m_Data.TryAdd(item, false, threadIndexOverride);

            /// <summary>
            /// Adds a new value.
            /// If the item is already present, this method returns false without replacing said item.
            /// </summary>
            /// <param name="item">The value to add.</param>
            /// <returns>True if the value is not already present.</returns>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public bool TryAdd(T item) => m_Data.TryAdd(item, false);

            /// <summary>
            /// Adds a new value.
            /// If the item is already present, this method returns false without replacing said item.
            /// </summary>
            /// <param name="item">The value to add.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            /// <returns>True if the value is not already present.</returns>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public bool TryAdd(T item, int threadIndexOverride) => m_Data.TryAdd(item, false, threadIndexOverride);
        }

        /// <summary>
        /// Returns an enumerator over the values of this set.
        /// </summary>
        /// <returns>An enumerator over the values of this set.</returns>
        public Enumerator GetEnumerator()
        {
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Data.m_Safety);
            var ash = m_Data.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
            return new Enumerator
            {
                m_Safety = ash,
                m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Data.m_HashMapData.m_Buffer),
            };
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
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<T>
        {
            internal AtomicSafetyHandle m_Safety;
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value.
            /// </summary>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                    return m_Enumerator.GetCurrentKey<T>();
                }
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            object IEnumerator.Current => Current;
        }

        /// <summary>
        /// Returns a readonly version of this NativeParallelHashSet instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the NativeParallelHashSet it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref this);
        }

        /// <summary>
        /// A read-only alias for the value of a NativeParallelHashSet. Does not have its own allocated storage.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public struct ReadOnly
            : IEnumerable<T>
        {
            internal UnsafeParallelHashMap<T, bool> m_Data;

            AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ReadOnly>();

            internal ReadOnly(ref NativeParallelHashSet<T> data)
            {
                m_Data = data.m_Data.m_HashMapData;
                m_Safety = data.m_Data.m_Safety;
                CollectionHelper.SetStaticSafetyId<ReadOnly>(ref m_Safety, ref s_staticSafetyId.Data);
            }

            /// <summary>
            /// Whether this hash set has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash set has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.IsCreated;
            }

            /// <summary>
            /// Whether this hash set is empty.
            /// </summary>
            /// <value>True if this hash set is empty or if the map has not been constructed.</value>
            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (!IsCreated)
                    {
                        return true;
                    }

                    CheckRead();
                    return m_Data.IsEmpty;
                }
            }

            /// <summary>
            /// The current number of items in this hash set.
            /// </summary>
            /// <returns>The current number of items in this hash set.</returns>
            public readonly int Count()
            {
                CheckRead();
                return m_Data.Count();
            }

            /// <summary>
            /// The number of items that fit in the current allocation.
            /// </summary>
            /// <value>The number of items that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    return m_Data.Capacity;
                }
            }

            /// <summary>
            /// Returns true if a given item is present in this hash set.
            /// </summary>
            /// <param name="item">The item to look up.</param>
            /// <returns>True if the item was present.</returns>
            public readonly bool Contains(T item)
            {
                CheckRead();
                return m_Data.ContainsKey(item);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash set's items (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash set's items (in no particular order).</returns>
            public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
            {
                CheckRead();
                return m_Data.GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an enumerator over the items of this hash set.
            /// </summary>
            /// <returns>An enumerator over the items of this hash set.</returns>
            public readonly Enumerator GetEnumerator()
            {
                AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
                var ash = m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref ash);
                return new Enumerator
                {
                    m_Safety = ash,
                    m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Data.m_Buffer),
                };
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

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            readonly void CheckRead()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }
        }
    }

    sealed internal class NativeParallelHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
        NativeParallelHashSet<T> Data;

        public NativeParallelHashSetDebuggerTypeProxy(NativeParallelHashSet<T> data)
        {
            Data = data;
        }

        public List<T> Items
        {
            get
            {
                var result = new List<T>();
                using (var keys = Data.ToNativeArray(Allocator.Temp))
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
