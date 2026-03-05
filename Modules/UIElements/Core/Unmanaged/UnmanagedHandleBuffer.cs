// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.Unmanaged;

[NativeHeader("Modules/UIElements/Core/Native/Unmanaged/UnmanagedList.h")]
[NativeClass("UnmanagedHandleBuffer")]
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UnmanagedHandleBuffer : IDisposable
{
    private static readonly UnmanagedHandleBuffer k_Uncreated = new();

    [NativeName("data")]
    private IntPtr m_Data;

    public int Count => NativeCount();
    public ref UnmanagedDataHandle this[int index] => ref *(UnmanagedDataHandle*)GetPtr(index);

    public ReadOnlySpan<UnmanagedDataHandle> ReadOnlySpan => Count == 0 ? default : new ReadOnlySpan<UnmanagedDataHandle>((UnmanagedDataHandle*)GetPtr(0), Count);

    [NativeName("Create")]
    private extern void _Create();
    [NativeName("CreateTemporary")]
    private extern void _CreateTemporary();

    public static UnmanagedHandleBuffer None() => k_Uncreated;
    public static UnmanagedHandleBuffer Create()
    {
        UnmanagedHandleBuffer b = new();
        b._Create();
        return b;
    }
    public static UnmanagedHandleBuffer CreateTemporary()
    {
        UnmanagedHandleBuffer b = new();
        b._CreateTemporary();
        return b;
    }
    public extern void Dispose();

    [NativeName("Count")]
    private extern int NativeCount();

    private extern IntPtr GetPtr(int index);

    public extern void Clear();

    public extern void Add(UnmanagedDataHandle handle);
}
