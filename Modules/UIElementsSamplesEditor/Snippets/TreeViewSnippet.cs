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

            /// <sample>
            // Create some list of data, here simply numbers in a few foldouts
            var items = new List<TreeViewItemData<string>>(110);
            for (var i = 0; i < 10; i++)
            {
                var itemIndex = i * 10 + i;

                var treeViewSubItemsData = new List<TreeViewItemData<string>>(10);
                for (var j = 0; j < 10; j++)
                    treeViewSubItemsData.Add(new TreeViewItemData<string>(itemIndex + j + 1, (j+1).ToString()));

                var treeViewItemData = new TreeViewItemData<string>(itemIndex, (i+1).ToString(), treeViewSubItemsData);
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
                (e as Label).text = item;
            };

            treeView.SetRootItems(items);
            treeView.makeItem = makeItem;
            treeView.bindItem = bindItem;
            treeView.selectionType = SelectionType.Multiple;
            treeView.Rebuild();

            // Callback invoked when the user double clicks an item
            treeView.itemsChosen += Debug.Log;

            // Callback invoked when the user changes the selection inside the TreeView
            treeView.selectedIndicesChanged += Debug.Log;
            /// </sample>
        }
    }
}
