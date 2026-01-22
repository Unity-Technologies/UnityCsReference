// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class VisualElementExtensions
    {
        public static void OnLeftClick(this VisualElement element, Action action)
        {
            element.RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == 0)
                    action?.Invoke();
            });
        }

        /// <summary>
        /// Utility method when toggling between two classes based on boolean
        /// </summary>
        /// <param name="element">Extension element</param>
        /// <param name="classnameA">Class to set if enabled</param>
        /// <param name="classnameB">Class to set if disabled</param>
        /// <param name="enable">State to set classes from</param>
        public static void EnableClassToggle(this VisualElement element, string classnameA, string classnameB, bool enable)
        {
            element.RemoveFromClassList(classnameA);
            element.RemoveFromClassList(classnameB);

            if (enable)
                element.AddToClassList(classnameA);
            else
                element.AddToClassList(classnameB);
        }
    }
}
