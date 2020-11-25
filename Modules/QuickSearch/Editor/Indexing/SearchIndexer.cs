// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        internal const int version = 0x4242E000 | 0x014;

        public enum Type : int
        {
            Undefined = 0,
            Word,
            Number,
            Property
        }

        public readonly long key;      // Value hash
        public readonly int crc;       // Value correction code (can be length, property key hash, etc.)
        public readonly Type type;     // Type of the index entry
        public readonly int index;     // Index of documents in the documents array
        public readonly int score;
        public readonly double number;

        public SearchIndexEntry(long _key, int _crc, Type _type, int _index = -1, int _score = int.MaxValue)
        {
            key = _key;
            crc = _crc;
            type = _type;
            index = _index;
            score = _score;
            number = BitConverter.Int64BitsToDouble(key);
        }

        public override string ToString()
        {
            if (type == Type.Number)
                return $"[{index}, N] {crc}:{number} ({score})";
            else if (type == Type.Property)
                return $"[{index}, P] {crc}:{key} ({score})";
            return $"[{index}, W] {key},{crc} ({score})";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return key.GetHashCode() ^ crc.GetHashCode() ^ type.GetHashCode() ^ index.GetHashCode();
            }
        }

        public override bool Equals(object other)
        {
            return other is SearchIndexEntry l && Equals(l);
        }

        public bool Equals(SearchIndexEntry other)
        {
            return key == other.key && crc == other.crc && type == other.type && index == other.index;
        }
    }

    /// <summary>
    /// Encapsulate an element that was retrieved from a query in a g.
    /// </summary>
    [DebuggerDisplay("{id}[{index}] ({score})")]
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
        public bool valid => index != -1;

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

        internal SearchResult(in SearchIndexEntry entry)
        {
            this.id = null;
            this.index = entry.index;
            this.score = entry.score;
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
    }

    /// <summary>
    /// Represents a searchable document that has been indexed.
    /// </summary>
    public readonly struct SearchDocument : IEquatable<SearchDocument>, IComparable<SearchDocument>
    {
        public readonly string id;
        public readonly int index;
        public readonly int score;
        private readonly string m_Path;

        public string path => m_Path ?? id;
        public bool valid => !string.IsNullOrEmpty(id);

        /// <summary>
        /// Create a new SearchDocument
        /// </summary>
        /// <param name="id">Document Id</param>
        /// <param name="metadata">Additional data about this document</param>
        public SearchDocument(int index, string id, string path = null, int score = int.MaxValue)
        {
            this.id = id;
            this.index = index;
            this.score = score;
            m_Path = path;
        }

        public SearchDocument(SearchDocument doc, int score)
        {
            this.id = doc.id;
            this.index = doc.index;
            this.score = score;
            m_Path = doc.m_Path;
        }

        public SearchDocument(SearchDocument doc, string path)
        {
            this.id = doc.id;
            this.index = doc.index;
            this.score = doc.score;
            m_Path = path;
        }

        /// <summary>
        /// Returns the document id string.
        /// </summary>
        /// <returns>Returns a string representation of the Document.</returns>
        public override string ToString()
        {
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
            return path.CompareTo(other.path);
        }
    }

    /// <summary>
    /// Base class for an Indexer of document which allow retrieving of a document given a specific pattern in roughly log(n).
    /// </summary>
    public class SearchIndexer
    {
        /// <summary>
        /// Name of the index. Generally this name is given by a user from a <see cref="SearchDatabase.Settings"/>
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Time indicating when the index was saved
        /// </summary>
        internal long timestamp => m_Timestamp;

        /// <summary>
        /// Returns how many keywords the index contains.
        /// </summary>
        public int keywordCount => m_Keywords.Count;

        /// <summary>
        /// Returns how many documents the index contains.
        /// </summary>
        public int documentCount => m_Documents.Count;

        internal int indexCount
        {
            get
            {
                lock (this)
                {
                    int total = 0;
                    if (m_Indexes != null && m_Indexes.Length > 0)
                        total += m_Indexes.Length;
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
        /// Handler used to parse and split the search query text into words. The tokens needs to be split similarly to words and properties are indexed.
        /// </summary>
        public Func<string, string[]> getQueryTokensHandler { get; set; }

        /// <summary>
        /// Handler used to resolve a document id to some other data string.
        /// </summary>
        public Func<string, string> resolveDocumentHandler { get; set; }

        /// <summary>
        /// Minimal indexed word size.
        /// </summary>
        public int minWordIndexationLength { get; set; } = 2;

        /// <summary>
        /// Is the current indexing thread aborted.
        /// </summary>
        protected volatile bool m_ThreadAborted = false;
        private Thread m_IndexerThread;
        private volatile bool m_IndexReady = false;
        private long m_Timestamp;

        private readonly QueryEngine<SearchResult> m_QueryEngine = new QueryEngine<SearchResult>(validateFilters: false);
        private readonly Dictionary<string, Query<SearchResult, object>> m_QueryPool = new Dictionary<string, Query<SearchResult, object>>();

        private readonly Dictionary<RangeSet, IndexRange> m_FixedRanges;
        private SearchResultCollection m_AllDocumentIndexes;
        private readonly Dictionary<int, int> m_PatternMatchCount;

        // Temporary documents and entries while the index is being built (i.e. Start/Finish).
        private readonly List<SearchIndexEntry> m_BatchIndexes;

        // Final documents and entries when the index is ready.
        private List<SearchDocument> m_Documents;
        private SearchIndexEntry[] m_Indexes;
        private HashSet<string> m_Keywords;
        private Dictionary<string, Hash128> m_SourceDocuments;
        private Dictionary<string, string> m_MetaInfo;

        protected Dictionary<string, int> m_IndexByDocuments;

        /// <summary>
        /// Create a new default SearchIndexer.
        /// </summary>
        public SearchIndexer()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Create a new SearchIndexer.
        /// </summary>
        /// <param name="name">Name of the indexer</param>
        public SearchIndexer(string name)
        {
            this.name = name;

            skipEntryHandler = e => false;
            getQueryTokensHandler = ParseQuery;

            m_Keywords = new HashSet<string>();
            m_Documents = new List<SearchDocument>();
            m_IndexByDocuments = new Dictionary<string, int>();
            m_Indexes = new SearchIndexEntry[0];
            m_BatchIndexes = new List<SearchIndexEntry>();
            m_PatternMatchCount = new Dictionary<int, int>();
            m_FixedRanges = new Dictionary<RangeSet, IndexRange>();
            m_SourceDocuments = new Dictionary<string, Hash128>();
            m_MetaInfo = new Dictionary<string, string>();

            m_QueryEngine.SetSearchDataCallback(e => null, s => s.Length < minWordIndexationLength ? null : s, StringComparison.Ordinal);
        }

        private Query<SearchResult, object> BuildQuery(string searchQuery, int maxScore, int patternMatchLimit)
        {
            Query<SearchResult, object> query;
            if (m_QueryPool.TryGetValue(searchQuery, out query) && query.valid)
                return query;

            if (m_QueryPool.Count > 50)
                m_QueryPool.Clear();

            query = m_QueryEngine.Parse(searchQuery, new SearchIndexerQueryFactory(args => EvaluateSearchNode(args, maxScore, patternMatchLimit)));
            if (query.valid)
                m_QueryPool[searchQuery] = query;
            return query;
        }

        private SearchQueryEvaluator<SearchResult>.EvalResult EvaluateSearchNode(SearchQueryEvaluator<SearchResult>.EvalHandlerArgs args, int maxScore, int patternMatchLimit)
        {
            if (args.op == SearchIndexOperator.None)
                return SearchIndexerQuery.EvalResult.None;

            SearchResultCollection subset = null;
            if (args.andSet != null)
                subset = new SearchResultCollection(args.andSet);
            var results = SearchTerm(args.name, args.value, args.op, args.exclude, maxScore, subset, patternMatchLimit) ?? Enumerable.Empty<SearchResult>();

            if (args.orSet != null)
                results = results.Concat(args.orSet);

            return SearchIndexerQuery.EvalResult.Combined(results);
        }

        /// <summary>
        /// Build custom derived indexes.
        /// </summary>
        public virtual void Build()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="score">Relevance score of the word.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        public void AddWord(string word, int score, int documentIndex)
        {
            lock (this)
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
            lock (this)
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
            lock (this)
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
            lock (this)
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
            lock (this)
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
            lock (this)
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
            lock (this)
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
            lock (this)
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

            var parsedQuery = BuildQuery(query, maxScore, patternMatchLimit);
            if (!parsedQuery.valid)
            {
                context.AddSearchQueryErrors(parsedQuery.errors.Select(e => new SearchQueryError(e.index, e.length, e.reason, context, provider)));
                return Enumerable.Empty<SearchResult>();
            }
            return parsedQuery.Apply(null).OrderBy(e => e.score).Distinct();
        }

        /// <summary>
        /// Write a binary representation of the the index on a stream.
        /// </summary>
        /// <param name="stream">Stream where to write the index.</param>
        public void Write(Stream stream)
        {
            using (var indexWriter = new BinaryWriter(stream))
            {
                m_Timestamp = DateTime.Now.ToBinary();

                indexWriter.Write(SearchIndexEntry.version);
                indexWriter.Write(timestamp);

                // Documents
                indexWriter.Write(m_Documents.Count);
                foreach (var doc in m_Documents)
                {
                    indexWriter.Write(doc.valid ? doc.id ?? string.Empty : string.Empty);
                    indexWriter.Write(doc.valid ? doc.path ?? string.Empty : string.Empty);
                }

                // Hashes
                indexWriter.Write(m_SourceDocuments.Count);
                foreach (var kvp in m_SourceDocuments)
                {
                    indexWriter.Write(kvp.Key);
                    indexWriter.Write(kvp.Value.ToString());
                }

                // Metainfo
                indexWriter.Write(m_MetaInfo.Count);
                foreach (var kvp in m_MetaInfo)
                {
                    indexWriter.Write(kvp.Key);
                    indexWriter.Write(kvp.Value);
                }

                // Indexes
                indexWriter.Write(m_Indexes.Length);
                foreach (var p in m_Indexes)
                {
                    indexWriter.Write(p.key);
                    indexWriter.Write(p.crc);
                    indexWriter.Write((int)p.type);
                    indexWriter.Write(p.index);
                    indexWriter.Write(p.score);
                }

                // Keywords
                indexWriter.Write(m_Keywords.Count);
                foreach (var t in m_Keywords)
                    indexWriter.Write(t);
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

                // Documents
                var elementCount = indexReader.ReadInt32();
                var documents = new SearchDocument[elementCount];
                for (int i = 0; i < elementCount; ++i)
                {
                    var docId = indexReader.ReadString();
                    var docPath = indexReader.ReadString();
                    if (string.IsNullOrEmpty(docId))
                    {
                        documents[i] = default;
                    }
                    else
                    {
                        documents[i] = new SearchDocument(i, docId, docPath);
                    }
                }

                // Hashes
                elementCount = indexReader.ReadInt32();
                var hashes = new Dictionary<string, Hash128>();
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = indexReader.ReadString();
                    var hash = Hash128.Parse(indexReader.ReadString());
                    hashes[key] = hash;
                }

                // Metainfos
                elementCount = indexReader.ReadInt32();
                var metainfos = new Dictionary<string, string>();
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = indexReader.ReadString();
                    var value = indexReader.ReadString();
                    metainfos[key] = value;
                }

                // Indexes
                elementCount = indexReader.ReadInt32();
                var indexes = new List<SearchIndexEntry>(elementCount);
                for (int i = 0; i < elementCount; ++i)
                {
                    var key = indexReader.ReadInt64();
                    var crc = indexReader.ReadInt32();
                    var type = (SearchIndexEntry.Type)indexReader.ReadInt32();
                    var index = indexReader.ReadInt32();
                    var score = indexReader.ReadInt32();
                    indexes.Add(new SearchIndexEntry(key, crc, type, index, score));
                }

                // Keywords
                elementCount = indexReader.ReadInt32();
                var keywords = new string[elementCount];
                for (int i = 0; i < elementCount; ++i)
                    keywords[i] = indexReader.ReadString();

                // No need to sort the index, it is already sorted in the file stream.
                lock (this)
                {
                    ApplyIndexes(documents, indexes.ToArray(), hashes, metainfos);
                    m_Keywords = new HashSet<string>(keywords);
                }

                return true;
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
            return t.ThreadState != System.Threading.ThreadState.Unstarted;
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

        internal void CombineIndexes(SearchIndexer si, int baseScore = 0, Action<int, SearchIndexer> documentIndexing = null)
        {
            int sourceIndex = 0;
            lock (this)
            {
                foreach (var doc in si.m_Documents)
                {
                    var di = AddDocument(doc.id, doc.path, false);
                    documentIndexing?.Invoke(di, this);
                    m_BatchIndexes.AddRange(si.m_Indexes.Where(i => i.index == sourceIndex)
                        .Select(i => new SearchIndexEntry(i.key, i.crc, i.type, di, baseScore + i.score)));
                    sourceIndex++;
                }

                m_Keywords.UnionWith(si.m_Keywords);
                foreach (var hkvp in si.m_SourceDocuments)
                    m_SourceDocuments[hkvp.Key] = hkvp.Value;

                foreach (var mikvp in si.m_MetaInfo)
                    m_MetaInfo[mikvp.Key] = mikvp.Value;
            }
        }

        internal int FindDocumentIndex(string id)
        {
            if (m_IndexByDocuments.TryGetValue(id, out var di))
                return di;
            return m_Documents.FindIndex(d => d.valid && d.id.Equals(id, StringComparison.Ordinal));
        }

        internal void Merge(string[] removeDocuments, SearchIndexer si, int baseScore = 0, Action<int, SearchIndexer> documentIndexing = null)
        {
            var progressId = Progress.Start("Merging indexes");

            int[] removeDocIndexes = null;
            int[] updatedDocIndexes = null;
            List<SearchIndexEntry> indexes = null;
            lock (this)
            {
                removeDocIndexes = removeDocuments.Select(FindDocumentIndex).Where(i => i != -1).ToArray();
                foreach (var idi in removeDocIndexes)
                    m_Documents[idi] = default;
                updatedDocIndexes = si.m_Documents.Select(d => FindDocumentIndex(d.id)).ToArray();

                var ignoreDocuments = removeDocIndexes.Concat(updatedDocIndexes.Where(i => i != -1)).OrderBy(i => i).ToArray();
                indexes = new List<SearchIndexEntry>(m_Indexes.Where(e => m_Documents[e.index].valid && Array.BinarySearch(ignoreDocuments, e.index) < 0));
            }

            if (si.documentCount > 0)
            {
                var sourceIndexes = new List<SearchIndexEntry>(si.m_Indexes);
                int sourceIndex = 0;
                var wiec = new SearchIndexComparer();
                var count = (float)si.documentCount;
                foreach (var doc in si.m_Documents)
                {
                    Progress.SetDescription(progressId, doc.id);
                    documentIndexing?.Invoke(sourceIndex, si);

                    var di = updatedDocIndexes[sourceIndex];
                    if (di == -1)
                        di = AddDocument(doc.id, doc.path, false);

                    foreach (var sie in sourceIndexes.Concat(si.m_BatchIndexes).Where(i => i.index == sourceIndex))
                    {
                        var ne = new SearchIndexEntry(sie.key, sie.crc, sie.type, di, baseScore + sie.score);
                        var insertAt = indexes.BinarySearch(ne, wiec);
                        if (insertAt < 0) insertAt = ~insertAt;
                        indexes.Insert(insertAt, ne);
                    }

                    sourceIndexes.RemoveAll(e => e.index == sourceIndex);
                    si.m_BatchIndexes.Clear();
                    sourceIndex++;

                    Progress.Report(progressId, sourceIndex / count);
                }
            }

            lock (this)
            {
                m_IndexReady = false;

                m_Indexes = indexes.ToArray();
                m_Keywords.UnionWith(si.m_Keywords);
                foreach (var hkvp in si.m_SourceDocuments)
                    m_SourceDocuments[hkvp.Key] = hkvp.Value;
                foreach (var mikvp in si.m_MetaInfo)
                    m_MetaInfo[mikvp.Key] = mikvp.Value;
                m_Timestamp = DateTime.Now.ToBinary();
                m_IndexReady = true;

                BuildDocumentIndexTable();
            }

            Progress.Finish(progressId, Progress.Status.Succeeded);
        }

        private void BuildDocumentIndexTable()
        {
            m_IndexByDocuments.Clear();
            for (int docIndex = 0; docIndex < m_Documents.Count; ++docIndex)
            {
                var doc = m_Documents[docIndex];
                if (!doc.valid)
                    continue;
                m_IndexByDocuments[doc.id] = docIndex;
            }
        }

        internal void ApplyFrom(SearchIndexer source)
        {
            lock (this)
                lock (source)
                {
                    m_IndexReady = false;
                    m_Timestamp = DateTime.Now.ToBinary();
                    m_Indexes = source.m_Indexes;
                    m_Documents = source.m_Documents;
                    m_Keywords = source.m_Keywords;
                    m_SourceDocuments = source.m_SourceDocuments;
                    m_MetaInfo = source.m_MetaInfo;

                    BuildDocumentIndexTable();

                    m_BatchIndexes.Clear();
                    m_IndexReady = true;
                }
        }

        internal void ApplyUnsorted()
        {
            lock (this)
                m_Indexes = m_BatchIndexes.ToArray();
        }

        internal IEnumerable<string> GetKeywords() { lock (this) return m_Keywords; }
        internal IEnumerable<SearchDocument> GetDocuments(bool ignoreNulls = false) { lock (this) return ignoreNulls ? m_Documents.Where(d => d.valid) : m_Documents; }

        /// <summary>
        /// Return a search document by its index.
        /// </summary>
        /// <param name="index">Valid index of the document to access.</param>
        /// <returns>Indexed search document</returns>
        public SearchDocument GetDocument(int index)
        {
            lock (this)
            {
                if (index < 0 || index >= m_Documents.Count)
                    return default;
                return m_Documents[index];
            }
        }

        internal bool TryGetHash(string id, out Hash128 hash)
        {
            if (!m_IndexReady)
            {
                hash = default;
                return false;
            }
            lock (this)
            {
                return m_SourceDocuments.TryGetValue(id, out hash);
            }
        }

        internal void AddWord(string word, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            AddWord(word, 2, word.Length, score, documentIndex, indexes);
        }

        /// <summary>
        /// Add a new document to be indexed.
        /// </summary>
        /// <param name="document">Unique id of the document</param>
        /// <param name="checkIfExists">Pass true if this document has some chances of existing already.</param>
        /// <returns>The document index/handle used to add new index entries.</returns>
        public int AddDocument(string document, bool checkIfExists = true)
        {
            return AddDocument(document, null, checkIfExists);
        }

        internal int AddDocument(string document, string path, bool checkIfExists = true)
        {
            // Reformat entry to have them all uniformed.
            if (skipEntryHandler(document))
                return -1;

            lock (this)
            {
                if (checkIfExists)
                {
                    var di = m_Documents.FindIndex(d => d.id == document);
                    if (di >= 0)
                    {
                        var existingDoc = m_Documents[di];
                        m_Documents[di] = new SearchDocument(existingDoc, path);
                        return di;
                    }
                }
                var newDocument = new SearchDocument(m_Documents.Count, document, path);
                m_Documents.Add(newDocument);
                m_IndexByDocuments[document] = newDocument.index;
                return newDocument.index;
            }
        }

        internal void AddSourceDocument(string sourcePath, Hash128 hash)
        {
            lock (this)
            {
                m_SourceDocuments[sourcePath] = hash;
            }
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

        internal void AddExactWord(string word, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            indexes.Add(new SearchIndexEntry(word.GetHashCode(), int.MaxValue, SearchIndexEntry.Type.Word, documentIndex, score));
        }

        internal void AddWord(string word, int minVariations, int maxVariations, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            if (word == null || word.Length == 0)
                return;

            maxVariations = Math.Min(maxVariations, word.Length);

            for (int c = Math.Min(minVariations, maxVariations); c <= maxVariations; ++c)
            {
                var ss = word.Substring(0, c);
                indexes.Add(new SearchIndexEntry(ss.GetHashCode(), ss.Length, SearchIndexEntry.Type.Word, documentIndex, score));
            }

            if (word.Length > maxVariations)
                indexes.Add(new SearchIndexEntry(word.GetHashCode(), word.Length, SearchIndexEntry.Type.Word, documentIndex, score - 1));
        }

        private bool ExcludeWordVariations(string word)
        {
            if (word == "true" || word == "false")
                return true;
            return false;
        }

        internal void AddExactProperty(string name, string value, int score, int documentIndex, bool saveKeyword)
        {
            lock (this)
                AddExactProperty(name, value, score, documentIndex, m_BatchIndexes, saveKeyword);
        }

        internal void AddExactProperty(string name, string value, int score, int documentIndex, List<SearchIndexEntry> indexes, bool saveKeyword)
        {
            var nameHash = name.GetHashCode();
            var valueHash = value.GetHashCode();

            // Add an exact match for property="match"
            nameHash ^= name.Length.GetHashCode();
            valueHash ^= value.Length.GetHashCode();
            indexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, documentIndex, score - 3));

            if (saveKeyword)
                m_Keywords.Add($"{name}:{value}");
            else
                m_Keywords.Add($"{name}:");
        }

        internal void AddProperty(string name, string value, int minVariations, int maxVariations, int score, int documentIndex, List<SearchIndexEntry> indexes, bool exact, bool saveKeyword)
        {
            var nameHash = name.GetHashCode();
            var valueHash = value.GetHashCode();
            maxVariations = Math.Min(maxVariations, value.Length);
            if (minVariations > value.Length)
                minVariations = value.Length;
            if (ExcludeWordVariations(value))
                minVariations = maxVariations = value.Length;

            for (int c = Math.Min(minVariations, maxVariations); c <= maxVariations; ++c)
            {
                var ss = value.Substring(0, c);
                indexes.Add(new SearchIndexEntry(ss.GetHashCode(), nameHash, SearchIndexEntry.Type.Property, documentIndex, score + (maxVariations - c)));
            }

            if (value.Length > maxVariations)
            {
                indexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, documentIndex, score - 1));
                //UnityEngine.Debug.Log($"Add Property [{documentIndex}]: {name}, {nameHash}, {value}, {valueHash}, {score - 1}");
            }

            if (exact)
            {
                nameHash ^= name.Length.GetHashCode();
                valueHash ^= value.Length.GetHashCode();
                indexes.Add(new SearchIndexEntry(valueHash, nameHash, SearchIndexEntry.Type.Property, documentIndex, score - 3));
                //UnityEngine.Debug.Log($"Add Property [{documentIndex}]: {name}, {nameHash}, {value}, {valueHash}, {score - 3}");
            }

            if (saveKeyword)
                m_Keywords.Add($"{name}:{value}");
            else
                m_Keywords.Add($"{name}:");
        }

        internal void AddNumber(string key, double value, int score, int documentIndex, List<SearchIndexEntry> indexes)
        {
            var keyHash = key.GetHashCode();
            var longNumber = BitConverter.DoubleToInt64Bits(value);
            indexes.Add(new SearchIndexEntry(longNumber, keyHash, SearchIndexEntry.Type.Number, documentIndex, score));

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
                m_ThreadAborted = false;
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
                    m_Indexes = new SearchIndexEntry[0];
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

        internal void Finish(Action threadCompletedCallback, string[] removedDocuments)
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

        internal void Finish(Action<byte[]> threadCompletedCallback, string[] removedDocuments, bool saveBytes)
        {
            m_ThreadAborted = false;
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
                    m_ThreadAborted = true;
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

        internal void Finish(string[] removedDocuments)
        {
            lock (this)
            {
                var shouldRemoveDocuments = removedDocuments != null && removedDocuments.Length > 0;
                if (shouldRemoveDocuments)
                {
                    var removedDocIndexes = new HashSet<int>();
                    foreach (var rd in removedDocuments)
                    {
                        if (m_IndexByDocuments.TryGetValue(rd, out var di))
                            removedDocIndexes.Add(di);
                    }
                    m_BatchIndexes.AddRange(m_Indexes.Where(e => !removedDocIndexes.Contains(e.index)));
                }
                else
                {
                    m_BatchIndexes.AddRange(m_Indexes);
                }
                UpdateIndexes(m_BatchIndexes, null);
                m_BatchIndexes.Clear();
            }
        }

        internal string Print(IEnumerable<SearchIndexEntry> indexes)
        {
            var sb = new StringBuilder();
            foreach (var i in indexes)
                sb.AppendLine(i.ToString());
            return sb.ToString();
        }

        internal virtual IEnumerable<SearchResult> SearchWord(string word, SearchIndexOperator op, int maxScore, SearchResultCollection subset, int patternMatchLimit)
        {
            var comparer = new SearchIndexComparer(op);
            int crc = word.Length;
            if (op == SearchIndexOperator.Equal)
                crc = int.MaxValue;

            return SearchIndexes(word.GetHashCode(), crc, SearchIndexEntry.Type.Word, maxScore, comparer, subset, patternMatchLimit);
        }

        private IEnumerable<SearchResult> ExcludeWord(string word, SearchIndexOperator op, SearchResultCollection subset)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchWord(word, op, int.MaxValue, null, int.MaxValue));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        private IEnumerable<SearchResult> ExcludeProperty(string name, string value, SearchIndexOperator op, int maxScore, SearchResultCollection subset, int limit)
        {
            if (subset == null)
                subset = GetAllDocumentIndexesSet();

            var includedDocumentIndexes = new SearchResultCollection(SearchProperty(name, value, op, int.MaxValue, null, int.MaxValue));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        private IEnumerable<SearchResult> SearchProperty(string name, string value, SearchIndexOperator op, int maxScore, SearchResultCollection subset, int patternMatchLimit)
        {
            var comparer = new SearchIndexComparer(op);
            var valueHash = value.GetHashCode();
            var nameHash = name.GetHashCode();
            if (comparer.op == SearchIndexOperator.Equal)
            {
                nameHash ^= name.Length.GetHashCode();
                valueHash ^= value.Length.GetHashCode();
            }

            return SearchIndexes(valueHash, nameHash, SearchIndexEntry.Type.Property, maxScore, comparer, subset, patternMatchLimit);
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

            var includedDocumentIndexes = new SearchResultCollection(SearchNumber(name, number, op, int.MaxValue, null).Select(m => new SearchResult(m.index, m.score)));
            return subset.Where(d => !includedDocumentIndexes.Contains(d));
        }

        private IEnumerable<SearchResult> SearchNumber(string key, double value, SearchIndexOperator op, int maxScore, SearchResultCollection subset)
        {
            var wiec = new SearchIndexComparer(op);
            return SearchIndexes(BitConverter.DoubleToInt64Bits(value), key.GetHashCode(), SearchIndexEntry.Type.Number, maxScore, wiec, subset);
        }

        internal IEnumerable<SearchResult> SearchTerm(
            string name, object value, SearchIndexOperator op, bool exclude,
            int maxScore = int.MaxValue, SearchResultCollection subset = null, int limit = int.MaxValue)
        {
            if (op == SearchIndexOperator.NotEqual)
            {
                exclude = true;
                op = SearchIndexOperator.Equal;
            }

            IEnumerable<SearchResult> matches = null;
            if (!String.IsNullOrEmpty(name))
            {
                name = name.ToLowerInvariant();

                // Search property
                double number;
                if (value is double)
                {
                    number = (double)value;
                    matches = SearchNumber(name, number, op, maxScore, subset);
                }
                else if (value is string)
                {
                    var valueString = (string)value;
                    if (double.TryParse(valueString, out number))
                    {
                        if (!exclude && op != SearchIndexOperator.NotEqual)
                            matches = SearchNumber(name, number, op, maxScore, subset);
                        else
                            matches = ExcludeNumber(name, number, op, subset);
                    }
                    else
                    {
                        if (!exclude)
                            matches = SearchProperty(name, valueString.ToLowerInvariant(), op, maxScore, subset, limit);
                        else
                            matches = ExcludeProperty(name, valueString.ToLowerInvariant(), op, maxScore, subset, limit);
                    }
                }
                else
                    throw new ArgumentException($"value must be a number or a string", nameof(value));
            }
            else if (value is string word)
            {
                // Search word
                if (!exclude)
                    matches = SearchWord(word, op, maxScore, subset, limit).Concat(SearchDocumentWords(word, subset).Where(r => r.valid));
                else
                    matches = ExcludeWord(word, op, subset);
            }
            else
                throw new ArgumentException($"word value must be a string", nameof(value));

            if (matches == null)
                return null;
            return matches.Select(r => new SearchResult(r.id ?? (m_Documents[r.index].id ?? r.id), r.index, r.score));
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

        private void SaveIndexToDisk(string indexFilePath)
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
            lock (this)
            {
                using (var fileStream = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return Read(fileStream, checkVersionOnly);
            }
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

            m_ThreadAborted = true;
        }

        private bool UpdateIndexes(List<SearchIndexEntry> entries, Action onIndexesCreated)
        {
            if (entries == null)
                return false;

            lock (this)
            {
                m_IndexReady = false;
                var comparer = new SearchIndexComparer();

                try
                {
                    // Sort word indexes to run quick binary searches on them.
                    entries.Sort(comparer);
                    m_Indexes = entries.Distinct(comparer).ToArray();
                    onIndexesCreated?.Invoke();

                    //File.WriteAllText($"Logs/{Guid.NewGuid().ToString("N")}_{m_Indexes.Length}.txt", Print(m_Indexes));

                    m_IndexReady = true;
                }
                catch
                {
                    // This can happen while a domain reload is happening.
                    return false;
                }

                return true;
            }
        }

        private void ApplyIndexes(IEnumerable<SearchDocument> documents, SearchIndexEntry[] entries, Dictionary<string, Hash128> hashes, Dictionary<string, string> metainfos)
        {
            m_Documents = documents.ToList();
            m_SourceDocuments = hashes;
            m_MetaInfo = metainfos;
            m_Indexes = entries;
            BuildDocumentIndexTable();
            m_IndexReady = true;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool NumberCompare(SearchIndexOperator op, double d1, double d2)
        {
            if (op == SearchIndexOperator.Equal)
                return d1 == d2;
            if (op == SearchIndexOperator.Contains)
                return Mathf.Approximately((float)d1, (float)d2);
            if (op == SearchIndexOperator.Greater)
                return d1 > d2;
            if (op == SearchIndexOperator.GreaterOrEqual)
                return d1 >= d2;
            if (op == SearchIndexOperator.Less)
                return d1 < d2;
            if (op == SearchIndexOperator.LessOrEqual)
                return d1 <= d2;

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
                return NumberCompare(op, prevEntry.number, term.number);

            return prevEntry.key == term.key;
        }

        private bool Advance(int foundIndex, in SearchIndexEntry term, SearchIndexOperator op)
        {
            if (foundIndex < 0 || foundIndex >= m_Indexes.Length ||
                m_Indexes[foundIndex].crc != term.crc || m_Indexes[foundIndex].type != term.type)
                return false;

            if (term.type == SearchIndexEntry.Type.Number)
                return NumberCompare(op, m_Indexes[foundIndex].number, term.number);

            return m_Indexes[foundIndex].key == term.key;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool Lower(ref int foundIndex, in SearchIndexEntry term, SearchIndexOperator op)
        {
            if (op == SearchIndexOperator.Less || op == SearchIndexOperator.LessOrEqual)
            {
                var cont = !Advance(foundIndex, term, op);
                if (cont)
                    foundIndex--;
                return IsIndexValid(foundIndex, term.key, term.type) && cont;
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
            return foundIndex >= 0 && foundIndex < m_Indexes.Length && m_Indexes[foundIndex].crc == crc && m_Indexes[foundIndex].type == type;
        }

        private IndexRange FindRange(in SearchIndexEntry term, SearchIndexComparer comparer)
        {
            // Find a first match in the sorted indexes.
            int foundIndex = Array.BinarySearch(m_Indexes, term, comparer);
            if (foundIndex < 0 && comparer.op != SearchIndexOperator.Contains && comparer.op != SearchIndexOperator.Equal)
            {
                // Potential range insertion, only used for not exact matches
                foundIndex = (-foundIndex) - 1;
            }

            if (!IsIndexValid(foundIndex, term.crc, term.type))
                return IndexRange.Invalid;

            // Rewind to first element
            while (Lower(ref foundIndex, term, comparer.op))
                ;

            if (!IsIndexValid(foundIndex, term.crc, term.type))
                return IndexRange.Invalid;

            int startRange = foundIndex;

            // Advance to last matching element
            while (Upper(ref foundIndex, term, comparer.op))
                ;

            return new IndexRange(startRange, foundIndex);
        }

        private IndexRange FindTypeRange(int hitIndex, in SearchIndexEntry term)
        {
            if (term.type == SearchIndexEntry.Type.Word)
            {
                if (m_Indexes[0].type != SearchIndexEntry.Type.Word || m_Indexes[hitIndex].type != SearchIndexEntry.Type.Word)
                    return IndexRange.Invalid; // No words

                IndexRange range;
                var rangeSet = new RangeSet(term.type, 0);
                if (m_FixedRanges.TryGetValue(rangeSet, out range))
                    return range;

                int endRange = hitIndex;
                while (m_Indexes[endRange + 1].type == SearchIndexEntry.Type.Word)
                    endRange++;

                range = new IndexRange(0, endRange);
                m_FixedRanges[rangeSet] = range;
                return range;
            }
            else if (term.type == SearchIndexEntry.Type.Property || term.type == SearchIndexEntry.Type.Number)
            {
                if (m_Indexes[hitIndex].type != SearchIndexEntry.Type.Property)
                    return IndexRange.Invalid;

                IndexRange range;
                var rangeSet = new RangeSet(term.type, term.crc);
                if (m_FixedRanges.TryGetValue(rangeSet, out range))
                    return range;

                int startRange = hitIndex, prev = hitIndex - 1;
                while (prev >= 0 && m_Indexes[prev].type == SearchIndexEntry.Type.Property && m_Indexes[prev].crc == term.crc)
                    startRange = prev--;

                var indexCount = m_Indexes.Length;
                int endRange = hitIndex, next = hitIndex + 1;
                while (next < indexCount && m_Indexes[next].type == SearchIndexEntry.Type.Property && m_Indexes[next].crc == term.crc)
                    endRange = next++;

                range = new IndexRange(startRange, endRange);
                m_FixedRanges[rangeSet] = range;
                return range;
            }

            return IndexRange.Invalid;
        }

        private IEnumerable<SearchResult> SearchRange(
            int foundIndex, in SearchIndexEntry term,
            int maxScore, SearchIndexComparer comparer,
            SearchResultCollection subset, int limit)
        {
            if (foundIndex < 0 && comparer.op != SearchIndexOperator.Contains && comparer.op != SearchIndexOperator.Equal)
            {
                // Potential range insertion, only used for not exact matches
                foundIndex = (-foundIndex) - 1;
            }

            if (!IsIndexValid(foundIndex, term.crc, term.type))
                return Enumerable.Empty<SearchResult>();

            // Rewind to first element
            while (Lower(ref foundIndex, term, comparer.op))
                ;

            if (!IsIndexValid(foundIndex, term.crc, term.type))
                return Enumerable.Empty<SearchResult>();

            var matches = new List<SearchResult>();
            bool findAll = subset == null;
            do
            {
                var indexEntry = new SearchResult(m_Indexes[foundIndex]);
                bool intersects = findAll || subset.Contains(indexEntry);
                if (intersects && indexEntry.score < maxScore)
                {
                    if (term.type == SearchIndexEntry.Type.Number)
                        matches.Add(new SearchResult(indexEntry.index, indexEntry.score + (int)Math.Abs(term.number - m_Indexes[foundIndex].number)));
                    else
                        matches.Add(new SearchResult(indexEntry.index, indexEntry.score));

                    if (matches.Count >= limit)
                        return matches;
                }

                // Advance to last matching element
            }
            while (Upper(ref foundIndex, term, comparer.op));

            return matches;
        }

        private IEnumerable<SearchResult> SearchIndexes(
            long key, int crc, SearchIndexEntry.Type type, int maxScore,
            SearchIndexComparer comparer, SearchResultCollection subset, int limit = int.MaxValue)
        {
            if (subset != null && subset.Count == 0)
                return Enumerable.Empty<SearchResult>();

            // Find a first match in the sorted indexes.
            var matchKey = new SearchIndexEntry(key, crc, type);
            int foundIndex = Array.BinarySearch(m_Indexes, matchKey, comparer);
            return SearchRange(foundIndex, matchKey, maxScore, comparer, subset, limit);
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
