// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.Layout;

[RequiredByNativeCode]
enum LayoutNodeDataType
{
    Node = 0,
    Computed = 1,
    Cache = 2,
    ComputedStyle = 3,
    Transform = 4,
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
        Shutdown // The SharedManager was disposed and must not be-created
    }
    static SharedManagerState s_Initialized;
    static bool s_AppDomainUnloadRegistered;

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

        if (!s_AppDomainUnloadRegistered)
        {
            // important: this will always be called from a special unload thread (main thread will be blocking on this)
#pragma warning disable UAC0006 // CORECLR_FIXME: CoreCLR would handle this using BeforeCodeUnloading on Shutdown
            AppDomain.CurrentDomain.DomainUnload += (_, __) =>
            {
               Shutdown();
            };
#pragma warning restore UAC0006

            s_AppDomainUnloadRegistered = true;
        }

        s_SharedInstance = new LayoutManager(Allocator.Persistent);

        fixed (UnmanagedDataStore* nodesPtr = &s_SharedInstance.m_Nodes)
            NativeTransformUtils.SetDataAccess((IntPtr)nodesPtr);
    }

    static void Shutdown()
    {
        if (s_Initialized != SharedManagerState.Initialized)
            return;

        s_Initialized = SharedManagerState.Shutdown;

        s_SharedInstance.Dispose(domainUnload: true);
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

    readonly ConcurrentQueue<UnmanagedDataHandle> m_NodesToFree = new();

    readonly UnmanagedDataHandle m_DefaultConfig;

    readonly ManagedObjectStore<LayoutMeasureFunction> m_ManagedMeasureFunctions = new(k_CapacitySmall);
    readonly ManagedObjectStore<LayoutBaselineFunction> m_ManagedBaselineFunctions = new(k_CapacitySmall);
    readonly ManagedObjectStore<GCHandle> m_ManagedOwners = new();

    readonly ProfilerMarker m_CollectMarker = new (ProfilerCategory.UIToolkit, "UIElements.CollectLayoutNodes");

    // Last allocated index in the store (0 mean index 0 is valid aka a node was allocated)
    int m_HighMark = -1;

    // Used in tests.
    public int NodeCapacity => m_Nodes.Capacity;

    private ComputedStyle m_InitialStyle = InitialStyle.Get();

    // Used in tests. Set to true for tests with nodes that don't have VisualElements driving them.
    // Normally, computed styles use the InitialStyle as their starting value,
    // but our tests that create layout nodes currently expect a "default" computed style instead.
    internal bool OverrideInitialStyle
    {
        set => m_InitialStyle = value ? ComputedStyle.CreateInitial() : InitialStyle.Get();
    }

    internal static LayoutManager GetManager(int index)
        => (uint) index < s_Managers.Count ? s_Managers[index] : null;

    public LayoutManager(Allocator allocator) : this(allocator, DefaultCapacity) {}

    public LayoutManager(Allocator allocator, int initialNodeCapacity)
    {
        m_Index = s_Managers.Count;
        s_Managers.Add(this);

        var nodeComponentTypes = new[]
        {
            UnmanagedComponentType.Create<LayoutNodeData>(),
            UnmanagedComponentType.Create<LayoutComputedData>(),
            UnmanagedComponentType.Create<LayoutCacheData>(),
            UnmanagedComponentType.Create<ComputedStyle>(),
            UnmanagedComponentType.Create<VisualElementTransformData>(),
        };

        const string areaName = nameof(UIElements);
        ReadOnlySpan<MemoryLabel> nodeComponentLabels = stackalloc MemoryLabel[]
        {
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutNodeData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutComputedData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutCacheData)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(ComputedStyle)}>"),
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(VisualElementTransformData)}>"),
        };

        var configComponentTypes = new[]
        {
            UnmanagedComponentType.Create<LayoutConfigData>()
        };
        ReadOnlySpan<MemoryLabel> configComponentLabels = stackalloc MemoryLabel[]
        {
            new MemoryLabel(areaName, $"Layout.ComponentData<{nameof(LayoutConfigData)}>"),
        };

        m_Nodes = new UnmanagedDataStore(nodeComponentTypes, nodeComponentLabels, initialNodeCapacity, allocator);
        m_Configs = new UnmanagedDataStore(configComponentTypes, configComponentLabels, k_InitialConfigCapacity, allocator);

        m_DefaultConfig = CreateConfig().Handle;
    }

    // Called by unit tests
    public void Dispose() => Dispose(false);

    private void Dispose(bool domainUnload)
    {
        s_Managers[m_Index] = null;

        unsafe
        {
            // if m_HighMark == 0, then a single node was allocated and we need to dispose it
            for (var i = 0; i <= m_HighMark; i++)
            {
                var cache = (LayoutCacheData*)m_Nodes.GetComponentDataPtr(i, (int)LayoutNodeDataType.Cache);
                cache->ClearCachedMeasurements();

                var data = (LayoutNodeData*) m_Nodes.GetComponentDataPtr(i, (int)LayoutNodeDataType.Node);

                if (data->Children.IsCreated)
                {
                    data->Children.Dispose();
                    data->Children = new();
                }

                var computedStyle = (ComputedStyle*) m_Nodes.GetComponentDataPtr(i, (int)LayoutNodeDataType.ComputedStyle);
                // During domain reload in CreateEditorWindowTests, there is one element that has an uninitialized
                // style which causes a NullReferenceException if we don't use SafeRelease here.
                if (domainUnload)
                    computedStyle->SafeRelease();
                else
                    computedStyle->Release();

                var owner = m_ManagedOwners.GetValue(data->ManagedOwnerIndex);
                if (owner.IsAllocated)
                    owner.Free();
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

    unsafe LayoutNode CreateNodeInternal(UnmanagedDataHandle configHandle)
    {
        TryRecycleSingleNode();

        var data = LayoutNodeData.Default;
        data.Config = configHandle;

        var computedStyle = m_InitialStyle.Acquire();

        var handle = m_Nodes.Allocate(
            data,
            LayoutComputedData.Default,
            LayoutCacheData.Default,
            computedStyle,
            VisualElementTransformData.Default
        );

        if (handle.Index > m_HighMark)
            m_HighMark = handle.Index;

        var node = new LayoutNode(GetAccess(), handle);

        Debug.Assert(!GetAccess().GetNodeData(handle).Children.IsCreated, "memory is not initialized" );
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
        ref var data = ref access.GetNodeData(handle);
        if (data.Children.IsCreated)
        {
            data.Children.Dispose();
            data.Children = new();
        }

        ref var cache = ref access.GetCacheData(handle);
        cache.ClearCachedMeasurements();

        data.UsesMeasure = false;
        data.UsesBaseline = false;

        ref var computedStyle = ref access.GetComputedStyle(handle);
        computedStyle.Release();

        //This assumes an internal behavior of the managed object store... invalid could be -1 instead
        if (data.ManagedOwnerIndex != 0)
        {
            GCHandle owner = m_ManagedOwners.GetValue(data.ManagedOwnerIndex);
            if (owner.IsAllocated)
                owner.Free();
            m_ManagedOwners.UpdateValue(ref data.ManagedOwnerIndex, default);
        }

        m_Nodes.Free(handle);
    }

    public void Collect()
    {
        using (m_CollectMarker.Auto())
            TryRecycleNodes();
    }

    public VisualElement GetOwner(UnmanagedDataHandle handle)
    {
        //This assumes an internal behavior of the managed object store... invalid could be -1 instead
        if (GetAccess().GetNodeData(handle).ManagedOwnerIndex == 0)
            return null;

        // Will throw if the weak referenc is not in the list
        return m_ManagedOwners.GetValue(GetAccess().GetNodeData(handle).ManagedOwnerIndex).Target as VisualElement;
    }

    public void SetOwner(UnmanagedDataHandle handle, VisualElement value)
    {
        if (value == null)
        {
            if (GetAccess().GetNodeData(handle).UsesMeasure) Debug.LogWarning("Node with no owner uses measure feature");
            if (GetAccess().GetNodeData(handle).UsesBaseline) Debug.LogWarning("Node with no owner uses baseline feature");
        }
        ref var index = ref GetAccess().GetNodeData(handle).ManagedOwnerIndex;

        GCHandle gcHandle = m_ManagedOwners.GetValue(index);
        if (gcHandle.IsAllocated)
            gcHandle.Free();

        if (value == null)
            gcHandle = default;
        else
            gcHandle = GCHandle.Alloc(value, GCHandleType.Weak);

        m_ManagedOwners.UpdateValue(ref index, gcHandle);
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
