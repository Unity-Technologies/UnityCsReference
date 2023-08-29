// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Accessibility
{
    /// <summary>
    /// Search field used to search items by id, label or role in a accessibility hierarchy tree view.
    /// </summary>
    internal class TreeViewSearchBar : VisualElement
    {
        private static readonly string s_UssClassName = "hierarchy-tree-view__search-bar";
        private static readonly string s_HiddenElementUssClassName = "hierarchy-tree-view__search-bar__element-hidden";
        private static readonly string s_SearchLabelUssClassName = s_UssClassName + "__label";
        private static readonly string s_SearchLabelHelpUssClassName = s_UssClassName + "__label-help";
        private static readonly string s_SearchFieldUssClassName = s_UssClassName + "__field";
        private static readonly string s_SearchButtonUssClassName = s_UssClassName + "__button";
        private static readonly string s_SearchPrevButtonUssClassName = s_SearchButtonUssClassName + "-prev";
        private static readonly string s_SearchNextButtonUssClassName = s_UssClassName + "-next";

        class SearchResultItem
        {
            public int itemId;
        }

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new TreeViewSearchBar();
        }

        private List<SearchResultItem> m_FoundItems;
        private int m_SelectedIndex;
        private string m_CurrentQuery;

        private TextField m_SearchTextField;

        private TreeView m_TreeView;
        private Label m_CountLabel;
        private Label m_FieldHelpLabel;

        private List<VisualElement> m_SearchResultsHightlights;

        public string currentQuery => m_CurrentQuery;

        /// <summary>
        /// The tree view on which search is performed.
        /// </summary>
        public TreeView treeView
        {
            get => m_TreeView;
            set => m_TreeView = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TreeViewSearchBar()
        {
            m_FoundItems = new List<SearchResultItem>();
            m_SearchResultsHightlights = new List<VisualElement>();

            AddToClassList(s_UssClassName);

            m_FieldHelpLabel = new Label(L10n.Tr("Search by id, label, role"));
            m_FieldHelpLabel.pickingMode = PickingMode.Ignore;
            m_FieldHelpLabel.AddToClassList(s_SearchLabelUssClassName);
            m_FieldHelpLabel.AddToClassList(s_SearchLabelHelpUssClassName);
            Add(m_FieldHelpLabel);

            m_SearchTextField = new TextField();
            m_SearchTextField.AddToClassList(s_SearchFieldUssClassName);
            m_SearchTextField.RegisterValueChangedCallback((e) => PerformSearch());
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
                    ClearSearchResults();
                    treeView.Focus();
                }
            }, TrickleDown.TrickleDown);
            Add(m_SearchTextField);

            m_CountLabel = new Label();
            m_CountLabel.AddToClassList(s_SearchLabelUssClassName);
            m_CountLabel.AddToClassList(s_HiddenElementUssClassName);
            Add(m_CountLabel);

            var prevButton = new Button(SelectPrev) {text = "<"};
            prevButton.AddToClassList(s_SearchButtonUssClassName);
            prevButton.AddToClassList(s_SearchPrevButtonUssClassName);
            Add(prevButton);

            var nextButton = new Button(SelectNext) {text = ">"};
            nextButton.AddToClassList(s_SearchButtonUssClassName);
            nextButton.AddToClassList(s_SearchNextButtonUssClassName);
            Add(nextButton);
        }

        private IEnumerable<TreeViewItemData<AccessibilityViewModelNode>> GetAllItems()
        {
            if (m_TreeView?.viewController == null)
            {
                yield break;
            }

            var treeViewController = m_TreeView?.viewController as DefaultTreeViewController<AccessibilityViewModelNode>;

            foreach (var itemId in m_TreeView.viewController.GetAllItemIds())
            {
                yield return treeViewController.GetTreeViewItemDataForId(itemId);
            }
        }

        /// <summary>
        /// Clears the text of the search field.
        /// </summary>
        public void ClearSearch()
        {
            m_SearchTextField.value = string.Empty;
        }

        private void SelectNext()
        {
            if (m_FoundItems.Count == 0)
                return;

            m_SelectedIndex = (m_SelectedIndex + 1) % m_FoundItems.Count;
            SelectElement(m_FoundItems[m_SelectedIndex].itemId, m_CurrentQuery);
            m_CountLabel.text = $"{m_SelectedIndex + 1} of {m_FoundItems.Count}";
        }

        private void SelectPrev()
        {
            if (m_FoundItems.Count == 0)
                return;

            var count = m_FoundItems.Count;
            m_SelectedIndex--;
            m_SelectedIndex = (m_SelectedIndex % count + count) % count;

            SelectElement(m_FoundItems[m_SelectedIndex].itemId, m_CurrentQuery);
            m_CountLabel.text = $"{m_SelectedIndex + 1} of {m_FoundItems.Count}";
        }

        /// <summary>
        /// Performs a search.
        /// </summary>
        public void PerformSearch()
        {
            m_FoundItems.Clear();
            m_SelectedIndex = 0;

            m_CountLabel.text = string.Empty;
            m_CountLabel.AddToClassList(s_HiddenElementUssClassName);

            ClearSearchResults();

            m_FieldHelpLabel.AddToClassList(s_HiddenElementUssClassName);

            m_CurrentQuery = m_SearchTextField.text;

            m_TreeView.RefreshItems();

            if (string.IsNullOrEmpty(m_CurrentQuery))
            {
                m_FieldHelpLabel.RemoveFromClassList(s_HiddenElementUssClassName);
                return;
            }

            var items = GetAllItems();
            if (items == null)
                return;

            foreach (var treeItem in items)
            {
                var element = treeItem.data;
                
                if (element.isRoot)
                    continue;

                var idText = element.id.ToString();
                var labelText = AccessibilityHierarchyTreeViewItem.GetDisplayLabelText(element.isActive, element.label);
                var roleText = AccessibilityHierarchyTreeViewItem.GetDisplayRoleText(element.role);

                if ((idText.IndexOf(m_CurrentQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (labelText?.IndexOf(m_CurrentQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (roleText.IndexOf(m_CurrentQuery, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    m_FoundItems.Add(new SearchResultItem() {itemId = treeItem.id});
                }
            }

            if (m_FoundItems.Count == 0)
                return;

            m_CountLabel.RemoveFromClassList(s_HiddenElementUssClassName);
            m_CountLabel.text = $"{m_SelectedIndex + 1} of {m_FoundItems.Count}";

            var firstItem = m_FoundItems[0];
            SelectElement(firstItem.itemId, m_CurrentQuery);
        }

        private void ClearSearchResults()
        {
            foreach (var hl in m_SearchResultsHightlights)
                hl.RemoveFromHierarchy();

            m_SearchResultsHightlights.Clear();
        }

        private void SelectElement(int itemId, string query)
        {
            ClearSearchResults();

            var node = m_TreeView.GetItemDataForId<AccessibilityViewModelNode>(itemId);

            if (node.isNull || node.isRoot)
                return;

            m_TreeView.SetSelectionById(itemId);
            m_TreeView.ScrollToItemById(itemId);
        }
    }
}
