// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.Layout;

/// <summary>
/// The <see cref="LayoutDataAccess"/> gives strongly typed unmanaged access to the individual components of a node.
/// </summary>
[RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
readonly unsafe struct LayoutDataAccess
{
    readonly int m_Manager;
    readonly LayoutDataStore m_Nodes;
    readonly LayoutDataStore m_Configs;

    public bool IsValid => m_Nodes.IsValid && m_Configs.IsValid;

    internal LayoutDataAccess(int manager, LayoutDataStore nodes, LayoutDataStore configs)
    {
        m_Manager = manager;
        m_Nodes = nodes;
        m_Configs = configs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ref T GetTypedNodeDataRef<T>(LayoutHandle handle, LayoutNodeDataType type) where T : unmanaged
        => ref ((T*) m_Nodes.GetComponentDataPtr(handle.Index, (int)type))[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ref T GetTypedConfigDataRef<T>(LayoutHandle handle, LayoutConfigDataType type) where T : unmanaged
        => ref ((T*) m_Configs.GetComponentDataPtr(handle.Index, (int)type))[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutNodeData GetNodeData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutNodeData>(handle, LayoutNodeDataType.Node);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutStyleData GetStyleData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutStyleData>(handle, LayoutNodeDataType.Style);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutStyleBorderData GetStyleBorderData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutStyleBorderData>(handle, LayoutNodeDataType.StyleBorder);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutStyleMarginData GetStyleMarginData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutStyleMarginData>(handle, LayoutNodeDataType.StyleMargin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutStyleDimensionData GetStyleDimensionData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutStyleDimensionData>(handle, LayoutNodeDataType.StyleDimensions);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutComputedData GetComputedData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutComputedData>(handle, LayoutNodeDataType.Computed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutCacheData GetCacheData(LayoutHandle handle)
        => ref GetTypedNodeDataRef<LayoutCacheData>(handle, LayoutNodeDataType.Cache);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutConfigData GetConfigData(LayoutHandle handle)
        => ref GetTypedConfigDataRef<LayoutConfigData>(handle, LayoutConfigDataType.Config);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LayoutMeasureFunction GetMeasureFunction(LayoutHandle handle)
        => LayoutManager.GetManager(m_Manager).GetMeasureFunction(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMeasureFunction(LayoutHandle handle, LayoutMeasureFunction value)
        => LayoutManager.GetManager(m_Manager).SetMeasureFunction(handle, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LayoutBaselineFunction GetBaselineFunction(LayoutHandle handle)
        => LayoutManager.GetManager(m_Manager).GetBaselineFunction(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBaselineFunction(LayoutHandle handle, LayoutBaselineFunction value)
        => LayoutManager.GetManager(m_Manager).SetBaselineFunction(handle, value);
}
