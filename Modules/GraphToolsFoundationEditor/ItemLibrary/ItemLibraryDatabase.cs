// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Search;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// ItemLibrary Database using Unity Search as a backend
    /// </summary>
    class ItemLibraryDatabase : ItemLibraryDatabaseBase
    {
        List<int> m_MatchIndicesBuffer;

        /// <summary>
        /// During one search query, stores the item being processed currently.
        /// </summary>
        ItemLibraryItem m_CurrentItemLibraryItem;

        float m_ScoreMultiplier = 1f;
        QueryEngine<ItemLibraryItem> m_QueryEngine;

        /// <summary>
        /// Instantiates an empty database.
        /// </summary>
        public ItemLibraryDatabase()
            : this(new List<ItemLibraryItem>())
        {
        }

        /// <summary>
        /// Instantiates a database with items.
        /// </summary>
        /// <param name="items">Items to populate the database.</param>
        public ItemLibraryDatabase(IReadOnlyList<ItemLibraryItem> items)
            : base(items)
        {
            SetupQueryEngine();
        }

        /// <summary>
        /// Creates a database from a serialized file.
        /// </summary>
        /// <param name="directory">Path of the directory where the database is stored.</param>
        /// <returns>A Database with items retrieved from the serialized file.</returns>
        public static ItemLibraryDatabase LoadFromFile(string directory)
        {
            var db = new ItemLibraryDatabase();
            var reader = new StreamReader(directory + k_SerializedJsonFile);
            var serializedData = reader.ReadToEnd();
            reader.Close();

            EditorJsonUtility.FromJsonOverwrite(serializedData, db);
            return db;
        }

        /// <inheritdoc/>
        public override IEnumerable<ItemLibraryItem> PerformSearch(string query,
            IReadOnlyList<ItemLibraryItem> filteredItems)
        {
            var searchQuery = m_QueryEngine.ParseQuery("\"" + query + "\""); // TODO add support for "doc:" filter?
            m_CurrentItemLibraryItem = null;
            var searchResults = searchQuery.Apply(filteredItems);
            return searchResults;
        }

        void SetupQueryEngine()
        {
            m_QueryEngine = new QueryEngine<ItemLibraryItem>();
            m_QueryEngine.SetSearchDataCallback(GetSearchData, s => s.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
            m_QueryEngine.SetSearchWordMatcher((searchWord, _, _, searchData) =>
            {
                if (m_MatchIndicesBuffer == null)
                    m_MatchIndicesBuffer = new List<int>(searchData.Length);
                else
                    m_MatchIndicesBuffer.Clear();
                long score = 0;
                var fuzzyMatch = FuzzySearch.FuzzyMatch(searchWord, searchData, ref score, m_MatchIndicesBuffer);
                if (m_CurrentItemLibraryItem != null)
                {
                    LastSearchData_Internal[m_CurrentItemLibraryItem] = new SearchData_Internal {
                        MatchedIndices = m_MatchIndicesBuffer,
                        MatchedString = searchData,
                        Score = (long)(score * m_ScoreMultiplier) };
                }
                return fuzzyMatch;
            });
        }

        IEnumerable<string> GetSearchData(ItemLibraryItem item)
        {
            if (item == null)
                yield break;
            m_CurrentItemLibraryItem = item;
            foreach (var data in item.GetSearchData())
            {
                var terms = data.Terms;
                m_ScoreMultiplier = data.Multiplier;
                foreach (var term in terms)
                    yield return term;
            }
        }
    }
}
