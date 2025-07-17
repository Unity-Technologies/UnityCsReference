// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A container that allows for efficient insertion and removal of elements at both ends.
    /// Elements will not be moved when inserted or removed, but the buffer will be resized when necessary.
    /// </summary>
    /// <typeparam name="T">The type of elements.</typeparam>
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafeCircularBufferTDebugView<>))]
    class CircularBuffer<T>
    {
        T[] m_Buffer;
        int m_Front;
        int m_Back;
        int m_Capacity;
        int m_Count;
        bool m_Locked;

        public int Capacity
        {
            get => m_Capacity;
            set => EnsureCapacity(value);
        }

        public int Count => m_Count;

        public bool IsEmpty => m_Count == 0;

        public int FrontIndex => m_Front;

        public int BackIndex => m_Back;

        public bool Locked
        {
            get => m_Locked;
            set => m_Locked = value;
        }

        public CircularBuffer()
        {
            m_Buffer = Array.Empty<T>();
        }

        public CircularBuffer(int initialCapacity)
        {
            EnsureCapacity(initialCapacity);
        }

        public CircularBuffer(T[] items)
        {
            EnsureCapacity(items.Length);
            Array.Copy(items, m_Buffer, items.Length);
            m_Count = items.Length;
        }

        public T this[int index]
        {
            get => m_Buffer[GetIndex(index)];
            set => m_Buffer[GetIndex(index)] = value;
        }

        public T Front()
        {
            ThrowIfEmpty();
            return m_Buffer[m_Front];
        }

        public T Back()
        {
            ThrowIfEmpty();
            return m_Buffer[(m_Back == 0 ? m_Capacity : m_Back) - 1];
        }

        public void PushFront(in T item)
        {
            ThrowIfLocked();
            EnsureCapacity(m_Count + 1);
            m_Front = Modulo(m_Front - 1, m_Capacity);
            m_Buffer[m_Front] = item;
            m_Count++;
        }

        public void PushBack(in T item)
        {
            ThrowIfLocked();
            EnsureCapacity(m_Count + 1);
            m_Buffer[m_Back] = item;
            m_Back = Modulo(m_Back + 1, m_Capacity);
            m_Count++;
        }

        public void PopFront() => PopFront(1);

        void PopFront(int count)
        {
            ThrowIfLocked();
            ThrowIfEmpty();
            count = Math.Min(count, m_Count);
            if (m_Buffer[m_Front] is IDisposable disposable)
                disposable.Dispose();
            m_Buffer[m_Front] = default;
            m_Front = Modulo(m_Front + count, m_Capacity);
            m_Count -= count;
        }

        public void PopBack() => PopBack(1);

        void PopBack(int count)
        {
            ThrowIfLocked();
            ThrowIfEmpty();
            count = Math.Min(count, m_Count);
            if (m_Buffer[m_Back] is IDisposable disposable)
                disposable.Dispose();
            m_Buffer[m_Back] = default;
            m_Back = Modulo(m_Back - count, m_Capacity);
            m_Count -= count;
        }

        public void Clear()
        {
            ThrowIfLocked();
            for (var i = 0; i < m_Buffer.Length; ++i)
            {
                if (m_Buffer[i] is IDisposable disposable)
                    disposable.Dispose();
                m_Buffer[i] = default;
            }
            m_Count = 0;
            m_Back = 0;
            m_Front = 0;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            readonly CircularBuffer<T> m_Buffer;
            int m_Index;

            public T Current => m_Buffer[m_Index];

            internal Enumerator(CircularBuffer<T> buffer)
            {
                m_Buffer = buffer;
                m_Index = -1;
            }

            public bool MoveNext() => ++m_Index < m_Buffer.m_Count;
            public void Reset() => m_Index = -1;
        }

        public T[] ToArray()
        {
            var array = new T[m_Count];
            for (var i = 0; i < m_Count; ++i)
                array[i] = m_Buffer[GetIndex(i)];
            return array;
        }

        void Allocate(int capacity)
        {
            var buffer = new T[capacity];
            if (m_Count > 0)
            {
                // Unwrap the buffer
                for (var i = 0; i < m_Count; ++i)
                    buffer[i] = m_Buffer[GetIndex(i)];
                m_Front = 0;
                m_Back = m_Count;
            }

            m_Buffer = buffer;
            m_Capacity = capacity;
        }

        void EnsureCapacity(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException($"{nameof(capacity)} must be greater than zero.");

            var newCapacity = Mathf.NextPowerOfTwo(capacity);
            if (newCapacity <= m_Capacity)
                return;

            Allocate(newCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetIndex(int index)
        {
            ThrowIfIndexOutOfRange(index);
            return m_Front + (index < (m_Capacity - m_Front) ? index : index - m_Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int Modulo(int x, int y)
        {
            var r = x % y;
            return r < 0 ? r + y : r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfEmpty()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Buffer is empty.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfIndexOutOfRange(int index)
        {
            if (IsEmpty)
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer is empty.");
            if (index < 0)
                throw new IndexOutOfRangeException($"Cannot access index {index}.");
            if (index >= m_Count)
                throw new IndexOutOfRangeException($"Cannot access index {index}. Buffer count is {m_Count}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfLocked()
        {
            if (m_Locked)
                throw new InvalidOperationException("Buffer is locked.");
        }
    }

    internal sealed class UnsafeCircularBufferTDebugView<T>
        where T : class
    {
        readonly CircularBuffer<T> m_Buffer;

        public UnsafeCircularBufferTDebugView(CircularBuffer<T> buffer)
        {
            m_Buffer = buffer;
        }

        public T[] Items => m_Buffer.ToArray();
    }
}
