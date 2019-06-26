// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class VisualElementExtensions
    {
        public static void EnableClass(this VisualElement element, string classname, bool enable)
        {
            element.RemoveFromClassList(classname);

            if (enable)
                element.AddToClassList(classname);
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

        /// <summary>
        /// Utility method to add multiple classes at once
        /// </summary>
        /// <param name="element">Extension element</param>
        /// <param name="classnames">Space-separated list of classes to add</param>
        public static void AddClasses(this VisualElement element, string classnames)
        {
            if (!string.IsNullOrEmpty(classnames))
                foreach (var classname in classnames.Split(' '))
                    element.AddToClassList(classname);
        }
    }
}
