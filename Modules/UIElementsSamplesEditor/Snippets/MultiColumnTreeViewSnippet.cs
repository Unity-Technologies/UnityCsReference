// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace UnityEditor.UIElements.Samples
{
    internal class MultiColumnTreeViewSnippet : ElementSnippet<MultiColumnTreeViewSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Create some list of data, here simply numbers in a few foldouts
            var items = new List<TreeViewItemData<string>>(10);
            for (var i = 0; i < 10; i++)
            {
                var itemIndex = i * 10 + i;

                var treeViewSubItemsData = new List<TreeViewItemData<string>>(10);
                for (var j = 0; j < 10; j++)
                    treeViewSubItemsData.Add(new TreeViewItemData<string>(itemIndex + j + 1, (j+1).ToString()));

                var treeViewItemData = new TreeViewItemData<string>(itemIndex, (i+1).ToString(), treeViewSubItemsData);
                items.Add(treeViewItemData);
            }

            // The columns were created in the UXML but they can also be set using MultiColumnTreeView.columns here.
            var multiColumnTreeView = container.Q<MultiColumnTreeView>();
            multiColumnTreeView.selectionType = SelectionType.Multiple;

            // Call MultiColumnTreeView.SetRootItems() to populate the data in the tree.
            multiColumnTreeView.SetRootItems(items);

            // For each column, set Column.makeCell to initialize each node in the tree.
            // You can index the columns array with names or numerical indices.
            multiColumnTreeView.columns["index"].makeCell = () => new Label();
            multiColumnTreeView.columns["active"].makeCell = () =>
            {
                var toggle = new Toggle();
                toggle.SetEnabled(false);
                return toggle;
            };

            // For each column, set Column.bindCell to bind an initialized node to a data item.
            multiColumnTreeView.columns["index"].bindCell = (VisualElement element, int index) =>
                (element as Label).text = multiColumnTreeView.GetItemDataForIndex<string>(index);
            multiColumnTreeView.columns["active"].bindCell = (VisualElement element, int index) =>
                (element as Toggle).value = index % 2 == 0;

            // Callback invoked when the user double clicks an item
            multiColumnTreeView.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
            };

            // Callback invoked when the user changes the selection inside the ListView
            multiColumnTreeView.selectionChanged += (selectedItems) =>
            {
                Debug.Log("Items selected: " + string.Join(", ", selectedItems));
            };
            /// </sample>
        }
    }
}
