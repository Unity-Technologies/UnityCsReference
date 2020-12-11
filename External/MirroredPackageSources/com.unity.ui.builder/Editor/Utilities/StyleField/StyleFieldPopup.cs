using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleFieldPopup : VisualElement
    {
        static readonly string s_UssClassName = "unity-style-field-popup";
        const int k_PopupMaxWidth = 350;

        private VisualElement m_AnchoredControl;

        public event Action onShow;
        public event Action onHide;

        public VisualElement anchoredControl
        {
            get
            {
                return m_AnchoredControl;
            }
            set
            {
                if (m_AnchoredControl == value)
                    return;

                if (m_AnchoredControl != null)
                    m_AnchoredControl.UnregisterCallback<GeometryChangedEvent>(OnAnchoredControlGeometryChanged);

                m_AnchoredControl = value;

                if (m_AnchoredControl != null)
                    m_AnchoredControl.RegisterCallback<GeometryChangedEvent>(OnAnchoredControlGeometryChanged);
            }
        }

        public StyleFieldPopup()
        {
            AddToClassList(s_UssClassName);
            // Popup is hidden by default
            AddToClassList(BuilderConstants.HiddenStyleClassName);
            this.RegisterCallback<GeometryChangedEvent>(e => EnsureVisibilityInParent());
            this.RegisterCallback<MouseDownEvent>(e => e.PreventDefault(), TrickleDown.TrickleDown); // To prevent MouseDownEvent on a child from switching focus
        }

        public virtual void Show()
        {
            AdjustGeometry();
            onShow?.Invoke();
            RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
        }

        public virtual void Hide()
        {
            AddToClassList(BuilderConstants.HiddenStyleClassName);
            onHide?.Invoke();
        }

        void OnAnchoredControlGeometryChanged(GeometryChangedEvent e)
        {
            AdjustGeometry();
        }

        public virtual void AdjustGeometry()
        {
            if (m_AnchoredControl != null && m_AnchoredControl.visible && parent != null && !float.IsNaN(layout.width) && !float.IsNaN(layout.height))
            {
                var pos = m_AnchoredControl.ChangeCoordinatesTo(parent, Vector2.zero);

                style.left = pos.x;
                style.top = pos.y + m_AnchoredControl.layout.height;
                style.width = Math.Max(k_PopupMaxWidth, m_AnchoredControl.resolvedStyle.width);
            }
        }

        public virtual Vector2 GetAdjustedPosition()
        {
            if (m_AnchoredControl == null)
            {
                return new Vector2(Mathf.Min(style.left.value.value, parent.layout.width - resolvedStyle.width),
                    Mathf.Min(style.top.value.value, parent.layout.height - resolvedStyle.height));
            }
            else
            {
                var currentPos = new Vector2(style.left.value.value, style.top.value.value);
                var newPos = new Vector2(Mathf.Min(currentPos.x, parent.layout.width - resolvedStyle.width), currentPos.y);
                var fieldTopLeft = m_AnchoredControl.ChangeCoordinatesTo(parent, Vector2.zero);
                var fieldBottom = fieldTopLeft.y + m_AnchoredControl.layout.height;
                const float tolerance = 2f;

                newPos.y = (fieldBottom < parent.layout.height / 2) ? (currentPos.y) : (fieldTopLeft.y - resolvedStyle.height);

                if (Math.Abs(newPos.x - currentPos.x) > tolerance || Math.Abs(newPos.y - currentPos.y) > tolerance)
                    return newPos;
                return currentPos;
            }
        }

        private void EnsureVisibilityInParent()
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
