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
            UpdateHierarchy(usesAnimatedDragger);
        }

        void UpdateHierarchy(bool usesAnimatedDragger)
        {
            if (usesAnimatedDragger)
            {
                if (m_Container != null)
                    return;

                m_Container = new VisualElement { name = ListView.reorderableItemUssClassName };
                m_Container.AddToClassList(ListView.reorderableItemUssClassName);

                m_DragHandle = new VisualElement { name = ListView.reorderableItemHandleUssClassName };
                m_DragHandle.AddToClassList(ListView.reorderableItemHandleUssClassName);

                var handle1 = new VisualElement { name = ListView.reorderableItemHandleBarUssClassName };
                handle1.AddToClassList(ListView.reorderableItemHandleBarUssClassName);
                m_DragHandle.Add(handle1);
                var handle2 = new VisualElement { name = ListView.reorderableItemHandleBarUssClassName };
                handle2.AddToClassList(ListView.reorderableItemHandleBarUssClassName);
                m_DragHandle.Add(handle2);

                m_ItemContainer = new VisualElement { name = ListView.reorderableItemContainerUssClassName };
                m_ItemContainer.AddToClassList(ListView.reorderableItemContainerUssClassName);
                m_ItemContainer.Add(bindableElement);

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
                    rootElement.AddToClassList(ListView.reorderableItemUssClassName);
                }
            }
            else
            {
                if (m_DragHandle?.parent != null)
                {
                    m_DragHandle.RemoveFromHierarchy();
                    rootElement.RemoveFromClassList(ListView.reorderableItemUssClassName);
                }
            }
        }

        public override void PreAttachElement()
        {
            base.PreAttachElement();
            rootElement.AddToClassList(ListView.itemUssClassName);
        }

        public override void DetachElement()
        {
            base.DetachElement();
            rootElement.RemoveFromClassList(ListView.itemUssClassName);
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
