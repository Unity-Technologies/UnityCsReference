// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_SEARCHINDEXER_DISPOSE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    /// <summary>
    /// Encapsulate an element that was retrieved from a query in a g.
    /// </summary>
    public readonly struct SearchResult : IEquatable<SearchResult>, IComparable<SearchResult>
    {
        /// <summary>
        /// Represents a null search result.
        /// </summary>
        public static readonly SearchResult nil = new SearchResult(-1);

        /// <summary>Id of the document containing that result.</summary>
        public readonly string id;
        /// <summary>Index of the document containing that result.</summary>
        public readonly int index;
        /// <summary>Score of the result. Higher means it is a more relevant result.</summary>
        public readonly int score;

        /// <summary>
        /// Checks if a search result is valid.
        /// </summary>
        public bool valid => index != -1 || id != null;

        /// <summary>
        /// Create a new SearchResult
        /// </summary>
        /// <param name="id">Id of the document containing that result.</param>
        /// <param name="index">Index of the document containing that result.</param>
        /// <param name="score">Score of the result. Higher means it is a more relevant result.</param>
        public SearchResult(string id, int index, int score)
        {
            this.id = id;
            this.index = index;
            this.score = score;
        }

        /// <summary>
        /// Create a new SearchResult
        /// </summary>
        /// <param name="index">Index of the document containing that result.</param>
        public SearchResult(int index)
        {
            this.id = null;
            this.score = 0;
            this.index = index;
        }

        /// <summary>
        /// Create a new SearchResult
        /// </summary>
        /// <param name="index">Index of the document containing that result.</param>
        /// <param name="score">Score of the result. Higher means it is a more relevant result.</param>
        public SearchResult(int index, int score)
        {
            this.id = null;
            this.index = index;
            this.score = score;
        }

        /// <summary>
        /// Compare Search Result using their index value.
        /// </summary>
        /// <param name="other">Another SearchResult to compare.</param>
        /// <returns>Returns true if both SearchResult have the same index.</returns>
        public bool Equals(SearchResult other)
        {
            return index == other.index;
        }

        /// <summary>
        /// Compute the hash code for this SearchResult from its index property.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return index.GetHashCode();
            }
        }

        /// <summary>
        /// Compare Search Result using their index value.
        /// </summary>
        /// <param name="other">Another SearchResult to compare.</param>
        /// <returns>Returns true if both SearchResult have the same index.</returns>
        public override bool Equals(object other)
        {
            return other is SearchResult l && Equals(l);
        }

        /// <summary>
        /// Compare Search Result using their index value.
        /// </summary>
        /// <param name="other">Another SearchResult to compare.</param>
        /// <returns>Returns true if both SearchResult have the same index.</returns>
        public int CompareTo(SearchResult other)
        {
            // TODO: Investigate the impact of removing the score compare that should not be here, if we consider the Equals and HashCode methods.
            var c = other.score.CompareTo(other.score);
            if (c == 0)
                return index.CompareTo(other.index);
            return c;
        }

        public override string ToString()
        {
            return id;
        }
    }

    [Flags, NativeHeader("Modules/QuickSearch/SearchDocumentFlags.h")]
    public enum SearchDocumentFlags
    {
        None = 0,
        Asset = 1 << 0,
        Object = 1 << 1,
        Nested = 1 << 2,
        Grouped = 1 << 3,

        Resources = Asset | Grouped
    }

    static class SearchDocumentFlagsExtensions
    {
        public static bool HasAny(this SearchDocumentFlags flags, SearchDocumentFlags f) => (flags & f) != 0;
        public static bool HasAll(this SearchDocumentFlags flags, SearchDocumentFlags all) => (flags & all) == all;
    }

    /// <summary>
    /// Represents a searchable document that has been indexed.
    /// </summary>
    public readonly struct SearchDocument : IEquatable<SearchDocument>, IComparable<SearchDocument>
    {
        internal static readonly SearchDocument invalid = new SearchDocument();

        public readonly string id;
        public readonly int score;
        internal readonly string m_Name;
        internal readonly string m_Source;
        internal readonly SearchDocumentFlags flags;

        public string name
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Name))
                    return m_Name;
                if (!string.IsNullOrEmpty(m_Source))
                    return m_Source;
                return id;
            }
        }

        public string source
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Source))
                    return m_Source;
                return id;
            }
        }

        public bool valid => !string.IsNullOrEmpty(id);

        internal SearchDocument(string id, string name, string source, int score, SearchDocumentFlags flags)
        {
            this.id = id;
            this.score = score;
            this.flags = flags;
            m_Name = name;
            m_Source = source;
        }

        /// <summary>
        /// Create a new SearchDocument
        /// </summary>
        /// <param name="id">Document Id</param>
        /// <param name="metadata">Additional data about this document</param>
        public SearchDocument(string id, string name = null, string source = null, int score = int.MaxValue)
            : this(id, name, source, score, SearchDocumentFlags.None)
        {
        }

        public SearchDocument(SearchDocument doc, int score)
            : this(doc.id, doc.m_Name, doc.m_Source, score, doc.flags)
        {
        }

        public SearchDocument(SearchDocument doc, string path)
            : this(doc.id, path, doc.m_Source, doc.score, doc.flags)
        {
        }

        /// <summary>
        /// Returns the document id string.
        /// </summary>
        /// <returns>Returns a string representation of the Document.</returns>
        public override string ToString()
        {
            if (m_Name != null && m_Source != null)
                return $"{m_Name} ({m_Source}) {{{id}}}";
            if (m_Name != null)
                return $"{m_Name} {{{id}}}";
            if (m_Source != null)
                return $"{m_Source} {{{id}}}";
            return id;
        }

        public bool Equals(SearchDocument other)
        {
            return string.Equals(id, other.id, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashingUtils.GetHashCode(id);
        }

        public override bool Equals(object other)
        {
            return other is SearchDocument l && Equals(l);
        }

        public int CompareTo(SearchDocument other)
        {
            return string.CompareOrdinal(id, other.id);
        }
    }

    /// <summary>
    /// Base class for an Indexer of document which allow retrieving of a document given a specific pattern in roughly log(n).
    /// </summary>
    public class SearchIndexer : IDisposable
    {

        // 1- Initial version after removing the old SearchIndexEntry
        // 2- Fix Search Areas for sub assets and sub objects
        internal const int version = 2;

        /// <summary>
        /// Name of the index. Generally this name is given by a user from a <see cref="SearchDatabase.Settings"/>
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Time indicating when the index was saved
        /// </summary>
        public long timestamp => m_Storage.Timestamp;

        /// <summary>
        /// Returns how many keywords the index contains.
        /// </summary>
        public int keywordCount => m_Storage.KeywordCount;

        /// <summary>
        /// Returns how many documents the index contains.
        /// </summary>
        public int documentCount => m_Storage.DocumentCount;

        internal int indexCount => m_Storage.EntryCount;

        /// <summary>
        /// Handler used to skip some entries.
        /// </summary>
        public Func<string, bool> skipEntryHandler { get; set; }

        /// <summary>
        /// Handler used to resolve a document id to some other data string.
        /// </summary>
        public Func<string, string> resolveDocumentHandler
        {
            get => m_Storage.ResolveDocumentHandler;
            set => m_Storage.ResolveDocumentHandler = value;
        }

        Func<IEnumerable<string>> m_FetchDefaultFilter;
        internal Func<IEnumerable<string>> fetchDefaultFiler {
            get => m_FetchDefaultFilter;
            set
            {
                if (m_FetchDefaultFilter != value)
                {
                    m_RebuildFilters = true;
                    m_FetchDefaultFilter = value;
                }
            }
        }

        /// <summary>
        /// Minimal indexed word size.
        /// </summary>
        public int minWordIndexationLength { get; set; } = 2;

        public int minQueryLength { get; set; } = 1;

        private Thread m_IndexerThread;

        private volatile bool m_RebuildFilters = true;
        [ThreadStatic] internal bool m_DoFuzzyMatch;

        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions {validateFilters = true, skipNestedQueries = true};
        private QueryEngine<SearchResult> m_QueryEngine;
        private ConcurrentDictionary<string, ParsedQuery<SearchResult, object>> m_QueryPool;

        private readonly Dictionary<int, int> m_PatternMatchCount;

        // Final documents and entries when the index is ready.
        internal const int invalidDocumentIndex = -1;

        ISearchIndexerStorage m_Storage;
        internal ISearchIndexerStorage storage => m_Storage;

        static readonly ProfilerMarker k_MapPropertyMarker = new($"{nameof(SearchIndexer)}.{nameof(MapProperty)}");
        static readonly ProfilerMarker k_AddWordMarker = new($"{nameof(SearchIndexer)}.{nameof(AddWord)}");
        static readonly ProfilerMarker k_AddExactWordMarker = new($"{nameof(SearchIndexer)}.{nameof(AddExactWord)}");
        static readonly ProfilerMarker k_AddNumberMarker = new($"{nameof(SearchIndexer)}.{nameof(AddNumber)}");
        static readonly ProfilerMarker k_AddPropertyMarker = new($"{nameof(SearchIndexer)}.{nameof(AddProperty)}");

        [Obsolete("This constructor is no longer supported. Please use SearchIndexer(string name, string filePath)")]
        /// <summary>
        /// Create a new default SearchIndexer.
        /// </summary>
        public SearchIndexer()
            : this(string.Empty, new LMDBIndexStorage(FileUtil.GetUniqueTempPathInProject()))
        {}

        [Obsolete("This constructor is no longer supported. Please use SearchIndexer(string name, string filePath)")]
        /// <summary>
        /// Create a new SearchIndexer.
        /// </summary>
        /// <param name="name">Name of the indexer</param>
        public SearchIndexer(string name)
            : this(name, new LMDBIndexStorage(FileUtil.GetUniqueTempPathInProject()))
        {}

        public SearchIndexer(string name, string filePath)
            : this(name, new LMDBIndexStorage(filePath))
        {}

        internal SearchIndexer(string name, ISearchIndexerStorage storage)
        {
            this.name = name;

            skipEntryHandler = e => false;
            m_PatternMatchCount = new Dictionary<int, int>();
            minWordIndexationLength = SearchSettings.minIndexVariations;
            m_Storage = storage;
        }

        ~SearchIndexer()
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

            m_Storage.Dispose(disposing);

            if (disposing)
            {
                m_IndexerThread = null;
                m_PatternMatchCount.Clear();
            }
        }

        private ParsedQuery<SearchResult, object> BuildQuery(string searchQuery)
        {
            lock (this)
            {
                if (m_QueryEngine == null)
                {
                    m_QueryEngine = new QueryEngine<SearchResult>(k_QueryEngineOptions);
                    m_QueryEngine.SetSearchDataCallback(e => null, s => s.Length < minQueryLength ? null : s, StringComparison.Ordinal);
                    m_QueryPool = new ConcurrentDictionary<string, ParsedQuery<SearchResult, object>>();
                }

                if (m_RebuildFilters)
                {
                    AddFilters();
                    m_RebuildFilters = false;
                }
            }

            ParsedQuery<SearchResult, object> query;
            if (m_QueryPool.TryGetValue(searchQuery, out query) && query.valid)
                return query;

            if (m_QueryPool.Count > 50)
                m_QueryPool.Clear();

            query = m_QueryEngine.ParseQuery(searchQuery, new SearchIndexerQueryFactory(EvaluateSearchNode));
            if (query.valid)
                m_QueryPool[searchQuery] = query;
            return query;
        }

        private SearchQueryEvaluator<SearchResult>.EvalResult EvaluateSearchNode(SearchQueryEvaluator<SearchResult>.EvalHandlerArgs args)
        {
            if (args.op == SearchIndexOperator.None)
                return SearchQueryEvaluator<SearchResult>.EvalResult.None;

            SearchResultCollection subset = null;
            if (args.andSet != null)
                subset = new SearchResultCollection(args.andSet);

            // TODO: Change this so that parsing finds StringView and let each storage parse the string number values.
            // TODO: Support range find.
            IEnumerable<SearchResult> results;
            if (!string.IsNullOrEmpty(args.name) && Utils.TryParseVectorValue(args.value, out var v, out _))
            {
                // Spread vector terms
                SearchResultCollection vresults = subset;
                if (!float.IsNaN(v.x))
                    vresults = new SearchResultCollection(SearchTerm(args.name + ".x", (double)v.x, args.op, args.exclude, vresults));
                if (!float.IsNaN(v.y))
                    vresults = new SearchResultCollection(SearchTerm(args.name + ".y", (double)v.y, args.op, args.exclude, vresults));
                if (!float.IsNaN(v.z))
                    vresults = new SearchResultCollection(SearchTerm(args.name + ".z", (double)v.z, args.op, args.exclude, vresults));
                if (!float.IsNaN(v.w))
                    vresults = new SearchResultCollection(SearchTerm(args.name + ".w", (double)v.w, args.op, args.exclude, vresults));
                results = vresults;
            }
            else if (!string.IsNullOrEmpty(args.name) && Utils.TryParseRange(args.value, out var range))
            {
                SearchResultCollection subResults = subset;
                if (args.op == SearchIndexOperator.Contains || args.op == SearchIndexOperator.Equal)
                {
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.min, SearchIndexOperator.GreaterOrEqual, args.exclude, subResults));
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.max, SearchIndexOperator.LessOrEqual, args.exclude, subResults));
                }
                else if (args.op == SearchIndexOperator.NotEqual)
                {
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.min, SearchIndexOperator.Less, false, subset));
                    subResults.Add(new SearchResultCollection(SearchTerm(args.name, range.max, SearchIndexOperator.Greater, false, subset)));
                }
                else if (args.op == SearchIndexOperator.Greater)
                {
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.max, args.op, args.exclude, subResults));
                }
                else if (args.op == SearchIndexOperator.GreaterOrEqual)
                {
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.min, args.op, args.exclude, subResults));
                }
                else if (args.op == SearchIndexOperator.Less)
                {
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.min, args.op, args.exclude, subResults));
                }
                else if (args.op == SearchIndexOperator.LessOrEqual)
                {
                    subResults = new SearchResultCollection(SearchTerm(args.name, range.max, args.op, args.exclude, subResults));
                }

                results = subResults;
            }
            else
            {
                results = SearchTerm(args.name, args.value, args.op, args.exclude, subset) ?? Enumerable.Empty<SearchResult>();
            }

            if (args.orSet != null)
                results = results.Concat(args.orSet);

            return SearchQueryEvaluator<SearchResult>.EvalResult.Combined(results);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddWord(string word, int score, int documentIndex)
        {
            using var _ = k_AddWordMarker.Auto();
            m_Storage.AddWord(word, score, documentIndex);
        }

        [Obsolete("AddWord with variations is no longer supported. Variations are handled automatically internally. Please use AddWord(string word, int score, int documentIndex)")]
        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="size">Number of variations to compute.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddWord(string word, int size, int score, int documentIndex)
        {
            using var _ = k_AddWordMarker.Auto();
            m_Storage.AddWord(word, score, documentIndex);
        }

        [Obsolete("AddExactWord is no longer supported. Variations are handled automatically internally. Please use AddWord(string word, int score, int documentIndex)")]
        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added as an exact match.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddExactWord(string word, int score, int documentIndex)
        {
            using var _ = k_AddExactWordMarker.Auto();
            m_Storage.AddWord(word, score, documentIndex);
        }

        [Obsolete("AddWord with variations is no longer supported. Variations are handled automatically internally. Please use AddWord(string word, int score, int documentIndex)")]
        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="minVariations">Minimum number of variations to compute. Cannot be higher than the length of the word.</param>
        /// <param name="maxVariations">Maximum number of variations to compute. Cannot be higher than the length of the word.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddWord(string word, int minVariations, int maxVariations, int score, int documentIndex)
        {
            using var _ = k_AddWordMarker.Auto();
            m_Storage.AddWord(word, score, documentIndex);
        }

        /// <summary>
        /// Add a key-number value pair to the index. The key won't be added with variations.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">Number value to store in the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        public void AddNumber(string key, double value, int score, int documentIndex)
        {
            using var _ = k_AddNumberMarker.Auto();
            m_Storage.AddProperty(key, value, score, documentIndex);
        }

        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        public void AddProperty(string key, string value, int documentIndex)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(key, value, 0, documentIndex, false);
        }

        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        public void AddProperty(string key, string value, int score, int documentIndex)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(key, value, score, documentIndex, false);
        }

        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        public void AddProperty(string key, string value, int documentIndex, bool saveKeyword)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(key, value, 0, documentIndex, saveKeyword);
        }

        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        public void AddProperty(string key, string value, int score, int documentIndex, bool saveKeyword)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(key, value, score, documentIndex, saveKeyword);
        }

        [Obsolete("AddProperty with variations is no longer supported. Variations are handled automatically internally. Please use AddProperty(string name, string value, int score, int documentIndex, bool saveKeyword)")]
        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value. The value will be stored with multiple variations.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        public void AddProperty(string key, string value, int documentIndex, bool saveKeyword = false, bool exact = true)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(key, value, 0, documentIndex, saveKeyword);
        }

        [Obsolete("AddProperty with variations is no longer supported. Variations are handled automatically internally. Please use AddProperty(string name, string value, int score, int documentIndex, bool saveKeyword)")]
        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value. The value will be stored with multiple variations.
        /// </summary>
        /// <param name="key">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        public void AddProperty(string key, string value, int score, int documentIndex, bool saveKeyword = false, bool exact = true)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(key, value, score, documentIndex, saveKeyword);
        }

        [Obsolete("AddProperty with variations is no longer supported. Variations are handled automatically internally. Please use AddProperty(string name, string value, int score, int documentIndex, bool saveKeyword)")]
        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value. The value will be stored with multiple variations.
        /// </summary>
        /// <param name="name">Key used to retrieve the value.</param>
        /// <param name="value">String value to store in the index.</param>
        /// <param name="minVariations">Minimum number of variations to compute for the value. Cannot be higher than the length of the word.</param>
        /// <param name="maxVariations">Maximum number of variations to compute for the value. Cannot be higher than the length of the word.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        public void AddProperty(string name, string value, int minVariations, int maxVariations, int score, int documentIndex, bool saveKeyword = false, bool exact = true)
        {
            using var _ = k_AddPropertyMarker.Auto();
            m_Storage.AddProperty(name, value, score, documentIndex, saveKeyword);
        }

        /// <summary>
        /// Is the index fully built and up to date and ready for search.
        /// </summary>
        /// <returns>Returns true if the index is ready for search.</returns>
        public bool IsReady()
        {
            return m_Storage.IsReady();
        }

        public virtual IEnumerable<SearchResult> Search(string query, int maxScore = int.MaxValue, int patternMatchLimit = 2999)
        {
            return Search(query, null, null, maxScore, patternMatchLimit);
        }

        public virtual IEnumerable<SearchResult> Search(SearchContext context, SearchProvider provider, int maxScore = int.MaxValue, int patternMatchLimit = 2999)
        {
            return Search(context.searchQuery, context, provider, maxScore, patternMatchLimit);
        }

        /// <summary>
        /// Run a search query in the index.
        /// </summary>
        /// <param name="query">Search query to look out for. If if matches any of the indexed variations a result will be returned.</param>
        /// <param name="context">The search context on which the query is applied.</param>
        /// <param name="provider">The provider that initiated the search.</param>
        /// <param name="maxScore">Maximum score of any matched Search Result. See <see cref="SearchResult.score"/>.</param>
        /// <param name="patternMatchLimit">Maximum number of matched Search Result that can be returned. See <see cref="SearchResult"/>.</param>
        /// <returns>Returns a collection of Search Result matching the query.</returns>
        public virtual IEnumerable<SearchResult> Search(string query, SearchContext context, SearchProvider provider, int maxScore = int.MaxValue, int patternMatchLimit = 2999)
        {
            if (!IsReady())
                throw new Exception($"Cannot search index {name} since it is not ready");

            var parsedQuery = BuildQuery(query);
            if (!parsedQuery.valid)
            {
                if (context != null && provider != null)
                    context.AddSearchQueryErrors(parsedQuery.errors.Select(e => new SearchQueryError(e, context, provider)));
                return Enumerable.Empty<SearchResult>();
            }
            m_DoFuzzyMatch = parsedQuery.HasToggle("fuzzy");

            return patternMatchLimit == int.MaxValue ? parsedQuery.Apply(null) : parsedQuery.Apply(null).Take(patternMatchLimit);
        }

        [Obsolete("This method is no longer supported. The content of the indexer is automatically saved on disk.")]
        /// <summary>
        /// Write a binary representation of the the index on a stream.
        /// </summary>
        /// <param name="stream">Stream where to write the index.</param>
        public void Write(Stream stream)
        {
            m_Storage.Write(stream);
        }

        [Obsolete("This method is no longer supported. The content of the indexer is automatically saved on disk.")]
        /// <summary>
        /// Get the bytes representation of this index. See <see cref="SearchIndexer.Write"/>.
        /// </summary>
        /// <returns>Bytes representation of the index.</returns>
        public byte[] SaveBytes()
        {
            using (var memoryStream = new MemoryStream())
            {
                Write(memoryStream);
                return memoryStream.ToArray();
            }
        }

        [Obsolete("This method is no longer supported. The content of the indexer is automatically saved on disk.")]
        /// <summary>
        /// Read a stream and populate the index from it.
        /// </summary>
        /// <param name="stream">Stream where to read the index from.</param>
        /// <param name="checkVersionOnly">If true, it will only read the version of the index and stop reading any more content.</param>
        /// <returns>Returns false if the version of the index is not supported.</returns>
        public bool Read(Stream stream, bool checkVersionOnly)
        {
            var success = m_Storage.Read(stream, checkVersionOnly);
            if (success && !checkVersionOnly)
                m_RebuildFilters = true;
            return success;
        }

        internal void MapProperty(in string name, in string label, in string help, in string propertyType, in string ownerTypeName, SearchPropositionGenerationOptions propositionGenerationOptions = SearchPropositionGenerationOptions.None, bool removeNestedKeys = false)
        {
            using var _ = k_MapPropertyMarker.Auto();
            m_Storage.MapProperty(name, label, help, propertyType, ownerTypeName, propositionGenerationOptions, removeNestedKeys);
        }

        [Obsolete("This method is no longer supported. The content of the indexer is automatically saved on disk.")]
        /// <summary>
        /// Load asynchronously (i.e. in another thread) the index from a binary buffer.
        /// </summary>
        /// <param name="bytes">Binary buffer containing the index representation.</param>
        /// <param name="finished">Callback that will trigger when the index is fully loaded. The callback parameters indicate if the loading was succesful.</param>
        /// <returns>Returns false if the index is of an unsupported version or if there was a problem initializing the reading thread.</returns>
        public bool LoadBytes(byte[] bytes, Action<bool> finished)
        {
            using (var memoryStream = new MemoryStream(bytes))
                if (!Read(memoryStream, true))
                    return false;

            var t = new Thread(() =>
            {
                using (var memoryStream = new MemoryStream(bytes))
                {
                    var success = Read(memoryStream, false);
                    Dispatcher.Enqueue(() => finished(success));
                }
            });
            t.Start();
            return t.ThreadState != ThreadState.Unstarted;
        }

        /// <summary>
        /// Called when the index is built to see if a specified document needs to be indexed. See <see cref="SearchIndexer.skipEntryHandler"/>
        /// </summary>
        /// <param name="document">Path of a document</param>
        /// <param name="checkRoots"></param>
        /// <returns>Returns true if the document doesn't need to be indexed.</returns>
        public virtual bool SkipEntry(string document, bool checkRoots = false)
        {
            return skipEntryHandler?.Invoke(document) ?? false;
        }

        /// <summary>
        /// Function to override in a concrete SearchIndexer to index the content of a document.
        /// </summary>
        /// <param name="document">Path of the document to index.</param>
        /// <param name="checkIfDocumentExists">Check if the document actually exists.</param>
        public virtual void IndexDocument(string document, bool checkIfDocumentExists)
        {
            throw new NotImplementedException($"{nameof(IndexDocument)} must be implemented by a specialized indexer.");
        }

        // This method assumes that all the entries in each artifact is already sorted, which should be the case since we sort them
        // when we call Finish.
        // Keep this method internal, we are modifying the entries buffer of the passed artifacts.
        internal void CombineIndexes(IReadOnlyList<SearchIndexer> artifacts, int baseScore, string indexName, SearchTask<TaskData> task)
        {
            //using (new DebugTimer("Combining artifacts"))
            {
                m_Storage.CombineIndexes(artifacts, baseScore, indexName, task);
                m_RebuildFilters = true;
            }
        }

        public void Merge(string[] removeDocuments, SearchIndexer other, int baseScore = 0,
            Action<int, SearchIndexer, int> documentIndexing = null)
        {
            Merge(removeDocuments, other, baseScore, documentIndexing, null);
        }

        internal void Merge(string[] removeDocuments, SearchIndexer other, int baseScore,
            Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task)
        {
            m_Storage.Merge(removeDocuments, other, baseScore, documentIndexing, task);
            m_RebuildFilters = true;
        }

        void AddFilters()
        {
            if (fetchDefaultFiler != null)
            {
                var defaultFilters = fetchDefaultFiler();
                foreach (var f in defaultFilters)
                    if (!m_QueryEngine.HasFilter(f))
                        m_QueryEngine.AddFilter(f);
            }

            foreach (var kw in GetKeywords())
            {
                var filter = kw;
                var ft = filter.IndexOfAny(SearchUtils.KeywordsValueDelimiters);
                if (ft >= 0)
                    filter = filter.Substring(0, ft);
                if (!m_QueryEngine.HasFilter(filter))
                    m_QueryEngine.AddFilter(filter);
            }
        }

        internal IEnumerable<string> GetKeywords()
        {
            return m_Storage.GetKeywords();
        }

        internal IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls = false)
        {
            return m_Storage.GetDocuments(ignoreNulls);
        }

        /// <summary>
        /// Return a search document by its index.
        /// </summary>
        /// <param name="index">Valid index of the document to access.</param>
        /// <returns>Indexed search document</returns>
        public SearchDocument GetDocument(int index)
        {
            return m_Storage.GetDocument(index);
        }

        internal int FindDocumentIndex(string documentId)
        {
            return m_Storage.FindDocumentIndex(documentId);
        }

        internal bool TryGetHash(string id, out Hash128 hash)
        {
            return m_Storage.TryGetSourceDocument(id, out hash);
        }

        internal bool TryGetTypeStructuralVersion(string id, out Hash128 hash)
        {
            return m_Storage.TryGetTypeStructuralVersion(id, out hash);
        }

        /// <summary>
        /// Add a new document to be indexed.
        /// </summary>
        /// <param name="document">Unique id of the document</param>
        /// <param name="checkIfExists">Pass true if this document has some chances of existing already.</param>
        /// <returns>The document index/handle used to add new index entries.</returns>
        public int AddDocument(string document, bool checkIfExists = true)
        {
            return AddDocument(document, null, null, checkIfExists, SearchDocumentFlags.None);
        }

        public int AddDocument(string document, string name, string source, bool checkIfExists, SearchDocumentFlags flags)
        {
            // Reformat entry to have them all uniformed.
            if (skipEntryHandler(document))
                return invalidDocumentIndex;

            return m_Storage.AddDocument(document, name, source, checkIfExists, flags);
        }

        internal void AddSourceDocument(string sourcePath, Hash128 hash)
        {
            m_Storage.AddSourceDocument(sourcePath, hash);
        }

        internal void AddTypeStructuralVersion(string sourcePath, Hash128 hash)
        {
            m_Storage.AddTypeStructuralVersion(sourcePath, hash);
        }

        public void SetMetaInfo(string documentId, string metadata)
        {
            m_Storage.SetMetaInfo(documentId, metadata);
        }

        public string GetMetaInfo(string documentId)
        {
            return m_Storage.GetMetaInfo(documentId);
        }

        /// <summary>
        /// Start indexing entries.
        /// </summary>
        /// <param name="clear">True if the current index should be cleared.</param>
        public void Start(bool clear = false)
        {
            m_IndexerThread = null;
            m_Storage.Start(clear);
            if (clear)
            {
                m_PatternMatchCount.Clear();
            }
        }

        /// <summary>
        /// Finalize the current index, sorting and compiling of all the indexes.
        /// </summary>
        public void Finish()
        {
            Finish(removedDocuments: null);
        }

        /// <summary>
        /// /// Finalize the current index, sorting and compiling of all the indexes.
        /// </summary>
        /// <param name="threadCompletedCallback">Callback invoked when the index is ready to be used.</param>
        public void Finish(Action threadCompletedCallback)
        {
            Finish(threadCompletedCallback, null);
        }

        public void Finish(Action threadCompletedCallback, string[] removedDocuments)
        {
            m_IndexerThread = new Thread(() =>
            {
                try
                {
                    using (new IndexerThreadScope(AbortIndexing))
                    {
                        Finish(removedDocuments);

                        if (threadCompletedCallback != null)
                        {
                            Dispatcher.Enqueue(threadCompletedCallback);
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    if (Utils.isDeveloperBuild)
                        UnityEngine.Debug.LogException(ex);
                    else
                        Console.WriteLine($"[QS] Failed to finalize index: {ex}");
                }
            });
            m_IndexerThread.Start();
        }

        [Obsolete("This method is no longer supported. The content of the indexer is automatically saved on disk. Please use Finish(Action threadCompletedCallback, string[] removedDocuments) instead.")]
        /// <summary>
        /// /// Finalize the current index, sorting and compiling of all the indexes.
        /// </summary>
        /// <param name="threadCompletedCallback">Callback invoked when the index binary blob is ready.</param>
        /// <param name="removedDocuments">Documents to be removed from current index (if any)</param>
        public void Finish(Action<byte[]> threadCompletedCallback, string[] removedDocuments)
        {
            Finish(threadCompletedCallback, removedDocuments, saveBytes: true);
        }

        [Obsolete("This method is no longer supported. The content of the indexer is automatically saved on disk. Please use Finish(Action threadCompletedCallback, string[] removedDocuments) instead.")]
        public void Finish(Action<byte[]> threadCompletedCallback, string[] removedDocuments, bool saveBytes)
        {
            m_IndexerThread = new Thread(() =>
            {
                try
                {
                    using (new IndexerThreadScope(AbortIndexing))
                    {
                        Finish(removedDocuments);

                        if (threadCompletedCallback != null)
                        {
                            byte[] bytes = null;
                            if (saveBytes)
                                bytes = SaveBytes();
                            Dispatcher.Enqueue(() => threadCompletedCallback(bytes));
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    if (Utils.isDeveloperBuild)
                        UnityEngine.Debug.LogException(ex);
                    else
                        Console.WriteLine($"[QS] Failed to finalize index: {ex}");
                }
            });
            m_IndexerThread.Start();
        }

        public void Finish(string[] removedDocuments)
        {
            m_Storage.Finish(removedDocuments);

            m_RebuildFilters = true;

            if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                OnFinish();
            else
                Dispatcher.Enqueue(OnFinish);
        }

        private protected virtual void OnFinish()
        {}

        internal virtual IEnumerable<SearchResult> SearchTerm(
            string name, in object value, SearchIndexOperator op, bool exclude, SearchResultCollection subset = null)
        {
            return m_Storage.SearchTerm(name, value, op, exclude, subset);
        }

        private void AbortIndexing()
        {
            m_IndexerThread?.Abort();
            m_IndexerThread = null;
        }

        struct IndexerThreadScope : IDisposable
        {
            private bool m_Disposed;
            private readonly AssemblyReloadEvents.AssemblyReloadCallback m_AbortHandler;

            public IndexerThreadScope(AssemblyReloadEvents.AssemblyReloadCallback abortHandler)
            {
                m_Disposed = false;
                m_AbortHandler = abortHandler;
                AssemblyReloadEvents.beforeAssemblyReload -= abortHandler;
                AssemblyReloadEvents.beforeAssemblyReload += abortHandler;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                AssemblyReloadEvents.beforeAssemblyReload -= m_AbortHandler;
                m_Disposed = true;
            }
        }
    }
}
