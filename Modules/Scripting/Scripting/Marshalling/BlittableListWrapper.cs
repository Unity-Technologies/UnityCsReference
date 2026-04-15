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
        private MarshalledArray arrayWrapper;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlittableListWrapper(BlittableArrayWrapper arrayWrapper, int listSize)
        {
            this.arrayWrapper = arrayWrapper.arrayWrapper;
            this.arrayWrapper.size = listSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Unmarshal<T>(List<T> list) where T : unmanaged
        {
            var accessor = new ListMarshallingAccessor<T, T>(list);
            arrayWrapper.UnmarshalBlittable<T, ListMarshallingAccessor<T, T>>(ref accessor);
            arrayWrapper.Free();
        }
    }
}
