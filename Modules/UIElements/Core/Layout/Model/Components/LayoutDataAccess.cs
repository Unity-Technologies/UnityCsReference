// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.Layout;

/// <summary>
/// The <see cref="LayoutDataAccess"/> gives strongly typed unmanaged access to the individual components of a node.
/// </summary>
[RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
readonly unsafe struct LayoutDataAccess
{
    readonly int m_Manager;
    readonly UnmanagedDataStore m_Nodes;
    readonly UnmanagedDataStore m_Configs;

    public bool IsValid => m_Nodes.IsValid && m_Configs.IsValid;

    internal LayoutDataAccess(int manager, UnmanagedDataStore nodes, UnmanagedDataStore configs)
    {
        m_Manager = manager;
        m_Nodes = nodes;
        m_Configs = configs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ref T GetTypedNodeDataRef<T>(UnmanagedDataHandle handle, LayoutNodeDataType type) where T : unmanaged
        => ref ((T*) m_Nodes.GetComponentDataPtr(handle.Index, (int)type))[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ref T GetTypedConfigDataRef<T>(UnmanagedDataHandle handle, LayoutConfigDataType type) where T : unmanaged
        => ref ((T*) m_Configs.GetComponentDataPtr(handle.Index, (int)type))[0];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutNodeData GetNodeData(UnmanagedDataHandle handle)
        => ref GetTypedNodeDataRef<LayoutNodeData>(handle, LayoutNodeDataType.Node);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutComputedData GetComputedData(UnmanagedDataHandle handle)
        => ref GetTypedNodeDataRef<LayoutComputedData>(handle, LayoutNodeDataType.Computed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutCacheData GetCacheData(UnmanagedDataHandle handle)
        => ref GetTypedNodeDataRef<LayoutCacheData>(handle, LayoutNodeDataType.Cache);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ComputedStyle GetComputedStyle(UnmanagedDataHandle handle)
        => ref GetTypedNodeDataRef<ComputedStyle>(handle, LayoutNodeDataType.ComputedStyle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref LayoutConfigData GetConfigData(UnmanagedDataHandle handle)
        => ref GetTypedConfigDataRef<LayoutConfigData>(handle, LayoutConfigDataType.Config);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LayoutMeasureFunction GetMeasureFunction(UnmanagedDataHandle handle)
        => LayoutManager.GetManager(m_Manager).GetMeasureFunction(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMeasureFunction(UnmanagedDataHandle handle, LayoutMeasureFunction value)
        => LayoutManager.GetManager(m_Manager).SetMeasureFunction(handle, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisualElement GetOwner(UnmanagedDataHandle handle)
    => LayoutManager.GetManager(m_Manager).GetOwner(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOwner(UnmanagedDataHandle handle, VisualElement value)
        => LayoutManager.GetManager(m_Manager).SetOwner(handle, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LayoutBaselineFunction GetBaselineFunction(UnmanagedDataHandle handle)
        => LayoutManager.GetManager(m_Manager).GetBaselineFunction(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBaselineFunction(UnmanagedDataHandle handle, LayoutBaselineFunction value)
        => LayoutManager.GetManager(m_Manager).SetBaselineFunction(handle, value);
}
