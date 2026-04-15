// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Singleton handler for UIToolkit VisualElement animation authoring in the Animation Window.
    /// Manages VisualElementSelection objects for proper selection behavior when animating UI elements.
    /// </summary>
    internal class VisualElementAnimationAuthoringHandler
    {
        private static VisualElementAnimationAuthoringHandler s_Instance;

        internal static VisualElementAnimationAuthoringHandler Instance => s_Instance;

        internal static void Register()
        {
            s_Instance = new VisualElementAnimationAuthoringHandler();
            UIAnimationBinder.s_GetSelectionEntityIdCallback = GetSelectionEntityIdCallback;
        }

        private static EntityId GetSelectionEntityIdCallback(VisualElement element)
        {
            return s_Instance?.GetSelectionEntityIdForElement(element) ?? EntityId.None;
        }

        internal EntityId GetSelectionEntityIdForElement(VisualElement element)
        {
            if (element == null)
                return EntityId.None;

            var selection = VisualElementUtility.GetSelectionObject(element);

            if(selection == null)
            {
                selection = ScriptableObject.CreateInstance<VisualElementSelection>();
                selection.Element = element;
                selection.hideFlags = HideFlags.HideAndDontSave;
                VisualElementUtility.SetSelectionObject(element, selection);
            }

            return selection != null ? selection.GetEntityId() : EntityId.None;
        }
    }
}
