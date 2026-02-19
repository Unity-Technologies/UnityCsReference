// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    abstract class Overlay : VisualElement
    {
        public bool isShown => style.display == DisplayStyle.Flex;

        protected Overlay(PickingMode pickingMode = PickingMode.Ignore)
        {
            ApplyOverlayStyle(this, pickingMode);
        }

        static void ApplyOverlayStyle(VisualElement element, PickingMode mode)
        {
            element.style.position = Position.Absolute;
            element.pickingMode = mode;
            element.style.display = DisplayStyle.Flex;
        }

        public void Show()
        {
            style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
        }
    }
}
