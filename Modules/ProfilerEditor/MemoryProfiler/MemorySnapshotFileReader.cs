// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.IO;

namespace UnityEditorInternal.Profiling.Memory.Experimental
{
    // keep in sync with MemorySnapshot/FileFormat.h
    namespace FileFormat
    {
        public enum EntryType : UInt16
        {
            Metadata_Version,
            Metadata_RecordDate,
            Metadata_UserMetadata,
            Metadata_CaptureFlags,
            Metadata_VirtualMachineInformation,
            NativeTypes_Name,
            NativeTypes_NativeBaseTypeArrayIndex,
            NativeObjects_NativeTypeArrayIndex,
            NativeObjects_HideFlags,
            NativeObjects_Flags,
            NativeObjects_InstanceId,
            NativeObjects_Name,
            NativeObjects_NativeObjectAddress,
            NativeObjects_Size,
            NativeObjects_RootReferenceId,
            GCHandles_Target,
            Connections_From,
            Connections_To,
            ManagedHeapSections_StartAddress,
            ManagedHeapSections_Bytes,
            ManagedStacks_StartAddress,
            ManagedStacks_Bytes,
            TypeDescriptions_Flags,
            TypeDescriptions_Name,
            TypeDescriptions_Assembly,
            TypeDescriptions_FieldIndices,
            TypeDescriptions_StaticFieldBytes,
            TypeDescriptions_BaseOrElementTypeIndex,
            TypeDescriptions_Size,
            TypeDescriptions_TypeInfoAddress,
            TypeDescriptions_TypeIndex,
            FieldDescriptions_Offset,
            FieldDescriptions_TypeIndex,
            FieldDescriptions_Name,
            FieldDescriptions_IsStatic,
            NativeRootReferences_Id,
            NativeRootReferences_AreaName,
            NativeRootReferences_ObjectName,
            NativeRootReferences_AccumulatedSize,
            NativeAllocations_MemoryRegionIndex,
            NativeAllocations_RootReferenceId,
            NativeAllocations_AllocationSiteId,
            NativeAllocations_Address,
            NativeAllocations_Size,
            NativeAllocations_OverheadSize,
            NativeAllocations_PaddingSize,
            NativeMemoryRegions_Name,
            NativeMemoryRegions_ParentIndex,
            NativeMemoryRegions_AddressBase,
            NativeMemoryRegions_AddressSize,
            NativeMemoryRegions_FirstAllocationIndex,
            NativeMemoryRegions_NumAllocations,
            NativeMemoryLabels_Name,
            NativeAllocationSites_Id,
            NativeAllocationSites_MemoryLabelIndex,
            NativeAllocationSites_CallstackSymbols,
            NativeCallstackSymbol_Symbol,
            NativeCallstackSymbol_ReadableStackTrace
        }
    }

    [Serializable]
    public class MemorySnapshotFileReader : ISerializationCallbackReceiver
    {
        const UInt32 kMemorySnapshotHeadSignature = 0xAEABCDCD;
        const UInt32 kMemorySnapshotDirectorySignature = 0xCDCDAEAB;
        const UInt32 kMemorySnapshotTailSignature = 0xABCDCDAE;
        const UInt32 kMemorySnapshotChapterSectionVersion = 0x20170724;
        const UInt32 kMemorySnapshotBlockSectionVersion = 0x20170724;

        [SerializeField]
        string m_AbsoluteFilePath;

        [NonSerialized]
        Chapter[] m_Chapters;

        [NonSerialized]
        BinaryReader m_Reader;

        [NonSerialized]
        ulong m_ChapterOffset;

        [NonSerialized]
        Block[] m_Blocks;

        public MemorySnapshotFileReader(string filePath)
        {
            Open(filePath);
        }

        public MemorySnapshotFileReader()
        {
        }

        public void Dispose()
        {
            Close();
        }

        public string GetFilePath()
        {
            return m_AbsoluteFilePath;
        }

        public void Open(string filePath)
        {
            m_AbsoluteFilePath = filePath;

            FileStream stream = new FileStream(m_AbsoluteFilePath, FileMode.Open, FileAccess.Read);

            m_Reader = new BinaryReader(stream, System.Text.Encoding.UTF8);

            BuildChapters();
        }

        public void Close()
        {
            if (m_Reader != null)
            {
                m_Reader.Close();
                m_AbsoluteFilePath = null;
                m_Reader = null;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Open(m_AbsoluteFilePath);
        }

        public void GetDataArray<T>(FileFormat.EntryType entryType, uint entryIndex, uint numEntries, ref T[] dataOut, GetItem<T> getItemFunc)
        {
            Chapter chapter = GetChapter(entryType);

            if (entryIndex + numEntries > chapter.GetNumEntries())
            {
                throw new IOException("Invalid entry indices");
            }

            if (numEntries > dataOut.Length)
            {
                throw new IOException("Not enough space in dataOut array");
            }

            uint  blockIndex = chapter.GetBlockIndex();
            ulong startBlockOffset = chapter.GetBlockOffsetForEntryIndex(entryIndex);
            ulong endBlockOffset = chapter.GetBlockOffsetForEntryIndex(entryIndex + numEntries);
            uint  dataSize = (uint)(endBlockOffset - startBlockOffset);

            byte[] data = GetDataCache(dataSize);

            m_Blocks[blockIndex].GetData(startBlockOffset, dataSize, ref data, m_Reader);

            uint dataIndex = 0;
            for (uint i = 0; i < numEntries; i++)
            {
                uint entrySize = chapter.GetSizeForEntryIndex(entryIndex + i);
                dataOut[i] = getItemFunc(data, dataIndex, entrySize);
                dataIndex += entrySize;
            }
        }

        public T GetDataSingle<T>(FileFormat.EntryType entryType, GetItem<T> getItemFunc)
        {
            Chapter chapter = GetChapter(entryType);

            if (!(chapter is SingleValueChapter))
            {
                throw new IOException("Chapter is not a single value chapter");
            }

            uint  blockIndex = chapter.GetBlockIndex();
            ulong blockOffset = chapter.GetBlockOffsetForEntryIndex(0);
            uint  dataSize = chapter.GetSizeForEntryIndex(0);

            byte[] data = GetDataCache(dataSize);

            m_Blocks[blockIndex].GetData(blockOffset, dataSize, ref data, m_Reader);

            return getItemFunc(data, 0, dataSize);
        }

        public uint GetNumEntries(FileFormat.EntryType entryType)
        {
            if ((uint)entryType >= m_Chapters.Length)
            {
                throw new IOException("Invalid entry type");
            }

            Chapter chapter = m_Chapters[(uint)entryType];

            if (chapter == null)
            {
                return 0;
            }

            return chapter.GetNumEntries();
        }

        internal void BuildChapters()
        {
            m_Reader.BaseStream.Seek(0, SeekOrigin.Begin);

            if (m_Reader.ReadUInt32() != kMemorySnapshotHeadSignature)
            {
                throw new IOException(m_AbsoluteFilePath + " Corrupted head signature.");
            }

            m_Reader.BaseStream.Seek(-sizeof(UInt32), SeekOrigin.End);

            if (m_Reader.ReadUInt32() != kMemorySnapshotTailSignature)
            {
                throw new IOException(m_AbsoluteFilePath + " Corrupted tail signature.");
            }

            m_Reader.BaseStream.Seek(-sizeof(UInt32) - sizeof(UInt64), SeekOrigin.End);
            m_ChapterOffset = m_Reader.ReadUInt64();
            m_Reader.BaseStream.Seek((long)m_ChapterOffset, SeekOrigin.Begin);

            if (m_Reader.ReadUInt32() != kMemorySnapshotDirectorySignature)
            {
                throw new IOException(m_AbsoluteFilePath + " Corrupted directory signature.");
            }

            if (m_Reader.ReadUInt32() != kMemorySnapshotChapterSectionVersion)
            {
                throw new IOException("Unsupported Snapshot format");
            }

            ulong blockSectionPosition = m_Reader.ReadUInt64();

            uint    entryTypeCount = m_Reader.ReadUInt32();
            ulong[] chapterOffsets = new ulong[entryTypeCount];

            for (int i = 0; i < entryTypeCount; i++)
            {
                chapterOffsets[i] = m_Reader.ReadUInt64();
            }

            m_Chapters = new Chapter[entryTypeCount];

            for (int i = 0; i < entryTypeCount; i++)
            {
                long offset = (long)chapterOffsets[i];
                if (offset != 0)
                {
                    m_Reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    m_Chapters[i] = Chapter.CreateChapter(m_Reader);
                }
            }

            m_Reader.BaseStream.Seek((long)blockSectionPosition, SeekOrigin.Begin);

            if (m_Reader.ReadUInt32() > kMemorySnapshotBlockSectionVersion)
            {
                throw new IOException("Unsupported Snapshot Version");
            }

            uint numBlocks = m_Reader.ReadUInt32();
            ulong[] blockPositions = new ulong[numBlocks];
            for (int i = 0; i < numBlocks; i++)
            {
                blockPositions[i] = m_Reader.ReadUInt64();
            }

            m_Blocks = new Block[numBlocks];
            for (int i = 0; i < numBlocks; i++)
            {
                m_Reader.BaseStream.Seek((long)blockPositions[i], SeekOrigin.Begin);
                m_Blocks[i] = new Block(m_Reader);
            }
        }

        internal Chapter GetChapter(FileFormat.EntryType entryType)
        {
            if ((uint)entryType >= m_Chapters.Length)
            {
                throw new IOException("Invalid entry type");
            }

            Chapter chapter = m_Chapters[(uint)entryType];

            if (chapter == null)
            {
                throw new IOException("Attempted to read from empty chapter");
            }

            return chapter;
        }

        internal uint dataCapacity = 0;
        internal byte[] data = null;

        internal byte[] GetDataCache(UInt32 dataSize)
        {
            if (dataCapacity < dataSize)
            {
                dataCapacity = dataSize;
                data = new byte[dataSize];
            }

            return data;
        }
    }
}
