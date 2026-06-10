// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class EditorFocusMonitor
    {
        // If no focusController is provided, check the keyboard-focused window.
        internal static bool HasTextElementFocus(FocusController focusController = null)
        {
            if (focusController != null)
                return IsTextEditingActive(focusController);

            var focusedWindow = EditorWindow.focusedWindow;
            return focusedWindow != null &&
                   IsTextEditingActive(focusedWindow.rootVisualElement?.panel?.focusController);
        }

        static bool IsTextEditingActive(FocusController focusController)
        {
            if (focusController == null)
                return false;

            if (focusController.GetLeafFocusedElement() is TextElement textElement)
                return textElement.hasFocus && textElement.selection.isSelectable && !textElement.edition.isReadOnly;

            return false;
        }
    }
}
