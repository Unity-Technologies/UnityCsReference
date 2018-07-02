// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal class GroupDropArea : VisualElement, IDropTarget
    {
        List<GraphElement> m_RemovedElements = null;

        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            if (selection.Count == 0)
                return false;

            return !selection.Cast<GraphElement>().Any(ge => ge == null || ge is Group);
        }

        public bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            RemoveFromClassList("dragEntered");
            return true;
        }

        public bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return true;
        }

        public bool DragExited()
        {
            RemoveFromClassList("dragEntered");
            m_RemovedElements = null;
            return false;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            Group group = parent.GetFirstAncestorOfType<Group>();

            List<GraphElement> elemsToAdd =
                selection
                .Cast<GraphElement>()
                .Where(e => e != group && !group.containedElements.Contains(e) && !(e.GetContainingScope() is Group))
                .ToList();     // ToList required here as the enumeration might be done again *after* the elements are added to the group

            if (elemsToAdd.Any())
            {
                group.AddElements(elemsToAdd);
            }

            RemoveFromClassList("dragEntered");
            m_RemovedElements = null;
            return true;
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            Group group = parent.GetFirstAncestorOfType<Group>();
            bool canDrop = false;
            bool areElementsDraggedOut = false;

            foreach (ISelectable selectedElement in selection)
            {
                if (selectedElement == group || selectedElement is Edge)
                    continue;

                var selectedGraphElement = selectedElement as GraphElement;

                if (evt.shiftKey)
                {
                    if (group.containedElements.Contains(selectedGraphElement))
                    {
                        areElementsDraggedOut = true;
                        if (m_RemovedElements == null)
                        {
                            m_RemovedElements = new List<GraphElement>();
                        }
                        m_RemovedElements.Add(selectedGraphElement);
                    }
                }

                if (!group.containedElements.Contains(selectedGraphElement) && !(selectedGraphElement.GetContainingScope() is Group))
                {
                    canDrop = true;
                }
            }

            if (areElementsDraggedOut)
            {
                group.RemoveElementsWithoutNotification(m_RemovedElements);
            }

            if (canDrop)
            {
                AddToClassList("dragEntered");
            }
            else
            {
                RemoveFromClassList("dragEntered");
            }

            return true;
        }
    }
}
