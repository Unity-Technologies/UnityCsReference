// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;

namespace UnityEditor.Search
{
    struct SearchDocumentListTableHeader
    {
        public int version;
        public int count;
        public int symbolSlots;
        public int allocatedBlocks;
        public int usedBlocks;
        public int totalAllocatedBytes;

        public const int size = sizeof(int) * 6;
        public const int blockSize = sizeof(int);

        public SearchDocumentListTableHeader(int version, int count, int symbolSlots, int allocatedBlocks, int usedBlocks, int totalAllocatedBytes)
        {
            this.version = version;
            this.count = count;
            this.symbolSlots = symbolSlots;
            this.allocatedBlocks = allocatedBlocks;
            this.usedBlocks = usedBlocks;
            this.totalAllocatedBytes = totalAllocatedBytes;
        }

        public void ToBinary(BinaryWriter bw)
        {
            bw.Write(version);
            bw.Write(count);
            bw.Write(symbolSlots);
            bw.Write(allocatedBlocks);
            bw.Write(usedBlocks);
            bw.Write(totalAllocatedBytes);
        }

        public static SearchDocumentListTableHeader FromBinary(BinaryReader br)
        {
            var version = br.ReadInt32();
            var count = br.ReadInt32();
            var symbolSlots = br.ReadInt32();
            var allocatedBlocks = br.ReadInt32();
            var usedBlocks = br.ReadInt32();
            var totalAllocatedBytes = br.ReadInt32();

            return new SearchDocumentListTableHeader(version, count, symbolSlots, allocatedBlocks, usedBlocks, totalAllocatedBytes);
        }
    }

    class SearchDocumentListTable : IDisposable
    {
        public const int tableFullSymbol = -1;
        public const int emptyTableSymbol = 0;

        // Versions:
        // 1: First version
        // 2: Added allocated vs length for content length and hash struct.
        internal const int defaultVersion = 0x02;

        internal const int defaultContentCount = 30;
        internal const int defaultAverageContentSize = 16;
        internal const int hashFactor = 2; // Number of slots for symbols with same hash

        struct ContentLengthAndHash
        {
            public const int size = 3;

            public int length;
            public int allocated;
            public int hash;

            public int totalContentSize => size + allocated;

            public ContentLengthAndHash(int length, int allocated, int hash)
            {
                this.length = length;
                this.allocated = allocated;
                this.hash = hash;
            }

            public static ContentLengthAndHash Read(ReadOnlySpan<int> buffer, int index)
            {
                return new ContentLengthAndHash(buffer[index], buffer[index + 1], buffer[index + 2]);
            }

            public void Write(Span<int> buffer, int index)
            {
                buffer[index] = length;
                buffer[index + 1] = allocated;
                buffer[index + 2] = hash;
            }
        }

        SearchDocumentListTableHeader m_Header;
        SearchNativeReadOnlyArray<int> m_Buffer;

        public bool autoGrow { get; }

        public int version => m_Header.version;

        public int count => m_Header.count;

        public int symbolSlots => m_Header.symbolSlots;

        public int allocatedContentBlocks => m_Header.allocatedBlocks;

        public int usedContentBlocks => m_Header.usedBlocks;

        public int totalAllocatedBytes => m_Header.totalAllocatedBytes;

        public SearchDocumentListTable(int contentCount, int averageContentLength = defaultAverageContentSize, bool autoGrow = true, bool doCreate = true)
        {
            this.autoGrow = autoGrow;
            if (doCreate)
                Create(contentCount, averageContentLength);
            else
                m_Header = new SearchDocumentListTableHeader(defaultVersion, 0, 0, 0, 0, 0);
        }

        public SearchDocumentListTable(bool autoGrow = true, bool doCreate = true)
            : this(defaultContentCount, defaultAverageContentSize, autoGrow, doCreate)
        { }

        ~SearchDocumentListTable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
                m_Header = new SearchDocumentListTableHeader(defaultVersion, 0, 0, 0, 0, 0);
            m_Buffer.Dispose();
        }

        public void Create(int contentCount, int averageContentLength)
        {
            m_Header = new SearchDocumentListTableHeader(defaultVersion, 0, contentCount * hashFactor, 0, 0, 0);

            if (contentCount == 0)
                return;

            var blocksPerContent = GetContentBlockSize(averageContentLength);
            m_Header.allocatedBlocks = contentCount * blocksPerContent;
            m_Header.usedBlocks = ContentLengthAndHash.size; // Add the empty array

            var size = GetNextBufferSize(GetSymbolsBlockSize(m_Header.symbolSlots) + m_Header.allocatedBlocks);
            m_Buffer = new SearchNativeReadOnlyArray<int>(size, Allocator.Persistent);
            m_Header.totalAllocatedBytes = size * SearchDocumentListTableHeader.blockSize;
        }

        public int ToSymbol(IReadOnlyCollection<int> sortedDocumentList)
        {
            if (sortedDocumentList == null || sortedDocumentList.Count == 0)
                return emptyTableSymbol;

            var lengthAndHash = Hash(sortedDocumentList, sortedDocumentList.Count);

            var symbolIndex = 0;
            var symbol = 0;

            if (m_Header.symbolSlots > 0)
            {
                symbolIndex = lengthAndHash.hash % m_Header.symbolSlots;
                symbol = GetSymbol(symbolIndex);
                while (symbol != 0)
                {
                    var fetchedContent = GetContent(symbol);
                    if (CompareContent(sortedDocumentList, fetchedContent))
                        return symbol;
                    symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                    symbol = GetSymbol(symbolIndex);
                }
            }

            if (m_Header.count + 1 >= m_Header.symbolSlots || (m_Header.symbolSlots / (float)m_Header.count < hashFactor))
            {
                if (autoGrow)
                {
                    // Double the content count
                    Grow(m_Header.symbolSlots / hashFactor * 2);

                    // Get a new symbol index
                    symbolIndex = lengthAndHash.hash % m_Header.symbolSlots;
                    symbol = GetSymbol(symbolIndex);
                    while (symbol != 0)
                    {
                        symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                        symbol = GetSymbol(symbolIndex);
                    }
                }
                else
                    return tableFullSymbol;
            }

            if (m_Header.usedBlocks + ContentLengthAndHash.size + lengthAndHash.length > m_Header.allocatedBlocks)
            {
                if (autoGrow)
                {
                    var newAllocatedBlockSize = m_Header.usedBlocks + ContentLengthAndHash.size + lengthAndHash.length;
                    ExpandAllocatedBlockSpace(newAllocatedBlockSize);
                }
                else
                    return tableFullSymbol;
            }

            symbol = m_Header.usedBlocks;
            WriteContentAtSymbol(symbolIndex, symbol, sortedDocumentList, lengthAndHash);
            return symbol;
        }

        public int Contains(IReadOnlyCollection<int> sortedDocumentList)
        {
            if (sortedDocumentList == null || sortedDocumentList.Count == 0)
                return emptyTableSymbol;

            if (m_Header.count == 0)
                return tableFullSymbol;

            var lengthAndHash = Hash(sortedDocumentList, sortedDocumentList.Count);

            var symbolIndex = lengthAndHash.hash % m_Header.symbolSlots;
            var symbol = GetSymbol(symbolIndex);
            while (symbol != 0)
            {
                var fetchedContent = GetContent(symbol);
                if (CompareContent(sortedDocumentList, fetchedContent))
                    return symbol;
                symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                symbol = GetSymbol(symbolIndex);
            }

            return tableFullSymbol;
        }

        public ReadOnlySpan<int> GetContent(int symbol)
        {
            if (symbol == emptyTableSymbol)
                return Array.Empty<int>();
            if (symbol == tableFullSymbol)
                return null;

            if (symbol >= m_Header.usedBlocks)
                return null;
            var byteOffset = GetContentOffset() + symbol;
            if (byteOffset > m_Buffer.Count - ContentLengthAndHash.size)
                return null;

            var lengthAndHash = ReadContentLengthAndHash(m_Buffer.AsSpan(), symbol);

            if (lengthAndHash.length == 0)
                return Array.Empty<int>();
            if (lengthAndHash.length < 0)
                return null;
            if (byteOffset + ContentLengthAndHash.size + lengthAndHash.length > m_Buffer.Count)
                return null;

            return m_Buffer.AsReadOnlySpan().Slice(byteOffset + ContentLengthAndHash.size, lengthAndHash.length);
        }

        // This method grows the size of the table, including the number of content that can be stored.
        public void Grow(int newContentCount)
        {
            var currentCount = m_Header.count;
            if (newContentCount < currentCount)
                return;

            // Compute new sizes
            var averageBlocksPerContent = GetAverageBlocksPerContent();
            var newSymbolSlots = Math.Max(newContentCount * hashFactor, m_Header.symbolSlots);
            var newSymbolsBlockSize = GetSymbolsBlockSize(newSymbolSlots);
            var newAllocatedBlocks = (int)(newContentCount * (averageBlocksPerContent + ContentLengthAndHash.size));
            newAllocatedBlocks = Math.Max(newAllocatedBlocks, m_Header.allocatedBlocks);

            if (newSymbolSlots == m_Header.symbolSlots)
                return;

            var oldContentOffset = GetContentOffset();
            var oldAllocatedBlocks = m_Header.allocatedBlocks;
            m_Header.symbolSlots = newSymbolSlots;
            m_Header.allocatedBlocks = newAllocatedBlocks;
            var newContentOffset = GetContentOffset();

            // Get new total size
            var newBufferSize = GetNextBufferSize(newSymbolsBlockSize + newAllocatedBlocks);

            // Resize buffer
            var oldBuffer = m_Buffer;
            var newBuffer = m_Buffer;
            var doDispose = false;
            if (newBufferSize > m_Buffer.Count)
            {
                newBuffer = new SearchNativeReadOnlyArray<int>(newBufferSize, Allocator.Persistent);
                doDispose = true;
            }

            // Move content
            NativeArray<int>.Copy(oldBuffer.BackingArray, oldContentOffset, newBuffer.BackingArray, newContentOffset, oldAllocatedBlocks);

            // Rebuild symbol table
            var bufferIndex = GetSymbolsOffset();
            var span = newBuffer.AsSpan().Slice(bufferIndex, newSymbolsBlockSize);
            span.Fill(0);

            // Start byteOffset at first content by skipping symbol 0
            var byteOffset = ContentLengthAndHash.size;
            for (var i = 0; i < m_Header.count; ++i)
            {
                var currentLengthAndHash = ReadContentLengthAndHash(newBuffer.AsSpan(), byteOffset);
                if (currentLengthAndHash.length == 0)
                {
                    byteOffset += ContentLengthAndHash.size;
                    continue;
                }

                var symbolIndex = (currentLengthAndHash.hash % m_Header.symbolSlots);
                var symbol = GetSymbol(symbolIndex);
                while (symbol != 0)
                {
                    symbolIndex = (symbolIndex + 1) % m_Header.symbolSlots;
                    symbol = GetSymbol(symbolIndex);
                }

                symbol = byteOffset;
                WriteSymbol(symbolIndex, symbol);

                byteOffset += ContentLengthAndHash.size + currentLengthAndHash.length;
            }

            if (doDispose)
                m_Buffer.Dispose();
            m_Buffer = newBuffer;
            m_Header.totalAllocatedBytes = m_Buffer.Count * SearchDocumentListTableHeader.blockSize;
        }

        public void ToBinary(BinaryWriter bw)
        {
            var realAllocatedBlocks = GetSymbolsBlockSize(symbolSlots) + allocatedContentBlocks;
            var realAllocatedBytes = realAllocatedBlocks * SearchDocumentListTableHeader.blockSize;
            var newHeader = new SearchDocumentListTableHeader(version, count, symbolSlots, allocatedContentBlocks, usedContentBlocks, realAllocatedBytes);
            newHeader.ToBinary(bw);
            {
                for (var i = 0; i < realAllocatedBlocks; ++i)
                    bw.Write(m_Buffer[i]);
            }
        }

        public static SearchDocumentListTable FromBinary(BinaryReader br)
        {
            var docTable = new SearchDocumentListTable(doCreate: false);
            docTable.m_Header = SearchDocumentListTableHeader.FromBinary(br);
            if (docTable.m_Header.totalAllocatedBytes > 0)
            {
                var size = docTable.m_Header.totalAllocatedBytes / SearchDocumentListTableHeader.blockSize;
                docTable.m_Buffer = new SearchNativeReadOnlyArray<int>(size, Allocator.Persistent);
                for (var i = 0; i < docTable.m_Buffer.Count; ++i)
                    docTable.m_Buffer[i] = br.ReadInt32();
            }
            return docTable;
        }

        public void RemapDocuments(Dictionary<int, int> updatedDocIndexes, bool keepSorted = false)
        {
            var totalUsedSize = m_Header.usedBlocks + GetSymbolsBlockSize(m_Header.symbolSlots);
            var index = GetContentOffset() + ContentLengthAndHash.size; // Skip empty array.
            while (index < totalUsedSize)
            {
                var currentLengthAndHash = ContentLengthAndHash.Read(m_Buffer.AsSpan(), index);
                if (currentLengthAndHash.length == 0)
                {
                    index += currentLengthAndHash.totalContentSize;
                    continue;
                }

                var docs = m_Buffer.AsSpan().Slice(index + ContentLengthAndHash.size, currentLengthAndHash.length);
                for (var i = 0; i < docs.Length; ++i)
                {
                    if (updatedDocIndexes.TryGetValue(docs[i], out var newIndex))
                        docs[i] = newIndex;
                }

                if (keepSorted)
                    QuickSort(docs, 0, docs.Length - 1);

                currentLengthAndHash = Hash(docs, currentLengthAndHash.allocated);
                currentLengthAndHash.Write(m_Buffer.AsSpan(), index);

                index += currentLengthAndHash.totalContentSize;
            }
        }

        public void RemoveDocuments(HashSet<int> removedDocuments)
        {
            var totalUsedSize = m_Header.usedBlocks + GetSymbolsBlockSize(m_Header.symbolSlots);
            var index = GetContentOffset() + ContentLengthAndHash.size; // Skip empty array.
            while (index < totalUsedSize)
            {
                var currentLengthAndHash = ContentLengthAndHash.Read(m_Buffer.AsSpan(), index);
                if (currentLengthAndHash.length == 0)
                {
                    index += currentLengthAndHash.totalContentSize;
                    continue;
                }

                var realAllocatedLength = currentLengthAndHash.allocated;
                var originalLength = currentLengthAndHash.length;
                var newLength = originalLength;
                var docs = m_Buffer.AsSpan().Slice(index + ContentLengthAndHash.size, currentLengthAndHash.length);
                for (var i = 0; i < docs.Length; ++i)
                {
                    if (removedDocuments.Contains(docs[i]))
                    {
                        docs[i] = SearchIndexer.invalidDocumentIndex;
                        --newLength;
                    }
                }

                if (newLength == originalLength)
                {
                    index += currentLengthAndHash.totalContentSize;
                    continue;
                };

                Compress(docs);
                docs = docs.Slice(0, newLength);
                currentLengthAndHash = Hash(docs, realAllocatedLength);
                currentLengthAndHash.Write(m_Buffer.AsSpan(), index);

                index += currentLengthAndHash.totalContentSize;
            }
        }

        long GetAverageBlocksPerContent()
        {
            if (m_Header.count == 0)
                return 0;
            // Remove the number of content lengths and hashes (+1 for the empty array) from used bytes to get average content size
            return (long)Math.Ceiling((m_Header.usedBlocks - (m_Header.count + 1) * (ContentLengthAndHash.size)) / (double)m_Header.count);
        }

        void ExpandAllocatedBlockSpace(int newAllocatedBlockSize)
        {
            if (newAllocatedBlockSize <= m_Header.allocatedBlocks)
                return;

            m_Header.allocatedBlocks = newAllocatedBlockSize;
            var newBufferSize = GetNextBufferSize(GetSymbolsBlockSize(m_Header.symbolSlots) + newAllocatedBlockSize);
            if (newBufferSize <= m_Buffer.Count)
                return;

            var newBuffer = new SearchNativeReadOnlyArray<int>(newBufferSize, Allocator.Persistent);
            NativeArray<int>.Copy(m_Buffer.BackingArray, newBuffer.BackingArray, m_Buffer.Count);
            m_Buffer.Dispose();
            m_Buffer = newBuffer;
            m_Header.totalAllocatedBytes = m_Buffer.Count * SearchDocumentListTableHeader.blockSize;
        }

        int GetSymbol(int symbolIndex)
        {
            var bufferIndex = GetSymbolsOffset() + symbolIndex;
            return m_Buffer[bufferIndex];
        }

        void WriteSymbol(int symbolIndex, int symbol)
        {
            var bufferIndex = GetSymbolsOffset() + symbolIndex;
            m_Buffer[bufferIndex] = symbol;
        }

        void WriteContentAtSymbol(int symbolIndex, int symbol, IReadOnlyCollection<int> documentList, ContentLengthAndHash lengthAndHash)
        {
            WriteSymbol(symbolIndex, symbol);
            var bufferIndex = GetContentOffset() + symbol;

            WriteContentLengthAndHash(m_Buffer.AsSpan(), symbol, lengthAndHash);
            bufferIndex += ContentLengthAndHash.size;

            foreach (var doc in documentList)
            {
                m_Buffer[bufferIndex] = doc;
                ++bufferIndex;
            }

            m_Header.count++;
            m_Header.usedBlocks += GetContentBlockSize(documentList.Count);
        }

        static int GetContentBlockSize(int contentLength)
        {
            var maxByteCount = contentLength;
            return maxByteCount + ContentLengthAndHash.size;
        }

        static int GetSymbolsBlockSize(int symbolCount)
        {
            return symbolCount;
        }

        static int GetSymbolsOffset()
        {
            return 0; // Header is not written inside buffer, so symbols start at 0
        }

        int GetContentOffset()
        {
            return GetSymbolsOffset() + m_Header.symbolSlots;
        }

        ContentLengthAndHash ReadContentLengthAndHash(Span<int> buffer, int symbol)
        {
            var index = GetContentOffset() + symbol;
            return ContentLengthAndHash.Read(buffer, index);
        }

        void WriteContentLengthAndHash(Span<int> buffer, int symbol, ContentLengthAndHash lengthAndHash)
        {
            var index = GetContentOffset() + symbol;
            lengthAndHash.Write(buffer, index);
        }

        static int GetNextBufferSize(int size)
        {
            return UnityEngine.Mathf.NextPowerOfTwo(size);
        }

        static ContentLengthAndHash Hash(IReadOnlyCollection<int> documentList, int realAllocatedLength)
        {
            var hash = new HashCode();
            foreach (var document in documentList)
            {
                hash.Add(document);
            }
            return new ContentLengthAndHash(documentList.Count, realAllocatedLength, UnityEngine.Mathf.Abs(hash.ToHashCode()));
        }

        static ContentLengthAndHash Hash(ReadOnlySpan<int> documentList, int realAllocatedLength)
        {
            var hash = new HashCode();
            foreach (var document in documentList)
            {
                hash.Add(document);
            }
            return new ContentLengthAndHash(documentList.Length, realAllocatedLength, UnityEngine.Mathf.Abs(hash.ToHashCode()));
        }

        // This methods assumes both document lists are sorted
        static bool CompareContent(IReadOnlyCollection<int> inputDocumentList, in ReadOnlySpan<int> existingDocumentList)
        {
            if (inputDocumentList.Count != existingDocumentList.Length)
                return false;

            var index = 0;
            foreach (var doc in inputDocumentList)
            {
                if (doc != existingDocumentList[index])
                    return false;
                ++index;
            }
            return true;
        }

        static void QuickSort(Span<int> input, int start, int end)
        {
            if (start < end)
            {
                int pivot = Partition(input, start, end);
                QuickSort(input, start, pivot - 1);
                QuickSort(input, pivot + 1, end);
            }
        }

        static int Partition(Span<int> input, int start, int end)
        {
            int pivot = input[end];
            int pIndex = start;

            for (int i = start; i < end; i++)
            {
                if (input[i] <= pivot)
                {
                    (input[i], input[pIndex]) = (input[pIndex], input[i]);
                    pIndex++;
                }
            }

            (input[pIndex], input[end]) = (input[end], input[pIndex]);
            return pIndex;
        }

        static void Compress(Span<int> docs)
        {
            int read = 0;
            int write = 0;

            while (read < docs.Length)
            {
                if (docs[read] != SearchIndexer.invalidDocumentIndex)
                {
                    if (read != write)
                    {
                        docs[write] = docs[read];
                    }

                    ++write;
                }

                ++read;
            }
        }
    }
}
