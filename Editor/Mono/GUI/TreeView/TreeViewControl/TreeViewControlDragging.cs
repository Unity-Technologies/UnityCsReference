// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.IMGUI.Controls
{
    public partial class TreeView<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        private class TreeViewControlDragging : TreeViewDragging<TIdentifier>
        {
            private TreeView<TIdentifier> m_Owner;

            public TreeViewControlDragging(TreeViewController<TIdentifier> treeView, TreeView<TIdentifier> owner)
                : base(treeView)
            {
                m_Owner = owner;
            }

            public override bool CanStartDrag(TreeViewItem<TIdentifier> targetItem, List<TIdentifier> draggedItemIDs, Vector2 mouseDownPosition)
            {
                return m_Owner.CanStartDrag(new CanStartDragArgs { draggedItem = targetItem, draggedItemIDs = draggedItemIDs });
            }

            public override void StartDrag(TreeViewItem<TIdentifier> draggedItem, List<TIdentifier> draggedItemIDs)
            {
                m_Owner.SetupDragAndDrop(new SetupDragAndDropArgs { draggedItemIDs = draggedItemIDs});
            }

            public override DragAndDropVisualMode DoDrag(TreeViewItem<TIdentifier> parentItem, TreeViewItem<TIdentifier> targetItem, bool perform, DropPosition dropPosition)
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

            DragAndDropPosition GetDragAndDropPosition(TreeViewItem<TIdentifier> parentItem, TreeViewItem<TIdentifier> targetItem)
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
