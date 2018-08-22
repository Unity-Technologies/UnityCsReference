// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.IO;

namespace UnityEditorInternal.Profiling.Memory.Experimental
{
    internal enum ChapterFormatType : ushort
    {
        Undefined = 0,
        SingleValue,
        ConstantSizeArray,
        DynamicSizeArray,
        Count
    };

    abstract class Chapter
    {
        static internal Chapter CreateChapter(BinaryReader reader)
        {
            ChapterFormatType format = (ChapterFormatType)reader.ReadUInt16();
            switch (format)
            {
                case ChapterFormatType.SingleValue:
                    return new SingleValueChapter(reader);
                case ChapterFormatType.ConstantSizeArray:
                    return new ConstantSizeArrayChapter(reader);
                case ChapterFormatType.DynamicSizeArray:
                    return new DynamicSizeArrayChapter(reader);
                default:
                    throw new IOException("Invalid chapter format");
            }
        }

        internal Chapter(BinaryReader reader)
        {
            BuildChapter(reader);
        }

        internal uint GetBlockIndex()
        {
            return m_BlockIndex;
        }

        protected uint m_BlockIndex;

        internal abstract void BuildChapter(BinaryReader reader);
        internal abstract uint GetNumEntries();
        internal abstract uint GetSizeForEntryIndex(uint entryIndex);
        internal abstract ulong GetBlockOffsetForEntryIndex(uint entryIndex);
    }

    class SingleValueChapter : Chapter
    {
        uint m_EntrySize;
        ulong m_BlockOffset;

        internal SingleValueChapter(BinaryReader reader)
            : base(reader)
        {}

        internal override void BuildChapter(BinaryReader reader)
        {
            m_BlockIndex = reader.ReadUInt32();
            m_EntrySize = reader.ReadUInt32();
            m_BlockOffset = reader.ReadUInt64();
        }

        internal override uint GetNumEntries()
        {
            return 1;
        }

        internal override uint GetSizeForEntryIndex(uint entryIndex)
        {
            Debug.Assert(entryIndex == 0);

            return m_EntrySize;
        }

        internal override ulong GetBlockOffsetForEntryIndex(uint entryIndex)
        {
            Debug.Assert(entryIndex == 0 || entryIndex == 1);

            return (entryIndex == 0 ? m_BlockOffset : m_BlockOffset + m_EntrySize);
        }
    }

    class ConstantSizeArrayChapter : Chapter
    {
        uint m_NumEntries;
        uint m_EntrySize;

        internal ConstantSizeArrayChapter(BinaryReader reader)
            : base(reader)
        {}

        internal override void BuildChapter(BinaryReader reader)
        {
            m_BlockIndex = reader.ReadUInt32();
            m_EntrySize = reader.ReadUInt32();
            m_NumEntries = reader.ReadUInt32();
        }

        internal override uint GetNumEntries()
        {
            return m_NumEntries;
        }

        internal override uint GetSizeForEntryIndex(uint entryIndex)
        {
            return m_EntrySize;
        }

        internal override ulong GetBlockOffsetForEntryIndex(uint entryIndex)
        {
            Debug.Assert(entryIndex <= m_NumEntries);

            return m_EntrySize * entryIndex;
        }
    }

    class DynamicSizeArrayChapter : Chapter
    {
        uint m_NumEntries;
        ulong[] m_BlockOffsets;

        internal DynamicSizeArrayChapter(BinaryReader reader)
            : base(reader)
        {}

        internal override void BuildChapter(BinaryReader reader)
        {
            m_BlockIndex = reader.ReadUInt32();
            m_NumEntries = reader.ReadUInt32();
            // last entry is the total size of the data, allowing
            // compute of data size of last entry
            m_BlockOffsets = new ulong[m_NumEntries + 1];
            for (int i = 0; i < m_NumEntries + 1; i++)
            {
                m_BlockOffsets[i] = reader.ReadUInt64();
            }
        }

        internal override uint GetNumEntries()
        {
            return m_NumEntries;
        }

        internal override uint GetSizeForEntryIndex(uint entryIndex)
        {
            Debug.Assert(entryIndex < m_NumEntries);
            return (uint)(m_BlockOffsets[entryIndex + 1] - m_BlockOffsets[entryIndex]);
        }

        internal override ulong GetBlockOffsetForEntryIndex(uint entryIndex)
        {
            Debug.Assert(entryIndex <= m_NumEntries);

            return m_BlockOffsets[entryIndex];
        }
    }
}
