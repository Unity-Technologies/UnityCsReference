// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using UnityEditorInternal.Profiling;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditorInternal
{
    internal class ProfilerFrameDataTreeView : TreeView
    {
        public static readonly GUIContent kFrameTooltip = EditorGUIUtility.TextContent("|Press 'F' to frame selection");

        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(1000);
        ProfilerFrameDataMultiColumnHeader m_MultiColumnHeader;

        FrameDataView m_FrameDataView;
        FrameDataView.MarkerPath? m_SelectedItemMarkerIdPath;
        string m_LegacySelectedItemMarkerNamePath;
        HashSet<FrameDataView.MarkerPath> m_ExpandedMarkerIdPaths;

        public delegate void SelectionChangedCallback(int id);
        public event SelectionChangedCallback selectionChanged;

        public delegate void SearchChangedCallback(string newSearch);
        public event SearchChangedCallback searchChanged;

        public ProfilerColumn sortedProfilerColumn
        {
            get { return m_MultiColumnHeader.sortedProfilerColumn; }
        }

        public bool sortedProfilerColumnAscending
        {
            get { return m_MultiColumnHeader.sortedProfilerColumnAscending; }
        }

        class FrameDataTreeViewItem : TreeViewItem
        {
            FrameDataView m_FrameDataView;
            bool m_Initialized;
            string[] m_StringProperties;
            string m_ResolvedCallstack;

            public string[] columnStrings
            {
                get { return m_StringProperties; }
            }

            public string resolvedCallstack
            {
                get
                {
                    // Lazy callstack resolution (only when requested)
                    if (m_ResolvedCallstack == null)
                        m_ResolvedCallstack = m_FrameDataView.ResolveItemCallstack(id);
                    return m_ResolvedCallstack;
                }
            }
            public int samplesCount
            {
                get
                {
                    return m_FrameDataView.GetItemSamplesCount(id);
                }
            }

            public FrameDataTreeViewItem(FrameDataView frameDataView, int id, int depth, TreeViewItem parent)
                : base(id, depth, parent, null)
            {
                Assert.IsNotNull(frameDataView);
                m_FrameDataView = frameDataView;
                m_Initialized = false;
            }

            public void Init(ProfilerFrameDataMultiColumnHeader.Column[] columns)
            {
                if (m_Initialized)
                    return;

                m_StringProperties = new string[columns.Length];
                for (var i = 0; i < columns.Length; i++)
                {
                    var data = m_FrameDataView.GetItemColumnData(id, columns[i].profilerColumn);
                    m_StringProperties[i] = data;
                    if (columns[i].profilerColumn == ProfilerColumn.FunctionName)
                        displayName = data;
                }

                m_Initialized = true;
            }
        }

        public ProfilerFrameDataTreeView(TreeViewState state, ProfilerFrameDataMultiColumnHeader multicolumnHeader) : base(state, multicolumnHeader)
        {
            Assert.IsNotNull(multicolumnHeader);
            m_MultiColumnHeader = multicolumnHeader;
            m_MultiColumnHeader.sortingChanged += OnSortingChanged;
        }

        public void SetFrameDataView(FrameDataView frameDataView)
        {
            var needReload = !Equals(m_FrameDataView, frameDataView);
            var needSorting = frameDataView != null && (frameDataView.sortColumn != m_MultiColumnHeader.sortedProfilerColumn ||
                                                        frameDataView.sortColumnAscending != m_MultiColumnHeader.sortedProfilerColumnAscending);

            if (needReload)
            {
                StoreExpandedState();
                StoreSelectedState();
            }

            m_FrameDataView = frameDataView;
            if (needSorting)
                m_FrameDataView.Sort(m_MultiColumnHeader.sortedProfilerColumn, m_MultiColumnHeader.sortedProfilerColumnAscending);

            if (needReload || needSorting)
                Reload();
        }

        void StoreExpandedState()
        {
            if (m_ExpandedMarkerIdPaths != null)
                return;

            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return;
            var oldExpanded = GetExpanded();
            if (oldExpanded.Count == 0)
                return;

            m_ExpandedMarkerIdPaths = new HashSet<FrameDataView.MarkerPath>();
            foreach (var expanded in oldExpanded)
            {
                var markerIdPath = m_FrameDataView.GetItemMarkerIDPath(expanded);
                m_ExpandedMarkerIdPaths.Add(markerIdPath);
            }
        }

        public void SetSelectionFromLegacyPropertyPath(string selectedPropertyPath)
        {
            if (string.IsNullOrEmpty(selectedPropertyPath))
                return;

            m_LegacySelectedItemMarkerNamePath = selectedPropertyPath;
            m_SelectedItemMarkerIdPath = null;
        }

        void StoreSelectedState()
        {
            if (m_SelectedItemMarkerIdPath != null || m_LegacySelectedItemMarkerNamePath != null)
                return;

            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return;
            var oldSelection = GetSelection();
            if (oldSelection.Count == 0)
                return;

            m_SelectedItemMarkerIdPath = m_FrameDataView.GetItemMarkerIDPath(oldSelection[0]);
        }

        bool IsMigratedExpanded(int id)
        {
            if (m_ExpandedMarkerIdPaths == null)
                return IsExpanded(id);

            var markerIdPath = m_FrameDataView.GetItemMarkerIDPath(id);
            return m_ExpandedMarkerIdPaths.Contains(markerIdPath);
        }

        void MigrateExpandedState(List<int> newExpandedIds)
        {
            if (newExpandedIds == null)
                return;

            state.expandedIDs = newExpandedIds;
        }

        void MigrateSelectedState()
        {
            if (m_SelectedItemMarkerIdPath == null && m_LegacySelectedItemMarkerNamePath == null)
                return;

            // Find view id which corresponds to markerPath
            var newSelectedId = m_FrameDataView.GetRootItemID();
            if (m_SelectedItemMarkerIdPath != null)
            {
                foreach (var marker in m_SelectedItemMarkerIdPath.Value.markerIds)
                {
                    var childrenId = m_FrameDataView.GetItemChildren(newSelectedId);
                    foreach (var childId in childrenId)
                    {
                        if (marker == m_FrameDataView.GetItemMarkerID(childId))
                        {
                            newSelectedId = childId;
                            break;
                        }
                    }

                    if (newSelectedId == 0)
                        break;
                }
            }
            else if (m_LegacySelectedItemMarkerNamePath != null)
            {
                var markerIdPath = new List<int>();
                var markerNames = m_LegacySelectedItemMarkerNamePath.Split('/');
                foreach (var markerName in markerNames)
                {
                    var childrenId = m_FrameDataView.GetItemChildren(newSelectedId);
                    foreach (var childId in childrenId)
                    {
                        if (markerName == m_FrameDataView.GetItemFunctionName(childId))
                        {
                            newSelectedId = childId;
                            markerIdPath.Add(m_FrameDataView.GetItemMarkerID(childId));
                            break;
                        }
                    }

                    if (newSelectedId == 0)
                        break;
                }

                m_SelectedItemMarkerIdPath = new FrameDataView.MarkerPath(markerIdPath);
                m_LegacySelectedItemMarkerNamePath = null;
            }

            var newSelection = (newSelectedId == 0) ? new List<int>() : new List<int>() { newSelectedId };
            state.selectedIDs = newSelection;
            FrameItem(newSelectedId);
        }

        public IList<int> GetSelectedInstanceIds()
        {
            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return null;
            var selection = GetSelection();
            if (selection == null || selection.Count == 0)
                return null;

            var instanceIds = new List<int>();
            foreach (var selectedId in selection)
                instanceIds.AddRange(m_FrameDataView.GetItemInstanceIDs(selectedId));
            return instanceIds;
        }

        public void Clear()
        {
            if (m_FrameDataView == null)
                return;

            m_FrameDataView.Dispose();
            m_FrameDataView = null;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var rootID = m_FrameDataView != null ? m_FrameDataView.GetRootItemID() : 0;
            return new FrameDataTreeViewItem(m_FrameDataView, rootID, -1, null);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return m_Rows;

            var newExpandedIds = m_ExpandedMarkerIdPaths == null ? null : new List<int>(m_ExpandedMarkerIdPaths.Count);
            if (!string.IsNullOrEmpty(searchString))
            {
                Search(root, searchString, m_Rows);
            }
            else
            {
                AddAllChildren((FrameDataTreeViewItem)root, m_Rows, newExpandedIds);
            }

            MigrateExpandedState(newExpandedIds);
            MigrateSelectedState();

            return m_Rows;
        }

        void Search(TreeViewItem searchFromThis, string search, List<TreeViewItem> result)
        {
            if (searchFromThis == null)
                throw new ArgumentException("Invalid searchFromThis: cannot be null", "searchFromThis");
            if (string.IsNullOrEmpty(search))
                throw new ArgumentException("Invalid search: cannot be null or empty", "search");

            const int kItemDepth = 0; // tree is flattened when searching

            var stack = new Stack<int>();
            if (m_FrameDataView.HasItemChildren(searchFromThis.id))
            {
                var childrenId = m_FrameDataView.GetItemChildren(searchFromThis.id);
                foreach (var childId in childrenId)
                    stack.Push(childId);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // Matches search?
                var functionName = m_FrameDataView.GetItemFunctionName(current);
                if (functionName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var item = new FrameDataTreeViewItem(m_FrameDataView, current, kItemDepth, searchFromThis);
                    searchFromThis.AddChild(item);
                    result.Add(item);
                }

                if (m_FrameDataView.HasItemChildren(current))
                {
                    var childrenId = m_FrameDataView.GetItemChildren(current);
                    foreach (var childId in childrenId)
                        stack.Push(childId);
                }
            }
        }

        void AddAllChildren(FrameDataTreeViewItem parent, IList<TreeViewItem> newRows, List<int> newExpandedIds)
        {
            var toVisitList = new LinkedList<FrameDataTreeViewItem>();
            toVisitList.AddFirst(parent);

            // Depth-first traversal
            while (toVisitList.First != null)
            {
                var currentItem = toVisitList.First.Value;
                toVisitList.RemoveFirst();

                if (currentItem.depth != -1)
                    newRows.Add(currentItem);

                if (!m_FrameDataView.HasItemChildren(currentItem.id))
                    continue;

                if (currentItem.depth != -1)
                {
                    // Check expansion state from a previous frame view state (marker id path) or current tree view state (frame-specific id).
                    var needsExpansion = IsMigratedExpanded(currentItem.id);
                    if (!needsExpansion)
                    {
                        if (currentItem.children == null)
                            currentItem.children = CreateChildListForCollapsedParent();
                        continue;
                    }

                    if (newExpandedIds != null)
                        newExpandedIds.Add(currentItem.id);
                }

                // Generate children based on the view data.
                // TODO: Potentially we can reuse children list if it's only expansion/collapsing, but that has to be tracked separately.
                var childrenId = m_FrameDataView.GetItemChildren(currentItem.id);
                if (currentItem.children != null)
                {
                    // Reuse existing list.
                    currentItem.children.Clear();
                    currentItem.children.Capacity = childrenId.Length;
                }
                else
                {
                    currentItem.children = new List<TreeViewItem>(childrenId.Length);
                }
                foreach (var childId in childrenId)
                {
                    var child = new FrameDataTreeViewItem(m_FrameDataView, childId, currentItem.depth + 1, currentItem);
                    currentItem.children.Add(child);
                }

                // Add children to the traversal list.
                // We add all of them in front, so it is depth search, but with preserved siblings order.
                LinkedListNode<FrameDataTreeViewItem> prev = null;
                foreach (var child in currentItem.children)
                    prev = prev == null ? toVisitList.AddFirst((FrameDataTreeViewItem)child) : toVisitList.AddAfter(prev, (FrameDataTreeViewItem)child);
            }

            if (newExpandedIds != null)
                newExpandedIds.Sort();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            // When we navigate through frames and there is no path exists,
            // we still want to be able to frame and select proper sample once it is present again.
            // Thus we invalidate selection only if user selected new item.
            // Same applies to expanded state.
            if (selectedIds.Count > 0)
            {
                m_SelectedItemMarkerIdPath = null;
                m_LegacySelectedItemMarkerNamePath = null;
            }

            var id = selectedIds.Count > 0 ? selectedIds[0] : -1;
            if (selectionChanged != null)
                selectionChanged.Invoke(id);
        }

        protected override void ExpandedStateChanged()
        {
            // Invalidate saved expanded state if user altered current state.
            m_ExpandedMarkerIdPaths = null;
        }

        protected override void DoubleClickedItem(int id)
        {
        }

        protected override void ContextClickedItem(int id)
        {
        }

        protected override void ContextClicked()
        {
        }

        protected override void SearchChanged(string newSearch)
        {
            if (searchChanged != null)
                searchChanged.Invoke(newSearch);
        }

        protected override IList<int> GetAncestors(int id)
        {
            if (m_FrameDataView == null)
                return new List<int>();

            return m_FrameDataView.GetItemAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            if (m_FrameDataView == null)
                return new List<int>();

            return m_FrameDataView.GetItemDescendantsThatHaveChildren(id);
        }

        void OnSortingChanged(MultiColumnHeader header)
        {
            if (m_FrameDataView == null || multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            m_FrameDataView.Sort(m_MultiColumnHeader.sortedProfilerColumn, m_MultiColumnHeader.sortedProfilerColumnAscending);
            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            if (m_LegacySelectedItemMarkerNamePath != null)
                MigrateSelectedState();

            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            var item = (FrameDataTreeViewItem)args.item;
            item.Init(m_MultiColumnHeader.columns);

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var cellRect = args.GetCellRect(i);
                CellGUI(cellRect, item, i == 0, args.GetColumn(i), ref args);
            }

            // Tooltip logic only when item is selected
            if (!args.selected)
                return;

            var hovered = args.rowRect.Contains(Event.current.mousePosition);
            if (!hovered)
                return;

            // Framing hint when searching
            if (hasSearch)
            {
                GUIStyle.SetMouseTooltip(kFrameTooltip.tooltip, args.rowRect);
                return;
            }
        }

        void CellGUI(Rect cellRect, FrameDataTreeViewItem item, bool needsIndent, int column, ref RowGUIArgs args)
        {
            if (needsIndent)
            {
                var indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
            }
            CenterRectUsingSingleLineHeight(ref cellRect);

            var content = GUIContent.Temp(item.columnStrings[column], string.Empty);
            DefaultStyles.label.Draw(cellRect, content, false, false, args.selected, args.focused);
        }
    }

    public class ProfilerFrameDataMultiColumnHeader : MultiColumnHeader
    {
        public struct Column
        {
            public ProfilerColumn profilerColumn;
            public GUIContent headerLabel;
        }
        Column[] m_Columns;

        public Column[] columns
        {
            get { return m_Columns; }
        }

        public ProfilerColumn sortedProfilerColumn
        {
            get { return GetProfilerColumn(sortedColumnIndex); }
        }
        public bool sortedProfilerColumnAscending
        {
            get { return IsSortedAscending(sortedColumnIndex); }
        }

        public ProfilerFrameDataMultiColumnHeader(MultiColumnHeaderState state, Column[] columns)
            : base(state)
        {
            Assert.IsNotNull(columns);
            m_Columns = columns;
        }

        public int GetMultiColumnHeaderIndex(ProfilerColumn profilerColumn)
        {
            for (var i = 0; i < m_Columns.Length; ++i)
            {
                if (m_Columns[i].profilerColumn == profilerColumn)
                    return i;
            }

            return 0;
        }

        public static int GetMultiColumnHeaderIndex(Column[] columns, ProfilerColumn profilerColumn)
        {
            for (var i = 0; i < columns.Length; ++i)
            {
                if (columns[i].profilerColumn == profilerColumn)
                    return i;
            }

            return 0;
        }

        public ProfilerColumn GetProfilerColumn(int multiColumnHeaderIndex)
        {
            return m_Columns[multiColumnHeaderIndex].profilerColumn;
        }
    }
}
