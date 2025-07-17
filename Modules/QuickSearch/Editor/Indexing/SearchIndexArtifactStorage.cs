// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

// Note: Endiannes. We don't deal with endian issues in this storage. The reason is that only Unity Editors can connect to cache servers
// and push artifacts, so we can assume that only Windows, Mac, and Linux editors will be used to create artifacts. These are all little-endian systems.

namespace UnityEditor.Search
{
    // Keep in sync with SearchIndexArtifactStorageHeader.h
    struct SearchIndexArtifactStorageHeader
    {
        public const int DefaultMagicNumber = 0x53494153; // "SIAS" in ASCII

        public int MagicNumber;
        public int Version;

        public long Timestamp;
        public ulong FileSize;

        // If the files are never going to be big, maybe we could get away by using uint instead of ulong.

        public ulong DocumentsOffset;
        public ulong DocumentsSize;

        public ulong DocumentSourcesOffset;
        public ulong DocumentSourcesSize;

        public ulong DocumentSourceTypeStructuralHashesOffset;
        public ulong DocumentSourceTypeStructuralHashesSize;

        public ulong WordsOffset;
        public ulong WordsSize;

        public ulong PropertyDoublesOffset;
        public ulong PropertyDoublesSize;

        public ulong PropertyStringsOffset;
        public ulong PropertyStringsSize;

        public ulong KeywordsOffset;
        public ulong KeywordsSize;

        public ulong KeywordsRemoveOffset;
        public ulong KeywordsRemoveSize;

        public ulong MetaInfoOffset;
        public ulong MetaInfoSize;

        public ulong StringTableOffset;
        public ulong StringTableSize;

        public void WriteToBinary(BinaryWriter bw, long position)
        {
            // Write everything as ordered in SearchIndexArtifactStorageHeader.h, i.e. smaller fields first.
            bw.BaseStream.Seek(position, SeekOrigin.Begin);
            bw.Write(MagicNumber);
            bw.Write(Version);
            bw.Write(Timestamp);
            bw.Write(FileSize);
            bw.Write(DocumentsOffset);
            bw.Write(DocumentsSize);
            bw.Write(DocumentSourcesOffset);
            bw.Write(DocumentSourcesSize);
            bw.Write(DocumentSourceTypeStructuralHashesOffset);
            bw.Write(DocumentSourceTypeStructuralHashesSize);
            bw.Write(WordsOffset);
            bw.Write(WordsSize);
            bw.Write(PropertyDoublesOffset);
            bw.Write(PropertyDoublesSize);
            bw.Write(PropertyStringsOffset);
            bw.Write(PropertyStringsSize);
            bw.Write(KeywordsOffset);
            bw.Write(KeywordsSize);
            bw.Write(KeywordsRemoveOffset);
            bw.Write(KeywordsRemoveSize);
            bw.Write(MetaInfoOffset);
            bw.Write(MetaInfoSize);
            bw.Write(StringTableOffset);
            bw.Write(StringTableSize);
        }

        public static SearchIndexArtifactStorageHeader ReadFromBinary(BinaryReader br, long position)
        {
            SearchIndexArtifactStorageHeader header = new SearchIndexArtifactStorageHeader();

            // Read everything as ordered in SearchIndexArtifactStorageHeader.h, i.e. smaller fields first.
            br.BaseStream.Seek(position, SeekOrigin.Begin);
            header.MagicNumber = br.ReadInt32();
            header.Version = br.ReadInt32();
            header.Timestamp = br.ReadInt64();
            header.FileSize = br.ReadUInt64();
            header.DocumentsOffset = br.ReadUInt64();
            header.DocumentsSize = br.ReadUInt64();
            header.DocumentSourcesOffset = br.ReadUInt64();
            header.DocumentSourcesSize = br.ReadUInt64();
            header.DocumentSourceTypeStructuralHashesOffset = br.ReadUInt64();
            header.DocumentSourceTypeStructuralHashesSize = br.ReadUInt64();
            header.WordsOffset = br.ReadUInt64();
            header.WordsSize = br.ReadUInt64();
            header.PropertyDoublesOffset = br.ReadUInt64();
            header.PropertyDoublesSize = br.ReadUInt64();
            header.PropertyStringsOffset = br.ReadUInt64();
            header.PropertyStringsSize = br.ReadUInt64();
            header.KeywordsOffset = br.ReadUInt64();
            header.KeywordsSize = br.ReadUInt64();
            header.KeywordsRemoveOffset = br.ReadUInt64();
            header.KeywordsRemoveSize = br.ReadUInt64();
            header.MetaInfoOffset = br.ReadUInt64();
            header.MetaInfoSize = br.ReadUInt64();
            header.StringTableOffset = br.ReadUInt64();
            header.StringTableSize = br.ReadUInt64();

            return header;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SearchIndexArtifactWordEntry
    {
        public const int ByteSize = sizeof(int) * 3; // WordId, DocumentIndex, Score

        public int WordId;
        public int DocumentIndex;
        public int Score;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct SearchIndexArtifactPropertyDoubleEntry
    {
        public const int ByteSize = sizeof(int) * 3 + sizeof(double); // NameId, DocumentIndex, Score, Value

        public int NameId;
        public int DocumentIndex;
        public int Score;
        public double Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SearchIndexArtifactPropertyStringEntry
    {
        public const int ByteSize = sizeof(int) * 4; // NameId, DocumentIndex, Score, Value

        public int NameId;
        public int DocumentIndex;
        public int Score;
        public int Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SearchIndexArtifactKeywordEntry
    {
        public const int ByteSize = sizeof(int) * 2; // PropertyNameId, HelpId

        public int PropertyNameId;
        public int HelpId;
    }

    enum SearchIndexArtifactKeywordRemoveType : byte
    {
        Empty = 0, // Remove empty keyword
        NestedKeys = 1, // This is used to remove nested keys from the keywords.
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SearchIndexArtifactKeywordRemoveEntry
    {
        public const int ByteSize = sizeof(byte) + sizeof(int); // RemoveType + PropertyNameId

        public SearchIndexArtifactKeywordRemoveType RemoveType;
        public int PropertyNameId;
    }

    // This storage is optimized for artifacts that are not meant to be modified or searched after creation.
    // Many of the functionalities are not implemented or implemented in a way that is not optimal for performance.
    class SearchIndexArtifactStorage : ISearchIndexerStorage, IDisposable
    {
        const int k_InvalidStringId = -1;
        const int k_MinimumDocumentSize = 4 * sizeof(int) + sizeof(SearchDocumentFlags); // 4 ints for lengths of documentId, name, source, and the documentIndex, plus flags
        readonly int k_SizeOfHash128 = UnsafeUtility.SizeOf<Hash128>();
        int m_NextDocumentIndex = SearchIndexer.invalidDocumentIndex;
        int m_NextStringId = k_InvalidStringId;

        SearchNativeList<byte> m_DocumentBytes = new(1024, Allocator.Persistent);
        SearchNativeList<byte> m_DocumentSourceHashes = new(256, Allocator.Persistent);
        SearchNativeList<byte> m_DocumentSourceTypeStructuralHashes = new(256, Allocator.Persistent);
        SearchNativeList<SearchIndexArtifactWordEntry> m_WordEntries = new(512, Allocator.Persistent);
        SearchNativeList<SearchIndexArtifactPropertyDoubleEntry> m_PropertyDoubleEntries = new(512, Allocator.Persistent);
        SearchNativeList<SearchIndexArtifactPropertyStringEntry> m_PropertyStringEntries = new(512, Allocator.Persistent);
        SearchNativeList<SearchIndexArtifactKeywordEntry> m_KeywordEntries = new(512, Allocator.Persistent);
        SearchNativeList<SearchIndexArtifactKeywordRemoveEntry> m_KeywordRemoveEntries = new(512, Allocator.Persistent);
        SearchNativeList<byte> m_MetaInfoBytes = new(128, Allocator.Persistent);

        SearchNativeList<byte> m_StringTable = new(1024, Allocator.Persistent);
        Dictionary<string, int> m_StringTableAccelerator = new(StringComparer.Ordinal);

        public const int DefaultVersion = 0x01;

        public int Version { get; set; }
        public long Timestamp { get; set; }

        public int KeywordCount
        {
            get
            {
                // This method does not need to be optimal, this will not be called outside of testing.
                var count = 0;
                foreach (var _ in GetKeywords())
                {
                    ++count;
                }
                return count;
            }
        }

        public int DocumentCount
        {
            get
            {
                // This method does not need to be optimal, this will not be called outside of testing.
                var docs = GetDocuments(true) as List<SearchDocument>;
                return docs?.Count ?? 0;
            }
        }

        public int EntryCount
        {
            get
            {
                // This method does not need to be optimal, this will not be called outside of testing.
                var totalEntries = m_WordEntries.Count;
                totalEntries += m_PropertyDoubleEntries.Count;
                return totalEntries;
            }
        }

        public Func<string, string> ResolveDocumentHandler { get; set; }
        public Func<string, SearchIndexOperator, SearchResultCollection, IEnumerable<SearchResult>> ExtraSearchWordHandler { get; set; }

        public bool IsReady()
        {
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            m_DocumentBytes.Dispose();
            m_DocumentSourceHashes.Dispose();
            m_DocumentSourceTypeStructuralHashes.Dispose();
            m_WordEntries.Dispose();
            m_PropertyDoubleEntries.Dispose();
            m_PropertyStringEntries.Dispose();
            m_KeywordEntries.Dispose();
            m_KeywordRemoveEntries.Dispose();
            m_MetaInfoBytes.Dispose();
            m_StringTable.Dispose();
            m_StringTableAccelerator.Clear();
        }

        public IEnumerable<string> GetKeywords()
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            var keywordIdsToRemove = new Dictionary<int, SearchIndexArtifactKeywordRemoveType>();
            for (var i = 0; i < m_KeywordRemoveEntries.Count; ++i)
            {
                var entry = m_KeywordRemoveEntries[i];
                if (entry.RemoveType == SearchIndexArtifactKeywordRemoveType.Empty)
                {
                    keywordIdsToRemove.TryAdd(entry.PropertyNameId, SearchIndexArtifactKeywordRemoveType.Empty);
                }
                else if (entry.RemoveType == SearchIndexArtifactKeywordRemoveType.NestedKeys)
                {
                    var propertyName = GetStringFromStringTable(entry.PropertyNameId);
                    if (propertyName != null)
                    {
                        int keywordId;
                        // Remove all nested keywords
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".x", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".y", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".z", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".w", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".r", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".g", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".b", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                        if (m_StringTableAccelerator.TryGetValue(propertyName + ".a", out keywordId))
                            keywordIdsToRemove.TryAdd(keywordId, SearchIndexArtifactKeywordRemoveType.NestedKeys);
                    }
                }
            }

            for (var i = 0; i < m_KeywordEntries.Count; ++i)
            {
                var entry = m_KeywordEntries[i];
                if (keywordIdsToRemove.TryGetValue(entry.PropertyNameId, out var removeType))
                {
                    if (removeType == SearchIndexArtifactKeywordRemoveType.Empty && entry.HelpId == k_InvalidStringId)
                        continue; // Skip empty keywords
                    if (removeType == SearchIndexArtifactKeywordRemoveType.NestedKeys)
                        continue; // Always remove nested keys, empty or not.
                }
                var propertyName = GetStringFromStringTable(entry.PropertyNameId);
                if (string.IsNullOrEmpty(propertyName))
                    continue;
                var help = GetStringFromStringTable(entry.HelpId);
                if (string.IsNullOrEmpty(help))
                    yield return propertyName + ":";
                else
                    yield return propertyName + ":" + help;
            }
        }

        public void SetMetaInfo(string documentId, string metadata)
        {
            var documentIdSymbol = AddStringToStringTable(documentId);

            var bytesNeededMetaData = metadata != null ? System.Text.Encoding.UTF8.GetByteCount(metadata) : 0;
            var totalBytesNeeded = bytesNeededMetaData + sizeof(int) * 2;

            m_MetaInfoBytes.GrowIfNeeded(m_MetaInfoBytes.Count + totalBytesNeeded);

            var offset = m_MetaInfoBytes.Count;
            var span = m_MetaInfoBytes.AsSpanFullCapacity().Slice(offset, totalBytesNeeded);
            BinaryWriteToSpan(documentIdSymbol, span.Slice(0, sizeof(int)));
            BinaryWriteToSpan(bytesNeededMetaData, span.Slice(sizeof(int), sizeof(int)));
            if (bytesNeededMetaData > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(metadata, span.Slice(sizeof(int) * 2, bytesNeededMetaData));
            }

            m_MetaInfoBytes.Count += totalBytesNeeded;
        }

        public string GetMetaInfo(string documentId)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            var documentIdSymbol = AddStringToStringTable(documentId);

            var totalByteSize = m_MetaInfoBytes.Count;
            var span = m_MetaInfoBytes.AsReadOnlySpan();
            var offset = 0;

            while (offset < totalByteSize)
            {
                // Read the documentIdSymbol
                var currentDocumentIdSymbol = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
                offset += sizeof(int);

                // Read the length of the metadata
                var metadataLength = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
                offset += sizeof(int);

                // If the documentIdSymbol matches, read the metadata
                if (currentDocumentIdSymbol == documentIdSymbol)
                {
                    if (metadataLength > 0)
                    {
                        var metadataContent = System.Text.Encoding.UTF8.GetString(span.Slice(offset, metadataLength));
                        return metadataContent;
                    }
                    return string.Empty; // No metadata found for this documentId
                }
                offset += metadataLength;
            }

            return null; // DocumentId not found
        }

        public IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            var documents = new List<SearchDocument>();

            var span = m_DocumentBytes.AsReadOnlySpan();
            int offset = 0;

            while (offset < span.Length)
            {
                int documentIndex = 0;
                int bytesRead = 0;
                var document = GetDocumentFromBytes(span.Slice(offset), ref documentIndex, ref bytesRead);
                offset += bytesRead;
                if (document.valid || !ignoreNulls)
                {
                    documents.Add(document);
                }
            }

            return documents;
        }

        public IEnumerable<int> GetDocumentIndexes()
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            var documentIndexes = new List<int>();

            var span = m_DocumentBytes.AsReadOnlySpan();
            int offset = 0;

            while (offset < span.Length)
            {
                int documentIndex = 0;
                int bytesRead = 0;
                _ = GetDocumentFromBytes(span.Slice(offset), ref documentIndex, ref bytesRead);
                offset += bytesRead;
                documentIndexes.Add(documentIndex);
            }

            return documentIndexes;
        }

        public SearchDocument GetDocument(int index)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            var span = m_DocumentBytes.AsReadOnlySpan();
            int offset = 0;

            while (offset < span.Length)
            {
                int documentIndex = 0;
                int bytesRead = 0;
                var document = GetDocumentFromBytes(span.Slice(offset), ref documentIndex, ref bytesRead);
                offset += bytesRead;
                if (index == documentIndex)
                {
                    return document;
                }
            }

            return SearchDocument.invalid;
        }

        public int FindDocumentIndex(string documentId)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            var span = m_DocumentBytes.AsReadOnlySpan();
            int offset = 0;

            while (offset < span.Length)
            {
                int documentIndex = 0;
                int bytesRead = 0;
                var document = GetDocumentFromBytes(span.Slice(offset), ref documentIndex, ref bytesRead);
                offset += bytesRead;
                if (documentId == document.id)
                {
                    return documentIndex;
                }
            }

            return SearchIndexer.invalidDocumentIndex;
        }

        public int AddDocument(string documentId, string name, string source, bool _, SearchDocumentFlags flags)
        {
            // We do not care about checking if the document already exists in an artifact storage. When creating an artifact, we assume that the document is unique.
            var documentIndex = ++m_NextDocumentIndex;

            // Convert the document to bytes and store it in the backing array.
            var bytesNeededDocumentId = documentId != null ? System.Text.Encoding.UTF8.GetByteCount(documentId) : 0;
            var bytesNeededName = name != null ? System.Text.Encoding.UTF8.GetByteCount(name) : 0;
            var bytesNeededSource = source != null ? System.Text.Encoding.UTF8.GetByteCount(source) : 0;
            var totalBytesNeeded = bytesNeededDocumentId + bytesNeededName + bytesNeededSource + k_MinimumDocumentSize;

            m_DocumentBytes.GrowIfNeeded(m_DocumentBytes.Count + totalBytesNeeded);

            var offset = 0;
            var span = m_DocumentBytes.AsSpanFullCapacity().Slice(m_DocumentBytes.Count, totalBytesNeeded);
            BinaryWriteToSpan(documentIndex, span.Slice(0, sizeof(int)));
            offset += sizeof(int);
            BinaryWriteToSpan(bytesNeededDocumentId, span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
            if (bytesNeededDocumentId > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(documentId, span.Slice(offset, bytesNeededDocumentId));
                offset += bytesNeededDocumentId;
            }

            BinaryWriteToSpan(bytesNeededName, span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
            if (bytesNeededName > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(name, span.Slice(offset, bytesNeededName));
                offset += bytesNeededName;
            }

            BinaryWriteToSpan(bytesNeededSource, span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
            if (bytesNeededSource > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(source, span.Slice(offset, bytesNeededSource));
                offset += bytesNeededSource;
            }

            BinaryWriteToSpan((int)flags, span.Slice(offset, sizeof(SearchDocumentFlags)));

            m_DocumentBytes.Count += totalBytesNeeded;

            return documentIndex;
        }

        public void AddSourceDocument(string sourceDocumentPath, Hash128 documentHash)
        {
            if (string.IsNullOrEmpty(sourceDocumentPath))
                return;

            var bytesNeededSourceDocumentPath = System.Text.Encoding.UTF8.GetByteCount(sourceDocumentPath);
            var totalBytesNeeded = bytesNeededSourceDocumentPath + sizeof(int) + k_SizeOfHash128;

            m_DocumentSourceHashes.GrowIfNeeded(m_DocumentSourceHashes.Count + totalBytesNeeded);

            var offset = 0;
            var span = m_DocumentSourceHashes.AsSpanFullCapacity().Slice(m_DocumentSourceHashes.Count, totalBytesNeeded);
            BinaryWriteToSpan(bytesNeededSourceDocumentPath, span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
            if (bytesNeededSourceDocumentPath > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(sourceDocumentPath, span.Slice(offset, bytesNeededSourceDocumentPath));
                offset += bytesNeededSourceDocumentPath;
            }

            // Write the Hash128 directly to the span
            BinaryWriteToSpan(documentHash, span.Slice(offset, k_SizeOfHash128));

            m_DocumentSourceHashes.Count += totalBytesNeeded;
        }

        public bool TryGetSourceDocument(string sourceDocumentPath, out Hash128 documentHash)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            documentHash = default;
            if (string.IsNullOrEmpty(sourceDocumentPath))
                return false;

            var totalByteSize = m_DocumentSourceHashes.Count;
            var span = m_DocumentSourceHashes.AsReadOnlySpan();
            var offset = 0;

            while (offset < totalByteSize)
            {
                // Read the length of the source document path
                var bytesNeededSourceDocumentPath = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
                offset += sizeof(int);

                // Read the source document path
                var sourceDocumentPathBytes = span.Slice(offset, bytesNeededSourceDocumentPath);
                var currentSourceDocumentPath = System.Text.Encoding.UTF8.GetString(sourceDocumentPathBytes);
                offset += bytesNeededSourceDocumentPath;

                // If the source document path matches, read the Hash128
                if (currentSourceDocumentPath == sourceDocumentPath)
                {
                    // Read both Hash128 parts directly from the span
                    var u64_0 = BitConverter.ToUInt64(span.Slice(offset, sizeof(ulong)));
                    offset += sizeof(ulong);
                    var u64_1 = BitConverter.ToUInt64(span.Slice(offset, sizeof(ulong)));

                    documentHash = new Hash128(u64_0, u64_1);
                    return true; // Found the source document path
                }

                // Skip the Hash128
                offset += k_SizeOfHash128; // Skip the Hash128 part
            }
            return false; // Source document path not found
        }

        public void AddTypeStructuralVersion(string sourceDocumentPath, Hash128 typeStructuralVersion)
        {
            if (string.IsNullOrEmpty(sourceDocumentPath))
                return;

            var bytesNeededSourceDocumentPath = System.Text.Encoding.UTF8.GetByteCount(sourceDocumentPath);
            var totalBytesNeeded = bytesNeededSourceDocumentPath + sizeof(int) + k_SizeOfHash128;

            m_DocumentSourceTypeStructuralHashes.GrowIfNeeded(m_DocumentSourceTypeStructuralHashes.Count + totalBytesNeeded);

            var offset = 0;
            var span = m_DocumentSourceTypeStructuralHashes.AsSpanFullCapacity().Slice(m_DocumentSourceTypeStructuralHashes.Count, totalBytesNeeded);
            BinaryWriteToSpan(bytesNeededSourceDocumentPath, span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
            if (bytesNeededSourceDocumentPath > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(sourceDocumentPath, span.Slice(offset, bytesNeededSourceDocumentPath));
                offset += bytesNeededSourceDocumentPath;
            }

            // Write the Hash128 directly to the span
            BinaryWriteToSpan(typeStructuralVersion, span.Slice(offset, k_SizeOfHash128));

            m_DocumentSourceTypeStructuralHashes.Count += totalBytesNeeded;
        }

        public bool TryGetTypeStructuralVersion(string sourceDocumentPath, out Hash128 typeStructuralVersion)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            typeStructuralVersion = default;
            if (string.IsNullOrEmpty(sourceDocumentPath))
                return false;

            var totalByteSize = m_DocumentSourceTypeStructuralHashes.Count;
            var span = m_DocumentSourceTypeStructuralHashes.AsReadOnlySpan();
            var offset = 0;

            while (offset < totalByteSize)
            {
                // Read the length of the source document path
                var bytesNeededSourceDocumentPath = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
                offset += sizeof(int);

                // Read the source document path
                var sourceDocumentPathBytes = span.Slice(offset, bytesNeededSourceDocumentPath);
                var currentSourceDocumentPath = System.Text.Encoding.UTF8.GetString(sourceDocumentPathBytes);
                offset += bytesNeededSourceDocumentPath;

                // If the source document path matches, read the Hash128
                if (currentSourceDocumentPath == sourceDocumentPath)
                {
                    // Read both Hash128 parts directly from the span
                    var u64_0 = BitConverter.ToUInt64(span.Slice(offset, sizeof(ulong)));
                    offset += sizeof(ulong);
                    var u64_1 = BitConverter.ToUInt64(span.Slice(offset, sizeof(ulong)));

                    typeStructuralVersion = new Hash128(u64_0, u64_1);
                    return true; // Found the source document path
                }

                // Skip the Hash128
                offset += k_SizeOfHash128; // Skip the Hash128 part
            }
            return false; // Source document path not found
        }

        public void RemoveDocuments(string[] documentsToRemove)
        {
            throw new NotSupportedException($"{nameof(RemoveDocuments)} is not supported by {nameof(SearchIndexArtifactStorage)}");
        }

        public void AddWord(string word, int minVariations, int maxVariations, int score, int documentIndex)
        {
            var wordId = AddStringToStringTable(word);

            var entry = new SearchIndexArtifactWordEntry() { WordId = wordId, DocumentIndex = documentIndex, Score = score };

            m_WordEntries.Add(entry);
        }

        public void AddExactWord(string word, int score, int documentIndex)
        {
            AddWord(word, 1, int.MaxValue, score, documentIndex);
        }

        public void AddProperty(string name, double value, int score, int documentIndex)
        {
            var nameId = AddStringToStringTable(name);

            var entry = new SearchIndexArtifactPropertyDoubleEntry() { NameId = nameId, DocumentIndex = documentIndex, Score = score, Value = value };

            m_PropertyDoubleEntries.Add(entry);
            AddPropertyKeyword(name, string.Empty, false);
        }

        public void AddProperty(string name, string value, int minVariations, int maxVariations, int score, int documentIndex, bool exact, bool saveKeyword)
        {
            var nameId = AddStringToStringTable(name);
            var valueId = AddStringToStringTable(value);

            var entry = new SearchIndexArtifactPropertyStringEntry() { NameId = nameId, DocumentIndex = documentIndex, Score = score, Value = valueId };

            m_PropertyStringEntries.Add(entry);
            AddPropertyKeyword(name, value, saveKeyword);
        }

        public void AddExactProperty(string name, string value, int score, int documentIndex, bool saveKeyword)
        {
            AddProperty(name, value, 1, int.MaxValue, score, documentIndex, true, saveKeyword);
        }

        public void MapProperty(string name, string label, string help, string propertyType, string ownerTypeName, SearchPropositionGenerationOptions propositionGenerationOptions, bool removeNestedKeys)
        {
            if (propositionGenerationOptions != SearchPropositionGenerationOptions.None)
                AddPropertyKeyword(name, $"|{label}|{help}|{propertyType}|{ownerTypeName}|{(int)propositionGenerationOptions}", true);
            else
                AddPropertyKeyword(name, $"|{label}|{help}|{propertyType}|{ownerTypeName}", true);

            var propertyNameId = AddStringToStringTable(name);
            // Add the command to remove the empty keyword.
            m_KeywordRemoveEntries.Add(new SearchIndexArtifactKeywordRemoveEntry { RemoveType = SearchIndexArtifactKeywordRemoveType.Empty, PropertyNameId = propertyNameId });

            if (removeNestedKeys)
            {
                var entry = new SearchIndexArtifactKeywordRemoveEntry { RemoveType = SearchIndexArtifactKeywordRemoveType.NestedKeys, PropertyNameId = propertyNameId };
                m_KeywordRemoveEntries.Add(entry);
            }
        }

        public void Start(bool clear) { }

        public void Finish(string[] removedDocuments)
        {
            Version = DefaultVersion;
            Timestamp = DateTime.UtcNow.Ticks;
        }

        public void CombineIndexes(IReadOnlyList<SearchIndexer> indexes, int baseScore, string indexName, SearchTask<TaskData> task)
        {
            throw new NotSupportedException($"{nameof(CombineIndexes)} is not supported by {nameof(SearchIndexArtifactStorage)}");
        }

        public void Merge(string[] removedDocuments, SearchIndexer other, int baseScore, Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task)
        {
            throw new NotSupportedException($"{nameof(Merge)} is not supported by {nameof(SearchIndexArtifactStorage)}");
        }

        public void ApplyFrom(SearchIndexer source)
        {
            throw new NotSupportedException($"{nameof(ApplyFrom)} is not supported by {nameof(SearchIndexArtifactStorage)}");
        }

        public void Write(Stream stream)
        {
            using var bw = new BinaryWriter(stream, Encoding.Default, true);

            var startPosition = stream.Position;

            var header = new SearchIndexArtifactStorageHeader
            {
                MagicNumber = SearchIndexArtifactStorageHeader.DefaultMagicNumber,
                FileSize = 0, // Will be filled later
                Version = DefaultVersion,
                Timestamp = Timestamp,
                DocumentsOffset = 0, // Will be filled later
                DocumentsSize = (ulong)m_DocumentBytes.Count,
                DocumentSourcesOffset = 0, // Will be filled later
                DocumentSourcesSize = (ulong)m_DocumentSourceHashes.Count,
                DocumentSourceTypeStructuralHashesOffset = 0, // Will be filled later
                DocumentSourceTypeStructuralHashesSize = (ulong)m_DocumentSourceTypeStructuralHashes.Count,
                WordsOffset = 0, // Will be filled later
                WordsSize = (ulong)m_WordEntries.Count * SearchIndexArtifactWordEntry.ByteSize,
                PropertyDoublesOffset = 0, // Will be filled later
                PropertyDoublesSize = (ulong)m_PropertyDoubleEntries.Count * SearchIndexArtifactPropertyDoubleEntry.ByteSize,
                PropertyStringsOffset = 0, // Not used in this storage
                PropertyStringsSize = (ulong)m_PropertyStringEntries.Count * SearchIndexArtifactPropertyStringEntry.ByteSize,
                KeywordsOffset = 0, // Will be filled later
                KeywordsSize = (ulong)m_KeywordEntries.Count * SearchIndexArtifactKeywordEntry.ByteSize,
                KeywordsRemoveOffset = 0, // Will be filled later
                KeywordsRemoveSize = (ulong)m_KeywordRemoveEntries.Count * SearchIndexArtifactKeywordRemoveEntry.ByteSize,
                MetaInfoOffset = 0, // Will be filled later
                MetaInfoSize = (ulong)m_MetaInfoBytes.Count,
                StringTableOffset = 0, // Will be filled later
                StringTableSize = (ulong)m_StringTable.Count
            };

            // Write the header with initial values
            header.WriteToBinary(bw, startPosition);

            // Documents
            header.DocumentsOffset = (ulong)stream.Position;
            bw.Write(m_DocumentBytes.AsReadOnlySpan());

            // Document sources
            header.DocumentSourcesOffset = (ulong)stream.Position;
            bw.Write(m_DocumentSourceHashes.AsReadOnlySpan());

            // Document source type structural hashes
            header.DocumentSourceTypeStructuralHashesOffset = (ulong)stream.Position;
            bw.Write(m_DocumentSourceTypeStructuralHashes.AsReadOnlySpan());

            // Words
            header.WordsOffset = (ulong)stream.Position;
            var wordsByteArray = m_WordEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactWordEntry.ByteSize);
            bw.Write(wordsByteArray.AsReadOnlySpan().Slice(0, m_WordEntries.Count * SearchIndexArtifactWordEntry.ByteSize));

            // Property doubles
            header.PropertyDoublesOffset = (ulong)stream.Position;
            var propertyDoublesByteArray = m_PropertyDoubleEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactPropertyDoubleEntry.ByteSize);
            bw.Write(propertyDoublesByteArray.AsReadOnlySpan().Slice(0, m_PropertyDoubleEntries.Count * SearchIndexArtifactPropertyDoubleEntry.ByteSize));

            // Property strings
            header.PropertyStringsOffset = (ulong)stream.Position;
            var propertyStringsByteArray = m_PropertyStringEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactPropertyStringEntry.ByteSize);
            bw.Write(propertyStringsByteArray.AsReadOnlySpan().Slice(0, m_PropertyStringEntries.Count * SearchIndexArtifactPropertyStringEntry.ByteSize));

            // Keywords
            header.KeywordsOffset = (ulong)stream.Position;
            var keywordsByteArray = m_KeywordEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactKeywordEntry.ByteSize);
            bw.Write(keywordsByteArray.AsReadOnlySpan().Slice(0, m_KeywordEntries.Count * SearchIndexArtifactKeywordEntry.ByteSize));

            // Keywords nested remove
            header.KeywordsRemoveOffset = (ulong)stream.Position;
            var keywordsNestedRemoveByteArray = m_KeywordRemoveEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactKeywordRemoveEntry.ByteSize);
            bw.Write(keywordsNestedRemoveByteArray.AsReadOnlySpan().Slice(0, m_KeywordRemoveEntries.Count * SearchIndexArtifactKeywordRemoveEntry.ByteSize));

            // Meta info
            header.MetaInfoOffset = (ulong)stream.Position;
            bw.Write(m_MetaInfoBytes.AsReadOnlySpan());

            // String table
            header.StringTableOffset = (ulong)stream.Position;
            bw.Write(m_StringTable.AsReadOnlySpan());

            // Update the file size
            header.FileSize = (ulong)(stream.Length - startPosition);

            // Write the header again with the correct values
            header.WriteToBinary(bw, startPosition);
        }

        public bool Read(Stream stream, bool checkVersionOnly)
        {
            using var br = new BinaryReader(stream, Encoding.Default, true);
            var startPosition = stream.Position;

            if (stream.Length < startPosition + UnsafeUtility.SizeOf<SearchIndexArtifactStorageHeader>())
                return false; // Not enough data to read the header
            var header = SearchIndexArtifactStorageHeader.ReadFromBinary(br, startPosition);
            if (header.MagicNumber != SearchIndexArtifactStorageHeader.DefaultMagicNumber)
                return false; // Invalid magic number
            if (header.Version != DefaultVersion)
                return false; // Unsupported version

            if (checkVersionOnly)
                return true; // If we are only checking the version, we can return early

            if (header.FileSize > (ulong)(stream.Length - startPosition))
                return false; // File size mismatch

            Version = header.Version;
            Timestamp = header.Timestamp;

            // Read documents
            if (header.DocumentsSize > 0)
            {
                m_DocumentBytes.GrowIfNeeded((int)header.DocumentsSize);
                stream.Seek((long)header.DocumentsOffset, SeekOrigin.Begin);
                m_DocumentBytes.Count = br.Read(m_DocumentBytes.AsSpanFullCapacity().Slice(0, (int)header.DocumentsSize));
            }

            // Read document sources
            if (header.DocumentSourcesSize > 0)
            {
                m_DocumentSourceHashes.GrowIfNeeded((int)header.DocumentSourcesSize);
                stream.Seek((long)header.DocumentSourcesOffset, SeekOrigin.Begin);
                m_DocumentSourceHashes.Count = br.Read(m_DocumentSourceHashes.AsSpanFullCapacity().Slice(0, (int)header.DocumentSourcesSize));
            }

            // Read document source type structural hashes
            if (header.DocumentSourceTypeStructuralHashesSize > 0)
            {
                m_DocumentSourceTypeStructuralHashes.GrowIfNeeded((int)header.DocumentSourceTypeStructuralHashesSize);
                stream.Seek((long)header.DocumentSourceTypeStructuralHashesOffset, SeekOrigin.Begin);
                m_DocumentSourceTypeStructuralHashes.Count = br.Read(m_DocumentSourceTypeStructuralHashes.AsSpanFullCapacity().Slice(0, (int)header.DocumentSourceTypeStructuralHashesSize));
            }

            // Read words
            if (header.WordsSize > 0)
            {
                m_WordEntries.GrowIfNeeded((int)(header.WordsSize / SearchIndexArtifactWordEntry.ByteSize));
                stream.Seek((long)header.WordsOffset, SeekOrigin.Begin);
                var wordsByteArray = m_WordEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactWordEntry.ByteSize);
                br.Read(wordsByteArray.AsSpan().Slice(0, (int)header.WordsSize));
                m_WordEntries.Count = (int)(header.WordsSize / SearchIndexArtifactWordEntry.ByteSize);
            }

            // Read property doubles
            if (header.PropertyDoublesSize > 0)
            {
                m_PropertyDoubleEntries.GrowIfNeeded((int)(header.PropertyDoublesSize / SearchIndexArtifactPropertyDoubleEntry.ByteSize));
                stream.Seek((long)header.PropertyDoublesOffset, SeekOrigin.Begin);
                var propertyDoublesByteArray = m_PropertyDoubleEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactPropertyDoubleEntry.ByteSize);
                br.Read(propertyDoublesByteArray.AsSpan().Slice(0, (int)header.PropertyDoublesSize));
                m_PropertyDoubleEntries.Count = (int)(header.PropertyDoublesSize / SearchIndexArtifactPropertyDoubleEntry.ByteSize);
            }

            // Read property strings
            if (header.PropertyStringsSize > 0)
            {
                m_PropertyStringEntries.GrowIfNeeded((int)(header.PropertyStringsSize / SearchIndexArtifactPropertyStringEntry.ByteSize));
                stream.Seek((long)header.PropertyStringsOffset, SeekOrigin.Begin);
                var propertyStringsByteArray = m_PropertyStringEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactPropertyStringEntry.ByteSize);
                br.Read(propertyStringsByteArray.AsSpan().Slice(0, (int)header.PropertyStringsSize));
                m_PropertyStringEntries.Count = (int)(header.PropertyStringsSize / SearchIndexArtifactPropertyStringEntry.ByteSize);
            }

            // Read keywords
            if (header.KeywordsSize > 0)
            {
                m_KeywordEntries.GrowIfNeeded((int)(header.KeywordsSize / SearchIndexArtifactKeywordEntry.ByteSize));
                stream.Seek((long)header.KeywordsOffset, SeekOrigin.Begin);
                var keywordsByteArray = m_KeywordEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactKeywordEntry.ByteSize);
                br.Read(keywordsByteArray.AsSpan().Slice(0, (int)header.KeywordsSize));
                m_KeywordEntries.Count = (int)(header.KeywordsSize / SearchIndexArtifactKeywordEntry.ByteSize);
            }

            // Read keywords remove
            if (header.KeywordsRemoveSize > 0)
            {
                m_KeywordRemoveEntries.GrowIfNeeded((int)(header.KeywordsRemoveSize / SearchIndexArtifactKeywordRemoveEntry.ByteSize));
                stream.Seek((long)header.KeywordsRemoveOffset, SeekOrigin.Begin);
                var keywordsNestedRemoveByteArray = m_KeywordRemoveEntries.BackingArray.Reinterpret<byte>(SearchIndexArtifactKeywordRemoveEntry.ByteSize);
                br.Read(keywordsNestedRemoveByteArray.AsSpan().Slice(0, (int)header.KeywordsRemoveSize));
                m_KeywordRemoveEntries.Count = (int)(header.KeywordsRemoveSize / SearchIndexArtifactKeywordRemoveEntry.ByteSize);
            }

            // Read meta info
            if (header.MetaInfoSize > 0)
            {
                m_MetaInfoBytes.GrowIfNeeded((int)header.MetaInfoSize);
                stream.Seek((long)header.MetaInfoOffset, SeekOrigin.Begin);
                m_MetaInfoBytes.Count = br.Read(m_MetaInfoBytes.AsSpanFullCapacity().Slice(0, (int)header.MetaInfoSize));
            }

            // Read string table
            if (header.StringTableSize > 0)
            {
                m_StringTable.GrowIfNeeded((int)header.StringTableSize);
                stream.Seek((long)header.StringTableOffset, SeekOrigin.Begin);
                m_StringTable.Count = br.Read(m_StringTable.AsSpanFullCapacity().Slice(0, (int)header.StringTableSize));
                m_StringTableAccelerator.Clear();
                var span = m_StringTable.AsReadOnlySpan();
                for (var i = 0; i < m_StringTable.Count;)
                {
                    var id = BitConverter.ToInt32(span.Slice(i, sizeof(int)));
                    i += sizeof(int);
                    var length = BitConverter.ToInt32(span.Slice(i, sizeof(int)));
                    i += sizeof(int);
                    if (length > 0)
                    {
                        var str = System.Text.Encoding.UTF8.GetString(span.Slice(i, length));
                        i += length;
                        m_StringTableAccelerator[str] = id;
                    }
                }
            }

            return true;
        }

        public IEnumerable<SearchResult> SearchTerm(string name, object value, SearchIndexOperator op, bool exclude, SearchResultCollection subset)
        {
            throw new NotSupportedException($"{nameof(SearchTerm)} is not supported by {nameof(SearchIndexArtifactStorage)}");
        }

        void AddPropertyKeyword(string propertyName, string propertyValue, bool saveValueKeyword)
        {
            var propertyNameId = AddStringToStringTable(propertyName);

            int propertyValueId = k_InvalidStringId;
            if (saveValueKeyword)
            {
                // If the value is not in the string table, add it.
                propertyValueId = AddStringToStringTable(propertyValue);
            }

            var entry = new SearchIndexArtifactKeywordEntry { PropertyNameId = propertyNameId, HelpId = propertyValueId };
            m_KeywordEntries.Add(entry);
        }

        internal int AddStringToStringTable(string str)
        {
            if (m_StringTableAccelerator.TryGetValue(str, out var strId))
                return strId;

            // If the name is not in the string table, add it.
            if (string.IsNullOrEmpty(str))
                return k_InvalidStringId;

            var id = ++m_NextStringId;
            var bytesNeededStr = System.Text.Encoding.UTF8.GetByteCount(str);
            var totalBytesNeeded = bytesNeededStr + sizeof(int) * 2; // sizeof(int) for the id and sizeof(int) for the length of the string

            m_StringTable.GrowIfNeeded(m_StringTable.Count + totalBytesNeeded);
            var offset = m_StringTable.Count;
            var span = m_StringTable.AsSpanFullCapacity().Slice(offset, totalBytesNeeded);
            BinaryWriteToSpan(id, span.Slice(0, sizeof(int)));
            BinaryWriteToSpan(bytesNeededStr, span.Slice(sizeof(int), sizeof(int)));
            if (bytesNeededStr > 0)
            {
                System.Text.Encoding.UTF8.GetBytes(str, span.Slice(sizeof(int) * 2, bytesNeededStr));
            }

            m_StringTable.Count += totalBytesNeeded;
            m_StringTableAccelerator[str] = id;
            return id;
        }

        string GetStringFromStringTable(int stringId)
        {
            // This method does not need to be optimal, this will not be called outside of testing.
            if (stringId == k_InvalidStringId)
                return null;

            foreach (var kvp in m_StringTableAccelerator)
            {
                if (kvp.Value == stringId)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        static SearchDocument GetDocumentFromBytes(ReadOnlySpan<byte> span, ref int documentIndex, ref int bytesRead)
        {
            var offset = 0;

            if (span.Length < k_MinimumDocumentSize)
                throw new InvalidOperationException("Invalid document data format.");

            // Read the document index
            documentIndex = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
            offset += sizeof(int);

            // Read the length of the documentId
            var documentIdLength = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
            offset += sizeof(int);

            // Read the documentId content
            var documentIdContent = documentIdLength > 0 ? System.Text.Encoding.UTF8.GetString(span.Slice(offset, documentIdLength)) : null;
            offset += documentIdLength;

            // Read the length of the name
            var nameLength = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
            offset += sizeof(int);

            // Read the name content
            var nameContent = nameLength > 0 ? System.Text.Encoding.UTF8.GetString(span.Slice(offset, nameLength)) : null;
            offset += nameLength;

            // Read the length of the source
            var sourceLength = BitConverter.ToInt32(span.Slice(offset, sizeof(int)));
            offset += sizeof(int);

            // Read the source content
            var sourceContent = sourceLength > 0 ? System.Text.Encoding.UTF8.GetString(span.Slice(offset, sourceLength)) : null;
            offset += sourceLength;

            // Read flags
            var flags = (SearchDocumentFlags)BitConverter.ToInt32(span.Slice(offset, sizeof(SearchDocumentFlags)));
            offset += sizeof(SearchDocumentFlags);
            bytesRead = offset;

            return new SearchDocument(documentIdContent, nameContent, sourceContent, 0, flags);
        }

        static void BinaryWriteToSpan(int value, Span<byte> span)
        {
            if (span.Length < sizeof(int))
                throw new ArgumentException("Span is too small to write an int value.");
            BitConverter.TryWriteBytes(span, value);
        }

        void BinaryWriteToSpan(Hash128 hash, Span<byte> span)
        {
            if (span.Length < k_SizeOfHash128)
                throw new ArgumentException("Span is too small to write a Hash128 value.");

            BitConverter.TryWriteBytes(span, hash.u64_0);
            BitConverter.TryWriteBytes(span.Slice(sizeof(ulong)), hash.u64_1);
        }
    }
}
