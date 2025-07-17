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
    internal readonly ref struct RentSpan<T> where T : class
    {
        readonly T[] m_Array;
        public readonly Span<T> Span;

        public RentSpan(int length, bool clear = false)
        {
            m_Array = ArrayPool<T>.Shared.Rent(length);
            Span = m_Array.AsSpan(0, length);
            if (clear)
                Span.Clear();
        }

        public void Dispose() => ArrayPool<T>.Shared.Return(m_Array);

        public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(in RentSpan<T> rentSpan) => rentSpan.Span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(in RentSpan<T> rentSpan) => rentSpan.Span;

        public unsafe Memory<T> AsMemory() => new(m_Array, 0, Span.Length);
    }
}
