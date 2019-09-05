// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define RD_DIAGNOSTICS

using System;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;

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
    internal class MeshHandle : PoolItem
    {
        internal Alloc allocVerts, allocIndices;
        internal uint triangleCount; // Can be less than the actual indices if only a portion of the allocation is used
        internal Page allocPage;
        internal uint allocTime; // Frame this mesh was allocated/updated
        internal uint updateAllocID; // If not 0, the alloc here points to a temporary location managed by an update record with the said ID
    }

    internal class UIRenderDevice : IDisposable
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

            public void Dispose()
            {
                while (page != null)
                {
                    Page pageToDispose = page;
                    page = page.next;
                    pageToDispose.Dispose();
                }
                if (drawRanges.IsCreated)
                    drawRanges.Dispose();
            }
        }

        private const uint k_MaxQueuedFrameCount = 4; // Support drivers queuing up to 4 frames

        private readonly bool m_MockDevice; // Don't access GfxDevice resources nor submit commands of any sort, used for tests

        // Those fields below are just for lazy creation
        private int m_LazyCreationDrawRangeRingSize;

        private Shader m_DefaultMaterialShader;
        private Material m_DefaultMaterial;
        private DrawingModes m_DrawingMode;
        private Page m_FirstPage;
        private uint m_NextPageVertexCount;
        private uint m_LargeMeshVertexCount;
        private float m_IndexToVertexCountRatio;
        private List<List<AllocToFree>> m_DeferredFrees;
        private List<List<AllocToUpdate>> m_Updates;
        private UInt32[] m_Fences;
        private NativeArray<DrawBufferRange> m_DrawRanges; // Size is powers of 2 strictly
        private int m_DrawRangeStart;
        private uint m_FrameIndex;
        private bool m_FrameIndexIncremented;
        private uint m_NextUpdateID = 1; // For the current frame only, 0 is not an accepted value here
        private DrawStatistics m_DrawStats;
        private bool m_APIUsesStraightYCoordinateSystem;

        readonly Pool<MeshHandle> m_MeshHandles = new Pool<MeshHandle>();
        readonly DrawParams m_DrawParams = new DrawParams();

        private static LinkedList<DeviceToFree> m_DeviceFreeQueue = new LinkedList<DeviceToFree>();   // Not thread safe for now
        private static int m_ActiveDeviceCount = 0; // Not thread safe for now
        private static bool m_SubscribedToNotifications; // Not thread safe for now
        private static bool m_SynchronousFree; // This is set on domain unload or app quit, so it is irreversible

        static readonly int s_FontTexPropID = Shader.PropertyToID("_FontTex");
        static readonly int s_CustomTexPropID = Shader.PropertyToID("_CustomTex");
        static readonly int s_1PixelClipInvViewPropID = Shader.PropertyToID("_1PixelClipInvView");
        static readonly int s_GradientSettingsTexID = Shader.PropertyToID("_GradientSettingsTex");
        static readonly int s_ShaderInfoTexID = Shader.PropertyToID("_ShaderInfoTex");
        static readonly int s_PixelClipRectPropID = Shader.PropertyToID("_PixelClipRect");
        static readonly int s_TransformsPropID = Shader.PropertyToID("_Transforms");
        static readonly int s_ClipRectsPropID = Shader.PropertyToID("_ClipRects");

        static CustomSampler s_AllocateSampler = CustomSampler.Create("UIR.Allocate");
        static CustomSampler s_FreeSampler = CustomSampler.Create("UIR.Free");
        static CustomSampler s_AdvanceFrameSampler = CustomSampler.Create("UIR.AdvanceFrame");
        static CustomSampler s_FenceSampler = CustomSampler.Create("UIR.WaitOnFence");
        static CustomSampler s_BeforeDrawSampler = CustomSampler.Create("UIR.BeforeDraw");

        static bool? s_VertexTexturingIsAvailable;
        const string k_VertexTexturingIsAvailableTag = "UIE_VertexTexturingIsAvailable";
        const string k_VertexTexturingIsAvailableTrue = "1";


        static UIRenderDevice()
        {
            UIR.Utility.EngineUpdate += OnEngineUpdateGlobal;
            UIR.Utility.FlushPendingResources += OnFlushPendingResources;
        }

        public enum DrawingModes { FlipY, StraightY, DisableClipping }

        public UIRenderDevice(Shader defaultMaterialShader, uint initialVertexCapacity = 0, uint initialIndexCapacity = 0, DrawingModes drawingMode = DrawingModes.FlipY, int drawRangeRingSize = 1024) :
            this(defaultMaterialShader, initialVertexCapacity, initialIndexCapacity, drawingMode, drawRangeRingSize, false)
        {
        }

        // This protected constructor creates a "mock" render device
        protected UIRenderDevice(uint initialVertexCapacity = 0, uint initialIndexCapacity = 0, DrawingModes drawingMode = DrawingModes.FlipY, int drawRangeRingSize = 1024) :
            this(null, initialVertexCapacity, initialIndexCapacity, drawingMode, drawRangeRingSize, true)
        {
        }

        private UIRenderDevice(Shader defaultMaterialShader, uint initialVertexCapacity, uint initialIndexCapacity, DrawingModes drawingMode, int drawRangeRingSize, bool mockDevice)
        {
            m_MockDevice = mockDevice;
            Debug.Assert(!m_SynchronousFree); // Shouldn't create render devices when the app is quitting or domain-unloading
            if (m_ActiveDeviceCount++ == 0)
            {
                if (!m_SubscribedToNotifications && !m_MockDevice)
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
            m_LazyCreationDrawRangeRingSize = Mathf.IsPowerOfTwo(drawRangeRingSize) ? drawRangeRingSize : Mathf.NextPowerOfTwo(drawRangeRingSize);

            m_DeferredFrees = new List<List<AllocToFree>>((int)k_MaxQueuedFrameCount);
            m_Updates = new List<List<AllocToUpdate>>((int)k_MaxQueuedFrameCount);
            for (int i = 0; i < k_MaxQueuedFrameCount; i++)
            {
                m_DeferredFrees.Add(new List<AllocToFree>());
                m_Updates.Add(new List<AllocToUpdate>());
            }

            m_APIUsesStraightYCoordinateSystem =
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
        }

        // TODO: Remove this once case 1148851 has been fixed.
        static internal Func<Shader> getEditorShader = null;

        #region Default system resources
        static private Texture2D s_WhiteTexel;
        static internal Texture2D whiteTexel
        {
            get
            {
                if (s_WhiteTexel == null)
                {
                    s_WhiteTexel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    s_WhiteTexel.hideFlags = HideFlags.HideAndDontSave;
                    s_WhiteTexel.filterMode = FilterMode.Bilinear; // Make sure it's bilinear so UIRAtlasManager accepts it on older HW targets
                    s_WhiteTexel.SetPixel(0, 0, Color.white);
                    s_WhiteTexel.Apply(false, true);
                }
                return s_WhiteTexel;
            }
        }

        static private Texture2D s_DefaultShaderInfoTexFloat, s_DefaultShaderInfoTexARGB8;
        static internal Texture2D defaultShaderInfoTexFloat
        {
            get
            {
                if (s_DefaultShaderInfoTexFloat == null)
                {
                    s_DefaultShaderInfoTexFloat = new Texture2D(64, 64, TextureFormat.RGBAFloat, false); // No mips
                    s_DefaultShaderInfoTexFloat.hideFlags = HideFlags.HideAndDontSave;
                    s_DefaultShaderInfoTexFloat.filterMode = FilterMode.Point;
                    s_DefaultShaderInfoTexFloat.SetPixel(UIRVEShaderInfoAllocator.identityTransformTexel.x, UIRVEShaderInfoAllocator.identityTransformTexel.y + 0, UIRVEShaderInfoAllocator.identityTransformRow0Value);
                    s_DefaultShaderInfoTexFloat.SetPixel(UIRVEShaderInfoAllocator.identityTransformTexel.x, UIRVEShaderInfoAllocator.identityTransformTexel.y + 1, UIRVEShaderInfoAllocator.identityTransformRow1Value);
                    s_DefaultShaderInfoTexFloat.SetPixel(UIRVEShaderInfoAllocator.identityTransformTexel.x, UIRVEShaderInfoAllocator.identityTransformTexel.y + 2, UIRVEShaderInfoAllocator.identityTransformRow2Value);
                    s_DefaultShaderInfoTexFloat.SetPixel(UIRVEShaderInfoAllocator.infiniteClipRectTexel.x, UIRVEShaderInfoAllocator.infiniteClipRectTexel.y, UIRVEShaderInfoAllocator.infiniteClipRectValue);
                    s_DefaultShaderInfoTexFloat.SetPixel(UIRVEShaderInfoAllocator.fullOpacityTexel.x, UIRVEShaderInfoAllocator.fullOpacityTexel.y, UIRVEShaderInfoAllocator.fullOpacityValue);
                    s_DefaultShaderInfoTexFloat.Apply(false, true);
                }
                return s_DefaultShaderInfoTexFloat;
            }
        }
        static internal Texture2D defaultShaderInfoTexARGB8
        {
            get
            {
                if (s_DefaultShaderInfoTexARGB8 == null)
                {
                    s_DefaultShaderInfoTexARGB8 = new Texture2D(64, 64, TextureFormat.RGBA32, false); // No mips
                    s_DefaultShaderInfoTexARGB8.hideFlags = HideFlags.HideAndDontSave;
                    s_DefaultShaderInfoTexARGB8.filterMode = FilterMode.Point;
                    s_DefaultShaderInfoTexARGB8.SetPixel(UIRVEShaderInfoAllocator.fullOpacityTexel.x, UIRVEShaderInfoAllocator.fullOpacityTexel.y, UIRVEShaderInfoAllocator.fullOpacityValue);
                    s_DefaultShaderInfoTexARGB8.Apply(false, true);
                }
                return s_DefaultShaderInfoTexARGB8;
            }
        }
        #endregion

        static internal bool vertexTexturingIsAvailable
        {
            get
            {
                if (!s_VertexTexturingIsAvailable.HasValue)
                {
                    // Remove this workaround once case 1148851 has been fixed. In the editor, subshaders aren't stripped
                    // according to the graphic device capabilities unless the shader is precompiled. Querying tags will
                    // always return the tags from the first subshader. The editor shader is precompiled and doesn't
                    // suffer this issue, so we can use it as a reference.
                    var stockDefaultShader = getEditorShader();
                    var stockDefaultMaterial = new Material(stockDefaultShader);
                    string tagValue = stockDefaultMaterial.GetTag(k_VertexTexturingIsAvailableTag, false);
                    UIRUtility.Destroy(stockDefaultMaterial);
                    s_VertexTexturingIsAvailable = (tagValue == k_VertexTexturingIsAvailableTrue);
                }

                return s_VertexTexturingIsAvailable.Value;
            }
        }

        void CompleteCreation()
        {
            if (m_DrawRanges.IsCreated)
                return;

            m_DrawRanges = new NativeArray<DrawBufferRange>(m_LazyCreationDrawRangeRingSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Fences = m_MockDevice ? null : new uint[(int)k_MaxQueuedFrameCount];

            if (!m_MockDevice)
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
                if (m_DrawRanges.IsCreated && !m_MockDevice)
                    UIR.Utility.EngineUpdate -= OnEngineUpdate;

                if (m_DefaultMaterial != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(m_DefaultMaterial);
                    else
                        Object.DestroyImmediate(m_DefaultMaterial);
                }
                DeviceToFree free = new DeviceToFree()
                { handle = m_MockDevice ? 0 : Utility.InsertCPUFence(), page = m_FirstPage, drawRanges = m_DrawRanges };
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
            s_AllocateSampler.Begin();

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

                // A huge mesh, push it to a page of its own. Put this page at the end so it won't be queried often
                Page lastPage = m_FirstPage;
                while (lastPage != null && lastPage.next != null)
                    lastPage = lastPage.next;

                Page dedicatedPage = new Page((uint)vertexCount, (uint)indexCount, k_MaxQueuedFrameCount, m_MockDevice);
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

            s_AllocateSampler.End();
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

        static void Set1PixelSizeOnMaterial(DrawParams drawParams, Material mat)
        {
            Vector4 _1PixelClipInvView = new Vector4();

            // Size of 1 pixel in clip space.
            RectInt viewport = Utility.GetActiveViewport();
            _1PixelClipInvView.x = 2.0f / viewport.width;
            _1PixelClipInvView.y = 2.0f / viewport.height;

            // Pixel density in group space.
            Matrix4x4 matVPInv = (drawParams.projection * drawParams.view.Peek().transform).inverse;
            Vector3 v = matVPInv.MultiplyVector(new Vector3(_1PixelClipInvView.x, _1PixelClipInvView.y));
            _1PixelClipInvView.z = 1 / (Mathf.Abs(v.x) + Mathf.Epsilon);
            _1PixelClipInvView.w = 1 / (Mathf.Abs(v.y) + Mathf.Epsilon);

            mat.SetVector(s_1PixelClipInvViewPropID, _1PixelClipInvView);
        }

        void BeforeDraw()
        {
            if (!m_FrameIndexIncremented)
                AdvanceFrame();
            m_FrameIndexIncremented = false;
            m_DrawStats = new DrawStatistics();
            m_DrawStats.currentFrameIndex = (int)m_FrameIndex;
            m_DrawStats.currentDrawRangeStart = m_DrawRangeStart;

            s_BeforeDrawSampler.Begin();

            // Send changes
            Page page = m_FirstPage;
            while (page != null)
            {
                page.vertices.SendUpdates();
                page.indices.SendUpdates();
                page = page.next;
            }
            s_BeforeDrawSampler.End();
        }

        void EvaluateChain(RenderChainCommand head, Rect viewport, Matrix4x4 projection, Texture atlas, Texture gradientSettings, Texture shaderInfo,
            float pixelsPerPoint, NativeArray<Transform3x4> transforms, NativeArray<Vector4> clipRects, ref Exception immediateException)
        {
            var usesStraightYCoordinateSystem = m_APIUsesStraightYCoordinateSystem;
            if (Utility.GetInvertProjectionMatrix())
                usesStraightYCoordinateSystem = !usesStraightYCoordinateSystem;

            var drawParams = m_DrawParams;
            drawParams.Reset(viewport, projection);

            Material standardMaterial = null;
            if (!m_MockDevice)
            {
                standardMaterial = GetStandardMaterial();
                standardMaterial.mainTexture = atlas;
                standardMaterial.SetTexture(s_GradientSettingsTexID, gradientSettings);
                standardMaterial.SetTexture(s_ShaderInfoTexID, shaderInfo);
                if (transforms.Length > 0)
                    UIR.Utility.SetVectorArray<Transform3x4>(standardMaterial, s_TransformsPropID, transforms);
                if (clipRects.Length > 0)
                    UIR.Utility.SetVectorArray<Vector4>(standardMaterial, s_ClipRectsPropID, clipRects);
                Set1PixelSizeOnMaterial(drawParams, standardMaterial);
                standardMaterial.SetVector(s_PixelClipRectPropID, drawParams.view.Peek().clipRect);
                GL.modelview = drawParams.view.Peek().transform;
                GL.LoadProjectionMatrix(drawParams.projection);
            }

            NativeArray<DrawBufferRange> ranges = m_DrawRanges;
            int rangesCount = ranges.Length;
            int rangesCountMinus1 = ranges.Length - 1;
            int rangesStart = m_DrawRangeStart;
            int rangesReady = 0;
            DrawBufferRange curDrawRange = new DrawBufferRange();
            Page curPage = null;
            State curState = new State() { material = m_DefaultMaterial };
            int curDrawIndex = -1;
            int maxVertexReferenced = 0;

            while (head != null)
            {
                m_DrawStats.commandCount++;
                m_DrawStats.drawCommandCount += (head.type == CommandType.Draw ? 1u : 0u);

                bool kickRanges = (head.type != CommandType.Draw);
                bool stashRange = true;
                bool materialChanges = false;
                if (!kickRanges)
                {
                    materialChanges = (head.state.material != curState.material);
                    curState.material = head.state.material;
                    if (head.state.custom != null)
                    {
                        materialChanges |= head.state.custom != curState.custom;
                        curState.custom = head.state.custom;
                    }

                    if (head.state.font != null)
                    {
                        materialChanges |= head.state.font != curState.font;
                        curState.font = head.state.font;
                    }

                    kickRanges = materialChanges || (head.mesh.allocPage != curPage);
                    if (!kickRanges) // For debugging just disable those lines to get a draw call per draw command
                        stashRange = curDrawIndex != (head.mesh.allocIndices.start + head.indexOffset); // Should we stash at least?
                }

                if (stashRange)
                {
                    // Stash the current draw range
                    if (curDrawRange.indexCount > 0)
                    {
                        int wrapAroundIndex = (rangesStart + rangesReady++) & rangesCountMinus1;
                        ranges[wrapAroundIndex] = curDrawRange; // Close the active range

                        // TODO: This check only works since ranges are serialized and will break once the ranges are
                        //       truly processed in a multi-threaded fashion without copies. When this happens, a new
                        //       mechanism will need to be implemented to handle the "ranges-buffer-full" condition. For
                        //       the time being, calling KickRanges will make the whole buffer available.
                        if (rangesReady == rangesCount)
                            KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, curPage);

                        curDrawRange = new DrawBufferRange();
                        m_DrawStats.drawRangeCount++;
                    }

                    if (head.type == CommandType.Draw)
                    {
                        curDrawRange.firstIndex = (int)head.mesh.allocIndices.start + head.indexOffset;
                        curDrawRange.indexCount = head.indexCount;
                        curDrawRange.vertsReferenced = (int)(head.mesh.allocVerts.start + head.mesh.allocVerts.size);
                        curDrawRange.minIndexVal = (int)head.mesh.allocVerts.start;
                        curDrawIndex = curDrawRange.firstIndex + head.indexCount;
                        maxVertexReferenced = curDrawRange.vertsReferenced + curDrawRange.minIndexVal;
                        m_DrawStats.totalIndices += (uint)head.indexCount;
                    }
                }
                else
                {
                    // We can chain
                    if (curDrawRange.indexCount == 0)
                        curDrawIndex = curDrawRange.firstIndex = (int)head.mesh.allocIndices.start + head.indexOffset; // A first draw after a stash
                    maxVertexReferenced = Math.Max(maxVertexReferenced, (int)(head.mesh.allocVerts.size + head.mesh.allocVerts.start));
                    curDrawRange.indexCount += head.indexCount;
                    curDrawRange.minIndexVal = Math.Min(curDrawRange.minIndexVal, (int)head.mesh.allocVerts.start);
                    curDrawRange.vertsReferenced = maxVertexReferenced - curDrawRange.minIndexVal;
                    curDrawIndex += head.indexCount;
                    m_DrawStats.totalIndices += (uint)head.indexCount;
                    head = head.next;
                    continue;
                }

                if (kickRanges)
                {
                    KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, curPage);

                    if (head.type != CommandType.Draw)
                    {
                        if (!m_MockDevice)
                            head.ExecuteNonDrawMesh(drawParams, usesStraightYCoordinateSystem, pixelsPerPoint, ref immediateException);
                        if (head.type == CommandType.Immediate)
                        {
                            curState.material = m_DefaultMaterial; // A value that is considered unique and not null to force material reset on next draw command
                            m_DrawStats.immediateDraws++;
                        }
                    }
                    else
                    {
                        curPage = head.mesh.allocPage;
                    }

                    if (materialChanges)
                    {
                        if (!m_MockDevice)
                        {
                            var mat = curState.material != null ? curState.material : standardMaterial;
                            if (mat != standardMaterial)
                            {
                                mat.mainTexture = atlas;
                                mat.SetTexture(s_GradientSettingsTexID, gradientSettings);
                                mat.SetTexture(s_ShaderInfoTexID, shaderInfo);
                                if (transforms.Length > 0)
                                    UIR.Utility.SetVectorArray<Transform3x4>(mat, s_TransformsPropID, transforms);
                                if (clipRects.Length > 0)
                                    UIR.Utility.SetVectorArray<Vector4>(mat, s_ClipRectsPropID, clipRects);
                                Set1PixelSizeOnMaterial(drawParams, mat);
                                mat.SetVector(s_PixelClipRectPropID, drawParams.view.Peek().clipRect);
                            }
                            else if (head.type == CommandType.PushView || head.type == CommandType.PopView)
                            {
                                Set1PixelSizeOnMaterial(drawParams, mat);
                                mat.SetVector(s_PixelClipRectPropID, drawParams.view.Peek().clipRect);
                            }

                            mat.SetTexture(s_CustomTexPropID, curState.custom);
                            mat.SetTexture(s_FontTexPropID, curState.font);
                            mat.SetPass(0); // No multipass support, should it be even considered?
                        }

                        m_DrawStats.materialSetCount++;
                    }
                    else if (head.type == CommandType.PushView || head.type == CommandType.PopView)
                    {
                        var mat = curState.material != null ? curState.material : standardMaterial;
                        if (!m_MockDevice)
                        {
                            Set1PixelSizeOnMaterial(drawParams, mat);
                            mat.SetVector(s_PixelClipRectPropID, drawParams.view.Peek().clipRect);
                            mat.SetPass(0);
                        }

                        m_DrawStats.materialSetCount++;
                    }
                }

                head = head.next;
            }

            // Kick any pending ranges, this usually occurs when the draw chain ends with a draw command.
            if (curDrawRange.indexCount > 0)
            {
                int wrapAroundIndex = (rangesStart + rangesReady++) & rangesCountMinus1;
                ranges[wrapAroundIndex] = curDrawRange;
            }
            if (rangesReady > 0)
                KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, curPage);

            m_DrawRangeStart = rangesStart;
        }

        void KickRanges(NativeArray<DrawBufferRange> ranges, ref int rangesReady, ref int rangesStart, int rangesCount, Page curPage)
        {
            if (rangesReady > 0)
            {
                if (rangesStart + rangesReady <= rangesCount)
                {
                    if (!m_MockDevice)
                        Utility.DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, new NativeSlice<DrawBufferRange>(ranges, rangesStart, rangesReady));
                    m_DrawStats.drawRangeCallCount++;
                }
                else
                {
                    // Less common situation, the numbers straddles the end of the ranges buffer
                    int firstRangeCount = ranges.Length - rangesStart;
                    int secondRangeCount = rangesReady - firstRangeCount;
                    if (!m_MockDevice)
                    {
                        Utility.DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, new NativeSlice<DrawBufferRange>(ranges, rangesStart, firstRangeCount));
                        Utility.DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, new NativeSlice<DrawBufferRange>(ranges, 0, secondRangeCount));
                    }

                    m_DrawStats.drawRangeCallCount += 2;
                }

                rangesStart = (rangesStart + rangesReady) & (rangesCount - 1);
                rangesReady = 0;
            }
        }

        // Called every frame to draw one entire UI window
        public void DrawChain(RenderChainCommand head, Rect viewport, Matrix4x4 projection, Texture atlas, Texture gradientSettings, Texture shaderInfo,
            float pixelsPerPoint, NativeArray<Transform3x4> transforms, NativeArray<Vector4> clipRects, ref Exception immediateException)
        {
            if (head == null)
                return;

            BeforeDraw();
            Utility.ProfileDrawChainBegin();

            EvaluateChain(head, viewport, projection, atlas, gradientSettings, shaderInfo, pixelsPerPoint, transforms, clipRects, ref immediateException);

            Utility.ProfileDrawChainEnd();

            if (m_Fences != null)
                m_Fences[(int)(m_FrameIndex % m_Fences.Length)] = Utility.InsertCPUFence();

        }

        public void AdvanceFrame()
        {
            s_AdvanceFrameSampler.Begin();

            m_FrameIndex++;
            m_FrameIndexIncremented = true;

            m_DrawStats.currentFrameIndex = (int)m_FrameIndex;

            if (m_Fences != null)
            {
                int fenceIndex = (int)(m_FrameIndex % m_Fences.Length);
                uint fence = m_Fences[fenceIndex];
                if (fence != 0 && !Utility.CPUFencePassed(fence))
                {
                    s_FenceSampler.Begin();
                    Utility.WaitForCPUFencePassed(fence);
                    s_FenceSampler.End();
                }
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

            s_AdvanceFrameSampler.End();
        }

        internal static void PrepareForGfxDeviceRecreate()
        {
            m_ActiveDeviceCount += 1; // Don't let the count reach 0 and unsubscribe from GfxDeviceRecreate
            if (s_WhiteTexel != null)
            {
                UIRUtility.Destroy(s_WhiteTexel);
                s_WhiteTexel = null;
            }
            if (s_DefaultShaderInfoTexFloat != null)
            {
                UIRUtility.Destroy(s_DefaultShaderInfoTexFloat);
                s_DefaultShaderInfoTexFloat = null;
            }
            if (s_DefaultShaderInfoTexARGB8 != null)
            {
                UIRUtility.Destroy(s_DefaultShaderInfoTexARGB8);
                s_DefaultShaderInfoTexARGB8 = null;
            }
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
            public int currentDrawRangeStart;
            public uint totalIndices;
            public uint commandCount;
            public uint drawCommandCount;
            public uint materialSetCount;
            public uint drawRangeCount;
            public uint drawRangeCallCount;
            public uint immediateDraws;
        }
        internal DrawStatistics GatherDrawStatistics() { return m_DrawStats; }


        #region Internals
        private void OnEngineUpdate()
        {
            AdvanceFrame();
        }

        private static void ProcessDeviceFreeQueue()
        {
            s_FreeSampler.Begin();

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
                if (s_WhiteTexel != null)
                {
                    UIRUtility.Destroy(s_WhiteTexel);
                    s_WhiteTexel = null;
                }
                if (s_DefaultShaderInfoTexFloat != null)
                {
                    UIRUtility.Destroy(s_DefaultShaderInfoTexFloat);
                    s_DefaultShaderInfoTexFloat = null;
                }
                if (s_DefaultShaderInfoTexARGB8 != null)
                {
                    UIRUtility.Destroy(s_DefaultShaderInfoTexARGB8);
                    s_DefaultShaderInfoTexARGB8 = null;
                }
                Utility.NotifyOfUIREvents(false);
                m_SubscribedToNotifications = false;
            }

            s_FreeSampler.End();
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
