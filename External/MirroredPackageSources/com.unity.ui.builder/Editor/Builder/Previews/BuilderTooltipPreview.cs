using System;
using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderTooltipPreview : VisualElement
    {
        static readonly string s_UssClassName = "unity-builder-tooltip-preview";
        static readonly string s_EnablerClassName = "unity-builder-tooltip-preview__enabler";
        static readonly string s_ContainerClassName = "unity-builder-tooltip-preview__container";
        public static readonly string s_EnabledElementName = "enabler";

        VisualElement m_Enabler;
        VisualElement m_Container;

        public bool isShowing => m_Enabler.resolvedStyle.display == DisplayStyle.Flex;

        public new class UxmlFactory : UxmlFactory<BuilderTooltipPreview, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits {}

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;

        public event Action onShow;
        public event Action onHide;

        public BuilderTooltipPreview()
        {
            AddToClassList(s_UssClassName);

            m_Enabler = new VisualElement();
            m_Enabler.name = s_EnabledElementName;
            m_Enabler.AddToClassList(s_EnablerClassName);
            hierarchy.Add(m_Enabler);

            m_Container = new VisualElement();
            m_Container.name = "content-container";
            m_Container.AddToClassList(s_ContainerClassName);
            m_Enabler.Add(m_Container);

            this.RegisterCallback<GeometryChangedEvent>(e => EnsureVisibilityInParent());
        }

        public void Show()
        {
            onShow?.Invoke();
            m_Enabler.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            m_Enabler.style.display = DisplayStyle.None;
            onHide?.Invoke();
        }

        public void Enable()
        {
            this.style.display = DisplayStyle.Flex;
        }

        public void Disable()
        {
            this.style.display = DisplayStyle.None;
        }

        public virtual Vector2 GetAdjustedPosition()
        {
            const float PopupAndWindowEdgesMargin = 10f;

            return new Vector2(Mathf.Min(style.left.value.value, parent.layout.width - resolvedStyle.width - PopupAndWindowEdgesMargin),
                Mathf.Min(style.top.value.value, parent.layout.height - resolvedStyle.height - PopupAndWindowEdgesMargin));
        }

        public void EnsureVisibilityInParent()
        {
            if (parent != null && !float.IsNaN(layout.width) && !float.IsNaN(layout.height))
            {
                var pos = GetAdjustedPosition();

                style.left = pos.x;
                style.top = pos.y;
            }
        }
    }
}
