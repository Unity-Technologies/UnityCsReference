// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    [UxmlElement]
    partial class TilemapHierarchyView : VisualElement
    {
        const string k_UXML = "U2DEditor/TilemapProfiler/TilemapHierarchyView/TilemapHierarchyView.uxml";
        MultiColumnTreeView m_Table;
        List<TreeViewItemData<TilemapHierarchyBaseNode>> m_Data = new();
        Label m_NoDataLabel;

        public TilemapHierarchyView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);
            m_Table = this.Q<MultiColumnTreeView>();
            m_Table.selectionChanged += OnSelectionChanged;
            m_NoDataLabel = this.Q<Label>("noDataLabel");
            SetupTable();
            ShowTable();
        }

        void OnSelectionChanged(IEnumerable<object> obj)
        {
            var cellData = m_Table.GetItemDataForIndex<TilemapHierarchyBaseNode>(m_Table.selectedIndex);
            if (cellData != null)
            {
                var unityObject = EditorUtility.EntityIdToObject(cellData.entityId);
                if (unityObject != null)
                    Selection.activeObject = unityObject;
            }
        }

        void SetupTable()
        {
            if (EditorGUIUtility.isProSkin)
                m_Table.AddToClassList("dark");
            else
                m_Table.AddToClassList("light");

            m_Table.sortingMode = ColumnSortingMode.Custom;
            m_Table.columnSortingChanged += OnColumnSortingChanged;
            for (int i = 0; i < m_Table.columns.Count; ++i)
            {
                Column column = m_Table.columns[i];

                if (column.name == "Name")
                {
                    column.bindCell = (element, i) =>
                    {
                        Label label = element.Q<Label>();
                        TilemapHierarchyBaseNode itemData = m_Table.GetItemDataForIndex<TilemapHierarchyBaseNode>(i);
                        BindLabelToDataSource(label, column.bindingPath, itemData);
                        SetNameColumnCellIcon(element, itemData);
                    };
                }
                else
                {
                    column.bindCell = (element, i) =>
                    {
                        TilemapHierarchyBaseNode itemData = m_Table.GetItemDataForIndex<TilemapHierarchyBaseNode>(i);
                        Label label = element.Q<Label>();
                        BindLabelToDataSource(label, column.bindingPath, itemData);
                    };
                }

                column.unbindCell = (element, _) =>
                {
                    Label label = element.Q<Label>();
                    label.SetBinding("text", null);
                };

                column.makeCell = () =>
                {
                    VisualElement ve = new VisualElement();
                    ve.AddToClassList("cell");
                    VisualElement icon = new VisualElement() { name = "Icon" };
                    icon.AddToClassList("cell-icon");
                    Label label = new Label();
                    label.AddToClassList("cell-label");
                    ve.Add(icon);
                    ve.Add(label);
                    return ve;
                };
                column.comparison = (a, b) =>
                {
                    TilemapHierarchyNodeData aData = m_Table.GetItemDataForIndex<TilemapHierarchyNodeData>(a);
                    TilemapHierarchyNodeData bData = m_Table.GetItemDataForIndex<TilemapHierarchyNodeData>(b);
                    return TilemapHierarchyNodeData.Compare(aData, bData, column.bindingPath);
                };
            }
        }

        void OnColumnSortingChanged()
        {
            if (m_Table.sortedColumns != null)
            {
                List<TreeViewItemData<TilemapHierarchyBaseNode>> sortedData = new();
                foreach (TreeViewItemData<TilemapHierarchyBaseNode> child in m_Data)
                {
                    List<TreeViewItemData<TilemapHierarchyBaseNode>> children = new();
                    foreach (TreeViewItemData<TilemapHierarchyBaseNode> c in child.children)
                    {
                        children.Add(c);
                    }

                    children.Sort(SortData);
                    sortedData.Add(new TreeViewItemData<TilemapHierarchyBaseNode>(child.id, child.data, children));
                }

                sortedData.Sort(SortData);
                m_Data.Clear();
                m_Data = sortedData;

                m_Table.Clear();
                HashSet<int> expandedIds = new();
                foreach (TreeViewItemData<TilemapHierarchyBaseNode> d in m_Data)
                {
                    if (m_Table.IsExpanded(d.id))
                        expandedIds.Add(d.id);
                }

                m_Table.SetRootItems(m_Data);
                m_Table.Rebuild();
                foreach (int id in expandedIds)
                {
                    m_Table.ExpandItem(id);
                }

                ShowTable();
            }
        }

        int SortData(TreeViewItemData<TilemapHierarchyBaseNode> a, TreeViewItemData<TilemapHierarchyBaseNode> b)
        {
            using (IEnumerator<SortColumnDescription> enumerator = m_Table.sortedColumns.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    int result = TilemapHierarchyBaseNode.Compare(a.data, b.data, enumerator.Current.column.bindingPath);
                    if (result != 0)
                        return result * (enumerator.Current.direction == SortDirection.Ascending ? 1 : -1);
                }
            }

            return TilemapHierarchyNodeData.Compare(a.data, b.data, null);
        }

        void BindLabelToDataSource(Label label, string path, TilemapHierarchyBaseNode cellData)
        {
            label.SetBinding("text", new DataBinding { dataSourcePath = new PropertyPath(path), bindingMode = BindingMode.ToTarget, dataSource = cellData });
        }

        void SetNameColumnCellIcon(VisualElement ele, TilemapHierarchyBaseNode data)
        {
            VisualElement icon = ele.Q("Icon");
            icon.RemoveFromClassList("gameObject-icon");
            icon.RemoveFromClassList("tilemap-icon");
            if (data.icon?.Length > 0)
                icon.AddToClassList(data.icon);
        }

        public void SetData(IEnumerable<TilemapHierarchyNodeData> values)
        {
            m_Table.Clear();
            HashSet<int> expandedIds = new();
            foreach (TreeViewItemData<TilemapHierarchyBaseNode> d in m_Data)
            {
                if (m_Table.IsExpanded(d.id))
                    expandedIds.Add(d.id);
            }

            m_Data.Clear();
            foreach (TilemapHierarchyNodeData node in values)
            {
                List<TreeViewItemData<TilemapHierarchyBaseNode>> children = null;
                if (node.chunkRecord != null)
                {
                    children = new();
                    foreach (TilemapChunkRecord child in node.chunkRecord)
                    {
                        child.icon = "tilemap-icon";
                        children.Add(new TreeViewItemData<TilemapHierarchyBaseNode>(child.id, child));
                    }

                    if (m_Table.sortedColumns != null)
                    {
                        children.Sort(SortData);
                    }
                }

                node.icon = "gameObject-icon";
                m_Data.Add(new TreeViewItemData<TilemapHierarchyBaseNode>(node.id, node, children));
            }

            if (m_Table.sortedColumns != null)
            {
                m_Data.Sort(SortData);
            }

            m_Table.SetRootItems(m_Data);
            m_Table.Rebuild();
            foreach (int id in expandedIds)
            {
                m_Table.ExpandItem(id);
            }

            ShowTable();
        }

        void ShowTable()
        {
            if (m_Data.Count == 0)
            {
                m_Table.style.display = DisplayStyle.None;
                m_NoDataLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_Table.style.display = DisplayStyle.Flex;
                m_NoDataLabel.style.display = DisplayStyle.None;
            }
        }
    }
}
