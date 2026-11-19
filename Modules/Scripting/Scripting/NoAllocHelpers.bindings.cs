// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // some helpers to handle List<T> in c# api (used for no-alloc apis where user provides list and we fill it):
    //   on il2cpp/mono we can "resize" List<T> (up to Capacity, sure, but this is/should-be handled higher level)
    //   also we can easily "convert" List<T> to System.Array
    // NB .net backend is treated as second-class citizen going through ToArray call
    [VisibleToOtherModules]
    internal static class NoAllocHelpers
    {
        public static void EnsureListElemCount<T>(List<T> list, int count)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (count < 0)
                throw new ArgumentException("invalid size to resize.", "list");

            list.Clear();

            // make sure capacity is enough (that's where alloc WILL happen if needed)
            if (list.Capacity < count)
                list.Capacity = count;

            if (count != list.Count)
            {
                ListPrivateFieldAccess<T>.Size(list) = count;
                ListPrivateFieldAccess<T>.Version(list)++;
            }
        }

        // tiny helpers
        public static int SafeLength(System.Array values) { return values != null ? values.Length : 0; }
        public static int SafeLength<T>(List<T> values) { return values != null ? values.Count : 0; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ExtractArrayFromList<T>(List<T> list)
        {
            if (list == null)
                return null;

            return ListPrivateFieldAccess<T>.Items(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> CreateSpan<T>(List<T> list)
        {
            if (list == null)
                return default;

            return new Span<T>(ListPrivateFieldAccess<T>.Items(list), 0, ListPrivateFieldAccess<T>.Size(list));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> CreateReadOnlySpan<T>(List<T> list)
        {
            if (list == null)
                return default;

            return new ReadOnlySpan<T>(ListPrivateFieldAccess<T>.Items(list), 0, ListPrivateFieldAccess<T>.Size(list));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetListSize<T>(List<T> list, int size)
        {
            if (list.Capacity < size) throw new ArgumentException($"Resetting to {size} which is bigger than capacity {list.Capacity} is not allowed!");

            ref int listSize = ref ListPrivateFieldAccess<T>.Size(list);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && listSize > size)
                Array.Clear(ListPrivateFieldAccess<T>.Items(list), size, listSize - size);

            listSize = size;
            ListPrivateFieldAccess<T>.Version(list)++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvalidateListEnumerators<T>(List<T> list)
        {
            ListPrivateFieldAccess<T>.Version(list)++;
        }

        // Accessors for the internal fields of System.Collections.Generic.List
        private static class ListPrivateFieldAccess<T>
        {
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
            public static extern ref T[] Items(List<T> list);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_size")]
            public static extern ref int Size(List<T> list);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_version")]
            public static extern ref int Version(List<T> list);
        }
    }
}
