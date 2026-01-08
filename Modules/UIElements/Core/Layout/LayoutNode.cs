// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
partial struct LayoutNode : IEquatable<LayoutNode>
{
    public static LayoutNode Undefined => new(default, UnmanagedDataHandle.Undefined);

    readonly LayoutDataAccess m_Access;
    readonly UnmanagedDataHandle m_Handle;

    internal LayoutNode(LayoutDataAccess access, UnmanagedDataHandle handle)
    {
        m_Access = access;
        m_Handle = handle;
    }

    /// <summary>
    /// Returns <see langword="true"/> if this is an invalid/undefined node.
    /// </summary>
    public bool IsUndefined => m_Handle.Equals(UnmanagedDataHandle.Undefined);

    /// <summary>
    /// Returns the handle for this node.
    /// </summary>
    public UnmanagedDataHandle Handle => m_Handle;

    /// <summary>
    /// Gets the computed layout struct for this node.
    /// </summary>
    public ref LayoutComputedData Layout => ref m_Access.GetComputedData(m_Handle);

    /// <summary>
    /// For internal setters only. Gets the style input struct for this node.
    /// </summary>
    private ref readonly LayoutData ReadOnlyStyle => ref m_Access.GetComputedStyle(m_Handle).layoutData.Read();

    // For internal setters only. The readonly version is also used in CalculateLayout().
    private ref LayoutData Style => ref m_Access.GetComputedStyle(m_Handle).layoutData.Write();

    /// <summary>
    /// Gets the style input struct for this node.
    /// </summary>
    internal ref LayoutCacheData Cache => ref m_Access.GetCacheData(m_Handle);

    internal unsafe VisualElementTransformData* VisualElementTransformDataPtr => m_Access.GetTransformDataPtr(m_Handle);
    internal unsafe LayoutComputedData* ComputedDataPtr => m_Access.GetComputedDataPtr(m_Handle);

    /// <summary>
    /// Gets the ComputedStyle data for this node.
    /// </summary>
    internal ref ComputedStyle ComputedStyle => ref m_Access.GetComputedStyle(m_Handle);

    /// <summary>
    /// Gets or sets the dirty flag for this node. Used when calculating layout.
    /// </summary>
    public bool IsDirty
    {
        get => m_Access.GetNodeData(m_Handle).IsDirty;
        set => m_Access.GetNodeData(m_Handle).IsDirty = value;
    }

    /// <summary>
    /// Gets or sets the new layout flag for this node. Used when calculating layout.
    /// </summary>
    public bool HasNewLayout
    {
        get => m_Access.GetNodeData(m_Handle).HasNewLayout;
        set => m_Access.GetNodeData(m_Handle).HasNewLayout = value;
    }

    /// <summary>
    /// Gets or sets a flag to indicate this node needs to invoke the config's measure function
    /// </summary>
    public bool UsesMeasure
    {
        get => m_Access.GetNodeData(m_Handle).UsesMeasure;
        set => m_Access.GetNodeData(m_Handle).UsesMeasure = value;
    }

    /// <summary>
    /// Gets or sets a flag to indicate this node needs to invoke the config's baseline function
    /// </summary>
    public bool UsesBaseline
    {
        get => m_Access.GetNodeData(m_Handle).UsesBaseline;
        set => m_Access.GetNodeData(m_Handle).UsesBaseline = value;
    }

    /// <summary>
    /// Sets the owner of this node.
    /// </summary>
    public void SetOwner(VisualElement func)
    {
        m_Access.SetOwner(m_Handle, func);
    }

    public VisualElement GetOwner()
    {
       return m_Access.GetOwner(m_Handle);
    }

    /// <summary>
    /// Gets or sets the line index for this node. Used when calculating layout.
    /// </summary>
    public ref int LineIndex => ref m_Access.GetNodeData(m_Handle).LineIndex;

    /// <summary>
    /// Gets or sets the shared configuration object for this node.
    /// </summary>
    public LayoutConfig Config
    {
        get => new(m_Access, m_Access.GetNodeData(m_Handle).Config);
        set => m_Access.GetNodeData(m_Handle).Config = value.Handle;
    }

    /// <summary>
    /// Marks this node and all ancestors as dirty.
    /// </summary>
    public void MarkDirty()
    {
        if (IsDirty)
            return;

        IsDirty = true;

        Layout.ComputedFlexBasis = float.NaN;

        if (!Parent.IsUndefined)
            Parent.MarkDirty();
    }

    /// <summary>
    /// Marks this node layout as seen (not new).
    /// </summary>
    public void MarkLayoutSeen()
    {
        HasNewLayout = false;
    }

    /// <summary>
    /// Copies the style from the given <see cref="LayoutNode"/>.
    /// </summary>
    /// <param name="node">The node to copy the style from.</param>
    public void CopyStyle(LayoutNode node)
    {
        var markDirty = false;
        unsafe
        {
            fixed (LayoutData* dstStyle = &ReadOnlyStyle)
            fixed (LayoutData* srcStyle = &node.ReadOnlyStyle)
            {
                if (UnsafeUtility.MemCmp(dstStyle, srcStyle, UnsafeUtility.SizeOf<LayoutData>()) != 0)
                {
                    Style = node.ReadOnlyStyle;
                    markDirty = true;
                }
            }
        }

        if (markDirty)
            MarkDirty();
    }

    /// <summary>
    /// Resets the node for immediate re-use on the same element.
    /// </summary>
    public void SoftReset()
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);
        data.HasNewLayout = true;

        unsafe {
            ref var cache = ref Cache;
            if (cache.CachedLayout.NextMeasurementCache != null)
            {
                cache.ClearCachedMeasurements();
            }
        }
    }

    /// <summary>
    /// Resets the node for re-use.
    /// </summary>
    public void Reset()
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        Assert.IsTrue(!data.Children.IsCreated || data.Children.Count == 0, "Cannot reset a node which still has children attached");

        data.Parent = default;
        data.HasNewLayout = true;
        data.ResolvedDimensions = new FixedBuffer2<Length>
        {
            [0] = Length.None(),
            [1] = Length.None()
        };
        data.UsesMeasure = false;
        data.UsesBaseline = false;

        SetOwner(null);

        Layout = LayoutComputedData.Default;
        Style = LayoutData.Default;
    }

    public bool Equals(LayoutNode other)
    {
        return m_Handle.Equals(other.m_Handle);
    }

    public override bool Equals(object obj)
    {
        return obj is LayoutNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return m_Handle.GetHashCode();
    }

    public static bool operator ==(LayoutNode lhs, LayoutNode rhs)
    {
        if (lhs.IsUndefined)
        {
            if (rhs.IsUndefined)
                return true;

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(LayoutNode lhs, LayoutNode rhs) => !(lhs == rhs);

    /// <summary>
    /// Performs the flexbox layout calculation.
    /// </summary>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height.</param>
    public void CalculateLayout(float width = float.NaN, float height = float.NaN)
    {
        LayoutProcessor.CalculateLayout(this, width, height, ReadOnlyStyle.Direction);
    }
}
