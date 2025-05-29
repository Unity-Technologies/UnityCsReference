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
    class AssetMarkerTreeViewItem : TreeViewItem
    {
        public AssetLoadMarker assetLoadingMarker { get; set; }

        public AssetMarkerTreeViewItem(int id, int depth, string displayName, AssetLoadMarker data) : base(id, depth, displayName)
        {
            assetLoadingMarker = data;
        }
    }

    class AssetMarkerTreeView : TreeView
    {
        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);
        AssetCaptureData m_CaptureData;
        AssetLoadingProfilerView m_ProfilerView;
        private int m_Count = 0;

        public enum SortOption
        {
            Index,
            Source,
            AssetName,
            ThreadName,
            Type,
            SizeBytes,
            MarkerType,
            Timestamp,
            MarkerTimeMs,
            FirstFrameIndex,
            LastFrameIndex,
            FrameCount,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.Index,
            SortOption.Source,
            SortOption.AssetName,
            SortOption.ThreadName,
            SortOption.Type,
            SortOption.SizeBytes,
            SortOption.MarkerType,
            SortOption.Timestamp,
            SortOption.MarkerTimeMs,
            SortOption.FirstFrameIndex,
            SortOption.LastFrameIndex,
            SortOption.FrameCount,
        };

        public AssetMarkerTreeView(TreeViewState treeViewState, MultiColumnHeader multicolumnHeader, AssetCaptureData captureData, AssetLoadingProfilerView assetLoadingProfilerView)
            : base(treeViewState, multicolumnHeader)
        {
            m_CaptureData = captureData;
            m_ProfilerView = assetLoadingProfilerView;

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
            AssetMarkerTreeViewItem root = new AssetMarkerTreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root", null);
            AssetLoadingFrameSetup frameSetup = m_ProfilerView.GetFrameSetup();
            int currentFrame = m_ProfilerView.GetSelectedFrame();
            bool singleFrame = frameSetup == AssetLoadingFrameSetup.ThisFrame;

            var data = m_CaptureData.m_AssetLoadMarkers.OrderBy(s => s.startNs).ToList();

            // reset the states
            foreach (var item in data)
            {
                item.addedToTreeView = false;
            }

            for (int index = 0, id = 0; index < data.Count; ++index)
            {
                CreateAndAddChild(data, root, currentFrame, singleFrame, ref index, ref id);
            }

            // Return root of the tree
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected void CreateAndAddChild(List<AssetLoadMarker> data, AssetMarkerTreeViewItem parent, int currentFrame, bool singleFrame, ref int index, ref int id)
        {
            if (data[index].addedToTreeView)
                return;

            var assetLoadMarker = data[index];

            if (!m_ProfilerView.FilterContains(assetLoadMarker.sourceName) && !m_ProfilerView.FilterContains(assetLoadMarker.assetName))
                return;

            if (singleFrame)
            {
                if (currentFrame < assetLoadMarker.firstFrameIndex || currentFrame > assetLoadMarker.lastFrameIndex)
                    return;
            }

            if (!m_ProfilerView.FilterActive())
            {
                assetLoadMarker.index = id;
            }

            var item = new AssetMarkerTreeViewItem(id, 0, assetLoadMarker.sourceName, assetLoadMarker);
            parent.AddChild(item);
            id++;
            m_Count++;
            data[index].addedToTreeView = true; // Avoid duplicates

            if (index + 1 >= data.Count)
                return;

            var nextMarker = data[index + 1];
            int lastIndex = index; // Record where to come back to when done checking for children

            // As markers are ordered by start time, only need to check until we find one that starts after this one has finished
            while (nextMarker.startNs < assetLoadMarker.endNs)
            {
                index++;

                if (!nextMarker.addedToTreeView && nextMarker.endNs <= assetLoadMarker.endNs && nextMarker.depth > assetLoadMarker.depth && nextMarker.threadIndex == assetLoadMarker.threadIndex)
                {
                    CreateAndAddChild(data, item, currentFrame, singleFrame, ref index, ref id);
                }

                if (index + 1 >= data.Count)
                    break;
                nextMarker = data[index + 1];
            }

            index = lastIndex;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            if (rootItem != null && rootItem.children != null)
            {
                foreach (AssetMarkerTreeViewItem node in rootItem.children)
                {
                    AddAllChildren(m_Rows, node);
                }
            }

            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            columnIndexForTreeFoldouts = (int)Columns.Source;

            var item = (AssetMarkerTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (Columns)args.GetColumn(i), ref args);
            }
        }

        private void CellLabel(Rect cellRect, AssetMarkerTreeViewItem item, GUIContent content)
        {
            EditorGUI.LabelField(cellRect, content);
        }

        public int GetCount()
        {
            return m_Count;
        }

        void CellGUI(Rect cellRect, AssetMarkerTreeViewItem item, Columns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case Columns.Index:
                    int id = item.assetLoadingMarker.index > 0 ? item.assetLoadingMarker.index + 1 : item.id + 1;
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", id)));
                    break;
                case Columns.Source:
                {
                    var indentX = GetContentIndent(item);
                    cellRect.x += indentX;
                    cellRect.width -= indentX;

                    string prefix = GetSourcePrefix(item.assetLoadingMarker.type);

                    if (string.IsNullOrEmpty(item.assetLoadingMarker.sourceName))
                    {
                        CellLabel(cellRect, item, new GUIContent($"{prefix}{item.assetLoadingMarker.readableFileName}", item.assetLoadingMarker.readablePath));
                    }
                    else
                    {
                        CellLabel(cellRect, item, new GUIContent($"{prefix}{item.assetLoadingMarker.sourceName}", item.assetLoadingMarker.readablePath));
                    }
                    break;
                }
                case Columns.AssetName:
                    CellLabel(cellRect, item, new GUIContent(item.assetLoadingMarker.assetName));
                    break;
                case Columns.ThreadName:
                    CellLabel(cellRect, item, new GUIContent(item.assetLoadingMarker.threadName));
                    break;
                case Columns.Type:
                    CellLabel(cellRect, item, new GUIContent(item.assetLoadingMarker.subsystem));
                    break;
                case Columns.SizeBytes:
                {
                    if (item.assetLoadingMarker.sizeBytes == 0)
                        CellLabel(cellRect, item, new GUIContent("Unknown size"));
                    else
                        CellLabel(cellRect, item, new GUIContent(string.Format("{0}", EditorUtility.FormatBytes((int)item.assetLoadingMarker.sizeBytes)), string.Format("{0} B", item.assetLoadingMarker.sizeBytes)));
                    break;
                }
                case Columns.MarkerType:
                    CellLabel(cellRect, item, new GUIContent(item.assetLoadingMarker.type.ToString()));
                    break;
                case Columns.Timestamp:
                {
                    var ts = TimeSpan.FromMilliseconds(item.assetLoadingMarker.startTimeMs);
                    string time = string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                    CellLabel(cellRect, item, new GUIContent(time, string.Format("{0}ms", item.assetLoadingMarker.startTimeMs)));
                    break;
                }
                case Columns.MarkerTimeMs:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0:f2}", item.assetLoadingMarker.ms)));
                    break;
                case Columns.FirstFrameIndex:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.assetLoadingMarker.firstFrameIndex)));
                    break;
                case Columns.LastFrameIndex:
                    CellLabel(cellRect, item, new GUIContent(string.Format("{0}", item.assetLoadingMarker.lastFrameIndex)));
                    break;
                case Columns.FrameCount:
                {
                    string label;
                    if (m_ProfilerView.GetFrameSetup() == AssetLoadingFrameSetup.ThisFrame)
                    {
                        int selectedFrame = m_ProfilerView.GetSelectedFrame(); // will the range of the profiler frames always match up with the item.fileAccessMarker.frameIndexes?
                        int framesSinceAccessStart = (selectedFrame - item.assetLoadingMarker.firstFrameIndex) + 1;
                        label = $"{framesSinceAccessStart}/{item.assetLoadingMarker.frameCount}";
                    }
                    else
                    {
                        label = item.assetLoadingMarker.frameCount.ToString();
                    }

                    CellLabel(cellRect, item, new GUIContent(label));
                    break;
                }
            }

            ShowContextMenu(cellRect, item.assetLoadingMarker);
        }

        void ShowContextMenu(Rect cellRect, AssetLoadMarker marker /*, GUIContent content*/) // Keep these in case we add 'Copy to Clipboard' later
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
            public static readonly GUIContent filterToThisSource = new GUIContent("Filter the view to this source", "");
            public static readonly GUIContent filterToThisAsset = new GUIContent("Filter the view to this asset name", "");
        }

        GenericMenu GenerateActiveContextMenu(AssetLoadMarker marker /*, Event evt, GUIContent content*/) // Keep these in case we add 'Copy to Clipboard' later
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
            if (!string.IsNullOrEmpty(marker.sourceName))
                menu.AddItem(Styles.filterToThisSource, false, () => m_ProfilerView.FilterBy(marker.sourceName));
            else
                menu.AddDisabledItem(Styles.filterToThisSource);

            if (!string.IsNullOrEmpty(marker.assetName))
                menu.AddItem(Styles.filterToThisAsset, false, () => m_ProfilerView.FilterBy(marker.assetName));
            else
                menu.AddDisabledItem(Styles.filterToThisAsset);

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
        public enum Columns
        {
            Index,
            Source,
            AssetName,
            ThreadName,
            Type,
            SizeBytes,
            MarkerType,
            Timestamp,
            MarkerTimeMs,
            FirstFrameIndex,
            LastFrameIndex,
            FrameCount,
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();
            HeaderData[] headerData = new HeaderData[]
            {
                new HeaderData("Index", "Ordered by start time and marker depth", 30),
                new HeaderData("Source", "Source of the asset", 150, 80, true, false),
                new HeaderData("Asset Name", "Name of the asset being loaded", 100, 80),
                new HeaderData("Thread", "Thread the load is performed on", 100, 80),
                new HeaderData("Type", "Type of the object being loaded, where known", 100, 80),
                new HeaderData("Size", "Size of the data loaded by this marker, excluding children", 60),
                new HeaderData("Marker Type", "Marker Type", 60),
                new HeaderData("Timestamp", "Time since capture start", 60),
                new HeaderData("Marker Length (ms)", "Length of the marker in milliseconds", 60),
                new HeaderData("First Frame Index", "The frame this marker started on", 60),
                new HeaderData("Last Frame Index", "The frame this marker ended on", 60),
                new HeaderData("Frame Count", "Number of frames this marker covers", 60),
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

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(Columns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);

            // Reordering this changes the order they appear in
            state.visibleColumns = new int[]
            {
                (int)Columns.Index,
                (int)Columns.Source,
                (int)Columns.AssetName,
                (int)Columns.ThreadName,
                (int)Columns.Type,
                (int)Columns.SizeBytes,
                (int)Columns.MarkerType,
                //(int)Columns.Timestamp,
                (int)Columns.MarkerTimeMs,
                //(int)Columns.FirstFrameIndex,
                //(int)Columns.LastFrameIndex,
                (int)Columns.FrameCount
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
            SortByMultipleColumns(rootItem);

            // Update the data with the sorted content
            rows.Clear();
            foreach (AssetMarkerTreeViewItem node in rootItem.children)
            {
                AddAllChildren(rows, node);
            }

            Repaint();
        }

        void AddAllChildren(IList<TreeViewItem> rows, AssetMarkerTreeViewItem node)
        {
            rows.Add(node);
            if (!node.hasChildren || !IsExpanded(node.id))
            {
                return;
            }
            SortByMultipleColumns(node);
            foreach (AssetMarkerTreeViewItem child in node.children)
            {
                AddAllChildren(rows, child);
            }
        }

        void SortByMultipleColumns(TreeViewItem parent)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var myTypes = parent.children.Cast<AssetMarkerTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.Index:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.index, ascending);
                        break;
                    case SortOption.Source:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.sourceName, ascending);
                        break;
                    case SortOption.AssetName:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.assetName, ascending);
                        break;
                    case SortOption.ThreadName:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.threadName, ascending);
                        break;
                    case SortOption.Type:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.subsystem, ascending);
                        break;
                    case SortOption.SizeBytes:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.sizeBytes, ascending);
                        break;
                    case SortOption.MarkerType:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.type, ascending);
                        break;
                    case SortOption.Timestamp:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.startNs, ascending);
                        break;
                    case SortOption.MarkerTimeMs:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.ms, ascending);
                        break;
                    case SortOption.FirstFrameIndex:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.firstFrameIndex, ascending);
                        break;
                    case SortOption.LastFrameIndex:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.lastFrameIndex, ascending);
                        break;
                    case SortOption.FrameCount:
                        orderedQuery = orderedQuery.ThenBy(l => l.assetLoadingMarker.frameCount, ascending);
                        break;
                    default:
                        Assert.IsTrue(false, "Unhandled enum");
                        break;
                }
            }

            parent.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<AssetMarkerTreeViewItem> InitialOrder(IEnumerable<AssetMarkerTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.Index:
                    return myTypes.Order(l => l.assetLoadingMarker.index, ascending);
                case SortOption.Source:
                    return myTypes.Order(l => l.assetLoadingMarker.sourceName, ascending);
                case SortOption.AssetName:
                    return myTypes.Order(l => l.assetLoadingMarker.assetName, ascending);
                case SortOption.ThreadName:
                    return myTypes.Order(l => l.assetLoadingMarker.threadName, ascending);
                case SortOption.Type:
                    return myTypes.Order(l => l.assetLoadingMarker.subsystem, ascending);
                case SortOption.SizeBytes:
                    return myTypes.Order(l => l.assetLoadingMarker.sizeBytes, ascending);
                case SortOption.MarkerType:
                    return myTypes.Order(l => l.assetLoadingMarker.type, ascending);
                case SortOption.Timestamp:
                    return myTypes.Order(l => l.assetLoadingMarker.startNs, ascending);
                case SortOption.MarkerTimeMs:
                    return myTypes.Order(l => l.assetLoadingMarker.ms, ascending);
                case SortOption.FirstFrameIndex:
                    return myTypes.Order(l => l.assetLoadingMarker.firstFrameIndex, ascending);
                case SortOption.LastFrameIndex:
                    return myTypes.Order(l => l.assetLoadingMarker.lastFrameIndex, ascending);
                case SortOption.FrameCount:
                    return myTypes.Order(l => l.assetLoadingMarker.frameCount, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.assetLoadingMarker.index, ascending);
        }

        string GetSourcePrefix(AssetMarkerType type)
        {
            switch (type)
            {
                case (AssetMarkerType.SyncLoadAsset):
                case (AssetMarkerType.AsyncLoadAsset):
                case (AssetMarkerType.SyncUnloadBundle):
                case (AssetMarkerType.AsyncUnloadBundle):
                case (AssetMarkerType.LoadBundle):
                    return "AssetBundle: ";
                case (AssetMarkerType.LoadScene):
                    return "Scene File: ";
                case (AssetMarkerType.LoadSceneObjects):
                case (AssetMarkerType.ReadObject):
                case (AssetMarkerType.SyncReadRequest):
                case (AssetMarkerType.AsyncReadRequest):
                    return "File: ";
                default:
                    return "";
            }
        }
    }
}
