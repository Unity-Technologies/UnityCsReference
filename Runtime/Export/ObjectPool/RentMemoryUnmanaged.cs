// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace UnityEngine.Pool
{
    [VisibleToOtherModules]
    readonly struct RentMemoryUnmanaged<T> : IDisposable where T : unmanaged
    {
        readonly T[] m_Array;
        public readonly Memory<T> Memory;

        public RentMemoryUnmanaged(int length, bool clear = false)
        {
            m_Array = ArrayPool<T>.Shared.Rent(length);
            Memory = new Memory<T>(m_Array, 0, length);
            if (clear)
                Memory.Span.Clear();
        }

        public void Dispose() => ArrayPool<T>.Shared.Return(m_Array);

        public Span<T>.Enumerator GetEnumerator() => Memory.Span.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Memory<T>(in RentMemoryUnmanaged<T> rentMemory) => rentMemory.Memory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<T>(in RentMemoryUnmanaged<T> rentMemory) => rentMemory.Memory;
    }
}
