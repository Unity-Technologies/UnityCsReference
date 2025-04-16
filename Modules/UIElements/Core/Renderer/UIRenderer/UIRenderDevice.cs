// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define RD_DIAGNOSTICS

using System;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Profiling;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    [StructLayout(LayoutKind.Sequential)]
    struct Transform3x4
    {
        public Vector4 v0, v1, v2;
    }

    // The values stored here could be updated behind the back of the holder of this
    // object. Hence, never turn this into a struct or else we can't do automatic
    // defragmentation and address ordering optimizations
    internal class MeshHandle : LinkedPoolItem<MeshHandle>
    {
        internal Alloc allocVerts, allocIndices;
        internal uint triangleCount; // Can be less than the actual indices if only a portion of the allocation is used
        internal Page allocPage;
        internal uint allocTime; // Frame this mesh was allocated/updated
        internal uint updateAllocID; // If not 0, the alloc here points to a temporary location managed by an update record with the said ID
    }

    internal class UIRenderDevice : IDisposable
    {
        internal struct AllocToUpdate
        {
            public uint id; // Never 0
            public uint allocTime; // Frame this update was registered
            public MeshHandle meshHandle;
            public Alloc permAllocVerts, permAllocIndices;
            public Page permPage;
            public bool copyBackIndices;
        }

        struct AllocToFree
        {
            public Alloc alloc;
            public Page page;
            public bool vertices;
        }

        struct DeviceToFree
        {
            public UInt32 handle;
            public Page page;
            public List<CommandList>[] commandLists;

            public void Dispose()
            {
                while (page != null)
                {
                    Page pageToDispose = page;
                    page = page.next;
                    pageToDispose.Dispose();
                }

                if (commandLists != null)
                {
                    for (int i = 0; i < commandLists.Length; ++i)
                    {
                        foreach (var c in commandLists[i])
                            c.Dispose();
                        commandLists[i] = null;
                    }
                }
            }
        }

        internal const uint k_MaxQueuedFrameCount = 4; // Support drivers queuing up to 4 frames
        internal const int k_PruneEmptyPageFrameCount = 60; // Empty pages will be pruned if they are empty for x consecutive frames.

        readonly bool m_MockDevice; // Don't access GfxDevice resources nor submit commands of any sort, used for tests

        IntPtr m_DefaultStencilState;
        IntPtr m_VertexDecl;
        Page m_FirstPage;
        uint m_NextPageVertexCount;
        uint m_LargeMeshVertexCount;
        float m_IndexToVertexCountRatio;
        List<List<AllocToFree>> m_DeferredFrees;
        List<List<AllocToUpdate>> m_Updates;
        List<CommandList>[] m_CommandLists;
        UInt32[] m_Fences;
        MaterialPropertyBlock m_ConstantProps; // Properties that are constant throughout the evaluation of the commands
        MaterialPropertyBlock m_BatchProps;
        uint m_FrameIndex;
        uint m_NextUpdateID = 1; // For the current frame only, 0 is not an accepted value here
        DrawStatistics m_DrawStats;

        readonly LinkedPool<MeshHandle> m_MeshHandles = new LinkedPool<MeshHandle>(() => new MeshHandle(), mh => {});
        readonly DrawParams m_DrawParams = new DrawParams();
        readonly TextureSlotManager m_TextureSlotManager = new TextureSlotManager();

        static LinkedList<DeviceToFree> m_DeviceFreeQueue = new LinkedList<DeviceToFree>();   // Not thread safe for now
        static int m_ActiveDeviceCount = 0; // Not thread safe for now
        static bool m_SubscribedToNotifications; // Not thread safe for now
        static bool m_SynchronousFree; // This is set on domain unload or app quit, so it is irreversible

        static readonly int s_GradientSettingsTexID = Shader.PropertyToID("_GradientSettingsTex");
        static readonly int s_ShaderInfoTexID = Shader.PropertyToID("_ShaderInfoTex");

        static ProfilerMarker s_MarkerAllocate = new ProfilerMarker("UIR.Allocate");
        static ProfilerMarker s_MarkerFree = new ProfilerMarker("UIR.Free");
        static ProfilerMarker s_MarkerAdvanceFrame = new ProfilerMarker("UIR.AdvanceFrame");
        static ProfilerMarker s_MarkerFence = new ProfilerMarker("UIR.WaitOnFence");
        static ProfilerMarker s_MarkerBeforeDraw = new ProfilerMarker("UIR.BeforeDraw");

        internal static uint maxVerticesPerPage => 0xFFFF; // On DX11, 0xFFFF is an invalid index (associated to primitive restart). With size = 0xFFFF last index is 0xFFFE    cases:1259449
        internal bool breakBatches { get; set; }
        internal bool isFlat { get; set; }
        internal bool drawsInCameras { get; set; }
        internal uint frameIndex => m_FrameIndex;

        internal List<CommandList>[] commandLists => m_CommandLists;
        internal List<CommandList> currentFrameCommandLists => m_CommandLists == null ? null : m_CommandLists[(int)(m_FrameIndex % m_CommandLists.Length)];
        internal int currentFrameCommandListCount = 0;


        static UIRenderDevice()
        {
            UIR.Utility.EngineUpdate += OnEngineUpdateGlobal;
            UIR.Utility.FlushPendingResources += OnFlushPendingResources;
        }

        public UIRenderDevice(uint initialVertexCapacity = 0, uint initialIndexCapacity = 0) :
            this(initialVertexCapacity, initialIndexCapacity, false)
        {
        }

        protected UIRenderDevice(uint initialVertexCapacity, uint initialIndexCapacity, bool mockDevice)
        {
            m_MockDevice = mockDevice;
            Debug.Assert(!m_SynchronousFree); // Shouldn't create render devices when the app is quitting or domain-unloading
            Debug.Assert(k_PruneEmptyPageFrameCount > k_MaxQueuedFrameCount); // To prevent pending updates from attempting to access a pruned page.
            if (m_ActiveDeviceCount++ == 0)
            {
                if (!m_SubscribedToNotifications && !m_MockDevice)
                {
                    Utility.NotifyOfUIREvents(true);
                    m_SubscribedToNotifications = true;
                }
            }

            m_NextPageVertexCount = Math.Max(initialVertexCapacity/2, 2048); // No less than 4k vertices (doubled from 2k effectively when the first page is allocated)
            m_LargeMeshVertexCount = m_NextPageVertexCount;
            m_IndexToVertexCountRatio = (float)initialIndexCapacity / (float)initialVertexCapacity;
            m_IndexToVertexCountRatio = Mathf.Max(m_IndexToVertexCountRatio, 2);

            m_DeferredFrees = new List<List<AllocToFree>>((int)k_MaxQueuedFrameCount);
            m_Updates = new List<List<AllocToUpdate>>((int)k_MaxQueuedFrameCount);
            for (int i = 0; i < k_MaxQueuedFrameCount; i++)
            {
                m_DeferredFrees.Add(new List<AllocToFree>());
                m_Updates.Add(new List<AllocToUpdate>());
            }
        }

        void InitVertexDeclaration()
        {
            var vertexDecl = new VertexAttributeDescriptor[]
            {
                // Vertex position first
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),

                // Then UINT32 color
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),

                // Then UV
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),

                // TransformID page coordinate (XY), ClipRectID page coordinate (ZW), packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UNorm8, 4),

                // In-page index for (TransformID, ClipRectID, OpacityID, TextCoreID)
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.UNorm8, 4),

                // Flags (vertex type), all packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.UNorm8, 4),

                // OpacityID page coordinate (XY), Color-page/TextCore-setting (16-bit encoded in ZW), packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.UNorm8, 4),

                // SVG (16-bit encoded in XY), packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.UNorm8, 4),

                // Circle arcs
                new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, 4),

                // TextureID, to represent integers from 0 to 2048
                // Float32 is overkill for the time being but it avoids conversion issues on GLES2 and metal. We should
                // use a Float16 instead but this isn't a trivial because C# doesn't have a native "half" datatype.
                new VertexAttributeDescriptor(VertexAttribute.TexCoord7, VertexAttributeFormat.Float32, 1)
            };
            m_VertexDecl = Utility.GetVertexDeclaration(vertexDecl);
        }

        void CompleteCreation()
        {
            if (m_MockDevice || fullyCreated)
                return;

            InitVertexDeclaration();

            m_Fences = new uint[(int)k_MaxQueuedFrameCount];
            m_ConstantProps = new MaterialPropertyBlock();
            m_BatchProps = new MaterialPropertyBlock();
            m_DefaultStencilState = Utility.CreateStencilState(new StencilState
            {
                enabled = isFlat,
                readMask = 255,
                writeMask = 255,

                compareFunctionFront = CompareFunction.Equal,
                passOperationFront = StencilOp.Keep,
                failOperationFront = StencilOp.Keep,
                zFailOperationFront = StencilOp.IncrementSaturate, // Push

                compareFunctionBack = CompareFunction.Less,
                passOperationBack = StencilOp.Keep,
                failOperationBack = StencilOp.Keep,
                zFailOperationBack = StencilOp.DecrementSaturate, // Pop
            });

            m_CommandLists = new List<CommandList>[k_MaxQueuedFrameCount];
            for (int i = 0; i < k_MaxQueuedFrameCount; ++i)
                m_CommandLists[i] = new List<CommandList>();
        }

        bool fullyCreated { get { return m_Fences != null; } }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // For tests tear down
        internal void DisposeImmediate()
        {
            Debug.Assert(!m_SynchronousFree);
            m_SynchronousFree = true;
            Dispose();
            m_SynchronousFree = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            m_ActiveDeviceCount--;

            if (disposing)
            {
                DeviceToFree free = new DeviceToFree
                {
                    handle = m_MockDevice ? 0 : Utility.InsertCPUFence(),
                    page = m_FirstPage,
                    commandLists = m_CommandLists
                };
                if (free.handle == 0)
                    free.Dispose();
                else
                {
                    m_DeviceFreeQueue.AddLast(free);
                    if (m_SynchronousFree)
                        ProcessDeviceFreeQueue();
                }
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public MeshHandle Allocate(uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset)
        {
            MeshHandle meshHandle = m_MeshHandles.Get();
            meshHandle.triangleCount = indexCount / 3;
            Allocate(meshHandle, vertexCount, indexCount, out vertexData, out indexData, false);
            indexOffset = (UInt16)meshHandle.allocVerts.start;
            return meshHandle;
        }

        public void Update(MeshHandle mesh, uint vertexCount, out NativeSlice<Vertex> vertexData)
        {
            Debug.Assert(mesh.allocVerts.size >= (uint)vertexCount);
            if (mesh.allocTime == m_FrameIndex)
            {
                // Update right after allocation and the GPU hasn't used the data yet.. update same allocation
                vertexData = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, (int)vertexCount);
                return;
            }

            uint oldIndexOffset = mesh.allocVerts.start; // Cache this before it gets modified in the call to Update below
            NativeSlice<UInt16> oldIndexData = new NativeSlice<UInt16>(mesh.allocPage.indices.cpuData, (int)mesh.allocIndices.start, (int)mesh.allocIndices.size);
            UInt16 indexOffset;
            NativeSlice<UInt16> indexData;
            AllocToUpdate allocToUpdate;
            UpdateAfterGPUUsedData(mesh, vertexCount, mesh.allocIndices.size, out vertexData, out indexData, out indexOffset, out allocToUpdate, false);

            // Carry original indices, but repoint them at the new vertices
            int indexCount = (int)mesh.allocIndices.size;
            int indexDifference = (int)indexOffset - (int)oldIndexOffset;
            for (int i = 0; i < indexCount; i++)
                indexData[i] = (UInt16)(oldIndexData[i] + indexDifference);
        }

        public void Update(MeshHandle mesh, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset)
        {
            Debug.Assert(mesh.allocVerts.size >= (uint)vertexCount);
            Debug.Assert(mesh.allocIndices.size >= (uint)indexCount);
            if (mesh.allocTime == m_FrameIndex)
            {
                // Update right after allocation and the GPU hasn't used the data yet.. update same allocation
                vertexData = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, (int)vertexCount);
                indexData = mesh.allocPage.indices.cpuData.Slice((int)mesh.allocIndices.start, (int)indexCount);
                indexOffset = (UInt16)mesh.allocVerts.start;

                // Case 1419340, having a nudge update, followed by a visuals update which causes a winding order change
                // was not working because the copyBackIndices flag was left to "false".
                UpdateCopyBackIndices(mesh, true);

                return;
            }

            AllocToUpdate allocToUpdate;
            UpdateAfterGPUUsedData(mesh, vertexCount, indexCount, out vertexData, out indexData, out indexOffset, out allocToUpdate, true);
        }

        void UpdateCopyBackIndices(MeshHandle mesh, bool copyBackIndices)
        {
            if (mesh.updateAllocID == 0)
                return; // Not a alloc update

            int activeUpdateIndex = (int)(mesh.updateAllocID - 1); // -1 since 1 is the first update id in a frame
            var updates = ActiveUpdatesForMeshHandle(mesh);
            var update = updates[activeUpdateIndex];
            update.copyBackIndices = true;
            updates[activeUpdateIndex] = update;
        }

        internal List<AllocToUpdate> ActiveUpdatesForMeshHandle(MeshHandle mesh) // Internal for tests
        {
            return m_Updates[(int)mesh.allocTime % m_Updates.Count];
        }

        bool TryAllocFromPage(Page page, uint vertexCount, uint indexCount, ref Alloc va, ref Alloc ia, bool shortLived)
        {
            va = page.vertices.allocator.Allocate((uint)vertexCount, shortLived);
            if (va.size != 0)
            {
                ia = page.indices.allocator.Allocate((uint)indexCount, shortLived);
                if (ia.size != 0)
                    return true;

                page.vertices.allocator.Free(va); // There is space for the vertices, but not for the indices
                va.size = 0;
            }
            return false;
        }

        void Allocate(MeshHandle meshHandle, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, bool shortLived)
        {
            s_MarkerAllocate.Begin();

            Page page = null;
            Alloc va = new Alloc(), ia = new Alloc();


            if (vertexCount <= m_LargeMeshVertexCount)
            {
                // Search for a page that will accept this allocation
                if (m_FirstPage != null)
                {
                    page = m_FirstPage;
                    for (;;)
                    {
                        if (TryAllocFromPage(page, (uint)vertexCount, (uint)indexCount, ref va, ref ia, shortLived) || (page.next == null))
                            break;
                        else page = page.next;
                    }
                }
                else CompleteCreation();
                if (ia.size == 0)
                {
                    m_NextPageVertexCount <<= 1; // Double the vertex count
                    m_NextPageVertexCount = Math.Max(m_NextPageVertexCount, vertexCount * 2);
                    m_NextPageVertexCount = Math.Min(m_NextPageVertexCount, maxVerticesPerPage);  // Stay below 64k for 16-bit indices
                    uint newPageIndexCount = (uint)(m_NextPageVertexCount * m_IndexToVertexCountRatio + 0.5f);
                    newPageIndexCount = Math.Max(newPageIndexCount, (uint)(indexCount * 2));
                    Debug.Assert(page?.next == null); // page MUST be the last page in the list, but can be null
                    page = new Page(m_NextPageVertexCount, newPageIndexCount, k_MaxQueuedFrameCount, m_MockDevice);
                    // Link this new page to the head of the list so next allocations have more chance of succeeding rather than scanning through all pages to land in this page
                    page.next = m_FirstPage;
                    m_FirstPage = page;
                    va = page.vertices.allocator.Allocate((uint)vertexCount, shortLived);
                    ia = page.indices.allocator.Allocate((uint)indexCount, shortLived);
                    Debug.Assert(va.size != 0);
                    Debug.Assert(ia.size != 0);

                }
            }
            else
            {
                CompleteCreation();

                // Search for an empty page that offers the best fit.
                Page current = m_FirstPage;
                Page lastPage = m_FirstPage;
                int bestFitExtraVertices = int.MaxValue;
                while (current != null)
                {
                    int extraVertices = current.vertices.cpuData.Length - (int)vertexCount;
                    int extraIndices = current.indices.cpuData.Length - (int)indexCount;
                    if (current.isEmpty && extraVertices >= 0 && extraIndices >= 0 && extraVertices < bestFitExtraVertices)
                    {
                        // The page is empty and large enough and wastes less vertices.
                        page = current;
                        bestFitExtraVertices = extraVertices;
                    }

                    lastPage = current;
                    current = current.next;
                }

                if (page == null)
                {
                    // If we want to do an allocation larger than the maximum the render device support,
                    // we allocate a small page and let the alloc fails.
                    // The page itself is not going to be usable and will be feed after 60 frames.
                    // This is done because because the page is required when creating the native slice
                    var pageVertexCount = (vertexCount > maxVerticesPerPage) ? 2 : vertexCount;
                    Debug.Assert(vertexCount <= maxVerticesPerPage, "Requested Vertex count is above the limit. Alloc will fail.");

                    // A huge mesh, push it to a page of its own. Put this page at the end so it won't be queried often
                    page = new Page((uint)pageVertexCount, (uint)indexCount, k_MaxQueuedFrameCount, m_MockDevice);
                    if (lastPage != null)
                        lastPage.next = page;
                    else m_FirstPage = page;
                }

                va = page.vertices.allocator.Allocate((uint)vertexCount, shortLived);
                ia = page.indices.allocator.Allocate((uint)indexCount, shortLived);

            }


            Debug.Assert(va.size == vertexCount, "Vertices allocated != Vertices requested");
            Debug.Assert(ia.size == indexCount, "Indices allocated != Indices requested");

            // If the allocated VB or IB has a different size than expected, both are invalidated.
            // The user may check one buffer size but not the other.
            if (va.size != vertexCount || ia.size != indexCount)
            {
                if (va.handle != null)
                    page.vertices.allocator.Free(va);
                if (ia.handle != null)
                    page.vertices.allocator.Free(ia);

                ia = new Alloc();
                va = new Alloc();
            }

            page.vertices.RegisterUpdate(va.start, va.size);
            page.indices.RegisterUpdate(ia.start, ia.size);

            vertexData = new NativeSlice<Vertex>(page.vertices.cpuData, (int)va.start, (int)va.size);
            indexData = new NativeSlice<UInt16>(page.indices.cpuData, (int)ia.start, (int)ia.size);

            meshHandle.allocPage = page;
            meshHandle.allocVerts = va;
            meshHandle.allocIndices = ia;
            meshHandle.allocTime = m_FrameIndex;

            s_MarkerAllocate.End();
        }

        void UpdateAfterGPUUsedData(MeshHandle mesh, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset, out AllocToUpdate allocToUpdate, bool copyBackIndices)
        {
            allocToUpdate = new AllocToUpdate()
            { id = m_NextUpdateID++, allocTime = m_FrameIndex, meshHandle = mesh, copyBackIndices = copyBackIndices };
            Debug.Assert(m_NextUpdateID > 0); // Wrapped-around 4 billion in one frame?!

            // Replace the update record that is currently active on the mesh (if present)
            if (mesh.updateAllocID == 0)
            {
                allocToUpdate.permAllocVerts = mesh.allocVerts;
                allocToUpdate.permAllocIndices = mesh.allocIndices;
                allocToUpdate.permPage = mesh.allocPage;
            }
            else
            {
                int activeUpdateIndex = (int)(mesh.updateAllocID - 1); // -1 since 1 is the first update id in a frame
                var updates = m_Updates[(int)mesh.allocTime % m_Updates.Count];
                var oldUpdate = updates[activeUpdateIndex];
                Debug.Assert(oldUpdate.id == mesh.updateAllocID);

                allocToUpdate.copyBackIndices |= oldUpdate.copyBackIndices;
                allocToUpdate.permAllocVerts = oldUpdate.permAllocVerts;
                allocToUpdate.permAllocIndices = oldUpdate.permAllocIndices;
                allocToUpdate.permPage = oldUpdate.permPage;
                oldUpdate.allocTime = 0xFFFFFFFF; // Effectively disable the old update
                updates[activeUpdateIndex] = oldUpdate;

                var queueToFree = m_DeferredFrees[(int)(m_FrameIndex % (uint)m_DeferredFrees.Count)];
                queueToFree.Add(new AllocToFree() { alloc = mesh.allocVerts, page = mesh.allocPage, vertices = true });
                queueToFree.Add(new AllocToFree() { alloc = mesh.allocIndices, page = mesh.allocPage, vertices = false });
            }

            // Try to allocate from the same page, if we fail, we revert to the general case
            if (TryAllocFromPage(mesh.allocPage, (uint)vertexCount, (uint)indexCount, ref mesh.allocVerts, ref mesh.allocIndices, true))
            {
                mesh.allocPage.vertices.RegisterUpdate(mesh.allocVerts.start, mesh.allocVerts.size);
                mesh.allocPage.indices.RegisterUpdate(mesh.allocIndices.start, mesh.allocIndices.size);
            }
            else Allocate(mesh, vertexCount, indexCount, out vertexData, out indexData, true);

            mesh.triangleCount = indexCount / 3;
            mesh.updateAllocID = allocToUpdate.id; // Own the update for the mesh
            mesh.allocTime = allocToUpdate.allocTime;

            m_Updates[(int)(m_FrameIndex % m_Updates.Count)].Add(allocToUpdate);

            vertexData = new NativeSlice<Vertex>(mesh.allocPage.vertices.cpuData, (int)mesh.allocVerts.start, (int)vertexCount);
            indexData = new NativeSlice<UInt16>(mesh.allocPage.indices.cpuData, (int)mesh.allocIndices.start, (int)indexCount);
            indexOffset = (UInt16)mesh.allocVerts.start;
        }

        public void Free(MeshHandle mesh)
        {
            if (mesh.updateAllocID != 0) // Is there an update over this mesh
            {
                int activeUpdateIndex = (int)(mesh.updateAllocID - 1); // -1 since 1 is the first update id in a frame
                var updates = m_Updates[(int)mesh.allocTime % m_Updates.Count];
                var oldUpdate = updates[activeUpdateIndex];
                Debug.Assert(oldUpdate.id == mesh.updateAllocID);

                var queueToFree = m_DeferredFrees[(int)(m_FrameIndex % (uint)m_DeferredFrees.Count)];
                queueToFree.Add(new AllocToFree() { alloc = oldUpdate.permAllocVerts, page = oldUpdate.permPage, vertices = true });
                queueToFree.Add(new AllocToFree() { alloc = oldUpdate.permAllocIndices, page = oldUpdate.permPage, vertices = false });
                queueToFree.Add(new AllocToFree() { alloc = mesh.allocVerts, page = mesh.allocPage, vertices = true });
                queueToFree.Add(new AllocToFree() { alloc = mesh.allocIndices, page = mesh.allocPage, vertices = false });

                oldUpdate.allocTime = 0xFFFFFFFF; // Effectively disable the old update
                updates[activeUpdateIndex] = oldUpdate;
            }
            else if (mesh.allocTime != m_FrameIndex) // Was it potentially used by the GPU?
            {
                int queueIndex = (int)(m_FrameIndex % (uint)m_DeferredFrees.Count);
                m_DeferredFrees[queueIndex].Add(new AllocToFree() { alloc = mesh.allocVerts, page = mesh.allocPage, vertices = true });
                m_DeferredFrees[queueIndex].Add(new AllocToFree() { alloc = mesh.allocIndices, page = mesh.allocPage, vertices = false });
            }
            else
            {
                // Freeing in the same frame the allocation happened, totally redundant. As the GPU didn't use this data, we don't need to defer the free
                mesh.allocPage.vertices.allocator.Free(mesh.allocVerts);
                mesh.allocPage.indices.allocator.Free(mesh.allocIndices);
            }

            mesh.allocVerts = new Alloc();
            mesh.allocIndices = new Alloc();
            mesh.allocPage = null;
            mesh.updateAllocID = 0;
            m_MeshHandles.Return(mesh);

        }

        public void OnFrameRenderingBegin()
        {
            AdvanceFrame();
            m_DrawStats = new DrawStatistics();
            m_DrawStats.currentFrameIndex = (int)m_FrameIndex;

            s_MarkerBeforeDraw.Begin();

            // Send changes
            Page page = m_FirstPage;
            while (page != null)
            {
                page.vertices.SendUpdates();
                page.indices.SendUpdates();
                page = page.next;
            }
            s_MarkerBeforeDraw.End();

            // UUM-101410: We must update the fence now in case that multiple calls to Update() are performed without
            // Render() being called. Otherwise, the previous fence (which has already passed) won't be updated and we
            // might be modifying the update ranges buffer, or the vertex/index buffers while they're already being
            // copied by the render thread.
            UpdateFenceValue();
        }

        internal unsafe static NativeSlice<T> PtrToSlice<T>(void* p, int count) where T : struct
        {
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(p, UnsafeUtility.SizeOf<T>(), count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
            return slice;
        }

        struct EvaluationState
        {
            public CommandList activeCommandList;
            public MaterialPropertyBlock constantProps; // Must be applied on material change only
            public MaterialPropertyBlock batchProps;
            public Material defaultMat;

            public State curState;
            public Page curPage;

            public bool mustApplyMaterial;
            public bool mustApplyBatchProps; // Indicates that the "stateMatProps" must be applied.
            public bool mustApplyStencil;
        }

        // Before leaving an iteration over a command, the state that is altered by a draw command must be applied.
        // This must ONLY be called after stash/kick of the previous ranges has been performed.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        void ApplyDrawCommandState(RenderChainCommand cmd, int textureSlot, Material newMat, bool newMatDiffers, ref EvaluationState st)
        {
            if (newMatDiffers)
            {
                st.curState.material = newMat;
                st.mustApplyMaterial = true;
            }

            st.curPage = cmd.mesh.allocPage;

            if (cmd.state.texture != TextureId.invalid)
            {
                if (textureSlot < 0)
                {
                    textureSlot = m_TextureSlotManager.FindOldestSlot();
                    m_TextureSlotManager.Bind(cmd.state.texture, cmd.state.sdfScale, cmd.state.sharpness, textureSlot, st.batchProps, st.activeCommandList);
                    st.mustApplyBatchProps = true;
                }
                else
                    m_TextureSlotManager.MarkUsed(textureSlot);
            }

            if (cmd.state.stencilRef != st.curState.stencilRef)
            {
                st.curState.stencilRef = cmd.state.stencilRef;
                st.mustApplyStencil = true;
            }
        }

        // Before calling KickRanges, this method MUST be called to ensure that any information previously stored
        // in the property blocks is applied.
        void ApplyBatchState(ref EvaluationState st)
        {
            if (!m_MockDevice)
            {
                if (st.mustApplyMaterial)
                {
                    if (drawsInCameras)
                    {
                        // This is only the case with world rendering where we are likely on the render thread. This is
                        // very flaky and will need to be revisited anyway. For instance, we shouldn't access the device
                        // and it should not be possible to modify the render chain from the main thread while we run this.
                        Debug.LogError("Attempted to change material when it is not allowed to do so.");
                        return;
                    }

                    m_DrawStats.materialSetCount++;

                    st.curState.material.SetPass(0); // No multipass support, should it be even considered?
                    if (st.constantProps != null)
                        Utility.SetPropertyBlock(st.constantProps);

                    st.mustApplyBatchProps = true;
                    st.mustApplyStencil = true;
                }

                if (st.mustApplyBatchProps)
                {
                    if (st.activeCommandList == null)
                        Utility.SetPropertyBlock(st.batchProps);
                    else
                        st.activeCommandList.ApplyBatchProps();
                }

                if (st.mustApplyStencil)
                {
                    ++m_DrawStats.stencilRefChanges;
                    if (st.activeCommandList == null)
                        Utility.SetStencilState(m_DefaultStencilState, st.curState.stencilRef);
                }
            }

            st.mustApplyMaterial = false;
            st.mustApplyBatchProps = false;
            st.mustApplyStencil = false;

            m_TextureSlotManager.StartNewBatch();
        }

        public unsafe void EvaluateChain(RenderChainCommand head, Material initialMat, Material defaultMat, Texture gradientSettings, Texture shaderInfo,
            float pixelsPerPoint, ref Exception immediateException)
        {
            Utility.ProfileDrawChainBegin();

            bool doBreakBatches = this.breakBatches; // Keeping this on the stack for better performance

            int rangesCount = 1024; // Must be powers of two. TODO: Can be estimated better from the render chain command count
            DrawBufferRange* ranges = stackalloc DrawBufferRange[rangesCount];
            int rangesCountMinus1 = rangesCount - 1;
            int rangesStart = 0;
            int rangesReady = 0;
            DrawBufferRange curDrawRange = new DrawBufferRange();
            int curDrawIndex = -1;
            int disableCounter = 0;

            currentFrameCommandListCount = 0;

            var st = new EvaluationState
            {
                constantProps = m_ConstantProps,
                batchProps = m_BatchProps,
                defaultMat = defaultMat,
                curState = new State { material = initialMat },
                mustApplyBatchProps = true,
                mustApplyStencil = true
            };

            var drawParams = m_DrawParams;
            drawParams.Reset();
            if (!drawsInCameras)
                drawParams.renderTexture.Add(RenderTexture.active);

            m_TextureSlotManager.Reset();

            if (fullyCreated)
            {
                st.batchProps.Clear();
                if (gradientSettings != null)
                    st.constantProps.SetTexture(s_GradientSettingsTexID, gradientSettings);
                if (shaderInfo != null)
                    st.constantProps.SetTexture(s_ShaderInfoTexID, shaderInfo);
                if (!drawsInCameras)
                    Utility.SetPropertyBlock(st.constantProps);
            }

            while (head != null)
            {
                if (head.type == CommandType.BeginDisable)
                {
                    m_DrawStats.commandCount++;
                    disableCounter++;
                    head = head.next;
                    continue;
                }

                if (head.type == CommandType.EndDisable)
                {
                    m_DrawStats.commandCount++;
                    disableCounter--;
                    head = head.next;
                    continue;
                }

                if (disableCounter > 0)
                {
                    m_DrawStats.skippedCommandCount++;
                    head = head.next;
                    continue;
                }

                m_DrawStats.drawCommandCount += (head.type == CommandType.Draw ? 1u : 0u);

                bool isLastRange = curDrawRange.indexCount > 0 && rangesReady == rangesCount - 1;
                bool stashRange = false; // Should we close the contiguous draw range that we had before this command (if any)?
                bool kickRanges = false; // Should we draw all the ranges that we had accumulated so far?

                // The following data is fetched during the preprocessing phase below.
                // They are cached so we can skip many checks afterwards.
                bool mustApplyCmdState = false; // Whenever a state change is detected, this must be true.
                int textureSlot = -1; // This avoids looping to find the index again in ApplyDrawCommandState
                Material newMat = null; // This avoids repeating the null check in ApplyDrawCommandState
                bool newMatDiffers = false; // This avoids repeating the material comparison in ApplyDrawCommandState

                // Preprocessing here. We determine whether the command requires to break the batch, and how it should
                // be processed afterwards to avoid redundant computations. ** The state must NOT be altered in any way **
                if (head.type == CommandType.Draw)
                {
                    newMat = head.state.material != null ? head.state.material : defaultMat;
                    if (newMat != st.curState.material)
                    {
                        mustApplyCmdState = true;
                        newMatDiffers = true;
                        stashRange = true;
                        kickRanges = true;
                    }

                    if (head.mesh.allocPage != st.curPage)
                    {
                        mustApplyCmdState = true;
                        stashRange = true;
                        kickRanges = true;
                    }
                    else if (curDrawIndex != head.mesh.allocIndices.start + head.indexOffset)
                        stashRange = true; // Same page but discontinuous range.

                    if (head.state.texture != TextureId.invalid)
                    {
                        mustApplyCmdState = true;
                        textureSlot = m_TextureSlotManager.IndexOf(head.state.texture);
                        if (textureSlot < 0 && m_TextureSlotManager.FreeSlots < 1)
                        { // No more slots available.
                            stashRange = true;
                            kickRanges = true;
                        }
                    }

                    if (head.state.stencilRef != st.curState.stencilRef)
                    {
                        mustApplyCmdState = true;
                        stashRange = true;
                        kickRanges = true;
                    }

                    if (stashRange && isLastRange)
                    {
                        // The range we'll close is the last that we can store.
                        // TODO: This only works since ranges are serialized and will break once the ranges are
                        //       truly processed in a multi-threaded fashion without copies. When this happens, a new
                        //       mechanism will need to be implemented to handle the "ranges-buffer-full" condition. For
                        //       the time being, calling KickRanges will make the whole buffer available.
                        kickRanges = true;
                    }
                }
                else
                {
                    stashRange = true;
                    kickRanges = true;
                }

                if (doBreakBatches)
                {
                    stashRange = true;
                    kickRanges = true;
                }

                if (stashRange)
                {
                    // Stash the current draw range (from previous commands)
                    if (curDrawRange.indexCount > 0)
                    {
                        int wrapAroundIndex = (rangesStart + rangesReady++) & rangesCountMinus1;
                        ranges[wrapAroundIndex] = curDrawRange; // Close the active range (BEFORE the current command).
                        Debug.Assert(rangesReady < rangesCount || kickRanges);
                        curDrawRange = new DrawBufferRange();
                        m_DrawStats.drawRangeCount++;
                    }

                    if (head.type == CommandType.Draw)
                    {
                        curDrawRange.firstIndex = (int)head.mesh.allocIndices.start + head.indexOffset;
                        curDrawRange.indexCount = head.indexCount;
                        curDrawRange.vertsReferenced = (int)(head.mesh.allocVerts.size);
                        curDrawRange.minIndexVal = (int)head.mesh.allocVerts.start;
                        curDrawIndex = curDrawRange.firstIndex + head.indexCount;
                        m_DrawStats.totalIndices += (uint)head.indexCount;
                    }
                }
                else // Only continuous draw commands can get here because other commands force stash+kick
                {
                    // We can chain
                    if (curDrawRange.indexCount == 0)
                        curDrawIndex = curDrawRange.firstIndex = (int)head.mesh.allocIndices.start + head.indexOffset; // A first draw after a stash
                    curDrawRange.indexCount += head.indexCount;
                    int prevFirstVertex = curDrawRange.minIndexVal;
                    int curFirstVertex = (int)head.mesh.allocVerts.start;
                    int prevLastVertex = curDrawRange.minIndexVal + curDrawRange.vertsReferenced;
                    int curLastVertex = (int)(head.mesh.allocVerts.start + head.mesh.allocVerts.size);
                    curDrawRange.minIndexVal = Mathf.Min(prevFirstVertex, curFirstVertex);
                    curDrawRange.vertsReferenced = Mathf.Max(prevLastVertex, curLastVertex) - curDrawRange.minIndexVal;
                    curDrawIndex += head.indexCount;
                    m_DrawStats.totalIndices += (uint)head.indexCount;

                    if (mustApplyCmdState)
                        ApplyDrawCommandState(head, textureSlot, newMat, newMatDiffers, ref st);
                    head = head.next;
                    continue;
                }

                // Only the stashed ranges are kicked: curDrawRange will NOT be kicked.
                if (kickRanges)
                {
                    if (rangesReady > 0)
                    {
                        ApplyBatchState(ref st);
                        KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, st.curPage, st.activeCommandList);
                    }

                    if (fullyCreated && head.type == CommandType.CutRenderChain)
                    {
                        // Reuse command lists whenever possible
                        CommandList cmdList = null;
                        if (currentFrameCommandListCount < currentFrameCommandLists.Count)
                        {
                            cmdList = currentFrameCommandLists[currentFrameCommandListCount];
                            cmdList.Reset(head.owner);
                        }
                        else
                        {
                            cmdList = new CommandList(head.owner, m_VertexDecl, m_DefaultStencilState);
                            currentFrameCommandLists.Add(cmdList);
                        }

                        ++currentFrameCommandListCount;

                        st.activeCommandList = cmdList;
                        st.constantProps = st.activeCommandList.constantProps;
                        st.batchProps = st.activeCommandList.batchProps;

                        st.batchProps.Clear();
                        if (gradientSettings != null)
                            st.constantProps.SetTexture(s_GradientSettingsTexID, gradientSettings);
                        if (shaderInfo != null)
                            st.constantProps.SetTexture(s_ShaderInfoTexID, shaderInfo);

                        m_TextureSlotManager.Reset();
                    }

                    if (head.type != CommandType.Draw)
                    {
                        if (!m_MockDevice)
                            head.ExecuteNonDrawMesh(drawParams, pixelsPerPoint, ref immediateException);
                        if (head.type == CommandType.Immediate || head.type == CommandType.ImmediateCull || head.type == CommandType.BlitToPreviousRT || head.type == CommandType.PushRenderTexture || head.type == CommandType.PopDefaultMaterial || head.type == CommandType.PushDefaultMaterial)
                        {
                            st.curState.material = null; // A value that is unique to force material reset on next draw command
                            st.mustApplyMaterial = false;
                            m_DrawStats.immediateDraws++;

                            if (head.type == CommandType.PopDefaultMaterial)
                            {
                                int index = drawParams.defaultMaterial.Count - 1;
                                defaultMat = drawParams.defaultMaterial[index];
                                drawParams.defaultMaterial.RemoveAt(index);
                            }
                            if (head.type == CommandType.PushDefaultMaterial)
                            {
                                drawParams.defaultMaterial.Add(defaultMat);
                                defaultMat = head.state.material;
                            }
                        }
                    }
                } // If kick ranges

                if (head.type == CommandType.Draw && mustApplyCmdState)
                    ApplyDrawCommandState(head, textureSlot, newMat, newMatDiffers, ref st);

                head = head.next;
            } // While there are commands to execute

            // Kick any pending ranges, this usually occurs when the draw chain ends with a draw command.
            if (curDrawRange.indexCount > 0)
            {
                int wrapAroundIndex = (rangesStart + rangesReady++) & rangesCountMinus1;
                ranges[wrapAroundIndex] = curDrawRange;
            }

            if (rangesReady > 0)
            {
                ApplyBatchState(ref st);
                KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, st.curPage, st.activeCommandList);
            }

            Debug.Assert(disableCounter == 0, "Rendering disabled counter is not 0, indicating a mismatch of commands");

            UpdateFenceValue(); // TODO: Replace by GPU fence.

            Utility.ProfileDrawChainEnd();

        }

        unsafe void UpdateFenceValue()
        {
            if (m_Fences != null)
            {
                uint newFenceVal = Utility.InsertCPUFence();
                fixed(uint* fence = &m_Fences[(int)(m_FrameIndex % m_Fences.Length)])
                {
                    for (;;)
                    {
                        uint curFenceVal = *fence;
                        if (((int)(newFenceVal - curFenceVal)) <= 0) // This is the same test as in GfxDeviceWorker::WaitOnCPUFence(). Handles wrap around.
                            break; // Our newFenceVal is already older than the current one, so keep the current
                        int cmpOldVal = System.Threading.Interlocked.CompareExchange(ref *((int*)fence), (int)newFenceVal, (int)curFenceVal);
                        if (cmpOldVal == curFenceVal)
                            break; // The exchange succeeded, now newFenceVal is stored atomically in (*fence)
                    }
                }
            }
        }

        unsafe void KickRanges(DrawBufferRange* ranges, ref int rangesReady, ref int rangesStart, int rangesCount, Page curPage, CommandList commandList)
        {
            Debug.Assert(rangesReady > 0);

            if (rangesStart + rangesReady <= rangesCount)
            {
                if (!m_MockDevice)
                    DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, PtrToSlice<DrawBufferRange>(ranges + rangesStart, rangesReady), commandList);
                m_DrawStats.drawRangeCallCount++;
            }
            else
            {
                // Less common situation, the numbers straddles the end of the ranges buffer
                int firstRangeCount = rangesCount - rangesStart;
                int secondRangeCount = rangesReady - firstRangeCount;
                if (!m_MockDevice)
                {
                    DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, PtrToSlice<DrawBufferRange>(ranges + rangesStart, firstRangeCount), commandList);
                    DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, PtrToSlice<DrawBufferRange>(ranges, secondRangeCount), commandList);
                }

                m_DrawStats.drawRangeCallCount += 2;
            }

            rangesStart = (rangesStart + rangesReady) & (rangesCount - 1);
            rangesReady = 0;
        }

        unsafe void DrawRanges(Utility.GPUBuffer<ushort> ib, Utility.GPUBuffer<Vertex> vb, NativeSlice<DrawBufferRange> ranges, CommandList commandList)
        {
            if (commandList != null)
            {
                commandList.DrawRanges(ib, vb, ranges);
            }
            else if (!drawsInCameras)
            {
                IntPtr* vStream = stackalloc IntPtr[1];
                vStream[0] = vb.BufferPointer;
                Utility.DrawRanges(ib.BufferPointer, vStream, 1, new IntPtr(ranges.GetUnsafePtr()), ranges.Length, m_VertexDecl);
            }
        }

        // Used for testing purposes only (e.g. performance test warmup)
        internal void WaitOnAllCpuFences()
        {
            for (int i = 0; i < m_Fences.Length; ++i)
                WaitOnCpuFence(m_Fences[i]);
        }

        void WaitOnCpuFence(uint fence)
        {
            if (fence != 0 && !Utility.CPUFencePassed(fence))
            {
                s_MarkerFence.Begin();
                Utility.WaitForCPUFencePassed(fence);
                s_MarkerFence.End();
            }
        }

        public void AdvanceFrame()
        {
            s_MarkerAdvanceFrame.Begin();

            m_FrameIndex++;

            m_DrawStats.currentFrameIndex = (int)m_FrameIndex;

            if (m_Fences != null)
            {
                int fenceIndex = (int)(m_FrameIndex % m_Fences.Length);
                uint fence = m_Fences[fenceIndex];
                WaitOnCpuFence(fence);
                m_Fences[fenceIndex] = 0;
            }

            m_NextUpdateID = 1; // Reset
            var queueToFree = m_DeferredFrees[(int)(m_FrameIndex % (uint)m_DeferredFrees.Count)];
            foreach (var alloc in queueToFree)
            {
                if (alloc.vertices)
                    alloc.page.vertices.allocator.Free(alloc.alloc);
                else alloc.page.indices.allocator.Free(alloc.alloc);
                // Don't dispose the page for now
            }
            queueToFree.Clear(); // Doesn't trim excess, which is exactly what we want

            var queueToUpdate = m_Updates[(int)(m_FrameIndex % (uint)m_DeferredFrees.Count)];
            foreach (var update in queueToUpdate)
            {
                if (update.meshHandle.updateAllocID == update.id && update.meshHandle.allocTime == update.allocTime)
                {
                    NativeSlice<Vertex> srcVerts = new NativeSlice<Vertex>(update.meshHandle.allocPage.vertices.cpuData, (int)update.meshHandle.allocVerts.start, (int)update.meshHandle.allocVerts.size);
                    NativeSlice<Vertex> destVerts = new NativeSlice<Vertex>(update.permPage.vertices.cpuData, (int)update.permAllocVerts.start, (int)update.meshHandle.allocVerts.size);
                    destVerts.CopyFrom(srcVerts);
                    update.permPage.vertices.RegisterUpdate(update.permAllocVerts.start, update.meshHandle.allocVerts.size);

                    if (update.copyBackIndices)
                    {
                        NativeSlice<UInt16> srcIndices = new NativeSlice<UInt16>(update.meshHandle.allocPage.indices.cpuData, (int)update.meshHandle.allocIndices.start, (int)update.meshHandle.allocIndices.size);
                        NativeSlice<UInt16> destIndices = new NativeSlice<UInt16>(update.permPage.indices.cpuData, (int)update.permAllocIndices.start, (int)update.meshHandle.allocIndices.size);

                        // Carry original indices, but repoint them at the new vertices
                        int indexCount = destIndices.Length;
                        int indexDifference = (int)update.permAllocVerts.start - (int)update.meshHandle.allocVerts.start;
                        for (int i = 0; i < indexCount; i++)
                            destIndices[i] = (UInt16)(srcIndices[i] + indexDifference);

                        update.permPage.indices.RegisterUpdate(update.permAllocIndices.start, update.meshHandle.allocIndices.size);
                    }
                    queueToFree.Add(new AllocToFree() { alloc = update.meshHandle.allocVerts, page = update.meshHandle.allocPage, vertices = true });
                    queueToFree.Add(new AllocToFree() { alloc = update.meshHandle.allocIndices, page = update.meshHandle.allocPage, vertices = false });

                    update.meshHandle.allocVerts = update.permAllocVerts;
                    update.meshHandle.allocIndices = update.permAllocIndices;
                    update.meshHandle.allocPage = update.permPage;
                    update.meshHandle.updateAllocID = 0;
                }
            }
            queueToUpdate.Clear();

            PruneUnusedPages();

            s_MarkerAdvanceFrame.End();
        }

        void PruneUnusedPages()
        {
            Page current, firstToKeep, lastToKeep, firstToPrune, lastToPrune;
            firstToKeep = lastToKeep = firstToPrune = lastToPrune = null;

            // Find pages to keep/prune and update their consecutive-empty-frames counters.
            current = m_FirstPage;
            while (current != null)
            {
                if (!current.isEmpty)
                    current.framesEmpty = 0;
                else
                    ++current.framesEmpty;

                if (current.framesEmpty < k_PruneEmptyPageFrameCount)
                {
                    if (firstToKeep != null)
                        lastToKeep.next = current;
                    else
                        firstToKeep = current;
                    lastToKeep = current;
                }
                else
                {
                    if (firstToPrune != null)
                        lastToPrune.next = current;
                    else
                        firstToPrune = current;
                    lastToPrune = current;
                }

                Page next = current.next;
                current.next = null;
                current = next;
            }

            m_FirstPage = firstToKeep;

            // Prune pages.
            current = firstToPrune;
            while (current != null)
            {
                Page next = current.next;
                current.next = null;
                current.Dispose();
                current = next;
            }
        }

        internal static void PrepareForGfxDeviceRecreate()
        {
            m_ActiveDeviceCount += 1; // Don't let the count reach 0 and unsubscribe from GfxDeviceRecreate
        }

        internal static void WrapUpGfxDeviceRecreate() { m_ActiveDeviceCount -= 1; }
        internal static void FlushAllPendingDeviceDisposes()
        {
            Utility.SyncRenderThread();
            ProcessDeviceFreeQueue();
        }

        internal struct AllocationStatistics
        {
            public struct PageStatistics { internal HeapStatistics vertices, indices; }
            public PageStatistics[] pages;
            public int[] freesDeferred;
            public bool completeInit;
        }
        internal AllocationStatistics GatherAllocationStatistics()
        {
            AllocationStatistics stats = new AllocationStatistics();
            stats.completeInit = fullyCreated;
            stats.freesDeferred = new int[m_DeferredFrees.Count];
            for (int i = 0; i < m_DeferredFrees.Count; i++)
                stats.freesDeferred[i] = m_DeferredFrees[i].Count;
            int pageCount = 0;
            Page page = m_FirstPage;
            while (page != null)
            {
                pageCount++;
                page = page.next;
            }
            stats.pages = new AllocationStatistics.PageStatistics[pageCount];
            pageCount = 0;
            page = m_FirstPage;
            while (page != null)
            {
                stats.pages[pageCount].vertices = page.vertices.allocator.GatherStatistics();
                stats.pages[pageCount].indices = page.indices.allocator.GatherStatistics();
                pageCount++;
                page = page.next;
            }
            return stats;
        }

        internal struct DrawStatistics
        {
            public int currentFrameIndex;
            public uint totalIndices;
            public uint commandCount;
            public uint skippedCommandCount;
            public uint drawCommandCount;
            public uint disableCommandCount;
            public uint materialSetCount;
            public uint drawRangeCount;
            public uint drawRangeCallCount;
            public uint immediateDraws;
            public uint stencilRefChanges;
        }
        internal DrawStatistics GatherDrawStatistics() { return m_DrawStats; }


        #region Internals

        public static void ProcessDeviceFreeQueue()
        {
            s_MarkerFree.Begin();

            if (m_SynchronousFree)
                Utility.SyncRenderThread();

            var freeNode = m_DeviceFreeQueue.First;
            while (freeNode != null)
            {
                if (!Utility.CPUFencePassed(freeNode.Value.handle))
                    break;
                freeNode.Value.Dispose();
                m_DeviceFreeQueue.RemoveFirst();
                freeNode = m_DeviceFreeQueue.First;
            }

            // After synchronizing with the render thread, all cpu fences should pass.
            Debug.Assert(!m_SynchronousFree || m_DeviceFreeQueue.Count == 0);

            if (m_ActiveDeviceCount == 0 && m_SubscribedToNotifications)
            {
                Utility.NotifyOfUIREvents(false);
                m_SubscribedToNotifications = false;
            }

            s_MarkerFree.End();
        }

        private static void OnEngineUpdateGlobal()
        {
            ProcessDeviceFreeQueue();
        }

        private static void OnFlushPendingResources()
        {
            m_SynchronousFree = true;
            ProcessDeviceFreeQueue();
        }

        #endregion // Internals
    }
}
