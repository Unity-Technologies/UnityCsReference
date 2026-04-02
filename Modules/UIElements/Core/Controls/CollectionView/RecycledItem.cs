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

                if (Mathf.Approximately(pos.y, value))
                    return;

                pos.y = value;
                m_Element.style.translate= pos;
            }
        }

        public static RecycledItem AllocateItem(VisualElement element, CollectionView parent)
        {
            var item = s_ItemPool.Get();
            element.style.position = Position.Absolute;
            element.style.top = 0;
            element.style.left = 0;
            element.style.right = 0;
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
            this.element = element;
            index = k_UndefinedIndex;
            element.AddToClassList(BaseVerticalCollectionView.itemUssClassNameUnique);
        }

        public static void UpdatePositions(RecycledItem item)
        {
            // we update the position of this item, and the ones after it
            var current = item.node;

            while (current != null)
            {
                var itemHeight = current.Value.m_CollectionView.fixedItemHeight;

                if (!float.IsNaN(itemHeight) && itemHeight > 0)
                {
                    current.Value.UpdatePosition();

                    if (current.Next == null)
                    {
                        current.Value.m_CollectionView.UpdateScrollingRangeAfterLayout();
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
                pos = node.Previous.Value.verticalOffset + m_CollectionView.fixedItemHeight;
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

            element.RemoveFromClassList(BaseVerticalCollectionView.itemUssClassNameUnique);
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
                    element.AddToClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassNameUnique);
                    element.pseudoStates |= PseudoStates.Checked;
                }
                else
                {
                    element.RemoveFromClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassNameUnique);
                    element.pseudoStates &= ~PseudoStates.Checked;
                }
            }
        }

        public void ClearHoverState()
        {
            if (element != null)
            {
                element.pseudoStates &= ~PseudoStates.Hover;
            }
        }
    }
}
