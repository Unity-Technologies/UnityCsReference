namespace UnityEngine.UIElements
{
    internal interface IListDragAndDropArgs
    {
        object target { get; }
        int insertAtIndex { get; }
        IDragAndDropData dragAndDropData { get; }
        DragAndDropPosition dragAndDropPosition { get; }
    }

    internal struct ListDragAndDropArgs : IListDragAndDropArgs
    {
        public object target { get; set; }
        public int insertAtIndex { get; set; }
        public DragAndDropPosition dragAndDropPosition { get; set;  }

        public IDragAndDropData dragAndDropData
        {
            get { return DragAndDropUtility.dragAndDrop.data; }
        }
    }

    internal enum DragAndDropPosition
    {
        OverItem,
        BetweenItems,
        OutsideItems
    }
}
