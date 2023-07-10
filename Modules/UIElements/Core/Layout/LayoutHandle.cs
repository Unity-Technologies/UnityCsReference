// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

/// <summary>
/// A <see cref="LayoutHandle"/> represents a reference to unmanaged memory.
///
/// * The index specifies the data index in the <see cref="LayoutDataStore"/>.
/// * The version is used for tracking lifecycle and re-using indices.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
readonly struct LayoutHandle
{
    public static LayoutHandle Undefined => new LayoutHandle();

    public readonly int Index;
    public readonly int Version;

    internal LayoutHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    public bool IsUndefined => this.Equals(Undefined);

    public bool Equals(LayoutHandle other)
        => Index == other.Index && Version == other.Version;

    public override bool Equals(object obj)
        => obj is LayoutHandle other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Index * 397) ^ Version;
        }
    }
}
