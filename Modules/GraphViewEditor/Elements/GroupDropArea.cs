// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal class GroupDropArea : VisualElement, IDropTarget
    {
        public GroupDropArea()
        {
        }

        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            if (selection.Count == 0)
                return false;

            return !selection.Cast<GraphElement>().Any(ge => ge == null || ge is Group);
        }

        public bool DragExited()
        {
            RemoveFromClassList("dragEntered");

            return false;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            Group group = parent.GetFirstAncestorOfType<Group>();

            foreach (ISelectable selectedElement in selection)
            {
                if (selectedElement != group)
                {
                    var selectedGraphElement = selectedElement as GraphElement;

                    if (group.containedElements.Contains(selectedGraphElement) || selectedGraphElement.GetContainingScope() is Group)
                        continue;

                    group.AddElement(selectedGraphElement);
                }
            }

            RemoveFromClassList("dragEntered");

            return true;
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget)
        {
            Group group = parent.GetFirstAncestorOfType<Group>();
            bool canDrop = false;

            foreach (ISelectable selectedElement in selection)
            {
                if (selectedElement == group || selectedElement is Edge)
                    continue;

                var selectedGraphElement = selectedElement as GraphElement;

                if (evt.shiftKey)
                {
                    if (group.containedElements.Contains(selectedGraphElement))
                    {
                        group.RemoveElement(selectedGraphElement);
                    }
                }
                else
                {
                    if (!group.containedElements.Contains(selectedGraphElement) && !(selectedGraphElement.GetContainingScope() is Group))
                    {
                        canDrop = true;
                    }
                }
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
