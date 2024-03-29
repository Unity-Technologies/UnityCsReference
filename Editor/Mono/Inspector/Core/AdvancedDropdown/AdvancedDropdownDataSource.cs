// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.IMGUI.Controls
{
    internal abstract class AdvancedDropdownDataSource
    {
        private static readonly string kSearchHeader = L10n.Tr("Search");

        private AdvancedDropdownItem m_MainTree;
        private AdvancedDropdownItem m_SearchTree;
        private AdvancedDropdownItem m_CurrentContextTree;
        private List<int> m_SelectedIDs = new List<int>();

        public AdvancedDropdownItem mainTree { get { return m_MainTree; }}
        public AdvancedDropdownItem searchTree { get { return m_SearchTree; }}
        public List<int> selectedIDs { get { return m_SelectedIDs; }}
        public bool CurrentFolderContextualSearch { get; set; }

        protected AdvancedDropdownItem root { get { return m_MainTree; }}
        protected List<AdvancedDropdownItem> m_SearchableElements;

        internal delegate bool SearchMatchItemHandler(in AdvancedDropdownItem item, in string[] words, out bool didMatchStart);

        internal SearchMatchItemHandler searchMatchItem;
        internal IComparer<AdvancedDropdownItem> searchMatchItemComparer;

        public void ReloadData()
        {
            m_MainTree = FetchData();
        }

        protected abstract AdvancedDropdownItem FetchData();

        public void RebuildSearch(string search, AdvancedDropdownItem currentTree)
        {
            if (CurrentFolderContextualSearch && m_CurrentContextTree != currentTree && currentTree != searchTree)
            {
                m_SearchableElements = null;
            }
            m_CurrentContextTree = currentTree;
            m_SearchTree = Search(search);
        }

        protected bool AddMatchItem(AdvancedDropdownItem e, string name, string[] searchWords, List<AdvancedDropdownItem> matchesStart, List<AdvancedDropdownItem> matchesWithin)
        {
            var didMatchAll = true;
            var didMatchStart = false;

            if (searchMatchItem != null)
            {
                didMatchAll = searchMatchItem(e, searchWords, out didMatchStart);
            }
            else
            {
                // See if we match ALL the search words.
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
            }

            // We always need to match all search words.
            // If we ALSO matched the start, this item gets priority.
            if (didMatchAll)
            {
                if (didMatchStart)
                    matchesStart.Add(e);
                else
                    matchesWithin.Add(e);
            }
            return didMatchAll;
        }

        protected virtual AdvancedDropdownItem Search(string searchString)
        {
            if (m_SearchableElements == null)
            {
                BuildSearchableElements();
            }
            if (string.IsNullOrEmpty(searchString))
                return null;

            // Support multiple search words separated by spaces.
            var searchWords = searchString.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            var matchesStart = new List<AdvancedDropdownItem>();
            var matchesWithin = new List<AdvancedDropdownItem>();

            foreach (var e in m_SearchableElements)
            {
                var name = e.name.ToLower().Replace(" ", "");
                AddMatchItem(e, name, searchWords, matchesStart, matchesWithin);
            }

            var searchTree = new AdvancedDropdownItem(kSearchHeader);
            if (searchMatchItemComparer == null)
                matchesStart.Sort();
            else
                matchesStart.Sort(searchMatchItemComparer);
            foreach (var element in matchesStart)
            {
                searchTree.AddChild(element);
            }
            if (searchMatchItemComparer == null)
                matchesWithin.Sort();
            else
                matchesWithin.Sort(searchMatchItemComparer);
            foreach (var element in matchesWithin)
            {
                searchTree.AddChild(element);
            }
            return searchTree;
        }

        void BuildSearchableElements()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            BuildSearchableElements(CurrentFolderContextualSearch && m_CurrentContextTree != null ? m_CurrentContextTree : root);
        }

        void BuildSearchableElements(AdvancedDropdownItem item)
        {
            if (!item.children.Any())
            {
                m_SearchableElements.Add(item);
                return;
            }
            foreach (var child in item.children)
            {
                BuildSearchableElements(child);
            }
        }
    }
}
