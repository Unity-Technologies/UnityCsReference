// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    interface ISelectable
    {
        bool IsSelectable();
        bool Overlaps(Rect rectangle);
        void Select(GraphView selectionContainer, bool additive);
        void Unselect(GraphView selectionContainer);
        bool IsSelected(GraphView selectionContainer);
    }
}
