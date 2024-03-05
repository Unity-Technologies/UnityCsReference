// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base collection list view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="BaseListView"/> inheritor.
    /// </summary>
    public abstract class BaseListViewController : CollectionViewController
    {
        /// <summary>
        /// Raised when the <see cref="CollectionViewController.itemsSource"/> size changes.
        /// </summary>
        public event Action itemsSourceSizeChanged;

        /// <summary>
        /// Raised when an item is added to the <see cref="CollectionViewController.itemsSource"/>.
        /// </summary>
        public event Action<IEnumerable<int>> itemsAdded;

        /// <summary>
        /// Raised when an item is removed from the <see cref="CollectionViewController.itemsSource"/>.
        /// </summary>
        public event Action<IEnumerable<int>> itemsRemoved;

        /// <summary>
        /// View for this controller, cast as a <see cref="BaseListView"/>.
        /// </summary>
        protected BaseListView baseListView => view as BaseListView;

        internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableListViewItem listItem)
            {
                listItem.Init(MakeItem(), baseListView.reorderable && baseListView.reorderMode == ListViewReorderMode.Animated);
                PostInitRegistration(listItem);
            }
        }

        internal void PostInitRegistration(ReusableListViewItem listItem)
        {
            listItem.bindableElement.style.position = Position.Relative;
            listItem.bindableElement.style.flexBasis = StyleKeyword.Initial;
            listItem.bindableElement.style.marginTop = 0f;
            listItem.bindableElement.style.marginBottom = 0f;
            listItem.bindableElement.style.paddingTop = 0f;
            listItem.bindableElement.style.flexGrow = 0f;
            listItem.bindableElement.style.flexShrink = 0f;
        }

        internal override void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
        {
            if (reusableItem is ReusableListViewItem listItem)
            {
                var usesAnimatedDragger = baseListView.reorderable && baseListView.reorderMode == ListViewReorderMode.Animated;
                listItem.UpdateDragHandle(usesAnimatedDragger && NeedsDragHandle(index));
            }

            base.InvokeBindItem(reusableItem, index);
        }

        /// <summary>
        /// Returns whether this item needs a drag handle or not with the Animated drag mode.
        /// </summary>
        /// <param name="index">Item index.</param>
        /// <returns>Whether or not the drag handle is needed.</returns>
        public virtual bool NeedsDragHandle(int index)
        {
            return true;
        }

        /// <summary>
        /// Adds a certain amount of items at the end of the collection.
        /// </summary>
        /// <param name="itemCount">The number of items to add.</param>
        public virtual void AddItems(int itemCount)
        {
            if (itemCount <= 0)
                return;

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
                    var sourceType = itemsSource.GetType();
                    bool IsGenericList(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>);
                    var listType = sourceType.GetInterfaces().FirstOrDefault(IsGenericList);
                    if (listType != null && listType.GetGenericArguments()[0].IsValueType)
                    {
                        var elementValueType = listType.GetGenericArguments()[0];
                        for (var i = 0; i < itemCount; i++)
                        {
                            indices.Add(previousCount + i);
                            itemsSource.Add(Activator.CreateInstance(elementValueType));
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
                }

                RaiseItemsAdded(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }

            RaiseOnSizeChanged();
        }

        /// <summary>
        /// Moves an item in the source.
        /// </summary>
        /// <param name="index">The source index.</param>
        /// <param name="newIndex">The destination index.</param>
        public virtual void Move(int index, int newIndex)
        {
            if (itemsSource == null)
                return;

            if (index == newIndex)
                return;

            var minIndex = Mathf.Min(index, newIndex);
            var maxIndex = Mathf.Max(index, newIndex);

            if (minIndex < 0 || maxIndex >= itemsSource.Count)
                return;

            var destinationIndex = newIndex;
            var direction = newIndex < index ? 1 : -1;

            while (Mathf.Min(index, newIndex) < Mathf.Max(index, newIndex))
            {
                Swap(index, newIndex);
                newIndex += direction;
            }

            RaiseItemIndexChanged(index, destinationIndex);
        }

        /// <summary>
        /// Removes an item from the source, by index.
        /// </summary>
        /// <param name="index">The item index.</param>
        public virtual void RemoveItem(int index)
        {
            using (ListPool<int>.Get(out var indices))
            {
                indices.Add(index);
                RemoveItems(indices);
            }
        }

        /// <summary>
        /// Removes items from the source, by indices.
        /// </summary>
        /// <param name="indices">A list of indices to remove.</param>
        public virtual void RemoveItems(List<int> indices)
        {
            EnsureItemSourceCanBeResized();

            if (indices == null)
                return;

            indices.Sort();
            RaiseItemsRemoved(indices);

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

            RaiseOnSizeChanged();
        }

        internal virtual void RemoveItems(int itemCount)
        {
            if (itemCount <= 0)
                return;

            var previousCount = GetItemsCount();
            var indices = ListPool<int>.Get();
            try
            {
                var newItemCount = previousCount - itemCount;
                for (var i = newItemCount; i < previousCount; i++)
                {
                    indices.Add(i);
                }

                RemoveItems(indices);
            }
            finally
            {
                ListPool<int>.Release(indices);
            }
        }

        /// <summary>
        /// Removes all items from the source.
        /// </summary>
        public virtual void ClearItems()
        {
            if (itemsSource == null)
                return;
            EnsureItemSourceCanBeResized();
            var itemsSourceIndices = Enumerable.Range(0, itemsSource.Count - 1);
            itemsSource.Clear();
            RaiseItemsRemoved(itemsSourceIndices);
            RaiseOnSizeChanged();
        }

        /// <summary>
        /// Invokes the <see cref="itemsSourceSizeChanged"/> event.
        /// </summary>
        protected void RaiseOnSizeChanged()
        {
            itemsSourceSizeChanged?.Invoke();
        }

        /// <summary>
        /// Invokes the <see cref="itemsAdded"/> event.
        /// </summary>
        protected void RaiseItemsAdded(IEnumerable<int> indices)
        {
            itemsAdded?.Invoke(indices);
        }

        /// <summary>
        /// Invokes the <see cref="itemsRemoved"/> event.
        /// </summary>
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
            (itemsSource[lhs], itemsSource[rhs]) = (itemsSource[rhs], itemsSource[lhs]);
        }

        void EnsureItemSourceCanBeResized()
        {
            var itemsSourceType = itemsSource?.GetType();

            var itemsSourceIsArray = itemsSourceType?.IsArray ?? false;
            if (itemsSource == null || itemsSource.IsFixedSize && !itemsSourceIsArray)
                throw new InvalidOperationException("Cannot add or remove items from source, because it is null or its size is fixed.");
        }
    }
}
