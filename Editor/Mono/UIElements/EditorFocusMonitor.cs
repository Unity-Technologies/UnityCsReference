// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class EditorFocusMonitor
    {
        /// <summary>
        /// Checks if any Editor windows have a bindable element currently focused.
        /// </summary>
        /// <returns>True if a bindable element is focused; false otherwise.</returns>
        public static bool AreBindableElementsSelected()
        {
            foreach (var window in EditorWindow.activeEditorWindows)
            {
                var focusController = window.rootVisualElement?.panel?.focusController;

                // We only care about elements that are bindable, as they are the only ones that could be making changes to the prefab.
                if (focusController?.focusedElement is BindableElement)
                    return true;
            }
            return false;
        }
    }
}
