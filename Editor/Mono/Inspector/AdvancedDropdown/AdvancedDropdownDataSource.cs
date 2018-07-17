// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.AdvancedDropdown
{
    internal abstract class AdvancedDropdownDataSource
    {
        private static string kSearchHeader = "Search";
        private readonly string kSearchHeaderLocalized = UnityEditor.L10n.Tr("Search");

        private AdvancedDropdownItem m_MainTree;
        private AdvancedDropdownItem m_SearchTree;

        public AdvancedDropdownItem mainTree { get { return m_MainTree; }}
        public AdvancedDropdownItem searchTree { get { return m_SearchTree; }}

        protected List<AdvancedDropdownItem> m_SearchableElements;

        public List<string> selectedIds = new List<string>();

        public void ReloadData()
        {
            m_MainTree = FetchData();
        }

        public virtual void UpdateSelectedId(AdvancedDropdownItem item)
        {
        }

        protected abstract AdvancedDropdownItem FetchData();

        public void RebuildSearch(string search)
        {
            m_SearchTree = Search(search);
        }

        private bool AddMatchItem(AdvancedDropdownItem e, string name, string[] searchWords, SortedList<string, AdvancedDropdownItem> matchesStart, SortedList<string, AdvancedDropdownItem> matchesWithin)
        {
            var didMatchAll = true;
            var didMatchStart = false;

            // See if we match ALL the seaarch words.
            for (var w = 0; w < searchWords.Length; w++)
            {
                var search = searchWords[w];
                if (name.Contains(search))
                {
                    // If the start of the item matches the first search word, make a note of that.
                    if (w == 0 && name.StartsWith(search))
                        didMatchStart = true;
                }
                else
                {
                    // As soon as any word is not matched, we disregard this item.
                    didMatchAll = false;
                    break;
                }
            }
            // We always need to match all search words.
            // If we ALSO matched the start, this item gets priority.
            if (didMatchAll)
            {
                if (didMatchStart)
                    matchesStart.Add(e.id, e);
                else
                    matchesWithin.Add(e.id, e);
            }
            return didMatchAll;
        }

        virtual protected AdvancedDropdownItem Search(string searchString)
        {
            if (string.IsNullOrEmpty(searchString) || m_SearchableElements == null)
                return null;

            // Support multiple search words separated by spaces.
            var searchWords = searchString.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            var matchesStart = new SortedList<string, AdvancedDropdownItem>();
            var matchesWithin = new SortedList<string, AdvancedDropdownItem>();

            bool found = false;
            foreach (var e in m_SearchableElements)
            {
                var name = e.searchableName.ToLower().Replace(" ", "");
                if (AddMatchItem(e, name, searchWords, matchesStart, matchesWithin))
                    found = true;
            }
            if (!found)
            {
                foreach (var e in m_SearchableElements)
                {
                    var name = e.searchableNameLocalized.Replace(" ", "");
                    AddMatchItem(e, name, searchWords, matchesStart, matchesWithin);
                }
            }

            var searchTree = new AdvancedDropdownItem(kSearchHeader, kSearchHeaderLocalized, -1);
            foreach (var element in matchesStart)
            {
                searchTree.AddChild(element.Value);
            }
            foreach (var element in matchesWithin)
            {
                searchTree.AddChild(element.Value);
            }
            return searchTree;
        }
    }
}
