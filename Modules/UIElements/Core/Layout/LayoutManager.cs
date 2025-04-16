// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.Layout;

[RequiredByNativeCode]
enum LayoutNodeDataType
{
    Node = 0,
    Style = 1,
    Computed = 2,
    Cache = 3
}

[RequiredByNativeCode]
enum LayoutConfigDataType
{
    Config = 0
}

delegate void LayoutMeasureFunction(
    VisualElement ve,
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

class ManagedObjectStore<T> where T : class
{
    const int k_ChunkSize = 1024 * 2; // 2k elements per 'chunk'

    int m_Length;

    readonly List<T[]> m_Chunks;
    readonly Queue<int> m_Free;

    public ManagedObjectStore()
    {
        m_Chunks = new List<T[]>
        {
            new T[k_ChunkSize]
        };

        m_Length = 1;

        m_Free = new Queue<int>();
    }

    public T GetValue(int index)
    {
        if (index == 0)
            return null;

        var chunkIndex = index / k_ChunkSize;
        var indexInChunk = index % k_ChunkSize;

        return m_Chunks[chunkIndex][indexInChunk];
    }

    public void UpdateValue(ref int index, T value)
    {
        if (index != 0)
        {
            if (value != null)
            {
                var chunkIndex = index / k_ChunkSize;
                var indexInChunk = index % k_ChunkSize;

                // We have an index already and the value we are assigning is non-null. Perform a simple update.
                m_Chunks[chunkIndex][indexInChunk] = value;
            }
            else
            {
                // We have an index but the assigned value is null. Treat this as a removal and record the index as free for re-use.
                m_Free.Enqueue(index);

                var chunkIndex = index / k_ChunkSize;
                var indexInChunk = index % k_ChunkSize;

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

                var chunkIndex = index / k_ChunkSize;
                var indexInChunk = index % k_ChunkSize;

                m_Chunks[chunkIndex][indexInChunk] = value;
            }
            else
            {
                // Otherwise allocate a new entry.
                index = m_Length++;

                if (index >= m_Chunks.Count * k_ChunkSize)
                    m_Chunks.Add(new T[k_ChunkSize]);

                var chunkIndex = index / k_ChunkSize;
                var indexInChunk = index % k_ChunkSize;

                m_Chunks[chunkIndex][indexInChunk] = value;
            }
        }
    }
}

internal class LayoutManager : IDisposable
{
    static bool s_Initialized;
    static bool s_AppDomainUnloadRegistered;

    static LayoutManager s_SharedInstance;

    public static bool IsSharedManagerCreated => s_Initialized;

    public static LayoutManager SharedManager
    {
        get
        {
            Initialize();
            return s_SharedInstance;
        }
    }

    static readonly List<LayoutManager> s_Managers = new List<LayoutManager>();

    static void Initialize()
    {
        if (s_Initialized)
            return;

        s_Initialized = true;

        if (!s_AppDomainUnloadRegistered)
        {
            // important: this will always be called from a special unload thread (main thread will be blocking on this)
            AppDomain.CurrentDomain.DomainUnload += (_, __) =>
            {
                if (s_Initialized)
                    Shutdown();
            };

            s_AppDomainUnloadRegistered = true;
        }

        s_SharedInstance = new LayoutManager(Allocator.Persistent);
    }

    static void Shutdown()
    {
        if (!s_Initialized)
            return;

        s_Initialized = false;

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

    LayoutDataStore m_Nodes;
    LayoutDataStore m_Configs;

    readonly ConcurrentQueue<LayoutHandle> m_NodesToFree = new();

    readonly LayoutHandle m_DefaultConfig;

    readonly ManagedObjectStore<LayoutMeasureFunction> m_ManagedMeasureFunctions = new();
    readonly ManagedObjectStore<LayoutBaselineFunction> m_ManagedBaselineFunctions = new();
    readonly ManagedObjectStore<WeakReference<VisualElement>> m_ManagedOwners = new();

    readonly ProfilerMarker m_CollectMarker = new ("UIElements.CollectLayoutNodes");

    // Last allocated index in the store (0 mean index 0 is valid aka a node was allocated)
    int m_HighMark = -1;

    // Used in tests.
    public int NodeCapacity => m_Nodes.Capacity;

    internal static LayoutManager GetManager(int index)
        => (uint) index < s_Managers.Count ? s_Managers[index] : null;

    public LayoutManager(Allocator allocator) : this(allocator, DefaultCapacity) {}

    public LayoutManager(Allocator allocator, int initialNodeCapacity)
    {
        m_Index = s_Managers.Count;
        s_Managers.Add(this);

        var nodeComponentTypes = new[]
        {
            ComponentType.Create<LayoutNodeData>(),
            ComponentType.Create<LayoutStyleData>(),
            ComponentType.Create<LayoutComputedData>(),
            ComponentType.Create<LayoutCacheData>(),
        };

        var configComponentTypes = new[]
        {
            ComponentType.Create<LayoutConfigData>()
        };

        m_Nodes = new LayoutDataStore(nodeComponentTypes, initialNodeCapacity, allocator);
        m_Configs = new LayoutDataStore(configComponentTypes, k_InitialConfigCapacity, allocator);

        m_DefaultConfig = CreateConfig().Handle;
    }

    public void Dispose()
    {
        s_Managers[m_Index] = null;

        unsafe
        {
            // if m_HighMark == 0, then a single node was allocated and we need to dispose it
            for (var i = 0; i <= m_HighMark; i++)
            {
                var data = (LayoutNodeData*) m_Nodes.GetComponentDataPtr(i, (int)LayoutNodeDataType.Node);

                if (!data->Children.IsCreated)
                    continue;

                data->Children.Dispose();
                data->Children = new();
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
        return new LayoutConfig(GetAccess(), m_Configs.Allocate(LayoutConfigData.Default));
    }

    public void DestroyConfig(ref LayoutConfig config)
    {
        m_Configs.Free(config.Handle);
        config = LayoutConfig.Undefined;
    }

    public LayoutNode CreateNode()
    {
        return CreateNodeInternal(m_DefaultConfig);
    }

    public LayoutNode CreateNode(LayoutConfig config)
    {
        return CreateNodeInternal(config.Handle);
    }

    public LayoutNode CreateNode(LayoutNode source)
    {
        var node = CreateNodeInternal(source.Config.Handle);
        node.CopyStyle(source);
        return node;
    }

    LayoutNode CreateNodeInternal(LayoutHandle configHandle)
    {
        TryRecycleSingleNode();

        var handle = m_Nodes.Allocate(
            new LayoutNodeData { Config = configHandle , Children= new() },
            LayoutStyleData.Default,
            LayoutComputedData.Default,
            LayoutCacheData.Default);

        if (handle.Index > m_HighMark)
            m_HighMark = handle.Index;

        var node = new LayoutNode(GetAccess(), handle);

        Debug.Assert(!GetAccess().GetNodeData(handle).Children.IsCreated, "memory is not initialized" );
        return node;
    }

    void TryRecycleSingleNode()
    {
        if (m_NodesToFree.TryDequeue(out LayoutHandle handle))
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
        while (iterations < maxIterations && m_NodesToFree.TryDequeue(out LayoutHandle handle))
        {
            FreeNode(handle);
            iterations++;
        }
    }

    public void EnqueueNodeForRecycling(ref LayoutNode node)
    {
        if (node.IsUndefined)
            return;

        m_NodesToFree.Enqueue(node.Handle);

        node = LayoutNode.Undefined;
    }

    void FreeNode(LayoutHandle handle)
    {
        ref var data = ref GetAccess().GetNodeData(handle);
        if (data.Children.IsCreated)
        {
            data.Children.Dispose();
            data.Children = new();
        }
        m_ManagedMeasureFunctions.UpdateValue(ref data.ManagedMeasureFunctionIndex, null);
        m_ManagedBaselineFunctions.UpdateValue(ref data.ManagedBaselineFunctionIndex, null);
        m_ManagedOwners.UpdateValue(ref data.ManagedOwnerIndex, null);
        m_Nodes.Free(handle);
    }

    public void Collect()
    {
        using (m_CollectMarker.Auto())
            TryRecycleNodes();
    }

    public LayoutMeasureFunction GetMeasureFunction(LayoutHandle handle)
    {
        return m_ManagedMeasureFunctions.GetValue(GetAccess().GetNodeData(handle).ManagedMeasureFunctionIndex);
    }

    public void SetMeasureFunction(LayoutHandle handle, LayoutMeasureFunction value)
    {
        if (GetAccess().GetNodeData(handle).ManagedOwnerIndex == 0) Debug.LogWarning("Setting Measure method on a node with no Owner");
        ref var index = ref GetAccess().GetNodeData(handle).ManagedMeasureFunctionIndex;
        m_ManagedMeasureFunctions.UpdateValue(ref index, value);
    }

    public VisualElement GetOwner(LayoutHandle handle)
    {
        //This assumes an internal behavior of the managed object store... invalid could be -1 instead
        if (GetAccess().GetNodeData(handle).ManagedOwnerIndex == 0)
            return null;

        // Will throw if the weak referenc is not in the list
        m_ManagedOwners.GetValue(GetAccess().GetNodeData(handle).ManagedOwnerIndex).TryGetTarget(out var ve);
        return ve;
    }

    public void SetOwner(LayoutHandle handle, VisualElement value)
    {
        if (value == null)
        {
            if (GetAccess().GetNodeData(handle).ManagedMeasureFunctionIndex != 0) Debug.LogWarning("Node with no owner has a Measure method");
            if (GetAccess().GetNodeData(handle).ManagedBaselineFunctionIndex != 0) Debug.LogWarning("Node with no owner has a baseline method");
        }
        ref var index = ref GetAccess().GetNodeData(handle).ManagedOwnerIndex;
        m_ManagedOwners.UpdateValue(ref index, new(value));
    }


    public LayoutBaselineFunction GetBaselineFunction(LayoutHandle handle)
    {
        return m_ManagedBaselineFunctions.GetValue(GetAccess().GetNodeData(handle).ManagedMeasureFunctionIndex);
    }

    public void SetBaselineFunction(LayoutHandle handle, LayoutBaselineFunction value)
    {
        if (GetAccess().GetNodeData(handle).ManagedOwnerIndex == 0) Debug.LogWarning("Setting Baseline method on a node with no Owner");
        ref var index = ref GetAccess().GetNodeData(handle).ManagedBaselineFunctionIndex;
        m_ManagedBaselineFunctions.UpdateValue(ref index, value);
    }
}
