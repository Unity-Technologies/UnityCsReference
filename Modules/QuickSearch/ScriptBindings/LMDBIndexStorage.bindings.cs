// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    [NativeHeader("Modules/QuickSearch/LMDB/LMDBIndexStorage.h")]
    [NativeHeader("Modules/QuickSearch/LMDB/LMDBIndexStorageBindings.h")]
    sealed class LMDBIndexStorage : ISearchIndexerStorage, IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(LMDBIndexStorage storage) => storage.m_Ptr;
        }

        IntPtr m_Ptr;

        public static extern int DefaultVersion
        {
            [FreeFunction("LMDBIndexStorageBindings::GetDefaultVersion", IsThreadSafe = true)]
            get;
        }

        public extern int Version
        {
            [NativeMethod("GetVersion", IsThreadSafe = true)]
            get;

            [NativeMethod("SetVersion", IsThreadSafe = true)]
            set;
        }

        public extern long Timestamp
        {
            [NativeMethod("GetTimestamp", IsThreadSafe = true)]
            get;

            [NativeMethod("SetTimestamp", IsThreadSafe = true)]
            set;
        }

        public int KeywordCount => (int)Internal_GetKeywordCount();
        public int DocumentCount => (int)Internal_GetDocumentCount();
        public int EntryCount => (int)Internal_GetEntryCount();

        public extern ulong MapSize
        {
            [NativeMethod("GetMapSize", IsThreadSafe = true)]
            get;
        }

        public Func<string, string> ResolveDocumentHandler { get; set; }
        public Func<string, SearchIndexOperator, SearchResultCollection, IEnumerable<SearchResult>> ExtraSearchWordHandler { get; set; }

        public bool IsCreated => m_Ptr != IntPtr.Zero;

        public LMDBIndexStorage(string filePath, ulong initialSize = 1UL << 21)
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), filePath, initialSize);
        }

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
            if (m_Ptr != IntPtr.Zero)
            {
                Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        public IEnumerable<string> GetKeywords()
        {
            return Internal_GetKeywords();
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern void SetMetaInfo(string documentId, string metadata);

        [NativeMethod(IsThreadSafe = true)]
        public extern string GetMetaInfo(string documentId);

        public IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls)
        {
            return Internal_GetDocuments(ignoreNulls);
        }

        public IEnumerable<int> GetDocumentIndexes()
        {
            return Internal_GetDocumentIndexes();
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern SearchDocument GetDocument(int index);

        [NativeMethod(IsThreadSafe = true)]
        public extern void Clear();

        [NativeMethod(IsThreadSafe = true)]
        public extern int FindDocumentIndex(string documentId);

        [NativeMethod(IsThreadSafe = true)]
        public extern int AddDocument(string documentId, string name, string source, bool checkIfExists, SearchDocumentFlags flags);

        [NativeMethod(IsThreadSafe = true)]
        public extern void AddSourceDocument(string sourceDocumentPath, Hash128 documentHash);

        [NativeMethod(IsThreadSafe = true)]
        public extern bool TryGetSourceDocument(string sourceDocumentPath, out Hash128 documentHash);

        [NativeMethod(IsThreadSafe = true)]
        public extern void AddTypeStructuralVersion(string sourceDocumentPath, Hash128 typeStructuralVersion);

        [NativeMethod(IsThreadSafe = true)]
        public extern bool TryGetTypeStructuralVersion(string sourceDocumentPath, out Hash128 typeStructuralVersion);

        [NativeMethod(IsThreadSafe = true)]
        public extern void RemoveDocuments(string[] documentsToRemove, SearchCancellationToken cancellationToken);

        void ISearchIndexerStorage.RemoveDocuments(string[] documentsToRemove)
        {
            RemoveDocuments(documentsToRemove, SearchCancellationToken.None);
        }

        public void AddWord(string word, int score, int documentIndex)
        {
            Internal_AddWord(word, score, documentIndex);
        }

        public void AddProperty(string name, double value, int score, int documentIndex)
        {
            Internal_AddPropertyDouble(name, value, score, documentIndex);
        }

        public void AddProperty(string name, string value, int score, int documentIndex, bool saveKeyword)
        {
            Internal_AddPropertyString(name, value, score, documentIndex, saveKeyword);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern void MapProperty(int documentIndex, string name, string label, string help, string propertyType, string ownerTypeName, SearchPropositionGenerationOptions propositionGenerationOptions, bool removeNestedKeys);

        public void Start(bool clear)
        {
            if (clear)
                Clear();
        }

        public void Finish(string[] removedDocuments)
        {
            if (removedDocuments?.Length > 0)
                RemoveDocuments(removedDocuments, SearchCancellationToken.None);
            Timestamp = DateTime.UtcNow.ToBinary();
            Version = DefaultVersion;
            Sync();
        }

        public void CombineIndexes(IReadOnlyList<SearchIndexer> indexes, int baseScore, string indexName, SearchTask<TaskData> task)
        {
            // Make sure to clear the current content, then merge the content of the other indexes.
            Clear();

            var otherStorageArray = ArrayPool<IntPtr>.Shared.Rent(indexes.Count);
            for (int i = 0; i < indexes.Count; ++i)
            {
                var other = indexes[i];
                if (other.storage is LMDBIndexStorage lmdbStorage)
                    otherStorageArray[i] = lmdbStorage.m_Ptr;
                else
                    otherStorageArray[i] = IntPtr.Zero;
            }
            var progressId = (task?.async ?? false) ? task.progressId : Progress.InvalidProgressId;
            using var cancellationToken = task != null ? new SearchCancellationToken(task.cancellationToken) : SearchCancellationToken.None;
            Internal_Merge(m_Ptr, Array.Empty<string>(), otherStorageArray.AsSpan(0, indexes.Count), baseScore, progressId, cancellationToken);
        }

        public void Merge(string[] removedDocuments, SearchIndexer other, int baseScore, Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task)
        {
            var otherStorageArray = ArrayPool<IntPtr>.Shared.Rent(1);
            if (other.storage is LMDBIndexStorage lmdbStorage)
                otherStorageArray[0] = lmdbStorage.m_Ptr;

            if (documentIndexing != null)
            {
                var otherDocumentCount = other.documentCount;
                var otherDocumentIndexes = other.storage.GetDocumentIndexes();
                foreach (var documentIndex in otherDocumentIndexes)
                {
                    documentIndexing(documentIndex, other, otherDocumentCount);
                }
            }
            var progressId = (task?.async ?? false) ? task.progressId : Progress.InvalidProgressId;
            using var cancellationToken = task != null ? new SearchCancellationToken(task.cancellationToken) : SearchCancellationToken.None;
            Internal_Merge(m_Ptr, removedDocuments, otherStorageArray.AsSpan(0, 1), baseScore, progressId, cancellationToken);
        }

        public void Merge(string[] removedDocuments, ReadOnlySpan<SearchIndexer> others, int baseScore, Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task)
        {
            var otherStorageArray = ArrayPool<IntPtr>.Shared.Rent(others.Length);
            for (int i = 0; i < others.Length; ++i)
            {
                var other = others[i];
                if (other.storage is LMDBIndexStorage lmdbStorage)
                    otherStorageArray[i] = lmdbStorage.m_Ptr;
                else
                    otherStorageArray[i] = IntPtr.Zero;

                if (documentIndexing != null)
                {
                    var otherDocumentCount = other.documentCount;
                    var otherDocumentIndexes = other.storage.GetDocumentIndexes();
                    foreach (var documentIndex in otherDocumentIndexes)
                    {
                        documentIndexing(documentIndex, other, otherDocumentCount);
                    }
                }
            }

            var progressId = (task?.async ?? false) ? task.progressId : Progress.InvalidProgressId;
            using var cancellationToken = task != null ? new SearchCancellationToken(task.cancellationToken) : SearchCancellationToken.None;
            Internal_Merge(m_Ptr, removedDocuments, otherStorageArray.AsSpan(0, others.Length), baseScore, progressId, cancellationToken);
        }

        public void MergeArtifacts(string[] removedDocuments, string[] artifactPaths, int baseScore, SearchTask<TaskData> task)
        {
            var progressId = (task?.async ?? false) ? task.progressId : Progress.InvalidProgressId;
            using var cancellationToken = task != null ? new SearchCancellationToken(task.cancellationToken) : SearchCancellationToken.None;
            Internal_MergeArtifacts(m_Ptr, removedDocuments, artifactPaths, baseScore, progressId, cancellationToken);
        }

        public void MergeArtifactsBatch(string[] removedDocuments, string[] artifactPaths, int baseScore, long batchSize, SearchTask<TaskData> task)
        {
            var progressId = (task?.async ?? false) ? task.progressId : Progress.InvalidProgressId;
            using var cancellationToken = task != null ? new SearchCancellationToken(task.cancellationToken) : SearchCancellationToken.None;
            Internal_MergeArtifactsBatch(m_Ptr, removedDocuments, artifactPaths, baseScore, progressId, batchSize, cancellationToken);
        }

        public void MergeArtifactImportDataBatch(string[] removedDocuments, in SearchIndexArtifactImportData.Batch artifactImportDataBatch, int baseScore, long batchSize, SearchTask<TaskData> task)
        {
            var progressId = (task?.async ?? false) ? task.progressId : Progress.InvalidProgressId;
            using var cancellationToken = task != null ? new SearchCancellationToken(task.cancellationToken) : SearchCancellationToken.None;
            Internal_MergeArtifactImportDataBatch(m_Ptr, removedDocuments, artifactImportDataBatch.ImporterHashCodes, artifactImportDataBatch.ArtifactKeys, artifactImportDataBatch.ImportResultIds, baseScore, progressId, batchSize, cancellationToken);
        }

        public void Write(Stream stream)
        {
            stream.Write(SaveToArtifactBytes());
        }

        public bool Read(Stream stream, bool checkVersionOnly)
        {
            if (stream == null || stream.Length == 0)
                return false;

            var bytes = new byte[stream.Length];
            if (stream.Read(bytes, 0, bytes.Length) != bytes.Length)
                return false;

            return LoadFromArtifactBytes(bytes, checkVersionOnly);
        }

        public IEnumerable<SearchResult> SearchTerm(string name, object value, SearchIndexOperator op, bool exclude, SearchResultCollection subset)
        {
            return FilterResults(SearchTerm(name, value, op, subset), exclude, subset);
        }

        [NativeMethod(IsThreadSafe = false)]
        public extern void StartGlobalTransaction_Unsafe();

        [NativeMethod(IsThreadSafe = false)]
        public extern bool StopGlobalTransaction_Unsafe(LMDBTransactionEndAction endAction);

        [NativeMethod(IsThreadSafe = true)]
        public extern void PrintAllContent();

        IEnumerable<SearchResult> SearchTerm(string name, object value, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // EvaluateSearchNode can send us a value that is already converted to a numerical value.
                if (value is float f)
                {
                    return SearchPropertyDouble(name, f, op);
                }
                else if (value is double d)
                {
                    return SearchPropertyDouble(name, d, op);
                }
                else if (value is string s)
                {
                    if (Utils.TryParse(s, out double number, false))
                    {
                        return SearchPropertyDouble(name, number, op);
                    }
                    else
                    {
                        return SearchPropertyString(name, s, op);
                    }
                }
                else
                    throw new ArgumentException($"value must be a number or a string", nameof(value));
            }
            else if (value is string word)
            {
                // Search words
                var allMatches = new List<SearchResult>();
                allMatches.AddRange(SearchWord(word, op));
                if (ExtraSearchWordHandler != null)
                    allMatches.AddRange(ExtraSearchWordHandler(word, op, subset));
                SearchDocumentWords(word, subset, allMatches);
                return allMatches;
            }
            else
                throw new ArgumentException($"word value must be a string", nameof(value));
        }

        IEnumerable<SearchResult> FilterResults(IEnumerable<SearchResult> results, bool exclude, SearchResultCollection subset)
        {
            if (exclude)
            {
                if (subset != null)
                {
                    subset.ExceptWith(results);
                    return subset;
                }

                var allDocResults = Internal_GetDocumentsAsResults(true);
                var filteredResults = new SearchResultCollection(allDocResults);
                filteredResults.ExceptWith(results);
                return filteredResults;
            }
            else
            {
                if (subset == null)
                    return results;

                subset.IntersectWith(results);
                return subset;
            }
        }

        void SearchDocumentWords(string word, SearchResultCollection subset, List<SearchResult> results)
        {
            if (ResolveDocumentHandler == null)
                return;

            if (subset != null)
            {
                foreach (var r in subset)
                {
                    var docId = r.id;
                    if (string.IsNullOrEmpty(docId))
                        docId = GetDocumentId(r.index);
                    if (string.IsNullOrEmpty(docId))
                        continue;

                    var resolvedDocumentString = ResolveDocumentHandler(docId);
                    if (!string.IsNullOrEmpty(resolvedDocumentString))
                    {
                        if (resolvedDocumentString.IndexOf(word, StringComparison.Ordinal) != -1)
                            results.Add(new SearchResult(docId, r.index, 0));
                    }
                }
            }
            else
            {
                var allDocResults = Internal_GetDocumentsAsResults(true);
                for (var i = 0; i < allDocResults.Length; ++i)
                {
                    var r = allDocResults[i];

                    var resolvedDocumentString = ResolveDocumentHandler(r.id);
                    if (!string.IsNullOrEmpty(resolvedDocumentString))
                    {
                        if (resolvedDocumentString.IndexOf(word, StringComparison.Ordinal) != -1)
                            results.Add(new SearchResult(r.id, r.index, 0));
                    }
                }
            }
        }

        [FreeFunction("LMDBIndexStorageBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, string filePath, ulong initialSize);

        [FreeFunction("LMDBIndexStorageBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [NativeMethod("GetDocumentCount", IsThreadSafe = true)]
        extern ulong Internal_GetDocumentCount();

        [NativeMethod("GetEntryCount", IsThreadSafe = true)]
        extern ulong Internal_GetEntryCount();

        [NativeMethod("GetKeywordCount", IsThreadSafe = true)]
        extern ulong Internal_GetKeywordCount();

        [NativeMethod("GetDocuments", IsThreadSafe = true)]
        extern SearchDocument[] Internal_GetDocuments(bool ignoreNulls);

        [NativeMethod("GetDocumentsAsResults", IsThreadSafe = true)]
        extern SearchResult[] Internal_GetDocumentsAsResults(bool ignoreNulls);

        [NativeMethod("GetDocumentIndexes", IsThreadSafe = true)]
        extern int[] Internal_GetDocumentIndexes();

        [NativeMethod(IsThreadSafe = true)]
        extern string GetDocumentId(int documentIndex);

        [NativeMethod("GetKeywords", IsThreadSafe = true)]
        extern string[] Internal_GetKeywords();

        [NativeMethod("AddWord", IsThreadSafe = true)]
        extern void Internal_AddWord(string word, int score, int documentIndex);

        [NativeMethod("AddPropertyDouble", IsThreadSafe = true)]
        extern void Internal_AddPropertyDouble(string name, double value, int score, int documentIndex);

        [NativeMethod("AddPropertyString", IsThreadSafe = true)]
        extern void Internal_AddPropertyString(string name, string value, int score, int documentIndex, bool saveKeyword);

        [FreeFunction("LMDBIndexStorageBindings::Merge", IsThreadSafe = true)]
        static extern void Internal_Merge(IntPtr currentStorage, string[] documentsToRemove, ReadOnlySpan<IntPtr> otherStoragePointers, int baseScore, int progressId, SearchCancellationToken cancellationToken);

        [FreeFunction("LMDBIndexStorageBindings::MergeArtifacts", IsThreadSafe = true)]
        static extern void Internal_MergeArtifacts(IntPtr currentStorage, string[] documentsToRemove, string[] artifactPaths, int baseScore, int progressId, SearchCancellationToken cancellationToken);

        [FreeFunction("LMDBIndexStorageBindings::MergeArtifactsBatch", IsThreadSafe = true)]
        static extern void Internal_MergeArtifactsBatch(IntPtr currentStorage, string[] documentsToRemove, string[] artifactPaths, int baseScore, int progressId, long batchSize, SearchCancellationToken cancellationToken);

        [FreeFunction("LMDBIndexStorageBindings::MergeArtifactImportDataBatch", IsThreadSafe = true)]
        static extern void Internal_MergeArtifactImportDataBatch(IntPtr currentStorage, string[] documentsToRemove, ReadOnlySpan<int> artifactImporterHashCodes, ReadOnlySpan<ArtifactKey> artifactKeys, ReadOnlySpan<ImportResultID> artifactImportResultIds, int baseScore, int progressId, long batchSize, SearchCancellationToken cancellationToken);

        [NativeMethod(IsThreadSafe = true)]
        extern SearchResult[] SearchPropertyDouble(string name, double value, SearchIndexOperator op);

        [NativeMethod(IsThreadSafe = true)]
        extern SearchResult[] SearchPropertyString(string name, string value, SearchIndexOperator op);

        [NativeMethod(IsThreadSafe = true)]
        extern SearchResult[] SearchWord(string word, SearchIndexOperator op);

        [NativeMethod(IsThreadSafe = true)]
        extern void Sync();

        [NativeMethod(IsThreadSafe = true)]
        extern byte[] SaveToArtifactBytes();

        [NativeMethod(IsThreadSafe = true)]
        extern bool LoadFromArtifactBytes(ReadOnlySpan<byte> bytes, bool checkVersionOnly);
    }
}
