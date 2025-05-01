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
                ListPrivateFieldAccess<T> tListAccess = UnsafeUtility.As<List<T>, ListPrivateFieldAccess<T>>(ref list);
                tListAccess._size = count;
                tListAccess._version++;
            }
        }

        // tiny helpers
        public static int SafeLength(System.Array values) { return values != null ? values.Length : 0; }
        public static int SafeLength<T>(List<T> values) { return values != null ? values.Count : 0; }

        [Obsolete("Use ExtractArrayFromList", false)]
        public static T[] ExtractArrayFromListT<T>(List<T> list) { return ExtractArrayFromList(list); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ExtractArrayFromList<T>(List<T> list)
        {
            if (list == null)
                return null;

            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            return tListAccess._items;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> CreateSpan<T>(List<T> list)
        {
            if (list == null)
                return default;

            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            return new Span<T>(tListAccess._items, 0, tListAccess._size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> CreateReadOnlySpan<T>(List<T> list)
        {
            if (list == null)
                return default;

            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            return new ReadOnlySpan<T>(tListAccess._items, 0, tListAccess._size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetListContents<T>(List<T> list, ReadOnlySpan<T> span)
        {
            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            tListAccess._items = span.ToArray();
            tListAccess._size = span.Length;
            tListAccess._version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetListContents<T>(List<T> list, T[] array)
        {
            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            tListAccess._items = array;
            tListAccess._size = array.Length;
            tListAccess._version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetListSize<T>(List<T> list, int size)
        {
            if (list.Capacity < size) throw new ArgumentException($"Resetting to {size} which is bigger than capacity {list.Capacity} is not allowed!");

            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && tListAccess._size > size)
                Array.Clear(tListAccess._items, size, tListAccess._size - size);

            tListAccess._size = size;
            tListAccess._version++;
        }

        [RequiredByNativeCode]
        private static Array PrepareListForNativeFill(object list, Type elementType, int newSize)
        {
            var listPrivateFieldAccess = UnsafeUtility.As<ListPrivateFieldAccess<byte>>(list);
            ref byte[] array = ref listPrivateFieldAccess._items;
            int capacity = array.Length;
            int previousCount = listPrivateFieldAccess._size;

            // If there is less capacity than is required, create a new array of the required count.
            if (capacity < newSize)
            {
                array = UnsafeUtility.As<byte[]>(Array.CreateInstance(elementType, newSize));
            }
            else if (previousCount > newSize)
            {
                // Otherwise it means that there is enough capacity. Then, if the previous count
                // is greater than the current count, it means that elements in between them must be cleared.
                Array.Clear(array as Array, newSize, previousCount - newSize); // Forcing the call to pass array as Array type. If an overload is introduced in the future, we want to avoid it as the element type is unknown
            }

            listPrivateFieldAccess._size = newSize;
            listPrivateFieldAccess._version++;

            return array;
        }

        // This is a helper class to allow the binding code to manipulate the internal fields of
        // System.Collections.Generic.List.  The field order below must not be changed.
        private class ListPrivateFieldAccess<T>
        {
#pragma warning disable CS0649
#pragma warning disable CS8618
            internal T[] _items; // Do not rename (binary serialization)
#pragma warning restore CS8618
            internal int _size; // Do not rename (binary serialization)
            internal int _version; // Do not rename (binary serialization)
#pragma warning restore CS0649
        }
    }
}
