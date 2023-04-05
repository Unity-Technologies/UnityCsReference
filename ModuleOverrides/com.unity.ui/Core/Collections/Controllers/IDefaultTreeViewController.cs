// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface IDefaultTreeViewController
    {
        object GetItemDataForId(int id);
    }

    internal interface IDefaultTreeViewController<T>
    {
        void SetRootItems(IList<TreeViewItemData<T>> items);
        void AddItem(in TreeViewItemData<T> item, int parentId, int childIndex, bool rebuildTree = true);
        TreeViewItemData<T> GetTreeViewItemDataForId(int id);
        TreeViewItemData<T> GetTreeViewItemDataForIndex(int index);
        T GetDataForId(int id);
        T GetDataForIndex(int index);
    }
}
