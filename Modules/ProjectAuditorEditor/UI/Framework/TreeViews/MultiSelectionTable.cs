// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class MultiSelectionTable : TreeView
    {
        // All columns
        public enum Column
        {
            ItemName,
            State,
            GroupName
        }

        // stephenm TODO - Sorting doesn't work in this window (or in the Thread Selection Window in Profile Analyzer that
        // this is based on). So maybe rip this all out?
        public enum SortOption
        {
            ItemName,
            GroupName
        }

        const float kRowHeights = 20f;
        readonly TreeItemIdentifier m_AllIdentifier;

        readonly string[] m_Names;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);
        readonly TreeViewSelection m_Selection;

        // stephenm TODO - Sorting doesn't work in this window (or in the Thread Selection Window in Profile Analyzer that
        // this is based on). So maybe rip this all out?
        // Sort options per column
        readonly SortOption[] m_SortOptions =
        {
            SortOption.ItemName,
            SortOption.ItemName,
            SortOption.GroupName
        };

        GUIStyle m_ActiveLineStyle;

        public MultiSelectionTable(TreeViewState state, MultiColumnHeader multicolumnHeader, string[] names,
                                   TreeViewSelection selection) : base(state, multicolumnHeader)
        {
            m_AllIdentifier = new TreeItemIdentifier();
            m_AllIdentifier.SetName("All");
            m_AllIdentifier.SetAll();

            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(Column)).Length,
                "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset =
                (kRowHeights - EditorGUIUtility.singleLineHeight) *
                0.5f; // center foldout in the row since we also center content. See RowGUI
            // extraSpaceBeforeIconAndLabel = 0;
            multicolumnHeader.sortingChanged += OnSortingChanged;

            m_Names = names;
            m_Selection = new TreeViewSelection(selection);
            Reload();
        }

        public void ClearSelection()
        {
            m_Selection.selection.Clear();
            m_Selection.groups.Clear();
            Reload();
        }

        public TreeViewSelection GetTreeViewSelection()
        {
            return m_Selection;
        }

        protected int GetChildCount(TreeItemIdentifier selectedIdentifier, out int selected)
        {
            var count = 0;
            var selectedCount = 0;

            if (selectedIdentifier.index == TreeItemIdentifier.kAll)
            {
                if (selectedIdentifier.name == "All")
                    for (var index = 0; index < m_Names.Length; ++index)
                    {
                        var nameWithIndex = m_Names[index];
                        var identifier = new TreeItemIdentifier(nameWithIndex);

                        if (identifier.index != TreeItemIdentifier.kAll)
                        {
                            count++;
                            if (m_Selection.selection.Contains(nameWithIndex))
                                selectedCount++;
                        }
                    }
                else
                    for (var index = 0; index < m_Names.Length; ++index)
                    {
                        var nameWithIndex = m_Names[index];
                        var identifier = new TreeItemIdentifier(nameWithIndex);

                        if (selectedIdentifier.name == identifier.name &&
                            identifier.index != TreeItemIdentifier.kAll)
                        {
                            count++;
                            if (m_Selection.selection.Contains(nameWithIndex))
                                selectedCount++;
                        }
                    }
            }

            selected = selectedCount;
            return count;
        }

        protected override TreeViewItem BuildRoot()
        {
            var idForHiddenRoot = -1;
            var depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            var depth = 0;

            var top = new SelectionWindowTreeViewItem(-1, depth, m_AllIdentifier.name, m_AllIdentifier);
            root.AddChild(top);

            var expandList = new List<int> {-1};
            var lastName = "";
            var node = root;
            for (var index = 0; index < m_Names.Length; ++index)
            {
                var nameWithIndex = m_Names[index];
                if (nameWithIndex == m_AllIdentifier.nameWithIndex)
                    continue;

                var identifier = new TreeItemIdentifier(nameWithIndex);
                var item = new SelectionWindowTreeViewItem(index, depth, m_Names[index], identifier);

                if (identifier.name != lastName)
                {
                    // New items at root
                    node = top;
                    depth = 0;
                }

                node.AddChild(item);

                if (identifier.name != lastName)
                {
                    // Extra instances hang of the parent
                    lastName = identifier.name;
                    node = item;
                    depth = 1;
                }
            }

            SetExpanded(expandList);

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        void BuildRowRecursive(IList<TreeViewItem> rows, TreeViewItem item)
        {
            if (!IsExpanded(item.id))
                return;

            foreach (SelectionWindowTreeViewItem subNode in item.children)
            {
                rows.Add(subNode);

                if (subNode.children != null)
                    BuildRowRecursive(rows, subNode);
            }
        }

        void BuildAllRows(IList<TreeViewItem> rows, TreeViewItem rootItem)
        {
            rows.Clear();
            if (rootItem == null)
                return;

            if (rootItem.children == null)
                return;

            foreach (SelectionWindowTreeViewItem node in rootItem.children)
            {
                rows.Add(node);

                if (node.children != null)
                    BuildRowRecursive(rows, node);
            }
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            BuildAllRows(m_Rows, root);

            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1) return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            // Sort the roots of the existing tree items
            SortByMultipleColumns();

            BuildAllRows(rows, rootItem);

            Repaint();
        }

        string GetItemGroupName(SelectionWindowTreeViewItem item)
        {
            var tokens = item.TreeItemIdentifier.name.Split('.');
            if (tokens.Length <= 1) return "";

            return tokens[0];
        }

        void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0) return;

            var myTypes = rootItem.children.Cast<SelectionWindowTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (var i = 1; i < sortedColumns.Length; i++)
            {
                var sortOption = m_SortOptions[sortedColumns[i]];
                var ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.GroupName:
                        orderedQuery = orderedQuery.ThenBy(l => GetItemGroupName(l), ascending);
                        break;
                    case SortOption.ItemName:
                        orderedQuery = orderedQuery.ThenBy(l => l.displayName, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<SelectionWindowTreeViewItem> InitialOrder(
            IEnumerable<SelectionWindowTreeViewItem> myTypes, int[] history)
        {
            var sortOption = m_SortOptions[history[0]];
            var ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.GroupName:
                    return myTypes.Order(l => GetItemGroupName(l), ascending);
                case SortOption.ItemName:
                    return myTypes.Order(l => l.displayName, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.displayName, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (SelectionWindowTreeViewItem)args.item;

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), item, (Column)args.GetColumn(i), ref args);
        }

        bool TreeItemSelected(TreeItemIdentifier selectedIdentifier)
        {
            if (m_Selection.selection != null &&
                m_Selection.selection.Count > 0 &&
                m_Selection.selection.Contains(selectedIdentifier.nameWithIndex))
                return true;

            // If querying the 'All' filter then check if all selected
            if (selectedIdentifier.nameWithIndex == m_AllIdentifier.nameWithIndex)
            {
                // Check all items without All in the name are selected
                foreach (var nameWithIndex in m_Names)
                {
                    var identifier = new TreeItemIdentifier(nameWithIndex);
                    if (identifier.index ==
                        TreeItemIdentifier.kAll /* || identifier.index == TreeItemIdentifier.kSingle*/)
                        continue;

                    if (!m_Selection.selection.Contains(nameWithIndex))
                        return false;
                }

                return true;
            }

            // Need to check 'all' and item group all.
            if (selectedIdentifier.index == TreeItemIdentifier.kAll)
            {
                // Count all items that match this item group
                var count = 0;
                foreach (var nameWithIndex in m_Names)
                {
                    var identifier = new TreeItemIdentifier(nameWithIndex);
                    if (identifier.index == TreeItemIdentifier.kAll || identifier.index == TreeItemIdentifier.kSingle)
                        continue;

                    if (selectedIdentifier.name != identifier.name)
                        continue;

                    count++;
                }

                // Count all the items we have selected that match this item group
                var selectedCount = 0;
                foreach (var nameWithIndex in m_Selection.selection)
                {
                    var identifier = new TreeItemIdentifier(nameWithIndex);
                    if (selectedIdentifier.name != identifier.name)
                        continue;
                    if (identifier.index > count)
                        continue;

                    selectedCount++;
                }

                if (count == selectedCount)
                    return true;
            }

            return false;
        }

        void CellGUI(Rect cellRect, SelectionWindowTreeViewItem item, Column column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case Column.ItemName:
                {
                    args.rowRect = cellRect;
                    // base.RowGUI(args);    // Required to show tree indenting

                    // Draw manually to keep indenting by add a tooltip
                    var rect = cellRect;
                    if (Event.current.rawType == EventType.Repaint)
                    {
                        int selectedChildren;
                        var childCount = GetChildCount(item.TreeItemIdentifier, out selectedChildren);

                        string text;
                        string tooltip;
                        var fullName = item.TreeItemIdentifier.name;
                        var groupName = GetItemGroupName(item);

                        if (childCount <= 1)
                        {
                            text = item.displayName;
                            tooltip = groupName == "" ? text : string.Format("{0}\n{1}", text, groupName);
                        }
                        else if (selectedChildren != childCount)
                        {
                            text = string.Format("{0} ({1} of {2})", fullName, selectedChildren, childCount);
                            tooltip = groupName == "" ? text : string.Format("{0}\n{1}", text, groupName);
                        }
                        else
                        {
                            text = string.Format("{0} (All)", fullName);
                            tooltip = groupName == "" ? text : string.Format("{0}\n{1}", text, groupName);
                        }

                        var content = new GUIContent(text, tooltip);

                        if (m_ActiveLineStyle == null)
                        {
                            m_ActiveLineStyle = new GUIStyle(DefaultStyles.label);
                            m_ActiveLineStyle.normal.textColor = DefaultStyles.boldLabel.onActive.textColor;
                        }

                        // The rect is assumed indented and sized after the content when pinging
                        var indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                        rect.xMin += indent;

                        var iconRectWidth = 16;
                        var kSpaceBetweenIconAndText = 2;

                        // Draw icon
                        var iconRect = rect;
                        iconRect.width = iconRectWidth;
                        // iconRect.x += 7f;

                        Texture icon = args.item.icon;
                        if (icon != null)
                            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                        rect.xMin += icon == null ? 0 : iconRectWidth + kSpaceBetweenIconAndText;

                        //bool mouseOver = rect.Contains(Event.current.mousePosition);
                        //DefaultStyles.label.Draw(rect, content, mouseOver, false, args.selected, args.focused);

                        // Must use this call to draw tooltip
                        EditorGUI.LabelField(rect, content, args.selected ? m_ActiveLineStyle : DefaultStyles.label);
                    }
                }
                break;
                case Column.GroupName:
                {
                    var groupName = GetItemGroupName(item);
                    var content = new GUIContent(groupName);
                    EditorGUI.LabelField(cellRect, content);
                }
                break;
                case Column.State:
                    var oldState = TreeItemSelected(item.TreeItemIdentifier);
                    var newState = EditorGUI.Toggle(cellRect, oldState);
                    if (newState != oldState)
                    {
                        if (item.TreeItemIdentifier.nameWithIndex == m_AllIdentifier.nameWithIndex)
                        {
                            // Record active groups
                            m_Selection.groups.Clear();
                            if (newState)
                                if (!m_Selection.groups.Contains(item.TreeItemIdentifier.nameWithIndex))
                                    m_Selection.groups.Add(item.TreeItemIdentifier.nameWithIndex);

                            // Update selection
                            m_Selection.selection.Clear();
                            if (newState)
                                foreach (var nameWithIndex in m_Names)
                                    if (nameWithIndex != m_AllIdentifier.nameWithIndex)
                                    {
                                        var identifier = new TreeItemIdentifier(nameWithIndex);
                                        if (identifier.index != TreeItemIdentifier.kAll)
                                            m_Selection.selection.Add(nameWithIndex);
                                    }
                        }
                        else if (item.TreeItemIdentifier.index == TreeItemIdentifier.kAll)
                        {
                            // Record active groups
                            if (newState)
                            {
                                if (!m_Selection.groups.Contains(item.TreeItemIdentifier.nameWithIndex))
                                    m_Selection.groups.Add(item.TreeItemIdentifier.nameWithIndex);
                            }
                            else
                            {
                                m_Selection.groups.Remove(item.TreeItemIdentifier.nameWithIndex);
                                // When turning off a sub group, turn of the 'all' group too
                                m_Selection.groups.Remove(m_AllIdentifier.nameWithIndex);
                            }

                            // Update selection
                            if (newState)
                            {
                                foreach (var nameWithIndex in m_Names)
                                {
                                    var identifier = new TreeItemIdentifier(nameWithIndex);
                                    if (identifier.name == item.TreeItemIdentifier.name &&
                                        identifier.index != TreeItemIdentifier.kAll)
                                        if (!m_Selection.selection.Contains(nameWithIndex))
                                            m_Selection.selection.Add(nameWithIndex);
                                }
                            }
                            else
                            {
                                var removeSelection = new List<string>();
                                foreach (var nameWithIndex in m_Selection.selection)
                                {
                                    var identifier = new TreeItemIdentifier(nameWithIndex);
                                    if (identifier.name == item.TreeItemIdentifier.name &&
                                        identifier.index != TreeItemIdentifier.kAll)
                                        removeSelection.Add(nameWithIndex);
                                }

                                foreach (var nameWithIndex in removeSelection)
                                    m_Selection.selection.Remove(nameWithIndex);
                            }
                        }
                        else
                        {
                            if (newState)
                            {
                                m_Selection.selection.Add(item.TreeItemIdentifier.nameWithIndex);
                            }
                            else
                            {
                                m_Selection.selection.Remove(item.TreeItemIdentifier.nameWithIndex);

                                // Turn off any group its in too
                                var groupIdentifier = new TreeItemIdentifier(item.TreeItemIdentifier);
                                groupIdentifier.SetAll();
                                m_Selection.groups.Remove(groupIdentifier.nameWithIndex);

                                // Turn of the 'all' group too
                                m_Selection.groups.Remove(m_AllIdentifier.nameWithIndex);
                            }
                        }
                    }

                    break;
            }
        }

        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(HeaderData[] headerData)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();

            foreach (var header in headerData)
                columnList.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = header.content,
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = header.width,
                    minWidth = header.minWidth,
                    autoResize = header.autoResize,
                    allowToggleVisibility = header.allowToggleVisibility
                });
            ;
            var columns = columnList.ToArray();

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(Column)).Length,
                "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            state.visibleColumns = new[]
            {
                (int)Column.ItemName,
                (int)Column.State
                //(int)MyColumns.GroupName
            };
            return state;
        }

        internal struct HeaderData
        {
            public GUIContent content;
            public float width;
            public float minWidth;
            public bool autoResize;
            public bool allowToggleVisibility;

            public HeaderData(string name, string tooltip = "", float _width = 50, float _minWidth = 30,
                              bool _autoResize = true, bool _allowToggleVisibility = true)
            {
                content = new GUIContent(name, tooltip);
                width = _width;
                minWidth = _minWidth;
                autoResize = _autoResize;
                allowToggleVisibility = _allowToggleVisibility;
            }
        }

//        protected override void SelectionChanged(IList<int> selectedIds)
//        {
//            base.SelectionChanged(selectedIds);
//
//            if (selectedIds.Count > 0)
//            {
//            }
//        }
    }


    // stephenm TODO - Can ditch this if we ditch sorting.
    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            if (ascending)
                return source.OrderBy(selector);
            return source.OrderByDescending(selector);
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector,
            bool ascending)
        {
            if (ascending)
                return source.ThenBy(selector);
            return source.ThenByDescending(selector);
        }
    }
}
