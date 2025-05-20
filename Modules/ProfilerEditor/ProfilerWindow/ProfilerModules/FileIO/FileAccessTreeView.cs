// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Assertions;
using System.Linq;
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditor
{
    class FileAccessTreeViewItem : TreeViewItem
    {
        public FileAccessMarker fileAccessMarker { get; set; }

        public FileAccessTreeViewItem(int id, int depth, string displayName, FileAccessMarker data) : base(id, depth, displayName)
        {
            fileAccessMarker = data;
        }
    }

    class FileAccessTreeView : TreeView
    {
        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);
        FileAccessCaptureData m_CaptureData;
        FileIOProfilerView m_ProfilerView;

        public enum SortOption
        {
            Index,
            Filename,
            Type,
            SizeBytes,
            OffsetBytes,
            Duration,
            Bandwidth,
            FirstFrameIndex,
            Frames,
            ThreadName,
            Timestamp,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Index,
            SortOption.Filename,
            SortOption.Type,
            SortOption.SizeBytes,
            SortOption.OffsetBytes,
            SortOption.Duration,
            SortOption.Bandwidth,
            SortOption.FirstFrameIndex,
            SortOption.Frames,
            SortOption.ThreadName,
            SortOption.Timestamp,
        };

        public FileAccessTreeView(TreeViewState treeViewState, MultiColumnHeader multicolumnHeader, FileAccessCaptureData captureData, FileIOProfilerView fileIOProfilerView)
            : base(treeViewState, multicolumnHeader)
        {
            m_CaptureData = captureData;
            m_ProfilerView = fileIOProfilerView;

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI

            multicolumnHeader.canSort = true;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            multicolumnHeader.visibleColumnsChanged += OnVisibleColumnsChanged;

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            FileAccessTreeViewItem root = new FileAccessTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);
            FileIOFrameSetup frameSetup = m_ProfilerView.GetFrameSetup();
            int currentFrame = m_ProfilerView.GetSelectedFrame();

            if (!m_CaptureData.accessesSorted)
            {
                m_CaptureData.m_FileAccessData.Sort((data1, data2) => data1.startNs.CompareTo(data2.startNs));
                m_CaptureData.accessesSorted = true;
            }

            var data = m_CaptureData.m_FileAccessData;
            for (int index = 0, id = 0; index < data.Count; ++index)
            {
                var fileAccess = data[index];

                var filename = Path.GetFileName(fileAccess.filename);

                if (!m_ProfilerView.FilterContains(filename))
                    continue;

                if (frameSetup == FileIOFrameSetup.ThisFrame)
                {
                    if (currentFrame < fileAccess.firstFrameIndex || currentFrame > fileAccess.lastFrameIndex)
                        continue;
                }

                if (!m_ProfilerView.FilterActive())
                    fileAccess.index = id;

                var item = new FileAccessTreeViewItem(id, 0, fileAccess.filename, fileAccess);
                root.AddChild(item);
                id++;
            }

            // Return root of the tree
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem != null && rootItem.children != null)
            {
                foreach (FileAccessTreeViewItem node in rootItem.children)
                {
                    m_Rows.Add(node);
                }
            }

            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (FileAccessTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (FileIOColumns)args.GetColumn(i), ref args);
            }
        }

        private void CellLabel(Rect cellRect, FileAccessTreeViewItem item, GUIContent content)
        {
            EditorGUI.LabelField(cellRect, content);
        }

        public int GetCount()
        {
            return m_Rows.Count;
        }

        void CellGUI(Rect cellRect, FileAccessTreeViewItem item, FileIOColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case FileIOColumns.Index:
                    int id = item.fileAccessMarker.index > 0 ? item.fileAccessMarker.index + 1 : item.id + 1;
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", id)));
                    break;
                case FileIOColumns.Timestamp:
                {
                    var ts = TimeSpan.FromMilliseconds(item.fileAccessMarker.startTimeMs);
                    string time = string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D4}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                    CellLabel(cellRect, item, new GUIContent(time, string.Format("{0}ms", item.fileAccessMarker.startTimeMs)));
                    break;
                }
                case FileIOColumns.Duration:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileAccessMarker.ms)));
                    break;
                case FileIOColumns.Filename:
                    CellLabel(cellRect, item, new GUIContent(item.fileAccessMarker.readableFileName, item.fileAccessMarker.readablePath));
                    break;
                case FileIOColumns.ThreadName:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileAccessMarker.threadName)));
                    break;
                case FileIOColumns.SizeBytes:
                    if (item.fileAccessMarker.type == FileAccessType.Seek)
                        CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.fileAccessMarker.newOffsetBytes)), string.Format("New offset: {0} B", item.fileAccessMarker.newOffsetBytes)));
                    else
                        CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.fileAccessMarker.sizeBytes)), string.Format("{0} B", item.fileAccessMarker.sizeBytes)));
                    break;
                case FileIOColumns.OffsetBytes:
                    if (item.fileAccessMarker.type == FileAccessType.Seek)
                        CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.fileAccessMarker.originBytes)), string.Format("Seek origin: {0} B", item.fileAccessMarker.originBytes)));
                    else
                        CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.fileAccessMarker.offsetBytes)), string.Format("{0} B", item.fileAccessMarker.offsetBytes)));
                    break;
                case FileIOColumns.Type:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileAccessMarker.type.ToString())));
                    break;
                case FileIOColumns.FirstFrameIndex:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileAccessMarker.firstFrameIndex + 1))); // Shift by one to match the indexing by the CPU view
                    break;
                case FileIOColumns.Frames:
                {
                    string label;
                    if (m_ProfilerView.GetFrameSetup() == FileIOFrameSetup.ThisFrame)
                    {
                        int selectedFrame = m_ProfilerView.GetSelectedFrame();     // will the range of frames in the profiler window always match up with the item.fileAccessMarker.frameIndexes?
                        int framesSinceAccessStart = (selectedFrame - item.fileAccessMarker.firstFrameIndex) + 1;
                        label = $"{framesSinceAccessStart}/{item.fileAccessMarker.frameCount}";
                    }
                    else
                    {
                        label = item.fileAccessMarker.frameCount.ToString();
                    }

                    CellLabel(cellRect, item, new GUIContent(label));
                    break;
                }
                case FileIOColumns.Bandwidth:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileAccessMarker.averageBandwidthMBps)));
                    break;
            }

            ShowContextMenu(cellRect, item.fileAccessMarker);
        }

        void ShowContextMenu(Rect cellRect, FileAccessMarker marker /*, GUIContent content*/) // Keep these in case we add 'Copy to Clipboard' later
        {
            Event current = Event.current;
            if (cellRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                GenericMenu menu;
                menu = GenerateActiveContextMenu(marker /*, current, content*/);

                menu.ShowAsContext();

                current.Use();
            }
        }

        internal static class Styles
        {
            public static readonly GUIContent goToMarker = new GUIContent("Show marker in timeline view", "");
            public static readonly GUIContent goToFirstFrame = new GUIContent("Go to first frame of this marker", "");
            public static readonly GUIContent goToLastFrame = new GUIContent("Go to last frame of this marker", "");
            public static readonly GUIContent filterToThisPath = new GUIContent("Filter the view to this file path", "");
        }

        GenericMenu GenerateActiveContextMenu(FileAccessMarker marker /*, Event evt, GUIContent content*/) // Keep these in case we add 'Copy to Clipboard' later
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(Styles.goToMarker, false, () => m_ProfilerView.GoToMarker(marker));
            menu.AddSeparator("");
            if (marker.frameCount == 1)
            {
                menu.AddDisabledItem(Styles.goToFirstFrame);
                menu.AddDisabledItem(Styles.goToLastFrame);
            }
            else
            {
                menu.AddItem(Styles.goToFirstFrame, false, () => m_ProfilerView.GoToFrameInModule(marker.firstFrameIndex));
                menu.AddItem(Styles.goToLastFrame, false, () => m_ProfilerView.GoToFrameInModule(marker.lastFrameIndex));
            }

            menu.AddSeparator("");
            if (!string.IsNullOrEmpty(marker.filename))
                menu.AddItem(Styles.filterToThisPath, false, () => m_ProfilerView.FilterBy(marker.readableFileName));
            else
                menu.AddDisabledItem(Styles.filterToThisPath);

            return menu;
        }

        struct HeaderData
        {
            public GUIContent content;
            public float width;
            public float minWidth;
            public bool autoResize;
            public bool allowToggleVisibility;

            public HeaderData(string name, string tooltip = "", float _width = 50, float _minWidth = 30, bool _autoResize = true, bool _allowToggleVisibility = true)
            {
                content = new GUIContent(name, tooltip);
                width = _width;
                minWidth = _minWidth;
                autoResize = _autoResize;
                allowToggleVisibility = _allowToggleVisibility;
            }
        }

        // All columns
        public enum FileIOColumns
        {
            Index,
            Filename,
            Type,
            SizeBytes,
            OffsetBytes,
            Duration,
            Bandwidth,
            FirstFrameIndex,
            Frames,
            ThreadName,
            Timestamp,
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();
            HeaderData[] headerData = new HeaderData[]
            {
                new HeaderData("Index", "Order of execution of markers", 30),
                new HeaderData("Filename", "Filename", 250, 100, true, false),
                new HeaderData("Type", "Access type", 60),
                new HeaderData("Access Size", "Size read or written during the file access", 60),
                new HeaderData("Offset", "Offset of the file access", 60),
                new HeaderData("Duration (ms)", "Marker duration (ms)", 60),
                new HeaderData("Avg Bandwidth (MBps)", "The average rate of data transfer for this file access", 60),
                new HeaderData("First Frame Index", "The index of the first frame this access appears on", 60),
                new HeaderData("Frames", "This frame in the Frames covered by this File Access", 60),
                new HeaderData("Thread", "Thread name", 100),
                new HeaderData("Timestamp", "Time marker started from Start", 60),
            };
            foreach (var header in headerData)
            {
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
            }
            ;
            var columns = columnList.ToArray();

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(FileIOColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);

            // Reordering this changes the order they appear in
            state.visibleColumns = new int[]
            {
                (int)FileIOColumns.Index,
                (int)FileIOColumns.Filename,
                (int)FileIOColumns.Type,
                (int)FileIOColumns.SizeBytes,
                (int)FileIOColumns.OffsetBytes,
                (int)FileIOColumns.Duration,
                (int)FileIOColumns.Bandwidth,
                (int)FileIOColumns.FirstFrameIndex,
                (int)FileIOColumns.Frames,
                (int)FileIOColumns.ThreadName,
                //(int)FileIOColumns.Timestamp,
            };

            return state;
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        protected virtual void OnVisibleColumnsChanged(MultiColumnHeader multiColumnHeader)
        {
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            // Sort the roots of the existing tree items
            SortByMultipleColumns();

            // Update the data with the sorted content
            rows.Clear();
            foreach (FileAccessTreeViewItem node in rootItem.children)
            {
                rows.Add(node);
            }

            Repaint();
        }

        void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var myTypes = rootItem.children.Cast<FileAccessTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.Index:
                        orderedQuery = orderedQuery.ThenBy(l => l.id, ascending);
                        break;
                    case SortOption.Timestamp:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.startNs, ascending);
                        break;
                    case SortOption.Duration:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.ms, ascending);
                        break;
                    case SortOption.Filename:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.readableFileName, ascending);
                        break;
                    case SortOption.ThreadName:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.threadName, ascending);
                        break;
                    case SortOption.SizeBytes:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.sizeBytes, ascending);
                        break;
                    case SortOption.OffsetBytes:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.offsetBytes, ascending);
                        break;
                    case SortOption.Type:
                        orderedQuery = orderedQuery.ThenBy(l => (int)(l.fileAccessMarker.type), ascending);
                        break;
                    case SortOption.FirstFrameIndex:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.firstFrameIndex, ascending);
                        break;
                    case SortOption.Frames:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.frameCount, ascending);
                        break;
                    case SortOption.Bandwidth:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileAccessMarker.averageBandwidthMBps, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<FileAccessTreeViewItem> InitialOrder(IEnumerable<FileAccessTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Index:
                    return myTypes.Order(l => l.id, ascending);
                case SortOption.Timestamp:
                    return myTypes.Order(l => l.fileAccessMarker.startNs, ascending);
                case SortOption.Duration:
                    return myTypes.Order(l => l.fileAccessMarker.ms, ascending);
                case SortOption.Filename:
                    return myTypes.Order(l => l.fileAccessMarker.readableFileName, ascending);
                case SortOption.ThreadName:
                    return myTypes.Order(l => l.fileAccessMarker.threadName, ascending);
                case SortOption.SizeBytes:
                    return myTypes.Order(l => l.fileAccessMarker.sizeBytes, ascending);
                case SortOption.OffsetBytes:
                    return myTypes.Order(l => l.fileAccessMarker.offsetBytes, ascending);
                case SortOption.Type:
                    return myTypes.Order(l => (int)(l.fileAccessMarker.type), ascending);
                case SortOption.FirstFrameIndex:
                    return myTypes.Order(l => l.fileAccessMarker.firstFrameIndex, ascending);
                case SortOption.Frames:
                    return myTypes.Order(l => l.fileAccessMarker.frameCount, ascending);
                case SortOption.Bandwidth:
                    return myTypes.Order(l => l.fileAccessMarker.averageBandwidthMBps, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.fileAccessMarker.startNs, ascending);
        }
    }
}
