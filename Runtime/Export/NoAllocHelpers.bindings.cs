// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // some helpers to handle List<T> in c# api (used for no-alloc apis where user provides list and we fill it):
    //   on il2cpp/mono we can "resize" List<T> (up to Capacity, sure, but this is/should-be handled higher level)
    //   also we can easily "convert" List<T> to System.Array
    // NB .net backend is treated as second-class citizen going through ToArray call
    [NativeHeader("Runtime/Export/NoAllocHelpers.bindings.h")]
    internal sealed class NoAllocHelpers
    {

        public static void ResizeList<T>(List<T> list, int size)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (size < 0 || size > list.Capacity)
                throw new ArgumentException("invalid size to resize.", "list");
            if (size != list.Count)
                Internal_ResizeList(list, size);
        }

        public static void EnsureListElemCount<T>(List<T> list, int count)
        {
            list.Clear();

            // make sure capacity is enough (that's where alloc WILL happen if needed)
            if (list.Capacity < count)
                list.Capacity = count;

            ResizeList(list, count);
        }

        // tiny helpers
        public static int SafeLength(System.Array values)           { return values != null ? values.Length : 0; }
        public static int SafeLength<T>(List<T> values)             { return values != null ? values.Count : 0; }
        public static T[] ExtractArrayFromListT<T>(List<T> list)    { return (T[])ExtractArrayFromList(list); }

        [FreeFunction("NoAllocHelpers_Bindings::Internal_ResizeList")]
        extern internal static void Internal_ResizeList(object list, int size);

        [FreeFunction("NoAllocHelpers_Bindings::ExtractArrayFromList")]
        extern public static System.Array ExtractArrayFromList(object list);
    }
}
