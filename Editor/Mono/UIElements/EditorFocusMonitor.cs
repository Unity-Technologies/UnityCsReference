// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class EditorFocusMonitor
    {
        /// <summary>
        /// Checks if any Editor windows have a focused element that is, or is
        /// inside, an <see cref="IDelayedField"/>. Delayed fields
        /// (TextInputBaseField, BaseCompositeField, etc.) have intermediate
        /// editing state where the user types characters one at a time, so
        /// prefab auto-save must be deferred until editing ends.
        /// Non-delayed fields like Toggle, EnumField, or Slider commit their
        /// values immediately and should not block auto-save.
        /// </summary>
        /// <returns>True if a delayed field is focused; false otherwise.</returns>
        public static bool IsDelayableFieldFocused()
        {
            foreach (var window in EditorWindow.activeEditorWindows)
            {
                var focusController = window.rootVisualElement?.panel?.focusController;

                if (focusController?.focusedElement is IDelayedField)
                    return true;
            }
            return false;
        }
    }
}
