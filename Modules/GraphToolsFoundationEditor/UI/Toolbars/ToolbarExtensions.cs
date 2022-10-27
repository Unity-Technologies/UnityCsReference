// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods related to toolbar elements.
    /// </summary>
    static class ToolbarExtensions
    {
        /// <summary>
        /// Adds or updates an item in the <paramref name="breadcrumbs"/>.
        /// </summary>
        /// <param name="breadcrumbs">The breadcrumbs to modify.</param>
        /// <param name="index">The index of the item to modify or add. If index is greater or equal to the number of items in the breadcrumb, a new item we be added. Otherwise, the item label at index will be updated (and <paramref name="clickedEvent"/> will be ignored).</param>
        /// <param name="itemLabel">The label for the item.</param>
        /// <param name="clickedEvent">The action to execute when the item is clicked.</param>
        public static void CreateOrUpdateItem(this ToolbarBreadcrumbs breadcrumbs, int index, string itemLabel, Action<int> clickedEvent)
        {
            if (index >= breadcrumbs.childCount)
            {
                breadcrumbs.PushItem(itemLabel, () =>
                {
                    int i = index;
                    clickedEvent?.Invoke(i);
                });
            }
            else
            {
                if (breadcrumbs.ElementAt(index) is ToolbarButton item)
                {
                    item.text = itemLabel;
                }
                else
                {
                    Debug.LogError("Trying to update an element that is not a ToolbarButton");
                }
            }
        }

        /// <summary>
        /// Removes all items from the <paramref name="breadcrumbs"/> except the first <paramref name="countToKeep"/>.
        /// </summary>
        /// <param name="breadcrumbs">The breadcrumbs to trim.</param>
        /// <param name="countToKeep">The number of elements to keep in the breadcrumb.</param>
        public static void TrimItems(this ToolbarBreadcrumbs breadcrumbs, int countToKeep)
        {
            while (breadcrumbs.childCount > countToKeep)
                breadcrumbs.PopItem();
        }
    }
}
