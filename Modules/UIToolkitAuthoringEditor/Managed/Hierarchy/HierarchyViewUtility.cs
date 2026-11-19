// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

static class HierarchyViewUtility
{
    /// <summary>
    /// Whether a press opens the context menu: the right button, or Ctrl + left click on macOS.
    /// </summary>
    public static bool IsContextClick(MouseButton button, EventModifiers modifiers)
    {
        if (UIElementsUtility.isOSXContextualMenuPlatform)
            return button == MouseButton.RightMouse || (button == MouseButton.LeftMouse && modifiers == EventModifiers.Control);

        return button == MouseButton.RightMouse;
    }
}
