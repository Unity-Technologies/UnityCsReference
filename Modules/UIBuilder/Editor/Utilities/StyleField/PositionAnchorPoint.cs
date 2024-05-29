// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class PositionAnchorPoint : VisualElement, INotifyValueChanged<bool>
    {
        static readonly string k_UssClassName = "unity-position-anchors__point";
        static readonly string k_UssAnchoredClassName = "anchored";
        static readonly string k_VisualContentName = "focus-outline";
        static readonly string k_ClickableAreaName = "clickable-area";

        VisualElement m_ClickableArea;

        public PositionProperty positionProperty { get; set; }
        public event Action<bool, bool> pointHovered;
        public event Action<PositionProperty, bool> pointSelected;

        private bool m_Value;

        public float x
        {
            set => style.left = new Length(value * 100, LengthUnit.Percent);
        }

        public float y
        {
            set => style.top = new Length(value * 100, LengthUnit.Percent);
        }

        public bool value
        {
            get => m_Value;
            set
            {
                var previousValue = m_Value;

                SetValueWithoutNotify(value);

                // not checking if previous value is equal to new value before sending event to
                // be able to send an event toupdate the position style fields when the square preset is clicked
                using (ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(previousValue, m_Value))
                {
                    evt.elementTarget = this;
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
            EnableInClassList(k_UssAnchoredClassName, newValue);
            // Used to update the container styles
            pointSelected?.Invoke(positionProperty, newValue);
        }

        public PositionAnchorPoint(PositionProperty positionProperty)
        {
            AddToClassList(k_UssClassName);
            style.position = Position.Absolute;

            var visualContentUXMLFile = BuilderConstants.UtilitiesPath + "/StyleField/PositionAnchorPoint.uxml";

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(visualContentUXMLFile);
            var visualContent = template.Instantiate();

            visualContent.pickingMode = PickingMode.Ignore;

            Add(visualContent);
            visualContent.name = k_VisualContentName;

            m_ClickableArea = this.Q(k_ClickableAreaName);

            AddToClassList(positionProperty.ToString().ToLowerInvariant());

            m_ClickableArea.AddManipulator(new Clickable(() =>
            {
                value = !value;
            }));

            RegisterClickableAreaInteractions();
            this.positionProperty = positionProperty;
            m_ClickableArea.tooltip = positionProperty.ToString();

            tabIndex = 1;
            focusable = true;
        }

        void RegisterClickableAreaInteractions()
        {
            m_ClickableArea.RegisterCallback<MouseOverEvent>(_ => pointHovered?.Invoke(false, true));
            m_ClickableArea.RegisterCallback<MouseLeaveEvent>(_ => pointHovered?.Invoke(false, false));
            m_ClickableArea.RegisterCallback<PointerDownEvent>(_ => pointHovered?.Invoke(true, true), TrickleDown.TrickleDown);
            m_ClickableArea.RegisterCallback<PointerUpEvent>(_ => pointHovered?.Invoke(true, false));
        }
    }
}
