// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    abstract class TransformOriginSelectorPointBase : VisualElement
    {
        static readonly string s_UssClassName = "unity-transform-origin-selector__point-base";
        static readonly string s_VisualContentName = "visual-content";

        float m_X;
        float m_Y;

        public float x
        {
            get => m_X;
            set
            {
                m_X = value;
                style.left = new Length(m_X * 100, LengthUnit.Percent);
            }
        }

        public float y
        {
            get => m_Y;
            set
            {
                m_Y = value;
                style.top = new Length(m_Y * 100, LengthUnit.Percent);
            }
        }

        protected TransformOriginSelectorPointBase(VisualElement visualContent)
        {
            AddToClassList(s_UssClassName);
            style.position = Position.Absolute;

            visualContent.pickingMode = PickingMode.Ignore;
            visualContent.name = s_VisualContentName;
            Add(visualContent);
        }
    }

    class TransformOriginSelectorPoint : TransformOriginSelectorPointBase
    {
        static readonly string s_UssClassName = "unity-transform-origin-selector__point";
        internal static readonly string s_ClickableAreaName = "clickable-area";

        static VisualElement CreateVisualContent()
        {
            var root = new VisualElement();
            var clickableArea = new VisualElement { name = s_ClickableAreaName };
            clickableArea.Add(new VisualElement { name = "shape", pickingMode = PickingMode.Ignore });
            root.Add(clickableArea);
            return root;
        }

        public TransformOriginSelectorPoint(Action<float, float> clicked, string tooltip) : base(CreateVisualContent())
        {
            AddToClassList(s_UssClassName);

            var clickableArea = this.Q(s_ClickableAreaName);
            clickableArea.AddManipulator(new Clickable(() => clicked(x, y)));
            clickableArea.tooltip = tooltip;
        }
    }

    class TransformOriginSelectorIndicator : TransformOriginSelectorPointBase
    {
        public static readonly string s_UssClassName = "unity-transform-origin-selector__indicator";
        public static readonly string s_IndicatorAtPresetClassName = s_UssClassName + "--preset";

        static VisualElement CreateVisualContent()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "shape", pickingMode = PickingMode.Ignore });
            return root;
        }

        public TransformOriginSelectorIndicator() : base(CreateVisualContent())
        {
            AddToClassList(s_UssClassName);
            pickingMode = PickingMode.Ignore;
            this.Q("shape").pickingMode = PickingMode.Ignore;
        }
    }

    [UxmlElement]
    partial class TransformOriginSelector : VisualElement
    {
        enum NavigationDirection
        {
            Backward = -1,
            Forward = 1
        }

        static readonly string s_UssClassName = "unity-transform-origin-selector";
        public static readonly string s_OutOfRangeHorName = "out-of-range-hor";
        public static readonly string s_OutOfRangeHorClassName = s_UssClassName + "__out-of-range-hor";
        public static readonly string s_OutOfRangeHorPosClassName = s_OutOfRangeHorClassName + "--pos";
        public static readonly string s_OutOfRangeHorNegClassName = s_OutOfRangeHorClassName + "--neg";
        public static readonly string s_OutOfRangeVerName = "out-of-range-ver";
        public static readonly string s_OutOfRangeVerClassName = s_UssClassName + "__out-of-range-ver";
        public static readonly string s_OutOfRangeVerPosClassName = s_OutOfRangeVerClassName + "--pos";
        public static readonly string s_OutOfRangeVerNegClassName = s_OutOfRangeVerClassName + "--neg";
        public static readonly string s_FocusRectName = "focus-rect";

        static readonly string s_ContainerName = "container";


        const float k_PresetStep = 50;
        const float k_Step = 0.5f;

        TransformOriginSelectorIndicator m_Indicator;
        VisualElement m_OutOfRangeVer;
        VisualElement m_OutOfRangeHor;
        VisualElement m_Container;
        float m_OriginX;
        float m_OriginY;

        public Action<float, float> pointSelected;

        public bool hasFocus => elementPanel != null && elementPanel.focusController.GetLeafFocusedElement() == this;

        public float originX
        {
            get => m_OriginX;
            set
            {
                if (Mathf.Approximately(m_OriginX, value))
                    return;
                m_OriginX = value;
                UpdateIndicator();
            }
        }

        public float originY
        {
            get => m_OriginY;
            set
            {
                if (Mathf.Approximately(m_OriginY, value))
                    return;
                m_OriginY = value;
                UpdateIndicator();
            }
        }

        public TransformOriginSelector()
        {
            AddToClassList(s_UssClassName);
            Add(new VisualElement { name = s_FocusRectName, pickingMode = PickingMode.Ignore });

            m_Container = new VisualElement { name = s_ContainerName };

            AddPoint(0, 0, "left top");
            AddPoint(k_Step, 0, "top");
            AddPoint(1, 0, "right top");
            AddPoint(1, k_Step, "right");
            AddPoint(1, 1, "right bottom");
            AddPoint(k_Step, 1, "bottom");
            AddPoint(0, 1, "left bottom");
            AddPoint(0, k_Step, "left");
            AddPoint(k_Step, k_Step, "Center");

            m_Indicator = new TransformOriginSelectorIndicator();
            m_OutOfRangeHor = new VisualElement
            {
                name = s_OutOfRangeHorName,
                pickingMode = PickingMode.Ignore
            };
            m_OutOfRangeHor.AddToClassList(s_OutOfRangeHorClassName);
            m_OutOfRangeVer = new VisualElement
            {
                name = s_OutOfRangeVerName,
                pickingMode = PickingMode.Ignore
            };
            m_OutOfRangeVer.AddToClassList(s_OutOfRangeVerClassName);

            m_Indicator.Add(m_OutOfRangeHor);
            m_Indicator.Add(m_OutOfRangeVer);

            m_Container.Add(m_Indicator);

            Add(m_Container);

            UpdateIndicator();

            focusable = true;

            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void UpdateIndicator()
        {
            m_OutOfRangeHor.RemoveFromClassList(s_OutOfRangeHorPosClassName);
            m_OutOfRangeHor.RemoveFromClassList(s_OutOfRangeHorNegClassName);
            m_OutOfRangeVer.RemoveFromClassList(s_OutOfRangeVerPosClassName);
            m_OutOfRangeVer.RemoveFromClassList(s_OutOfRangeVerNegClassName);
            m_Indicator.RemoveFromClassList(TransformOriginSelectorIndicator.s_IndicatorAtPresetClassName);

            if (float.IsNaN(originX) || float.IsNaN(originY))
            {
                m_Indicator.style.display = DisplayStyle.None;
                return;
            }

            m_Indicator.style.display = DisplayStyle.Flex;

            var x = (int)Math.Round(originX * 100);
            var y = (int)Math.Round(originY * 100);
            bool inRange = true;

            if (x > 100)
            {
                m_Indicator.x = 1;
                m_OutOfRangeHor.AddToClassList(s_OutOfRangeHorPosClassName);
                inRange = false;
            }
            else if (x < 0)
            {
                m_Indicator.x = 0;
                m_OutOfRangeHor.AddToClassList(s_OutOfRangeHorNegClassName);
                inRange = false;
            }
            else
            {
                m_Indicator.x = originX;
            }

            if (y > 100)
            {
                m_Indicator.y = 1;
                m_OutOfRangeVer.AddToClassList(s_OutOfRangeVerPosClassName);
                inRange = false;
            }
            else if (y < 0)
            {
                m_Indicator.y = 0;
                m_OutOfRangeVer.AddToClassList(s_OutOfRangeVerNegClassName);
                inRange = false;
            }
            else
            {
                m_Indicator.y = originY;
            }

            if (inRange && (x % k_PresetStep == 0) && (y % k_PresetStep == 0))
            {
                m_Indicator.AddToClassList(TransformOriginSelectorIndicator.s_IndicatorAtPresetClassName);
            }
        }

        void AddPoint(float x, float y, string tooltip)
        {
            var point = new TransformOriginSelectorPoint(OnPointClicked, tooltip)
            {
                x = x,
                y = y
            };

            m_Container.Add(point);
        }

        void OnPointClicked(float x, float y)
        {
            pointSelected?.Invoke(x, y);
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (IsNavigationKey(e.keyCode))
                Navigate(e.keyCode);
        }

        static bool IsNavigationKey(KeyCode keyCode)
        {
            return keyCode is KeyCode.LeftArrow or KeyCode.RightArrow or KeyCode.UpArrow or KeyCode.DownArrow;
        }

        void Navigate(KeyCode keyCode)
        {
            if (float.IsNaN(originX) || float.IsNaN(originY))
            {
                pointSelected(k_Step, k_Step);
            }
            else
            {
                float newX = Mathf.Clamp(originX, 0, 1);
                float newY = Mathf.Clamp(originY, 0, 1);

                switch (keyCode)
                {
                    case KeyCode.LeftArrow:
                        newX = GetNextPosition(originX, NavigationDirection.Backward);
                        break;
                    case KeyCode.RightArrow:
                        newX = GetNextPosition(originX, NavigationDirection.Forward);
                        break;
                    case KeyCode.UpArrow:
                        newY = GetNextPosition(originY, NavigationDirection.Backward);
                        break;
                    case KeyCode.DownArrow:
                        newY = GetNextPosition(originY, NavigationDirection.Forward);
                        break;
                }

                if (!Mathf.Approximately(newX, originX) || !Mathf.Approximately(newY, originY))
                    pointSelected(newX, newY);
            }
        }

        static float GetNextPosition(float value, NavigationDirection navigationDirection)
        {
            var newPos = value + ((int)navigationDirection * (k_Step / 2 + 0.0005f));
            float index = Mathf.Round(newPos / k_Step);
            return Mathf.Clamp(index * k_Step, 0, 1);
        }
    }
}
