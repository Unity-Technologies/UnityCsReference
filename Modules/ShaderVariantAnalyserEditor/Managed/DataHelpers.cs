// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.ShaderVariantAnalyser
{
    unsafe struct NativeReadOnlySpan<T> where T : unmanaged
    {
        T* m_Data;
        int m_Length;

        public readonly int Length => m_Length;

        public NativeReadOnlySpan(ReadOnlySpan<T> span)
        {
            fixed (T* addr = span)
            {
                m_Data = addr;
                m_Length = span.Length;
            }
        }

        public readonly ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length)
                    throw new IndexOutOfRangeException();

                return ref m_Data[index];
            }
        }

        public ReadOnlySpan<T> AsReadOnlySpan => new ReadOnlySpan<T>(m_Data, m_Length);
        public static implicit operator ReadOnlySpan<T>(NativeReadOnlySpan<T> s) => s.AsReadOnlySpan;

        public NativeReadOnlySpan<T> Slice(int start, int length)
        {
            return new NativeReadOnlySpan<T>(AsReadOnlySpan.Slice(start, length));
        }
    }

    unsafe struct NativeSpan<T> where T : unmanaged
    {
        T* m_Data;
        int m_Length;

        public readonly int Length => m_Length;

        public NativeSpan(Span<T> span)
        {
            fixed (T* addr = span)
            {
                m_Data = addr;
                m_Length = span.Length;
            }
        }

        public readonly ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Length)
                    throw new IndexOutOfRangeException();

                return ref m_Data[index];
            }
        }

        public Span<T> AsSpan => new Span<T>(m_Data, m_Length);
        public static implicit operator ReadOnlySpan<T>(NativeSpan<T> s) => s.AsSpan;
        public static implicit operator Span<T>(NativeSpan<T> s) => s.AsSpan;
        public static implicit operator NativeReadOnlySpan<T>(NativeSpan<T> s) => new NativeReadOnlySpan<T>(s.AsSpan);
    }

    interface IFilteredList<T> : IReadOnlyList<T>
    {
        int ToRawIndex(int idx);
        int FindRawIndex(int idx);
    }

    interface IFilterable<T>
    {
        int ToRawIndex(int idx);
        T AtRawIndex(int idx);
        int MaxCount { get; }
        int MaxSourceCount { get; }
    }

    // UIToolkit takes IList instead of IReadOnlyList<T>, so I use this adaptor to avoid
    // having to write the full interface every time
    class ReadOnlyIList<TData> : IList
    {
        public ReadOnlyIList(IReadOnlyList<TData> src)
        {
            source = src;
        }

        public IReadOnlyList<TData> source;

        public int Count => source?.Count ?? 0;
        public TData this[int index] => source[index];
        object IList.this[int index]
        {
            get => source[index];
            set => throw new NotSupportedException();
        }
        public IEnumerator GetEnumerator() => source.GetEnumerator();

        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public int Add(object o) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int i, object o) => throw new NotSupportedException();
        public void Remove(object o) => throw new NotSupportedException();
        public void RemoveAt(int i) => throw new NotSupportedException();

        public bool Contains(object o)
        {
            foreach (var i in source)
            {
                if ((object)i == o)
                    return true;
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank > 1)
                throw new ArgumentException("Array is multidimentional", "array");

            for (int i = 0; i < source.Count; ++i)
                array.SetValue(source[i], index + i);
        }

        public int IndexOf(object o)
        {
            for (int i = 0; i < source.Count; ++i)
            {
                if ((object)source[i] == o)
                    return i;
            }
            return -1;
        }
    }
}
