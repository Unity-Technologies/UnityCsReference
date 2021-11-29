// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    class ReusableCollectionItem
    {
        public const int UndefinedIndex = -1;

        public virtual VisualElement rootElement => bindableElement;
        public VisualElement bindableElement { get; protected set; }
        public ValueAnimation<StyleValues> animator { get; set; }

        public int index { get; set; }
        public int id { get; set; }

        public event Action<ReusableCollectionItem> onGeometryChanged;

        protected EventCallback<GeometryChangedEvent> m_GeometryChangedEventCallback;

        public ReusableCollectionItem()
        {
            index = id = UndefinedIndex;
            m_GeometryChangedEventCallback = OnGeometryChanged;
        }

        public virtual void Init(VisualElement item)
        {
            bindableElement = item;
        }

        public virtual void PreAttachElement()
        {
            rootElement.AddToClassList(BaseVerticalCollectionView.itemUssClassName);
            rootElement.RegisterCallback(m_GeometryChangedEventCallback);
        }

        public virtual void DetachElement()
        {
            rootElement.RemoveFromClassList(BaseVerticalCollectionView.itemUssClassName);
            rootElement.UnregisterCallback(m_GeometryChangedEventCallback);

            rootElement?.RemoveFromHierarchy();
            SetSelected(false);
            index = id = UndefinedIndex;
        }

        public virtual void SetSelected(bool selected)
        {
            if (selected)
            {
                rootElement.AddToClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName);
                rootElement.pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                rootElement.RemoveFromClassList(BaseVerticalCollectionView.itemSelectedVariantUssClassName);
                rootElement.pseudoStates &= ~PseudoStates.Checked;
            }
        }

        protected void OnGeometryChanged(GeometryChangedEvent evt)
        {
            onGeometryChanged?.Invoke(this);
        }
    }
}
