// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.Layout;

enum LayoutNodeDataType
{
    Node = 0,
    Computed = 1,
    Cache = 2,
    ComputedStyle = 3,
    Transform = 4,
    SelectorData = 5,
}

enum LayoutConfigDataType
{
    Config = 0,
    PanelTransform = 1
}

delegate void LayoutMeasureFunction(
    ref LayoutNode node,
    float width,
    LayoutMeasureMode widthMode,
    float height,
    LayoutMeasureMode heightMode,
    out LayoutSize result);

delegate float LayoutBaselineFunction(
    ref LayoutNode node,
    float width,
    float height);

class ManagedObjectStore<T>
{
    const int k_ChunkSize = 1024 * 2; // 2k elements per 'chunk'

    private readonly int m_ChunkSize;

    int m_Length;

    readonly List<T[]> m_Chunks;
    readonly Queue<int> m_Free;

    public ManagedObjectStore(int chunkSize = k_ChunkSize)
    {
        m_ChunkSize = chunkSize;
        m_Chunks = new List<T[]>
        {
            new T[m_ChunkSize]
        };

        m_Length = 1;

        m_Free = new Queue<int>();
    }

    public T GetValue(int index)
    {
        if (index == 0)
            return default;

        var chunkIndex = index / m_ChunkSize;
        var indexInChunk = index % m_ChunkSize;

        return m_Chunks[chunkIndex][indexInChunk];
    }

    public void UpdateValue(ref int index, T value)
    {
        if (index != 0)
        {
            if (value != null)
            {
                var chunkIndex = index / m_ChunkSize;
                var indexInChunk = index % m_ChunkSize;

                // We have an index already and the value we are assigning is non-null. Perform a simple update.
                m_Chunks[chunkIndex][indexInChunk] = value;
            }
            else
            {
                // We have an index but the assigned value is null. Treat this as a removal and record the index as free for re-use.
                m_Free.Enqueue(index);

                var chunkIndex = index / m_ChunkSize;
                var indexInChunk = index % m_ChunkSize;

                m_Chunks[chunkIndex][indexInChunk] = default;

                index = 0;
            }
        }
        else if (value != null)
        {
            // We don't have an index and the value is not null. We need a new entry in the list.
            if (m_Free.Count > 0)
            {
                // Use the free list if available.
                index = m_Free.Dequeue();

                var chunkIndex = index / m_ChunkSize;
                var indexInChunk = index % m_ChunkSize;

                m_Chunks[chunkIndex][indexInChunk] = value;
            }
            else
            {
                // Otherwise allocate a new entry.
                index = m_Length++;

                if (index >= m_Chunks.Count * m_ChunkSize)
                    m_Chunks.Add(new T[m_ChunkSize]);

                var chunkIndex = index / m_ChunkSize;
                var indexInChunk = index % m_ChunkSize;

                m_Chunks[chunkIndex][indexInChunk] = value;
            }
        }
    }
}

internal class LayoutManager : IDisposable
{
    enum SharedManagerState
    {
        Uninitialized, // The SharedManager was not accessed yet
        Initialized, // The SharedManager was accessed and created
        Shutdown // The SharedManager was disposed and must not re-created
    }
    static SharedManagerState s_Initialized;

    static LayoutManager s_SharedInstance;

    public static bool IsSharedManagerCreated => s_Initialized == SharedManagerState.Initialized;

    public static LayoutManager SharedManager
    {
        get
        {
            Initialize();
            return s_SharedInstance;
        }
    }

    static readonly List<LayoutManager> s_Managers = new List<LayoutManager>();

    // Important: Assumptions about Order of operations for Initialize() and Shutdown()
    // 1. Initialize() is always called first on the main thread.
    //    This is because VisualElement instances do not support being created on other threads.
    //    Later on Initialize() it is called on the finalizer thread when a VisualElement is finalized.
    //    THEREFORE there shouldn't be any race condition for the transition between Uninitialized and Initialized.
    // 2. Shutdown() is only called after the first call to Initialize()
    //    Since it is registered on the AppDomain unload event as part of Initialize().
    //    We also assume the AppDomain unload will not happen right in the middle of Initialize()'s execution.
    //    THEREFORE there shouldn't be any race condition for the transition between Initialized and Shutdown.
    static unsafe void Initialize()
    {
        // If the SharedManager was already created, we're good
        // If it was shut down, we do not want to re-create it
        if (s_Initialized != SharedManagerState.Uninitialized)
            return;

        s_Initialized = SharedManagerState.Initialized;
        s_SharedInstance = new LayoutManager(Allocator.Persistent);
        UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.LayoutManager, Shutdown);
    }

    static void Shutdown()
    {
        if (s_Initialized != SharedManagerState.Initialized)
            return;

        s_Initialized = SharedManagerState.Shutdown;

        s_SharedInstance.Dispose();
    }

    // The capacity of the LayoutManager impacts how many chunks are created per component
    // Because every component has a different size, the number of chunks will differ for each
    static int DefaultCapacity =>
        k_CapacityBig
    ;

    // For Editor, we use a very high initial capacity that should in practice rarely be exceeded
    public const int k_CapacityBig = 1024 * 64;

    // For the Player, memory consumption is a concern so we start smaller, normally leading to 1 chunk per component
    public const int k_CapacitySmall = 16;

    const int k_InitialConfigCapacity = 32;

    readonly int m_Index;

    UnmanagedDataStore m_Nodes;
    UnmanagedDataStore m_Configs;

    internal UnmanagedDataStore Nodes => m_Nodes;
    internal UnmanagedDataStore Configs => m_Configs;

    readonly ConcurrentQueue<UnmanagedDataHandle> m_NodesToFree = new();

    readonly UnmanagedDataHandle m_DefaultConfig;

    readonly ManagedObjectStore<LayoutMeasureFunction> m_ManagedMeasureFunctions = new(k_CapacitySmall);
    readonly ManagedObjectStore<LayoutBaselineFunction> m_ManagedBaselineFunctions = new(k_CapacitySmall);

    readonly ProfilerMarker m_CollectMarker = new (ProfilerCategory.UIToolkit, "UIElements.CollectLayoutNodes");

    // Last allocated index in the store (0 mean index 0 is valid aka a node was allocated)
    int m_HighMark = -1;

    // Used in tests.
    public int NodeCapacity => m_Nodes.Capacity;

    internal static LayoutManager GetManager(int index)
        => (uint) index < s_Managers.Count ? s_Managers[index] : null;

    public LayoutManager(Allocator allocator) : this(allocator, DefaultCapacity, InitialStyle.Get()) {}

    public unsafe LayoutManager(Allocator allocator, int initialNodeCapacity, ComputedStyle initialStyle)
    {
        m_Index = s_Managers.Count;
        s_Managers.Add(this);

        const string areaName = nameof(UIElements);

        // Configs

        var configComponentTypes = new[]
        {
            UnmanagedComponentType.Create<LayoutConfigData>(),
            UnmanagedComponentType.Create<PanelTransformData>()
        };
        ReadOnlySpan<MemoryLabel> configComponentLabels = stackalloc MemoryLabel[]
        {
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutConfigData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(PanelTransformData)}>"),
        };

        var layoutConfigData = LayoutConfigData.Default;
        var panelTransformData = PanelTransformData.Default;
        byte** configComponentData = stackalloc byte*[]
        {
            (byte*)&layoutConfigData,
            (byte*)&panelTransformData,
        };
        m_Configs = new UnmanagedDataStore(configComponentTypes, configComponentLabels, configComponentData,
            k_InitialConfigCapacity, allocator);

        m_DefaultConfig = CreateConfig().Handle;
        LayoutNodeData.Default.Config = m_DefaultConfig;

        // Nodes

        var nodeComponentTypes = new[]
        {
            UnmanagedComponentType.Create<LayoutNodeData>(),
            UnmanagedComponentType.Create<LayoutComputedData>(),
            UnmanagedComponentType.Create<LayoutCacheData>(),
            UnmanagedComponentType.Create<ComputedStyle>(),
            UnmanagedComponentType.Create<VisualElementTransformData>(),
            UnmanagedComponentType.Create<VisualElementSelectorData>(),
        };

        ReadOnlySpan<MemoryLabel> nodeComponentLabels = stackalloc MemoryLabel[]
        {
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutNodeData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutComputedData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutCacheData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(ComputedStyle)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(VisualElementTransformData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(VisualElementSelectorData)}>"),
        };

        var layoutNodeData = LayoutNodeData.Default;
        var layoutComputedData = LayoutComputedData.Default;
        var layoutCacheData = LayoutCacheData.Default;
        var visualElementTransformData = VisualElementTransformData.Default;
        var visualElementSelectorData = VisualElementSelectorData.Default;

        byte** nodeComponentData = stackalloc byte*[]
        {
            (byte*)&layoutNodeData,
            (byte*)&layoutComputedData,
            (byte*)&layoutCacheData,
            (byte*)&initialStyle,
            (byte*)&visualElementTransformData,
            (byte*)&visualElementSelectorData
        };

        m_Nodes = new UnmanagedDataStore(nodeComponentTypes, nodeComponentLabels, nodeComponentData,
            initialNodeCapacity, allocator);
    }

    public void Dispose()
    {
        s_Managers[m_Index] = null;

        unsafe
        {
            // if m_HighMark == 0, then a single node was allocated and we need to dispose it
            for (var i = 0; i <= m_HighMark; i++)
            {
                if (m_Nodes.IsFree(i))
                    continue;

                var cache = (LayoutCacheData*)m_Nodes.GetComponentDataPtr(i, (int)LayoutNodeDataType.Cache);
                cache->ClearCachedMeasurements();

                var computedStyle = (ComputedStyle*) m_Nodes.GetComponentDataPtr(i, (int)LayoutNodeDataType.ComputedStyle);
                // Safe-release is called because the ComputedStyle has already been released if this node was recycled through Collect()
                computedStyle->SafeRelease();
            }
        }

        m_Nodes.Dispose();
        m_Configs.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    LayoutDataAccess GetAccess()
    {
        return new LayoutDataAccess(m_Index, m_Nodes, m_Configs);
    }

    public LayoutConfig GetDefaultConfig()
    {
        return new LayoutConfig(GetAccess(), m_DefaultConfig);
    }

    public LayoutConfig CreateConfig()
    {
        return new LayoutConfig(GetAccess(), m_Configs.Allocate());
    }

    public void DestroyConfig(ref LayoutConfig config)
    {
        m_Configs.Free(config.Handle);
        config = LayoutConfig.Undefined;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LayoutNode CreateNode()
    {
        return CreateNodeInternal();
    }

    public LayoutNode CreateNode(LayoutConfig config)
    {
        var node = CreateNodeInternal();
        node.Config = config;
        return node;
    }

    public LayoutNode CreateNode(LayoutNode source)
    {
        var node = CreateNodeInternal();
        node.Config = source.Config;
        node.CopyStyle(source);
        return node;
    }

    LayoutNode CreateNodeInternal()
    {
        TryRecycleSingleNode();

        var handle = m_Nodes.Allocate();

        if (handle.Index > m_HighMark)
            m_HighMark = handle.Index;

        var node = new LayoutNode(GetAccess(), handle);
        node.ComputedStyle.Acquire();
        return node;
    }

    void TryRecycleSingleNode()
    {
        if (m_NodesToFree.TryDequeue(out UnmanagedDataHandle handle))
        {
            FreeNode(handle);
        }
    }

    void TryRecycleNodes()
    {
        // Since this just about pre-emptively freeing up memory, and because we always try to free one node before
        // allocating a new one, we will limit the number of iterations per frame
        // We should make this configurable, at least as a diagnostic switch
        const int maxIterations = 100;
        int iterations = 0;
        while (iterations < maxIterations && m_NodesToFree.TryDequeue(out UnmanagedDataHandle handle))
        {
            FreeNode(handle);
            iterations++;
        }
    }

    // Note: this operation is safe regardless of if the LayoutManager has been shutdown or not.
    // The nodes to free are ignored because the LayoutManager has already released all of their memory.
    public void EnqueueNodeForRecycling(ref LayoutNode node)
    {
        if (node.IsUndefined)
            return;

        m_NodesToFree.Enqueue(node.Handle);

        node = LayoutNode.Undefined;
    }

    unsafe void FreeNode(UnmanagedDataHandle handle)
    {
        var access = GetAccess();

        ref var cache = ref access.GetCacheData(handle);
        cache.ClearCachedMeasurements();

        ref var data = ref access.GetNodeData(handle);
        data.UsesMeasure = false;
        data.UsesBaseline = false;

        ref var computedStyle = ref access.GetComputedStyle(handle);
        computedStyle.Release();

        m_Nodes.Free(handle);
    }

    public void Collect()
    {
        using (m_CollectMarker.Auto())
            TryRecycleNodes();
    }

    public LayoutMeasureFunction GetMeasureFunction(UnmanagedDataHandle handle)
    {
        int index = GetAccess().GetConfigData(handle).ManagedMeasureFunctionIndex;
        return m_ManagedMeasureFunctions.GetValue(index);
    }

    public void SetMeasureFunction(UnmanagedDataHandle handle, LayoutMeasureFunction value)
    {
        ref var index = ref GetAccess().GetConfigData(handle).ManagedMeasureFunctionIndex;
        m_ManagedMeasureFunctions.UpdateValue(ref index, value);
    }

    public LayoutBaselineFunction GetBaselineFunction(UnmanagedDataHandle handle)
    {
        int index = GetAccess().GetConfigData(handle).ManagedBaselineFunctionIndex;
        return m_ManagedBaselineFunctions.GetValue(index);
    }

    public void SetBaselineFunction(UnmanagedDataHandle handle, LayoutBaselineFunction value)
    {
        ref var index = ref GetAccess().GetConfigData(handle).ManagedBaselineFunctionIndex;
        m_ManagedBaselineFunctions.UpdateValue(ref index, value);
    }
}
