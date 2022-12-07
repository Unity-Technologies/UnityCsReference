// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿// ReSharper disable FieldCanBeMadeReadOnly.Local
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer2<T> where T : unmanaged
{
    T __0;
    T __1;

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

[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer4<T> where T : unmanaged
{
    T __0;
    T __1;
    T __2;
    T __3;

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

[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer6<T> where T : unmanaged
{
    T __0;
    T __1;
    T __2;
    T __3;
    T __4;
    T __5;

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

[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer9<T> where T : unmanaged
{
    T __0;
    T __1;
    T __2;
    T __3;
    T __4;
    T __5;
    T __6;
    T __7;
    T __8;

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

[StructLayout(LayoutKind.Sequential)]
unsafe struct FixedBuffer16<T> where T : unmanaged
{
    T __0;
    T __1;
    T __2;
    T __3;
    T __4;
    T __5;
    T __6;
    T __7;
    T __8;
    T __9;
    T _10;
    T _11;
    T _12;
    T _13;
    T _14;
    T _15;

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
