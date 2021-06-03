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

namespace UnityEditor
{
    class FileSummaryTreeViewItem : TreeViewItem
    {
        public FileSummary fileSummary { get; set; }

        public FileSummaryTreeViewItem(int id, int depth, string displayName, FileSummary data) : base(id, depth, displayName)
        {
            fileSummary = data;
        }
    }

    class FileSummaryTreeView : TreeView
    {
        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);
        FileAccessCaptureData m_CaptureData;
        FileIOProfilerView m_ProfilerView;
        public enum SortOption // Must be in the same order as SummaryColumns
        {
            Id,
            Filename,
            TotalBytesRead,
            ReadAccessMs,
            ReadBandwidthMBps,
            TotalAccessCount,
            OpenCount,
            CloseCount,
            ReadCount,
            WriteCount,
            SeekCount,
            TotalBytesWritten,
            WriteBandwidthMBps,
            OpenAccessMs,
            CloseAccessMs,
            WriteAccessMs,
            TotalAccessTimeMs,
            FirstFrame,
            NumberOfFrames,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Id,
            SortOption.Filename,
            SortOption.TotalBytesRead,
            SortOption.ReadAccessMs,
            SortOption.ReadBandwidthMBps,
            SortOption.TotalAccessCount,
            SortOption.OpenCount,
            SortOption.CloseCount,
            SortOption.ReadCount,
            SortOption.WriteCount,
            SortOption.SeekCount,
            SortOption.TotalBytesWritten,
            SortOption.WriteBandwidthMBps,
            SortOption.OpenAccessMs,
            SortOption.CloseAccessMs,
            SortOption.WriteAccessMs,
            SortOption.TotalAccessTimeMs,
            SortOption.FirstFrame,
            SortOption.NumberOfFrames,
        };

        public FileSummaryTreeView(TreeViewState treeViewState, MultiColumnHeader multicolumnHeader, FileAccessCaptureData captureData, FileIOProfilerView fileIOProfilerView)
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
            int index = 0;
            FileSummaryTreeViewItem root = new FileSummaryTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);
            FileIOFrameSetup frameSetup = m_ProfilerView.GetFrameSetup();
            int currentFrame = m_ProfilerView.GetSelectedFrame();

            foreach (var fileSummary in m_CaptureData.m_FileSummaryData.Values)
            {
                var filename = Path.GetFileName(fileSummary.filename);

                if (!m_ProfilerView.FilterContains(filename))
                    continue;

                if (frameSetup == FileIOFrameSetup.ThisFrame)
                {
                    if (!fileSummary.frameIndices.Contains(currentFrame))
                        continue;
                }

                var item = new FileSummaryTreeViewItem(index, 0, fileSummary.filename, fileSummary);
                root.AddChild(item);
                index++;
            }

            // Return root of the tree
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem != null && rootItem.children != null)
            {
                foreach (FileSummaryTreeViewItem node in rootItem.children)
                {
                    m_Rows.Add(node);
                }
            }

            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        public int GetCount()
        {
            return m_Rows.Count;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (FileSummaryTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (SummaryColumns)args.GetColumn(i), ref args);
            }
        }

        private void CellLabel(Rect cellRect, FileSummaryTreeViewItem item, GUIContent content)
        {
            EditorGUI.LabelField(cellRect, content);
        }

        void CellGUI(Rect cellRect, FileSummaryTreeViewItem item, SummaryColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (column)
            {
                case SummaryColumns.Id:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.id + 1)));
                    break;
                case SummaryColumns.Filename:
                    CellLabel(cellRect, item, new GUIContent(item.fileSummary.readableFileName, item.fileSummary.readablePath));
                    break;
                case SummaryColumns.TotalAccessCount:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.accesses)));
                    break;
                case SummaryColumns.ReadCount:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.reads)));
                    break;
                case SummaryColumns.WriteCount:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.writes)));
                    break;
                case SummaryColumns.SeekCount:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.seeks)));
                    break;
                case SummaryColumns.OpenCount:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.opened)));
                    break;
                case SummaryColumns.CloseCount:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.closed)));
                    break;
                case SummaryColumns.TotalBytesRead:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.fileSummary.bytesRead)), string.Format("{0} B", item.fileSummary.bytesRead)));
                    break;
                case SummaryColumns.TotalBytesWritten:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.fileSummary.bytesWritten)), string.Format("{0} B", item.fileSummary.bytesWritten)));
                    break;
                case SummaryColumns.TotalAccessTimeMs:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.totalAccessMs)));
                    break;
                case SummaryColumns.ReadBandwidthMBps:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.readBandwidthMegaBytesPerSecond)));
                    break;
                case SummaryColumns.WriteBandwidthMBps:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.writeBandwidthMegaBytesPerSecond)));
                    break;
                case SummaryColumns.OpenAccessMs:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.openAccessMs)));
                    break;
                case SummaryColumns.CloseAccessMs:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.closeAccessMs)));
                    break;
                case SummaryColumns.ReadAccessMs:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.readAccessMs)));
                    break;
                case SummaryColumns.WriteAccessMs:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.fileSummary.writeAccessMs)));
                    break;
                case SummaryColumns.FirstFrame:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.firstFrame + 1))); // shift to match the indexing in the Profiler view
                    break;
                case SummaryColumns.NumberOfFrames:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.fileSummary.frameIndices.Count)));
                    break;
            }
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

        // All columns - Must be in the same order as SortOptions
        public enum SummaryColumns
        {
            Id,
            Filename,
            TotalBytesRead,
            ReadAccessMs,
            ReadBandwidthMBps,
            TotalAccessCount,
            OpenCount,
            CloseCount,
            ReadCount,
            WriteCount,
            SeekCount,
            TotalBytesWritten,
            WriteBandwidthMBps,
            OpenAccessMs,
            CloseAccessMs,
            WriteAccessMs,
            TotalAccessTimeMs,
            FirstFrame,
            NumberOfFrames,
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();
            HeaderData[] headerData = new HeaderData[] // Order here has to be the same as in SummaryColumns
            {
                new HeaderData("Id", "", 10),
                new HeaderData("Filename", "Filename", 250, 100, true, false),
                new HeaderData("Total Bytes Read", "Total number of bytes read from this file", 60),
                new HeaderData("Read Access Time (ms)", "Total time spent in read operations on the file in milliseconds", 60),
                new HeaderData("Read Bandwidth (MBps)", "Rate of data transfer from this file in Megabytes per second", 60),
                new HeaderData("Access Count", "Total number of open, close, seek, read or write accesses on this file", 60),
                new HeaderData("Open Count", "Number of open operations on this file", 60),
                new HeaderData("Close Count", "Number of close operations on this file", 60),
                new HeaderData("Read Count", "Number of reads from this file", 60),
                new HeaderData("Write Count", "Number of writes to this file", 60),
                new HeaderData("Seek Count", "Number of seeks in this file", 60),
                new HeaderData("Total Bytes Written", "Total number of bytes written to this file", 60),
                new HeaderData("Write Bandwidth (MBps)", "Rate of data transfer to this file in Megabytes per second", 60),
                new HeaderData("Open Access Time (ms)", "Total time spent in open operations on the file in milliseconds", 60),
                new HeaderData("Close Access Time (ms)", "Total time spent in close operations on the file in milliseconds", 60),
                new HeaderData("Write Access Time (ms)", "Total time spent in write operations on the file in milliseconds", 60),
                new HeaderData("Total Access Time (ms)", "Time spent across all accesses to the file in milliseconds", 60),
                new HeaderData("First frame", "First frame this file accessed on", 60),
                new HeaderData("Number of Frames", "Number of frames this file accessed on", 60),
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

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(SummaryColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);

            // Reordering this changes the order they appear in
            state.visibleColumns = new int[]
            {
                //(int)SummaryColumns.Id,
                (int)SummaryColumns.Filename,
                (int)SummaryColumns.TotalBytesRead,
                (int)SummaryColumns.ReadAccessMs,
                //(int)SummaryColumns.ReadBandwidthMBps,
                (int)SummaryColumns.TotalAccessCount,
                (int)SummaryColumns.FirstFrame,
                (int)SummaryColumns.NumberOfFrames,
                //(int)SummaryColumns.OpenCount,
                //(int)SummaryColumns.CloseCount,
                //(int)SummaryColumns.ReadCount,
                //(int)SummaryColumns.WriteCount,
                //(int)SummaryColumns.SeekCount,
                //(int)SummaryColumns.TotalBytesWritten,
                //(int)SummaryColumns.WriteBandwidthMBps,
                //(int)SummaryColumns.OpenAccessMs,
                //(int)SummaryColumns.CloseAccessMs,
                //(int)SummaryColumns.WriteAccessMs,
                //(int)SummaryColumns.TotalAccessTimeMs,
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
            foreach (FileSummaryTreeViewItem node in rootItem.children)
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

            var myTypes = rootItem.children.Cast<FileSummaryTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.Id:
                        orderedQuery = orderedQuery.ThenBy(l => l.id, ascending);
                        break;
                    case SortOption.Filename:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.readableFileName, ascending);
                        break;
                    case SortOption.TotalAccessCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.accesses, ascending);
                        break;
                    case SortOption.ReadCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.reads, ascending);
                        break;
                    case SortOption.WriteCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.writes, ascending);
                        break;
                    case SortOption.SeekCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.seeks, ascending);
                        break;
                    case SortOption.OpenCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.opened, ascending);
                        break;
                    case SortOption.CloseCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.closed, ascending);
                        break;
                    case SortOption.TotalBytesRead:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.bytesRead, ascending);
                        break;
                    case SortOption.TotalBytesWritten:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.bytesWritten, ascending);
                        break;
                    case SortOption.TotalAccessTimeMs:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.totalAccessMs, ascending);
                        break;
                    case SortOption.ReadBandwidthMBps:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.readBandwidthMegaBytesPerSecond, ascending);
                        break;
                    case SortOption.WriteBandwidthMBps:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.writeBandwidthMegaBytesPerSecond, ascending);
                        break;
                    case SortOption.OpenAccessMs:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.openAccessMs, ascending);
                        break;
                    case SortOption.CloseAccessMs:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.closeAccessMs, ascending);
                        break;
                    case SortOption.ReadAccessMs:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.readAccessMs, ascending);
                        break;
                    case SortOption.WriteAccessMs:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.writeAccessMs, ascending);
                        break;
                    case SortOption.FirstFrame:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.firstFrame, ascending);
                        break;
                    case SortOption.NumberOfFrames:
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.frameIndices.Count, ascending);
                        break;
                    default:
                        Assert.IsTrue(false, "Unhandled enum");
                        orderedQuery = orderedQuery.ThenBy(l => l.fileSummary.bytesRead, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<FileSummaryTreeViewItem> InitialOrder(IEnumerable<FileSummaryTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Id:
                    return myTypes.Order(l => l.id, ascending);
                case SortOption.Filename:
                    return myTypes.Order(l => l.fileSummary.readableFileName, ascending);
                case SortOption.TotalAccessCount:
                    return myTypes.Order(l => l.fileSummary.accesses, ascending);
                case SortOption.ReadCount:
                    return myTypes.Order(l => l.fileSummary.reads, ascending);
                case SortOption.WriteCount:
                    return myTypes.Order(l => l.fileSummary.writes, ascending);
                case SortOption.SeekCount:
                    return myTypes.Order(l => l.fileSummary.seeks, ascending);
                case SortOption.OpenCount:
                    return myTypes.Order(l => l.fileSummary.opened, ascending);
                case SortOption.CloseCount:
                    return myTypes.Order(l => l.fileSummary.closed, ascending);
                case SortOption.TotalBytesRead:
                    return myTypes.Order(l => l.fileSummary.bytesRead, ascending);
                case SortOption.TotalBytesWritten:
                    return myTypes.Order(l => l.fileSummary.bytesWritten, ascending);
                case SortOption.TotalAccessTimeMs:
                    return myTypes.Order(l => l.fileSummary.totalAccessMs, ascending);
                case SortOption.ReadBandwidthMBps:
                    return myTypes.Order(l => l.fileSummary.readBandwidthMegaBytesPerSecond, ascending);
                case SortOption.WriteBandwidthMBps:
                    return myTypes.Order(l => l.fileSummary.writeBandwidthMegaBytesPerSecond, ascending);
                case SortOption.OpenAccessMs:
                    return myTypes.Order(l => l.fileSummary.openAccessMs, ascending);
                case SortOption.CloseAccessMs:
                    return myTypes.Order(l => l.fileSummary.closeAccessMs, ascending);
                case SortOption.ReadAccessMs:
                    return myTypes.Order(l => l.fileSummary.readAccessMs, ascending);
                case SortOption.WriteAccessMs:
                    return myTypes.Order(l => l.fileSummary.writeAccessMs, ascending);
                case SortOption.FirstFrame:
                    return myTypes.Order(l => l.fileSummary.firstFrame, ascending);
                case SortOption.NumberOfFrames:
                    return myTypes.Order(l => l.fileSummary.frameIndices.Count, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.fileSummary.bytesRead, ascending);
        }
    }
    static class FileIOModuleExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
