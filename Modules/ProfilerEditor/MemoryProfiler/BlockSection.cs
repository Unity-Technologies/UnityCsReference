// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace UnityEditorInternal.Profiling.Memory.Experimental
{
    public delegate T GetItem<T>(byte[] data, uint startIndex, uint numBytes);

    class Block
    {
        ulong m_ChunkSize;
        ulong m_TotalBytes;
        ulong[] m_ChunkOffsets;

        internal Block(BinaryReader reader)
        {
            BuildBlock(reader);
        }

        internal void BuildBlock(BinaryReader reader)
        {
            m_ChunkSize = reader.ReadUInt64();
            m_TotalBytes = reader.ReadUInt64();

            uint numChunks = ((uint)(m_TotalBytes / m_ChunkSize));
            if (m_TotalBytes % m_ChunkSize != 0) numChunks++;

            m_ChunkOffsets = new ulong[numChunks];
            for (int i = 0; i < numChunks; i++)
            {
                m_ChunkOffsets[i] = reader.ReadUInt64();
            }
        }

        // We are waiting on a .NET runtime upgrade to implement the following optimization:
        // Temp copy-less transfer of data directly from file directly to the T[] array, for constant size array entries.
        // For Constant-size array entries, we can assume that the array entries are blittable --
        // which means that given an UnmanagedMemoryStream pointing to the input ref T[] array,
        // we can copy directly the bytes we need from the underlying FileStream of the BinaryReader,
        // effectively bypassing the need for getItemFunc and the temp byte array.
        // However, Stream.CopyTo(Stream, int) is not available as of .NET runtime 3.5, making our only
        // option the copy into the temp byte array.
        internal void GetData(ulong startBlockOffset, uint blockLength, ref byte[] dataOut, BinaryReader reader)
        {
            Debug.Assert(startBlockOffset + blockLength <= m_TotalBytes);

            ulong curOffset = 0;
            while (curOffset < blockLength)
            {
                ulong blockOffset = startBlockOffset + curOffset;

                uint chunkIndex = (uint)(blockOffset / m_ChunkSize);
                uint chunkLocalOffset = (uint)(blockOffset % m_ChunkSize);

                ulong chunkSize = Math.Min(m_ChunkSize, m_TotalBytes - m_ChunkSize * chunkIndex);

                ulong readSize = Math.Min(chunkSize - chunkLocalOffset, blockLength - curOffset);
                if (readSize == 0)
                {
                    throw new Exception("Corrupted File Format");
                }
                ulong chunkAddress = m_ChunkOffsets[chunkIndex];

                reader.BaseStream.Seek((long)(chunkAddress + chunkLocalOffset), SeekOrigin.Begin);
                reader.Read(dataOut, (int)curOffset, (int)readSize);

                curOffset += readSize;
            }
        }
    }
}
