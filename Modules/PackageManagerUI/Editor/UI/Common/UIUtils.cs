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
        public static readonly string k_SelectedClassName = "selected";
        private static readonly string[] s_SizeUnits = { "KB", "MB", "GB", "TB" };

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;

            element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public static void SetElementDisplayNonEmpty(TextElement element)
        {
            if (element == null)
                return;

            var nonEmpty = !String.IsNullOrEmpty(element.text);
            element.style.display = nonEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.visibility = nonEmpty ? Visibility.Visible : Visibility.Hidden;
        }

        public static bool IsElementVisible(VisualElement element)
        {
            return element.resolvedStyle.visibility == Visibility.Visible && element.resolvedStyle.display != DisplayStyle.None;
        }

        public static void ScrollIfNeeded(ScrollView container, VisualElement target)
        {
            if (target == null || container == null)
                return;

            var containerWorldBound = container.worldBound;
            var targetWorldBound = target.worldBound;

            var minY = containerWorldBound.yMin;
            var maxY = containerWorldBound.yMax;
            var itemMinY = targetWorldBound.yMin;
            var itemMaxY = targetWorldBound.yMax;

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

        public static string convertToHumanReadableSize(ulong sizeInBytes)
        {
            var len = sizeInBytes / 1024.0;
            var order = 0;
            while (len >= 1024 && order < s_SizeUnits.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {s_SizeUnits[order]}";
        }
    }
}
