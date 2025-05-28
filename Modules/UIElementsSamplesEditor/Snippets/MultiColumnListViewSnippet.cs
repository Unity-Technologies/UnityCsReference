// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace UnityEditor.UIElements.Samples
{
    internal class MultiColumnListViewSnippet : ElementSnippet<MultiColumnListViewSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Create some list of data, here simply numbers in interval [1, 20]
            const int itemCount = 20;
            var items = new List<string>(itemCount);
            for (var i = 0; i < itemCount; i++)
                items.Add(i.ToString());

            // The columns were created in the UXML but they can also be set using MultiColumnTreeView.columns here.
            var multiColumnListView = container.Q<MultiColumnListView>();

            multiColumnListView.showBoundCollectionSize = false;
            multiColumnListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            multiColumnListView.selectionType = SelectionType.Multiple;

            // Set MultiColumnListView.itemsSource to populate the data in the list.
            multiColumnListView.itemsSource = items;

            // For each column, set Column.makeCell to initialize each cell in the column.
            // You can index the columns array with names or numerical indices.
            multiColumnListView.columns["index"].makeCell = () => new Label();
            multiColumnListView.columns["active"].makeCell = () =>
            {
                var toggle = new Toggle();
                toggle.SetEnabled(false);
                return toggle;
            };

            // For each column, set Column.bindCell to bind an initialized cell to a data item.
            multiColumnListView.columns["index"].bindCell = (element, index) =>
                ((Label)element).text = items[index];

            multiColumnListView.columns["active"].bindCell = (element, index) =>
                ((Toggle)element).value = index % 2 == 0;

            // Callback invoked when the user double clicks an item
            multiColumnListView.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
            };

            // Callback invoked when the user changes the selection inside the MultiColumnListView
            multiColumnListView.selectedIndicesChanged += (selectedIndices) =>
            {
                Debug.Log("Index selected: " + string.Join(", ", selectedIndices));

                // Note: selectedIndices can also be used to get the selected items from the itemsSource directly or
                // by using multiColumnListView.viewController.GetItemForIndex(index).
            };
            /// </sample>
        }
    }
}
