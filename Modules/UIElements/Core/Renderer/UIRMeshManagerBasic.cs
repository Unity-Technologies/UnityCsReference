// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    sealed class MeshManagerBasic : MeshManager
    {
        public MeshManagerBasic(uint initialVertexCapacity, uint initialIndexCapacity, uint extrasStride, GpuUpdaterType gpuUpdaterType)
            : base(initialVertexCapacity, initialIndexCapacity, extrasStride, gpuUpdaterType)
        {
        }

        public override void Update(MeshHandle mesh, uint vertexCount, out RawSlice vertexData)
        {
            Debug.Assert(mesh.allocVerts.size >= vertexCount);

            vertexData = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, (int)vertexCount);

            if (mesh.allocTime != m_FrameIndex)
            {
                mesh.allocPage.MarkVertexRangeDirty(mesh.allocVerts.start, vertexCount);
            }
        }

        public override void Update(MeshHandle mesh, uint vertexCount, uint indexCount, out RawSlice vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset)
        {
            Debug.Assert(mesh.allocVerts.size >= vertexCount);
            Debug.Assert(mesh.allocIndices.size >= indexCount);

            int indexOfFirstVertex = (int)mesh.allocVerts.start;
            vertexData = mesh.allocPage.vertices.cpuData.Slice((int)mesh.allocVerts.start, (int)vertexCount);
            indexData = mesh.allocPage.indices.cpuData.SliceAs<UInt16>((int)mesh.allocIndices.start, (int)indexCount);
            indexOffset = (ushort)indexOfFirstVertex;

            if (mesh.allocTime != m_FrameIndex)
            {
                mesh.allocPage.MarkVertexRangeDirty(mesh.allocVerts.start, vertexCount);
                mesh.allocPage.indices.AddDirtyRange(mesh.allocIndices.start, indexCount);
            }
        }

        public override void Free(MeshHandle mesh)
        {
            mesh.allocPage.vertices.allocator.Free(mesh.allocVerts);
            mesh.allocPage.indices.allocator.Free(mesh.allocIndices);

            base.Free(mesh);
        }

#region Dispose Pattern

        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                Page page = m_FirstPage;
                while (page != null)
                {
                    Page pageToDispose = page;
                    page = page.next;
                    pageToDispose.Dispose();
                }
            }

            base.Dispose(disposing);
        }

#endregion // Dispose Pattern

    }
}
