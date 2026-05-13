// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class UIUtils
    {
        private static readonly IReadOnlyList<string> k_SizeUnits = Array.AsReadOnly(new []{ "KB", "MB", "GB", "TB" });

        private static readonly Dictionary<ScrollView, VisualElement> s_PendingScrollTargets = new();

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;

            element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public static bool IsElementVisible(VisualElement element)
        {
            return element?.resolvedStyle.visibility == Visibility.Visible && element.resolvedStyle.display != DisplayStyle.None;
        }

        public static void AppendAction(this GenericDropdownMenu menu, string itemName, bool enabled, Action<object> action, string tooltip = null, bool isChecked = false)
        {
            if (enabled)
                menu.AddItem(itemName, isChecked, action, null);
            else
                menu.AddDisabledItem(itemName, isChecked);
            if (string.IsNullOrEmpty(tooltip))
                return;
            var lastItem = menu.items.LastMatch(x => x.name == itemName);
            if (lastItem != null)
                lastItem.element.tooltip = tooltip;
        }

        public static void ScrollToWhenReady(VisualElement target)
        {
            if (target is null)
                return;

            var scrollViews = new List<ScrollView>();
            foreach (var scrollView in GetParentsOfType<ScrollView>(target))
            {
                scrollViews.Add(scrollView);
                s_PendingScrollTargets[scrollView] = target;
            }

            // GeometryChangedEvent may or may not fire depending on whether the layout changes, and the scheduler
            // takes up to 3 frames — using both gives us the fastest and most reliable scroll in all cases.
            var scrollToCalled = false;
            var numDelays = 0;
            target.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            {
                if (scrollToCalled)
                    return;
                scrollToCalled = true;
                ScrollToIfPending(target, scrollViews);
            });

            target.schedule.Execute(() =>
            {
                numDelays++;
                if (scrollToCalled || numDelays <= 2)
                    return;
                scrollToCalled = true;
                ScrollToIfPending(target, scrollViews);
            }).Every(1).Until(() => scrollToCalled);
        }

        // During the up-to-3-frame delay, another ScrollToWhenReady call may have changed the pending target
        // so we check each scroll view before scrolling to avoid scrolling to a stale target.
        private static void ScrollToIfPending(VisualElement target, List<ScrollView> scrollViews)
        {
            foreach (var scrollView in scrollViews)
            {
                if (!s_PendingScrollTargets.TryGetValue(scrollView, out var pending) || pending != target)
                    continue;
                scrollView.ScrollTo(target);
                s_PendingScrollTargets.Remove(scrollView);
            }
        }

        public static IEnumerable<T> GetParentsOfType<T>(VisualElement element) where T : VisualElement
        {
            var parent = element?.parent;
            while (parent != null)
            {
                if (parent is T selected)
                    yield return selected;
                parent = parent.parent;
            }
        }

        public static T GetParentOfType<T>(VisualElement element) where T : VisualElement
        {
            using var enumerator = GetParentsOfType<T>(element).GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }

        public static string ConvertToHumanReadableSize(ulong sizeInBytes)
        {
            var len = sizeInBytes / 1024.0;
            var order = 0;
            while (len >= 1024 && order < k_SizeUnits.Count - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {k_SizeUnits[order]}";
        }

        public static void ShowTextTooltipOnSizeChange<T>(this T element, int deltaWidth = 0) where T : TextElement
        {
            InternalShowTextTooltipOnSizeChange(element, deltaWidth);
        }

        private static void InternalShowTextTooltipOnSizeChange(TextElement element, int deltaWidth)
        {
            element.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (Mathf.Approximately(evt.newRect.width, evt.oldRect.width))
                    return;

                var target = evt.target as TextElement;
                if (target == null)
                    return;

                var size = target.MeasureTextSize(target.text, float.MaxValue, VisualElement.MeasureMode.AtMost, evt.newRect.height, VisualElement.MeasureMode.Undefined);
                var width = evt.newRect.width + deltaWidth;
                target.tooltip = width < size.x ? target.text : string.Empty;
            });

            element.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == evt.previousValue)
                    return;

                var target = evt.target as TextElement;
                if (target == null)
                    return;

                var size = target.MeasureTextSize(evt.newValue, float.MaxValue, VisualElement.MeasureMode.AtMost, target.contentRect.height, VisualElement.MeasureMode.Undefined);
                var width = target.contentRect.width + deltaWidth;
                target.tooltip = width < size.x ? target.text : string.Empty;
            });
        }

        private static void ActionShowTextTooltipOnSizeChange(TextElement element)
        {
            InternalShowTextTooltipOnSizeChange(element, 0);
        }

        public static readonly Action<Label> TextTooltipOnSizeChange = ActionShowTextTooltipOnSizeChange;
    }
}
