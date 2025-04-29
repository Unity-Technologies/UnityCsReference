// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define DEBUG_SEARCHINDEXER_DISPOSE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.Collections;
using Unity.Profiling;
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
        // 34- Modify ObjectIndex.IndexProperty to force all property names to be lowercase.
        internal const int version = 0x034;

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
        {}

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

    [Flags]
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

        public string name => m_Name ?? m_Source ?? id;
        public string source => m_Source ?? id;
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
            unchecked
            {
                return id.GetHashCode();
            }
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

        /// <summary>
        /// Name of the index. Generally this name is given by a user from a <see cref="SearchDatabase.Settings"/>
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Time indicating when the index was saved
        /// </summary>
        public long timestamp => m_Timestamp;

        /// <summary>
        /// Returns how many keywords the index contains.
        /// </summary>
        public int keywordCount => m_Keywords.Count;

        /// <summary>
        /// Returns how many documents the index contains.
        /// </summary>
        public int documentCount => GetDocuments(true).Count();

        internal int indexCount
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

        /// <summary>
        /// Handler used to skip some entries.
        /// </summary>
        public Func<string, bool> skipEntryHandler { get; set; }

        /// <summary>
        /// Handler used to resolve a document id to some other data string.
        /// </summary>
        public Func<string, string> resolveDocumentHandler { get; set; }

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

        internal IEnumerable<SearchIndexEntry> indexes => m_Indexes;
        internal SearchDocumentListTable documentListTable => m_DocumentListTable;

        private long m_Timestamp;
        private Thread m_IndexerThread;
        private volatile bool m_IndexReady = false;
        private volatile bool m_RebuildFilters = true;
        [ThreadStatic] internal bool m_DoFuzzyMatch;

        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions {validateFilters = true, skipNestedQueries = true};
        private QueryEngine<SearchResult> m_QueryEngine;
        private ConcurrentDictionary<string, ParsedQuery<SearchResult, object>> m_QueryPool;

        private readonly Dictionary<RangeSet, IndexRange> m_FixedRanges;
        private SearchResultCollection m_AllDocumentIndexes;
        private readonly Dictionary<int, int> m_PatternMatchCount;

        // Temporary documents and entries while the index is being built (i.e. Start/Finish).
        private readonly List<SearchIndexEntry> m_BatchIndexes;

        // Final documents and entries when the index is ready.
        internal const int invalidDocumentIndex = -1;
        private int m_NextDocumentIndex;
        private ConcurrentDictionary<int, SearchDocument> m_Documents;
        SearchNativeReadOnlyArray<SearchIndexEntry> m_Indexes;
        private HashSet<string> m_Keywords;
        private ConcurrentDictionary<string, Hash128> m_SourceDocuments;
        private ConcurrentDictionary<string, string> m_MetaInfo;
        internal ConcurrentDictionary<string, int> m_IndexByDocuments;
        SearchDocumentListTable m_DocumentListTable;
        GCHandle m_DocumentListTableHandle;

        static readonly ProfilerMarker k_MapPropertyMarker = new($"{nameof(SearchIndexer)}.{nameof(MapProperty)}");
        static readonly ProfilerMarker k_AddWordMarker = new($"{nameof(SearchIndexer)}.{nameof(AddWord)}");
        static readonly ProfilerMarker k_AddExactWordMarker = new($"{nameof(SearchIndexer)}.{nameof(AddExactWord)}");
        static readonly ProfilerMarker k_AddNumberMarker = new($"{nameof(SearchIndexer)}.{nameof(AddNumber)}");
        static readonly ProfilerMarker k_AddPropertyMarker = new($"{nameof(SearchIndexer)}.{nameof(AddProperty)}");
        static readonly ProfilerMarker k_AddExactPropertyMarker = new($"{nameof(SearchIndexer)}.{nameof(AddExactProperty)}");

        /// <summary>
        /// Create a new default SearchIndexer.
        /// </summary>
        public SearchIndexer()
            : this(String.Empty)
        {
            minWordIndexationLength = SearchSettings.minIndexVariations;
        }

        /// <summary>
        /// Create a new SearchIndexer.
        /// </summary>
        /// <param name="name">Name of the indexer</param>
        public SearchIndexer(string name)
        {
            this.name = name;

            skipEntryHandler = e => false;

            m_NextDocumentIndex = invalidDocumentIndex;
            m_Keywords = new HashSet<string>();
            m_Documents = new ConcurrentDictionary<int, SearchDocument>();
            m_IndexByDocuments = new ConcurrentDictionary<string, int>();
            m_Indexes = default;
            m_BatchIndexes = new List<SearchIndexEntry>();
            m_PatternMatchCount = new Dictionary<int, int>();
            m_FixedRanges = new Dictionary<RangeSet, IndexRange>();
            m_SourceDocuments = new ConcurrentDictionary<string, Hash128>();
            m_MetaInfo = new ConcurrentDictionary<string, string>();
            minWordIndexationLength = SearchSettings.minIndexVariations;
            m_DocumentListTable = new SearchDocumentListTable(doCreate: false);
            m_DocumentListTableHandle = default;
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

            m_Indexes.Dispose();
            if (m_DocumentListTableHandle.IsAllocated)
                m_DocumentListTableHandle.Free();
            m_DocumentListTable?.Dispose();

            if (disposing)
            {
                m_IndexerThread = null;
                m_IndexReady = false;
                m_BatchIndexes.Clear();
                m_FixedRanges.Clear();
                m_PatternMatchCount.Clear();
                m_Keywords.Clear();
                m_Documents.Clear();
                m_IndexByDocuments.Clear();
                m_SourceDocuments.Clear();
                m_MetaInfo.Clear();
                m_DocumentListTable = new SearchDocumentListTable(doCreate: false);
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

            IEnumerable<SearchResult> results;
            if (Utils.TryParseVectorValue(args.value, out var v, out _))
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
            AddWord(word, 2, word.Length, score, documentIndex, m_BatchIndexes);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="size">Number of variations to compute.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddWord(string word, int size, int score, int documentIndex)
        {
            AddWord(word, size, size, score, documentIndex, m_BatchIndexes);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added as an exact match.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddExactWord(string word, int score, int documentIndex)
        {
            AddExactWord(word, score, documentIndex, m_BatchIndexes);
        }

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
            AddWord(word, minVariations, maxVariations, score, documentIndex, m_BatchIndexes);
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
            AddNumber(key, value, score, documentIndex, m_BatchIndexes);
        }

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
            AddProperty(key, value, minWordIndexationLength, value.Length, 0, documentIndex, m_BatchIndexes, exact, saveKeyword);
        }

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
            AddProperty(key, value, minWordIndexationLength, value.Length, score, documentIndex, m_BatchIndexes, exact, saveKeyword);
        }

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
            AddProperty(name, value, minVariations, maxVariations, score, documentIndex, m_BatchIndexes, exact, saveKeyword);
        }

        /// <summary>
        /// Is the index fully built and up to date and ready for search.
        /// </summary>
        /// <returns>Returns true if the index is ready for search.</returns>
        public bool IsReady()
        {
            return m_IndexReady;
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

        class StringTable : IList<string>
        {
            private List<string> m_Table;
            private int m_Candidates = 0;

            public StringTable()
            {
                m_Table = new List<string>();
                Add(string.Empty);
            }

            public override string ToString()
            {
                return $"{m_Table.Count}/{m_Candidates}";
            }

            public StringTable(BinaryReader indexReader)
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
            [System.Runtime.CompilerServices.MethodImpl(256)] public int WIndexOf(string item)
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

        /// <summary>
        /// Write a binary representation of the the index on a stream.
        /// </summary>
        /// <param name="stream">Stream where to write the index.</param>
        public void Write(Stream stream)
        {
            // Build string table
            var st = new StringTable();
            st.UnionWith(m_SourceDocuments.Keys);
            st.UnionWith(m_IndexByDocuments.Keys);
            st.UnionWith(m_MetaInfo.Keys);
            st.UnionWith(m_MetaInfo.Values);
            st.UnionWith(m_Documents.Select(d => d.Value.id));
            st.UnionWith(m_Documents.Select(d => d.Value.m_Name));
            st.UnionWith(m_Documents.Select(d => d.Value.m_Source));
            st.UnionWith(m_Keywords);

            using (var indexWriter = new BinaryWriter(stream))
            {
                m_Timestamp = DateTime.UtcNow.ToBinary();

                indexWriter.Write(SearchIndexEntry.version);
                indexWriter.Write(timestamp);

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
                    indexWriter.Write(kvp.Value.ToString());
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

        /// <summary>
        /// Get the bytes representation of this index. See <see cref="SearchIndexer.Write"/>.
        /// </summary>
        /// <returns>Bytes representation of the index.</returns>
        public byte[] SaveBytes()
        {
            using (var memoryStream = new MemoryStream())
            {
                lock (this)
                    Write(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Read a stream and populate the index from it.
        /// </summary>
        /// <param name="stream">Stream where to read the index from.</param>
        /// <param name="checkVersionOnly">If true, it will only read the version of the index and stop reading any more content.</param>
        /// <returns>Returns false if the version of the index is not supported.</returns>
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
                var st = new StringTable(indexReader);

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
                var hashes = new ConcurrentDictionary<string, Hash128>();
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = st.Read(indexReader);
                    var hash = Hash128.Parse(indexReader.ReadString());
                    hashes[key] = hash;
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
                    ApplyIndexes(documents, indexes, documentListTable, handle, hashes, metainfos);
                    m_Keywords = new HashSet<string>(keywords);
                }

                return true;
            }
        }

        internal void MapProperty(in string name, in string label, in string help, in string propertyType, in string ownerTypeName, SearchPropositionGenerationOptions propositionGenerationOptions = SearchPropositionGenerationOptions.None, bool removeNestedKeys = false)
        {
            using var _ = k_MapPropertyMarker.Auto();
            if (propositionGenerationOptions != SearchPropositionGenerationOptions.None)
                MapKeyword(name + ":", $"{label}|{help}|{propertyType}|{ownerTypeName}|{(int)propositionGenerationOptions}");
            else
                MapKeyword(name + ":", $"{label}|{help}|{propertyType}|{ownerTypeName}");
            if (removeNestedKeys)
            {
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

        internal void MapKeyword(string keyword, string help)
        {
            m_Keywords.Remove(keyword);
            m_Keywords.Add($"{keyword}|{help}");
        }

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

        internal bool LoadBytes(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
                return Read(memoryStream, false);
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

        internal int FindDocumentIndex(string id)
        {
            if (m_IndexByDocuments.TryGetValue(id, out var di) && m_Documents.ContainsKey(di))
                return di;
            foreach (var d in m_Documents)
            {
                if (d.Value.valid && d.Value.id.Equals(id, StringComparison.Ordinal))
                    return d.Key;
            }
            return -1;
        }

        private int FindDocumentIndex(SearchDocument doc)
        {
            if (m_IndexByDocuments.TryGetValue(doc.id, out var di))
            {
                var fdoc = m_Documents[di];
                if (string.Equals(fdoc.m_Name ?? fdoc.m_Source, doc.m_Name ?? doc.m_Source, StringComparison.Ordinal))
                    return di;
            }
            return -1;
        }

        private IEnumerable<int> FindDocumentIndexesByPath(string path)
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

        // This method assumes that all the entries in each artifact is already sorted, which should be the case since we sort them
        // when we call Finish.
        // Keep this method internal, we are modifying the entries buffer of the passed artifacts.
        internal void CombineIndexes(IReadOnlyList<SearchIndexer> artifacts, int baseScore, string indexName, SearchTask<TaskData> task)
        {
            //using (new DebugTimer("Combining artifacts"))
            {
                int i = 0;
                var wiec = new SearchIndexComparer(SearchIndexOperator.DoNotCompareScore);
                var artifactCount = artifacts.Count;
                m_Timestamp = DateTime.UtcNow.ToBinary();

                var entryEnumerables = new List<IReadOnlyList<SearchIndexEntry>>(artifacts.Count);
                var updatedDocIndexes = new Dictionary<int, int>();
                task.Report("Analyzing artifacts...", 0.0f);
                var progressRate = artifactCount < 100000 ? 0 : artifactCount / 100;
                task.throttleProgressReport = progressRate > 0;
                task.throttleProgressRate = progressRate;
                var totalEntries = 0;
                foreach (var other in artifacts)
                {
                    if (other.documentCount == 0)
                        continue;

                    task.Report(++i, artifactCount);
                    foreach (var od in other.m_Documents)
                    {
                        var currentDocIndex = FindDocumentIndex(od.Value);
                        if (currentDocIndex != -1)
                            updatedDocIndexes[od.Key] = currentDocIndex;
                    }

                    foreach (var od in other.m_Documents)
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
                    other.m_DocumentListTable.RemapDocuments(updatedDocIndexes);
                    for (var ei = 0; ei < other.m_Indexes.Count; ++ei)
                    {
                        var e = other.m_Indexes[ei];
                        other.m_Indexes[ei] = new SearchIndexEntry(e.key, e.crc, e.type, baseScore + e.score, e.docs.RemapDocuments(updatedDocIndexes));
                    }
                    if (other.m_Indexes.Count > 0)
                        entryEnumerables.Add(other.m_Indexes);
                    totalEntries += other.m_Indexes.Count;

                    // This should be very fast since there is not a lot of entries added in batchIndexes
                    other.m_BatchIndexes.Sort(wiec);
                    for (var ei = 0; ei < other.m_BatchIndexes.Count; ++ei)
                    {
                        var e = other.m_BatchIndexes[ei];
                        other.m_BatchIndexes[ei] = new SearchIndexEntry(e.key, e.crc, e.type, baseScore + e.score, e.docs.RemapDocuments(updatedDocIndexes));
                    }

                    if (other.m_BatchIndexes.Count > 0)
                        entryEnumerables.Add(other.m_BatchIndexes);
                    totalEntries += other.m_BatchIndexes.Count;

                    m_Keywords.UnionWith(other.m_Keywords);
                    foreach (var hkvp in other.m_SourceDocuments)
                        m_SourceDocuments[hkvp.Key] = hkvp.Value;
                    foreach (var mikvp in other.m_MetaInfo)
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
        }

        public void Merge(string[] removeDocuments, SearchIndexer other, int baseScore = 0,
            Action<int, SearchIndexer, int> documentIndexing = null)
        {
            Merge(removeDocuments, other, baseScore, documentIndexing, null);
        }

        internal void Merge(string[] removeDocuments, SearchIndexer other, int baseScore,
            Action<int, SearchIndexer, int> documentIndexing, SearchTask<TaskData> task)
        {
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
            var removeDocIndexes = new HashSet<int>(removeDocuments.Concat(other.m_SourceDocuments.Keys).SelectMany(FindDocumentIndexesByPath));
            if (removeDocIndexes.Count > 0)
            {
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
            foreach (var removedDocument in removeDocuments)
                m_SourceDocuments.TryRemove(removedDocument, out _);

            var wiec = new SearchIndexComparer(SearchIndexOperator.DoNotCompareScore);
            var enumerables = new List<IReadOnlyList<SearchIndexEntry>>(3);
            var totalEntries = m_Indexes.Count;
            if (indexes.Count > 0)
                enumerables.Add(indexes);
            else
                indexes.Dispose();
            if (other.documentCount > 0)
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

                    documentIndexing?.Invoke(od.Key, other, count);
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
                using var compressedEntries = new SearchNativeList<SearchIndexEntry>(indexes.Count + other.indexCount, Allocator.Persistent);
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

        private void BuildDocumentIndexTable()
        {
            m_RebuildFilters = true;
            m_IndexByDocuments.Clear();
            m_NextDocumentIndex = -1;
            foreach (var kvp in m_Documents)
            {
                if (kvp.Value.valid)
                    m_IndexByDocuments[kvp.Value.id] = kvp.Key;
                if (kvp.Key > m_NextDocumentIndex)
                    m_NextDocumentIndex = kvp.Key;
            }
            Interlocked.Increment(ref m_NextDocumentIndex);
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

            foreach (var kw in m_Keywords)
            {
                var filter = kw;
                var ft = filter.IndexOfAny(SearchUtils.KeywordsValueDelimiters);
                if (ft >= 0)
                    filter = filter.Substring(0, ft);
                if (!m_QueryEngine.HasFilter(filter))
                    m_QueryEngine.AddFilter(filter);
            }
        }

        // If this ever comes public, remove GC.SuppressFinalize(source) and make sure
        // to copy the native data. Also, do not call Dispose on the SearchIndexer passed
        // to ApplyFrom.
        internal void ApplyFrom(SearchIndexer source)
        {
            lock (this)
                lock (source)
                {
                    m_IndexReady = false;
                    m_Timestamp = DateTime.UtcNow.ToBinary();
                    m_Indexes.Dispose();
                    m_Indexes = source.m_Indexes;
                    m_Documents = source.m_Documents;
                    m_Keywords = source.m_Keywords;
                    m_SourceDocuments = source.m_SourceDocuments;
                    m_MetaInfo = source.m_MetaInfo;
                    if (m_DocumentListTableHandle.IsAllocated)
                        m_DocumentListTableHandle.Free();
                    m_DocumentListTableHandle = source.m_DocumentListTableHandle;
                    m_DocumentListTable.Dispose();
                    m_DocumentListTable = source.m_DocumentListTable;

                    // Suppress finalizer for the source, because we now own
                    // the native arrays.
                    GC.SuppressFinalize(source);

                    BuildDocumentIndexTable();

                    m_BatchIndexes.Clear();
                    m_IndexReady = true;
                }
        }

        // internal void ApplyUnsorted()
        // {
        //     UpdateIndexes(m_BatchIndexes, null, sort: false);
        // }

        internal IEnumerable<string> GetKeywords()
        {
            lock (this)
                return m_Keywords;
        }

        internal IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls = false)
        {
            return ignoreNulls ? m_Documents.Values.Where(d => d.valid) : m_Documents.Values;
        }

        /// <summary>
        /// Return a search document by its index.
        /// </summary>
        /// <param name="index">Valid index of the document to access.</param>
        /// <returns>Indexed search document</returns>
        public SearchDocument GetDocument(int index)
        {
            if (m_Documents.TryGetValue(index, out var doc))
                return doc;
            return default;
        }

        internal bool TryGetHash(string id, out Hash128 hash)
        {
            if (!m_IndexReady)
            {
                hash = default;
                return false;
            }

            return m_SourceDocuments.TryGetValue(id, out hash);
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
                return -1;

            if (checkIfExists)
            {
                var di = FindDocumentIndex(document);
                if (m_Documents.TryGetValue(di, out var existingDoc))
                {
                    m_Documents[di] = new SearchDocument(existingDoc, name);
                    return di;
                }
            }
            var newIndex = Interlocked.Increment(ref m_NextDocumentIndex);
            var newDocument = new SearchDocument(document, name, source, 0, flags);
            while (!m_Documents.TryAdd(newIndex, newDocument))
                newIndex = Interlocked.Increment(ref m_NextDocumentIndex);
            m_IndexByDocuments[document] = newIndex;
            return newIndex;
        }

        internal void AddSourceDocument(string sourcePath, Hash128 hash)
        {
            m_SourceDocuments[sourcePath] = hash;
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

        void AddWord(string word, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            AddWord(word, 2, word.Length, score, documentIndex, indexes);
        }

        void AddExactWord(string word, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            using var _ = k_AddExactWordMarker.Auto();
            indexes.Add(new SearchIndexEntry(word.GetHashCode(), int.MaxValue, SearchIndexEntry.Type.Word, score, new SearchDocumentList(documentIndex)));
        }

        void AddWord(string word, int minVariations, int maxVariations, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            using var _ = k_AddWordMarker.Auto();
            if (word == null || word.Length == 0)
                return;

            maxVariations = Math.Min(maxVariations, word.Length);
            var docList = new SearchDocumentList(documentIndex);
            for (int c = Math.Min(minVariations, maxVariations); c <= maxVariations; ++c)
            {
                var ss = word.Substring(0, c);
                indexes.Add(new SearchIndexEntry(ss.GetHashCode(), ss.Length, SearchIndexEntry.Type.Word, score, docList));
            }

            if (word.Length > maxVariations)
                indexes.Add(new SearchIndexEntry(word.GetHashCode(), word.Length, SearchIndexEntry.Type.Word, score - 1, docList));
        }

        private static bool ExcludeWordVariations(string word)
        {
            if (word == "true" || word == "false")
                return true;
            return false;
        }

        void AddExactProperty(string name, string value, int score, int documentIndex, bool saveKeyword)
        {
            AddExactProperty(name, value, score, documentIndex, m_BatchIndexes, saveKeyword);
        }

        void AddExactProperty(string name, string value, int score, int documentIndex, List<SearchIndexEntry> indexes, bool saveKeyword)
        {
            using var _ = k_AddExactPropertyMarker.Auto();
            var nameHash = name.GetHashCode();
            var valueHash = value.GetHashCode();

            // Add an exact match for property="match"
            nameHash ^= name.Length.GetHashCode();
            valueHash ^= value.Length.GetHashCode();
            indexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, score - 3, new SearchDocumentList(documentIndex)));

            if (saveKeyword)
                m_Keywords.Add($"{name}:{value}");
            else
                m_Keywords.Add($"{name}:");
        }

        void AddProperty(string name, string value, int minVariations, int maxVariations, int score, int documentIndex, List<SearchIndexEntry> indexes, bool exact, bool saveKeyword)
        {
            using var _ = k_AddPropertyMarker.Auto();
            var nameHash = name.GetHashCode();
            var valueHash = value.GetHashCode();
            maxVariations = Math.Min(maxVariations, value.Length);
            if (minVariations > value.Length)
                minVariations = value.Length;
            if (ExcludeWordVariations(value))
                minVariations = maxVariations = value.Length;

            var docList = new SearchDocumentList(documentIndex);
            for (int c = Math.Min(minVariations, maxVariations); c <= maxVariations; ++c)
            {
                var ss = value.Substring(0, c);
                indexes.Add(new SearchIndexEntry(ss.GetHashCode(), nameHash, SearchIndexEntry.Type.Property, score + (maxVariations - c), docList));
            }

            if (value.Length > maxVariations)
                indexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, score - 1, docList));

            if (exact)
            {
                nameHash ^= name.Length.GetHashCode();
                valueHash ^= value.Length.GetHashCode();
                indexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, score - 3, docList));
            }

            if (saveKeyword)
                m_Keywords.Add($"{name}:{value}");
            else
                m_Keywords.Add($"{name}:");
        }

        void AddNumber(string key, double value, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            using var _ = k_AddNumberMarker.Auto();
            var keyHash = key.GetHashCode();
            var longNumber = BitConverter.DoubleToInt64Bits(value);
            indexes.Add(new SearchIndexEntry(longNumber, keyHash, SearchIndexEntry.Type.Number, score, new SearchDocumentList(documentIndex)));

            m_Keywords.Add($"{key}:");
        }

        /// <summary>
        /// Start indexing entries.
        /// </summary>
        /// <param name="clear">True if the the current index should be cleared.</param>
        public void Start(bool clear = false)
        {
            lock (this)
            {
                m_IndexerThread = null;
                m_IndexReady = false;
                m_BatchIndexes.Clear();
                m_FixedRanges.Clear();
                m_PatternMatchCount.Clear();

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

        /// <summary>
        /// Finalize the current index, sorting and compiling of all the indexes.
        /// </summary>
        public void Finish()
        {
            Finish(null, null, saveBytes: false);
        }

        /// <summary>
        /// /// Finalize the current index, sorting and compiling of all the indexes.
        /// </summary>
        /// <param name="threadCompletedCallback">Callback invoked when the index is ready to be used.</param>
        public void Finish(Action threadCompletedCallback)
        {
            Finish(bytes => threadCompletedCallback?.Invoke(), null, saveBytes: false);
        }

        public void Finish(Action threadCompletedCallback, string[] removedDocuments)
        {
            Finish(bytes => threadCompletedCallback?.Invoke(), removedDocuments, saveBytes: false);
        }

        /// <summary>
        /// /// Finalize the current index, sorting and compiling of all the indexes.
        /// </summary>
        /// <param name="threadCompletedCallback">Callback invoked when the index binary blob is ready.</param>
        /// <param name="removedDocuments">Documents to be removed from current index (if any)</param>
        public void Finish(Action<byte[]> threadCompletedCallback, string[] removedDocuments)
        {
            Finish(threadCompletedCallback, removedDocuments, saveBytes: true);
        }

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
            lock (this)
            {
                var shouldRemoveDocuments = removedDocuments != null && removedDocuments.Length > 0;
                if (shouldRemoveDocuments)
                {
                    var removedDocIndexes = new HashSet<int>();
                    foreach (var rd in removedDocuments)
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
                    foreach (var removedDocument in removedDocuments)
                        m_SourceDocuments.TryRemove(removedDocument, out _);
                }
                UpdateIndexesOnFinish();
                m_BatchIndexes.Clear();
            }

            if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                OnFinish();
            else
                Dispatcher.Enqueue(OnFinish);
        }

        private protected virtual void OnFinish()
        {}

        internal string Print(IEnumerable<SearchIndexEntry> indexes)
        {
            var sb = new StringBuilder();
            foreach (var i in indexes)
                sb.AppendLine(i.ToString());
            return sb.ToString();
        }

        internal virtual IEnumerable<SearchResult> SearchWord(string word, SearchIndexOperator op, SearchResultCollection subset)
        {
            var comparer = new SearchIndexComparer(op);
            int crc = word.Length;
            if (op == SearchIndexOperator.Equal)
                crc = int.MaxValue;

            return SearchIndexes(word.GetHashCode(), crc, SearchIndexEntry.Type.Word, comparer, subset);
        }

        private IEnumerable<SearchResult> ExcludeWord(string word, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchWord(word, op, null));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        private IEnumerable<SearchResult> ExcludeProperty(string name, string value, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchProperty(name, value, op, null));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        private IEnumerable<SearchResult> SearchProperty(string name, string value, SearchIndexOperator op, SearchResultCollection subset)
        {
            var comparer = new SearchIndexComparer(op);
            var valueHash = value.GetHashCode();
            var nameHash = name.GetHashCode();
            if (comparer.op == SearchIndexOperator.Equal)
            {
                nameHash ^= name.Length.GetHashCode();
                valueHash ^= value.Length.GetHashCode();
            }

            return SearchIndexes(valueHash, nameHash, SearchIndexEntry.Type.Property, comparer, subset);
        }

        private SearchResultCollection GetAllDocumentIndexesSet()
        {
            if (m_AllDocumentIndexes != null)
                return m_AllDocumentIndexes;
            m_AllDocumentIndexes = new SearchResultCollection();
            for (int i = 0; i < documentCount; ++i)
                m_AllDocumentIndexes.Add(new SearchResult(i, 0));
            return m_AllDocumentIndexes;
        }

        private IEnumerable<SearchResult> ExcludeNumber(string name, double number, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchNumber(name, number, op, null).Select(m => new SearchResult(m.index, m.score)));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        private IEnumerable<SearchResult> SearchNumber(string key, double value, SearchIndexOperator op, SearchResultCollection subset)
        {
            var wiec = new SearchIndexComparer(op);
            return SearchIndexes(BitConverter.DoubleToInt64Bits(value), key.GetHashCode(), SearchIndexEntry.Type.Number, wiec, subset);
        }

        private IEnumerable<SearchResult> SearchTerm(
            string name, in object value, SearchIndexOperator op, bool exclude, SearchResultCollection subset = null)
        {
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
                if (value is double)
                {
                    number = (double)value;
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
                return new SearchResult(id, r.index, r.score);
            });
        }

        private IEnumerable<SearchResult> SearchDocumentWords(string word, SearchResultCollection subset)
        {
            if (resolveDocumentHandler == null)
                yield break;
            foreach (var r in subset ?? GetAllDocumentIndexesSet())
            {
                var doc = GetDocument(r.index);
                if (doc.valid)
                {
                    var resolvedDocumentString = resolveDocumentHandler(doc.id);
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

        private int SortTokensByPatternMatches(string item1, string item2)
        {
            m_PatternMatchCount.TryGetValue(item1.GetHashCode(), out var item1PatternMatchCount);
            m_PatternMatchCount.TryGetValue(item2.GetHashCode(), out var item2PatternMatchCount);
            var c = item1PatternMatchCount.CompareTo(item2PatternMatchCount);
            if (c != 0)
                return c;
            return item1.Length.CompareTo(item2.Length);
        }

        internal void SaveIndexToDisk(string indexFilePath)
        {
            if (String.IsNullOrEmpty(indexFilePath))
                return;

            var indexTempFilePath = Path.GetTempFileName();
            using (var fileStream = new FileStream(indexTempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                Write(fileStream);

            try
            {
                try
                {
                    if (File.Exists(indexFilePath))
                        File.Delete(indexFilePath);
                }
                catch (IOException)
                {
                    // ignore file index persistence operation, since it is not critical and will redone later.
                }

                File.Move(indexTempFilePath, indexFilePath);
            }
            catch (IOException)
            {
                // ignore file index persistence operation, since it is not critical and will redone later.
            }
        }

        internal bool ReadIndexFromDisk(string indexFilePath, bool checkVersionOnly = false)
        {
            using (var fileStream = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Read(fileStream, checkVersionOnly);
        }

        internal bool LoadIndexFromDisk(string indexFilePath, bool useThread = false)
        {
            if (indexFilePath == null || !File.Exists(indexFilePath))
                return false;

            if (useThread)
            {
                if (!ReadIndexFromDisk(indexFilePath, true))
                    return false;

                var t = new Thread(() => ReadIndexFromDisk(indexFilePath));
                t.Start();
                return t.ThreadState != System.Threading.ThreadState.Unstarted;
            }

            return ReadIndexFromDisk(indexFilePath);
        }

        private void AbortIndexing()
        {
            if (m_IndexReady)
                return;
        }

        private bool UpdateIndexesOnFinish()
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
                            if (enumerable is SearchNativeReadOnlyArray<SearchIndexEntry> {Allocator: Allocator.Temp} na)
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

        private void ApplyIndexes(ConcurrentDictionary<int, SearchDocument> documents, SearchNativeReadOnlyArray<SearchIndexEntry> entries, SearchDocumentListTable documentListTable, GCHandle documentListTableHandle, ConcurrentDictionary<string, Hash128> hashes, ConcurrentDictionary<string, string> metainfos)
        {
            m_Documents = documents;
            m_SourceDocuments = hashes;
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

        private IEnumerable<SearchResult> SearchIndexes(
            long key, int crc, SearchIndexEntry.Type type, SearchIndexComparer comparer, SearchResultCollection subset)
        {
            if (subset != null && subset.Count == 0)
                return Enumerable.Empty<SearchResult>();

            // Find a first match in the sorted indexes.
            var matchKey = new SearchIndexEntry(key, crc, type);
            int foundIndex = m_Indexes.AsReadOnlySpan().BinarySearch(matchKey, comparer);
            return SearchRange(foundIndex, matchKey, comparer, subset);
        }

        private string[] ParseQuery(string query)
        {
            return Regex.Matches(query, @"([\!]*([\""](.+?)[\""]|[^\s_\/]))+").Cast<Match>()
                .Select(m => m.Value.Replace("\"", "").ToLowerInvariant())
                .Where(t => t.Length > 0)
                .OrderBy(t => - t.Length)
                .ToArray();
        }

        readonly struct IndexRange
        {
            public readonly int start;
            public readonly int end;

            public IndexRange(int s, int e)
            {
                start = s;
                end = e;
            }

            public bool valid => start != -1;

            public static IndexRange Invalid = new IndexRange(-1, -1);
        }

        readonly struct RangeSet : IEquatable<RangeSet>
        {
            public readonly SearchIndexEntry.Type type;
            public readonly int crc;

            public RangeSet(SearchIndexEntry.Type type, int crc)
            {
                this.type = type;
                this.crc = crc;
            }

            public override int GetHashCode() => (type, crc).GetHashCode();
            public override bool Equals(object other) => other is RangeSet l && Equals(l);
            public bool Equals(RangeSet other) => type == other.type && crc == other.crc;
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
