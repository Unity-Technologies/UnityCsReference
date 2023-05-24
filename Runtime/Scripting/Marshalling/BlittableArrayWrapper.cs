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
    internal unsafe ref struct BlittableArrayWrapper
    {
        internal enum UpdateFlags : int
        {
            NoUpdateNeeded = 0,
            SizeChanged = 1,
            DataIsNativePointer = 2,
            DataIsNativeOwnedMemory = 3,
            DataIsEmpty = 4,
            DataIsNull = 5,
        }

        // Managed->Native - a pointer to a pinned buffer (null if size == 0)
        // Native->Managed: Depends on UpdateFlags:
        //      NoUpdateNeeed - unchanged
        //      SizeChanged - the collection size changed
        //      DataIsNativePointer - data is a pointer to native memory, but does not need to be freed
        //      DataIsNativeOwnedMemory - data is native owned memory, that we need to free
        //      DataIsEmpty - zero length collection returned
        //      DataIsNull - native returned a null value

        internal void* data;

        // Managed->Native: The number of elements in the pinned data buffer
        // Native->Managed: The number of elements in the returned data buffer
        internal int size;

        internal UpdateFlags updateFlags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlittableArrayWrapper(void* data, int size)
        {
            this.data = data;
            this.size = size;
            this.updateFlags = UpdateFlags.NoUpdateNeeded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Unmarshal<T>(ref T[] array) where T : unmanaged
        {
            switch(updateFlags)
            {
                case UpdateFlags.NoUpdateNeeded:
                    break;
                case UpdateFlags.SizeChanged:
                case UpdateFlags.DataIsNativePointer:
                    array = new Span<T>(data, size).ToArray();
                    break;
                case UpdateFlags.DataIsNativeOwnedMemory:
                    array = new Span<T>(BindingsAllocator.GetNativeOwnedDataPointer(data), size).ToArray();
                    BindingsAllocator.FreeNativeOwnedMemory(data);
                    break;
                case UpdateFlags.DataIsEmpty:
                    array = Array.Empty<T>();
                    break;
                case UpdateFlags.DataIsNull:
                    array = null;
                    break;
            }
        }
    }
}
