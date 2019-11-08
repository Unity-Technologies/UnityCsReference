// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
