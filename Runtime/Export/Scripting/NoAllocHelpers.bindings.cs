// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine
{
    // some helpers to handle List<T> in c# api (used for no-alloc apis where user provides list and we fill it):
    //   on il2cpp/mono we can "resize" List<T> (up to Capacity, sure, but this is/should-be handled higher level)
    //   also we can easily "convert" List<T> to System.Array
    // NB .net backend is treated as second-class citizen going through ToArray call
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
        public static int SafeLength(System.Array values)           { return values != null ? values.Length : 0; }
        public static int SafeLength<T>(List<T> values)             { return values != null ? values.Count : 0; }

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
        public static void ResetListContents<T>(List<T> list, ReadOnlySpan<T> span) 
        {
            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            tListAccess._items = span.ToArray();
            tListAccess._size = span.Length;
            tListAccess._version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetListSize<T>(List<T> list, int size) where T: unmanaged
        {
            Debug.Assert(list.Capacity >= size);

            var tListAccess = UnsafeUtility.As<ListPrivateFieldAccess<T>>(list);
            tListAccess._size = size;
            tListAccess._version++;
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
