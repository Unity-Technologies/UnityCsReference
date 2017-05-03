// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;


namespace UnityEditor.TreeViewExamples
{
    internal class TestDragging : TreeViewDragging
    {
        private const string k_GenericDragID = "FooDragging";
        private BackendData m_BackendData;


        class FooDragData
        {
            public FooDragData(List<TreeViewItem> draggedItems)
            {
                m_DraggedItems = draggedItems;
            }

            public List<TreeViewItem> m_DraggedItems;
        }

        public TestDragging(TreeViewController treeView, BackendData data)
            : base(treeView)
        {
            m_BackendData = data;
        }

        public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(k_GenericDragID, new FooDragData(GetItemsFromIDs(draggedItemIDs)));
            string title = draggedItemIDs.Count + " Foo" + (draggedItemIDs.Count > 1 ? "s" : ""); // title is only shown on OSX (at the cursor)
            DragAndDrop.StartDrag(title);
        }

        public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPos)
        {
            var dragData = DragAndDrop.GetGenericData(k_GenericDragID) as FooDragData;
            var fooParent = parentItem as FooTreeViewItem;
            if (fooParent != null && dragData != null)
            {
                bool validDrag = ValidDrag(parentItem, dragData.m_DraggedItems);
                if (perform && validDrag)
                {
                    // Do reparenting here
                    List<BackendData.Foo> draggedFoos = (from x in dragData.m_DraggedItems where x is FooTreeViewItem select((FooTreeViewItem)x).foo).ToList();
                    var selectedIDs = (from x in dragData.m_DraggedItems where x is FooTreeViewItem select((FooTreeViewItem)x).id).ToArray();
                    int insertionIndex = GetInsertionIndex(parentItem, targetItem, dropPos);
                    m_BackendData.ReparentSelection(fooParent.foo, insertionIndex, draggedFoos);
                    m_TreeView.ReloadData();
                    m_TreeView.SetSelection(selectedIDs, true);
                }
                return validDrag ? DragAndDropVisualMode.Move : DragAndDropVisualMode.None;
            }
            return DragAndDropVisualMode.None;
        }

        bool ValidDrag(TreeViewItem parent, List<TreeViewItem> draggedItems)
        {
            TreeViewItem currentParent = parent;
            while (currentParent != null)
            {
                if (draggedItems.Contains(currentParent))
                    return false;
                currentParent = currentParent.parent;
            }
            return true;
        }

        private List<TreeViewItem> GetItemsFromIDs(IEnumerable<int> draggedItemIDs)
        {
            // Note we only drag visible items here...
            return TreeViewUtility.FindItemsInList(draggedItemIDs, m_TreeView.data.GetRows());
        }
    }
} // UnityEditor
