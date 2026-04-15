// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.U2D.UTess
{

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(ArraySliceDebugView<>))]
    internal unsafe struct ArraySlice<T> : System.IEquatable<ArraySlice<T>> where T : struct
    {
        [NativeDisableUnsafePtrRestriction] internal byte* m_Buffer;
        internal int m_Stride;
        internal int m_Length;

        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal AtomicSafetyHandle m_Safety;

        public ArraySlice(NativeArray<T> array, int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), $"Slice start {start} < 0.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"Slice length {length} < 0.");
            if (start + length > array.Length)
                throw new ArgumentException(
                    $"Slice start + length ({start + length}) range must be <= array.Length ({array.Length})");
            m_MinIndex = 0;
            m_MaxIndex = length - 1;
            m_Safety = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);

            m_Stride = UnsafeUtility.SizeOf<T>();
            var ptr = (byte*)array.GetUnsafePtr() + m_Stride * start;
            m_Buffer = ptr;
            m_Length = length;
        }

        public ArraySlice(Array<T> array, int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), $"Slice start {start} < 0.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), $"Slice length {length} < 0.");
            if (start + length > array.Length)
                throw new ArgumentException(
                    $"Slice start + length ({start + length}) range must be <= array.Length ({array.Length})");
            m_MinIndex = 0;
            m_MaxIndex = length - 1;
            m_Safety = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array.m_Array);

            m_Stride = UnsafeUtility.SizeOf<T>();
            var ptr = (byte*)array.UnsafePtr + m_Stride * start;
            m_Buffer = ptr;
            m_Length = length;
        }

        public bool Equals(ArraySlice<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Stride == other.m_Stride && m_Length == other.m_Length;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ArraySlice<T> && Equals((ArraySlice<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_Buffer;
                hashCode = (hashCode * 397) ^ m_Stride;
                hashCode = (hashCode * 397) ^ m_Length;
                return hashCode;
            }
        }

        public static bool operator ==(ArraySlice<T> left, ArraySlice<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArraySlice<T> left, ArraySlice<T> right)
        {
            return !left.Equals(right);
        }

        // These are double-whammy excluded to we can elide bounds checks in the Burst disassembly view
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadIndex(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteIndex(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index)
        {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                throw new System.IndexOutOfRangeException(
                    $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                    "You can use double buffering strategies to avoid race conditions due to " +
                    "reading & writing in parallel to the same elements from a job.");

            throw new System.IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }


        public static unsafe ArraySlice<T> ConvertExistingDataToArraySlice(void* dataPointer, int stride, int length)
        {
            if (length < 0)
                throw new System.ArgumentException($"Invalid length of '{length}'. It must be greater than 0.",
                    nameof(length));
            if (stride < 0)
                throw new System.ArgumentException($"Invalid stride '{stride}'. It must be greater than 0.",
                    nameof(stride));

            var newSlice = new ArraySlice<T>
            {
                m_Stride = stride,
                m_Buffer = (byte*)dataPointer,
                m_Length = length,
                m_MinIndex = 0,
                m_MaxIndex = length - 1,
            };

            return newSlice;
        }

        public T this[int index]
        {
            get
            {
                CheckReadIndex(index);
                return UnsafeUtility.ReadArrayElementWithStride<T>(m_Buffer, index, m_Stride);
            }

            [WriteAccessRequired]
            set
            {
                CheckWriteIndex(index);
                UnsafeUtility.WriteArrayElementWithStride(m_Buffer, index, m_Stride, value);
            }
        }

        internal unsafe void* GetUnsafeReadOnlyPtr()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return m_Buffer;
        }

        internal void CopyTo(T[] array)
        {
            if (Length != array.Length)
                throw new ArgumentException($"array.Length ({array.Length}) does not match the Length of this instance ({Length}).", nameof(array));
            unsafe
            {
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                IntPtr addr = handle.AddrOfPinnedObject();

                var sizeOf = UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpyStride((byte*)addr, sizeOf, this.GetUnsafeReadOnlyPtr(), Stride, sizeOf, m_Length);

                handle.Free();
            }
        }

        internal T[] ToArray()
        {
            var array = new T[Length];
            CopyTo(array);
            return array;
        }

        public int Stride => m_Stride;
        public int Length => m_Length;

    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="ArraySlice{T}"/>
    /// </summary>
    internal sealed class ArraySliceDebugView<T> where T : struct
    {
        ArraySlice<T> m_Slice;

        public ArraySliceDebugView(ArraySlice<T> slice)
        {
            m_Slice = slice;
        }

        public T[] Items
        {
            get { return m_Slice.ToArray(); }
        }
    }

}
