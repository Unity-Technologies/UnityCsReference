// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [StructLayout(LayoutKind.Sequential)]
    internal ref struct ManagedListWrapper
    {
        [VisibleToOtherModules]

        internal enum UpdateFlags : byte
        {
            NoUpdateNeeded = 0,
            UpdateNeeded = 1,
            FreeNeeded = 2
        }

        public unsafe void* listBegin;
        public unsafe void* currentBegin;
        public int size;
        public int capacity;
        public UpdateFlags updateFlags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Unmarshal<T>(ListPrivateFieldAccess<T> list) where T : unmanaged
        {
            // This is done outside the call in the ilwrapper
            // if (updateFlags == UpdateFlags.NoUpdateNeeded)
            //     return;

            if (list == null)
            {
                if (updateFlags == UpdateFlags.FreeNeeded)
                    BindingsAllocator.Free(currentBegin);
                return;
            }

            if (listBegin != currentBegin)
            {
                list._items = new Span<T>(currentBegin, capacity).ToArray();
                if (updateFlags == UpdateFlags.FreeNeeded)
                    BindingsAllocator.Free(currentBegin);
            }

            list._size = size;
            list._version++;
        }

        // This is a helper class to allow the binding code to manipulate the internal fields of
        // System.Collections.Generic.List.  The field order below must not be changed.
        [VisibleToOtherModules]
        internal class ListPrivateFieldAccess<T>
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
