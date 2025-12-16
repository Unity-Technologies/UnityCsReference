using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.DataModel;

internal class UnsafeHelper
{
    internal static unsafe ref T AsRef<T>(void* source) where T : struct
    {
        return ref UnsafeUtility.AsRef<T>(source);
    }

    internal static unsafe ref T Add<T>(ref T source, int elementOffset) where T : unmanaged
    {
        return ref UnsafeUtility.Add(ref source, elementOffset);
    }

    internal static unsafe void CopyBlock(ref byte destination, ref byte source, uint byteCount)
    {
        fixed (byte* dstPtr = &destination)
        fixed (byte* srcPtr = &source)
        {
            Buffer.MemoryCopy(srcPtr, dstPtr, byteCount, byteCount);
        }
    }

    internal static unsafe void* AsPointer<T>(ref T value) where T : unmanaged
    {
        return UnsafeUtility.AsPointer(ref value);
    }

    internal static unsafe byte* AsBytePointer<T>(ref T value) where T : unmanaged
    {
        fixed (T* p = &value)
        {
            return (byte*)p;
        }
    }

    internal static unsafe byte* AsBytePointerFromReadOnly<T>(in T value) where T : unmanaged
    {
        fixed (T* p = &value)
        {
            return (byte*)p;
        }
    }

    internal static T As<T>(object value) where T : class
    {
        return UnsafeUtility.As<T>(value);
    }

    public static ref T As<U, T>(ref U from)
    {
        return ref UnsafeUtility.As<U, T>(ref from);
    }
}
