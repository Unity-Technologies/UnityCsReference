// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace UnityEngine.Pool
{
    /// <summary>
    /// Rents an array from <see cref="ArrayPool{T}.Shared"/> and exposes it as a <see cref="Span{T}"/>,
    /// avoiding heap allocations and GC pressure.
    /// </summary>
    /// <remarks>
    /// Use this for managed types (classes). For unmanaged types (structs, primitives), use <see cref="RentSpanUnmanaged{T}"/> instead,
    /// which pools more efficiently by renting byte arrays and casting them to the desired type.
    /// </remarks>
    /// <typeparam name="T">The managed element type (must be a class).</typeparam>
    [VisibleToOtherModules]
    readonly ref struct RentSpan<T> where T : class
    {
        readonly T[] m_Array;

        /// <summary>
        /// Gets the span of elements. The span's length matches the requested size exactly.
        /// </summary>
        public readonly Span<T> Span;

        /// <summary>
        /// Rents an array from <see cref="ArrayPool{T}.Shared"/> and exposes it as a <see cref="Span{T}"/>.
        /// </summary>
        /// <remarks>
        /// The returned span has exactly <paramref name="length"/> elements. The underlying array may be larger
        /// but is not accessible. Call <see cref="Dispose"/> to return the array to the pool.
        /// </remarks>
        /// <param name="length">The number of elements required.</param>
        /// <param name="clear">If true, all elements are set to null before being returned.</param>
        public RentSpan(int length, bool clear = false)
        {
            m_Array = ArrayPool<T>.Shared.Rent(length);
            Span = m_Array.AsSpan(0, length);
            if (clear)
                Span.Clear();
        }

        /// <summary>
        /// Returns the rented array to <see cref="ArrayPool{T}.Shared"/>.
        /// </summary>
        public void Dispose() => ArrayPool<T>.Shared.Return(m_Array);

        /// <summary>
        /// Returns an enumerator for the span.
        /// </summary>
        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

        /// <summary>
        /// Implicitly converts to <see cref="Span{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(in RentSpan<T> rentSpan) => rentSpan.Span;

        /// <summary>
        /// Implicitly converts to <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(in RentSpan<T> rentSpan) => rentSpan.Span;

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> view over the rented array with the correct logical length.
        /// </summary>
        /// <remarks>
        /// Use this when you need to pass the buffer to async methods or APIs that require <see cref="Memory{T}"/>.
        /// </remarks>
        public unsafe Memory<T> AsMemory() => new(m_Array, 0, Span.Length);
    }
}
