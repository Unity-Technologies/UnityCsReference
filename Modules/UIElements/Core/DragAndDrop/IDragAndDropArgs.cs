// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal interface IListDragAndDropArgs
    {
        object target { get; }
        int insertAtIndex { get; }
        int parentId { get; }
        int childIndex { get; }
        DragAndDropData dragAndDropData { get; }
        DragAndDropPosition dragAndDropPosition { get; }
    }

    internal struct DragAndDropArgs : IListDragAndDropArgs
    {
        public object target { get; set; }
        public int insertAtIndex { get; set; }
        public int parentId { get; set; }
        public int childIndex { get; set; }
        public DragAndDropPosition dragAndDropPosition { get; set;  }
        public DragAndDropData dragAndDropData { get; set; }
    }

    /// <summary>
    /// Position where the drop operation occurs.
    /// </summary>
    public enum DragAndDropPosition
    {
        /// <summary>
        /// Dragging over an item, to add as a child.
        /// </summary>
        OverItem,
        /// <summary>
        /// Dragging between items, to add as a sibling.
        /// </summary>
        BetweenItems,
        /// <summary>
        /// Dragging in front or after visible items.
        /// </summary>
        OutsideItems
    }
}
