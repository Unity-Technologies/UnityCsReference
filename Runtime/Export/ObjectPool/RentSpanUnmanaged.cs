// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.Pool
{
    /// <summary>
    /// Rents a byte array from <see cref="ArrayPool{byte}.Shared"/> and exposes it as a <see cref="Span{T}"/>,
    /// or wraps a stack-allocated span. Avoids heap allocations and GC pressure.
    /// </summary>
    /// <remarks>
    /// For unmanaged types, this is more efficient than <see cref="RentSpan{T}"/> because it rents from the shared byte pool
    /// and casts to the target type, reducing pool fragmentation. Supports both pooled and stack-allocated backing storage.
    /// </remarks>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    [VisibleToOtherModules]
    readonly ref struct RentSpanUnmanaged<T> where T : unmanaged
    {
        readonly byte[] m_Array;

        /// <summary>
        /// Gets the span of elements. The span's length matches the requested size exactly.
        /// </summary>
        public readonly Span<T> Span;

        /// <summary>
        /// Creates a wrapper around a stack-allocated span. No array is rented from the pool.
        /// </summary>
        /// <remarks>
        /// Use this constructor when you want to use stack allocation for small, short-lived buffers.
        /// The <see cref="Dispose"/> method becomes a no-op when using this constructor.
        /// <code>
        /// using var buffer = new RentSpanUnmanaged&lt;int&gt;(stackalloc int[16]);
        /// buffer.Span[0] = 42;
        /// </code>
        /// </remarks>
        /// <param name="stackSpan">The stack-allocated span to wrap.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RentSpanUnmanaged(Span<T> stackSpan)
        {
            m_Array = null;
            Span = stackSpan;
        }

        /// <summary>
        /// Rents a byte array from <see cref="ArrayPool{byte}.Shared"/> and exposes it as a <see cref="Span{T}"/>.
        /// </summary>
        /// <remarks>
        /// The returned span has exactly <paramref name="length"/> elements. The underlying array may be larger
        /// but is not accessible. Call <see cref="Dispose"/> to return the array to the pool.
        /// </remarks>
        /// <param name="length">The number of elements required.</param>
        /// <param name="clear">If true, the span is zeroed before being returned.</param>
        public RentSpanUnmanaged(int length, bool clear = false)
        {
            var size = length * UnsafeUtility.SizeOf<T>();
            m_Array = ArrayPool<byte>.Shared.Rent(size);
            Span = MemoryMarshal.Cast<byte, T>(new Span<byte>(m_Array, 0, size));
            if (clear)
                Span.Clear();
        }

        /// <summary>
        /// Returns the rented array to <see cref="ArrayPool{byte}.Shared"/>, if one was rented.
        /// Stack-allocated spans require no cleanup.
        /// </summary>
        public void Dispose()
        {
            if (m_Array != null)
                ArrayPool<byte>.Shared.Return(m_Array);
        }

        /// <summary>
        /// Returns an enumerator for the span.
        /// </summary>
        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

        /// <summary>
        /// Implicitly converts to <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(in RentSpanUnmanaged<T> rentSpan) => rentSpan.Span;

        /// <summary>
        /// Implicitly converts to <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(in RentSpanUnmanaged<T> rentSpan) => rentSpan.Span;
    }
}
