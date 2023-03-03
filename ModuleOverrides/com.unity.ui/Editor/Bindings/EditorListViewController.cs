// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    internal class EditorListViewController : ListViewController
    {
        public SerializedObjectList serializedObjectList => itemsSource as SerializedObjectList;

        public override int GetItemsCount()
        {
            return serializedObjectList?.Count ?? 0;
        }

        internal override int GetItemsMinCount()
        {
            return serializedObjectList?.ArrayProperty.minArraySize ?? 0;
        }

        public override void AddItems(int itemCount)
        {
            var previousCount = GetItemsCount();
            serializedObjectList.ArrayProperty.arraySize += itemCount;
            serializedObjectList.ApplyChanges();

            var indices = ListPool<int>.Get();
            try
            {
                for (var i = 0; i < itemCount; i++)
                {
                    indices.Add(previousCount + i);
                }

                RaiseItemsAdded(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }

            RaiseOnSizeChanged();
        }

        internal override void RemoveItems(int itemCount)
        {
            var previousCount = GetItemsCount();
            serializedObjectList.ArrayProperty.arraySize -= itemCount;

            var indices = ListPool<int>.Get();
            try
            {
                for (var i = previousCount - itemCount; i < previousCount; i++)
                {
                    indices.Add(i);
                }

                RaiseItemsRemoved(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }

            serializedObjectList.ApplyChanges();
            RaiseOnSizeChanged();
        }

        public override void RemoveItems(List<int> indices)
        {
            indices.Sort();

            RaiseItemsRemoved(indices);

            for (var i = indices.Count - 1; i >= 0; i--)
            {
                var index = indices[i];

                if (view.sourceIncludesArraySize)
                {
                    //we must offset everything by 1
                    index--;
                }

                serializedObjectList.RemoveAt(index);
            }

            serializedObjectList.ApplyChanges();
            RaiseOnSizeChanged();
        }

        public override void RemoveItem(int index)
        {
            if (view.sourceIncludesArraySize)
            {
                //we must offset everything by 1
                index--;
            }

            using (ListPool<int>.Get(out var indices))
            {
                indices.Add(index);
                RaiseItemsRemoved(indices);
            }

            serializedObjectList.RemoveAt(index);
            serializedObjectList.ApplyChanges();

            RaiseOnSizeChanged();
        }

        public override void ClearItems()
        {
            var itemsSourceIndices = Enumerable.Range(0, GetItemsMinCount() - 1);
            serializedObjectList.ArrayProperty.arraySize = 0;
            serializedObjectList.ApplyChanges();
            RaiseItemsRemoved(itemsSourceIndices);
            RaiseOnSizeChanged();
        }

        public override void Move(int srcIndex, int destIndex)
        {
            if (view.sourceIncludesArraySize)
            {
                //we must offset everything by 1
                srcIndex--;
                destIndex--;
            }

            if (srcIndex == destIndex)
                return;

            serializedObjectList.Move(srcIndex, destIndex);
            serializedObjectList.ApplyChanges();
            RaiseItemIndexChanged(srcIndex, destIndex);
        }
    }
}
