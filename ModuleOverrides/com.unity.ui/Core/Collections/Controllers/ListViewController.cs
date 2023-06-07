// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// List view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="ListView"/> inheritor.
    /// </summary>
    public class ListViewController : BaseListViewController
    {
        /// <summary>
        /// View for this controller, cast as a <see cref="ListView"/>.
        /// </summary>
        protected ListView listView => view as ListView;

        /// <inheritdoc />
        protected override VisualElement MakeItem()
        {
            if (listView.makeItem == null)
            {
                if (listView.bindItem != null)
                    throw new NotImplementedException("You must specify makeItem if bindItem is specified.");
                return new Label();
            }
            return listView.makeItem.Invoke();
        }

        /// <inheritdoc />
        protected override void BindItem(VisualElement element, int index)
        {
            if (listView.bindItem == null)
            {
                var isMakeItemSet = listView.makeItem != null;

                // bindItem doesn't need to be specified if we are using data binding on the element.
                if (listView.autoAssignSource && isMakeItemSet)
                    return;

                if (isMakeItemSet)
                    throw new NotImplementedException("You must specify bindItem if makeItem is specified.");

                var label = (Label)element;
                var item = listView.itemsSource[index];
                label.text = item?.ToString() ?? "null";
                return;
            }

            listView.bindItem.Invoke(element, index);
        }

        /// <inheritdoc />
        protected override void UnbindItem(VisualElement element, int index)
        {
            listView.unbindItem?.Invoke(element, index);
        }

        /// <inheritdoc />
        protected override void DestroyItem(VisualElement element)
        {
            listView.destroyItem?.Invoke(element);
        }
    }
}
