// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.HierarchyV2
{
    internal class RecycledItem
    {
        static UnityEngine.Pool.ObjectPool<RecycledItem> s_ItemPool = new (() => new RecycledItem(), null, i => i.DetachElement(), i => i.DestroyElement());

        public LinkedListNode<RecycledItem> node { get; set; }
        public int index;
        public float renderedHeight;
        public bool isLastItem;
        public const int k_UndefinedIndex = -1;

        CollectionView m_CollectionView;
        VisualElement m_Element;

        public VisualElement element
        {
            get => m_Element;
            private set => m_Element = value;
        }

        public float verticalOffset
        {
            get => m_Element.resolvedStyle.translate.y;
            set
            {
                var pos = m_Element.resolvedStyle.translate;
                pos.y = value;
                m_Element.style.translate= pos;
            }
        }

        public static RecycledItem AllocateItem(VisualElement element, CollectionView parent)
        {
            var item = s_ItemPool.Get();
            item.Assign(element, parent);
            item.node = new LinkedListNode<RecycledItem>(item);
            return item;
        }

        public static void Recycle(RecycledItem item)
        {
            s_ItemPool.Release(item);
        }

        public static void ClearItemPool()
        {
            s_ItemPool.Clear();
        }

        public void Assign(VisualElement element, CollectionView parent)
        {
            m_CollectionView = parent;
            renderedHeight = -1;
            this.element = element;
            index = k_UndefinedIndex;
            element.AddToClassList(BaseVerticalCollectionView.itemUssClassName);
            element.RegisterCallback<GeometryChangedEvent>(OnSizeChange);
        }

        void OnSizeChange(GeometryChangedEvent evt)
        {
            renderedHeight = evt.newRect.height;

            if (evt.layoutPass < 4)
            {
                UpdatePositions(this);
            }
        }

        public static void UpdatePositions(RecycledItem item)
        {
            // we update the position of this item, and the ones after it
            var current = item.node;

            while (current != null)
            {
                var currentRenderedHeight = current.Value.renderedHeight;

                if (!float.IsNaN(currentRenderedHeight) && currentRenderedHeight > 0)
                {
                    current.Value.UpdatePosition();

                    if (current.Next == null)
                    {
                        current.Value.m_CollectionView.ItemPositionUpdated(current.Value);
                    }
                }

                current = current.Next;
            }
        }

        void UpdatePosition()
        {
            float pos = 0;

            if (node.Previous != null)
            {
                pos = node.Previous.Value.verticalOffset + node.Previous.Value.renderedHeight;
            }

            if (!Mathf.Approximately(pos, verticalOffset))
            {
                verticalOffset = pos;
            }
        }

        public void DetachElement()
        {
            if (element == null)
                return;

            element.UnregisterCallback<GeometryChangedEvent>(OnSizeChange);
            element.RemoveFromClassList(BaseVerticalCollectionView.itemUssClassName);
            element.RemoveFromHierarchy();
            SetSelected(false);
            index = k_UndefinedIndex;
        }

        void DestroyElement()
        {
            m_CollectionView.OnDestroyItem(this);
        }

        public void SetSelected(bool selected)
        {
            if (element != null)
            {
                if (selected)
                {
                    element.AddToClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName);
                    element.pseudoStates |= PseudoStates.Checked;
                }
                else
                {
                    element.RemoveFromClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName);
                    element.pseudoStates &= ~PseudoStates.Checked;
                }
            }
        }
    }
}
