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
        private TempAllocator m_TempAllocator;
        private List<MeshWriteData> m_MeshWriteDataPool;
        private List<int> m_FillGradientMeshIndices;
        private List<FillGradient> m_FillGradients;
        private int m_FillGradientDataCount;
        private List<int> m_FillTextureMeshIndices;
        private List<Texture> m_FillTextures;
        private int m_FillTextureDataCount;
        private int m_MeshWriteDataCount;

        public List<MeshWriteData> meshes => m_MeshWriteDataPool.GetRange(0, m_MeshWriteDataCount);

        public DetachedAllocator()
        {
            m_MeshWriteDataPool = new List<MeshWriteData>(16);
            m_FillGradientMeshIndices = new List<int>(16);
            m_FillGradients = new List<FillGradient>(16);
            m_FillTextureMeshIndices = new List<int>(16);
            m_FillTextures = new List<Texture>(16);
            m_MeshWriteDataCount = 0;
            m_TempAllocator = new TempAllocator(512 * 1024, 512 * 1024, 4 * 1024 * 1024);
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
                m_TempAllocator.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            m_Disposed = true;
        }

        // Make sure to add the Gradient after the MeshWriteData has been added (After calling Alloc()).
        public void AddGradient(FillGradient gradient)
        {
            if (m_FillGradientDataCount >= m_FillGradients.Count)
            {
                m_FillGradients.Add(gradient);
                m_FillGradientMeshIndices.Add(m_MeshWriteDataCount-1);
            }
            else
            {
                m_FillGradients[m_FillGradientDataCount] = gradient;
                m_FillGradientMeshIndices[m_FillGradientDataCount] = m_MeshWriteDataCount-1;
            }
            m_FillGradientDataCount++;
        }

        public FillGradient GetGradientFromMeshIndex(int index)
        {
            for (int i = 0; i < m_FillGradientDataCount; ++i)
            {
                if (m_FillGradientMeshIndices[i] == index)
                {
                    return m_FillGradients[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index), "No gradient found for the specified index.");
        }

        public FillGradient GetGradientAtIndex(int index)
        {
            return m_FillGradients[index];
        }

        public bool HasGradientsOrTextures()
        {
            return m_FillGradientDataCount > 0 || m_FillTextureDataCount > 0;
        }

        public bool HasGradientAtMeshIndex(int index)
        {
            for (int i = 0; i < m_FillGradientDataCount; ++i)
            {
                if (m_FillGradientMeshIndices[i] == index)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddTexture(Texture fillTexture)
        {
            if (m_FillTextureDataCount >= m_FillTextures.Count)
            {
                m_FillTextures.Add(fillTexture);
                m_FillTextureMeshIndices.Add(m_MeshWriteDataCount - 1);
            }
            else
            {
                m_FillTextures[m_FillTextureDataCount] = fillTexture;
                m_FillTextureMeshIndices[m_FillTextureDataCount] = m_MeshWriteDataCount - 1;
            }
            m_FillTextureDataCount++;
        }

        public Texture GetTextureFromMeshIndex(int index)
        {
            for (int i = 0; i < m_FillTextureDataCount; ++i)
            {
                if (m_FillTextureMeshIndices[i] == index)
                {
                    return m_FillTextures[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index), "No texture found for the specified index.");
        }

        public bool HasTextureAtMeshIndex(int index)
        {
            for (int i = 0; i < m_FillTextureDataCount; ++i)
            {
                if (m_FillTextureMeshIndices[i] == index)
                {
                    return true;
                }
            }
            return false;
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

            mwd.Reset(m_TempAllocator.Alloc<Vertex>(vertexCount), m_TempAllocator.Alloc<ushort>(indexCount));
            return mwd;
        }

        public void Clear()
        {
            m_TempAllocator.Reset();
            m_MeshWriteDataCount = 0;
            m_FillGradientDataCount = 0;
            m_FillTextureDataCount = 0;

            // Don't clear m_MeshWriteDataPool, m_FillGradients, m_FillGradientMeshIdices, m_FillTextures and
            // m_FillTextureIndices to allow reuse
        }
    }
}
