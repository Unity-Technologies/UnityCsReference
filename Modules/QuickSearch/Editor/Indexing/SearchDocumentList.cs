// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEditor.Search
{
    //The SearchDocumentList must be kept sorted most of the time,
    // since many workflows expect a sorted document list to perform optimally.
    readonly struct SearchDocumentList
    {
        readonly int m_Doc1;
        readonly int m_Doc2;
        readonly int m_Doc3;
        readonly int m_Doc4;

        readonly int m_DocTableSymbol;
        readonly IntPtr m_DocumentListTable;

        public static int size => 6 * sizeof(int) + IntPtr.Size; // 6 ints, because Count has a backing private field that we cannot ignore.

        public int Count { get; }
        public SearchDocumentListTable Table
        {
            get
            {
                if (m_DocumentListTable == IntPtr.Zero)
                    return null;
                var h = GCHandle.FromIntPtr(m_DocumentListTable);
                return h.Target as SearchDocumentListTable;
            }
        }

        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (Count <= 4)
                {
                    return GetEmbeddedDocumentUnsafe(index);
                }

                return Table.GetContent(m_DocTableSymbol)[index];
            }
        }

        public SearchDocumentList()
        {
            m_Doc1 = SearchIndexer.invalidDocumentIndex;
            m_Doc2 = SearchIndexer.invalidDocumentIndex;
            m_Doc3 = SearchIndexer.invalidDocumentIndex;
            m_Doc4 = SearchIndexer.invalidDocumentIndex;
            m_DocTableSymbol = SearchDocumentListTable.tableFullSymbol;
            m_DocumentListTable = IntPtr.Zero;
            Count = 0;
        }

        public SearchDocumentList(int doc1)
        {
            m_Doc1 = doc1;
            m_Doc2 = SearchIndexer.invalidDocumentIndex;
            m_Doc3 = SearchIndexer.invalidDocumentIndex;
            m_Doc4 = SearchIndexer.invalidDocumentIndex;
            m_DocTableSymbol = SearchDocumentListTable.tableFullSymbol;
            m_DocumentListTable = IntPtr.Zero;
            Count = 1;
        }

        public SearchDocumentList(int doc1, int doc2)
        {
            m_Doc1 = doc1;
            m_Doc2 = doc2;
            m_Doc3 = SearchIndexer.invalidDocumentIndex;
            m_Doc4 = SearchIndexer.invalidDocumentIndex;
            m_DocTableSymbol = SearchDocumentListTable.tableFullSymbol;
            m_DocumentListTable = IntPtr.Zero;
            Count = 2;
        }

        public SearchDocumentList(int doc1, int doc2, int doc3)
        {
            m_Doc1 = doc1;
            m_Doc2 = doc2;
            m_Doc3 = doc3;
            m_Doc4 = SearchIndexer.invalidDocumentIndex;
            m_DocTableSymbol = SearchDocumentListTable.tableFullSymbol;
            m_DocumentListTable = IntPtr.Zero;
            Count = 3;
        }

        public SearchDocumentList(int doc1, int doc2, int doc3, int doc4)
        {
            m_Doc1 = doc1;
            m_Doc2 = doc2;
            m_Doc3 = doc3;
            m_Doc4 = doc4;
            m_DocTableSymbol = SearchDocumentListTable.tableFullSymbol;
            m_DocumentListTable = IntPtr.Zero;
            Count = 4;
        }

        public SearchDocumentList(int documentListTableSymbol, SearchDocumentListTable documentListTable, GCHandle tableHandle)
        {
            if (documentListTable == null)
                throw new ArgumentNullException(nameof(documentListTable));
            if (!tableHandle.IsAllocated)
                throw new ArgumentException("SearchDocumentListTable handle is not allocated", nameof(tableHandle));
            m_Doc1 = SearchIndexer.invalidDocumentIndex;
            m_Doc2 = SearchIndexer.invalidDocumentIndex;
            m_Doc3 = SearchIndexer.invalidDocumentIndex;
            m_Doc4 = SearchIndexer.invalidDocumentIndex;
            m_DocTableSymbol = documentListTableSymbol;
            m_DocumentListTable = GCHandle.ToIntPtr(tableHandle);
            Count = documentListTable.GetContent(documentListTableSymbol).Length;
        }

        private SearchDocumentList(int documentListTableSymbol, SearchDocumentListTable documentListTable, IntPtr documentListTablePtr)
        {
            m_Doc1 = SearchIndexer.invalidDocumentIndex;
            m_Doc2 = SearchIndexer.invalidDocumentIndex;
            m_Doc3 = SearchIndexer.invalidDocumentIndex;
            m_Doc4 = SearchIndexer.invalidDocumentIndex;
            m_DocTableSymbol = documentListTableSymbol;
            m_DocumentListTable = documentListTablePtr;
            Count = documentListTable.GetContent(documentListTableSymbol).Length;
        }

        public SearchDocumentList RemapDocuments(Dictionary<int, int> updatedDocIndexes, bool keepSorted = false)
        {
            if (Count == 0 || Count > 4) // Nothing to do if using the table
                return this;

            Span<int> values = stackalloc int[4];
            var index = 0;
            for (var i = 0; i < Count; ++i)
            {
                var doc = GetEmbeddedDocumentUnsafe(i);
                values[index++] = updatedDocIndexes.TryGetValue(doc, out var newDoc) ? newDoc : doc;
            }

            switch (Count)
            {
                case 1:
                    return new SearchDocumentList(values[0]);
                case 2:
                    if (keepSorted) Sort(values.Slice(0, 2));
                    return new SearchDocumentList(values[0], values[1]);
                case 3:
                    if (keepSorted) Sort(values.Slice(0, 3));
                    return new SearchDocumentList(values[0], values[1], values[2]);
                case 4:
                    if (keepSorted) Sort(values);
                    return new SearchDocumentList(values[0], values[1], values[2], values[3]);
                default:
                    throw new InvalidOperationException("Invalid document collection size");
            }
        }

        public SearchDocumentList RemoveDocuments(HashSet<int> removedDocuments)
        {
            if (removedDocuments == null)
                return this;
            if (Count < 0)
                return this;

            if (Count > 4)
            {
                var content = Table.GetContent(m_DocTableSymbol);
                if (content.Length > 4)
                    return new SearchDocumentList(m_DocTableSymbol, Table, m_DocumentListTable);
                var contentSpan = content;
                switch (content.Length)
                {
                    case 0:
                        return new SearchDocumentList();
                    case 1:
                        return new SearchDocumentList(contentSpan[0]);
                    case 2:
                        return new SearchDocumentList(contentSpan[0], contentSpan[1]);
                    case 3:
                        return new SearchDocumentList(contentSpan[0], contentSpan[1], contentSpan[2]);
                    case 4:
                        return new SearchDocumentList(contentSpan[0], contentSpan[1], contentSpan[2], contentSpan[3]);
                }
            }

            Span<int> values = stackalloc int[4];
            var index = 0;
            for (var i = 0; i < Count; ++i)
            {
                var current = this[i];
                if (!removedDocuments.Contains(current))
                    values[index++] = current;
            }

            switch (index)
            {
                case 0:
                    return new SearchDocumentList();
                case 1:
                    return new SearchDocumentList(values[0]);
                case 2:
                    return new SearchDocumentList(values[0], values[1]);
                case 3:
                    return new SearchDocumentList(values[0], values[1], values[2]);
                case 4:
                    return new SearchDocumentList(values[0], values[1], values[2], values[3]);
            }

            return this;
        }

        public void ToBinary(BinaryWriter bw)
        {
            bw.Write(m_Doc1);
            bw.Write(m_Doc2);
            bw.Write(m_Doc3);
            bw.Write(m_Doc4);
            bw.Write(m_DocTableSymbol);
        }

        public static SearchDocumentList FromBinary(BinaryReader br, SearchDocumentListTable documentListTable, GCHandle documentListTableHandle)
        {
            var doc1 = br.ReadInt32();
            var doc2 = br.ReadInt32();
            var doc3 = br.ReadInt32();
            var doc4 = br.ReadInt32();
            var docTableSymbol = br.ReadInt32();

            return new SearchDocumentList(doc1, doc2, doc3, doc4, docTableSymbol, documentListTable, documentListTableHandle);
        }

        public void ToDocumentCollection(SortedSet<int> documentCollection)
        {
            if (Count <= 4)
            {
                for (var i = 0; i < Count; ++i)
                {
                    documentCollection.Add(GetEmbeddedDocumentUnsafe(i));
                }
            }
            else
            {
                var span = Table.GetContent(m_DocTableSymbol);
                for (var i = 0; i < Count; ++i)
                {
                    documentCollection.Add(span[i]);
                }
            }
        }

        // This methods assumes the doc collection is sorted.
        public static SearchDocumentList FromDocumentCollection(IReadOnlyCollection<int> docCollection, SearchDocumentListTable documentListTable, GCHandle documentListTableHandle)
        {
            if (documentListTable == null)
                throw new ArgumentNullException(nameof(documentListTable));
            if (!documentListTableHandle.IsAllocated)
                throw new ArgumentException("SearchDocumentListTable handle is not allocated", nameof(documentListTableHandle));
            if (docCollection == null || docCollection.Count == 0)
                return new SearchDocumentList();

            if (docCollection.Count > 4)
            {
                var symbol = documentListTable.ToSymbol(docCollection);
                return new SearchDocumentList(symbol, documentListTable, documentListTableHandle);
            }

            Span<int> values = stackalloc int[4];
            var index = 0;
            foreach (var doc in docCollection)
            {
                values[index++] = doc;
            }

            switch (docCollection.Count)
            {
                case 1:
                    return new SearchDocumentList(values[0]);
                case 2:
                    return new SearchDocumentList(values[0], values[1]);
                case 3:
                    return new SearchDocumentList(values[0], values[1], values[2]);
                case 4:
                    return new SearchDocumentList(values[0], values[1], values[2], values[3]);
                default:
                    throw new InvalidOperationException("Invalid document collection size");
            }
        }

        public EmbeddedEnumerator GetEmbeddedEnumerator()
        {
            return new EmbeddedEnumerator(this);
        }

        public TableEnumerator GetTableEnumerator()
        {
            return new TableEnumerator(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Count; ++i)
            {
                sb.Append(this[i]);
                if (i < Count - 1)
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        SearchDocumentList(int doc1, int doc2, int doc3, int doc4, int documentListSymbolTable, SearchDocumentListTable documentListTable, GCHandle documentListTableHandle)
        {
            m_Doc1 = doc1;
            m_Doc2 = doc2;
            m_Doc3 = doc3;
            m_Doc4 = doc4;
            m_DocTableSymbol = documentListSymbolTable;
            m_DocumentListTable = GCHandle.ToIntPtr(documentListTableHandle);
            if (documentListSymbolTable != SearchDocumentListTable.tableFullSymbol)
                Count = documentListTable.GetContent(documentListSymbolTable).Length;
            else if (doc4 != SearchIndexer.invalidDocumentIndex)
            {
                Count = 4;
            }
            else if (doc3 != SearchIndexer.invalidDocumentIndex)
            {
                Count = 3;
            }
            else if (doc2 != SearchIndexer.invalidDocumentIndex)
            {
                Count = 2;
            }
            else if (doc1 != SearchIndexer.invalidDocumentIndex)
            {
                Count = 1;
            }
            else
            {
                Count = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetEmbeddedDocumentUnsafe(int index)
        {
            switch (index)
            {
                case 0:
                    return m_Doc1;
                case 1:
                    return m_Doc2;
                case 2:
                    return m_Doc3;
                case 3:
                    return m_Doc4;
                default:
                    return SearchIndexer.invalidDocumentIndex;
            }
        }

        // TODO: Optimize this. Or remove
        static void Sort(Span<int> docs)
        {
            if (docs.Length < 2)
                return;

            if (docs.Length == 2)
            {
                if (docs[0] > docs[1])
                {
                    (docs[0], docs[1]) = (docs[1], docs[0]);
                }
                return;
            }

            if (docs.Length == 3)
            {
                if (docs[0] > docs[1])
                {
                    (docs[0], docs[1]) = (docs[1], docs[0]);
                }

                if (docs[1] > docs[2])
                {
                    (docs[1], docs[2]) = (docs[2], docs[1]);
                }

                if (docs[0] > docs[1])
                {
                    (docs[0], docs[1]) = (docs[1], docs[0]);
                }
                return;
            }

            if (docs.Length == 4)
            {
                if (docs[0] > docs[1])
                {
                    (docs[0], docs[1]) = (docs[1], docs[0]);
                }

                if (docs[1] > docs[2])
                {
                    (docs[1], docs[2]) = (docs[2], docs[1]);
                }

                if (docs[2] > docs[3])
                {
                    (docs[2], docs[3]) = (docs[3], docs[2]);
                }

                if (docs[0] > docs[1])
                {
                    (docs[0], docs[1]) = (docs[1], docs[0]);
                }

                if (docs[1] > docs[2])
                {
                    (docs[1], docs[2]) = (docs[2], docs[1]);
                }

                if (docs[0] > docs[1])
                {
                    (docs[0], docs[1]) = (docs[1], docs[0]);
                }
                return;
            }
        }

        public struct EmbeddedEnumerator
        {
            SearchDocumentList m_DocumentList;
            int m_Index;

            public EmbeddedEnumerator(SearchDocumentList documentList)
            {
                m_DocumentList = documentList;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_DocumentList.Count;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public int Current => m_DocumentList.GetEmbeddedDocumentUnsafe(m_Index);
        }

        public struct TableEnumerator
        {
            SearchDocumentList m_DocumentList;
            int m_Index;

            public TableEnumerator(SearchDocumentList documentList)
            {
                m_DocumentList = documentList;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_DocumentList.Count;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public int Current => m_DocumentList.Table.GetContent(m_DocumentList.m_DocTableSymbol)[m_Index];
        }
    }
}
