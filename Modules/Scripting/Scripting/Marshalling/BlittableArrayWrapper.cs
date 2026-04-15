// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe ref struct BlittableArrayWrapper
    {
        internal MarshalledArray arrayWrapper;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlittableArrayWrapper(void* data, int size)
        {
            arrayWrapper = MarshalledArray.CreateFromPinnedData(data, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Unmarshal<T>(ref T[] array) where T : unmanaged
        {
            var accessor = new ArrayByRefMarshallingAccessor<T, T>(array);
            arrayWrapper.UnmarshalBlittable<T, ArrayByRefMarshallingAccessor<T, T>>(ref accessor);
            array = accessor.GetArray();
            arrayWrapper.Free();
        }
    }
}
