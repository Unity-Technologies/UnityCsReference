// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
readonly struct LayoutConfig
{
    public static LayoutConfig Undefined => new LayoutConfig(default, LayoutHandle.Undefined);

    readonly LayoutDataAccess m_Access;
    readonly LayoutHandle m_Handle;

    internal LayoutConfig(LayoutDataAccess access, LayoutHandle handle)
    {
        m_Access = access;
        m_Handle = handle;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this is an invalid/undefined node.
    /// </summary>
    public bool IsUndefined => m_Handle.Equals(LayoutHandle.Undefined);

    /// <summary>
    /// Returns the handle for this node.
    /// </summary>
    public LayoutHandle Handle => m_Handle;

    /// <summary>
    /// Gets or sets the shared point scale factor for configured nodes.
    /// </summary>
    public ref float PointScaleFactor => ref m_Access.GetConfigData(m_Handle).PointScaleFactor;

    public ref bool ShouldLog => ref m_Access.GetConfigData(m_Handle).ShouldLog;
}
