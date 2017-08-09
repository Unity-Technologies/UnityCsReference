// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class UISystemProfilerTreeView : TreeView
    {
        private readonly CanvasBatchComparer m_Comparer;

        public ProfilerProperty property;

        private RootTreeViewItem m_AllCanvasesItem;

        public UISystemProfilerTreeView(State state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            m_Comparer = new CanvasBatchComparer();
            showBorder = false;
            showAlternatingRowBackgrounds = true;
        }

        public State profilerState
        {
            get { return (State)state; }
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem(0, -1);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            //Debug.Log(sortCol + " " + sortColumn.sortedAscending);
            profilerState.lastFrame = profilerState.profilerWindow.GetActiveVisibleFrameIndex();

            var rows = new List<TreeViewItem>();
            if (property == null || !property.frameDataReady)
                return rows;
            m_AllCanvasesItem = new RootTreeViewItem();
            SetExpanded(m_AllCanvasesItem.id, true);
            root.AddChild(m_AllCanvasesItem);
            UISystemProfilerInfo[] UISystemData = property.GetUISystemProfilerInfo();
            int[] allBatchesInstanceIDs = property.GetUISystemBatchInstanceIDs();

            if (UISystemData != null)
            {
                Dictionary<int, TreeViewItem> map = new Dictionary<int, TreeViewItem>();
                int batchIndex = 0;
                foreach (var data in UISystemData)
                {
                    TreeViewItem parent;
                    if (!map.TryGetValue(data.parentId, out parent))
                    {
                        parent = m_AllCanvasesItem;
                        m_AllCanvasesItem.totalBatchCount += data.totalBatchCount;
                        m_AllCanvasesItem.totalVertexCount += data.totalVertexCount;
                        m_AllCanvasesItem.gameObjectCount += data.instanceIDsCount;
                    }
                    string name;
                    BaseTreeViewItem canvasTreeViewItem;
                    if (data.isBatch)
                    {
                        name = "Batch " + batchIndex++;
                        canvasTreeViewItem = new BatchTreeViewItem(data, parent.depth + 1, name, allBatchesInstanceIDs);
                    }
                    else
                    {
                        name = property.GetUISystemProfilerNameByOffset(data.objectNameOffset);
                        canvasTreeViewItem = new CanvasTreeViewItem(data, parent.depth + 1, name);
                        batchIndex = 0;
                        map[data.objectInstanceId] = canvasTreeViewItem;
                    }
                    if (!IsExpanded(parent.id))
                    {
                        if (!parent.hasChildren)
                            parent.children = CreateChildListForCollapsedParent();
                        continue;
                    }
                    parent.AddChild(canvasTreeViewItem);
                }

                m_Comparer.Col = Column.Element;
                if (multiColumnHeader.sortedColumnIndex != -1)
                    m_Comparer.Col = (Column)multiColumnHeader.sortedColumnIndex;
                m_Comparer.isAscending = multiColumnHeader.GetColumn((int)m_Comparer.Col).sortedAscending;

                SetupRows(m_AllCanvasesItem, rows);
            }
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            for (int i = 0, count = args.GetNumVisibleColumns(); i < count; i++)
            {
                int column = args.GetColumn(i);
                Rect rect = args.GetCellRect(i);
                if (column == (int)Column.Element)
                {
                    GUIStyle lineStyle = DefaultStyles.label;
                    rect.xMin += lineStyle.margin.left + GetContentIndent(args.item);

                    int iconRectWidth = 16;
                    int kSpaceBetweenIconAndText = 2;

                    // Draw icon
                    Rect iconRect = rect;
                    iconRect.width = iconRectWidth;

                    Texture icon = args.item.icon;
                    if (icon != null)
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                    // Draw text
                    lineStyle.padding.left = icon == null ? 0 : iconRectWidth + kSpaceBetweenIconAndText;
                    lineStyle.Draw(rect, args.item.displayName, false, false, args.selected, args.focused);

                    continue;
                }

                string content = GetItemcontent(args, column);
                if (content != null)
                {
                    DefaultGUI.LabelRightAligned(rect, content, args.selected, args.focused);
                }
                else
                {
                    //var c = GUI.color;
                    //var f = .65F;
                    //GUI.color = new Color(f, f, f, 1F);
                    GUI.enabled = false;
                    DefaultGUI.LabelRightAligned(rect, "-", false, false);
                    //GUI.color = c;
                    GUI.enabled = true;
                }
            }
        }

        protected override void ContextClickedItem(int id)
        {
            GenericMenu pm = new GenericMenu();

            pm.AddItem(new GUIContent("Find matching objects in scene"), false, () => DoubleClickedItem(id));

            pm.ShowAsContext();
        }

        protected override void DoubleClickedItem(int id)
        {
            IList<TreeViewItem> rows = GetRowsFromIDs(new List<int> {id});
            HighlightRowsMatchingObjects(rows);
        }

        private static void HighlightRowsMatchingObjects(IList<TreeViewItem> rows)
        {
            List<int> instanceIds = new List<int>();
            foreach (var row in rows)
            {
                var batchRow = row as BatchTreeViewItem;
                if (batchRow != null)
                {
                    instanceIds.AddRange(batchRow.instanceIDs);
                    continue;
                }
                var canvasRow = row as CanvasTreeViewItem;
                if (canvasRow == null)
                    continue;
                Canvas canvas = EditorUtility.InstanceIDToObject(canvasRow.info.objectInstanceId) as Canvas;
                if (canvas == null || canvas.gameObject == null)
                    continue;
                instanceIds.Add(canvas.gameObject.GetInstanceID());
            }
            if (instanceIds.Count > 0)
                Selection.instanceIDs = instanceIds.ToArray();
        }

        private void SetupRows(TreeViewItem item, IList<TreeViewItem> rows)
        {
            rows.Add(item);
            if (!item.hasChildren || IsChildListForACollapsedParent(item.children))
                return;
            if (m_Comparer.Col != Column.Element || m_Comparer.isAscending)
                item.children.Sort(m_Comparer);
            foreach (var c in item.children)
            {
                SetupRows(c, rows);
            }
        }

        private string GetItemcontent(RowGUIArgs args, int column)
        {
            if (m_AllCanvasesItem != null && args.item.id == m_AllCanvasesItem.id)
            {
                switch ((Column)column)
                {
                    case Column.TotalBatchCount:
                        return m_AllCanvasesItem.totalBatchCount.ToString();
                    case Column.TotalVertexCount:
                        return m_AllCanvasesItem.totalVertexCount.ToString();
                    case Column.GameObjectCount:
                        return m_AllCanvasesItem.gameObjectCount.ToString();
                    default:
                        return null;
                }
            }

            var batchItem = args.item as BatchTreeViewItem;
            if (batchItem != null)
            {
                var info = batchItem.info;
                switch ((Column)column)
                {
                    case Column.VertexCount:
                        return info.vertexCount.ToString();
                    case Column.TotalVertexCount:
                        return info.totalVertexCount.ToString();
                    case Column.BatchBreakingReason:
                        if (info.batchBreakingReason != BatchBreakingReason.NoBreaking)
                            return FormatBatchBreakingReason(info);
                        break;
                    case Column.GameObjectCount:
                        return info.instanceIDsCount.ToString();
                    case Column.InstanceIds:
                        if (batchItem.instanceIDs.Length <= 5)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < batchItem.instanceIDs.Length; i++)
                            {
                                if (i != 0)
                                    sb.Append(", ");
                                int iid = batchItem.instanceIDs[i];
                                var o = EditorUtility.InstanceIDToObject(iid);
                                if (o == null)
                                    sb.Append(iid);
                                else
                                    sb.Append(o.name);
                            }
                            return sb.ToString();
                        }
                        return string.Format("{0} objects", batchItem.instanceIDs.Length);
                    case Column.Element:
                    case Column.BatchCount:
                    case Column.TotalBatchCount:
                        break;

                    case Column.Rerender:
                        return info.renderDataIndex.ToString();

                    default:
                        return "Missing";
                }
                return null;
            }

            var canvasItem = args.item as CanvasTreeViewItem;
            if (canvasItem != null)
            {
                UISystemProfilerInfo info = canvasItem.info;
                switch ((Column)column)
                {
                    case Column.BatchCount:
                        return info.batchCount.ToString();
                    case Column.TotalBatchCount:
                        return info.totalBatchCount.ToString();
                    case Column.TotalVertexCount:
                        return info.totalVertexCount.ToString();
                    case Column.GameObjectCount:
                        return info.instanceIDsCount.ToString();
                    case Column.VertexCount:
                    case Column.BatchBreakingReason:
                    case Column.InstanceIds:
                    case Column.Element:
                        break;

                    case Column.Rerender:
                        return info.renderDataIndex + " : " + info.renderDataCount;

                    default:
                        return "Missing";
                }
                return null;
            }
            return null;
        }

        internal IList<TreeViewItem> GetRowsFromIDs(IList<int> selection)
        {
            return FindRows(selection);
        }

        private static string FormatBatchBreakingReason(UISystemProfilerInfo info)
        {
            switch (info.batchBreakingReason)
            {
                case BatchBreakingReason.NoBreaking:
                    return "NoBreaking";
                case BatchBreakingReason.NotCoplanarWithCanvas:
                    return "Not Coplanar With Canvas";
                case BatchBreakingReason.CanvasInjectionIndex:
                    return "Canvas Injection Index";
                case BatchBreakingReason.DifferentMaterialInstance:
                    return "Different Material Instance";
                case BatchBreakingReason.DifferentRectClipping:
                    return "Different Rect Clipping";
                case BatchBreakingReason.DifferentTexture:
                    return "Different Texture";
                case BatchBreakingReason.DifferentA8TextureUsage:
                    return "Different A8 Texture Usage";
                case BatchBreakingReason.DifferentClipRect:
                    return "Different Clip Rect";
                case BatchBreakingReason.Unknown:
                    return "Unknown";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal class State : TreeViewState
        {
            public int lastFrame;
            public ProfilerWindow profilerWindow;
        }

        internal class CanvasBatchComparer : IComparer<TreeViewItem>
        {
            internal Column Col;
            internal bool isAscending;

            public int Compare(TreeViewItem x, TreeViewItem y)
            {
                var i = isAscending ? 1 : -1;
                BaseTreeViewItem cx = (BaseTreeViewItem)x;
                BaseTreeViewItem cy = (BaseTreeViewItem)y;
                if (cx.info.isBatch != cy.info.isBatch)
                {
                    return cx.info.isBatch ? 1 : -1;
                }
                switch (Col)
                {
                    case Column.Element:
                        if (cx.info.isBatch)
                            return -1;
                        return cx.displayName.CompareTo(cy.displayName) * i;
                    case Column.BatchCount:
                        if (cx.info.isBatch)
                            return -1;
                        return cx.info.batchCount.CompareTo(cy.info.batchCount) * i;
                    case Column.TotalBatchCount:
                        if (cx.info.isBatch)
                            return -1;
                        return cx.info.totalBatchCount.CompareTo(cy.info.totalBatchCount) * i;
                    case Column.VertexCount:
                        if (cx.info.isBatch)
                            return cx.info.vertexCount.CompareTo(cy.info.vertexCount) * i;
                        // no * i, keep the canvas names ascending
                        return String.CompareOrdinal(cx.displayName, cy.displayName);
                    case Column.TotalVertexCount:
                        return cx.info.totalVertexCount.CompareTo(cy.info.totalVertexCount) * i;
                    case Column.GameObjectCount:
                        return cx.info.instanceIDsCount.CompareTo(cy.info.instanceIDsCount) * i;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal class RootTreeViewItem : TreeViewItem
        {
            public int gameObjectCount;
            public int totalBatchCount;
            public int totalVertexCount;

            public RootTreeViewItem() : base(1, 0, null, "All Canvases")
            {
            }
        }

        internal class BaseTreeViewItem : TreeViewItem
        {
            protected static readonly Texture2D s_CanvasIcon = EditorGUIUtility.LoadIcon("RectTool On");
            public UISystemProfilerInfo info;
            public int renderDataIndex;

            internal BaseTreeViewItem(UISystemProfilerInfo info, int depth, string displayName)
                : base(info.objectInstanceId, depth, displayName)
            {
                this.info = info;
            }
        }

        internal sealed class CanvasTreeViewItem : BaseTreeViewItem
        {
            public CanvasTreeViewItem(UISystemProfilerInfo info, int depth, string displayName) : base(info, depth, displayName)
            {
                icon = s_CanvasIcon;
            }
        }

        internal sealed class BatchTreeViewItem : BaseTreeViewItem
        {
            public int[] instanceIDs;

            public BatchTreeViewItem(UISystemProfilerInfo info, int depth, string displayName, int[] allBatchesInstanceIDs)
                : base(info, depth, displayName)
            {
                icon = null;
                instanceIDs = new int[info.instanceIDsCount];
                Array.Copy(allBatchesInstanceIDs, info.instanceIDsIndex, instanceIDs, 0, info.instanceIDsCount);
                renderDataIndex = info.renderDataIndex;
            }
        }

        internal enum Column
        {
            Element,
            BatchCount,
            TotalBatchCount,
            VertexCount,
            TotalVertexCount,
            BatchBreakingReason,

            GameObjectCount,
            InstanceIds,

            //Debug
            Rerender,
        }
    }
}
