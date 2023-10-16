// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Hierarchy
{
    enum NativeSparseArrayResizePolicy
    {
        ExactSize,
        DoubleSize,
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct NativeSparseArray<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [StructLayout(LayoutKind.Sequential)]
        readonly struct Pair
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public Pair(in TKey key, in TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        public delegate int KeyIndex(in TKey key);
        public delegate bool KeyEqual(in TKey lhs, in TKey rhs);

        Pair* m_Ptr;
        int m_Capacity;
        int m_Count;

        readonly Allocator m_Allocator;
        readonly Pair m_InitValue;
        readonly KeyIndex m_KeyIndex;
        readonly KeyEqual m_KeyEqual;

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Ptr != null;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Capacity;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Allocate(value);
        }

        public int Count => m_Count;

        public TValue this[in TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var index = m_KeyIndex(in key);
                ThrowIfIndexOutOfRange(index);

                ref var current = ref m_Ptr[index];
                if (!m_KeyEqual(in current.Key, in key))
                    throw new KeyNotFoundException(key.ToString());

                return current.Value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var index = m_KeyIndex(in key);
                ThrowIfIndexIsNegative(index);

                EnsureCapacity(index + 1, NativeSparseArrayResizePolicy.ExactSize);

                ref var current = ref m_Ptr[index];
                if (m_KeyEqual(in current.Key, default))
                    m_Count++;

                m_Ptr[index] = new Pair(in key, in value);
            }
        }

        public NativeSparseArray(KeyIndex keyIndex, Allocator allocator)
        {
            m_Ptr = null;
            m_Capacity = 0;
            m_Count = 0;
            m_Allocator = allocator;
            m_InitValue = default;
            m_KeyIndex = keyIndex;
            m_KeyEqual = (in TKey lhs, in TKey rhs) => { return lhs.Equals(rhs); };
        }

        public NativeSparseArray(KeyIndex keyIndex, KeyEqual keyEqual, Allocator allocator)
        {
            m_Ptr = null;
            m_Capacity = 0;
            m_Count = 0;
            m_Allocator = allocator;
            m_InitValue = default;
            m_KeyIndex = keyIndex;
            m_KeyEqual = keyEqual;
        }

        public NativeSparseArray(in TValue initValue, KeyIndex keyIndex, Allocator allocator)
        {
            m_Ptr = null;
            m_Capacity = 0;
            m_Count = 0;
            m_Allocator = allocator;
            m_InitValue = new Pair(default, in initValue);
            m_KeyIndex = keyIndex;
            m_KeyEqual = (in TKey lhs, in TKey rhs) => { return lhs.Equals(rhs); };
        }

        public NativeSparseArray(in TValue initValue, KeyIndex keyIndex, KeyEqual keyEqual, Allocator allocator)
        {
            m_Ptr = null;
            m_Capacity = 0;
            m_Count = 0;
            m_Allocator = allocator;
            m_InitValue = new Pair(default, in initValue);
            m_KeyIndex = keyIndex;
            m_KeyEqual = keyEqual;
        }

        public void Dispose()
        {
            Deallocate();
        }

        public void Reserve(int capacity)
        {
            EnsureCapacity(capacity, NativeSparseArrayResizePolicy.ExactSize);
        }

        public bool ContainsKey(in TKey key)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexOutOfRange(index);

            ref var current = ref m_Ptr[index];
            return m_KeyEqual(in current.Key, in key);
        }

        public void Add(in TKey key, in TValue value, NativeSparseArrayResizePolicy policy = NativeSparseArrayResizePolicy.ExactSize)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexIsNegative(index);

            EnsureCapacity(index + 1, policy);

            ref var current = ref m_Ptr[index];
            if (m_KeyEqual(in current.Key, in key))
                throw new ArgumentException($"an element with the same key [{key}] already exists");

            if (m_KeyEqual(in current.Key, default))
                m_Count++;

            m_Ptr[index] = new Pair(in key, in value);
        }

        public void AddNoResize(in TKey key, in TValue value)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexOutOfRange(index);

            ref var current = ref m_Ptr[index];
            if (m_KeyEqual(in current.Key, in key))
                throw new ArgumentException($"an element with the same key [{key}] already exists");

            if (m_KeyEqual(in current.Key, default))
                m_Count++;

            m_Ptr[index] = new Pair(in key, in value);
        }

        public bool TryAdd(in TKey key, in TValue value, NativeSparseArrayResizePolicy policy = NativeSparseArrayResizePolicy.ExactSize)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexIsNegative(index);

            EnsureCapacity(index + 1, policy);

            ref var current = ref m_Ptr[index];
            if (m_KeyEqual(in current.Key, in key))
                return false;

            if (m_KeyEqual(in current.Key, default))
                m_Count++;

            m_Ptr[index] = new Pair(in key, in value);
            return true;
        }

        public bool TryAddNoResize(in TKey key, in TValue value)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexOutOfRange(index);

            ref var current = ref m_Ptr[index];
            if (m_KeyEqual(in current.Key, in key))
                return false;

            if (m_KeyEqual(in current.Key, default))
                m_Count++;

            m_Ptr[index] = new Pair(in key, in value);
            return true;
        }

        public bool TryGetValue(in TKey key, out TValue value)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexOutOfRange(index);

            ref var current = ref m_Ptr[index];
            if (m_KeyEqual(in current.Key, in key))
            {
                value = current.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool Remove(in TKey key)
        {
            var index = m_KeyIndex(in key);
            ThrowIfIndexOutOfRange(index);

            ref var current = ref m_Ptr[index];
            if (!m_KeyEqual(in current.Key, in key))
                return false;

            m_Ptr[index] = m_InitValue;
            m_Count--;
            return true;
        }

        public void Clear()
        {
            if (m_Ptr != null)
            {
                fixed (void* initValuePtr = &m_InitValue)
                {
                    UnsafeUtility.MemCpyReplicate(m_Ptr, initValuePtr, UnsafeUtility.SizeOf<Pair>(), m_Capacity);
                }
            }
            m_Count = 0;
        }

        void Allocate(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException($"capacity [{capacity}] cannot be negative");

            var sizeOf = UnsafeUtility.SizeOf<Pair>();
            var alignOf = UnsafeUtility.AlignOf<Pair>();
            if (m_Ptr == null)
            {
                m_Ptr = (Pair*)UnsafeUtility.Malloc(capacity * sizeOf, alignOf, m_Allocator);
                fixed (Pair* initValuePtr = &m_InitValue)
                {
                    UnsafeUtility.MemCpyReplicate(m_Ptr, initValuePtr, sizeOf, capacity);
                }
            }
            else
            {
                m_Ptr = (Pair*)Realloc(m_Ptr, capacity * sizeOf, alignOf, m_Allocator);
                if (capacity > m_Capacity)
                {
                    fixed (Pair* initValuePtr = &m_InitValue)
                    {
                        UnsafeUtility.MemCpyReplicate(m_Ptr + m_Capacity, initValuePtr, sizeOf, capacity - m_Capacity);
                    }
                }
            }
            m_Capacity = capacity;
        }

        void Deallocate()
        {
            if (m_Ptr != null)
            {
                UnsafeUtility.Free(m_Ptr, m_Allocator);
                m_Ptr = null;
            }
            m_Capacity = 0;
            m_Count = 0;
        }

        void EnsureCapacity(int capacity, NativeSparseArrayResizePolicy policy)
        {
            if (capacity <= m_Capacity)
                return;

            switch (policy)
            {
                case NativeSparseArrayResizePolicy.ExactSize:
                    Allocate(capacity);
                    break;

                case NativeSparseArrayResizePolicy.DoubleSize:
                    Allocate(Math.Max(capacity, m_Capacity * 2));
                    break;

                default:
                    throw new NotImplementedException(policy.ToString());
            }
        }

        void ThrowIfIndexIsNegative(int index)
        {
            if (index < 0)
                throw new InvalidOperationException($"key index [{index}] cannot be negative");
        }

        void ThrowIfIndexOutOfRange(int index)
        {
            ThrowIfIndexIsNegative(index);
            if (index >= m_Capacity)
                throw new InvalidOperationException($"key index [{index}] is out of range [0, {m_Capacity}]");
        }

        static void* Realloc(void* ptr, long size, int alignment, Allocator allocator)
        {
            if (ptr == null)
                return UnsafeUtility.Malloc(size, alignment, allocator);

            var newPtr = UnsafeUtility.Malloc(size, alignment, allocator);
            UnsafeUtility.MemCpy(newPtr, ptr, size);
            UnsafeUtility.Free(ptr, allocator);
            return newPtr;
        }
    }
}
