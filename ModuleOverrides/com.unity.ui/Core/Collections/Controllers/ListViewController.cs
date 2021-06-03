// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    internal class ListViewController : CollectionViewController
    {
        public event Action itemsSourceSizeChanged;
        public event Action<IEnumerable<int>> itemsAdded;
        public event Action<IEnumerable<int>> itemsRemoved;

        ListView listView => view as ListView;

        internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableListViewItem listItem)
            {
                listItem.Init(MakeItem(), listView.reorderable && listView.reorderMode == ListViewReorderMode.Animated);
                listItem.bindableElement.style.position = Position.Relative;
                listItem.bindableElement.style.flexBasis = StyleKeyword.Initial;
                listItem.bindableElement.style.marginTop = 0f;
                listItem.bindableElement.style.marginBottom = 0f;
                listItem.bindableElement.style.flexGrow = 0f;
                listItem.bindableElement.style.flexShrink = 0f;
            }
        }

        internal override void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
        {
            if (reusableItem is ReusableListViewItem listItem)
            {
                var usesAnimatedDragger = listView.reorderable && listView.reorderMode == ListViewReorderMode.Animated;
                listItem.UpdateDragHandle(usesAnimatedDragger && NeedsDragHandle(index));
            }

            base.InvokeBindItem(reusableItem, index);
        }

        public virtual bool NeedsDragHandle(int index)
        {
            return !listView.sourceIncludesArraySize || index != 0;
        }

        public virtual void AddItems(int itemCount)
        {
            EnsureItemSourceCanBeResized();
            var previousCount = itemsSource.Count;
            var indices = ListPool<int>.Get();
            try
            {
                if (itemsSource.IsFixedSize)
                {
                    itemsSource = AddToArray((Array)itemsSource, itemCount);

                    for (var i = 0; i < itemCount; i++)
                    {
                        indices.Add(previousCount + i);
                    }
                }
                else
                {
                    for (var i = 0; i < itemCount; i++)
                    {
                        indices.Add(previousCount + i);
                        itemsSource.Add(default);
                    }
                }

                RaiseItemsAdded(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }

            RaiseOnSizeChanged();

            if (itemsSource.IsFixedSize)
                listView.Rebuild();
        }

        public virtual void Move(int index, int newIndex)
        {
            var destinationIndex = newIndex;
            var direction = newIndex < index ? 1 : -1;

            while (Mathf.Min(index, newIndex) < Mathf.Max(index, newIndex))
            {
                Swap(index, newIndex);
                newIndex += direction;
            }

            RaiseItemIndexChanged(index, destinationIndex);
        }

        public virtual void RemoveItem(int index)
        {
            var indices = ListPool<int>.Get();
            try
            {
                indices.Add(index);
                RemoveItems(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }
        }

        public virtual void RemoveItems(List<int> indices)
        {
            EnsureItemSourceCanBeResized();
            indices.Sort();

            if (itemsSource.IsFixedSize)
            {
                itemsSource = RemoveFromArray((Array)itemsSource, indices);
            }
            else
            {
                for (var i = indices.Count - 1; i >= 0; i--)
                {
                    itemsSource.RemoveAt(indices[i]);
                }
            }

            RaiseItemsRemoved(indices);
            RaiseOnSizeChanged();
        }

        protected void RaiseOnSizeChanged()
        {
            itemsSourceSizeChanged?.Invoke();
        }

        protected void RaiseItemsAdded(IEnumerable<int> indices)
        {
            itemsAdded?.Invoke(indices);
        }

        protected void RaiseItemsRemoved(IEnumerable<int> indices)
        {
            itemsRemoved?.Invoke(indices);
        }

        static Array AddToArray(Array source, int itemCount)
        {
            var elementType = source.GetType().GetElementType();
            if (elementType == null)
                throw new InvalidOperationException("Cannot resize source, because its size is fixed.");

            var newItemsSource = Array.CreateInstance(elementType, source.Length + itemCount);
            Array.Copy(source, newItemsSource, source.Length);
            return newItemsSource;
        }

        // Requires the list to be sorted without duplicates.
        static Array RemoveFromArray(Array source, List<int> indicesToRemove)
        {
            var count = source.Length;
            var newCount = count - indicesToRemove.Count;
            if (newCount < 0)
                throw new InvalidOperationException("Cannot remove more items than the current count from source.");

            var elementType = source.GetType().GetElementType();
            if (newCount == 0)
                return Array.CreateInstance(elementType, 0);

            var newSource = Array.CreateInstance(elementType, newCount);

            var newSourceIndex = 0;
            var toRemove = 0;
            for (var index = 0; index < source.Length; ++index)
            {
                if (toRemove < indicesToRemove.Count && indicesToRemove[toRemove] == index)
                {
                    ++toRemove;
                    continue;
                }

                newSource.SetValue(source.GetValue(index), newSourceIndex);
                ++newSourceIndex;
            }

            return newSource;
        }

        void Swap(int lhs, int rhs)
        {
            var current = itemsSource[lhs];
            itemsSource[lhs] = itemsSource[rhs];
            itemsSource[rhs] = current;
        }

        void EnsureItemSourceCanBeResized()
        {
            if (itemsSource.IsFixedSize && !itemsSource.GetType().IsArray)
                throw new InvalidOperationException("Cannot add or remove items from source, because its size is fixed.");
        }
    }
}
