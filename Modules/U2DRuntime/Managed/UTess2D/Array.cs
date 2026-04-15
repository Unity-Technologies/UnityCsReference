// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using Unity.Burst;

namespace UnityEngine.U2D.UTess
{

    /// <summary>
    /// Array. Used within UTess and constrained to
    /// 1. Auto-resizes upto the Max count with a smaller initial count.
    /// 2. Only be used within the created thread. Read 1.
    /// 3. Read/Write access are all fast-paths.
    /// 4. Mostly used with Temp Alloc within UTess ontext.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(ArrayDebugView<>))]
    internal unsafe struct Array<T> : IDisposable where T : struct
    {
        internal NativeArray<T> m_Array;
        internal int m_MaxSize;
        internal Allocator m_AllocLabel;
        internal NativeArrayOptions m_Options;

        public Array(int length, int maxSize, Allocator allocMode, NativeArrayOptions options)
        {
            m_Array = new NativeArray<T>(length, allocMode, options);
            m_AllocLabel = allocMode;
            m_Options = options;
            m_MaxSize = maxSize;
        }


        private void ResizeIfRequired(int index)
        {
            if (index >= m_MaxSize || index < 0)
                throw new IndexOutOfRangeException(
                    $"Trying to access beyond allowed size. {index} is out of range of '{m_MaxSize}' MaxSize.");
            if (index < m_Array.Length)
                return;

            int requiredSize = Length == 0 ? 1 : Length;
            while (requiredSize <= index)
                requiredSize = requiredSize * 2;

            requiredSize = requiredSize > m_MaxSize ? m_MaxSize : requiredSize;
            var copyArray = new NativeArray<T>(requiredSize, m_AllocLabel, m_Options);

            NativeArray<T>.Copy(m_Array, copyArray, Length);
            m_Array.Dispose();
            m_Array = copyArray;
        }

        public unsafe T this[int index]
        {
            get
            {
                return m_Array[index];
            }

            set
            {
                ResizeIfRequired(index);
                m_Array[index] = value;
            }
        }

        public bool IsCreated => m_Array.IsCreated;

        public int Length => (m_MaxSize != 0) ? m_Array.Length : 0;

        public int MaxSize => m_MaxSize;

        public void Dispose()
        {
            m_Array.Dispose();
            m_MaxSize = 0;
        }

        public void* UnsafePtr
        {
            get
            {
                return m_Array.GetUnsafePtr();
            }
        }

        public void* UnsafeReadOnlyPtr
        {
            get
            {
                return m_Array.GetUnsafeReadOnlyPtr();
            }
        }

        // Should only ever be used for Debugging.
        public void CopyTo(T[] array)
        {
            m_Array.CopyTo(array);
        }
    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="Array{T}"/>
    /// </summary>
    internal sealed class ArrayDebugView<T> where T : struct
    {
        private Array<T> array;

        public ArrayDebugView(Array<T> array)
        {
            this.array = array;
        }

        public T[] Items
        {
            get
            {
                var ret = new T[array.Length];
                array.CopyTo(ret);
                return ret;
            }
        }
    }

}
