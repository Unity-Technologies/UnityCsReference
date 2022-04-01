// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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
                    if (e.modifiers == EventModifiers.Shift)
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
            m_CountLabel.text =
                string.Format("{0} of {1}", m_SelectedIndex + 1, m_FoundItems.Count);
        }

        private void SelectPrev()
        {
            if (m_FoundItems.Count == 0)
                return;

            var count = m_FoundItems.Count;
            m_SelectedIndex--;
            m_SelectedIndex = (m_SelectedIndex % count + count) % count;

            m_ParentTreeView.SelectElement(m_FoundItems[m_SelectedIndex].item.data, m_CurrentQuery, m_FoundItems[m_SelectedIndex].highlight);
            m_CountLabel.text =
                string.Format("{0} of {1}", m_SelectedIndex + 1, m_FoundItems.Count);
        }

        private void PerformSearch(ChangeEvent<string> evt)
        {
            m_FoundItems.Clear();
            m_SelectedIndex = 0;

            m_CountLabel.text = string.Empty;
            m_CountLabel.AddToClassList(searchBarHiddenUssClassName);

            m_ParentTreeView.ClearSearchResults();

            m_FieldHelpLabel.AddToClassList(searchBarHiddenUssClassName);

            var query = evt.newValue;
            if (string.IsNullOrEmpty(query))
            {
                m_FieldHelpLabel.RemoveFromClassList(searchBarHiddenUssClassName);
                return;
            }

            var items = m_ParentTreeView.treeItems;
            if (items == null)
                return;

            m_CurrentFilter = DebuggerSearchBarFilter.All;

            if (query.StartsWith("#"))
                m_CurrentFilter = DebuggerSearchBarFilter.Name;
            else if (query.StartsWith("."))
                m_CurrentFilter = DebuggerSearchBarFilter.Class;

            if ((m_CurrentFilter & DebuggerSearchBarFilter.All) != DebuggerSearchBarFilter.All)
                query = query.Remove(0, 1);

            m_CurrentQuery = query;
            foreach (var item in items)
            {
                var treeItem = item;
                var element = treeItem.data;

                if (m_CurrentFilter == DebuggerSearchBarFilter.All &&
                    element.typeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_FoundItems.Add(new SearchResultItem() {item = treeItem, highlight = SearchHighlight.Type});
                }

                if ((m_CurrentFilter & DebuggerSearchBarFilter.Name) != 0 &&
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

            if (m_FoundItems.Count == 0)
                return;

            m_CountLabel.RemoveFromClassList(searchBarHiddenUssClassName);
            m_CountLabel.text =
                string.Format("{0} of {1}", m_SelectedIndex + 1, m_FoundItems.Count);

            var firstItem = m_FoundItems.First();
            m_ParentTreeView.SelectElement(firstItem.item.data, query, firstItem.highlight);
        }
    }
}
