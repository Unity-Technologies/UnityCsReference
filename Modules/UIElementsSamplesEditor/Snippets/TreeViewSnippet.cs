// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace UnityEditor.UIElements.Samples
{
    internal class TreeViewSnippet : ElementSnippet<TreeViewSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            var treeView = container.Q<TreeView>();

            #region sample
            /// <sample>
            // Create some list of data, here simply numbers in a few foldouts
            var items = new List<TreeViewItemData<string>>(10);
            for (var i = 0; i < 10; i++)
            {
                var itemIndex = i * 10 + i;

                var treeViewSubItemsData = new List<TreeViewItemData<string>>(10);
                for (var j = 0; j < 10; j++)
                    treeViewSubItemsData.Add(new TreeViewItemData<string>(itemIndex + j + 1, $"Data {i+1}-{j+1}"));

                var treeViewItemData = new TreeViewItemData<string>(itemIndex, $"Data {i+1}", treeViewSubItemsData);
                items.Add(treeViewItemData);
            };

            // The "makeItem" function will be called as needed
            // when the TreeView needs more items to render
            Func<VisualElement> makeItem = () => new Label();

            // As the user scrolls through the list, the TreeView object
            // will recycle elements created by the "makeItem"
            // and invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the list)
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                var item = treeView.GetItemDataForIndex<string>(i);
                var id = treeView.GetIdForIndex(i);
                ((Label)e).text = $"ID {id} - {item}";
            };

            treeView.SetRootItems(items);
            treeView.makeItem = makeItem;
            treeView.bindItem = bindItem;
            treeView.selectionType = SelectionType.Multiple;
            treeView.Rebuild();

            // Callback invoked when the user double clicks an item
            treeView.itemsChosen += (selectedItems) =>
            {
                Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
            };

            // Callback invoked when the user changes the selection inside the TreeView
            treeView.selectedIndicesChanged += (selectedIndices) =>
            {
                var log = "IDs selected: ";
                foreach (var index in selectedIndices)
                {
                    log += $"{treeView.GetIdForIndex(index)}, ";
                }
                Debug.Log(log.TrimEnd(',', ' '));
            };
            /// </sample>
            #endregion
        }
    }
}
