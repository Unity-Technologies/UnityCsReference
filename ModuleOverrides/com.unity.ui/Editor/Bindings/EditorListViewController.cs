// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    internal class EditorListViewController : ListViewController
    {
        public SerializedObjectList serializedObjectList => itemsSource as SerializedObjectList;

        public override int GetItemCount()
        {
            return serializedObjectList?.Count ?? 0;
        }

        public override void AddItems(int itemCount)
        {
            var previousCount = GetItemCount();
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

        public override void RemoveItems(List<int> indices)
        {
            indices.Sort();

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

            RaiseItemsRemoved(indices);

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

            serializedObjectList.RemoveAt(index);
            serializedObjectList.ApplyChanges();
            var indices = ListPool<int>.Get();
            try
            {
                indices.Add(index);
                RaiseItemsRemoved(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }

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

            serializedObjectList.Move(srcIndex, destIndex);
            serializedObjectList.ApplyChanges();
            RaiseItemIndexChanged(srcIndex, destIndex);
        }
    }
}
