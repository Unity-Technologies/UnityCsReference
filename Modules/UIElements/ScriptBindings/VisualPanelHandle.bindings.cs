// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

/// <summary>
/// The <see cref="VisualPanelHandle"/> represents a lightweight handle to native visual panel data.
/// </summary>
[NativeType(Header = "Modules/UIElements/VisualPanelHandle.h")]
[StructLayout(LayoutKind.Sequential)]
readonly struct VisualPanelHandle : IEquatable<VisualPanelHandle>
{
    /// <summary>
    /// Represents a null/invalid handle.
    /// </summary>
    public static readonly VisualPanelHandle Null;

    readonly int m_Id;
    readonly int m_Version;

    /// <summary>
    /// The unique id for this handle.
    /// </summary>
    public int Id => m_Id;

    /// <summary>
    /// The version number for this handle.
    /// </summary>
    public int Version => m_Version;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualPanelHandle"/> struct.
    /// </summary>
    /// <param name="id">The handle id.</param>
    /// <param name="version">The handle version.</param>
    public VisualPanelHandle(int id, int version)
    {
        m_Id = id;
        m_Version = version;
    }

    public static bool operator ==(in VisualPanelHandle lhs, in VisualPanelHandle rhs) => lhs.Id == rhs.Id && lhs.Version == rhs.Version;
    public static bool operator !=(in VisualPanelHandle lhs, in VisualPanelHandle rhs) => !(lhs == rhs);
    public bool Equals(VisualPanelHandle other) => other.Id == Id && other.Version == Version;
    public override string ToString() => $"{nameof(VisualPanelHandle)}({(this == Null ? nameof(Null) : $"{Id}:{Version}")})";
    public override bool Equals(object obj) => obj is VisualPanelHandle node && Equals(node);
    public override int GetHashCode() => HashCode.Combine(Id, Version);
}
