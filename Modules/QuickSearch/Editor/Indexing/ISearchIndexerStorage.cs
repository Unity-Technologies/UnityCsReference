// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor.Search
{
    interface ISearchIndexerStorage
    {
        // Versioning
        int Version { get; }

        // State
        long Timestamp { get; }
        int KeywordCount { get; } // Should return ulong, but SearchIndexer is stuck with int
        int DocumentCount { get; } // Should return ulong, but SearchIndexer is stuck with int
        int EntryCount { get; } // Maps to indexCount, but I prefer this name. Should return ulong, but SearchIndexer is stuck with int
        Func<string, string> ResolveDocumentHandler { get; set; }
        Func<string, SearchIndexOperator, SearchResultCollection, IEnumerable<SearchResult>> ExtraSearchWordHandler { get; set; }
        bool IsReady();

        // Dispose pattern for releasing resources
        void Dispose(bool disposing);

        // Data indexing
        IEnumerable<string> GetKeywords();
        void SetMetaInfo(string documentId, string metadata);
        string GetMetaInfo(string documentId);
        IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls);
        IEnumerable<int> GetDocumentIndexes();
        SearchDocument GetDocument(int index);
        int FindDocumentIndex(string documentId);
        int AddDocument(string documentId, string name, string source, bool checkIfExists, SearchDocumentFlags flags);
        void AddSourceDocument(string sourceDocumentPath, Hash128 documentHash);
        bool TryGetSourceDocument(string sourceDocumentPath, out Hash128 documentHash);
        void AddTypeStructuralVersion(string sourceDocumentPath, Hash128 typeStructuralVersion);
        bool TryGetTypeStructuralVersion(string sourceDocumentPath, out Hash128 typeStructuralVersion);
        void RemoveDocuments(string[] documentsToRemove);
        void AddWord(string word, int minVariations, int maxVariations, int score, int documentIndex);
        void AddExactWord(string word, int score, int documentIndex);
        void AddProperty(string name, double value, int score, int documentIndex);
        void AddProperty(string name, string value, int minVariations, int maxVariations, int score, int documentIndex, bool exact, bool saveKeyword);
        void AddExactProperty(string name, string value, int score, int documentIndex, bool saveKeyword);
        void MapProperty(string name, string label, string help, string propertyType, string ownerTypeName, SearchPropositionGenerationOptions propositionGenerationOptions, bool removeNestedKeys);
        void Start(bool clear);
        void Finish(string[] removedDocuments);

        // Merging
        void CombineIndexes(IReadOnlyList<SearchIndexer> indexes, int baseScore, string indexName, SearchTask<TaskData> task); // Combine is a Merge operation without removing any document.
        void Merge(string[] removedDocuments, SearchIndexer other, int baseScore, Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task);
        void ApplyFrom(SearchIndexer source);

        // IO
        void Write(Stream stream);
        bool Read(Stream stream, bool checkVersionOnly);

        // Searching
        IEnumerable<SearchResult> SearchTerm(string name, object value, SearchIndexOperator op, bool exclude, SearchResultCollection subset);
    }
}
