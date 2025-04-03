// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// ReSharper disable FieldCanBeMadeReadOnly.Local
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer2<T> where T : unmanaged
{
    [SerializeField] T __0;
    [SerializeField] T __1;

    public const int Length = 2;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (void* ptr = &this)
            {
                var p = (T*) ptr;
                return ref p[index];
            }
        }
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer4<T> where T : unmanaged
{
    [SerializeField] T __0;
    [SerializeField] T __1;
    [SerializeField] T __2;
    [SerializeField] T __3;

    public FixedBuffer4(T x, T y, T z, T w)
    {
        __0 = x;
        __1 = y;
        __2 = z;
        __3 = w;
    }

    public const int Length = 4;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (void* ptr = &this)
            {
                var p = (T*) ptr;
                return ref p[index];
            }
        }
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer6<T> where T : unmanaged
{
    [SerializeField] T __0;
    [SerializeField] T __1;
    [SerializeField] T __2;
    [SerializeField] T __3;
    [SerializeField] T __4;
    [SerializeField] T __5;

    public const int Length = 6;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= Length)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (void* ptr = &this)
            {
                var p = (T*) ptr;
                return ref p[index];
            }
        }
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer9<T> where T : unmanaged
{
    [SerializeField] T __0;
    [SerializeField] T __1;
    [SerializeField] T __2;
    [SerializeField] T __3;
    [SerializeField] T __4;
    [SerializeField] T __5;
    [SerializeField] T __6;
    [SerializeField] T __7;
    [SerializeField] T __8;

    public const int Length = 9;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            if (index is < 0 or >= Length)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (void* ptr = &this)
            {
                var p = (T*) ptr;
                return ref p[index];
            }
        }
    }
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer16<T> where T : unmanaged
{
    [SerializeField] T __0;
    [SerializeField] T __1;
    [SerializeField] T __2;
    [SerializeField] T __3;
    [SerializeField] T __4;
    [SerializeField] T __5;
    [SerializeField] T __6;
    [SerializeField] T __7;
    [SerializeField] T __8;
    [SerializeField] T __9;
    [SerializeField] T _10;
    [SerializeField] T _11;
    [SerializeField] T _12;
    [SerializeField] T _13;
    [SerializeField] T _14;
    [SerializeField] T _15;

    public const int Length = 16;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            if (index is < 0 or >= Length)
                throw new IndexOutOfRangeException(nameof(index));

            fixed (void* ptr = &this)
            {
                var p = (T*) ptr;
                return ref p[index];
            }
        }
    }
}
