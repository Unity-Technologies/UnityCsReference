// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace UnityEditor.Search
{
    readonly struct SearchIndexEntry : IEquatable<SearchIndexEntry>
    {
        //  1- Initial format
        //  2- Added score to words
        //  3- Save base name in entry paths
        //  4- Added entry types
        //  5- Added indexing tags
        //  6- Revert indexes back to 32 bits instead of 64 bits.
        //  7- Remove min and max char variations.
        //  8- Add metadata field to documents
        //  9- Add document hash support
        // 10- Remove the index tag header
        // 11- Save more keywords
        // 12- Save timestamp in header
        // 13- Update SearchDocument structure
        // 14- Add document meta info support
        // 15- Add document name and source
        // 16- Save all strings in string table header to save space.
        // 17- Compress search index entry with same document indexes.
        // 18- Add search document flags.
        // 19- Merge SearchIndexEntry.version and SearchIndexEntryImporter.version from now on.
        // 20- Optimize search index entry document indexes serialization.
        // 21- Expand keyword serialization.
        // 22- Discard long string properties and add support for hash128 while indexing
        // 23- Improve keyword type encoding
        // 24- Optimize asset path indexing in size
        // 25- Fix SearchDocument concurrency
        // 26- Index all dir tokens in combination for dir: filter.
        // 27- Use AssemblyQualifiedName for PropertyType in IndexProperty<TProperty, TPropertyOwner>
        // 28- Improve Properties indexing (supports more types)
        // 29- Add component prefix + m_Enabled property
        // 30- Use SearchDocumentList
        // 31- Update custom indexers with prefabs.
        // 32- Property references also index GlobalObjectId with PropertyName.
        // 33- Support for EnumFlags.
        // 34- Support structural hashes
        // 35- Modify ObjectIndex.IndexProperty to force all property names to be lowercase.
        internal const int version = 0x035;

        public enum Type : byte
        {
            Undefined = 0,
            Word,
            Number,
            Property
        }

        // Serialized and sorted fields
        public readonly long key;      // Value hash
        public readonly int crc;       // Value correction code (can be length, property key hash, etc.)
        public readonly Type type;     // Type of the index entry
        public readonly int score;     // Score of the entry (used for sorting)

        public readonly double number;
        public readonly SearchDocumentList docs;

        public static int size => sizeof(long) + 2 * sizeof(int) + sizeof(Type) + sizeof(double) + SearchDocumentList.size;

        public static readonly SearchIndexEntry MaxEntry = new SearchIndexEntry(long.MaxValue, int.MaxValue, (Type)byte.MaxValue);

        public SearchIndexEntry(long _key, int _crc, Type _type)
            : this(_key, _crc, _type, int.MaxValue, new SearchDocumentList())
        { }

        public SearchIndexEntry(long _key, int _crc, Type _type, int _score, SearchDocumentList _docs)
        {
            key = _key;
            crc = _crc;
            type = _type;
            score = _score;
            docs = _docs;
            number = _type == Type.Number ? BitConverter.Int64BitsToDouble(key) : double.NaN;
        }

        public override string ToString()
        {
            if (type == Type.Number)
                return $"[{type}] {crc}:{number} ({score}) [{docs.ToString()}]";
            return $"[{type}] {crc}:{key} ({score}) [{docs.ToString()}]";
        }

        public override int GetHashCode()
        {
            return key.GetHashCode() ^ crc.GetHashCode() ^ type.GetHashCode();
        }

        public override bool Equals(object other)
        {
            return other is SearchIndexEntry l && Equals(l);
        }

        public bool Equals(SearchIndexEntry other)
        {
            return key == other.key && crc == other.crc && type == other.type;
        }
    }

    class SearchStringTable : IList<string>
    {
        private List<string> m_Table;
        private int m_Candidates = 0;

        public SearchStringTable()
        {
            m_Table = new List<string>();
            Add(string.Empty);
        }

        public override string ToString()
        {
            return $"{m_Table.Count}/{m_Candidates}";
        }

        public SearchStringTable(BinaryReader indexReader)
        {
            var elementCount = indexReader.ReadInt32();
            m_Table = new List<string>(elementCount);
            for (int i = 0; i < elementCount; ++i)
                m_Table.Add(indexReader.ReadString());
        }

        public void Write(BinaryWriter indexWriter)
        {
            indexWriter.Write(m_Table.Count);
            foreach (var s in m_Table)
                indexWriter.Write(s);
        }

        public string this[int index]
        {
            get => m_Table[index];
            set => throw new NotSupportedException();
        }

        public int Count => m_Table.Count;
        public bool IsReadOnly => false;

        public void Add(string item)
        {
            m_Candidates++;
            var insertAt = IndexOf(item);
            if (insertAt < 0)
            {
                insertAt = ~insertAt;
                m_Table.Insert(insertAt, item);
            }
        }

        public void Clear() => m_Table.Clear();
        public bool Contains(string item) => IndexOf(item) >= 0;
        [System.Runtime.CompilerServices.MethodImpl(256)] public int IndexOf(string item) => m_Table.BinarySearch(item != null ? item : string.Empty);
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public int WIndexOf(string item)
        {
            var si = IndexOf(item);
            if (si < 0 || si >= m_Table.Count)
                throw new Exception($"Invalid index for {item}");
            return si;
        }

        public void CopyTo(string[] array, int arrayIndex) => m_Table.CopyTo(array, arrayIndex);
        public IEnumerator<string> GetEnumerator() => m_Table.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => m_Table.GetEnumerator();

        public void Insert(int index, string item) => throw new NotSupportedException();
        public bool Remove(string item)
        {
            var ri = IndexOf(item);
            if (ri < 0)
                return false;
            RemoveAt(ri);
            return true;
        }

        public void RemoveAt(int index)
        {
            m_Table.RemoveAt(index);
        }

        public void UnionWith(IEnumerable<string> items)
        {
            foreach (var e in items)
                Add(e);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void Write(BinaryWriter indexWriter, string s)
        {
            indexWriter.Write(WIndexOf(s));
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public string Read(BinaryReader indexReader)
        {
            var si = indexReader.ReadInt32();
            return string.Intern(m_Table[si]);
        }
    }

    class SearchIndexEntryStorage : ISearchIndexerStorage, IDisposable
    {
        [Serializable]
        struct DocumentContentDescriptor
        {
            public Hash128 contentHash;
            public Hash128 structuralHash;
        }

        long m_Timestamp;
        volatile bool m_IndexReady = false;

        // Temporary documents and entries while the index is being built (i.e. Start/Finish).
        readonly List<SearchIndexEntry> m_BatchIndexes;

        // Final documents and entries when the index is ready.
        int m_NextDocumentIndex;
        ConcurrentDictionary<int, SearchDocument> m_Documents;
        SearchNativeReadOnlyArray<SearchIndexEntry> m_Indexes;
        HashSet<string> m_Keywords;
        ConcurrentDictionary<string, DocumentContentDescriptor> m_SourceDocuments;
        ConcurrentDictionary<string, string> m_MetaInfo;
        internal ConcurrentDictionary<string, int> m_IndexByDocuments;
        SearchDocumentListTable m_DocumentListTable;
        GCHandle m_DocumentListTableHandle;

        SearchResultCollection m_AllDocumentIndexes;

        public int Version => SearchIndexEntry.version; // Nothing is loaded unless it has the correct version

        public long Timestamp => m_Timestamp;
        public int KeywordCount => m_Keywords.Count;
        public int DocumentCount => GetDocuments(true).Count();

        public int EntryCount
        {
            get
            {
                lock (this)
                {
                    int total = 0;
                    if (m_Indexes.Count > 0)
                        total += m_Indexes.Count;
                    if (m_BatchIndexes != null && m_BatchIndexes.Count > 0)
                        total += m_BatchIndexes.Count;
                    return total;
                }
            }
        }

        public Func<string, string> ResolveDocumentHandler { get; set; }
        public Func<string, SearchIndexOperator, SearchResultCollection, IEnumerable<SearchResult>> ExtraSearchWordHandler { get; set; }

        public int InvalidDocumentIndex => SearchIndexer.invalidDocumentIndex;

        internal IEnumerable<SearchIndexEntry> indexes => m_Indexes;
        internal SearchDocumentListTable documentListTable => m_DocumentListTable;

        public SearchIndexEntryStorage()
        {
            m_Keywords = new HashSet<string>();
            m_Documents = new ConcurrentDictionary<int, SearchDocument>();
            m_NextDocumentIndex = InvalidDocumentIndex;
            m_IndexByDocuments = new ConcurrentDictionary<string, int>();
            m_Indexes = default;
            m_BatchIndexes = new List<SearchIndexEntry>();
            m_SourceDocuments = new ConcurrentDictionary<string, DocumentContentDescriptor>();
            m_MetaInfo = new ConcurrentDictionary<string, string>();
            m_DocumentListTable = new SearchDocumentListTable(doCreate: false);
            m_DocumentListTableHandle = default;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            m_Indexes.Dispose();
            if (m_DocumentListTableHandle.IsAllocated)
                m_DocumentListTableHandle.Free();
            m_DocumentListTable?.Dispose();

            if (disposing)
            {
                m_IndexReady = false;
                m_BatchIndexes.Clear();
                m_Keywords.Clear();
                m_Documents.Clear();
                m_IndexByDocuments.Clear();
                m_SourceDocuments.Clear();
                m_MetaInfo.Clear();
                m_DocumentListTable = new SearchDocumentListTable(doCreate: false);
            }
        }

        public bool IsReady() => m_IndexReady;

        public IEnumerable<string> GetKeywords()
        {
            lock (this)
                return m_Keywords;
        }

        public void SetMetaInfo(string documentId, string metadata)
        {
            m_MetaInfo[documentId] = metadata;
        }

        public string GetMetaInfo(string documentId)
        {
            if (m_MetaInfo.TryGetValue(documentId, out var metadata))
                return metadata;
            return null;
        }

        public IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls = false)
        {
            return ignoreNulls ? m_Documents.Values.Where(d => d.valid) : m_Documents.Values;
        }

        public IEnumerable<int> GetDocumentIndexes()
        {
            return m_Documents.Keys;
        }

        public SearchDocument GetDocument(int index)
        {
            if (m_Documents.TryGetValue(index, out var doc))
                return doc;
            return default;
        }

        public int FindDocumentIndex(string documentId)
        {
            if (m_IndexByDocuments.TryGetValue(documentId, out var di) && m_Documents.ContainsKey(di))
                return di;
            foreach (var d in m_Documents)
            {
                if (d.Value.valid && d.Value.id.Equals(documentId, StringComparison.Ordinal))
                    return d.Key;
            }
            return InvalidDocumentIndex;
        }

        public int AddDocument(string documentId, string name, string source, bool checkIfExists, SearchDocumentFlags flags)
        {
            if (checkIfExists)
            {
                var di = FindDocumentIndex(documentId);
                if (m_Documents.TryGetValue(di, out var existingDoc))
                {
                    m_Documents[di] = new SearchDocument(existingDoc, name);
                    return di;
                }
            }
            var newIndex = Interlocked.Increment(ref m_NextDocumentIndex);
            var newDocument = new SearchDocument(documentId, name, source, 0, flags);
            while (!m_Documents.TryAdd(newIndex, newDocument))
                newIndex = Interlocked.Increment(ref m_NextDocumentIndex);
            m_IndexByDocuments[documentId] = newIndex;
            return newIndex;
        }

        public void AddSourceDocument(string sourceDocumentPath, Hash128 documentHash)
        {
            if (!m_SourceDocuments.TryGetValue(sourceDocumentPath, out var desc))
            {
                desc = new DocumentContentDescriptor();
            }
            desc.contentHash = documentHash;
            m_SourceDocuments[sourceDocumentPath] = desc;
        }

        public bool TryGetSourceDocument(string sourceDocumentPath, out Hash128 documentHash)
        {
            if (!m_IndexReady)
            {
                documentHash = default;
                return false;
            }

            var found = m_SourceDocuments.TryGetValue(sourceDocumentPath, out var desc);
            documentHash = desc.contentHash;
            return found;
        }

        public void AddTypeStructuralVersion(string sourceDocumentPath, Hash128 typeStructuralVersion)
        {
            if (!m_SourceDocuments.TryGetValue(sourceDocumentPath, out var desc))
            {
                desc = new DocumentContentDescriptor();
            }
            desc.structuralHash = typeStructuralVersion;
            m_SourceDocuments[sourceDocumentPath] = desc;
        }

        public bool TryGetTypeStructuralVersion(string sourceDocumentPath, out Hash128 typeStructuralVersion)
        {
            if (!m_IndexReady)
            {
                typeStructuralVersion = default;
                return false;
            }

            var found = m_SourceDocuments.TryGetValue(sourceDocumentPath, out var desc);
            typeStructuralVersion = desc.structuralHash;
            return found;
        }

        public void AddWord(string word, int minVariations, int maxVariations, int score, int documentIndex)
        {
            if (string.IsNullOrEmpty(word))
                return;

            maxVariations = Math.Min(maxVariations, word.Length);
            var docList = new SearchDocumentList(documentIndex);
            for (int c = Math.Min(minVariations, maxVariations); c <= maxVariations; ++c)
            {
                var ss = word.Substring(0, c);
                m_BatchIndexes.Add(new SearchIndexEntry(HashingUtils.GetHashCode(ss), ss.Length, SearchIndexEntry.Type.Word, score, docList));
            }

            if (word.Length > maxVariations)
                m_BatchIndexes.Add(new SearchIndexEntry(HashingUtils.GetHashCode(word), word.Length, SearchIndexEntry.Type.Word, score - 1, docList));
        }

        public void AddExactWord(string word, int score, int documentIndex)
        {
            m_BatchIndexes.Add(new SearchIndexEntry(HashingUtils.GetHashCode(word), int.MaxValue, SearchIndexEntry.Type.Word, score, new SearchDocumentList(documentIndex)));
        }

        public void AddProperty(string name, double value, int score, int documentIndex)
        {
            var keyHash = HashingUtils.GetHashCode(name);
            var longNumber = BitConverter.DoubleToInt64Bits(value);
            m_BatchIndexes.Add(new SearchIndexEntry(longNumber, keyHash, SearchIndexEntry.Type.Number, score, new SearchDocumentList(documentIndex)));

            m_Keywords.Add($"{name}:");
        }

        public void AddProperty(string name, string value, int minVariations, int maxVariations, int score, int documentIndex, bool exact, bool saveKeyword)
        {
            var nameHash = HashingUtils.GetHashCode(name);
            var valueHash = HashingUtils.GetHashCode(value);
            maxVariations = Math.Min(maxVariations, value.Length);
            if (minVariations > value.Length)
                minVariations = value.Length;
            if (ExcludeWordVariations(value))
                minVariations = maxVariations = value.Length;

            var docList = new SearchDocumentList(documentIndex);
            for (int c = Math.Min(minVariations, maxVariations); c <= maxVariations; ++c)
            {
                var ss = value.Substring(0, c);
                m_BatchIndexes.Add(new SearchIndexEntry(HashingUtils.GetHashCode(ss), nameHash, SearchIndexEntry.Type.Property, score + (maxVariations - c), docList));
            }

            if (value.Length > maxVariations)
                m_BatchIndexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, score - 1, docList));

            if (exact)
            {
                nameHash ^= HashingUtils.GetHashCode(name.Length);
                valueHash ^= HashingUtils.GetHashCode(value.Length);
                m_BatchIndexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, score - 3, docList));
            }

            if (saveKeyword)
                m_Keywords.Add($"{name}:{value}");
            else
                m_Keywords.Add($"{name}:");
        }

        public void AddExactProperty(string name, string value, int score, int documentIndex, bool saveKeyword)
        {
            var nameHash = HashingUtils.GetHashCode(name);
            var valueHash = HashingUtils.GetHashCode(value);

            // Add an exact match for property="match"
            nameHash ^= HashingUtils.GetHashCode(name.Length);
            valueHash ^= HashingUtils.GetHashCode(value.Length);
            m_BatchIndexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, score - 3, new SearchDocumentList(documentIndex)));

            if (saveKeyword)
                m_Keywords.Add($"{name}:{value}");
            else
                m_Keywords.Add($"{name}:");
        }

        public void MapProperty(string name, string label, string help, string propertyType, string ownerTypeName, SearchPropositionGenerationOptions propositionGenerationOptions, bool removeNestedKeys)
        {
            if (propositionGenerationOptions != SearchPropositionGenerationOptions.None)
                MapKeyword(name + ":", $"{label}|{help}|{propertyType}|{ownerTypeName}|{(int)propositionGenerationOptions}");
            else
                MapKeyword(name + ":", $"{label}|{help}|{propertyType}|{ownerTypeName}");
            if (removeNestedKeys)
            {
                // TODO: Is this correct? It only removes nested keys that
                // don't have any help.
                m_Keywords.Remove(name + ".x:");
                m_Keywords.Remove(name + ".y:");
                m_Keywords.Remove(name + ".z:");
                m_Keywords.Remove(name + ".w:");
                m_Keywords.Remove(name + ".r:");
                m_Keywords.Remove(name + ".g:");
                m_Keywords.Remove(name + ".b:");
                m_Keywords.Remove(name + ".a:");
            }
        }

        public void Start(bool clear = false)
        {
            lock (this)
            {
                m_IndexReady = false;
                m_BatchIndexes.Clear();

                if (clear)
                {
                    m_Keywords.Clear();
                    m_Documents.Clear();
                    m_IndexByDocuments.Clear();
                    m_SourceDocuments.Clear();
                    m_MetaInfo.Clear();
                    m_Indexes.Dispose();
                    m_Indexes = default;
                    if (m_DocumentListTableHandle.IsAllocated)
                        m_DocumentListTableHandle.Free();
                    m_DocumentListTable.Dispose();
                    m_DocumentListTable = new SearchDocumentListTable(doCreate: false);
                }
            }
        }

        public void Finish(string[] removedDocuments)
        {
            lock (this)
            {
                RemoveDocuments(removedDocuments, false);
                UpdateIndexesOnFinish();
                m_BatchIndexes.Clear();
            }
        }

        public void RemoveDocuments(string[] documentsToRemove)
        {
            RemoveDocuments(documentsToRemove, true);
        }

        public void RemoveDocuments(string[] documentsToRemove, bool executeCleanup)
        {
            var shouldRemoveDocuments = documentsToRemove != null && documentsToRemove.Length > 0;
            if (shouldRemoveDocuments)
            {
                var removedDocIndexes = new HashSet<int>();
                foreach (var rd in documentsToRemove)
                {
                    var docs = FindDocumentIndexesByPath(rd);
                    removedDocIndexes.UnionWith(docs);
                }

                foreach (var removedDocIndex in removedDocIndexes)
                {
                    if (!m_Documents.TryRemove(removedDocIndex, out var removedDoc) || !m_IndexByDocuments.TryRemove(removedDoc.id, out _))
                        Console.WriteLine($"[QS] Failed to remove search document {removedDoc.id} at {removedDocIndexes}");
                }

                m_DocumentListTable.RemoveDocuments(removedDocIndexes);
                for (var i = 0; i < m_Indexes.Count; i++)
                {
                    var ie = m_Indexes[i];
                    m_Indexes[i] = new SearchIndexEntry(ie.key, ie.crc, ie.type, ie.score, ie.docs.RemoveDocuments(removedDocIndexes));
                }

                for (var i = 0; i < m_BatchIndexes.Count; ++i)
                {
                    var ie = m_BatchIndexes[i];
                    m_BatchIndexes[i] = new SearchIndexEntry(ie.key, ie.crc, ie.type, ie.score, ie.docs.RemoveDocuments(removedDocIndexes));
                }

                // Remove the removedDocuments from the source documents. It is possible for some assets to get removed and then added back with the same
                // path and file content (for example removing and re-adding a package), in which case when fetching the changeset from the SearchMonitor
                // we check for the source document's existence and if the file hashes match to know if there needs to be an update. Therefore, we should definitely
                // remove the source document to avoid this kind of issue (UUM-66122).
                foreach (var removedDocument in documentsToRemove)
                    m_SourceDocuments.TryRemove(removedDocument, out _);

                if (executeCleanup)
                {
                    SearchNativeList<SearchIndexEntry> indexes = new SearchNativeList<SearchIndexEntry>(m_Indexes.Count, Allocator.Persistent);
                    CompressEntriesAndGenerateDocumentListTable(m_Indexes, indexes, new SearchIndexEntryExactComparer(), out var tempTable, out var tempTableHandle);
                    m_Indexes.Dispose();
                    m_Indexes = new SearchNativeReadOnlyArray<SearchIndexEntry>(indexes.ToReadOnly(), Allocator.Persistent);
                    if (m_DocumentListTableHandle.IsAllocated)
                        m_DocumentListTableHandle.Free();
                    m_DocumentListTableHandle = tempTableHandle;
                    m_DocumentListTable.Dispose();
                    m_DocumentListTable = tempTable;
                }
            }
        }

        public void CombineIndexes(IReadOnlyList<SearchIndexer> indexes, int baseScore, string indexName, SearchTask<TaskData> task)
        {
            int i = 0;
            var wiec = new SearchIndexComparer(SearchIndexOperator.DoNotCompareScore);
            var artifactCount = indexes.Count;
            m_Timestamp = DateTime.UtcNow.ToBinary();

            var entryEnumerables = new List<IReadOnlyList<SearchIndexEntry>>(indexes.Count);
            var updatedDocIndexes = new Dictionary<int, int>();
            task.Report("Analyzing artifacts...", 0.0f);
            var progressRate = artifactCount < 100000 ? 0 : artifactCount / 100;
            task.throttleProgressReport = progressRate > 0;
            task.throttleProgressRate = progressRate;
            var totalEntries = 0;
            foreach (var other in indexes)
            {
                if (other.storage is not SearchIndexEntryStorage sies)
                    throw new ArgumentException($"Only {nameof(SearchIndexEntryStorage)} is supported.", nameof(other.storage));

                if (sies.DocumentCount == 0)
                    continue;

                task.Report(++i, artifactCount);
                foreach (var od in sies.m_Documents)
                {
                    var currentDocIndex = FindDocumentIndex(od.Value);
                    if (currentDocIndex != -1)
                        updatedDocIndexes[od.Key] = currentDocIndex;
                }

                foreach (var od in sies.m_Documents)
                {
                    var sourceDoc = od.Value;
                    if (updatedDocIndexes.TryGetValue(od.Key, out var di))
                        m_Documents[di] = sourceDoc;
                    else
                    {
                        di = AddDocument(sourceDoc.id, sourceDoc.m_Name, sourceDoc.m_Source, false, sourceDoc.flags); ;
                        updatedDocIndexes[od.Key] = di;
                        other.AddProperty("a", indexName, indexName.Length, indexName.Length, baseScore, od.Key, saveKeyword: true, exact: true);
                    }
                }

                // Remap entries and add them to the batch
                sies.m_DocumentListTable.RemapDocuments(updatedDocIndexes);
                for (var ei = 0; ei < sies.m_Indexes.Count; ++ei)
                {
                    var e = sies.m_Indexes[ei];
                    sies.m_Indexes[ei] = new SearchIndexEntry(e.key, e.crc, e.type, baseScore + e.score, e.docs.RemapDocuments(updatedDocIndexes));
                }
                if (sies.m_Indexes.Count > 0)
                    entryEnumerables.Add(sies.m_Indexes);
                totalEntries += sies.m_Indexes.Count;

                // This should be very fast since there is not a lot of entries added in batchIndexes
                sies.m_BatchIndexes.Sort(wiec);
                for (var ei = 0; ei < sies.m_BatchIndexes.Count; ++ei)
                {
                    var e = sies.m_BatchIndexes[ei];
                    sies.m_BatchIndexes[ei] = new SearchIndexEntry(e.key, e.crc, e.type, baseScore + e.score, e.docs.RemapDocuments(updatedDocIndexes));
                }

                if (sies.m_BatchIndexes.Count > 0)
                    entryEnumerables.Add(sies.m_BatchIndexes);
                totalEntries += sies.m_BatchIndexes.Count;

                m_Keywords.UnionWith(sies.m_Keywords);
                foreach (var hkvp in sies.m_SourceDocuments)
                    m_SourceDocuments[hkvp.Key] = hkvp.Value;
                foreach (var mikvp in sies.m_MetaInfo)
                    m_MetaInfo[mikvp.Key] = mikvp.Value;

                updatedDocIndexes.Clear();
            }

            // Once everything is finished, sort and build the document table
            task.Report("Compressing entries...", 0f);
            progressRate = totalEntries < 100000 ? 0 : totalEntries / 100;
            task.throttleProgressReport = progressRate > 0;
            task.throttleProgressRate = progressRate;

            using var batchIndexes = new SearchNativeList<SearchIndexEntry>(artifactCount, Allocator.Persistent);
            CompressEntriesFromEnumeratorAndGenerateDocumentListTable(new SearchIndexEntryHeapEnumerator(entryEnumerables, wiec), m_Documents.Count, batchIndexes, wiec, out var documentListTable, out var documentListTableHandle, progress: progress => task.Report(progress, totalEntries));
            foreach (var enumerable in entryEnumerables)
            {
                if (enumerable is SearchNativeReadOnlyArray<SearchIndexEntry> { Allocator: Allocator.Temp } na)
                {
                    na.Dispose();
                }
            }
            m_Indexes.Dispose();
            m_Indexes = new SearchNativeReadOnlyArray<SearchIndexEntry>(batchIndexes.ToReadOnly(), Allocator.Persistent);
            if (m_DocumentListTableHandle.IsAllocated)
                m_DocumentListTableHandle.Free();
            m_DocumentListTableHandle = documentListTableHandle;
            m_DocumentListTable.Dispose();
            m_DocumentListTable = documentListTable;
            m_BatchIndexes.Clear();
            BuildDocumentIndexTable();
            m_IndexReady = true;
            task.throttleProgressReport = false;
        }

        public void Merge(string[] removedDocuments, SearchIndexer otherIndexer, int baseScore, Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task)
        {
            if (otherIndexer.storage is not SearchIndexEntryStorage other)
                throw new ArgumentException($"Only {nameof(SearchIndexEntryStorage)} is supported.", nameof(otherIndexer.storage));

            var indexes = new SearchNativeList<SearchIndexEntry>(m_Indexes.Count, Allocator.Persistent);
            var updatedDocIndexes = new Dictionary<int/*other*/, int/*current*/>();
            foreach (var od in other.m_Documents)
            {
                var currentDocIndex = FindDocumentIndex(od.Value);
                if (currentDocIndex != -1)
                    updatedDocIndexes[od.Key] = currentDocIndex;
            }

            SearchDocumentListTable tempTable = null; // Keep this alive until the end of Merge.
            GCHandle tempTableHandle = default;

            // We concatenate other.m_SourceDocument here because we want to remove all documents in m_Document that come from other.m_SourceDocument but are
            // not in other.m_Document. This can happen when a modification on an asset makes it index less documents, so other.m_Documents would contain less documents
            // than m_Documents. We consider that other contains the truth for those source documents, so if we have superfluous documents we have to remove them.
            // For example, consider a scene that has 3 objects (a cube, a capsule and a sphere). The scene is indexed. The current index contains 4 documents (scene + 3 objects)
            // and 1 source document. We remove the sphere from the scene. The new index will contain 3 documents (scene + 2 objects) and 1 source document. Since the scene is a
            // source document that is in both indexes, we need to remove the sphere document from the current index otherwise we will have a stale document.
            var removeDocIndexes = new HashSet<int>(removedDocuments.Concat(other.m_SourceDocuments.Keys).SelectMany(FindDocumentIndexesByPath));
            if (removeDocIndexes.Count > 0)
            {
                // We only call Except(updatedDocIndexes) for the document themselves, and not when removing the entries corresponding to all removed documents.
                // This is important otherwise entries for values that have changed would not get removed.
                foreach (var idi in removeDocIndexes.Except(updatedDocIndexes.Values))
                {
                    if (!m_Documents.TryRemove(idi, out var removedDoc) || !m_IndexByDocuments.TryRemove(removedDoc.id, out _))
                        Console.WriteLine($"[QS] Failed to remove search document {removedDoc.id} at {idi}");
                }

                lock (this)
                {
                    m_DocumentListTable.RemoveDocuments(removeDocIndexes);
                    for (var i = 0; i < m_Indexes.Count; i++)
                    {
                        var ie = m_Indexes[i];
                        m_Indexes[i] = new SearchIndexEntry(ie.key, ie.crc, ie.type, ie.score, ie.docs.RemoveDocuments(removeDocIndexes));
                    }
                    CompressEntriesAndGenerateDocumentListTable(m_Indexes, indexes, new SearchIndexEntryExactComparer(), out tempTable, out tempTableHandle);
                }
            }
            else
            {
                lock (this)
                    indexes.AddRange(m_Indexes);
            }

            // Remove the removedDocuments from the source documents. It is possible for some assets to get removed and then added back with the same
            // path and file content (for example removing and re-adding a package), in which case when fetching the changeset from the SearchMonitor
            // we check for the source document's existence and if the file hashes match to know if there needs to be an update. Therefore, we should definitely
            // remove the source document to avoid this kind of issue (UUM-66122).
            foreach (var removedDocument in removedDocuments)
            {
                m_SourceDocuments.TryRemove(removedDocument, out _);
            }

            var wiec = new SearchIndexComparer(SearchIndexOperator.DoNotCompareScore);
            var enumerables = new List<IReadOnlyList<SearchIndexEntry>>(3);
            var totalEntries = m_Indexes.Count;
            if (indexes.Count > 0)
                enumerables.Add(indexes);
            else
                indexes.Dispose();
            if (other.DocumentCount > 0)
            {
                var count = updatedDocIndexes.Count;
                foreach (var od in other.m_Documents)
                {
                    var sourceDoc = od.Value;
                    if (updatedDocIndexes.TryGetValue(od.Key, out var di))
                        m_Documents[di] = sourceDoc;
                    else
                    {
                        di = AddDocument(sourceDoc.id, sourceDoc.m_Name, sourceDoc.m_Source, false, sourceDoc.flags);
                        updatedDocIndexes[od.Key] = di;
                    }

                    documentIndexing?.Invoke(od.Key, otherIndexer, count);
                }

                var bi = 0;
                count = other.m_Indexes.Count + other.m_BatchIndexes.Count;
                if (task != null)
                {
                    task.Report("Analyzing new entries...", 0.0f);
                    var progressRate = count < 100000 ? 0 : count / 100;
                    task.throttleProgressReport = progressRate > 0;
                    task.throttleProgressRate = progressRate;
                }

                // Remap entries and add them to the batch
                // Here we need to know if the new entries exists in the current index. If they do, we need to
                // make sure the score remains the same no matter which entry survives the compression. If they don't
                // then we need to adjust the score with the index score.
                // TODO: Optimize this
                // TODO: Seriously, how is this situation really going to happen if we removed all entries from the new db's documents? There shouldn't be any entries that match?
                other.m_DocumentListTable.RemapDocuments(updatedDocIndexes);
                var tmpList = new SearchNativeList<SearchIndexEntry>(other.m_Indexes.Count, Allocator.Persistent);
                var tmpBatch = new SearchNativeList<SearchIndexEntry>(other.m_BatchIndexes.Count, Allocator.Persistent);
                foreach (var sie in other.m_Indexes)
                {
                    var insertAt = indexes.BinarySearch(sie, wiec);
                    if (insertAt < 0)
                    {
                        tmpList.Add(new SearchIndexEntry(sie.key, sie.crc, sie.type, baseScore + sie.score, sie.docs.RemapDocuments(updatedDocIndexes)));
                    }
                    else
                    {
                        var index = indexes[insertAt];
                        tmpList.Add(new SearchIndexEntry(sie.key, sie.crc, sie.type, index.score, sie.docs.RemapDocuments(updatedDocIndexes)));
                    }
                    task?.Report(++bi, count);
                }
                if (tmpList.Count > 0)
                    enumerables.Add(tmpList);
                else
                    tmpList.Dispose();
                totalEntries += tmpList.Count;

                tmpBatch.AddRange(other.m_BatchIndexes);
                tmpBatch.Sort(wiec);
                for (var i = 0; i < tmpBatch.Count; i++)
                {
                    var sie = tmpBatch[i];
                    var insertAt = indexes.BinarySearch(sie, wiec);
                    if (insertAt < 0)
                    {
                        tmpBatch[i] = new SearchIndexEntry(sie.key, sie.crc, sie.type, baseScore + sie.score, sie.docs.RemapDocuments(updatedDocIndexes));
                    }
                    else
                    {
                        var index = indexes[insertAt];
                        tmpBatch[i] = new SearchIndexEntry(sie.key, sie.crc, sie.type, index.score, sie.docs.RemapDocuments(updatedDocIndexes));
                    }
                    task?.Report(++bi, count);
                }
                if (tmpBatch.Count > 0)
                    enumerables.Add(tmpBatch);
                else
                    tmpBatch.Dispose();
                totalEntries += tmpBatch.Count;
            }

            lock (this)
            {
                if (task != null)
                {
                    task.Report("Compressing entries...", 0f);
                    var progressRate = totalEntries < 100000 ? 0 : totalEntries / 100;
                    task.throttleProgressReport = progressRate > 0;
                    task.throttleProgressRate = progressRate;
                }

                m_IndexReady = false;
                // Once everything is finished, sort and build the document table
                using var compressedEntries = new SearchNativeList<SearchIndexEntry>(indexes.Count + other.EntryCount, Allocator.Persistent);
                CompressEntriesFromEnumeratorAndGenerateDocumentListTable(new SearchIndexEntryHeapEnumerator(enumerables, wiec), m_Documents.Count, compressedEntries, wiec, out var documentListTable, out var documentListTableHandle, progress: progress => task?.Report(progress, totalEntries));
                m_Indexes.Dispose();
                m_Indexes = new SearchNativeReadOnlyArray<SearchIndexEntry>(compressedEntries.ToReadOnly(), Allocator.Persistent);
                if (m_DocumentListTableHandle.IsAllocated)
                    m_DocumentListTableHandle.Free();
                m_DocumentListTableHandle = documentListTableHandle;
                m_DocumentListTable.Dispose();
                m_DocumentListTable = documentListTable;
                m_Keywords.UnionWith(other.m_Keywords);
                foreach (var hkvp in other.m_SourceDocuments)
                    m_SourceDocuments[hkvp.Key] = hkvp.Value;
                foreach (var hkvp in other.m_SourceDocuments)
                    m_SourceDocuments[hkvp.Key] = hkvp.Value;
                foreach (var mikvp in other.m_MetaInfo)
                    m_MetaInfo[mikvp.Key] = mikvp.Value;
                m_Timestamp = DateTime.UtcNow.ToBinary();
                BuildDocumentIndexTable();
                m_IndexReady = true;

                if (tempTableHandle.IsAllocated)
                    tempTableHandle.Free();
                tempTable?.Dispose();

                foreach (var enumerable in enumerables)
                {
                    if (enumerable is SearchNativeList<SearchIndexEntry> snl)
                        snl.Dispose();
                }

                if (task != null)
                    task.throttleProgressReport = false;
            }
        }

        // If SearchIndexer.ApplyFrom ever comes public, remove GC.SuppressFinalize(source) and make sure
        // to copy the native data. Also, do not call Dispose on the SearchIndexer passed
        // to ApplyFrom.
        public void ApplyFrom(SearchIndexer source)
        {
            if (source.storage is not SearchIndexEntryStorage sies)
                throw new ArgumentException($"Only {nameof(SearchIndexEntryStorage)} is supported.", nameof(source.storage));

            lock (this)
            lock (source)
            {
                m_IndexReady = false;
                m_Timestamp = DateTime.UtcNow.ToBinary();
                m_Indexes.Dispose();
                m_Indexes = sies.m_Indexes;
                m_Documents = sies.m_Documents;
                m_Keywords = sies.m_Keywords;
                m_SourceDocuments = sies.m_SourceDocuments;
                m_MetaInfo = sies.m_MetaInfo;
                if (m_DocumentListTableHandle.IsAllocated)
                    m_DocumentListTableHandle.Free();
                m_DocumentListTableHandle = sies.m_DocumentListTableHandle;
                m_DocumentListTable.Dispose();
                m_DocumentListTable = sies.m_DocumentListTable;

                // Suppress finalizer for the source, because we now own
                // the native arrays.
                GC.SuppressFinalize(source);

                BuildDocumentIndexTable();

                m_BatchIndexes.Clear();
                m_IndexReady = true;
            }
        }

        public void Write(Stream stream)
        {
            lock (this)
            {
                // Build string table
                var st = new SearchStringTable();
                st.UnionWith(m_SourceDocuments.Keys);
                st.UnionWith(m_IndexByDocuments.Keys);
                st.UnionWith(m_MetaInfo.Keys);
                st.UnionWith(m_MetaInfo.Values);
                st.UnionWith(m_Documents.Select(d => d.Value.id));
                st.UnionWith(m_Documents.Select(d => d.Value.m_Name));
                st.UnionWith(m_Documents.Select(d => d.Value.m_Source));
                st.UnionWith(m_Keywords);

                using (var indexWriter = new BinaryWriter(stream, Encoding.Default, true))
                {
                    m_Timestamp = DateTime.UtcNow.ToBinary();

                    indexWriter.Write(SearchIndexEntry.version);
                    indexWriter.Write(m_Timestamp);

                    // Save string table
                    st.Write(indexWriter);

                    // Documents
                    indexWriter.Write(m_Documents.Count);
                    foreach (var doc in m_Documents)
                    {
                        indexWriter.Write(doc.Key);
                        indexWriter.Write(doc.Value.valid);
                        if (doc.Value.valid)
                        {
                            st.Write(indexWriter, doc.Value.id);
                            st.Write(indexWriter, doc.Value.m_Name);
                            st.Write(indexWriter, doc.Value.m_Source);
                            indexWriter.Write((int)doc.Value.flags);
                        }
                    }

                    // Hashes
                    indexWriter.Write(m_SourceDocuments.Count);
                    foreach (var kvp in m_SourceDocuments)
                    {
                        st.Write(indexWriter, kvp.Key);
                        indexWriter.Write(kvp.Value.contentHash.ToString());
                        indexWriter.Write(kvp.Value.structuralHash.ToString());
                    }

                    // Meta info
                    indexWriter.Write(m_MetaInfo.Count);
                    foreach (var kvp in m_MetaInfo)
                    {
                        st.Write(indexWriter, kvp.Key);
                        st.Write(indexWriter, kvp.Value);
                    }

                    // DocList table
                    m_DocumentListTable.ToBinary(indexWriter);

                    // Indexes
                    indexWriter.Write(m_Indexes.Count);
                    foreach (var p in m_Indexes)
                    {
                        indexWriter.Write(p.key);
                        indexWriter.Write(p.crc);
                        indexWriter.Write((byte)p.type);
                        indexWriter.Write(p.score);
                        p.docs.ToBinary(indexWriter);
                    }

                    // Keywords
                    indexWriter.Write(m_Keywords.Count);
                    foreach (var t in m_Keywords)
                        st.Write(indexWriter, t);
                }
            }
        }

        public bool Read(Stream stream, bool checkVersionOnly)
        {
            using (var indexReader = new BinaryReader(stream))
            {
                int version = indexReader.ReadInt32();
                if (version != SearchIndexEntry.version)
                    return false;

                if (checkVersionOnly)
                    return true;

                m_Timestamp = indexReader.ReadInt64();

                // Read string table
                var st = new SearchStringTable(indexReader);

                // Documents
                var elementCount = indexReader.ReadInt32();
                var documents = new ConcurrentDictionary<int, SearchDocument>();
                for (int i = 0; i < elementCount; ++i)
                {
                    var index = indexReader.ReadInt32();
                    var valid = indexReader.ReadBoolean();
                    if (!valid)
                        continue;

                    var docId = st.Read(indexReader);
                    var docName = st.Read(indexReader);
                    var docSource = st.Read(indexReader);
                    var docFlags = (SearchDocumentFlags)indexReader.ReadInt32();
                    documents[index] = new SearchDocument(docId, string.IsNullOrEmpty(docName) ? null : docName, string.IsNullOrEmpty(docSource) ? null : docSource, int.MaxValue, docFlags);
                }

                // Hashes (m_SourceDocuments)
                elementCount = indexReader.ReadInt32();
                var documentDescriptors = new ConcurrentDictionary<string, DocumentContentDescriptor>();
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = st.Read(indexReader);
                    var contentHash = Hash128.Parse(indexReader.ReadString());
                    var structuralHash = Hash128.Parse(indexReader.ReadString());
                    documentDescriptors[key] = new DocumentContentDescriptor { contentHash = contentHash, structuralHash = structuralHash };
                }

                // Meta infos
                elementCount = indexReader.ReadInt32();
                var metainfos = new ConcurrentDictionary<string, string>();
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = st.Read(indexReader);
                    var value = st.Read(indexReader);
                    metainfos[key] = value;
                }

                // DocList table
                var documentListTable = SearchDocumentListTable.FromBinary(indexReader);
                var handle = GCHandle.Alloc(documentListTable, GCHandleType.Pinned);

                // Indexes
                elementCount = indexReader.ReadInt32();
                var indexes = new SearchNativeReadOnlyArray<SearchIndexEntry>(elementCount, Allocator.Persistent);
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = indexReader.ReadInt64();
                    var crc = indexReader.ReadInt32();
                    var type = (SearchIndexEntry.Type)indexReader.ReadByte();
                    var score = indexReader.ReadInt32();
                    var documentList = SearchDocumentList.FromBinary(indexReader, documentListTable, handle);

                    indexes[i] = (new SearchIndexEntry(key, crc, type, score, documentList));
                }

                // Keywords
                elementCount = indexReader.ReadInt32();
                var keywords = new string[elementCount];
                for (int i = 0; i < elementCount; ++i)
                    keywords[i] = st.Read(indexReader);

                // No need to sort the index, it is already sorted in the file stream.
                lock (this)
                {
                    ApplyIndexes(documents, indexes, documentListTable, handle, documentDescriptors, metainfos);
                    m_Keywords = new HashSet<string>(keywords);
                }

                return true;
            }
        }

        public IEnumerable<SearchResult> SearchTerm(string name, object value, SearchIndexOperator op, bool exclude, SearchResultCollection subset)
        {
            // TODO: This is wrong. property!=value does not mean the same thing as all documents excluding those that match property=value,
            // because the exclusion will return documents that do not contain that property.
            if (op == SearchIndexOperator.NotEqual)
            {
                exclude = true;
                op = SearchIndexOperator.Equal;
            }

            IEnumerable<SearchResult> matches = null;
            if (!string.IsNullOrEmpty(name))
            {
                name = name.ToLowerInvariant();

                // Search property
                double number;
                if (value is float f)
                {
                    number = f;
                    matches = SearchNumber(name, number, op, subset);
                }
                else if (value is double d)
                {
                    number = d;
                    matches = SearchNumber(name, number, op, subset);
                }
                else if (value is string)
                {
                    var valueString = (string)value;
                    if (Utils.TryParse(valueString, out number, false))
                    {
                        if (!exclude && op != SearchIndexOperator.NotEqual)
                            matches = SearchNumber(name, number, op, subset);
                        else
                            matches = ExcludeNumber(name, number, op, subset);
                    }
                    else
                    {
                        if (!exclude)
                            matches = SearchProperty(name, valueString.ToLowerInvariant(), op, subset);
                        else
                            matches = ExcludeProperty(name, valueString.ToLowerInvariant(), op, subset);
                    }
                }
                else
                    throw new ArgumentException($"value must be a number or a string", nameof(value));
            }
            else if (value is string word)
            {
                // Search word
                // TODO: Why are we not calling ToLowerInvariant on "word"? Everywhere else we call it on the value.
                if (!exclude)
                    matches = SearchWord(word, op, subset).Concat(SearchDocumentWords(word, subset).Where(r => r.valid));
                else
                    matches = ExcludeWord(word, op, subset);
            }
            else
                throw new ArgumentException($"word value must be a string", nameof(value));

            if (matches == null)
                return null;

            return matches.Select(r =>
            {
                var id = r.id ?? (m_Documents.TryGetValue(r.index, out var doc) ? doc.id : r.id);
                var index = id == null ? -1 : r.index;
                return new SearchResult(id, index, r.score);
            });
        }

        IEnumerable<SearchResult> SearchWord(string word, SearchIndexOperator op, SearchResultCollection subset)
        {
            var comparer = new SearchIndexComparer(op);
            int crc = word.Length;
            if (op == SearchIndexOperator.Equal)
                crc = int.MaxValue;

            foreach (var result in SearchIndexes(HashingUtils.GetHashCode(word), crc, SearchIndexEntry.Type.Word, comparer, subset))
            {
                yield return result;
            }

            if (ExtraSearchWordHandler != null)
            {
                foreach (var result in ExtraSearchWordHandler(word, op, subset))
                {
                    yield return result;
                }
            }
        }

        static bool ExcludeWordVariations(string word)
        {
            if (word == "true" || word == "false")
                return true;
            return false;
        }

        void MapKeyword(string keyword, string help)
        {
            // TODO: This does not work for property:value, only property:
            // Is this what we want? Also, once keyword with help has been added it cannot be removed.
            m_Keywords.Remove(keyword);
            m_Keywords.Add($"{keyword}|{help}");
        }

        int FindDocumentIndex(SearchDocument doc)
        {
            if (m_IndexByDocuments.TryGetValue(doc.id, out var di))
            {
                var fdoc = m_Documents[di];
                if (string.Equals(fdoc.m_Name ?? fdoc.m_Source, doc.m_Name ?? doc.m_Source, StringComparison.Ordinal))
                    return di;
            }
            return InvalidDocumentIndex;
        }

        IEnumerable<int> FindDocumentIndexesByPath(string path)
        {
            foreach (var kvp in m_Documents)
            {
                var d = kvp.Value;
                if (!d.valid)
                    continue;

                if (d.m_Source != null && string.CompareOrdinal(d.m_Source, path) == 0)
                    yield return kvp.Key;
                else if (string.CompareOrdinal(d.id, path) == 0)
                    yield return kvp.Key;
            }
        }

        void ApplyIndexes(ConcurrentDictionary<int, SearchDocument> documents, SearchNativeReadOnlyArray<SearchIndexEntry> entries, SearchDocumentListTable documentListTable, GCHandle documentListTableHandle, ConcurrentDictionary<string, DocumentContentDescriptor> docDescriptors, ConcurrentDictionary<string, string> metainfos)
        {
            m_Documents = documents;
            m_SourceDocuments = docDescriptors;
            m_MetaInfo = metainfos;
            m_Indexes.Dispose();
            m_Indexes = entries;
            if (m_DocumentListTableHandle.IsAllocated)
                m_DocumentListTableHandle.Free();
            m_DocumentListTableHandle = documentListTableHandle;
            m_DocumentListTable.Dispose();
            m_DocumentListTable = documentListTable;
            BuildDocumentIndexTable();
            m_IndexReady = true;
        }

        void BuildDocumentIndexTable()
        {
            m_IndexByDocuments.Clear();
            m_NextDocumentIndex = -1;
            foreach (var kvp in m_Documents)
            {
                if (kvp.Value.valid)
                    m_IndexByDocuments[kvp.Value.id] = kvp.Key;
                if (kvp.Key > m_NextDocumentIndex)
                    m_NextDocumentIndex = kvp.Key;
            }
            m_AllDocumentIndexes?.Clear();
            m_AllDocumentIndexes = null;
            Interlocked.Increment(ref m_NextDocumentIndex);
        }

        bool UpdateIndexesOnFinish()
        {
            lock (this)
            {
                m_IndexReady = false;
                try
                {
                    var comparer = new SearchIndexComparer();
                    var exactComparer = new SearchIndexEntryExactComparer();
                    m_BatchIndexes.Sort(comparer);
                    using var compressedEntries = new SearchNativeList<SearchIndexEntry>(m_Indexes.Count, Allocator.Persistent);
                    if (m_Indexes.Count > 0)
                    {
                        var enumerables = new List<IReadOnlyList<SearchIndexEntry>>() { m_Indexes };
                        if (m_BatchIndexes.Count > 0)
                            enumerables.Add(m_BatchIndexes);
                        CompressEntriesFromEnumeratorAndGenerateDocumentListTable(new SearchIndexEntryHeapEnumerator(enumerables, exactComparer), m_Documents.Count, compressedEntries, exactComparer, out var documentListTable, out var documentListTableHandle);
                        if (m_DocumentListTableHandle.IsAllocated)
                            m_DocumentListTableHandle.Free();
                        m_DocumentListTableHandle = documentListTableHandle;
                        m_DocumentListTable.Dispose();
                        m_DocumentListTable = documentListTable;
                        foreach (var enumerable in enumerables)
                        {
                            if (enumerable is SearchNativeReadOnlyArray<SearchIndexEntry> { Allocator: Allocator.Temp } na)
                                na.Dispose();
                        }
                    }
                    else
                    {
                        CompressEntriesAndGenerateDocumentListTable(m_BatchIndexes, compressedEntries, exactComparer, out var documentListTable, out var documentListTableHandle);
                        if (m_DocumentListTableHandle.IsAllocated)
                            m_DocumentListTableHandle.Free();
                        m_DocumentListTableHandle = documentListTableHandle;
                        m_DocumentListTable.Dispose();
                        m_DocumentListTable = documentListTable;
                    }
                    m_Indexes.Dispose();
                    m_Indexes = new SearchNativeReadOnlyArray<SearchIndexEntry>(compressedEntries.ToReadOnly(), Allocator.Persistent);
                    BuildDocumentIndexTable();
                    m_IndexReady = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to update index:" + ex.ToString());
                    // This can happen while a domain reload is happening.
                    return false;
                }

                return true;
            }
        }

        // This method assumes entries are sorted.
        // Merge and Combine needs SearchIndexComparer with number compare to compress numbers that are similar.
        static void CompressEntriesAndGenerateDocumentListTable(IReadOnlyList<SearchIndexEntry> sortedEntries, SearchNativeList<SearchIndexEntry> outCompressedEntries, IComparer<SearchIndexEntry> comparer, out SearchDocumentListTable documentListTable, out GCHandle documentListTableHandle, Action<int> progress = null)
        {
            documentListTable = new SearchDocumentListTable(doCreate: false);
            documentListTableHandle = default;

            if (sortedEntries.Count == 0)
                return;

            var le = sortedEntries[0];
            var nbDocList = 1;
            var totalDocRefs = le.docs.Count;
            var currentDocRefs = le.docs.Count;
            for (var i = 1; i < sortedEntries.Count; ++i)
            {
                var e = sortedEntries[i];
                currentDocRefs += e.docs.Count;
                if (comparer.Compare(le, e) != 0)
                {
                    // Only take into account those that are > 4, since only those will go into the table
                    if (currentDocRefs > 4)
                    {
                        totalDocRefs += currentDocRefs;
                        ++nbDocList;
                    }

                    currentDocRefs = 0;
                    le = e;
                }
            }

            documentListTable.Create(nbDocList, totalDocRefs / nbDocList);
            documentListTableHandle = GCHandle.Alloc(documentListTable, GCHandleType.Pinned);
            var enumerator = sortedEntries.GetEnumerator();
            enumerator.MoveNext();
            CompressEntriesFromEnumerator(enumerator, outCompressedEntries, comparer, documentListTable, documentListTableHandle, progress);
        }

        static void CompressEntriesFromEnumeratorAndGenerateDocumentListTable(IEnumerator<SearchIndexEntry> sortedEntriesEnumerator, int nbDocuments, SearchNativeList<SearchIndexEntry> outCompressedEntries, IComparer<SearchIndexEntry> comparer, out SearchDocumentListTable documentListTable, out GCHandle documentListTableHandle, Action<int> progress = null)
        {
            documentListTable = new SearchDocumentListTable(doCreate: false);
            documentListTableHandle = default;

            if (!sortedEntriesEnumerator.MoveNext())
                return;

            // We found that on average, we have about the same number of unique document lists
            // as the number of documents, with the majority of them being from size 5 to 20, and few of bigger size.
            // Using an average of 10 seems to give a good starting point.
            documentListTable.Create(nbDocuments, 10);
            documentListTableHandle = GCHandle.Alloc(documentListTable, GCHandleType.Pinned);
            CompressEntriesFromEnumerator(sortedEntriesEnumerator, outCompressedEntries, comparer, documentListTable, documentListTableHandle, progress);
        }

        // If you call this anywhere else other than the other Compress methods, make sure that MoveNext is called once on the enumerator
        // to verify that it contains some values.
        static void CompressEntriesFromEnumerator(IEnumerator<SearchIndexEntry> sortedEntriesEnumerator, SearchNativeList<SearchIndexEntry> outCompressedEntries, IComparer<SearchIndexEntry> comparer, SearchDocumentListTable documentListTable, GCHandle documentListTableHandle, Action<int> progress = null)
        {
            var currentEntry = 1;
            var sortedSet = new SortedSet<int>();
            var le = sortedEntriesEnumerator.Current;
            le.docs.ToDocumentCollection(sortedSet);
            while (sortedEntriesEnumerator.MoveNext())
            {
                progress?.Invoke(++currentEntry);
                var e = sortedEntriesEnumerator.Current;
                if (comparer.Compare(le, e) != 0)
                {
                    if (sortedSet.Count > 0)
                        outCompressedEntries.Add(new SearchIndexEntry(le.key, le.crc, le.type, le.score, SearchDocumentList.FromDocumentCollection(sortedSet, documentListTable, documentListTableHandle)));
                    sortedSet.Clear();
                    le = e;
                }
                e.docs.ToDocumentCollection(sortedSet);
            }

            // Last batch
            if (sortedSet.Count > 0)
                outCompressedEntries.Add(new SearchIndexEntry(le.key, le.crc, le.type, le.score, SearchDocumentList.FromDocumentCollection(sortedSet, documentListTable, documentListTableHandle)));
        }

        IEnumerable<SearchResult> SearchNumber(string key, double value, SearchIndexOperator op, SearchResultCollection subset)
        {
            var wiec = new SearchIndexComparer(op);
            return SearchIndexes(BitConverter.DoubleToInt64Bits(value), HashingUtils.GetHashCode(key), SearchIndexEntry.Type.Number, wiec, subset);
        }

        IEnumerable<SearchResult> SearchProperty(string name, string value, SearchIndexOperator op, SearchResultCollection subset)
        {
            var comparer = new SearchIndexComparer(op);
            var valueHash = HashingUtils.GetHashCode(value);
            var nameHash = HashingUtils.GetHashCode(name);
            if (comparer.op == SearchIndexOperator.Equal)
            {
                nameHash ^= HashingUtils.GetHashCode(name.Length);
                valueHash ^= HashingUtils.GetHashCode(value.Length);
            }

            return SearchIndexes(valueHash, nameHash, SearchIndexEntry.Type.Property, comparer, subset);
        }

        IEnumerable<SearchResult> ExcludeWord(string word, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchWord(word, op, null));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        IEnumerable<SearchResult> ExcludeProperty(string name, string value, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchProperty(name, value, op, null));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        IEnumerable<SearchResult> ExcludeNumber(string name, double number, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchNumber(name, number, op, null).Select(m => new SearchResult(m.index, m.score)));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        SearchResultCollection GetAllDocumentIndexesSet()
        {
            if (m_AllDocumentIndexes != null)
                return m_AllDocumentIndexes;
            m_AllDocumentIndexes = new SearchResultCollection();
            // TODO: If you remove a document, this no longer works...
            for (int i = 0; i < DocumentCount; ++i)
                m_AllDocumentIndexes.Add(new SearchResult(i, 0));
            return m_AllDocumentIndexes;
        }

        IEnumerable<SearchResult> SearchDocumentWords(string word, SearchResultCollection subset)
        {
            if (ResolveDocumentHandler == null)
                yield break;
            foreach (var r in subset ?? GetAllDocumentIndexesSet())
            {
                var doc = GetDocument(r.index);
                if (doc.valid)
                {
                    var resolvedDocumentString = ResolveDocumentHandler(doc.id);
                    if (!string.IsNullOrEmpty(resolvedDocumentString))
                    {
                        if (resolvedDocumentString.IndexOf(word, StringComparison.Ordinal) == -1)
                            yield return SearchResult.nil;
                        else
                            yield return new SearchResult(r.index);
                    }
                    else
                        yield return SearchResult.nil;
                }
                else
                    yield return SearchResult.nil;
            }
        }

        IEnumerable<SearchResult> SearchIndexes(
            long key, int crc, SearchIndexEntry.Type type, SearchIndexComparer comparer, SearchResultCollection subset)
        {
            if (subset != null && subset.Count == 0)
                return Enumerable.Empty<SearchResult>();

            // Find a first match in the sorted indexes.
            var matchKey = new SearchIndexEntry(key, crc, type);
            int foundIndex = m_Indexes.AsReadOnlySpan().BinarySearch(matchKey, comparer);
            return SearchRange(foundIndex, matchKey, comparer, subset);
        }

        private IEnumerable<SearchResult> SearchRange(int foundIndex, SearchIndexEntry term, SearchIndexComparer comparer, SearchResultCollection subset)
        {
            if (foundIndex < 0)
            {
                // Potential range insertion, only used for not exact matches
                foundIndex = (-foundIndex) - 1;

                if ((comparer.op == SearchIndexOperator.Less || comparer.op == SearchIndexOperator.LessOrEqual) && foundIndex > 0)
                {
                    foundIndex--;
                }
            }

            // Rewind to first element
            while (Lower(ref foundIndex, term, comparer.op))
                ;

            if (!IsIndexValid(foundIndex, term.crc, term.type))
                yield break;

            bool findAll = subset == null;
            do
            {
                var fi = m_Indexes[foundIndex];
                if (fi.docs.Count <= 0)
                    continue;

                if (fi.docs.Count <= 4)
                {
                    var enumerator = fi.docs.GetEmbeddedEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var di = enumerator.Current;
                        if (GetRangeResult(term, di, fi.score, foundIndex, subset, findAll, out var sr))
                            yield return sr;
                    }
                }
                else
                {
                    var enumerator = fi.docs.GetTableEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var di = enumerator.Current;
                        if (GetRangeResult(term, di, fi.score, foundIndex, subset, findAll, out var sr))
                            yield return sr;
                    }
                }

                // Advance to last matching element
            }
            while (Upper(ref foundIndex, term, comparer.op));
        }

        bool GetRangeResult(SearchIndexEntry term, int doc, int score, int foundIndex, SearchResultCollection subset, bool findAll, out SearchResult result)
        {
            result = new SearchResult(doc, score);
            bool intersects = findAll || subset.Contains(result);
            if (intersects)
            {
                if (term.type == SearchIndexEntry.Type.Number)
                    result = new SearchResult(result.index, result.score + (int)Math.Abs(term.number - m_Indexes[foundIndex].number));
                return true;
            }

            return false;
        }

        private bool Rewind(int foundIndex, in SearchIndexEntry term, SearchIndexOperator op)
        {
            if (foundIndex <= 0)
                return false;

            var prevEntry = m_Indexes[foundIndex - 1];
            if (prevEntry.crc != term.crc || prevEntry.type != term.type)
                return false;

            if (term.type == SearchIndexEntry.Type.Number)
                return Utils.NumberCompare(op, prevEntry.number, term.number);

            return prevEntry.key == term.key;
        }

        private bool Advance(int foundIndex, in SearchIndexEntry term, SearchIndexOperator op)
        {
            if (foundIndex < 0 || foundIndex >= m_Indexes.Count ||
                m_Indexes[foundIndex].crc != term.crc || m_Indexes[foundIndex].type != term.type)
                return false;

            if (term.type == SearchIndexEntry.Type.Number)
                return Utils.NumberCompare(op, m_Indexes[foundIndex].number, term.number);

            return m_Indexes[foundIndex].key == term.key;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool Lower(ref int foundIndex, in SearchIndexEntry term, SearchIndexOperator op)
        {
            if (op == SearchIndexOperator.Less)
            {
                var cont = !Advance(foundIndex, term, op);
                if (cont)
                    foundIndex--;
                return IsIndexValid(foundIndex, term.crc, term.type) && cont;
            }
            else if (op == SearchIndexOperator.LessOrEqual)
            {
                if (!Advance(foundIndex, term, op))
                {
                    foundIndex = -1;
                    return false;
                }

                var cont = Advance(foundIndex + 1, term, op);
                if (cont)
                    foundIndex++;
                return IsIndexValid(foundIndex, term.crc, term.type) && cont;
            }
            else if (op == SearchIndexOperator.Greater)
            {
                var cont = !Advance(foundIndex, term, op);
                if (cont)
                    foundIndex++;
                return IsIndexValid(foundIndex, term.crc, term.type) && cont;
            }
            else if (op == SearchIndexOperator.Equal || op == SearchIndexOperator.Contains)
            {
                if (!IsIndexValid(foundIndex, term.crc, term.type))
                {
                    var cont = Rewind(foundIndex, term, op);
                    if (cont)
                    {
                        foundIndex--;
                        return true;
                    }
                    else
                    {
                        foundIndex = -1;
                        return false;
                    }
                }
                if (!Advance(foundIndex, term, op) && !Rewind(foundIndex, term, op))
                {
                    foundIndex = -1;
                    return false;
                }
            }

            {
                var cont = Rewind(foundIndex, term, op);
                if (cont)
                    foundIndex--;
                return cont;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool Upper(ref int foundIndex, in SearchIndexEntry term, SearchIndexOperator op)
        {
            if (op == SearchIndexOperator.Less || op == SearchIndexOperator.LessOrEqual)
            {
                var cont = Rewind(foundIndex, term, op);
                if (cont)
                    foundIndex--;
                return IsIndexValid(foundIndex, term.crc, term.type) && cont;
            }

            return Advance(++foundIndex, term, op);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool IsIndexValid(int foundIndex, long crc, SearchIndexEntry.Type type)
        {
            return foundIndex >= 0 && foundIndex < m_Indexes.Count && m_Indexes[foundIndex].crc == crc && m_Indexes[foundIndex].type == type;
        }
    }
}
