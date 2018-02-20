// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public interface IDroppable
    {
        bool IsDroppable();
    }

    public interface IDropTarget
    {
        bool CanAcceptDrop(List<ISelectable> selection);

        // evt.mousePosition will be in global coordinates.
        bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget);
        bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget);
        bool DragExited();
    }
}
