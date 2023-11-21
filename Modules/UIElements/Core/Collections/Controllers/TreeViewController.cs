// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="TreeView"/> inheritor.
    /// </summary>
    public abstract class TreeViewController : BaseTreeViewController
    {
        /// <summary>
        /// View for this controller, cast as a <see cref="TreeView"/>.
        /// </summary>
        protected TreeView treeView => view as TreeView;

        /// <inheritdoc />
        protected override VisualElement MakeItem()
        {
            if (treeView.makeItem == null)
            {
                if (treeView.bindItem != null)
                    throw new NotImplementedException("You must specify makeItem if bindItem is specified.");
                return new Label();
            }
            return treeView.makeItem.Invoke();
        }

        /// <inheritdoc />
        protected override void BindItem(VisualElement element, int index)
        {
            if (treeView.bindItem == null)
            {
                var isMakeItemSet = treeView.makeItem != null;

                if (isMakeItemSet)
                    throw new NotImplementedException("You must specify bindItem if makeItem is specified.");

                var label = (Label)element;
                var item = GetItemForIndex(index);
                label.text = item?.ToString() ?? "null";
                return;
            }

            treeView.bindItem.Invoke(element, index);
        }

        /// <inheritdoc />
        protected override void UnbindItem(VisualElement element, int index)
        {
            treeView.unbindItem?.Invoke(element, index);
        }

        /// <inheritdoc />
        protected override void DestroyItem(VisualElement element)
        {
            treeView.destroyItem?.Invoke(element);
        }
    }
}
