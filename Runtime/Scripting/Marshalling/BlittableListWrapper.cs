// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe ref struct BlittableListWrapper
    {
        private BlittableArrayWrapper arrayWrapper;
        private int listSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlittableListWrapper(BlittableArrayWrapper arrayWrapper, int listSize)
        {
            this.arrayWrapper = arrayWrapper;
            this.listSize = listSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Unmarshal<T>(List<T> list) where T : unmanaged
        {
            if (list == null)
                return;

            switch (arrayWrapper.updateFlags)
            {
                case BlittableArrayWrapper.UpdateFlags.NoUpdateNeeded:
                    break;
                case BlittableArrayWrapper.UpdateFlags.SizeChanged:
                case BlittableArrayWrapper.UpdateFlags.DataIsNull:
                case BlittableArrayWrapper.UpdateFlags.DataIsEmpty:
                    NoAllocHelpers.ResetListSize(list, listSize);
                    break;
                case BlittableArrayWrapper.UpdateFlags.DataIsNativePointer:
                    NoAllocHelpers.ResetListContents(list, new ReadOnlySpan<T>(arrayWrapper.data, arrayWrapper.size));
                    break;
                case BlittableArrayWrapper.UpdateFlags.DataIsNativeOwnedMemory:
                    NoAllocHelpers.ResetListContents(list, new ReadOnlySpan<T>(BindingsAllocator.GetNativeOwnedDataPointer(arrayWrapper.data), arrayWrapper.size));
                    BindingsAllocator.FreeNativeOwnedMemory(arrayWrapper.data);
                    break;
            }
        }
    }
}
