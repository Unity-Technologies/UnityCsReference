// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    internal class CollectionViewController
    {
        BaseVerticalCollectionView m_View;
        IList m_ItemsSource;

        public event Action itemsSourceChanged;
        public event Action<int, int> itemIndexChanged;

        public IList itemsSource
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

        protected void SetItemsSourceWithoutNotify(IList source)
        {
            m_ItemsSource = source;
        }

        protected BaseVerticalCollectionView view => m_View;

        public void SetView(BaseVerticalCollectionView view)
        {
            m_View = view;
            Assert.IsNotNull(m_View, "View must not be null.");
        }

        public virtual int GetItemsCount()
        {
            return m_ItemsSource?.Count ?? 0;
        }

        public virtual int GetIndexForId(int id)
        {
            return id;
        }

        public virtual int GetIdForIndex(int index)
        {
            return m_View.getItemId?.Invoke(index) ?? index;
        }

        public virtual object GetItemForIndex(int index)
        {
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

        public virtual VisualElement MakeItem()
        {
            if (m_View.makeItem == null)
            {
                if (m_View.bindItem != null)
                    throw new NotImplementedException("You must specify makeItem if bindItem is specified.");
                return new Label();
            }

            return m_View.makeItem.Invoke();
        }

        protected virtual void BindItem(VisualElement element, int index)
        {
            if (m_View.bindItem == null)
            {
                if (m_View.makeItem != null)
                    throw new NotImplementedException("You must specify bindItem if makeItem is specified.");

                var label = (Label)element;
                var item = m_ItemsSource[index];
                label.text = item?.ToString() ?? "null";
                return;
            }

            m_View.bindItem.Invoke(element, index);
        }

        public virtual void UnbindItem(VisualElement element, int index)
        {
            m_View.unbindItem?.Invoke(element, index);
        }

        public virtual void DestroyItem(VisualElement element)
        {
            m_View.destroyItem?.Invoke(element);
        }

        protected void RaiseItemsSourceChanged()
        {
            itemsSourceChanged?.Invoke();
        }

        protected void RaiseItemIndexChanged(int srcIndex, int dstIndex)
        {
            itemIndexChanged?.Invoke(srcIndex, dstIndex);
        }
    }
}
