// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Serialization
{
    // Public only so source-generated sized collection factories can call it
    // from user assemblies; don't use directly. A sized factory owns the whole
    // slot materialization for a collection read: the reuse decision, the
    // allocation, the slot store, and returning the backing array the read
    // executor fills — one calli in place of UnmarshalSystemType +
    // Array.CreateInstance (and, for lists, the uninitialized-shell path).
    [UnityEngine.Internal.ExcludeFromDocs]
    public static class SerializationFactoryHelpers
    {
        // Signature shared by every sized factory. The generated non-unsafe
        // fallback materializes the function pointer through this delegate.
        [UnityEngine.Internal.ExcludeFromDocs]
        public delegate object SizedFactory(ref object slot, int n);

        // Mirrors List<T>'s leading instance fields (see the identical mirror
        // in SerializationBackendManagedCommands.bindings.cs).
        private sealed class ListLayout
        {
#pragma warning disable 0649
            public byte[] _items;
            public int    _size;
#pragma warning restore 0649
        }

        // AggressiveInlining: the generated per-element-type thunks are the calli
        // targets; inlining the helper into them makes each calli land on one
        // flat body, deterministically (tier-0, IL2CPP hint).
        [UnityEngine.Internal.ExcludeFromDocs]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object EnsureArrayBacking<T>(ref object slot, int n)
        {
            ref T[] arr = ref Unsafe.As<object, T[]>(ref slot);
            if (arr == null || arr.Length != n)
                arr = new T[n];
            return arr;
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object EnsureListBacking<T>(ref object slot, int n)
        {
            ref List<T> list = ref Unsafe.As<object, List<T>>(ref slot);
            if (list == null)
            {
                // The common case (fresh read): one ctor call yields shell +
                // exact-size backing; adopt it as the filled size.
                list = new List<T>(n);
                var fresh = Unsafe.As<ListLayout>(list);
                fresh._size = n;
                return fresh._items;
            }

            // Reuse (deserializing onto an existing object, the rare case):
            // instance identity is preserved; the backing is reused only when
            // the logical size matches exactly and capacity suffices (v1's
            // contract — every kept slot is then overwritten and no stale tail
            // is exposed).
            var layout = Unsafe.As<ListLayout>(list);
            if (layout._size != n || layout._items == null || layout._items.Length < n)
            {
                var replacement = new T[n];
                layout._items = Unsafe.As<T[], byte[]>(ref replacement);
            }
            layout._size = n;
            return layout._items;
        }
    }
}
