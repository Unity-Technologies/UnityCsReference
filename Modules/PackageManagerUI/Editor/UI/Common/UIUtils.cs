// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class UIUtils
    {
        private const string DisplayNone = "display-none";

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;

            if (value)
                element.RemoveFromClassList(DisplayNone);
            else
                element.AddToClassList(DisplayNone);

            element.visible = value;
        }

        public static void SetElementDisplayNonEmpty(Label element)
        {
            if (element == null)
                return;

            var empty = string.IsNullOrEmpty(element.text);
            if (empty)
                element.AddToClassList(DisplayNone);
            else
                element.RemoveFromClassList(DisplayNone);

            element.visible = !empty;
        }

        public static void SetElementDisplayNonEmpty(Button element)
        {
            if (element == null)
                return;

            var empty = string.IsNullOrEmpty(element.text);
            if (empty)
                element.AddToClassList(DisplayNone);
            else
                element.RemoveFromClassList(DisplayNone);

            element.visible = !empty;
        }

        public static bool IsElementVisible(VisualElement element)
        {
            return element.visible && !element.ClassListContains(DisplayNone);
        }

        public static void ScrollIfNeeded(ScrollView container, VisualElement target)
        {
            if (target == null || container == null)
                return;

            var minY = container.worldBound.yMin;
            var maxY = container.worldBound.yMax;
            var itemMinY = target.worldBound.yMin;
            var itemMaxY = target.worldBound.yMax;
            var scroll = container.scrollOffset;

            if (itemMinY < minY)
            {
                scroll.y -= Math.Max(0, minY - itemMinY);
                container.scrollOffset = scroll;
            }
            else if (itemMaxY > maxY)
            {
                scroll.y += itemMaxY - maxY;
                container.scrollOffset = scroll;
            }
        }

        public static IEnumerable<T> GetParentsOfType<T>(VisualElement element) where T : VisualElement
        {
            var result = new List<T>();

            var parent = element;
            while (parent != null)
            {
                var selected = parent as T;
                if (selected != null)
                    result.Add(selected);

                parent = parent.parent;
            }

            return result;
        }

        public static T GetParentOfType<T>(VisualElement element) where T : VisualElement
        {
            return GetParentsOfType<T>(element).FirstOrDefault();
        }
    }
}
