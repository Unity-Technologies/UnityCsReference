// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly ref struct ManagedSpanWrapper
    {
        public readonly unsafe void* begin;
        public readonly int length;

        public unsafe ManagedSpanWrapper(void* begin, int length)
        {
            this.begin = begin;
            this.length = length;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Span<T> ToSpan<T>(ManagedSpanWrapper spanWrapper)
        {
            return new Span<T>(spanWrapper.begin, spanWrapper.length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ReadOnlySpan<T> ToReadOnlySpan<T>(ManagedSpanWrapper spanWrapper)
        {
            return new ReadOnlySpan<T>(spanWrapper.begin, spanWrapper.length);
        }
    }
}
