// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        public IDragAndDropData dragAndDropData { get; set; }
    }

    internal enum DragAndDropPosition
    {
        OverItem,
        BetweenItems,
        OutsideItems
    }
}
