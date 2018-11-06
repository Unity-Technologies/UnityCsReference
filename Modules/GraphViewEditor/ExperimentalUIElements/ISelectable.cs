// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public interface ISelectable
    {
        bool IsSelectable();
        bool HitTest(Vector2 localPoint);
        bool Overlaps(Rect rectangle);
        void Select(VisualElement selectionContainer, bool additive);
        void Unselect(VisualElement selectionContainer);
        bool IsSelected(VisualElement selectionContainer);
    }
}
