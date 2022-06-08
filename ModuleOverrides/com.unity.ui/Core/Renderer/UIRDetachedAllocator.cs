// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    internal class DetachedAllocator : IDisposable
    {
        private TempAllocator<Vertex> m_VertsPool;
        private TempAllocator<UInt16> m_IndexPool;
        private List<MeshWriteData> m_MeshWriteDataPool;
        private int m_MeshWriteDataCount;

        public List<MeshWriteData> meshes => m_MeshWriteDataPool.GetRange(0, m_MeshWriteDataCount);

        public DetachedAllocator()
        {
            m_MeshWriteDataPool = new List<MeshWriteData>(16);
            m_MeshWriteDataCount = 0;
            m_VertsPool = new TempAllocator<Vertex>(8192, 2048, 64 * 1024);
            m_IndexPool = new TempAllocator<UInt16>(8192 << 1, 2048 << 1, (64 * 1024) << 1);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool m_Disposed;
        protected void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (disposing)
            {
                m_VertsPool.Dispose();
                m_IndexPool.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            m_Disposed = true;
        }

        public MeshWriteData Alloc(int vertexCount, int indexCount)
        {
            MeshWriteData mwd = null;
            if (m_MeshWriteDataCount < m_MeshWriteDataPool.Count)
                mwd = m_MeshWriteDataPool[m_MeshWriteDataCount];
            else
            {
                mwd = new MeshWriteData();
                m_MeshWriteDataPool.Add(mwd);
            }
            ++m_MeshWriteDataCount;

            if (vertexCount == 0 || indexCount == 0)
            {
                mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<UInt16>());
                return mwd;
            }

            mwd.Reset(m_VertsPool.Alloc(vertexCount), m_IndexPool.Alloc(indexCount));
            return mwd;
        }

        public void Clear()
        {
            m_VertsPool.Reset();
            m_IndexPool.Reset();
            m_MeshWriteDataCount = 0;

            // Don't clear m_MeshWriteDataPool to allow reuse.
        }
    }
}
