// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define RD_DIAGNOSTICS

using System;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.UIR
{
    [StructLayout(LayoutKind.Sequential)]
    struct Transform3x4
    {
        public Vector4 v0, v1, v2;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ClippingData
    {
        public const int k_Size = 3 * 4 * sizeof(float);
        public Vector4 WorldClip, ViewClip, TransformClip;
    }

    // The values stored here could be updated behind the back of the holder of this
    // object. Hence, never turn this into a struct or else we can't do automatic
    // defragmentation and address ordering optimizations
    internal class MeshHandle : PoolItem
    {
        internal Alloc allocVerts, allocIndices;
        internal uint triangleCount; // Can be less than the actual indices if only a portion of the allocation is used
        internal Page allocPage;
        internal uint allocTime; // Frame this mesh was allocated/updated
        internal uint updateAllocID; // If not 0, the alloc here points to a temporary location managed by an update record with the said ID

        public void Reset()
        {
            allocVerts = new Alloc();
            allocIndices = new Alloc();
            triangleCount = 0;
            allocPage = null;
            allocTime = 0;
            updateAllocID = 0;
        }
    }

    interface IUIRenderDevice : IDisposable
    {
        MeshNodePool meshNodePool { get; }
        MeshRendererPool meshRendererPool { get; }
        StatePool statePool { get; }

        MeshHandle Allocate(uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset);
        Alloc AllocateTransform();
        Alloc AllocateClipping();

        void Update(MeshHandle mesh, uint vertexCount, out NativeSlice<Vertex> vertexData);
        void Update(MeshHandle mesh, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset);
        void UpdateTransform(Alloc alloc, Matrix4x4 newTransform);
        void UpdateClipping(Alloc alloc, Rect worldRect, Rect viewRect, Rect transformRect);

        void Free(MeshHandle mesh);
        void FreeTransform(Alloc alloc);
        void FreeClipping(Alloc alloc);

        bool supportsFragmentClipping { get; }
        Shader standardShader { get; set; }
        Material GetStandardMaterial();
        void DrawChain(RendererBase head, Rect viewport, Matrix4x4 projection, Texture atlas);
        void AdvanceFrame();
    }

    internal class UIRenderDevice : IUIRenderDevice
    {
        struct AllocToUpdate
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
            public NativeArray<DrawBufferRange> drawRanges;
            public List<NativeArray<Transform3x4>> transformPages;
            public ComputeBuffer[] transformBuffers;
            public List<NativeArray<ClippingData>> clippingPages;
            public ComputeBuffer[] clippingBuffers;

            public void Dispose()
            {
                while (page != null)
                {
                    Page pageToDispose = page;
                    page = page.next;
                    pageToDispose.Dispose();
                }

                if (transformPages != null)
                {
                    foreach (var transformPage in transformPages)
                        transformPage.Dispose();
                }
                if (transformBuffers != null)
                {
                    foreach (var transformBuffer in transformBuffers)
                    {
                        if (transformBuffer != null)
                            transformBuffer.Dispose();
                    }
                }
                if (clippingPages != null)
                {
                    foreach (var clippingPage in clippingPages)
                        clippingPage.Dispose();
                }
                if (clippingBuffers != null)
                {
                    foreach (var clippingBuffer in clippingBuffers)
                    {
                        if (clippingBuffer != null)
                            clippingBuffer.Dispose();
                    }
                }

                if (drawRanges.IsCreated)
                    drawRanges.Dispose();
            }
        }

        private const uint k_MaxQueuedFrameCount = 4; // Support drivers queuing up to 4 frames

        public bool verbose = false;

        // Those fields below are just for lazy creation
        private uint m_LazyCreationInitialTransformCapacity;
        private uint m_LazyCreationInitialClippingCapacity;
        private int m_LazyCreationDrawRangeRingSize;

        private Shader m_DefaultMaterialShader;
        private Material m_DefaultMaterial;
        private DrawingModes m_DrawingMode;
        private Page m_FirstPage;
        private uint m_NextPageVertexCount;
        private uint m_LargeMeshVertexCount;
        private float m_IndexToVertexCountRatio;

        BlockAllocator m_TransformAllocator;
        List<NativeArray<Transform3x4>> m_TransformPages;
        uint m_TransformBufferToUse;
        bool m_TransformBufferNeedsUpdate;
        ComputeBuffer[] m_TransformBuffers;

        // We need to make sure we're using version 4.5+ with OpenGL as this is the condition used by the shader
        // to determine whether StructuredBuffers are used or not
        public bool supportsFragmentClipping { get; } = SystemInfo.supportsComputeShaders && !OpenGLCoreBelow45();
        BlockAllocator m_ClippingAllocator;
        List<NativeArray<ClippingData>> m_ClippingPages;
        uint m_ClippingBufferToUse;
        bool m_ClippingBufferNeedsUpdate;
        ComputeBuffer[] m_ClippingBuffers;
        static readonly Vector4 s_InfiniteRect = new Vector4(-1000000, -1000000, 1000000, 1000000);

        private List<List<AllocToFree>> m_DeferredFrees;
        private List<List<AllocToUpdate>> m_Updates;
        private UInt32[] m_Fences;
        private NativeArray<DrawBufferRange> m_DrawRanges; // Size is powers of 2 strictly
        private int m_DrawRangeStart;
        private uint m_FrameIndex;
        private bool m_FrameIndexIncremented;
        private uint m_NextUpdateID = 1; // For the current frame only, 0 is not an accepted value here
        private static LinkedList<DeviceToFree> m_DeviceFreeQueue = new LinkedList<DeviceToFree>();   // Not thread safe for now
        private static int m_ActiveDeviceCount = 0; // Not thread safe for now
        private static bool m_SubscribedToNotifications; // Not thread safe for now
        private static bool m_SynchronousFree; // This is set on domain unload or app quit, so it is irreversible

        public MeshNodePool meshNodePool { get; } = new MeshNodePool();
        public MeshRendererPool meshRendererPool { get; } = new MeshRendererPool();
        public StatePool statePool { get; } = new StatePool();
        readonly MeshHandlePool m_MeshHandlePool = new MeshHandlePool();
        readonly DrawChainState m_DrawChainState = new DrawChainState();


        static UIRenderDevice()
        {
            UIR.Utility.EngineUpdate += OnEngineUpdateGlobal;
            UIR.Utility.FlushPendingResources += OnFlushPendingResources;
        }

        public enum DrawingModes { FlipY, StraightY, DisableClipping };

        public UIRenderDevice(Shader defaultMaterialShader, uint initialVertexCapacity = 0, uint initialIndexCapacity = 0, uint initialTransformCapacity = 1024, uint initialClippingCapacity = 128, DrawingModes drawingMode = DrawingModes.FlipY, int drawRangeRingSize = 1024)
        {
            Debug.Assert(!m_SynchronousFree); // Shouldn't create render devices when the app is quitting or domain-unloading
            if (m_ActiveDeviceCount++ == 0)
            {
                if (!m_SubscribedToNotifications)
                {
                    Utility.NotifyOfUIREvents(true);
                    m_SubscribedToNotifications = true;
                }
            }

            m_DefaultMaterialShader = defaultMaterialShader;
            m_DrawingMode = drawingMode;

            m_NextPageVertexCount = Math.Max(initialVertexCapacity, 2048); // No less than 4k vertices (doubled from 2k effectively when the first page is allocated)
            m_LargeMeshVertexCount = m_NextPageVertexCount;
            m_IndexToVertexCountRatio = (float)initialIndexCapacity / (float)initialVertexCapacity;
            m_IndexToVertexCountRatio = Mathf.Max(m_IndexToVertexCountRatio, 2);
            m_LazyCreationInitialTransformCapacity = initialTransformCapacity;
            m_LazyCreationInitialClippingCapacity = initialClippingCapacity;
            m_LazyCreationDrawRangeRingSize = Mathf.IsPowerOfTwo(drawRangeRingSize) ? drawRangeRingSize : Mathf.NextPowerOfTwo(drawRangeRingSize);

            m_DeferredFrees = new List<List<AllocToFree>>((int)k_MaxQueuedFrameCount);
            m_Updates = new List<List<AllocToUpdate>>((int)k_MaxQueuedFrameCount);
            for (int i = 0; i < k_MaxQueuedFrameCount; i++)
            {
                m_DeferredFrees.Add(new List<AllocToFree>());
                m_Updates.Add(new List<AllocToUpdate>());
            }
        }

        void CompleteCreation()
        {
            if (m_DrawRanges.IsCreated)
                return;

            // Initialize Skinning-Transform Data
            {
                var initialTransformCapacity = m_LazyCreationInitialTransformCapacity;
                bool unlimitedTransformCount = SystemInfo.supportsComputeShaders && !OpenGLCoreBelow45();
                if (!unlimitedTransformCount)
                    // This should be in sync with the fallback value of UIE_SKIN_ELEMS_COUNT_MAX_CONSTANTS in UnityUIE.cginc (minus one for the identity matrix)
                    initialTransformCapacity = 19;
                initialTransformCapacity = Math.Max(1, initialTransformCapacity); // Reserve one entry for "unskinned" meshes
                m_TransformAllocator = new BlockAllocator(unlimitedTransformCount ? uint.MaxValue : initialTransformCapacity);
                m_TransformPages = new List<NativeArray<Transform3x4>>(1);
                var firstTransformPage = new NativeArray<Transform3x4>((int)initialTransformCapacity + 1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                firstTransformPage[0] = new Transform3x4() { v0 = new Vector4(1, 0, 0, 0), v1 = new Vector4(0, 1, 0, 0), v2 = new Vector4(0, 0, 1, 0) };
                m_TransformPages.Add(firstTransformPage);
                if (unlimitedTransformCount)
                {
                    m_TransformBuffers = new ComputeBuffer[k_MaxQueuedFrameCount];
                    m_TransformBuffers[0] = new ComputeBuffer((int)initialTransformCapacity, sizeof(float) * 12, ComputeBufferType.Default);
                    m_TransformBuffers[0].SetData(firstTransformPage, 0, 0, 1);
                }
            }

            // Initialize Clipping Data
            if (supportsFragmentClipping)
            {
                var initialClippingCapacity = m_LazyCreationInitialClippingCapacity;
                m_ClippingAllocator = new BlockAllocator(uint.MaxValue);
                m_ClippingPages = new List<NativeArray<ClippingData>>(1);
                var firstClippingPage = new NativeArray<ClippingData>((int)initialClippingCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                firstClippingPage[0] = new ClippingData { WorldClip = s_InfiniteRect, ViewClip = s_InfiniteRect, TransformClip = s_InfiniteRect };
                m_ClippingPages.Add(firstClippingPage);
                m_ClippingBuffers = new ComputeBuffer[k_MaxQueuedFrameCount];
                m_ClippingBuffers[0] = new ComputeBuffer((int)initialClippingCapacity, ClippingData.k_Size, ComputeBufferType.Default);
                m_ClippingBuffers[0].SetData(firstClippingPage, 0, 0, 1);
            }

            m_DrawRanges = new NativeArray<DrawBufferRange>(m_LazyCreationDrawRangeRingSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Fences = new uint[(int)k_MaxQueuedFrameCount];

            UIR.Utility.EngineUpdate += OnEngineUpdate;
        }

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
                if (m_DrawRanges.IsCreated)
                    UIR.Utility.EngineUpdate -= OnEngineUpdate;

                UIRUtility.Destroy(m_DefaultMaterial);

                DeviceToFree free = new DeviceToFree
                {
                    handle = Utility.InsertCPUFence(),
                    page = m_FirstPage,
                    drawRanges = m_DrawRanges,
                    transformPages = m_TransformPages,
                    transformBuffers = m_TransformBuffers,
                    clippingPages = m_ClippingPages,
                    clippingBuffers = m_ClippingBuffers
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
            MeshHandle meshHandle = m_MeshHandlePool.Get();
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
                return;
            }

            AllocToUpdate allocToUpdate;
            UpdateAfterGPUUsedData(mesh, vertexCount, indexCount, out vertexData, out indexData, out indexOffset, out allocToUpdate, true);
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
            UnityEngine.Profiling.Profiler.BeginSample("UIR.Allocate");

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
                    m_NextPageVertexCount = Math.Min(m_NextPageVertexCount, 64 * 1024); // Stay below 64k for 16-bit indices
                    uint newPageIndexCount = (uint)(m_NextPageVertexCount * m_IndexToVertexCountRatio + 0.5f);
                    newPageIndexCount = Math.Max(newPageIndexCount, (uint)(indexCount * 2));
                    Debug.Assert(page?.next == null); // page MUST be the last page in the list, but can be null
                    page = new Page(m_NextPageVertexCount, newPageIndexCount, k_MaxQueuedFrameCount);
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

                // A huge mesh, push it to a page of its own. Put this page at the end so it won't be queried often
                Page lastPage = m_FirstPage;
                while (lastPage != null && lastPage.next != null)
                    lastPage = lastPage.next;

                Page dedicatedPage = new Page((uint)vertexCount, (uint)indexCount, k_MaxQueuedFrameCount);
                if (lastPage != null)
                    lastPage.next = dedicatedPage;
                else m_FirstPage = dedicatedPage;
                page = dedicatedPage;
                va = dedicatedPage.vertices.allocator.Allocate((uint)vertexCount, shortLived);
                ia = dedicatedPage.indices.allocator.Allocate((uint)indexCount, shortLived);
            }

            page.vertices.RegisterUpdate(va.start, va.size);
            page.indices.RegisterUpdate(ia.start, ia.size);

            vertexData = new NativeSlice<Vertex>(page.vertices.cpuData, (int)va.start, (int)vertexCount);
            indexData = new NativeSlice<UInt16>(page.indices.cpuData, (int)ia.start, (int)indexCount);

            meshHandle.allocPage = page;
            meshHandle.allocVerts = va;
            meshHandle.allocIndices = ia;
            meshHandle.allocTime = m_FrameIndex;

            UnityEngine.Profiling.Profiler.EndSample();
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

        static void UpdateHostBuffer<T>(Alloc alloc, T newValue, List<NativeArray<T>> pages) where T : struct
        {
            int itemIndex = (int)alloc.start;
            int pageLen = pages[0].Length;
            int pageIndex = itemIndex / pageLen;
            itemIndex -= pageIndex * pageLen;
            while (pages.Count <= pageIndex)
                pages.Add(new NativeArray<T>(pages[0].Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory));
            var page = pages[pageIndex];
            page[itemIndex] = newValue;
        }

        public Alloc AllocateTransform()
        {
            CompleteCreation();
            return m_TransformAllocator.Allocate();
        }

        public Alloc AllocateClipping()
        {
            Debug.Assert(supportsFragmentClipping);
            CompleteCreation();
            return m_ClippingAllocator.Allocate();
        }

        public void UpdateTransform(Alloc alloc, Matrix4x4 newTransform)
        {
            if (m_TransformBuffers != null)
                m_TransformBufferNeedsUpdate = true;

            UpdateHostBuffer(alloc, new Transform3x4 { v0 = newTransform.GetRow(0), v1 = newTransform.GetRow(1), v2 = newTransform.GetRow(2) }, m_TransformPages);
        }

        public void UpdateClipping(Alloc alloc, Rect worldRect, Rect viewRect, Rect transformRect)
        {
            Debug.Assert(supportsFragmentClipping);
            m_ClippingBufferNeedsUpdate = true;

            var clippingData = new ClippingData
            {
                WorldClip = new Vector4(worldRect.xMin, worldRect.yMin, worldRect.xMax, worldRect.yMax),
                ViewClip = new Vector4(viewRect.xMin, viewRect.yMin, viewRect.xMax, viewRect.yMax),
                TransformClip = new Vector4(transformRect.xMin, transformRect.yMin, transformRect.xMax, transformRect.yMax)
            };

            UpdateHostBuffer(alloc, clippingData, m_ClippingPages);
        }

        public void FreeTransform(Alloc alloc)
        {
            m_TransformAllocator.Free(alloc);
        }

        public void FreeClipping(Alloc alloc)
        {
            Debug.Assert(supportsFragmentClipping);
            m_ClippingAllocator.Free(alloc);
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


            m_MeshHandlePool.Return(mesh);
        }

        public Shader standardShader
        {
            get { return m_DefaultMaterialShader; }
            set
            {
                if (m_DefaultMaterialShader != value)
                {
                    m_DefaultMaterialShader = value;
                    UIRUtility.Destroy(m_DefaultMaterial);
                    m_DefaultMaterial = null;
                }
            }
        }

        public Material GetStandardMaterial()
        {
            if (m_DefaultMaterial == null && m_DefaultMaterialShader != null)
            {
                m_DefaultMaterial = new Material(m_DefaultMaterialShader);
                SetupStandardMaterial(m_DefaultMaterial, m_DrawingMode);
            }

            return m_DefaultMaterial;
        }

        static bool OpenGLCoreBelow45()
        {
            int maj, min;
            if (UIRUtility.GetOpenGLCoreVersion(out maj, out min))
            {
                if (maj == 4)
                    return min < 5;
                return maj < 4;
            }
            else return false;
        }

        static void SetupStandardMaterial(Material material, DrawingModes mode)
        {
            const CompareFunction compFront = CompareFunction.Always;
            const StencilOp passFront = StencilOp.Keep;
            const StencilOp zFailFront = StencilOp.Replace;
            const StencilOp failFront = StencilOp.Keep;

            const CompareFunction compBack = CompareFunction.Equal;
            const StencilOp passBack = StencilOp.Keep;
            const StencilOp zFailBack = StencilOp.Zero;
            const StencilOp failBack = StencilOp.Keep;

            if (mode == DrawingModes.FlipY)
            {
                material.SetInt("_StencilCompFront", (int)compBack);
                material.SetInt("_StencilPassFront", (int)passBack);
                material.SetInt("_StencilZFailFront", (int)zFailBack);
                material.SetInt("_StencilFailFront", (int)failBack);

                material.SetInt("_StencilCompBack", (int)compFront);
                material.SetInt("_StencilPassBack", (int)passFront);
                material.SetInt("_StencilZFailBack", (int)zFailFront);
                material.SetInt("_StencilFailBack", (int)failFront);
            }
            else if (mode == DrawingModes.StraightY)
            {
                material.SetInt("_StencilCompFront", (int)compFront);
                material.SetInt("_StencilPassFront", (int)passFront);
                material.SetInt("_StencilZFailFront", (int)zFailFront);
                material.SetInt("_StencilFailFront", (int)failFront);

                material.SetInt("_StencilCompBack", (int)compBack);
                material.SetInt("_StencilPassBack", (int)passBack);
                material.SetInt("_StencilZFailBack", (int)zFailBack);
                material.SetInt("_StencilFailBack", (int)failBack);
            }
            else if (mode == DrawingModes.DisableClipping)
            {
                material.SetInt("_StencilCompFront", (int)CompareFunction.Always);
                material.SetInt("_StencilPassFront", (int)StencilOp.Keep);
                material.SetInt("_StencilZFailFront", (int)StencilOp.Keep);
                material.SetInt("_StencilFailFront", (int)StencilOp.Keep);

                material.SetInt("_StencilCompBack", (int)CompareFunction.Always);
                material.SetInt("_StencilPassBack", (int)StencilOp.Keep);
                material.SetInt("_StencilZFailBack", (int)StencilOp.Keep);
                material.SetInt("_StencilFailBack", (int)StencilOp.Keep);
            }
        }

        static void UpdateComputeBuffer<T>(List<NativeArray<T>> pages, ComputeBuffer[] buffers, ref uint bufferToUse, int stride) where T : struct
        {
            int pageLength = pages[0].Length;
            bufferToUse = (bufferToUse + 1) % (uint)buffers.Length;
            int itemCount = pages.Count * pageLength;
            ComputeBuffer buffer = buffers[bufferToUse];
            if (buffer != null && buffer.count < itemCount)
            {
                buffer.Dispose();
                buffer = null;
            }
            if (buffer == null)
            {
                buffer = new ComputeBuffer(itemCount, stride, ComputeBufferType.Default);
                buffers[bufferToUse] = buffer;
            }

            for (int i = 0; i < pages.Count; i++)
                buffer.SetData(pages[i], 0, i * pageLength, pageLength);
        }

        void BeforeDraw()
        {
            if (!m_FrameIndexIncremented)
                AdvanceFrame();
            m_FrameIndexIncremented = false;

            // Send changes
            Page page = m_FirstPage;
            while (page != null)
            {
                page.vertices.SendUpdates();
                page.indices.SendUpdates();
                page = page.next;
            }

            if (m_TransformBufferNeedsUpdate)
            {
                m_TransformBufferNeedsUpdate = false;
                UpdateComputeBuffer(m_TransformPages, m_TransformBuffers, ref m_TransformBufferToUse, 12 * sizeof(float));
            }

            if (m_ClippingBufferNeedsUpdate)
            {
                m_ClippingBufferNeedsUpdate = false;
                UpdateComputeBuffer(m_ClippingPages, m_ClippingBuffers, ref m_ClippingBufferToUse, ClippingData.k_Size);
            }
        }

        // Called every frame to draw one entire UI window
        public void DrawChain(RendererBase head, Rect viewport, Matrix4x4 projection, Texture atlas)
        {
            BeforeDraw();
            Utility.ProfileDrawChainBegin();

            m_DrawChainState.Reset(m_DrawRangeStart, m_DrawRanges, viewport, projection, atlas, GetStandardMaterial(), m_TransformPages,
                m_TransformBuffers != null ? m_TransformBuffers[m_TransformBufferToUse] : null,
                m_ClippingBuffers != null ? m_ClippingBuffers[m_ClippingBufferToUse] : null);
            ContinueChain(head, m_DrawChainState, false);
            m_DrawRangeStart = m_DrawChainState.CloseAndReturnRangesNewStart();
            Utility.ProfileDrawChainEnd();

            if (m_Fences != null)
                m_Fences[(int)(m_FrameIndex % m_Fences.Length)] = Utility.InsertCPUFence();

        }

        internal static void ContinueChain(RendererBase head, DrawChainState dcs, bool outerChainsWithMeshRenderer)
        {
            while (head != null)
            {
                dcs.nextInChainIsMeshRenderer = (head.next != null) ? head.next.chainsWithMeshRenderer : outerChainsWithMeshRenderer;
                head.Draw(dcs);
                head = head.next;
            }
        }

        public void AdvanceFrame()
        {
            m_FrameIndex++;
            m_FrameIndexIncremented = true;
            m_NextUpdateID = 1; // Reset

            if (m_Fences != null)
            {
                int fenceIndex = (int)(m_FrameIndex % m_Fences.Length);
                uint fence = m_Fences[fenceIndex];
                if (fence != 0 && !Utility.CPUFencePassed(fence))
                {
                    if (verbose)
                        Debug.LogWarning("Waiting for render thread synchronization.");
                    Utility.WaitOnCPUFence(fence);
                }
                m_Fences[fenceIndex] = 0;
            }

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

        public struct Statistics
        {
            public struct PageStatistics { public HeapStatistics vertices, indices; }
            public PageStatistics[] pages;
            public int[] freesDeferred;
            public int currentFrameIndex;
            public int currentDrawRangeStart;
            public bool completeInit;
        }
        public Statistics GatherStatistics()
        {
            Statistics stats = new Statistics();
            stats.currentFrameIndex = (int)m_FrameIndex;
            stats.currentDrawRangeStart = m_DrawRangeStart;
            stats.completeInit = m_DrawRanges.IsCreated;
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
            stats.pages = new Statistics.PageStatistics[pageCount];
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

        #region Internals
        private void OnEngineUpdate()
        {
            AdvanceFrame();
        }

        private static void ProcessDeviceFreeQueue()
        {
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

    internal class DrawChainState
    {
        public void Reset(int rangesStart,
            NativeArray<DrawBufferRange> ranges,
            Rect viewport,
            Matrix4x4 proj,
            Texture atlas,
            Material defaultMat,
            List<NativeArray<Transform3x4>> transforms,
            ComputeBuffer transformsBuffer,
            ComputeBuffer clippingBuffer)
        {
            state?.Reset();
            m_StateIsValid = false;

            // Default initialization
            pageObj = null;
            currentDrawRange = new DrawBufferRange();
            nextInChainIsMeshRenderer = false;
            scissorRect = new Rect();
            scissorCount = 0;
            m_RangesReady = 0;

            // Parameter-based initialization
            m_RangesStart = rangesStart;
            m_Ranges = ranges;
            this.viewport = viewport;
            view = Matrix4x4.identity;
            projection = proj;
            this.atlas = atlas;
            m_DefaultMaterial = defaultMat;
            m_TransformsForConstants = transforms;
            m_TransformsAsStructBuffer = transformsBuffer;
            m_ClippingBuffer = clippingBuffer;
            if (m_DefaultMaterial != null)
            {
                if (m_TransformsAsStructBuffer != null)
                    m_DefaultMaterial.SetBuffer(s_TransformsBufferPropID, m_TransformsAsStructBuffer);
                else if (m_TransformsForConstants != null)
                    UIR.Utility.SetVectorArray<Transform3x4>(m_DefaultMaterial, s_TransformsPropID, m_TransformsForConstants[0]);
                if (m_ClippingBuffer != null)
                    m_DefaultMaterial.SetBuffer(s_ClippingBufferPropID, m_ClippingBuffer);
            }
        }

        internal State state { get; } = new State();
        internal object pageObj;
        internal DrawBufferRange currentDrawRange;
        internal bool nextInChainIsMeshRenderer;
        internal Rect viewport { get; private set; }
        internal Matrix4x4 view { get; set; }
        internal Matrix4x4 projection { get; private set; }
        internal Rect scissorRect { get; set; }
        internal int scissorCount { get; set; }
        internal Texture atlas { get; set; }
        internal Matrix4x4 GetTransform(uint transformID)
        {
            if (m_TransformsForConstants == null)
            {
                System.Diagnostics.Debug.Assert(transformID == 0);
                return Matrix4x4.identity;
            }
            int pageLen = m_TransformsForConstants[0].Length;
            int page = (int)transformID / pageLen;
            Transform3x4 transform = m_TransformsForConstants[page][(int)transformID - page * pageLen];
            Matrix4x4 mat = new Matrix4x4();
            mat.SetRow(0, transform.v0);
            mat.SetRow(1, transform.v1);
            mat.SetRow(2, transform.v2);
            mat.SetRow(3, new Vector4(0, 0, 0, 1));
            return mat;
        }


        bool m_StateIsValid;
        NativeArray<DrawBufferRange> m_Ranges;
        int m_RangesStart;
        int m_RangesReady;
        Material m_DefaultMaterial;
        List<NativeArray<Transform3x4>> m_TransformsForConstants;
        ComputeBuffer m_TransformsAsStructBuffer;
        ComputeBuffer m_ClippingBuffer;

        internal void StashCurrentAndOpenNewDrawRange()
        {
            if (currentDrawRange.indexCount > 0)
            {
                int wrapAroundIndex = (m_RangesStart + m_RangesReady++) & (m_Ranges.Length - 1);
                m_Ranges[wrapAroundIndex] = currentDrawRange; // Close the active range
            }
            currentDrawRange = new DrawBufferRange();
        }

        internal void InvalidateState()
        {
            state.Reset();
            m_StateIsValid = false;
        }

        /// <summary>
        /// Performs the draw calls for the ranges.
        /// </summary>
        internal void KickRanges()
        {
            StashCurrentAndOpenNewDrawRange();
            if (m_RangesReady == 0)
                return;

            Page page = (Page)pageObj;
            if (m_RangesStart + m_RangesReady <= m_Ranges.Length)
                Utility.DrawRanges(page.indices.gpuData, page.vertices.gpuData, new NativeSlice<DrawBufferRange>(m_Ranges, m_RangesStart, m_RangesReady));
            else
            {
                // Less common situation, the numbers straddles the end of the ranges buffer
                int firstRangeCount = m_Ranges.Length - m_RangesStart;
                int secondRangeCount = m_RangesReady - firstRangeCount;
                Utility.DrawRanges(page.indices.gpuData, page.vertices.gpuData, new NativeSlice<DrawBufferRange>(m_Ranges, m_RangesStart, firstRangeCount));
                Utility.DrawRanges(page.indices.gpuData, page.vertices.gpuData, new NativeSlice<DrawBufferRange>(m_Ranges, 0, secondRangeCount));
            }
            m_RangesStart = (m_RangesStart + m_RangesReady) & (m_Ranges.Length - 1);
            m_RangesReady = 0;
        }

        internal int CloseAndReturnRangesNewStart() { return m_RangesStart; }

        static readonly int s_FontTexPropID = Shader.PropertyToID("_FontTex");
        static readonly int s_CustomTexPropID = Shader.PropertyToID("_CustomTex");
        static readonly int s_1PixelClipInvViewPropID = Shader.PropertyToID("_1PixelClipInvView");
        static readonly int s_ViewportID = Shader.PropertyToID("_Viewport");
        static readonly int s_RenderTargetSize = Shader.PropertyToID("_RenderTargetSize");
        static readonly int s_TransformsPropID = Shader.PropertyToID("_Transforms");
        static readonly int s_TransformsBufferPropID = Shader.PropertyToID("_TransformsBuffer");
        static readonly int s_ClippingBufferPropID = Shader.PropertyToID("_ClippingBuffer");
        internal void SetState(State newState)
        {
            Debug.Assert(newState != null);

            StateFields overrides = state.OverrideWith(newState);
            if (m_StateIsValid)
            {
                if (overrides == StateFields.None)
                    return;

                // Kick any pending ranges before the overrides are applied.
                KickRanges();
            }
            else
                m_StateIsValid = true;

            Material mat = state.material != null ? state.material : m_DefaultMaterial;
            mat.mainTexture = atlas;
            if (state.custom != null)
                mat.SetTexture(s_CustomTexPropID, state.custom);
            if (state.font != null)
                mat.SetTexture(s_FontTexPropID, state.font);
            if (mat != m_DefaultMaterial)
            {
                if (m_TransformsAsStructBuffer != null)
                    mat.SetBuffer(s_TransformsBufferPropID, m_TransformsAsStructBuffer);
                else if (m_TransformsForConstants != null)
                    UIR.Utility.SetVectorArray<Transform3x4>(mat, s_TransformsPropID, m_TransformsForConstants[0]);
                if (m_ClippingBuffer != null)
                    mat.SetBuffer(s_ClippingBufferPropID, m_ClippingBuffer);
            }

            var viewport = Utility.GetViewport();
            mat.SetVector(s_ViewportID, new Vector4(viewport.x, viewport.y, viewport.width, viewport.height));

            // Size of 1 pixel in clip space.
            Vector4 _1PixelClipInvView;
            _1PixelClipInvView.x = 2.0f / viewport.width;
            _1PixelClipInvView.y = 2.0f / viewport.height;

            // Pixel density in group space.
            Matrix4x4 matVPInv = (projection * view).inverse;
            Vector3 v = matVPInv.MultiplyVector(new Vector3(_1PixelClipInvView.x, _1PixelClipInvView.y));
            _1PixelClipInvView.z = 1 / (Mathf.Abs(v.x) + Mathf.Epsilon);
            _1PixelClipInvView.w = 1 / (Mathf.Abs(v.y) + Mathf.Epsilon);

            mat.SetVector(s_1PixelClipInvViewPropID, _1PixelClipInvView);

            Vector2 renderTargetSize = UIRUtility.GetRenderTargetSize();
            mat.SetVector(s_RenderTargetSize, renderTargetSize);

            mat.SetPass(0);
            GL.modelview = view;
            GL.LoadProjectionMatrix(projection);
        }
    }
}
