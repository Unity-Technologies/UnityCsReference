// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    internal class GroupDropArea : VisualElement, IDropTarget
    {
        public bool CanAcceptDrop(List<ISelectable> selection)
        {
            if (selection.Count == 0)
                return false;

            return !selection.Exists(ge =>
            {
                var element = ge as GraphElement;
                return !(element is Edge) && (element == null || element is Group || !element.IsGroupable());
            });
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
            return false;
        }

        public bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            Group group = parent.GetFirstAncestorOfType<Group>();

            List<GraphElement> elemsToAdd =
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                selection
#pragma warning restore UA2001
                    .Cast<GraphElement>()
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    .Where(e => e != group && !group.containedElements.Contains(e) && !(e.GetContainingScope() is Group) && e.IsGroupable())
#pragma warning restore UA2001
                    .ToList(); // ToList required here as the enumeration might be done again *after* the elements are added to the group

            if (elemsToAdd.Count > 0)
            {
                group.AddElements(elemsToAdd);
            }

            RemoveFromClassList("dragEntered");
            return true;
        }

        public bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            Group group = parent.GetFirstAncestorOfType<Group>();
            bool canDrop = false;

            foreach (ISelectable selectedElement in selection)
            {
                if (selectedElement == group || selectedElement is Edge)
                    continue;

                var selectedGraphElement = selectedElement as GraphElement;
                bool dropCondition = selectedGraphElement != null
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    && !group.containedElements.Contains(selectedGraphElement)
#pragma warning restore UA2001
                    && !(selectedGraphElement.GetContainingScope() is Group)
                    && selectedGraphElement.IsGroupable();

                if (dropCondition)
                {
                    canDrop = true;
                }
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                else if (evt.modifiers == EventModifiers.Shift && group.containedElements.Contains(selectedElement))
#pragma warning restore UA2001
                {
                    group.RemoveElement(selectedGraphElement);
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
