// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Provides utility methods for unsafe, untyped buffers.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe static class UnsafeUtilityExtensions
    {
        /// <summary>
        /// Swaps bytes between two buffers.
        /// </summary>
        /// <param name="ptr">A buffer.</param>
        /// <param name="otherPtr">Another buffer.</param>
        /// <param name="size">The number of bytes to swap.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if the two ranges of bytes to swap overlap in memory.</exception>
        internal static void MemSwap(void* ptr, void* otherPtr, long size)
        {
            byte* dst = (byte*) ptr;
            byte* src = (byte*) otherPtr;

            CheckMemSwapOverlap(dst, src, size);

            var tmp = stackalloc byte[1024];

            while (size > 0)
            {
                var numBytes = math.min(size, 1024);
                UnsafeUtility.MemCpy(tmp, dst, numBytes);
                UnsafeUtility.MemCpy(dst, src, numBytes);
                UnsafeUtility.MemCpy(src, tmp, numBytes);

                size -= numBytes;
                src += numBytes;
                dst += numBytes;
            }
        }

        /// <summary>
        /// Reads an element from a buffer after bounds checking.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="source">The buffer to read from.</param>
        /// <param name="index">The index of the element.</param>
        /// <param name="capacity">The buffer capacity (in number of elements). Used for the bounds checking.</param>
        /// <returns>The element read from the buffer.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe static T ReadArrayElementBoundsChecked<T>(void* source, int index, int capacity)
            where T : unmanaged
        {
            CheckIndexRange(index, capacity);

            return UnsafeUtility.ReadArrayElement<T>(source, index);
        }

        /// <summary>
        /// Writes an element to a buffer after bounds checking.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="destination">The buffer to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="index">The index at which to store the element.</param>
        /// <param name="capacity">The buffer capacity (in number of elements). Used for the bounds checking.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe static void WriteArrayElementBoundsChecked<T>(void* destination, int index, T value, int capacity)
            where T : unmanaged
        {
            CheckIndexRange(index, capacity);

            UnsafeUtility.WriteArrayElement<T>(destination, index, value);
        }

        /// <summary>
        /// Returns the address of a read-only reference.
        /// </summary>
        /// <typeparam name="T">The type of referenced value.</typeparam>
        /// <param name="value">A read-only reference.</param>
        /// <returns>A pointer to the referenced value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static void* AddressOf<T>(in T value)
            where T : unmanaged
        {
            return System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AsRef(in value));
        }

        /// <summary>
        /// Returns a read-write reference from a read-only reference.
        /// <remarks>Useful when you want to pass an `in` arg (read-only reference) where a `ref` arg (read-write reference) is expected.
        /// Do not mutate the referenced value, as doing so may break the runtime's assumptions.</remarks>
        /// </summary>
        /// <typeparam name="T">The type of referenced value.</typeparam>
        /// <param name="value">A read-only reference.</param>
        /// <returns>A read-write reference to the value referenced by `item`.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static ref T AsRef<T>(in T value)
            where T : unmanaged
        {
            return ref System.Runtime.CompilerServices.Unsafe.AsRef(in value);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static unsafe void CheckMemSwapOverlap(byte* dst, byte* src, long size)
        {
            if (dst + size > src && src + size > dst)
            {
                throw new InvalidOperationException("MemSwap memory blocks are overlapped.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckIndexRange(int index, int capacity)
        {
            if ((index > capacity - 1) || (index < 0))
            {
                throw new IndexOutOfRangeException(
                    $"Attempt to read or write from array index {index}, which is out of bounds. Array capacity is {capacity}. "
                    +"This may lead to a crash, data corruption, or reading invalid data."
                    );
            }
        }
    }
}
