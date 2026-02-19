// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.Unmanaged;

/// <summary>
/// A <see cref="UnmanagedDataHandle"/> represents a reference to unmanaged memory.
///
/// * The index specifies the data index in the <see cref="UnmanagedDataStore"/>.
/// * The version is used for tracking lifecycle and re-using indices.
/// </summary>
[NativeHeader("Modules/UIElements/Core/Native/Unmanaged/UnmanagedDataHandle.h")]
[StructLayout(LayoutKind.Sequential)]
readonly struct UnmanagedDataHandle
{
    public static UnmanagedDataHandle Undefined => new UnmanagedDataHandle();

    public readonly int Index;
    public readonly int Version;

    internal UnmanagedDataHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public bool IsUndefined => this.Equals(Undefined);

    public bool Equals(UnmanagedDataHandle other)
        => Index == other.Index && Version == other.Version;

    public override bool Equals(object obj)
        => obj is UnmanagedDataHandle other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Index * 397) ^ Version;
        }
    }

    internal class EqualityComparer : IEqualityComparer<UnmanagedDataHandle>
    {
        public bool Equals(UnmanagedDataHandle x, UnmanagedDataHandle y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(UnmanagedDataHandle handle)
        {
            return handle.GetHashCode();
        }
    }

    internal static readonly EqualityComparer k_EqualityComparer = new();
}
