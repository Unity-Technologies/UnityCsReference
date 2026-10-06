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
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Collections
{
    ///<summary>Options for controlling how memory is cleared.</summary>
    ///<remarks>You can either clear memory on allocation or leave it uninitialized. <see cref="NativeArrayOptions.ClearMemory" /> is the default.</remarks>
    public enum NativeArrayOptions
    {
        ///<summary>Leaves <see cref="Unity.Collections.NativeArray{T}" /> memory uninitialized.</summary>
        ///<remarks>Uninitialized memory can improve performance, but means that the contents of the NativeArray elements are undefined.
        ///In performance sensitive code you might use NativeArrayOptions.Uninitialized if you want to write to the entire array right after 
        ///creating it without reading any of the elements first.</remarks>
        UninitializedMemory            = 0,
        ///<summary>Clears <see cref="Unity.Collections.NativeArray{T}" /> memory on allocation.</summary>
        ClearMemory                    = 1
    }

    ///<summary>Provides a buffer of native memory to managed code, making it possible to share data between managed and native code without marshalling costs.</summary>
    ///<remarks>NativeArray is a fixed-size block of unmanaged memory which you can directly access from managed code. You can use NativeArray instances in jobs and Burst-compiled code, with optional safety checks for bounds, access, and dependencies. You explicitly control allocation and disposal via an allocator, and Unity tracks allocations to help detect memory leaks. You can use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.collections@latest" &gt;Unity Collections package&lt;/a&gt; to further extend its functionality.</remarks>
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
        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.ContentLoadModule", "UnityEngine.TilemapModule", "UnityEditor.CoreModule")]
        internal void*                    m_Buffer;

        internal int                      m_Length;

        internal int                      m_MinIndex;
        internal int                      m_MaxIndex;
        [VisibleToOtherModules("UnityEngine.CoreModule")]
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
        [NoAutoStaticsCleanup] // AtomicSafetyHandle static safety id is stable across code reload; re-registering would leak ids
        static int                        s_staticSafetyId;

        [BurstDiscard]
        static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if (s_staticSafetyId == 0)
                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativeArray<T>>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, s_staticSafetyId);
        }


        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEditor.CoreModule")]
        internal Allocator                m_AllocatorLabel;

        ///<summary>Creates a new NativeArray and allocates enough memory to fit the provided amount of elements.</summary>
        ///<param name="length">Number of elements to allocate.</param>
        ///<param name="allocator">The <see cref="Unity.Collections.Allocator" /> to use for the data.</param>
        ///<param name="options">The <see cref="Unity.Collections.NativeArrayOptions" /> to use for the data.</param>
        public NativeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, default, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(m_Buffer, (long)Length * UnsafeUtility.SizeOf<T>());
        }

        ///<summary>Creates a new NativeArray and allocates enough memory to fit the provided number of elements, using the specified memory label.</summary>
        ///<param name="length">Number of elements to allocate.</param>
        ///<param name="label">The <see cref="Unity.Collections.MemoryLabel" /> to allocate under.</param>
        ///<param name="options">The <see cref="Unity.Collections.NativeArrayOptions" /> to use for the data.</param>
        public NativeArray(int length, MemoryLabel label, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            label.CheckArgument();
            Allocate(length, label.allocator, label, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                UnsafeUtility.MemClear(m_Buffer, (long)Length * UnsafeUtility.SizeOf<T>());
        }

        ///<summary>Creates a NativeArray from an array of elements.</summary>
        ///<param name="array">An array to copy the data from.</param>
        ///<param name="allocator">The <see cref="Unity.Collections.Allocator" /> to use for the data.</param>
        public NativeArray(T[] array, Allocator allocator)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            Allocate(array.Length, allocator, default, out this);
            Copy(array, this);
        }

        ///<summary>Creates a NativeArray from an array of elements, using the specified memory label.</summary>
        ///<param name="array">An array to copy the data from.</param>
        ///<param name="label">The <see cref="Unity.Collections.MemoryLabel" /> to allocate under.</param>
        public NativeArray(T[] array, MemoryLabel label)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            label.CheckArgument();

            Allocate(array.Length, label.allocator, label, out this);
            Copy(array, this);
        }

        ///<summary>Creates a NativeArray from an existing NativeArray.</summary>
        ///<param name="array">The <see cref="Unity.Collections.NativeArray{T}" /> to copy the data from.</param>
        ///<param name="allocator">The <see cref="Unity.Collections.Allocator" /> to use for the data.</param>
        public NativeArray(NativeArray<T> array, Allocator allocator)
        {
            AtomicSafetyHandle.CheckReadAndThrow(array.m_Safety);
            Allocate(array.Length, allocator, default, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        ///<summary>Creates a NativeArray from an existing NativeArray, using the specified memory label.</summary>
        ///<param name="array">The <see cref="Unity.Collections.NativeArray{T}" /> to copy the data from.</param>
        ///<param name="label">The <see cref="Unity.Collections.MemoryLabel" /> to allocate under.</param>
        public NativeArray(NativeArray<T> array, MemoryLabel label)
        {
            AtomicSafetyHandle.CheckReadAndThrow(array.m_Safety);

            label.CheckArgument();
            Allocate(array.Length, label.allocator, label, out this);
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
                throw new ArgumentException("Use CollectionHelper.CreateNativeArray in com.unity.collections package for custom allocator", nameof(allocator));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
        }

        static void Allocate(int length, Allocator allocator, MemoryLabel label, out NativeArray<T> array)
        {
            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator);

            array = default(NativeArray<T>);

            IsUnmanagedAndThrow();

            array.m_Buffer = UnsafeUtility.MallocTracked(totalSize, UnsafeUtility.AlignOf<T>(), allocator, 0, label.pointer);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;

            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
            AtomicSafetyHandle.CreateHandle(out array.m_Safety, allocator);
            InitStaticSafetyId(ref array.m_Safety);
            InitNestedNativeContainer(array.m_Safety);
        }

        ///<summary>Number of elements in a <see cref="Unity.Collections.NativeArray{T}" />.</summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckElementReadAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckElementWriteAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
                FailOutOfRangeError(index);

            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }

        ///<summary>Access <see cref="Unity.Collections.NativeArray{T}" /> elements by index.</summary>
        ///<remarks>Structs are returned by value and not by reference.</remarks>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [WriteAccessRequired]
            set
            {
                CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
            }
        }

        ///<summary>Indicates that a <see cref="Unity.Collections.NativeArray{T}" /> has an allocated memory buffer.</summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer != null;
        }

        ///<summary>Disposes a <see cref="Unity.Collections.NativeArray{T}" />.</summary>
        ///<remarks>This methods disposes the memory owned by a <see cref="Unity.Collections.NativeArray{T}" />. The behavior of this methods depends on the 
        ///<see cref="Allocator" /> used when the NativeArray was created.</remarks>
        [WriteAccessRequired]
        public void Dispose()
        {
            if (m_AllocatorLabel != Allocator.None
            && !AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            if (!IsCreated)
            {
                return;
            }

            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_AllocatorLabel >= Allocator.FirstUserIndex)
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was allocated with a custom allocator, use CollectionHelper.Dispose in com.unity.collections package.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                AtomicSafetyHandle.DisposeHandle(ref m_Safety);
                UnsafeUtility.FreeTracked(m_Buffer, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Buffer = null;
        }

        ///<summary>Creates and schedules a job that releases all resources (memory and safety handles) of a <see cref="Unity.Collections.NativeArray{T}" />.</summary>
        ///<param name="inputDeps">The dependency of the new job.</param>
        ///<returns>The <see cref="Unity.Jobs.JobHandle" />] of the new job. The job depends upon <c>inputDeps</c> and releases all resources (memory and safety handles) 
        ///of the <see cref="Unity.Collections.NativeArray{T}" />.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel != Allocator.None
            && !AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            if (!IsCreated)
            {
                return inputDeps;
            }

            if (m_AllocatorLabel >= Allocator.FirstUserIndex)
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was allocated with a custom allocator, use CollectionHelper.Dispose in com.unity.collections package.");
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

        ///<summary>Copies all the elements from a <see cref="Unity.Collections.NativeArray{T}" /> or a managed array of the same length.</summary>
        ///<param name="array">The array to copy elements from.</param>
        [WriteAccessRequired]
        public void CopyFrom(T[] array)
        {
            Copy(array, this);
        }

        ///<summary>Copies all the elements from a <see cref="Unity.Collections.NativeArray{T}" /> or a managed array of the same length.</summary>
        ///<param name="array">The array to copy elements from.</param>
        [WriteAccessRequired]
        public void CopyFrom(NativeArray<T> array)
        {
            Copy(array, this);
        }

        ///<summary>Copies all the elements to another <see cref="Unity.Collections.NativeArray{T}" /> or a managed array of the same length.</summary>
        ///<param name="array">The array to copy elements to.</param>
        public void CopyTo(T[] array)
        {
            Copy(this, array);
        }

        ///<summary>Copies all the elements to another <see cref="Unity.Collections.NativeArray{T}" /> or a managed array of the same length.</summary>
        ///<param name="array">The array to copy elements to.</param>
        public void CopyTo(NativeArray<T> array)
        {
            Copy(this, array);
        }

        ///<summary>Convert a <see cref="Unity.Collections.NativeArray{T}" /> to an array.</summary>
        ///<returns>The converted array.</returns>
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


        ///<summary>Gets an enumerator.</summary>
        ///<returns>An enumerator.</returns>
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
            T value;

            public Enumerator(ref NativeArray<T> array)
            {
                m_Array = array;
                m_Index = -1;
                value = default;
            }

            public void Dispose()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                m_Index++;
                if (m_Index < m_Array.m_Length)
                {
                    AtomicSafetyHandle.CheckReadAndThrow(m_Array.m_Safety);
                    value = UnsafeUtility.ReadArrayElement<T>(m_Array.m_Buffer, m_Index);
                    return true;
                }
                value = default;
                return false;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            // Let NativeArray indexer check for out of range.
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return value;
                }
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return Current;
                }
            }
        }

        ///<summary>Compares two <see cref="Unity.Collections.NativeArray{T}" /> instances.</summary>
        ///<remarks>Two <see cref="Unity.Collections.NativeArray{T}" /> instances are considered the same if they point to the same underlying memory buffer, and 
        ///have the same length.</remarks>
        ///<param name="other">The NativeArray to compare against.</param>
        ///<returns>True if the two NativeArray instances are the same, false otherwise.</returns>
        public bool Equals(NativeArray<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Length == other.m_Length;
        }

        ///<summary>Compares a <see cref="Unity.Collections.NativeArray{T}" /> instance and an object.</summary>
        ///<remarks>A NativeArray and an object are considered the same if they point to the same underlying memory buffer, and have the same length.</remarks>
        ///<param name="obj">The object to compare against.</param>
        ///<returns>True if the NativeArray and the object are the same, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NativeArray<T> && Equals((NativeArray<T>)obj);
        }

        ///<summary>Gets a hash code for the current instance.</summary>
        ///<returns>A hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)m_Buffer * 397) ^ m_Length;
            }
        }

        ///<exclude />
        public static bool operator==(NativeArray<T> left, NativeArray<T> right)
        {
            return left.Equals(right);
        }

        ///<exclude />
        public static bool operator!=(NativeArray<T> left, NativeArray<T> right)
        {
            return !left.Equals(right);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        public static void Copy(NativeArray<T> src, NativeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        public static void Copy(ReadOnly src, NativeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        public static void Copy(T[] src, NativeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        public static void Copy(NativeArray<T> src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        public static void Copy(ReadOnly src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);

            CopySafe(src, 0, dst, 0, src.Length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        public static void Copy(NativeArray<T> src, NativeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        public static void Copy(ReadOnly src, NativeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        public static void Copy(T[] src, NativeArray<T> dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        public static void Copy(NativeArray<T> src, T[] dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        public static void Copy(ReadOnly src, T[] dst, int length)
        {
            CopySafe(src, 0, dst, 0, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        ///<param name="srcIndex">A 32-bit integer that represents the index in the <c>src</c> array where copying begins.</param>
        ///<param name="dstIndex">A 32-bit integer that represents the index in the <c>dst</c> array where storing begins.</param>
        public static void Copy(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        ///<param name="srcIndex">A 32-bit integer that represents the index in the <c>src</c> array where copying begins.</param>
        ///<param name="dstIndex">A 32-bit integer that represents the index in the <c>dst</c> array where storing begins.</param>
        public static void Copy(ReadOnly src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        ///<param name="srcIndex">A 32-bit integer that represents the index in the <c>src</c> array where copying begins.</param>
        ///<param name="dstIndex">A 32-bit integer that represents the index in the <c>dst</c> array where storing begins.</param>
        public static void Copy(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        ///<param name="srcIndex">A 32-bit integer that represents the index in the <c>src</c> array where copying begins.</param>
        ///<param name="dstIndex">A 32-bit integer that represents the index in the <c>dst</c> array where storing begins.</param>
        public static void Copy(NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            CopySafe(src, srcIndex, dst, dstIndex, length);
        }

        ///<summary>Copies a range of elements from a source array to a destination array, starting from the source index and copying them to the destination index.</summary>
        ///<param name="src">The data to copy.</param>
        ///<param name="dst">The array that receives the data.</param>
        ///<param name="length">A 32-bit integer that represents the number of elements to copy. The integer must be equal to or greater than zero.</param>
        ///<param name="srcIndex">A 32-bit integer that represents the index in the <c>src</c> array where copying begins.</param>
        ///<param name="dstIndex">A 32-bit integer that represents the index in the <c>dst</c> array where storing begins.</param>
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

        ///<summary>Reinterpret and load data starting at underlying index as a different type.</summary>
        ///<param name="sourceIndex">Index in the underlying array where the load should start.</param>
        ///<returns>The loaded data.</returns>
        public U ReinterpretLoad<U>(int sourceIndex) where U : struct
        {
            CheckReinterpretLoadRange<U>(sourceIndex);
            byte* src_ptr = ((byte*)m_Buffer) + ((long)UnsafeUtility.SizeOf<T>()) * sourceIndex;
            return UnsafeUtility.ReadArrayElement<U>(src_ptr, 0);
        }

        ///<summary>Reinterpret and store data starting at underlying index as a different type.</summary>
        ///<param name="destIndex">Index in the underlying array where the data is to be stored.</param>
        ///<param name="data">The data to store.</param>
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

        ///<summary>Reinterpret a <see cref="Unity.Collections.NativeArray{T}" /> with a different data type (type punning).</summary>
        ///<remarks>If an expected element size isn't given, the sizes of <c>T</c> and <c>U</c> must match.
        ///
        ///When an expected element size is given, this method allows you to create a view into memory that has a different element size and length compared to the source array. For example, you can reinterpret an array of float triples as an array of 3D vector structs. The expected element size serves as a checkpoint so that the underlying element size in the source array doesn't change size, which would otherwise make all future uses of the reinterpreted array invalid.</remarks>
        ///<returns>An alias of the same array, reinterpreted as the target type.</returns>
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

        ///<summary>Reinterpret a <see cref="Unity.Collections.NativeArray{T}" /> with a different data type (type punning).</summary>
        ///<remarks>If an expected element size isn't given, the sizes of <c>T</c> and <c>U</c> must match.
        ///
        ///When an expected element size is given, this method allows you to create a view into memory that has a different element size and length compared to the source array. For example, you can reinterpret an array of float triples as an array of 3D vector structs. The expected element size serves as a checkpoint so that the underlying element size in the source array doesn't change size, which would otherwise make all future uses of the reinterpreted array invalid.</remarks>
        ///<param name="expectedTypeSize">The expected size (in bytes, as given by sizeof) of the current element type of the array.</param>
        ///<returns>An alias of the same array, reinterpreted as the target type.</returns>
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

        ///<summary>Gets a view into an array starting at the specified index.</summary>
        ///<param name="start">The start index of the sub array.</param>
        ///<param name="length">The length of the sub array.</param>
        ///<returns>A view into the array that aliases the original array, which can't be disposed.</returns>
        public NativeArray<T> GetSubArray(int start, int length)
        {
            CheckGetSubArrayArguments(start, length);
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(((byte*)m_Buffer) + ((long)UnsafeUtility.SizeOf<T>()) * start, length, Allocator.None);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, m_Safety);
            return result;
        }

        ///<summary>Casts a <see cref="Unity.Collections.NativeArray{T}" /> to read-only array.</summary>
        ///<returns>A read-only array.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(m_Buffer, m_Length, ref m_Safety);
        }

        ///<summary>Represents a <see cref="Unity.Collections.NativeArray{T}" /> interface constrained to read-only operations.</summary>
        [StructLayout(LayoutKind.Sequential)]
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [DebuggerDisplay("Length = {Length}")]
        [DebuggerTypeProxy(typeof(NativeArrayReadOnlyDebugView<>))]
        public struct ReadOnly : IEnumerable<T>
        {
            [NativeDisableUnsafePtrRestriction]
            [VisibleToOtherModules("UnityEditor.CoreModule")]
            internal void* m_Buffer;
            [VisibleToOtherModules("UnityEditor.CoreModule")]
            internal int   m_Length;

            [VisibleToOtherModules("UnityEditor.CoreModule")]
            internal AtomicSafetyHandle m_Safety;

            internal ReadOnly(void* buffer, int length, ref AtomicSafetyHandle safety)
            {
                m_Buffer = buffer;
                m_Length = length;
                m_Safety = safety;
            }


            ///<summary>Provides the number of elements in the <see cref="Unity.Collections.NativeArray{T}.ReadOnly" />.</summary>
            public int Length
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return m_Length;
                }
            }

            ///<summary>Copies all elements to a <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> or managed array of the same length.</summary>
            ///<param name="array">The destination array to copy to.</param>
            public void CopyTo(T[] array) => Copy(this, array);

            ///<summary>Copies all elements to a <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> or managed array of the same length.</summary>
            ///<param name="array">The destination array to copy to.</param>
            public void CopyTo(NativeArray<T> array) => Copy(this, array);

            ///<summary>Convert the data in a <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> to a managed array.</summary>
            ///<returns>A new managed array with the same contents as the given <see cref="Unity.Collections.NativeArray{T}.ReadOnly" />.</returns>
            public T[] ToArray()
            {
                var array = new T[m_Length];
                Copy(this, array, m_Length);
                return array;
            }

            ///<summary>Reinterpret a <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> with a different data type (type punning).</summary>
            ///<remarks>The sizes of <c>T</c> and <c>U</c> must match. You can use this method to create a view into memory that has a different element size 
            ///and length compared to the source array. For example, an array of float triples can be reinterpreted as an array of 3D vector structs.</remarks>
            ///<returns>An alias of the <see cref="Unity.Collections.NativeArray{T}.ReadOnly" />, but reinterpreted as the target type.</returns>
            public NativeArray<U>.ReadOnly Reinterpret<U>() where U : struct
            {
                CheckReinterpretSize<U>();
                return new NativeArray<U>.ReadOnly(m_Buffer, m_Length, ref m_Safety);
            }

            ///<summary>Provides read-only access to <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> elements by index.</summary>
            ///<remarks>The elements of the array are returned by value, not by reference. If you need the reference, use the <see cref="NativeArray{T}.ReadOnly.UnsafeElementAt(int)" /> method.</remarks>
            ///<seealso cref="ReadOnly.UnsafeElementAt" />
            public T this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckElementReadAccess(index);
                    return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                }
            }

            // This method does not copy T, but returns a readonly T.
            // It is marked as unsafe because the value returned by this method can become invalid at any time, for example, if the container was disposed.
            ///<summary>Provides read-only access to <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> elements by index.</summary>
            ///<remarks>This method is dangerous because it returns reference to the memory that can be destroyed (with <see cref="Dispose" />, for instance) and 
            ///if the <c>ref local</c> is accessed that might lead to undefined results including crashes.</remarks>
            ///<param name="index">The index of the element.</param>
            ///<returns>A <c>ref readonly</c> to a value of type <c>T</c>.</returns>
            ///<example nocheck="true">
            ///  <code><![CDATA[public readonly struct BigStruct
            ///{
            ///    public readonly long a;
            ///    public readonly long b;
            ///    public readonly long c;
            ///    public readonly long d;
            ///}
            ///
            ///static void ProcessByte(byte b)
            ///{
            ///    ...
            ///}
            ///
            ///static void ProcessBigStructWithoutCopy(in BigStruct bigStruct) // see 'in' modificator
            ///{
            ///    ...
            ///}
            ///
            ///static void Example()
            ///{
            ///    const int n = 32;
            ///
            ///    var nativeArrayOfBytes = new NativeArray<byte>(n, Allocator.Temp);
            ///    var nativeArrayOfBigStructures = new NativeArray<FixedString4096Bytes>(n, Allocator.Temp);
            ///
            ///    // ... fill the arrays with some data ...
            ///
            ///    var readOnlyBytes = nativeArrayOfBytes.AsReadOnly();
            ///    for (var i = 0; i < n; ++i)
            ///    {
            ///        ProcessByte(readOnlyBytes[i]);
            ///        //ProcessByte(readOnlyBytes.UnsafeElementAt(i)); is more expensive, since pointer on x64 platforms is 8 times bigger than a byte
            ///    }
            ///
            ///    var readOnlyBigStructures = nativeArrayOfBigStructures.AsReadOnly();
            ///    for (var i = 0; i < n; ++i)
            ///    {
            ///        //ProcessBigStructWithoutCopy(readOnlyBigStructures[i]); copy - expensive in this case
            ///        ProcessBigStructWithoutCopy(readOnlyBigStructures.UnsafeElementAt(i));
            ///    }
            ///
            ///    // dangerous part
            ///    ref var element = ref readOnlyBigStructures.UnsafeElementAt(4);
            ///
            ///    // element is valid here
            ///    ProcessBigStructWithoutCopy(element);
            ///
            ///    nativeArrayOfBigStructures.Dispose();
            ///
            ///    // access to element here is undefined, can lead to a crash or wrong results
            ///    // ProcessBigStructWithoutCopy(element); <-- do not do this!
            ///}
            ///]]></code>
            ///</example>
            public ref readonly T UnsafeElementAt(int index)
            {
                CheckElementReadAccess(index);
                return ref UnsafeUtility.ArrayElementAsRef<T>(m_Buffer, index);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void CheckElementReadAccess(int index)
            {
                if ((uint)index >= (uint)m_Length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {m_Length-1}).");
                }

                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }

            ///<summary>Indicates that a <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> has an allocated memory buffer.</summary>
            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Buffer != null;
            }

            [ExcludeFromDocs]
            public struct Enumerator : IEnumerator<T>
            {
                ReadOnly m_Array;
                int m_Index;
                T value;

                public Enumerator(in ReadOnly array)
                {
                    m_Array = array;
                    m_Index = -1;
                    value = default;
                }

                public void Dispose()
                {
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    m_Index++;
                    if (m_Index < m_Array.m_Length)
                    {
                        AtomicSafetyHandle.CheckReadAndThrow(m_Array.m_Safety);
                        value = UnsafeUtility.ReadArrayElement<T>(m_Array.m_Buffer, m_Index);
                        return true;
                    }
                    value = default;
                    return false;
                }

                public void Reset()
                {
                    m_Index = -1;
                }

                // Let NativeArray indexer check for out of range.
                public T Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => value;
                }

                object IEnumerator.Current => Current;
            }

            ///<summary>Gets an enumerator.</summary>
            ///<returns>The enumerator.</returns>
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

            ///<summary>Exposes <see cref="Unity.Collections.NativeArray{T}.ReadOnly" /> data as a <c>System.ReadOnlySpan&lt;T&gt;</c>.</summary>
            ///<returns>A <c>System.ReadOnlySpan&lt;T&gt;</c>.</returns>
            public readonly ReadOnlySpan<T> AsReadOnlySpan()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                return new ReadOnlySpan<T>(m_Buffer, m_Length);
            }

            ///<exclude />
            public static implicit operator ReadOnlySpan<T>(in ReadOnly source)
            {
                return source.AsReadOnlySpan();
            }
        }

        ///<summary>Exposes <see cref="Unity.Collections.NativeArray{T}" /> data as a <c>System.Span&lt;T&gt;</c>.</summary>
        ///<returns>A read-write <c>System.Span&lt;T&gt;</c>.</returns>
        [WriteAccessRequired]
        public readonly Span<T> AsSpan()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            return new Span<T>(m_Buffer, m_Length);
        }

        ///<summary>Exposes <see cref="Unity.Collections.NativeArray{T}" /> data as a <c>System.ReadOnlySpan&lt;T&gt;</c>.</summary>
        ///<returns>A <c>System.ReadOnlySpan&lt;T&gt;</c>.</returns>
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return new ReadOnlySpan<T>(m_Buffer, m_Length);
        }

        ///<exclude />
        public static implicit operator Span<T>(in NativeArray<T> source)
        {
            return source.AsSpan();
        }

        ///<exclude />
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
    [NativeClass(null)]
    internal struct NativeArrayDisposeJob : IJob
    {
        internal NativeArrayDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }

        [RequiredByNativeCode]
        internal static void RegisterNativeArrayDisposeJobReflectionData()
        {
            // Necessary so we may schedule NativeArrayDisposeJob from
            // burst compiled codepaths
            IJobExtensions.EarlyJobInit<NativeArrayDisposeJob>();
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
    ///<summary>Contains &lt;a href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/unsafe"&gt;unsafe&lt;/a&gt; methods for 
    ///working with <see cref="Unity.Collections.NativeArray{T}"> NativeArray</see> instances.</summary>
    ///<remarks>This class contains methods that you can use to perform unsafe operations that ignore the job safety system. For example, if you are
    ///implementing your own [custom native container](xref:job-system-custom-nativecontainer), you might want to call <c>NativeArray</c> methods that cause the safety 
    ///system to produce errors, even though you're implementing these methods safely. <c>NativeArrayUnsafeUtility</c> allows you to perform these operations without 
    ///triggering errors in the safety system.</remarks>
    public static class NativeArrayUnsafeUtility
    {
        ///<summary>Gets the <see cref="AtomicSafetyHandle" /> that is used for safety control on a NativeArray.</summary>
        ///<param name="array">The NativeArray to check.</param>
        ///<returns>The <see cref="AtomicSafetyHandle" /> of the given NativeArray.</returns>
        public static AtomicSafetyHandle GetAtomicSafetyHandle<T>(NativeArray<T> array) where T : struct
        {
            return array.m_Safety;
        }

        ///<summary>Sets a new <see cref="AtomicSafetyHandle" /> on a <see cref="NativeArray{T}" />.</summary>
        ///<param name="array">The NativeArray to set a new handle on.</param>
        ///<param name="safety">The new AtomicSafetyHandle to set.</param>
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

        ///<summary>Converts a buffer to a NativeArray.</summary>
        ///<remarks>You can use this method to turn an existing buffer into a NativeArray. Ownership of the data is controlled via the allocation 
        ///strategy that the allocator argument provides. Use <see cref="Allocator.None" /> if the data is owned externally, and the other arguments to transfer control to the 
        ///NativeArray.</remarks>
        ///<param name="dataPointer">Pointer to the preallocated data.</param>
        ///<param name="length">Number of elements. The length of the data in bytes is computed automatically from this.</param>
        ///<param name="allocator">The <see cref="Unity.Collections.Allocator" /> type to use.</param>
        ///<returns>A new NativeArray, allocated with the given <c>allocator</c> strategy and wrapping the provided data.</returns>
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

        ///<summary>Converts a buffer to a NativeArray.</summary>
        ///<remarks>You can use this method to turn an existing buffer into a NativeArray. Ownership of the data is controlled via the allocation 
        ///strategy that the allocator argument provides. Use <see cref="Allocator.None" /> if the data is owned externally, and the other arguments to transfer control to the 
        ///NativeArray.</remarks>
        ///<param name="data">Span of the preallocated data.</param>
        ///<param name="allocator">The <see cref="Unity.Collections.Allocator" /> type to use.</param>
        ///<returns>A new NativeArray, allocated with the given <c>allocator</c> strategy and wrapping the provided data.</returns>
        public static unsafe NativeArray<T> ConvertExistingDataToNativeArray<T>(Span<T> data, Allocator allocator) where T : unmanaged
        {
            CheckConvertArguments<T>(data.Length);
            fixed (T* addr = data)
            {
                var newArray = new NativeArray<T>
                {
                    m_Buffer = addr,
                    m_Length = data.Length,
                    m_AllocatorLabel = allocator,

                    m_MinIndex = 0,
                    m_MaxIndex = data.Length - 1,
                };

                return newArray;
            }
        }

        ///<summary>Gets the pointer to the memory buffer owner of a <see cref="NativeArray{T}" />, and checks whether there is write access to the NativeArray. If there is no write access to the NativeArray, an 
        ///InvalidOperationException is thrown.</summary>
        ///<param name="nativeArray">The NativeArray to check.</param>
        ///<returns>The memory buffer pointer of the NativeArray.</returns>
        public static unsafe void* GetUnsafePtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckWriteAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        ///<summary>Gets a pointer to the memory buffer of a <see cref="NativeArray{T}" /> or <see cref="NativeArray{T}.ReadOnly" />.</summary>
        ///<remarks>When ENABLE_UNITY_COLLECTIONS_CHECKS is set (which is always the case in the Editor, but never in a built player), this method 
        ///checks that the <c>AtomicSafetyHandle</c> associated with the NativeContainer can be read from and isn't written to from another thread. If it can't be read, 
        ///a <see cref="System.InvalidOperationException" /> is raised. While you can write to the returned pointer, this is generally unsafe. When this method call succeeds, you're 
        ///only guaranteed that reading from this pointer is safe. Writing to this pointer might lead to race conditions and crashes later during program execution.</remarks>
        ///<param name="nativeArray">The NativeArray to check.</param>
        ///<returns>The memory buffer pointer of the NativeArray.</returns>
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeArray<T> nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        ///<summary>Gets a pointer to the memory buffer of a <see cref="NativeArray{T}" /> or <see cref="NativeArray{T}.ReadOnly" />.</summary>
        ///<remarks>When ENABLE_UNITY_COLLECTIONS_CHECKS is set (which is always the case in the Editor, but never in a built player), this method 
        ///checks that the <c>AtomicSafetyHandle</c> associated with the NativeContainer can be read from and isn't written to from another thread. If it can't be read, 
        ///a <see cref="System.InvalidOperationException" /> is raised. While you can write to the returned pointer, this is generally unsafe. When this method call succeeds, you're 
        ///only guaranteed that reading from this pointer is safe. Writing to this pointer might lead to race conditions and crashes later during program execution.</remarks>
        ///<param name="nativeArray">The NativeArray to check.</param>
        ///<returns>The memory buffer pointer of the NativeArray.</returns>
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeArray<T>.ReadOnly nativeArray) where T : struct
        {
            AtomicSafetyHandle.CheckReadAndThrow(nativeArray.m_Safety);
            return nativeArray.m_Buffer;
        }

        ///<summary>Gets the pointer to the data owner of a <see cref="NativeArray{T}" />, without performing checks.</summary>
        ///<param name="nativeArray">The NativeArray to check.</param>
        ///<returns>The memory buffer pointer of the NativeArray.</returns>
        public static unsafe void* GetUnsafeBufferPointerWithoutChecks<T>(NativeArray<T> nativeArray) where T : struct
        {
            return nativeArray.m_Buffer;
        }
    }
}

