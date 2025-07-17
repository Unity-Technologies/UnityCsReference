// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [VisibleToOtherModules("UnityEngine.HierarchyModule", "UnityEditor.HierarchyModule")]
    internal readonly struct RentMemory<T> : IDisposable where T : class
    {
        readonly T[] m_Array;
        public readonly Memory<T> Memory;

        public RentMemory(int length, bool clear = false)
        {
            m_Array = ArrayPool<T>.Shared.Rent(length);
            Memory = m_Array.AsMemory(0, length);
            if (clear)
                Memory.Span.Clear();
        }

        public Span<T>.Enumerator GetEnumerator() => Memory.Span.GetEnumerator();

        public void Dispose() => ArrayPool<T>.Shared.Return(m_Array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Memory<T>(in RentMemory<T> rentMemory) => rentMemory.Memory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyMemory<T>(in RentMemory<T> rentMemory) => rentMemory.Memory;
    }
}
