// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal class TreeViewReorderableDragAndDropController : BaseReorderableDragAndDropController
    {
        protected readonly BaseTreeView m_TreeView;

        public TreeViewReorderableDragAndDropController(BaseTreeView view)
            : base(view)
        {
            m_TreeView = view;
            enableReordering = true;
        }

        public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args)
        {
            if (!enableReordering)
                return DragVisualMode.Rejected;

            return args.dragAndDropData.userData == m_TreeView ? DragVisualMode.Move : DragVisualMode.Rejected;
        }

        public override void OnDrop(IListDragAndDropArgs args)
        {
            var insertAtId = m_TreeView.GetIdForIndex(args.insertAtIndex);
            var insertAtParentId = m_TreeView.GetParentIdForIndex(args.insertAtIndex);
            var insertAtChildIndex = m_TreeView.viewController.GetChildIndexForId(insertAtId);

            if (args.dragAndDropPosition == DragAndDropPosition.OverItem || (insertAtId == -1 && insertAtParentId == -1 && insertAtChildIndex == -1))
            {
                for (var i = 0; i < m_SelectedIndices.Count; i++)
                {
                    var index = m_SelectedIndices[i];
                    var id = m_TreeView.GetIdForIndex(index);

                    var parentId = insertAtId;
                    var childIndex = -1;
                    m_TreeView.viewController.Move(id, parentId, childIndex, false);
                }
            }
            else
            {
                for (var i = m_SelectedIndices.Count - 1; i >= 0; --i)
                {
                    var index = m_SelectedIndices[i];
                    var id = m_TreeView.GetIdForIndex(index);

                    var parentId = insertAtParentId;
                    var childIndex = insertAtChildIndex;
                    m_TreeView.viewController.Move(id, parentId, childIndex, false);
                }
            }

            m_TreeView.viewController.RebuildTree();
            m_TreeView.RefreshItems();
        }
    }
}
