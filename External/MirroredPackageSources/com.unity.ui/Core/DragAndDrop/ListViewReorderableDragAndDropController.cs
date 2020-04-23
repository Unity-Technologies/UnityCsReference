using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal class ListViewReorderableDragAndDropController : IListViewDragAndDropController
    {
        protected readonly ListView m_ListView;

        public ListViewReorderableDragAndDropController(ListView listView)
        {
            m_ListView = listView;
            enableReordering = true;
        }

        public bool enableReordering { get; set; }
        public Action<ItemMoveArgs<object>> onItemMoved { get; set; }

        public virtual bool CanStartDrag(IEnumerable<object> items)
        {
            return enableReordering;
        }

        public virtual StartDragArgs SetupDragAndDrop(IEnumerable<object> items)
        {
            var title = string.Empty;
            foreach (var unused in items)
            {
                if (string.IsNullOrEmpty(title))
                {
                    var index = m_ListView.selectedIndex;
                    var label = m_ListView.GetRecycledItemFromIndex(index)?.element.Q<Label>();
                    title = label != null ? label.text : $"Item {index}";
                }
                else
                {
                    title = "<Multiple>";
                    break;
                }
            }

            return new StartDragArgs(title, m_ListView);
        }

        public virtual DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args)
        {
            if (args.dragAndDropPosition == DragAndDropPosition.OverItem || !enableReordering)
                return DragVisualMode.Rejected;

            return args.dragAndDropData.userData == m_ListView ? DragVisualMode.Move : DragVisualMode.Rejected;
        }

        public virtual void OnDrop(IListDragAndDropArgs args)
        {
            int indexShift = 0;

            var selectedIndices = m_ListView.selectedIndices.OrderBy(i => i).ToArray();

            for (int i = selectedIndices.Length - 1; i >= 0; --i)
            {
                var index = selectedIndices[i];
                if (index < args.insertAtIndex)
                    indexShift--;

                m_ListView.itemsSource.RemoveAt(index);
            }

            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.OutsideItems:
                case DragAndDropPosition.BetweenItems:
                    InsertRange(args.insertAtIndex + indexShift);
                    break;
                default:
                    throw new ArgumentException($"{args.dragAndDropPosition} is not supported by {nameof(ListViewReorderableDragAndDropController)}.");
            }

            m_ListView.Refresh();
        }

        private void InsertRange(int index)
        {
            var newSelection = new List<int>();
            var selectedItems = m_ListView.selectedItems.ToArray();
            var selectedIndices = m_ListView.selectedIndices.ToArray();

            for (var i = 0; i < selectedItems.Length; i++)
            {
                var item = selectedItems[i];
                m_ListView.itemsSource.Insert(index, item);
                onItemMoved?.Invoke(new ItemMoveArgs<object>
                {
                    item = item,
                    newIndex = index,
                    previousIndex = selectedIndices[i]
                });

                newSelection.Add(index);
                index++;
            }

            m_ListView.SetSelectionWithoutNotify(newSelection);
        }
    }
}
