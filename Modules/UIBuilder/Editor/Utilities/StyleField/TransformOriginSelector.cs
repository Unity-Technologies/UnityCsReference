// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    abstract class TransformOriginSelectorPointBase : VisualElement
    {
        static readonly string s_UssClassName = "unity-transform-origin-selector__point-base";
        static readonly string s_VisualContentName = "visual-content";

        float m_X = 0f;
        float m_Y = 0f;

        public float x
        {
            get
            {
                return m_X;
            }
            set
            {
                m_X = value;
                style.left = new Length(m_X * 100, LengthUnit.Percent);
            }
        }

        public float y
        {
            get
            {
                return m_Y;
            }
            set
            {
                m_Y = value;
                style.top = new Length(m_Y * 100, LengthUnit.Percent);
            }
        }

        protected TransformOriginSelectorPointBase(string visualContentUXMLFile = null)
        {
            AddToClassList(s_UssClassName);
            style.position = Position.Absolute;
            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(visualContentUXMLFile);
            VisualElement visualContent = template.Instantiate();

            visualContent.pickingMode = PickingMode.Ignore;

            Add(visualContent);
            visualContent.name = s_VisualContentName;
        }
    }

    class TransformOriginSelectorPoint : TransformOriginSelectorPointBase
    {
        static readonly string s_UssClassName = "unity-transform-origin-selector__point";
        internal static readonly string s_ClickableAreaName = "clickable-area";

        public TransformOriginSelectorPoint(Action<float, float> clicked, string tooltip) : base(BuilderConstants.UtilitiesPath + "/StyleField/TransformOriginSelectorPoint.uxml")
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

        public TransformOriginSelectorIndicator() : base(BuilderConstants.UtilitiesPath + "/StyleField/TransformOriginSelectorIndicator.uxml")
        {
            AddToClassList(s_UssClassName);
            pickingMode = PickingMode.Ignore;
            this.Q("shape").pickingMode = PickingMode.Ignore;
        }
    }

    [UsedImplicitly]
    class TransformOriginSelector : VisualElement
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

        private TransformOriginSelectorIndicator m_Indicator;
        private VisualElement m_OutOfRangeVer;
        private VisualElement m_OutOfRangeHor;
        private VisualElement m_Container;
        float m_OriginX = 0;
        float m_OriginY = 0;

        public Action<float, float> pointSelected;

        public bool hasFocus
        {
            get { return elementPanel != null && elementPanel.focusController.GetLeafFocusedElement() == this; }
        }

        public float originX
        {
            get => m_OriginX;
            set {
                if (m_OriginX == value)
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
                if (m_OriginY == value)
                    return;
                m_OriginY = value;
                UpdateIndicator();
            }
        }

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new TransformOriginSelector();
        }

        public TransformOriginSelector()
        {
            AddToClassList(s_UssClassName);
            Add(new VisualElement { name = s_FocusRectName, pickingMode = PickingMode.Ignore });

            m_Container = new VisualElement();
            m_Container.name = s_ContainerName;

            AddPoint(0, 0, "left top");
            AddPoint(0.5f, 0, "top");
            AddPoint(1, 0, "right top");
            AddPoint(1, 0.5f, "right");
            AddPoint(1, 1, "right bottom");
            AddPoint(0.5f, 1, "bottom");
            AddPoint(0, 1, "left bottom");
            AddPoint(0, 0.5f, "left");
            AddPoint(0.5f, 0.5f, "Center");

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
            else
            {
                m_Indicator.style.display = DisplayStyle.Flex;
            }

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

            // if the indicator is at a preset value: 0%, 50%, 100% then highlight it
            if (inRange && (x % 50 == 0) && (y % 50 == 0))
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
            {
                Navigate(e.keyCode);
            }
        }

        static bool IsNavigationKey(KeyCode keyCode)
        {
            return keyCode == KeyCode.LeftArrow ||
                keyCode == KeyCode.RightArrow ||
                keyCode == KeyCode.UpArrow ||
                keyCode == KeyCode.DownArrow;
        }

        void Navigate(KeyCode keyCode)
        {
            // If the position were undefined (values in px) then move to the center
            if (float.IsNaN(originX) || float.IsNaN(originY))
            {
                pointSelected(0.5f, 0.5f);
            }
            else
            {
                // Ensure that to clamp the new postion in case the current position is out of bounds
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
                    default:
                        break;
                }

                if (!Mathf.Approximately(newX, originX) || !Mathf.Approximately(newY, originY))
                {
                    pointSelected(newX, newY);
                }
            }
        }

        static float GetNextPosition(float value, NavigationDirection navigationDirection)
        {
            const float k_Step = 0.5f;

            var newPos = value + ((int)navigationDirection * (k_Step / 2 + 0.0005f));
            float index = Mathf.Round(newPos / k_Step);
            return Mathf.Clamp(index * k_Step, 0, 1);
        }
    }
}
