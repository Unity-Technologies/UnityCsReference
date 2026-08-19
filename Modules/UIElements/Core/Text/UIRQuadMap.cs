// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements
{
    // Maps text-mesh quads (meshIndex, textElementInfoIndex) to their UIR vertices; the
    // slice list is not 1:1 with generator meshes (empty meshes, multi-atlas, page splits).
    // Backed by a single grown-once buffer so pooled reuse does not allocate.
    internal class UIRQuadMap
    {
        struct Entry
        {
            public int sliceIndex;
            public int vertexOffset;
        }

        readonly List<int> m_MeshStarts = new();
        Entry[] m_Entries = Array.Empty<Entry>();
        int m_Length;

        public void Clear()
        {
            m_MeshStarts.Clear();
            m_Length = 0;
        }

        public void BeginMesh(int quadCount)
        {
            m_MeshStarts.Add(m_Length);
            int end = m_Length + quadCount;
            if (m_Entries.Length < end)
                Array.Resize(ref m_Entries, Math.Max(end, m_Entries.Length * 2));
            for (int i = m_Length; i < end; i++)
                m_Entries[i].sliceIndex = -1;
            m_Length = end;
        }

        public void Record(int textElementInfoIndex, int sliceIndex, int vertexOffset)
        {
            m_Entries[m_MeshStarts[^1] + textElementInfoIndex] = new Entry { sliceIndex = sliceIndex, vertexOffset = vertexOffset };
        }

        public bool TryGetQuad(int meshIndex, int textElementInfoIndex, List<NativeSlice<Vertex>> vertices, out NativeSlice<Vertex> quad)
        {
            quad = default;
            if ((uint)meshIndex >= (uint)m_MeshStarts.Count || textElementInfoIndex < 0)
                return false;

            int start = m_MeshStarts[meshIndex];
            int end = meshIndex + 1 < m_MeshStarts.Count ? m_MeshStarts[meshIndex + 1] : m_Length;
            if (start + textElementInfoIndex >= end)
                return false;

            var entry = m_Entries[start + textElementInfoIndex];
            if (entry.sliceIndex < 0)
                return false;

            quad = vertices[entry.sliceIndex].Slice(entry.vertexOffset, 4);
            return true;
        }
    }
}
