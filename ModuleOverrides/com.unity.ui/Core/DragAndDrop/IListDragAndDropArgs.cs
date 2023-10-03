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
        IDragAndDropData dragAndDropData { get; }
        DragAndDropPosition dragAndDropPosition { get; }
    }

    internal struct DragAndDropArgs : IListDragAndDropArgs
    {
        public object target { get; set; }
        public int insertAtIndex { get; set; }
        public int parentId { get; set; }
        public int childIndex { get; set; }
        public DragAndDropPosition dragAndDropPosition { get; set;  }
        public IDragAndDropData dragAndDropData { get; set; }
    }

    internal enum DragAndDropPosition
    {
        OverItem,
        BetweenItems,
        OutsideItems
    }
}
