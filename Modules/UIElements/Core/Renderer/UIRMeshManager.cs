// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Profiling;
using static UnityEngine.UIElements.UIR.UIRenderDevice;

namespace UnityEngine.UIElements.UIR
{
    abstract class MeshManager : IDisposable
    {
        protected readonly LinkedPool<MeshHandle> m_MeshHandles = new LinkedPool<MeshHandle>(() => new MeshHandle(), mh => { });

        public abstract void Update(MeshHandle mesh, uint vertexCount, out NativeSlice<Vertex> vertexData);
        public abstract void Update(MeshHandle mesh, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset);
        public virtual void Free(MeshHandle mesh)
        {
            mesh.allocVerts = new Alloc();
            mesh.allocIndices = new Alloc();
            mesh.allocPage = null;
            mesh.updateAllocID = 0;
            m_MeshHandles.Return(mesh);
        }

        public virtual void AdvanceFrame()
        {
            ++m_FrameIndex;
            PruneUnusedPages();
            m_VertexUpdater.AdvanceFrame();
            m_IndexUpdater.AdvanceFrame();
        }

        public void OnFrameRenderingBegin()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(MeshManager));

            // Update vertex buffers
            {
                Page page = m_FirstPage;
                while (page != null)
                {
                    m_VertexUpdater.ProcessDataSet(page.vertices);
                    page = page.next;
                }
                m_VertexUpdater.CompleteUpdate();
            }

            // Update index buffers
            {
                Page page = m_FirstPage;
                while (page != null)
                {
                    m_IndexUpdater.ProcessDataSet(page.indices);
                    page = page.next;
                }
                m_IndexUpdater.CompleteUpdate();
            }
        }

        protected uint m_NextPageVertexCount;
        protected readonly uint m_LargeMeshVertexCount;
        protected readonly float m_IndexToVertexCountRatio;
        protected readonly bool m_PagesGpuDataIsMapped;

        protected uint m_FrameIndex;
        protected Page m_FirstPage;

        protected GpuUpdater<Vertex> m_VertexUpdater;
        protected GpuUpdater<UInt16> m_IndexUpdater;

        static ProfilerMarker s_MarkerAllocate = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.Allocate");

        protected MeshManager(uint initialVertexCapacity, uint initialIndexCapacity, GpuUpdaterType gpuUpdaterType)
        {
            switch (gpuUpdaterType)
            {
                case GpuUpdaterType.Mapped:
                    m_PagesGpuDataIsMapped = true;
                    m_VertexUpdater = new GpuUpdaterMapped<Vertex>();
                    m_IndexUpdater = new GpuUpdaterMapped<ushort>();
                    break;
                case GpuUpdaterType.StagedGpuOnly:
                    m_PagesGpuDataIsMapped = false;
                    m_VertexUpdater = new GpuUpdaterStaged<Vertex>(Utility.GPUBufferType.Vertex, StagingMode.GpuOnly);
                    m_IndexUpdater = new GpuUpdaterStaged<ushort>(Utility.GPUBufferType.Index, StagingMode.GpuOnly);
                    break;
                case GpuUpdaterType.StagedCpuGpu:
                    m_PagesGpuDataIsMapped = false;
                    m_VertexUpdater = new GpuUpdaterStaged<Vertex>(Utility.GPUBufferType.Vertex, StagingMode.CpuGpu);
                    m_IndexUpdater = new GpuUpdaterStaged<ushort>(Utility.GPUBufferType.Index, StagingMode.CpuGpu);
                    break;
                default:
                    throw new NotImplementedException();
            }

            m_NextPageVertexCount = Math.Max(initialVertexCapacity / 2, 2048); // No less than 4k vertices (doubled from 2k effectively when the first page is allocated)
            m_LargeMeshVertexCount = m_NextPageVertexCount;
            m_IndexToVertexCountRatio = (float)initialIndexCapacity / (float)initialVertexCapacity;
            m_IndexToVertexCountRatio = Mathf.Max(m_IndexToVertexCountRatio, 2);
        }

        public MeshHandle Allocate(uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset)
        {
            MeshHandle meshHandle = m_MeshHandles.Get();
            Allocate(meshHandle, vertexCount, indexCount, out vertexData, out indexData, false);
            indexOffset = (UInt16)meshHandle.allocVerts.start;
            return meshHandle;
        }

        protected void Allocate(MeshHandle meshHandle, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, bool shortLived)
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

                if (ia.size == 0)
                {
                    m_NextPageVertexCount <<= 1; // Double the vertex count
                    m_NextPageVertexCount = Math.Max(m_NextPageVertexCount, vertexCount * 2);
                    m_NextPageVertexCount = Math.Min(m_NextPageVertexCount, UIRenderDevice.maxVerticesPerPage);  // Stay below 64k for 16-bit indices
                    uint newPageIndexCount = (uint)(m_NextPageVertexCount * m_IndexToVertexCountRatio + 0.5f);
                    newPageIndexCount = Math.Max(newPageIndexCount, (uint)(indexCount * 2));
                    Debug.Assert(page?.next == null); // page MUST be the last page in the list, but can be null
                    page = new Page(m_NextPageVertexCount, newPageIndexCount, m_PagesGpuDataIsMapped);
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
                    // The page itself is not going to be usable and will be freed after 60 frames.
                    // This is done because because the page is required when creating the native slice
                    var pageVertexCount = (vertexCount > UIRenderDevice.maxVerticesPerPage) ? 2 : vertexCount;
                    Debug.Assert(vertexCount <= UIRenderDevice.maxVerticesPerPage, "Requested Vertex count is above the limit. Alloc will fail.");

                    // A huge mesh, push it to a page of its own. Put this page at the end so it won't be queried often
                    page = new Page((uint)pageVertexCount, (uint)indexCount, m_PagesGpuDataIsMapped);
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
                    page.indices.allocator.Free(ia);

                ia = new Alloc();
                va = new Alloc();
            }

            if (va.size > 0)
                page.vertices.AddDirtyRange(va.start, va.size);
            if (ia.size > 0)
                page.indices.AddDirtyRange(ia.start, ia.size);

            vertexData = new NativeSlice<Vertex>(page.vertices.cpuData, (int)va.start, (int)va.size);
            indexData = new NativeSlice<UInt16>(page.indices.cpuData, (int)ia.start, (int)ia.size);

            meshHandle.allocPage = page;
            meshHandle.allocVerts = va;
            meshHandle.allocIndices = ia;
            meshHandle.allocTime = m_FrameIndex;

            s_MarkerAllocate.End();
        }

        protected bool TryAllocFromPage(Page page, uint vertexCount, uint indexCount, ref Alloc va, ref Alloc ia, bool shortLived)
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

                if (current.framesEmpty < UIRenderDevice.k_PruneEmptyPageFrameCount)
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

        public virtual AllocationStatistics GatherAllocationStatistics()
        {
            AllocationStatistics stats = new AllocationStatistics();
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

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_VertexUpdater?.Dispose();
                m_VertexUpdater = null;

                m_IndexUpdater?.Dispose();
                m_IndexUpdater = null;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion
    }
}
