// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    interface IDroppable
    {
        bool IsDroppable();
    }

    internal
    interface IDropTarget
    {
        bool CanAcceptDrop(List<ISelectable> selection);

        // evt.mousePosition will be in global coordinates.
        EventPropagation DragUpdated(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget);
        EventPropagation DragPerform(IMGUIEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget);
        EventPropagation DragExited();
    }
}
