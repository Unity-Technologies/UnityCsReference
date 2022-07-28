// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base collection view controller. View controllers are meant to take care of data virtualized by any <see cref="BaseVerticalCollectionView"/> inheritor.
    /// </summary>
    public abstract class CollectionViewController : IDisposable
    {
        BaseVerticalCollectionView m_View;
        IList m_ItemsSource;

        /// <summary>
        /// Raised when the <see cref="itemsSource"/> changes.
        /// </summary>
        public event Action itemsSourceChanged;

        /// <summary>
        /// Raised when an item in the source changes index.
        /// The first argument is source index, second is destination index.
        /// </summary>
        public event Action<int, int> itemIndexChanged;

        /// <summary>
        /// The items source stored in a non-generic list.
        /// </summary>
        public virtual IList itemsSource
        {
            get => m_ItemsSource;
            set
            {
                if (m_ItemsSource == value)
                    return;
                m_ItemsSource = value;
                RaiseItemsSourceChanged();
            }
        }

        /// <summary>
        /// Set the <see cref="itemsSource"/> without raising the <see cref="itemsSourceChanged"/> event.
        /// </summary>
        /// <param name="source">The new source.</param>
        protected void SetItemsSourceWithoutNotify(IList source)
        {
            m_ItemsSource = source;
        }

        /// <summary>
        /// The view for this controller.
        /// </summary>
        protected BaseVerticalCollectionView view => m_View;

        /// <summary>
        /// Sets the view for this controller.
        /// </summary>
        /// <param name="collectionView">The view for this controller. Must not be null.</param>
        public void SetView(BaseVerticalCollectionView collectionView)
        {
            m_View = collectionView;
            PrepareView();
            Assert.IsNotNull(m_View, "View must not be null.");
        }

        /// <summary>
        /// Initialization step once the view is set.
        /// </summary>
        protected virtual void PrepareView()
        {
            // Nothing to do here in the base class.
        }

        /// <summary>
        /// Called when this controller is not longer needed to provide a way to release resources.
        /// </summary>
        public virtual void Dispose()
        {
            itemsSourceChanged = null;
            itemIndexChanged = null;
            m_View = null;
        }

        /// <summary>
        /// Returns the expected item count in the source.
        /// </summary>
        /// <returns>The item count.</returns>
        public virtual int GetItemsCount()
        {
            return m_ItemsSource?.Count ?? 0;
        }

        internal virtual int GetItemsMinCount() => GetItemsCount();

        /// <summary>
        /// Returns the index for the specified id.
        /// </summary>
        /// <param name="id">The item id..</param>
        /// <returns>The item index.</returns>
        /// <remarks>For example, the index will be different from the id in a tree.</remarks>
        public virtual int GetIndexForId(int id)
        {
            return id;
        }

        /// <summary>
        /// Returns the id for the specified index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The item id.</returns>
        /// <remarks>For example, the index will be different from the id in a tree.</remarks>
        public virtual int GetIdForIndex(int index)
        {
            return m_View.getItemId?.Invoke(index) ?? index;
        }

        /// <summary>
        /// Returns the item for the specified index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The object in the source at this index.</returns>
        public virtual object GetItemForIndex(int index)
        {
            if (m_ItemsSource == null)
                return null;

            if (index < 0 || index >= m_ItemsSource.Count)
                return null;

            return m_ItemsSource[index];
        }

        internal virtual void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            reusableItem.Init(MakeItem());
        }

        internal virtual void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
        {
            BindItem(reusableItem.bindableElement, index);
            reusableItem.SetSelected(m_View.selectedIndices.Contains(index));
            reusableItem.rootElement.pseudoStates &= ~PseudoStates.Hover;
        }

        internal virtual void InvokeUnbindItem(ReusableCollectionItem reusableItem, int index)
        {
            UnbindItem(reusableItem.bindableElement, index);
        }

        internal virtual void InvokeDestroyItem(ReusableCollectionItem reusableItem)
        {
            DestroyItem(reusableItem.bindableElement);
        }

        /// <summary>
        /// Creates a VisualElement to use in the virtualization of the collection view.
        /// </summary>
        /// <returns>A VisualElement for the row.</returns>
        protected abstract VisualElement MakeItem();

        /// <summary>
        /// Binds a row to an item index.
        /// </summary>
        /// <param name="element">The element from that row, created by MakeItem().</param>
        /// <param name="index">The item index.</param>
        protected abstract void BindItem(VisualElement element, int index);

        /// <summary>
        /// Unbinds a row to an item index.
        /// </summary>
        /// <param name="element">The element from that row, created by MakeItem().</param>
        /// <param name="index">The item index.</param>
        protected abstract void UnbindItem(VisualElement element, int index);

        /// <summary>
        /// Destroys a VisualElement when the view is rebuilt or cleared.
        /// </summary>
        /// <param name="element">The element being destroyed.</param>
        protected abstract void DestroyItem(VisualElement element);

        /// <summary>
        /// Invokes the <see cref="itemsSourceChanged"/> event.
        /// </summary>
        protected void RaiseItemsSourceChanged()
        {
            itemsSourceChanged?.Invoke();
        }

        /// <summary>
        /// Invokes the <see cref="itemIndexChanged"/> event.
        /// </summary>
        /// <param name="srcIndex">The source index.</param>
        /// <param name="dstIndex">The destination index.</param>
        protected void RaiseItemIndexChanged(int srcIndex, int dstIndex)
        {
            itemIndexChanged?.Invoke(srcIndex, dstIndex);
        }
    }
}
