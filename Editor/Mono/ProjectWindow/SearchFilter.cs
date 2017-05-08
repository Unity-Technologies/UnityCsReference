// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Collaboration;
using UnityEditor.Connect;

namespace UnityEditor
{
    [System.Serializable]
    internal class SearchFilter
    {
        public enum SearchArea
        {
            AllAssets,
            SelectedFolders,
            AssetStore
        }

        public enum State
        {
            EmptySearchFilter,
            FolderBrowsing,
            SearchingInAllAssets,
            SearchingInFolders,
            SearchingInAssetStore
        }

        // Searching
        [SerializeField]
        private string m_NameFilter = "";
        [SerializeField]
        private string[] m_ClassNames = new string[0];
        [SerializeField]
        private string[] m_AssetLabels = new string[0];
        [SerializeField]
        private string[] m_AssetBundleNames = new string[0];
        [SerializeField]
        private string[] m_VersionControlStates = new string[0];
        [SerializeField]
        private string[] m_SoftLockControlStates = new string[0];
        [SerializeField]
        private int[] m_ReferencingInstanceIDs = new int[0];
        [SerializeField]
        private string[] m_ScenePaths;
        [SerializeField]
        private bool m_ShowAllHits = false;         // If true then just one filter must match to show an object, if false then all filters must match to show an object
        [SerializeField]
        SearchArea m_SearchArea = SearchArea.AllAssets;

        // Folder browsing
        [SerializeField]
        private string[] m_Folders = new string[0];

        // Interface
        public string nameFilter {get {return m_NameFilter; } set { m_NameFilter = value; } }
        public string[] classNames { get {return m_ClassNames; } set {m_ClassNames = value; }}
        public string[] assetLabels {get {return m_AssetLabels; } set {m_AssetLabels = value; }}
        public string[] versionControlStates { get { return m_VersionControlStates; } set { m_VersionControlStates = value; } }
        public string[] softLockControlStates { get { return m_SoftLockControlStates; } set { m_SoftLockControlStates = value; } }
        public string[] assetBundleNames {get {return m_AssetBundleNames; } set {m_AssetBundleNames = value; }}
        public int[] referencingInstanceIDs { get { return m_ReferencingInstanceIDs; } set { m_ReferencingInstanceIDs = value; } }
        public string[] scenePaths { get { return m_ScenePaths; } set { m_ScenePaths = value; } }
        public bool showAllHits { get { return m_ShowAllHits; } set { m_ShowAllHits = value; }}
        public string[] folders { get {return m_Folders; } set {m_Folders = value; }}
        public SearchArea searchArea {  get { return m_SearchArea; } set { m_SearchArea = value; }}

        public void ClearSearch()
        {
            m_NameFilter = "";
            m_ClassNames = new string[0];
            m_AssetLabels = new string[0];
            m_AssetBundleNames = new string[0];
            m_ReferencingInstanceIDs = new int[0];
            m_ScenePaths = new string[0];
            m_VersionControlStates = new string[0];
            m_SoftLockControlStates = new string[0];
            m_ShowAllHits = false;
        }

        bool IsNullOrEmpty<T>(T[] list)
        {
            return (list == null || list.Length == 0);
        }

        public State GetState()
        {
            bool isSearchActive = !string.IsNullOrEmpty(m_NameFilter) ||
                !IsNullOrEmpty(m_AssetLabels) ||
                !IsNullOrEmpty(m_ClassNames) ||
                !IsNullOrEmpty(m_AssetBundleNames) ||
                !IsNullOrEmpty(m_ReferencingInstanceIDs);

            isSearchActive = isSearchActive || !IsNullOrEmpty(m_VersionControlStates);
            isSearchActive = isSearchActive || !IsNullOrEmpty(m_SoftLockControlStates);


            bool foldersActive = !IsNullOrEmpty(m_Folders);

            if (isSearchActive)
            {
                if (m_SearchArea == SearchArea.AssetStore)
                    return State.SearchingInAssetStore;

                if (foldersActive && m_SearchArea == SearchArea.SelectedFolders)
                    return State.SearchingInFolders;

                return State.SearchingInAllAssets;
            }
            else if (foldersActive)
            {
                return State.FolderBrowsing;
            }

            return State.EmptySearchFilter;
        }

        public bool IsSearching()
        {
            State state = GetState();
            return (state == State.SearchingInAllAssets || state == State.SearchingInFolders || state == State.SearchingInAssetStore);
        }

        public bool SetNewFilter(SearchFilter newFilter)
        {
            bool changed = false;

            if (newFilter.m_NameFilter != m_NameFilter)
            {
                m_NameFilter = newFilter.m_NameFilter;
                changed = true;
            }

            if (newFilter.m_ClassNames != m_ClassNames)
            {
                m_ClassNames = newFilter.m_ClassNames;
                changed = true;
            }

            if (newFilter.m_Folders != m_Folders)
            {
                m_Folders = newFilter.m_Folders;
                changed = true;
            }
            if (newFilter.m_VersionControlStates != m_VersionControlStates)
            {
                m_VersionControlStates = newFilter.m_VersionControlStates;
                changed = true;
            }
            if (newFilter.m_SoftLockControlStates != m_SoftLockControlStates)
            {
                m_SoftLockControlStates = newFilter.m_SoftLockControlStates;
                changed = true;
            }
            if (newFilter.m_AssetLabels != m_AssetLabels)
            {
                m_AssetLabels = newFilter.m_AssetLabels;
                changed = true;
            }

            if (newFilter.m_AssetBundleNames != m_AssetBundleNames)
            {
                m_AssetBundleNames = newFilter.m_AssetBundleNames;
                changed = true;
            }

            if (newFilter.m_ReferencingInstanceIDs != m_ReferencingInstanceIDs)
            {
                m_ReferencingInstanceIDs = newFilter.m_ReferencingInstanceIDs;
                changed = true;
            }

            if (newFilter.m_ScenePaths != m_ScenePaths)
            {
                m_ScenePaths = newFilter.m_ScenePaths;
                changed = true;
            }

            if (newFilter.m_SearchArea != m_SearchArea)
            {
                m_SearchArea = newFilter.m_SearchArea;
                changed = true;
            }

            m_ShowAllHits = newFilter.m_ShowAllHits;


            return changed;
        }

        // Debug
        public override string ToString()
        {
            string result = "SearchFilter: ";

            result += string.Format("[Area: {0}, State: {1}]", m_SearchArea, GetState());

            if (!string.IsNullOrEmpty(m_NameFilter))
                result += "[Name: " + m_NameFilter + "]";

            if (m_AssetLabels != null && m_AssetLabels.Length > 0)
                result += "[Labels: " + m_AssetLabels[0] + "]";

            if (m_VersionControlStates != null && m_VersionControlStates.Length > 0)
                result += "[VersionStates: " + m_VersionControlStates[0] + "]";

            if (m_SoftLockControlStates != null && m_SoftLockControlStates.Length > 0)
                result += "[SoftLockStates: " + m_SoftLockControlStates[0] + "]";

            if (m_AssetBundleNames != null && m_AssetBundleNames.Length > 0)
                result += "[AssetBundleNames: " + m_AssetBundleNames[0] + "]";

            if (m_ClassNames != null && m_ClassNames.Length > 0)
                result += "[Types: " + m_ClassNames[0] + " (" + m_ClassNames.Length + ")]";

            if (m_ReferencingInstanceIDs != null && m_ReferencingInstanceIDs.Length > 0)
                result += "[RefIDs: " + m_ReferencingInstanceIDs[0] + "]";

            if (m_Folders != null && m_Folders.Length > 0)
                result += "[Folders: " + m_Folders[0] + "]";

            result += "[ShowAllHits: " + showAllHits + "]";
            return result;
        }

        internal string FilterToSearchFieldString()
        {
            string result = "";
            if (!string.IsNullOrEmpty(m_NameFilter))
                result += m_NameFilter;

            // See SearchUtility.cs for search tokens
            AddToString("t:", m_ClassNames, ref result);
            AddToString("l:", m_AssetLabels, ref result);
            AddToString("v:", m_VersionControlStates, ref result);
            AddToString("s:", m_SoftLockControlStates, ref result);
            AddToString("b:", m_AssetBundleNames, ref result);
            return result;
        }

        void AddToString<T>(string prefix, T[] list, ref string result)
        {
            if (list == null)
                return;
            if (result == null)
                result = "";

            foreach (T item in list)
            {
                if (!string.IsNullOrEmpty(result))
                    result += " ";
                result += prefix + item;
            }
        }

        // Keeps current SearchArea
        internal void SearchFieldStringToFilter(string searchString)
        {
            ClearSearch();

            if (string.IsNullOrEmpty(searchString))
                return;

            SearchUtility.ParseSearchString(searchString, this);
        }

        internal static SearchFilter CreateSearchFilterFromString(string searchText)
        {
            SearchFilter searchFilter = new SearchFilter();
            SearchUtility.ParseSearchString(searchText, searchFilter);
            return searchFilter;
        }

        // Split text into words separated by whitespace but handle quotes
        // E.g 'one man' becomes:   'one', 'man'
        // E.g '"one man' becomes:  'one', 'man'
        // E.g '"one man"' becomes: 'one man'
        public static string[] Split(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new string[0];

            List<string> words = new List<string>();
            foreach (Match m in Regex.Matches(text, "\".+?\"|\\S+"))
            {
                words.Add(m.Value.Replace("\"", ""));   // remove quotes
            }

            return words.ToArray();
        }
    } // end of class SearchFilter
}
