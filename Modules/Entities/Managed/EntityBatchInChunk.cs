// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    internal struct EntityBatchInChunk
    {
        public ChunkIndex Chunk;
        public int StartIndex;
        public int Count;
    }
}
