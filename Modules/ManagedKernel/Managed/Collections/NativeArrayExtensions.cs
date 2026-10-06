// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using static Unity.Collections.AllocatorManager;

namespace Unity.Collections
{
    /// <summary>
    /// Extension methods for NativeArray.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe static class NativeArrayExtensions
    {
        /// <summary>
        /// Provides a Burst compatible id for NativeArray<typeparamref name="T"/> types. Used by the Job Safety System.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public struct NativeArrayStaticId<T>
            where T : unmanaged
        {
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeArray<T>>();
        }

        /// <summary>
        /// Returns true if a particular value is present in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="container">The container to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this container.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this NativeArray<T> container, U value) where T : unmanaged, IEquatable<U> => IndexOf(container.AsReadOnlySpan(), value) != -1;

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="container">The container to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value in this container. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this NativeArray<T> container, U value) where T : unmanaged, IEquatable<U> => IndexOf(container.AsReadOnlySpan(), value);

        /// <summary>
        /// Returns true if a particular value is present in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="container">The container to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this container.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this NativeArray<T>.ReadOnly container, U value) where T : unmanaged, IEquatable<U> => IndexOf(container.AsReadOnlySpan(), value) != -1;

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">The container to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value in this container. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this NativeArray<T>.ReadOnly container, U value) where T : unmanaged, IEquatable<U> => IndexOf(container.AsReadOnlySpan(), value);

        /// <summary>
        /// Returns true if a particular value is present in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="container">The container to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this container.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this NativeList<T> container, U value) where T : unmanaged, IEquatable<U> => IndexOf(container.AsReadOnlySpan(), value) != -1;

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">The container to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value in this container. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this NativeList<T> container, U value) where T : unmanaged, IEquatable<U> => IndexOf(container.AsReadOnlySpan(), value);

        /// <summary>
        /// Returns true if a particular value is present in a buffer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the buffer.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="ptr">The buffer.</param>
        /// <param name="length">Number of elements in the buffer.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in the buffer.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(T* ptr, int length, U value) where T : unmanaged, IEquatable<U> => IndexOf(ptr, length, value) != -1;

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in a buffer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the buffer.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="ptr">A buffer.</param>
        /// <param name="length">Number of elements in the buffer.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value in the buffer. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(T* ptr, int length, U value) where T : unmanaged, IEquatable<U> => IndexOf(new ReadOnlySpan<T>(ptr, length), value);

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in a buffer.
        /// </summary>
        /// <typeparam name="T">The type of elements in the buffer.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="roSpan">A ReadOnlySpan.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value in the buffer. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(ReadOnlySpan<T> roSpan, U value)
            where T : unmanaged, IEquatable<U>
        {
            var count = roSpan.Length;
            if (count > 0)
            {
                fixed (T* ptr = &roSpan[0])
                {
                    for (int i = 0; i != count; i++)
                    {
                        if (UnsafeUtility.ReadArrayElement<T>(ptr, i).Equals(value))
                            return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns true if a particular value is present in this container.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <typeparam name="U">The value type.</typeparam>
        /// <param name="roSpan">A ReadOnlySpan.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this container.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this ReadOnlySpan<T> roSpan, U value) where T : unmanaged, IEquatable<U> => IndexOf(roSpan, value) != -1;

        /// <summary>
        /// Copies all elements of specified container to array.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <param name="container">Container to copy to.</param>
        /// <param name="other">An container to copy into this array.</param>
        /// <exception cref="ArgumentException">Thrown if the array and container have unequal length.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static void CopyFrom<T>(this ref NativeArray<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            container.CopyFrom(other.AsArray());
        }

        /// <summary>
        /// Copies all elements of specified container to array.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <param name="container">Container to copy to.</param>
        /// <param name="other">An container to copy into this array.</param>
        /// <exception cref="ArgumentException">Thrown if the array and container have unequal length.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static void CopyFrom<T>(this ref NativeArray<T> container, in NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            using (var array = other.ToNativeArray(Allocator.TempJob))
            {
                container.CopyFrom(array);
            }
        }

        /// <summary>
        /// Copies all elements of specified container to array.
        /// </summary>
        /// <typeparam name="T">The type of elements in this container.</typeparam>
        /// <param name="container">Container to copy to.</param>
        /// <param name="other">An container to copy into this array.</param>
        /// <exception cref="ArgumentException">Thrown if the array and container have unequal length.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static void CopyFrom<T>(this ref NativeArray<T> container, in UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            using (var array = other.ToNativeArray(Allocator.TempJob))
            {
                container.CopyFrom(array);
            }
        }

        /// <summary>
        /// Returns the reinterpretation of this array into another kind of NativeArray.
        /// See [Array reinterpretation](https://docs.unity3d.com/Packages/com.unity.collections@latest?subfolder=/manual/allocation.html#array-reinterpretation).
        /// </summary>
        /// <param name="array">The array to reinterpret.</param>
        /// <typeparam name="T">Type of elements in the array.</typeparam>
        /// <typeparam name="U">Type of elements in the reinterpreted array.</typeparam>
        /// <returns>The reinterpretation of this array into another kind of NativeArray.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this array's capacity cannot be evenly divided by `sizeof(U)`.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static NativeArray<U> Reinterpret<T, U>(this NativeArray<T> array)
            where U : unmanaged
            where T : unmanaged
        {
            var tSize = sizeof(T);
            var uSize = UnsafeUtility.SizeOf<U>();

            var byteLen = ((long)array.Length) * tSize;
            var uLen = byteLen / uSize;

            CheckReinterpretSize<T, U>(ref array);

            var ptr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<U>(ptr, (int)uLen, Allocator.None);

            var handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, handle);

            return result;
        }

        /// <summary>
        /// Returns true if this array and another have equal length and content.
        /// </summary>
        /// <typeparam name="T">The type of the source array's elements.</typeparam>
        /// <param name="container">The array to compare for equality.</param>
        /// <param name="other">The other array to compare for equality.</param>
        /// <returns>True if the arrays have equal length and content.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static bool ArraysEqual<T>(this NativeArray<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            if (container.Length != other.Length)
                return false;

            for (int i = 0; i != container.Length; i++)
            {
                if (!container[i].Equals(other[i]))
                    return false;
            }

            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckReinterpretSize<T, U>(ref NativeArray<T> array)
            where U : unmanaged
            where T : unmanaged
        {
            var tSize = sizeof(T);
            var uSize = UnsafeUtility.SizeOf<U>();

            var byteLen = ((long)array.Length) * tSize;
            var uLen = byteLen / uSize;

            if (uLen * uSize != byteLen)
            {
                throw new InvalidOperationException($"Types {typeof(T)} (array length {array.Length}) and {typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal static void Initialize<T>(ref this NativeArray<T> array,
                                            int length,
                                            AllocatorManager.AllocatorHandle allocator,
                                            NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : unmanaged
        {
            AllocatorHandle handle = allocator;
            array = default;
            array.m_Buffer = handle.AllocateStruct(default(T), length);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator.IsAutoDispose ? Allocator.None : allocator.ToAllocator;
            if (options == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(array.m_Buffer, (long)array.m_Length * sizeof(T));
            }

            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
            array.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.SetStaticSafetyId<NativeArray<T>>(ref array.m_Safety, ref NativeArrayStaticId<T>.s_staticSafetyId.Data);
            handle.AddSafetyHandle(array.m_Safety);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        internal static void Initialize<T, U>(ref this NativeArray<T> array,
                                                int length,
                                                ref U allocator,
                                                NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : unmanaged
            where U : unmanaged, AllocatorManager.IAllocator
        {
            array = default;
            array.m_Buffer = allocator.AllocateStruct(default(T), length);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator.IsAutoDispose ? Allocator.None : allocator.ToAllocator;
            if (options == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(array.m_Buffer, (long)array.m_Length * sizeof(T));
            }

            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
            array.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.ToAllocator);

            CollectionHelper.SetStaticSafetyId<NativeArray<T>>(ref array.m_Safety, ref NativeArrayStaticId<T>.s_staticSafetyId.Data);
            allocator.Handle.AddSafetyHandle(array.m_Safety);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal static void DisposeCheckAllocator<T>(ref this NativeArray<T> array)
            where T : unmanaged
        {
            if (array.m_Buffer == null)
            {
                throw new ObjectDisposedException("The NativeArray is already disposed.");
            }

            if (!AllocatorManager.IsCustomAllocator(array.m_AllocatorLabel))
            {
                array.Dispose();
            }
            else
            {
                AtomicSafetyHandle.DisposeHandle(ref array.m_Safety);
                AllocatorManager.Free(array.m_AllocatorLabel, array.m_Buffer);
                array.m_AllocatorLabel = Allocator.Invalid;

                array.m_Buffer = null;
            }
        }
    }
}
