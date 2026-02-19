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
    [VisibleToOtherModules]
    readonly ref struct RentSpanUnmanaged<T> where T : unmanaged
    {
        readonly byte[] m_Array;
        public readonly Span<T> Span;

        public RentSpanUnmanaged(int length, bool clear = false)
        {
            var size = length * UnsafeUtility.SizeOf<T>();
            m_Array = ArrayPool<byte>.Shared.Rent(size);
            Span = MemoryMarshal.Cast<byte, T>(new Span<byte>(m_Array, 0, size));
            if (clear)
                Span.Clear();
        }

        public void Dispose() => ArrayPool<byte>.Shared.Return(m_Array);

        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(in RentSpanUnmanaged<T> rentSpan) => rentSpan.Span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(in RentSpanUnmanaged<T> rentSpan) => rentSpan.Span;
    }
}
