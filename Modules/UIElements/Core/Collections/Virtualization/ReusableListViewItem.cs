// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    class ReusableListViewItem : ReusableCollectionItem
    {
        VisualElement m_Container;
        VisualElement m_DragHandle;
        VisualElement m_ItemContainer;

        public override VisualElement rootElement => m_Container ?? bindableElement;

        public void Init(VisualElement item, bool usesAnimatedDragger)
        {
            base.Init(item);

            var root = new VisualElement() { name = BaseListView.reorderableItemUssClassName };
            UpdateHierarchy(root, bindableElement, usesAnimatedDragger);
        }

        protected void UpdateHierarchy(VisualElement root, VisualElement item, bool usesAnimatedDragger)
        {
            if (usesAnimatedDragger)
            {
                if (m_Container != null)
                    return;

                m_Container = root;
                m_Container.AddToClassList(BaseListView.reorderableItemUssClassName);

                m_DragHandle = new VisualElement { name = BaseListView.reorderableItemHandleUssClassName };
                m_DragHandle.AddToClassList(BaseListView.reorderableItemHandleUssClassName);

                var handle1 = new VisualElement { name = BaseListView.reorderableItemHandleBarUssClassName };
                handle1.AddToClassList(BaseListView.reorderableItemHandleBarUssClassName);
                m_DragHandle.Add(handle1);
                var handle2 = new VisualElement { name = BaseListView.reorderableItemHandleBarUssClassName };
                handle2.AddToClassList(BaseListView.reorderableItemHandleBarUssClassName);
                m_DragHandle.Add(handle2);

                m_ItemContainer = new VisualElement { name = BaseListView.reorderableItemContainerUssClassName };
                m_ItemContainer.AddToClassList(BaseListView.reorderableItemContainerUssClassName);
                m_ItemContainer.Add(item);

                m_Container.Add(m_DragHandle);
                m_Container.Add(m_ItemContainer);
            }
            else
            {
                if (m_Container == null)
                    return;

                m_Container.RemoveFromHierarchy();
                m_Container = null;
            }
        }

        public void UpdateDragHandle(bool needsDragHandle)
        {
            if (needsDragHandle)
            {
                if (m_DragHandle.parent == null)
                {
                    rootElement.Insert(0, m_DragHandle);
                    rootElement.AddToClassList(BaseListView.reorderableItemUssClassName);
                }
            }
            else
            {
                if (m_DragHandle?.parent != null)
                {
                    m_DragHandle.RemoveFromHierarchy();
                    rootElement.RemoveFromClassList(BaseListView.reorderableItemUssClassName);
                }
            }
        }

        public override void PreAttachElement()
        {
            base.PreAttachElement();
            rootElement.AddToClassList(BaseListView.itemUssClassName);
        }

        public override void DetachElement()
        {
            base.DetachElement();
            rootElement.RemoveFromClassList(BaseListView.itemUssClassName);
        }

        public override void SetDragGhost(bool dragGhost)
        {
            base.SetDragGhost(dragGhost);
            if (m_DragHandle != null)
            {
                m_DragHandle.style.display = isDragGhost ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }
}
