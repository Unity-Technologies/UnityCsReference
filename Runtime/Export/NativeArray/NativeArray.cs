// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

namespace Unity.Collections
{
    public enum NativeArrayOptions
    {
        UninitializedMemory            = 0,
        ClearMemory                    = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainerSupportsDeferredConvertListToArray]
    [DebuggerDisplay("Length = {m_Length}")]
    [DebuggerTypeProxy(typeof(NativeArrayDebugView<>))]
    public unsafe struct NativeArray<T> : IDisposable, IEnumerable<T>, IEquatable<NativeArray<T>> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal void*                    m_Buffer;
        internal int                      m_Length;

        internal int                      m_MinIndex;
        internal int                      m_MaxIndex;
        internal AtomicSafetyHandle       m_Safety;

        internal unsafe ref DisposeSentinel.Dummy m_DisposeSentinel
        {
            get
            {
                void* pointer = UnsafeUtility.Malloc(sizeof(DisposeSentinel.Dummy), 8, Allocator.Temp);
                return ref UnsafeUtility.AsRef<DisposeSentinel.Dummy>(pointer);
            }
        }

        // TODO: Use SharedStatic for burst compatible static id once we have typehash intrinsic for unity in burst 1.6.5 and 1.7.0
        static int                        s_staticSafetyId;

        [BurstDiscard]
        static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if (s_staticSafetyId == 0)
                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativeArray<T>>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, s_staticSafetyId);
        }


        internal Allocator                m_AllocatorLabel;

        public NativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(m_Buffer, (long)Length * UnsafeUtility.SizeOf<T>());
        }

        public NativeArray(T[] array, Allocator allocator)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        public NativeArray(NativeArray<T> array, Allocator allocator)
        {
            AtomicSafetyHandle.CheckReadAndThrow(array.m_Safety);
            Allocate(array.Length, allocator, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocateArguments(int length, Allocator allocator)
        {
            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

            // NativeArray constructor does not support custom allocator
            if (allocator >= Allocator.FirstUserIndex)
                throw new ArgumentException("Use CollectionHelper.CreateNativeArray for custom allocator", nameof(allocator));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
        }

        static void Allocate(int length, Allocator allocator, out NativeArray<T> array)
        {
            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator);

            array = default(NativeArray<T>);

            IsUnmanagedAndThrow();

            array.m_Buffer = UnsafeUtility.MallocTracked(totalSize, UnsafeUtility.AlignOf<T>(), allocator, 0);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
            AtomicSafetyHandle.CreateHandle(out array.m_Safety, allocator);
            InitStaticSafetyId(ref array.m_Safety);
            InitNestedNativeContainer(array.m_Safety);
        }

        public int Length
        {
            get
            {
                return m_Length;
            }
        }

        internal static void InitNestedNativeContainer(AtomicSafetyHandle handle)
        {
            if (UnsafeUtility.IsNativeContainerType<T>())
            {
                AtomicSafetyHandle.SetNestedContainer(handle, true);
            }
        }

        // NativeArray is not constrained to unmanaged so it must be checked
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        internal static void IsUnmanagedAndThrow()
        {
            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new InvalidOperationException(
                    $"{typeof(T)} used in NativeArray<{typeof(T)}> must be unmanaged (contain no managed types).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementReadAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            var versionPtr = (int*)m_Safety.versionNode;
            if (m_Safety.version != ((*versionPtr) & AtomicSafetyHandle.ReadCheck))
                AtomicSafetyHandle.CheckReadAndThrowNoEarlyOut(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckElementWriteAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            var versionPtr = (int*)m_Safety.versionNode;
            if (m_Safety.version != ((*versionPtr) & AtomicSafetyHandle.WriteCheck))
                AtomicSafetyHandle.CheckWriteAndThrowNoEarlyOut(m_Safety);
        }

        public T this[int index]
        {
            get
            {
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }

            [WriteAccessRequired]
            set
            {
                CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
            }
        }

        public bool IsCreated => m_Buffer != null;

        [WriteAccessRequired]
        public void Dispose()
        {
            if (m_Buffer == null)
            {
                throw new ObjectDisposedException("The NativeArray is already disposed.");
            }

            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                AtomicSafetyHandle.DisposeHandle(ref m_Safety);
                UnsafeUtility.FreeTracked(m_Buffer, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Buffer = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_Buffer == null)
            {
                throw new InvalidOperationException("The NativeArray is already disposed.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
                // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
                // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
                // will check that no jobs are writing to the container).

                var jobHandle = new NativeArrayDisposeJob { Data = new NativeArrayDispose { m_Buffer = m_Buffer, m_AllocatorLabel = m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

                AtomicSafetyHandle.Release(m_Safety);

                m_Buffer = null;
                m_AllocatorLabel = Allocator.Invalid;

                return jobHandle;
            }

            m_Buffer = null;

            return inputDeps;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array)
        {
            Copy(array, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(NativeArray<T> array)
        {
            Copy(array, this);
        }

        public void CopyTo(T[] array)
        {
            Copy(this, array);
        }

        public void CopyTo(NativeArray<T> array)
        {
            Copy(this, array);
        }

        public T[] ToArray()
        {
            var array = new T[Length];
            Copy(this, array, Length);
            return array;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void FailOutOfRangeError(int index)
        {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                    "You can use double buffering strategies to avoid race conditions due to " +
                    "reading & writing in parallel to the same elements from a job.");

            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }


        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>
        {
            NativeArray<T> m_Array;
            int m_Index;

            public Enumerator(ref NativeArray<T> array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_Index++;
                return m_Index < m_Array.Length;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            // Let NativeArray indexer check for out of range.
            public T Current => m_Array[m_Index];

            object IEnumerator.Current => Current;
        }

        public bool Equals(NativeArray<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Length == other.m_Length;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NativeArray<T> && Equals((NativeArray<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_Buffer * 397) ^ m_Length;
            }
        }

        public static bool operator==(NativeArray<T> left, NativeArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(NativeArray<T> left, NativeArray<T> right)
        {
            return !left.Equals(right);
        }

        public static void Copy(NativeArray<T> src, NativeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(ReadOnly src, NativeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, NativeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(NativeArray<T> src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(ReadOnly src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        public static void Copy(NativeArray<T> src, NativeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(ReadOnly src, NativeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(T[] src, NativeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(NativeArray<T> src, T[] dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(ReadOnly src, T[] dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        public static void Copy(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        public static void Copy(ReadOnly src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        public static void Copy(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        public static void Copy(NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        public static void Copy(ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        static void CopySafe(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);

            UnsafeUtility.MemCpy(
                (byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());
        }

        static void CopySafe(ReadOnly src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);

            UnsafeUtility.MemCpy(
                (byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());
        }

        static void CopySafe(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
            CheckCopyPtr(src);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);

            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(
                (byte*)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)addr + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        static void CopySafe(NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            CheckCopyPtr(dst);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);

            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(
                (byte*)addr + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        static void CopySafe(ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            CheckCopyPtr(dst);
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);

            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy(
                (byte*)addr + dstIndex * UnsafeUtility.SizeOf<T>(),
                (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(),
                length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckCopyPtr(T[] ptr)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
                throw new ArgumentException("source and destination length must be the same");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckCopyArguments(int srcLength, int srcIndex, int dstLength, int dstIndex, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");

            if (srcIndex < 0 || srcIndex > srcLength || (srcIndex == srcLength && srcLength > 0))
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source NativeArray.");

            if (dstIndex < 0 || dstIndex > dstLength || (dstIndex == dstLength && dstLength > 0))
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination NativeArray.");

            if (srcIndex + length > srcLength)
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source NativeArray.", nameof(length));

            if (srcIndex + length < 0)
                throw new ArgumentException("srcIndex + length causes an integer overflow");

            if (dstIndex + length > dstLength)
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination NativeArray.", nameof(length));

            if (dstIndex + length < 0)
                throw new ArgumentException("dstIndex + length causes an integer overflow");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReinterpretLoadRange<U>(int sourceIndex) where U : struct
        {
            long tsize = UnsafeUtility.SizeOf<T>();
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            long usize = UnsafeUtility.SizeOf<U>();
            long byteSize = Length * tsize;

            long firstByte = sourceIndex * tsize;
            long lastByte = firstByte + usize;

            if (firstByte < 0 || lastByte > byteSize)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "loaded byte range must fall inside container bounds");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReinterpretStoreRange<U>(int destIndex) where U : struct
        {
            long tsize = UnsafeUtility.SizeOf<T>();
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            long usize = UnsafeUtility.SizeOf<U>();
            long byteSize = Length * tsize;

            long firstByte = destIndex * tsize;
            long lastByte = firstByte + usize;

            if (firstByte < 0 || lastByte > byteSize)
                throw new ArgumentOutOfRangeException(nameof(destIndex), "stored byte range must fall inside container bounds");
        }

        public U ReinterpretLoad<U>(int sourceIndex) where U : struct
        {
            CheckReinterpretLoadRange<U>(sourceIndex);
            byte* src_ptr = ((byte*)m_Buffer) + ((long)UnsafeUtility.SizeOf<T>()) * sourceIndex;
            return UnsafeUtility.ReadArrayElement<U>(src_ptr, 0);
        }

        public void ReinterpretStore<U>(int destIndex, U data) where U : struct
        {
            CheckReinterpretStoreRange<U>(destIndex);
            byte* dst_ptr = ((byte*)m_Buffer) + ((long)UnsafeUtility.SizeOf<T>()) * destIndex;
            UnsafeUtility.WriteArrayElement<U>(dst_ptr, 0, data);
        }

        private NativeArray<U> InternalReinterpret<U>(int length) where U : struct
        {
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<U>(m_Buffer, length, m_AllocatorLabel);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, m_Safety);
            return result;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckReinterpretSize<U>() where U : struct
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<U>())
            {
                throw new InvalidOperationException($"Types {typeof(T)} and {typeof(U)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
            }
        }

        public NativeArray<U> Reinterpret<U>() where U : struct
        {
            CheckReinterpretSize<U>();
            return InternalReinterpret<U>(Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReinterpretSize<U>(long tSize, long uSize, int expectedTypeSize, long byteLen, long uLen)
        {
            if (tSize != expectedTypeSize)
            {
                throw new InvalidOperationException($"Type {typeof(T)} was expected to be {expectedTypeSize} but is {tSize} bytes");
            }

            if (uLen * uSize != byteLen)
            {
                throw new InvalidOperationException($"Types {typeof(T)} (array length {Length}) and {typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
            }
        }

        public NativeArray<U> Reinterpret<U>(int expectedTypeSize) where U : struct
        {
            long tSize = UnsafeUtility.SizeOf<T>();
            long uSize = UnsafeUtility.SizeOf<U>();

            long byteLen = ((long)Length) * tSize;
            long uLen = byteLen / uSize;

            CheckReinterpretSize<U>(tSize, uSize, expectedTypeSize, byteLen, uLen);
            return InternalReinterpret<U>((int)uLen);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckGetSubArrayArguments(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "start must be >= 0");
            }

            if (start + length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"sub array range {start}-{start + length - 1} is outside the range of the native array 0-{Length - 1}");
            }

            if (start + length < 0)
            {
                throw new ArgumentException($"sub array range {start}-{start + length - 1} caused an integer overflow and is outside the range of the native array 0-{Length - 1}");
            }
        }

        public NativeArray<T> GetSubArray(int start, int length)
        {
            CheckGetSubArrayArguments(start, length);
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(((byte*)m_Buffer) + ((long)UnsafeUtility.SizeOf<T>()) * start, length, Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, m_Safety);
            return result;
        }

        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(m_Buffer, m_Length, ref m_Safety);
        }

        [StructLayout(LayoutKind.Sequential)]
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [DebuggerDisplay("Length = {Length}")]
        [DebuggerTypeProxy(typeof(NativeArrayReadOnlyDebugView<>))]
        public struct ReadOnly : IEnumerable<T>
        {
            [NativeDisableUnsafePtrRestriction]
            internal void* m_Buffer;
            internal int   m_Length;

            internal AtomicSafetyHandle m_Safety;

            internal ReadOnly(void* buffer, int length, ref AtomicSafetyHandle safety)
            {
                m_Buffer = buffer;
                m_Length = length;
                m_Safety = safety;
            }


            public int Length
            {
                get
                {
                    return m_Length;
                }
            }

            public void CopyTo(T[] array) => Copy(this, array);

            public void CopyTo(NativeArray<T> array) => Copy(this, array);

            public T[] ToArray()
            {
                var array = new T[m_Length];
                Copy(this, array, m_Length);
                return array;
            }

            public NativeArray<U>.ReadOnly Reinterpret<U>() where U : struct
            {
                CheckReinterpretSize<U>();
                return new NativeArray<U>.ReadOnly(m_Buffer, m_Length, ref m_Safety);
            }

            public T this[int index]
            {
                get
                {
                    CheckElementReadAccess(index);
                    return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckElementReadAccess(int index)
            {
                if (index < 0 || index >= m_Length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {m_Length-1}).");
                }

                var versionPtr = (int*)m_Safety.versionNode;
                if (m_Safety.version != ((*versionPtr) & AtomicSafetyHandle.ReadCheck))
                    AtomicSafetyHandle.CheckReadAndThrowNoEarlyOut(m_Safety);
            }

            public bool IsCreated => m_Buffer != null;

            [ExcludeFromDocs]
            public struct Enumerator : IEnumerator<T>
            {
                ReadOnly m_Array;
                int m_Index;

                public Enumerator(in ReadOnly array)
                {
                    m_Array = array;
                    m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_Array.Length;
                }

                public void Reset()
                {
                    m_Index = -1;
                }

                // Let NativeArray indexer check for out of range.
                public T Current => m_Array[m_Index];

                object IEnumerator.Current => Current;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public readonly ReadOnlySpan<T> AsReadOnlySpan()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                return new ReadOnlySpan<T>(m_Buffer, m_Length);
            }

            public static implicit operator ReadOnlySpan<T>(in ReadOnly source)
            {
                return source.AsReadOnlySpan();
            }
        }

        [WriteAccessRequired]
        public readonly Span<T> AsSpan()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            return new Span<T>(m_Buffer, m_Length);
        }

        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return new ReadOnlySpan<T>(m_Buffer, m_Length);
        }

        public static implicit operator Span<T>(in NativeArray<T> source)
        {
            return source.AsSpan();
        }

        public static implicit operator ReadOnlySpan<T>(in NativeArray<T> source)
        {
            return source.AsReadOnlySpan();
        }
    }

    [NativeContainer]
    internal unsafe struct NativeArrayDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void*     m_Buffer;
        internal Allocator m_AllocatorLabel;

        internal AtomicSafetyHandle m_Safety;

        public void Dispose()
        {
            UnsafeUtility.FreeTracked(m_Buffer, m_AllocatorLabel);
        }
    }

    // [BurstCompile] - can't use attribute since it's inside com.unity.Burst.
    internal struct NativeArrayDisposeJob : IJob
    {
        internal NativeArrayDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="NativeArray{T}"/>
    /// </summary>
    internal unsafe sealed class NativeArrayDebugView<T> where T : struct
    {
        NativeArray<T> m_Array;

        public NativeArrayDebugView(NativeArray<T> array)
        {
            m_Array = array;
        }

        public T[] Items
        {
            get
            {
                if (!m_Array.IsCreated)
                {
                    return default;
                }

                // Trying to avoid safety checks, so that container can be read in debugger if it's safety handle
                // is in write-only mode.
                var length = m_Array.m_Length;
                var dst = new T[length];

                var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
                var addr = handle.AddrOfPinnedObject();

                UnsafeUtility.MemCpy((void*)addr, m_Array.m_Buffer, length * UnsafeUtility.SizeOf<T>());

                handle.Free();

                return dst;
            }
        }
    }

    /// <summary>
    /// DebuggerTypeProxy for <see cref="NativeArray{T}.ReadOnly"/>
    /// </summary>
    internal sealed class NativeArrayReadOnlyDebugView<T> where T : struct
    {
        NativeArray<T>.ReadOnly m_Array;

        public NativeArrayReadOnlyDebugView(NativeArray<T>.ReadOnly array)
        {
            m_Array = array;
        }

        public T[] Items
        {
            get
            {
                if (!m_Array.IsCreated)
                {
                    return default;
                }

                return m_Array.ToArray();
            }
        }
    }
}
namespace Unity.Collections.LowLevel.Unsafe
{
    public static class NativeArrayUnsafeUtility
    {
        public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(NativeArray<T> array) where T : struct
        {
            return array.m_Safety;
        }

        public static void SetAtomicSafetyHandle<T>(ref NativeArray<T> array, AtomicSafetyHandle safety) where T : struct
        {
            array.m_Safety = safety;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments<T>(int length) where T : struct
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");

            NativeArray<T>.IsUnmanagedAndThrow();
        }

        /// Internal method used typically by other systems to provide a view on them.
        /// The caller is still the owner of the data.
        public static unsafe NativeArray<T> ConvertExistingDataToNativeArray<T>(void* dataPointer, int length, Allocator allocator) where T : struct
        {
            CheckConvertArguments<T>(length);

            var newArray = new NativeArray<T>
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,

                m_MinIndex = 0,
                m_MaxIndex = length - 1,
            };

            return newArray;
        }

        public static unsafe void* GetUnsafePtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckWriteAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeArray<T>.ReadOnly nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeBufferPointerWithoutChecks<T>(NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }
    }
}
