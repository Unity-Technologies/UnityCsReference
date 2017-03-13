// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.IMGUI.Controls
{
    public partial class TreeView
    {
        private class TreeViewControlDragging : TreeViewDragging
        {
            private TreeView m_Owner;

            public TreeViewControlDragging(TreeViewController treeView, TreeView owner)
                : base(treeView)
            {
                m_Owner = owner;
            }

            public override bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition)
            {
                return m_Owner.CanStartDrag(new CanStartDragArgs { draggedItem = targetItem, draggedItemIDs = draggedItemIDs });
            }

            public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
            {
                m_Owner.SetupDragAndDrop(new SetupDragAndDropArgs { draggedItemIDs = draggedItemIDs});
            }

            public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition)
            {
                if (m_Owner.m_OverriddenMethods.hasHandleDragAndDrop)
                {
                    var args = new DragAndDropArgs
                    {
                        dragAndDropPosition = GetDragAndDropPosition(parentItem, targetItem),
                        insertAtIndex = GetInsertionIndex(parentItem, targetItem, dropPosition),
                        parentItem = parentItem,
                        performDrop = perform
                    };

                    return m_Owner.HandleDragAndDrop(args);
                }

                return DragAndDropVisualMode.None;
            }

            DragAndDropPosition GetDragAndDropPosition(TreeViewItem parentItem, TreeViewItem targetItem)
            {
                if (parentItem == null)
                    return DragAndDropPosition.OutsideItems;

                if (parentItem == targetItem)
                    return DragAndDropPosition.UponItem;

                return DragAndDropPosition.BetweenItems;
            }
        }
    }
}
