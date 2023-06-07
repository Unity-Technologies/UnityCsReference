// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor
{
    [System.Serializable]
    internal class SearchFilter
    {
        public enum SearchArea
        {
            AllAssets,
            InAssetsOnly,
            InPackagesOnly,
            SelectedFolders
        }

        public enum State
        {
            EmptySearchFilter,
            FolderBrowsing,
            SearchingInAllAssets,
            SearchingInAssetsOnly,
            SearchingInPackagesOnly,
            SearchingInFolders
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
        private int[] m_ReferencingInstanceIDs = new int[0];
        [SerializeField]
        private int[] m_SceneHandles;
        [SerializeField]
        private bool m_ShowAllHits = false;         // If true then just one filter must match to show an object, if false then all filters must match to show an object
        [SerializeField]
        private bool m_SkipHidden = false;
        [SerializeField]
        SearchArea m_SearchArea = SearchArea.InAssetsOnly;
        // Folder browsing
        [SerializeField]
        private string[] m_Folders = new string[0];
        [SerializeField]
        private string[] m_Globs = new string[0];
        [SerializeField]
        private string m_OriginalText = "";
        [SerializeField]
        private ImportLogFlags m_ImportLogFlags;

        [SerializeField]
        private bool m_FilterByTypeIntersection;

        // Interface
        public string nameFilter { get { return m_NameFilter; } set { m_NameFilter = value; }}
        public string[] classNames { get { return m_ClassNames; } set { m_ClassNames = value; }}
        public string[] assetLabels { get { return m_AssetLabels; } set { m_AssetLabels = value; }}
        public string[] assetBundleNames { get { return m_AssetBundleNames; } set { m_AssetBundleNames = value; }}
        public int[] referencingInstanceIDs { get { return m_ReferencingInstanceIDs; } set { m_ReferencingInstanceIDs = value; }}
        public int[] sceneHandles { get { return m_SceneHandles; } set { m_SceneHandles = value; }}
        public bool showAllHits { get { return m_ShowAllHits; } set { m_ShowAllHits = value; }}
        public bool skipHidden { get { return m_SkipHidden; } set { m_SkipHidden = value; }}
        public string[] folders { get { return m_Folders; } set { m_Folders = value; }}
        public SearchArea searchArea {  get { return m_SearchArea; } set { m_SearchArea = value; }}
        public string[] globs { get { return m_Globs; } set { m_Globs = value; }}
        public string originalText { get => m_OriginalText; set => m_OriginalText = value; }
        public ImportLogFlags importLogFlags { get => m_ImportLogFlags; set => m_ImportLogFlags = value; }
        internal bool filterByTypeIntersection { get => m_FilterByTypeIntersection; set => m_FilterByTypeIntersection = value; }

        public void ClearSearch()
        {
            m_NameFilter = "";
            m_OriginalText = "";
            m_ClassNames = new string[0];
            m_AssetLabels = new string[0];
            m_AssetBundleNames = new string[0];
            m_ReferencingInstanceIDs = new int[0];
            m_SceneHandles = new int[0];
            m_Globs = new string[0];
            m_ShowAllHits = false;
            m_SkipHidden = false;
            m_ImportLogFlags = ImportLogFlags.None;
            m_FilterByTypeIntersection = false;
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
                !IsNullOrEmpty(m_Globs) ||
                !IsNullOrEmpty(m_ReferencingInstanceIDs) ||
                m_ImportLogFlags != ImportLogFlags.None;



            bool foldersActive = !IsNullOrEmpty(m_Folders);

            if (isSearchActive)
            {
                if (foldersActive && m_SearchArea == SearchArea.SelectedFolders)
                    return State.SearchingInFolders;

                if (m_SearchArea == SearchArea.InAssetsOnly)
                    return State.SearchingInAssetsOnly;

                if (m_SearchArea == SearchArea.InPackagesOnly)
                    return State.SearchingInPackagesOnly;

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
            return (state == State.SearchingInAllAssets ||
                state == State.SearchingInAssetsOnly ||
                state == State.SearchingInPackagesOnly ||
                state == State.SearchingInFolders);
        }

        public bool SetNewFilter(SearchFilter newFilter)
        {
            bool changed = false;

            if (newFilter.m_NameFilter != m_NameFilter)
            {
                m_NameFilter = newFilter.m_NameFilter;
                changed = true;
            }

            if (newFilter.m_OriginalText != m_OriginalText)
            {
                m_OriginalText = newFilter.m_OriginalText;
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

            if (newFilter.m_SceneHandles != m_SceneHandles)
            {
                m_SceneHandles = newFilter.m_SceneHandles;
                changed = true;
            }

            if (newFilter.m_SearchArea != m_SearchArea)
            {
                m_SearchArea = newFilter.m_SearchArea;
                changed = true;
            }

            if (newFilter.m_ImportLogFlags != m_ImportLogFlags)
            {
                m_ImportLogFlags = newFilter.m_ImportLogFlags;
                changed = true;
            }

            m_ShowAllHits = newFilter.m_ShowAllHits;

            if (newFilter.m_SkipHidden != m_SkipHidden)
            {
                m_SkipHidden = newFilter.m_SkipHidden;
                changed = true;
            }

            if (newFilter.m_Globs != m_Globs)
            {
                m_Globs = newFilter.m_Globs;
                changed = true;
            }
            
            if (newFilter.m_FilterByTypeIntersection != m_FilterByTypeIntersection)
            {
                m_FilterByTypeIntersection = newFilter.m_FilterByTypeIntersection;
                changed = true;
            }


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


            if (m_AssetBundleNames != null && m_AssetBundleNames.Length > 0)
                result += "[AssetBundleNames: " + m_AssetBundleNames[0] + "]";

            if (m_ClassNames != null && m_ClassNames.Length > 0)
                result += "[Types: " + m_ClassNames[0] + " (" + m_ClassNames.Length + ")]";

            if (m_ReferencingInstanceIDs != null && m_ReferencingInstanceIDs.Length > 0)
                result += "[RefIDs: " + m_ReferencingInstanceIDs[0] + "]";

            if (m_Folders != null && m_Folders.Length > 0)
                result += "[Folders: " + m_Folders[0] + "]";

            if (m_Globs != null && m_Globs.Length > 0)
                result += "[Glob: " + m_Globs[0] + "]";

            result += "[ShowAllHits: " + showAllHits + "]";
            result += "[SkipHidden: " + skipHidden + "]";

            if (m_ImportLogFlags == (ImportLogFlags.Error | ImportLogFlags.Warning))
            {
                result += $"[ImportLog: {ImportLog.Filters.AllIssuesStr}]";
            }
            else if (m_ImportLogFlags == ImportLogFlags.Error)
            {
                result += $"[ImportLog: {ImportLog.Filters.ErrorsStr}]";
            }
            else if (m_ImportLogFlags == ImportLogFlags.Warning)
            {
                result += $"[ImportLog: {ImportLog.Filters.WarningsStr}]";
            }

            if (m_FilterByTypeIntersection)
            {
                result += $"[FilterByTypeIntersection]";
            }

            return result;
        }

        string FormatFilterTokenForSearchEngine(string token)
        {
            if (SearchService.ProjectSearch.HasEngineOverride())
                return $"{token}=";
            return $"{token}:";
        }

        internal string FilterToSearchFieldString()
        {
            string result = "";
            if (!string.IsNullOrEmpty(m_NameFilter))
                result += m_NameFilter;

            // See SearchUtility.cs for search tokens
            AddToString(FormatFilterTokenForSearchEngine("t"), m_ClassNames, ref result);
            AddToString(FormatFilterTokenForSearchEngine("l"), m_AssetLabels, ref result);
            AddToString("b:", m_AssetBundleNames, ref result);
            AddToString("glob:", m_Globs.Select(a => $"\"{a}\"").ToArray(), ref result);

            if (m_ImportLogFlags == (ImportLogFlags.Error | ImportLogFlags.Warning))
            {
                result += $" {ImportLog.Filters.SearchToken}:{ImportLog.Filters.AllIssuesStr}";
            }
            else if (m_ImportLogFlags == ImportLogFlags.Error)
            {
                result += $" {ImportLog.Filters.SearchToken}:{ImportLog.Filters.ErrorsStr}";
            }
            else if (m_ImportLogFlags == ImportLogFlags.Warning)
            {
                result += $" {ImportLog.Filters.SearchToken}:{ImportLog.Filters.WarningsStr}";
            }

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
