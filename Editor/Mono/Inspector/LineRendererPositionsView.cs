// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    class LineRendererPositionsView : TreeView
    {
        SerializedProperty m_Positions;

        const string k_DragId = "LineRendererPositionsViewDragging";
        const float k_DragRectWidth = 5;
        readonly static string[] k_PropertyNames = { "x", "y", "z" };

        public Action<List<int>> selectionChangedCallback { get; set; } // ids

        public LineRenderer lineRenderer { get; set; }

        public LineRendererPositionsView(SerializedProperty positions) :
            base(new TreeViewState())
        {
            m_Positions = positions;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            useScrollView = false;

            MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[4];
            for (int i = 0; i < columns.Length; ++i)
            {
                columns[i] = new MultiColumnHeaderState.Column();
                columns[i].minWidth = 50;
                columns[i].width = 100;
                columns[i].headerTextAlignment = TextAlignment.Center;
                columns[i].canSort = false;
            }
            columns[0].headerContent = new GUIContent("Index");
            columns[0].width = 50;
            columns[1].headerContent = new GUIContent("X");
            columns[2].headerContent = new GUIContent("Y");
            columns[3].headerContent = new GUIContent("Z");
            var multiColState = new MultiColumnHeaderState(columns);
            multiColumnHeader = new MultiColumnHeader(multiColState) { height = EditorGUI.kSingleLineHeight + EditorGUI.kSpacing };
            multiColumnHeader.ResizeToFit();
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var prop = m_Positions.GetArrayElementAtIndex(args.item.id);
            EditorGUI.BeginProperty(args.rowRect, GUIContent.none, prop);
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), prop);
            }
            EditorGUI.EndProperty();
        }

        protected void CellGUI(Rect cellRect, TreeViewItem item, int col, SerializedProperty property)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            if (col == 0)
            {
                EditorGUI.LabelField(cellRect, item.displayName);
            }
            else
            {
                var prop = property.FindPropertyRelative(k_PropertyNames[col - 1]);
                EditorGUI.BeginProperty(cellRect, GUIContent.none, prop);
                EditorGUI.BeginChangeCheck();
                var dragRect = cellRect;
                dragRect.width = k_DragRectWidth;
                cellRect.xMin += k_DragRectWidth;
                int id = GUIUtility.GetControlID(FocusType.Keyboard);
                var newVal = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, cellRect, dragRect, id, prop.floatValue, "g7", EditorStyles.numberField, true);
                if (EditorGUI.EndChangeCheck())
                    prop.floatValue = newVal;
                EditorGUI.EndProperty();
            }
        }

        int GetArraySize()
        {
            int arraySize = m_Positions.arraySize;
            if (m_Positions.serializedObject.isEditingMultipleObjects)
            {
                foreach (LineRenderer lineRend in m_Positions.serializedObject.targetObjects)
                {
                    arraySize = Mathf.Min(arraySize, lineRend.positionCount);
                    m_Positions.serializedObject.maxArraySizeForMultiEditing = Mathf.Max(m_Positions.serializedObject.maxArraySizeForMultiEditing, lineRend.positionCount);
                }
            }
            return arraySize;
        }

        protected override TreeViewItem BuildRoot()
        {
            int arraySize = GetArraySize();

            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>(arraySize);
            for (int i = 0; i < arraySize; ++i)
            {
                var item = new TreeViewItem(i, 0, i.ToString());
                allItems.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);

            // Focus on point
            var prop = m_Positions.GetArrayElementAtIndex(id);

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                var pos = prop.vector3Value;
                if (!lineRenderer.useWorldSpace)
                    pos = lineRenderer.transform.localToWorldMatrix.MultiplyPoint(pos);

                sceneView.Frame(new Bounds(pos, Vector3.one), false);
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectionChangedCallback != null)
                selectionChangedCallback(selectedIds.ToList());
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            var draggedRows = args.draggedItemIDs;
            DragAndDrop.SetGenericData(k_DragId, draggedRows);
            DragAndDrop.objectReferences = new UnityEngine.Object[] {};  // this is required for dragging to work
            DragAndDrop.StartDrag("Move Positions");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // Check if we can handle the current drag data (could be dragged in from other areas/windows in the editor)
            var draggedRows = DragAndDrop.GetGenericData(k_DragId) as List<int>;
            if (draggedRows == null || args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                if (m_Positions.hasMultipleDifferentValues)
                {
                    if (!EditorUtility.DisplayDialog(L10n.Tr("Moving an array element will copy the complete array to all other selected objects."),
                        L10n.Tr("Unique values in the different selected objects will be lost"),
                        L10n.Tr("OK"),
                        L10n.Tr("Cancel")))
                    {
                        return DragAndDropVisualMode.Rejected;
                    }
                }

                int arraySize = GetArraySize();
                var newList = new List<Vector3>(arraySize);
                draggedRows.Sort();

                int nextDragItem = 0;
                for (int i = 0; i < arraySize; ++i)
                {
                    if (i == args.insertAtIndex)
                    {
                        // Insert the items here
                        foreach (var viewItem in draggedRows)
                        {
                            newList.Add(m_Positions.GetArrayElementAtIndex(viewItem).vector3Value);
                        }
                    }

                    if (i == draggedRows[nextDragItem])
                    {
                        // Ignore this item, it is being moved
                        nextDragItem++;
                        if (nextDragItem >= draggedRows.Count)
                            nextDragItem = 0;
                    }
                    else
                    {
                        newList.Add(m_Positions.GetArrayElementAtIndex(i).vector3Value);
                    }
                }

                // Add to the end?
                if (args.insertAtIndex == arraySize)
                {
                    foreach (var viewItem in draggedRows)
                    {
                        newList.Add(m_Positions.GetArrayElementAtIndex(viewItem).vector3Value);
                    }
                }

                // Copy the list back
                for (int i = 0; i < arraySize; ++i)
                {
                    m_Positions.GetArrayElementAtIndex(i).vector3Value = newList[i];
                }

                SetSelection(Enumerable.Range(args.insertAtIndex - draggedRows.Count(o => o < args.insertAtIndex), draggedRows.Count).ToList());
            }
            return DragAndDropVisualMode.Move;
        }
    }
}
