// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.ComponentModel;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditor.TreeViewExamples
{
    class TestGUICustomItemHeights : TreeViewGUIWithCustomItemsHeights
    {
        internal class Styles
        {
            public static GUIStyle foldout = "IN Foldout";
        }

        private float m_Column1Width = 300;
        protected Rect m_DraggingInsertionMarkerRect;

        public TestGUICustomItemHeights(TreeViewController treeView)
            : base(treeView)
        {
            m_FoldoutWidth = Styles.foldout.fixedWidth;
        }

        protected override Vector2 GetSizeOfRow(TreeViewItem item)
        {
            return new Vector2(m_TreeView.GetTotalRect().width, item.hasChildren ? 20 : 36f);
        }

        public override void BeginRowGUI()
        {
            // Reset
            m_DraggingInsertionMarkerRect.x = -1;
        }

        public override void EndRowGUI()
        {
            base.EndRowGUI();

            // Draw row marker when dragging
            if (m_DraggingInsertionMarkerRect.x >= 0 && Event.current.type == EventType.Repaint)
            {
                Rect insertionRect = m_DraggingInsertionMarkerRect;
                insertionRect.height = 2f;
                insertionRect.y -= insertionRect.height / 2;
                if (!m_TreeView.dragging.drawRowMarkerAbove)
                    insertionRect.y += m_DraggingInsertionMarkerRect.height;

                EditorGUI.DrawRect(insertionRect, Color.white);
            }
        }

        public override void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused)
        {
            rowRect.height -= 1f;
            Rect column1Rect = rowRect;
            Rect column2Rect = rowRect;
            column1Rect.width = m_Column1Width;
            column1Rect.xMin += GetFoldoutIndent(item);
            column2Rect.xMin += m_Column1Width + 1;

            float indent = GetFoldoutIndent(item);
            Rect tmpRect = rowRect;

            int itemControlID = TreeViewController.GetItemControlID(item);

            bool isDropTarget = false;
            if (m_TreeView.dragging != null)
                isDropTarget = m_TreeView.dragging.GetDropTargetControlID() == itemControlID && m_TreeView.data.CanBeParent(item);
            bool showFoldout = m_TreeView.data.IsExpandable(item);


            Color selectedColor = new Color(0.0f, 0.22f, 0.44f);
            Color normalColor = new Color(0.1f, 0.1f, 0.1f);

            EditorGUI.DrawRect(column1Rect, selected ? selectedColor : normalColor);
            EditorGUI.DrawRect(column2Rect, selected ? selectedColor : normalColor);

            if (isDropTarget)
            {
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3, rowRect.height), Color.yellow);
            }

            if (Event.current.type == EventType.Repaint)
            {
                Rect labelRect = column1Rect;
                labelRect.xMin += m_FoldoutWidth;

                GUI.Label(labelRect, item.displayName, EditorStyles.largeLabel);
                if (rowRect.height > 20f)
                {
                    labelRect.y += 16f;
                    GUI.Label(labelRect, "Ut tincidunt tortor. Donec nonummy, enim in lacinia pulvinar", EditorStyles.miniLabel);
                }

                // Show marker below this Item
                if (m_TreeView.dragging != null && m_TreeView.dragging.GetRowMarkerControlID() == itemControlID)
                    m_DraggingInsertionMarkerRect = new Rect(rowRect.x + indent , rowRect.y, rowRect.width - indent, rowRect.height);
            }

            // Draw foldout (after text content above to ensure drop down icon is rendered above selection highlight)
            if (showFoldout)
            {
                tmpRect.x = indent;
                tmpRect.width = m_FoldoutWidth;
                EditorGUI.BeginChangeCheck();
                bool newExpandedValue = GUI.Toggle(tmpRect, m_TreeView.data.IsExpanded(item), GUIContent.none, Styles.foldout);
                if (EditorGUI.EndChangeCheck())
                {
                    m_TreeView.UserInputChangedExpandedState(item, row, newExpandedValue);
                }
            }
        }

        /*
        void ChangeExpandedState(TreeViewItem item, bool expand)
        {
            if (Event.current.alt)
                m_TreeView.data.SetExpandedWithChildren(item, expand);
            else
                m_TreeView.data.SetExpanded(item, expand);

            if (expand)
                m_TreeView.UserExpandedItem(item);
        }*/
    }
}
