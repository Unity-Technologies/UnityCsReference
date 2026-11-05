// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    [Flags]
    internal enum DebuggerSearchBarFilter
    {
        Name = 1 << 0,
        Class = 1 << 1,
        All = Name | Class
    }

    internal enum SearchHighlight
    {
        None,
        Type,
        Name,
        Class
    }

    internal class SearchResultItem
    {
        public TreeViewItemData<VisualElement> item;
        public SearchHighlight highlight;
    }

    internal class DebuggerSearchBar : VisualElement
    {
        internal const string searchBarUssClassName = "unity-treeview-searchbar";
        internal const string searchBarLabelUssClassName = searchBarUssClassName + "-label";
        internal const string searchBarLabelHelpUssClassName = searchBarLabelUssClassName + "-help";
        internal const string searchBarFieldUssClassName = searchBarUssClassName + "-field";
        internal const string searchBarButtonUssClassName = searchBarUssClassName + "-button";
        internal const string searchBarButtonPrevUssClassName = searchBarButtonUssClassName + "-prev";
        internal const string searchBarButtonNextUssClassName = searchBarButtonUssClassName + "-next";
        internal const string searchBarHiddenUssClassName = searchBarUssClassName + "-hidden";

        private List<SearchResultItem> m_FoundItems;
        private int m_SelectedIndex;
        private string m_CurrentQuery;
        private DebuggerSearchBarFilter m_CurrentFilter;

        private DebuggerTreeView m_ParentTreeView;
        private TextField m_SearchTextField;

        private Label m_CountLabel;
        private Label m_FieldHelpLabel;

        public DebuggerSearchBar(DebuggerTreeView parent)
        {
            m_ParentTreeView = parent;
            m_FoundItems = new List<SearchResultItem>();

            this.AddToClassList(searchBarUssClassName);

            m_FieldHelpLabel = new Label("Search by type, name, or class");
            m_FieldHelpLabel.pickingMode = PickingMode.Ignore;
            m_FieldHelpLabel.AddToClassList(searchBarLabelUssClassName);
            m_FieldHelpLabel.AddToClassList(searchBarLabelHelpUssClassName);
            Add(m_FieldHelpLabel);

            m_SearchTextField = new TextField();
            m_SearchTextField.AddToClassList(searchBarFieldUssClassName);
            m_SearchTextField.RegisterValueChangedCallback(PerformSearch);
            m_SearchTextField.RegisterCallback<KeyDownEvent>((e) =>
            {
                var targetField = m_SearchTextField;
                if (e.keyCode == KeyCode.F3 || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    if (e.modifiers.HasFlag(EventModifiers.Shift))
                        SelectPrev();
                    else
                        SelectNext();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    targetField.value = string.Empty;
                    targetField.visualInput.Blur();
                    m_ParentTreeView.ClearSearchResults();
                    m_ParentTreeView.Focus();
                }
            }, TrickleDown.TrickleDown);
            Add(m_SearchTextField);

            m_CountLabel = new Label();
            m_CountLabel.AddToClassList(searchBarLabelUssClassName);
            m_CountLabel.AddToClassList(searchBarHiddenUssClassName);
            Add(m_CountLabel);

            var prevButton = new Button(SelectPrev) { text = "<" };
            prevButton.AddToClassList(searchBarButtonUssClassName);
            prevButton.AddToClassList(searchBarButtonPrevUssClassName);
            Add(prevButton);

            var nextButton = new Button(SelectNext) { text = ">" };
            nextButton.AddToClassList(searchBarButtonUssClassName);
            nextButton.AddToClassList(searchBarButtonNextUssClassName);
            Add(nextButton);
        }

        internal Label countLabel => m_CountLabel;

        public void ClearSearch()
        {
            m_SearchTextField.value = string.Empty;
        }

        private void SelectNext()
        {
            if (m_FoundItems.Count == 0)
                return;

            m_SelectedIndex = (m_SelectedIndex + 1) % m_FoundItems.Count;
            m_ParentTreeView.SelectElement(m_FoundItems[m_SelectedIndex].item.data, m_CurrentQuery, m_FoundItems[m_SelectedIndex].highlight);
            UpdateCountLabel();
        }

        private void SelectPrev()
        {
            if (m_FoundItems.Count == 0)
                return;

            var count = m_FoundItems.Count;
            m_SelectedIndex--;
            m_SelectedIndex = (m_SelectedIndex % count + count) % count;

            m_ParentTreeView.SelectElement(m_FoundItems[m_SelectedIndex].item.data, m_CurrentQuery, m_FoundItems[m_SelectedIndex].highlight);
            UpdateCountLabel();
        }

        private void PerformSearch(ChangeEvent<string> evt)
        {
            m_FoundItems.Clear();
            m_SelectedIndex = 0;

            UpdateCountLabel();

            m_ParentTreeView.ClearSearchResults();

            m_FieldHelpLabel.AddToClassList(searchBarHiddenUssClassName);

            var query = evt.newValue;
            if (string.IsNullOrEmpty(query))
            {
                m_FieldHelpLabel.RemoveFromClassList(searchBarHiddenUssClassName);
                return;
            }

            FindMatches(query);
            UpdateCountLabel();

            if (m_FoundItems.Count == 0)
                return;

            var firstItem = m_FoundItems[0];
            m_ParentTreeView.SelectElement(firstItem.item.data, m_CurrentQuery, firstItem.highlight);
        }

        public void RefreshResults()
        {
            var text = m_SearchTextField.value;
            if (string.IsNullOrEmpty(text))
                return;

            var previous = m_SelectedIndex < m_FoundItems.Count ? m_FoundItems[m_SelectedIndex] : null;

            FindMatches(text);

            m_ParentTreeView.ClearSearchResults();

            if (m_FoundItems.Count == 0)
            {
                m_SelectedIndex = 0;
                UpdateCountLabel();
                return;
            }

            m_SelectedIndex = Mathf.Min(m_SelectedIndex, m_FoundItems.Count - 1);
            if (previous != null)
            {
                for (var i = 0; i < m_FoundItems.Count; i++)
                {
                    if (m_FoundItems[i].item.data == previous.item.data && m_FoundItems[i].highlight == previous.highlight)
                    {
                        m_SelectedIndex = i;
                        break;
                    }
                }
            }

            UpdateCountLabel();

            var current = m_FoundItems[m_SelectedIndex];
            m_ParentTreeView.ReapplySearchHighlight(current.item.data, m_CurrentQuery, current.highlight);
        }

        private void UpdateCountLabel()
        {
            if (m_FoundItems.Count == 0)
            {
                m_CountLabel.text = string.Empty;
                m_CountLabel.AddToClassList(searchBarHiddenUssClassName);
                return;
            }

            m_CountLabel.RemoveFromClassList(searchBarHiddenUssClassName);
            m_CountLabel.text =
                string.Format("{0} of {1}", m_SelectedIndex + 1, m_FoundItems.Count);
        }

        private void FindMatches(string query)
        {
            m_FoundItems.Clear();
            m_CurrentFilter = DebuggerSearchBarFilter.All;

            if (query.StartsWith("#"))
                m_CurrentFilter = DebuggerSearchBarFilter.Name;
            else if (query.StartsWith("."))
                m_CurrentFilter = DebuggerSearchBarFilter.Class;

            if ((m_CurrentFilter & DebuggerSearchBarFilter.All) != DebuggerSearchBarFilter.All)
                query = query.Remove(0, 1);

            m_CurrentQuery = query;
            foreach (var item in m_ParentTreeView.treeItems)
            {
                var treeItem = item;
                var element = treeItem.data;

                if (m_CurrentFilter == DebuggerSearchBarFilter.All &&
                    element.typeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_FoundItems.Add(new SearchResultItem() {item = treeItem, highlight = SearchHighlight.Type});
                }

                if ((m_CurrentFilter & DebuggerSearchBarFilter.Name) != 0 && !string.IsNullOrEmpty(element.name) &&
                    element.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_FoundItems.Add(new SearchResultItem() {item = treeItem, highlight = SearchHighlight.Name});
                }

                if ((m_CurrentFilter & DebuggerSearchBarFilter.Class) != 0)
                {
                    foreach (var styleClass in element.GetClasses())
                    {
                        if (styleClass.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            m_FoundItems.Add(new SearchResultItem() {item = treeItem, highlight = SearchHighlight.Class});
                            break;
                        }
                    }
                }
            }
        }
    }
}
