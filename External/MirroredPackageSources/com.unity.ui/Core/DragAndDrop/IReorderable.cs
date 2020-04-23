using System;

namespace UnityEngine.UIElements
{
    internal interface IReorderable<T>
    {
        bool enableReordering { get; set; }
        Action<ItemMoveArgs<T>> onItemMoved { get; set; }
    }

    internal struct ItemMoveArgs<T>
    {
        public T item;
        public int newIndex;
        public int previousIndex;
    }
}
