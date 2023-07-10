// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements;

class ChunkAllocatingArray<T>
{
    const int k_ChunkSize = 1024 * 2;

    readonly List<T[]> m_Chunks;

    public ChunkAllocatingArray()
    {
        m_Chunks = new List<T[]>
        {
            new T[k_ChunkSize]
        };
    }

    public T this[int index]
    {
        get
        {
            var chunkIndex = index / k_ChunkSize;
            var indexInChunk = index % k_ChunkSize;

            if (chunkIndex >= m_Chunks.Count)
                throw new IndexOutOfRangeException();

            return m_Chunks[chunkIndex][indexInChunk];
        }
        set
        {
            var chunkIndex = index / k_ChunkSize;
            var indexInChunk = index % k_ChunkSize;

            while (chunkIndex >= m_Chunks.Count)
                m_Chunks.Add(new T[k_ChunkSize]);

            m_Chunks[chunkIndex][indexInChunk] = value;
        }
    }
}
