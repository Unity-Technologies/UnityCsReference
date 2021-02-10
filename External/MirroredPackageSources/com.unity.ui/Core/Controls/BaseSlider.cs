using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is the direction of the <see cref="Slider"/> and <see cref="SliderInt"/>.
    /// </summary>
    public enum SliderDirection
    {
        /// <summary>
        /// An horizontal slider is made with a SliderDirection Horizontal.
        /// </summary>
        Horizontal,
        /// <summary>
        /// An vertical slider is made with a SliderDirection Vertical.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// This is a base class for the Slider fields.
    /// </summary>
    public abstract class BaseSlider<TValueType> : BaseField<TValueType>
        where TValueType : System.IComparable<TValueType>
    {
        internal VisualElement dragContainer { get; private set; }
        internal VisualElement dragElement { get; private set; }
        internal VisualElement dragBorderElement { get; private set; }
        internal TextField inputTextField { get; private set; }

        [SerializeField]
        private TValueType m_LowValue;

        /// <summary>
        /// This is the minimum value that the slider encodes.
        /// </summary>
        public TValueType lowValue
        {
            get { return m_LowValue; }
            set
            {
                if (!EqualityComparer<TValueType>.Default.Equals(m_LowValue, value))
                {
                    m_LowValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                    SaveViewData();
                }
            }
        }

        [SerializeField]
        private TValueType m_HighValue;

        /// <summary>
        /// This is the maximum value that the slider encodes.
        /// </summary>
        public TValueType highValue
        {
            get { return m_HighValue; }
            set
            {
                if (!EqualityComparer<TValueType>.Default.Equals(m_HighValue, value))
                {
                    m_HighValue = value;
                    ClampValue();
                    UpdateDragElementPosition();
                    SaveViewData();
                }
            }
        }

        /// <summary>
        /// This is the range from the minimum value to the maximum value of the slider.
        /// </summary>
        public TValueType range
        {
            get { return SliderRange(); }
        }

        private float m_PageSize;

        /// <summary>
        /// This is a generic page size used to change the value when clicking in the slider.
        /// </summary>
        public virtual float pageSize
        {
            get { return m_PageSize; }
            set { m_PageSize = value; }
        }

        private bool m_ShowInputField = false;

        public virtual bool showInputField
        {
            get { return m_ShowInputField; }
            set
            {
                if (m_ShowInputField != value)
                {
                    m_ShowInputField = value;
                    UpdateTextFieldVisibility();
                }
            }
        }

        internal bool clamped { get; set; } = true;

        internal ClampedDragger<TValueType> clampedDragger { get; private set; }
        Rect m_DragElementStartPos;

        TValueType Clamp(TValueType value, TValueType lowBound, TValueType highBound)
        {
            TValueType result = value;
            if (lowBound.CompareTo(value) > 0)
            {
                result = lowBound;
            }
            else if (highBound.CompareTo(value) < 0)
            {
                result = highBound;
            }

            return result;
        }

        private TValueType GetClampedValue(TValueType newValue)
        {
            TValueType lowest = lowValue, highest = highValue;
            if (lowest.CompareTo(highest) > 0)
            {
                var t = lowest;
                lowest = highest;
                highest = t;
            }

            return Clamp(newValue, lowest, highest);
        }

        /// <summary>
        /// The actual value of the slider.
        /// </summary>
        public override TValueType value
        {
            get { return base.value; }
            set
            {
                var newValue = clamped ? GetClampedValue(value) : value;
                base.value = newValue;
            }
        }

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            // Clamp the value around the real lowest and highest range values.
            var clampedValue = clamped ? GetClampedValue(newValue) : newValue;

            base.SetValueWithoutNotify(clampedValue);
            UpdateDragElementPosition();
            UpdateTextFieldValue();
        }

        private SliderDirection m_Direction;

        /// <summary>
        /// This is the actual property to contain the direction of the slider.
        /// </summary>
        public SliderDirection direction
        {
            get { return m_Direction; }
            set
            {
                m_Direction = value;
                if (m_Direction == SliderDirection.Horizontal)
                {
                    RemoveFromClassList(verticalVariantUssClassName);
                    AddToClassList(horizontalVariantUssClassName);
                }
                else
                {
                    RemoveFromClassList(horizontalVariantUssClassName);
                    AddToClassList(verticalVariantUssClassName);
                }
            }
        }

        private bool m_Inverted = false;

        /// <summary>
        /// This is indicating whether or not this slider is inverted.
        /// For an inverted horizontal slider, high value is located to the left, low value is located to the right
        /// For an inverted vertical slider, high value is located to the bottom, low value is located to the top.
        /// </summary>
        public bool inverted
        {
            get { return m_Inverted; }
            set
            {
                if (m_Inverted != value)
                {
                    m_Inverted = value;
                    UpdateDragElementPosition();
                }
            }
        }

        internal const float kDefaultPageSize = 0.0f;
        internal const bool kDefaultShowInputField = false;
        internal const bool kDefaultInverted = false;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-base-slider";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of elements of this type, when they are displayed horizontally.
        /// </summary>
        public static readonly string horizontalVariantUssClassName = ussClassName + "--horizontal";
        /// <summary>
        /// USS class name of elements of this type, when they are displayed vertically.
        /// </summary>
        public static readonly string verticalVariantUssClassName = ussClassName + "--vertical";
        /// <summary>
        /// USS class name of container elements in elements of this type.
        /// </summary>
        public static readonly string dragContainerUssClassName = ussClassName + "__drag-container";
        /// <summary>
        /// USS class name of tracker elements in elements of this type.
        /// </summary>
        public static readonly string trackerUssClassName = ussClassName + "__tracker";
        /// <summary>
        /// USS class name of dragger elements in elements of this type.
        /// </summary>
        public static readonly string draggerUssClassName = ussClassName + "__dragger";
        /// <summary>
        /// USS class name of the dragger border element in elements of this type.
        /// </summary>
        public static readonly string draggerBorderUssClassName = ussClassName + "__dragger-border";
        /// <summary>
        /// USS class name of the text field element in elements of this type.
        /// </summary>
        public static readonly string textFieldClassName = ussClassName + "__text-field";

        internal BaseSlider(string label, TValueType start, TValueType end, SliderDirection direction = SliderDirection.Horizontal, float pageSize = kDefaultPageSize)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            this.direction = direction;
            this.pageSize = pageSize;
            lowValue = start;
            highValue = end;
            pickingMode = PickingMode.Ignore;

            dragContainer = new VisualElement() { name = "unity-drag-container" };
            dragContainer.AddToClassList(dragContainerUssClassName);
            visualInput.Add(dragContainer);

            var trackElement = new VisualElement() { name = "unity-tracker" };
            trackElement.AddToClassList(trackerUssClassName);
            dragContainer.Add(trackElement);

            dragBorderElement = new VisualElement() { name = "unity-dragger-border" };
            dragBorderElement.AddToClassList(draggerBorderUssClassName);
            dragContainer.Add(dragBorderElement);

            dragElement = new VisualElement() { name = "unity-dragger" };
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);
            dragElement.AddToClassList(draggerUssClassName);
            dragContainer.Add(dragElement);

            clampedDragger = new ClampedDragger<TValueType>(this, SetSliderValueFromClick, SetSliderValueFromDrag);
            dragContainer.pickingMode = PickingMode.Position;
            dragContainer.AddManipulator(clampedDragger);

            RegisterCallback<KeyDownEvent>(OnKeyDown);

            UpdateTextFieldVisibility();
        }

        protected static float GetClosestPowerOfTen(float positiveNumber)
        {
            if (positiveNumber <= 0)
                return 1;
            return Mathf.Pow(10, Mathf.RoundToInt(Mathf.Log10(positiveNumber)));
        }

        protected static float RoundToMultipleOf(float value, float roundingValue)
        {
            if (roundingValue == 0)
                return value;
            return Mathf.Round(value / roundingValue) * roundingValue;
        }

        private void ClampValue()
        {
            // The property setter takes care of this
            value = rawValue;
        }

        internal abstract TValueType SliderLerpUnclamped(TValueType a, TValueType b, float interpolant);
        internal abstract float SliderNormalizeValue(TValueType currentValue, TValueType lowerValue, TValueType higherValue);
        internal abstract TValueType SliderRange();
        internal abstract TValueType ParseStringToValue(string stringValue);
        internal abstract void ComputeValueFromKey(SliderKey sliderKey, bool isShift);

        internal enum SliderKey
        {
            None,
            Lowest,
            LowerPage,
            Lower,
            Higher,
            HigherPage,
            Highest
        };

        // Calculates the value based on desired direction (inverted and vertical sliders)
        TValueType SliderLerpDirectionalUnclamped(TValueType a, TValueType b, float positionInterpolant)
        {
            // For vertical slider, the default should be bottom is lowValue, top is highValue, so we need
            // to invert the interpolant because it is based on element position (top-0, bottom-1)
            var directionalInterpolant = direction == SliderDirection.Vertical ? 1 - positionInterpolant : positionInterpolant;

            if (inverted)
            {
                return SliderLerpUnclamped(b, a, directionalInterpolant);
            }

            return SliderLerpUnclamped(a, b, directionalInterpolant);
        }

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.Free)
                return;

            var delta = clampedDragger.delta;

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromDrag(dragContainer.resolvedStyle.width, dragElement.resolvedStyle.width, m_DragElementStartPos.x + delta.x);
            else
                ComputeValueAndDirectionFromDrag(dragContainer.resolvedStyle.height, dragElement.resolvedStyle.height, m_DragElementStartPos.y + delta.y);
        }

        void ComputeValueAndDirectionFromDrag(float sliderLength, float dragElementLength, float dragElementPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos, totalRange)) / totalRange;
            value = SliderLerpDirectionalUnclamped(lowValue, highValue, normalizedDragElementPosition);
        }

        // Handles slider clicks and page scrolls
        void SetSliderValueFromClick()
        {
            if (clampedDragger.dragDirection == ClampedDragger<TValueType>.DragDirection.Free)
                return;

            if (clampedDragger.dragDirection == ClampedDragger<TValueType>.DragDirection.None)
            {
                if (Mathf.Approximately(pageSize, 0.0f))
                {
                    // Jump drag element to current mouse position when user clicks on slider and pageSize == 0
                    var x = (direction == SliderDirection.Horizontal)
                        ? clampedDragger.startMousePosition.x - (dragElement.resolvedStyle.width / 2f)
                        : dragElement.transform.position.x;
                    var y = (direction == SliderDirection.Horizontal) ? dragElement.transform.position.y : clampedDragger.startMousePosition.y - (dragElement.resolvedStyle.height / 2f);

                    var pos = new Vector3(x, y, 0);

                    dragElement.transform.position = pos;
                    dragBorderElement.transform.position = pos;
                    m_DragElementStartPos = new Rect(x, y, dragElement.resolvedStyle.width, dragElement.resolvedStyle.height);

                    // Manipulation becomes a free form drag
                    clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.Free;
                    if (direction == SliderDirection.Horizontal)
                        ComputeValueAndDirectionFromDrag(dragContainer.resolvedStyle.width, dragElement.resolvedStyle.width, m_DragElementStartPos.x);
                    else
                        ComputeValueAndDirectionFromDrag(dragContainer.resolvedStyle.height, dragElement.resolvedStyle.height, m_DragElementStartPos.y);
                    return;
                }

                m_DragElementStartPos = new Rect(dragElement.transform.position.x, dragElement.transform.position.y, dragElement.resolvedStyle.width, dragElement.resolvedStyle.height);
            }

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromClick(dragContainer.resolvedStyle.width, dragElement.resolvedStyle.width, dragElement.transform.position.x, clampedDragger.lastMousePosition.x);
            else
                ComputeValueAndDirectionFromClick(dragContainer.resolvedStyle.height, dragElement.resolvedStyle.height, dragElement.transform.position.y, clampedDragger.lastMousePosition.y);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            SliderKey sliderKey = SliderKey.None;
            bool isHorizontal = direction == SliderDirection.Horizontal;

            if (isHorizontal && evt.keyCode == KeyCode.Home || !isHorizontal && evt.keyCode == KeyCode.End)
                sliderKey = inverted ? SliderKey.Highest : SliderKey.Lowest;
            else if (isHorizontal && evt.keyCode == KeyCode.End || !isHorizontal && evt.keyCode == KeyCode.Home)
                sliderKey = inverted ? SliderKey.Lowest : SliderKey.Highest;
            else if (isHorizontal && evt.keyCode == KeyCode.PageUp || !isHorizontal && evt.keyCode == KeyCode.PageDown)
                sliderKey = inverted ? SliderKey.HigherPage : SliderKey.LowerPage;
            else if (isHorizontal && evt.keyCode == KeyCode.PageDown || !isHorizontal && evt.keyCode == KeyCode.PageUp)
                sliderKey = inverted ? SliderKey.LowerPage : SliderKey.HigherPage;
            else if (isHorizontal && evt.keyCode == KeyCode.LeftArrow || !isHorizontal && evt.keyCode == KeyCode.DownArrow)
                sliderKey = inverted ? SliderKey.Higher : SliderKey.Lower;
            else if (isHorizontal && evt.keyCode == KeyCode.RightArrow || !isHorizontal && evt.keyCode == KeyCode.UpArrow)
                sliderKey = inverted ? SliderKey.Lower : SliderKey.Higher;

            if (sliderKey == SliderKey.None)
                return;

            ComputeValueFromKey(sliderKey, evt.shiftKey);
            evt.StopPropagation();
        }

        internal virtual void ComputeValueAndDirectionFromClick(float sliderLength, float dragElementLength, float dragElementPos, float dragElementLastPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < Mathf.Epsilon)
                return;

            var isPositionDecreasing = dragElementLastPos < dragElementPos;
            var isPositionIncreasing = dragElementLastPos > (dragElementPos + dragElementLength);
            var isDraggingHighToLow = inverted ? isPositionIncreasing : isPositionDecreasing;
            var isDraggingLowToHigh = inverted ? isPositionDecreasing : isPositionIncreasing;
            var adjustedPageSize = inverted ? -pageSize : pageSize;

            if (isDraggingHighToLow && (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.LowToHigh))
            {
                clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.HighToLow;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos - adjustedPageSize, totalRange)) / totalRange;
                value = SliderLerpDirectionalUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
            else if (isDraggingLowToHigh && (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.HighToLow))
            {
                clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.LowToHigh;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos + adjustedPageSize, totalRange)) / totalRange;
                value = SliderLerpDirectionalUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
        }

        /// <summary>
        /// Method used to adjust the dragelement. Mainly used in a scroller.
        /// </summary>
        /// <param name="factor">The factor used to adjust the drag element, where a value > 1 will make it invisible.</param>
        public void AdjustDragElement(float factor)
        {
            // Any factor greater or equal to 1f eliminates the need for a drag element
            bool needsElement = factor < 1f;
            dragElement.visible = needsElement;

            if (needsElement)
            {
                IStyle inlineStyles = dragElement.style;
                dragElement.visible = visible; // Only visible if parent is as well.

                // Any factor smaller than 1f will necessitate a drag element
                if (direction == SliderDirection.Horizontal)
                {
                    // Make sure the minimum width of drag element is honoured
                    float elemMinWidth = resolvedStyle.minWidth == StyleKeyword.Auto ? 0 : resolvedStyle.minWidth.value;
                    inlineStyles.width = Mathf.Round(Mathf.Max(dragContainer.layout.width * factor, elemMinWidth));
                }
                else
                {
                    // Make sure the minimum height of drag element is honoured
                    float elemMinHeight = resolvedStyle.minHeight == StyleKeyword.Auto ? 0 : resolvedStyle.minHeight.value;
                    inlineStyles.height = Mathf.Round(Mathf.Max(dragContainer.layout.height * factor, elemMinHeight));
                }
            }

            dragBorderElement.visible = dragElement.visible;
        }

        void UpdateDragElementPosition(GeometryChangedEvent evt)
        {
            // Only affected by dimension changes
            if (evt.oldRect.size == evt.newRect.size)
            {
                return;
            }

            UpdateDragElementPosition();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            UpdateDragElementPosition();
        }

        bool SameValues(float a, float b, float epsilon)
        {
            return Mathf.Abs(b - a) < epsilon;
        }

        void UpdateDragElementPosition()
        {
            // UpdateDragElementPosition() might be called at times where we have no panel
            // we must skip the position calculation and wait for a layout pass
            if (panel == null)
                return;

            float normalizedPosition = SliderNormalizeValue(value, lowValue, highValue);
            float directionalNormalizedPosition = inverted ? 1f - normalizedPosition : normalizedPosition;
            float halfPixel = scaledPixelsPerPoint * 0.5f;

            if (direction == SliderDirection.Horizontal)
            {
                float dragElementWidth = dragElement.resolvedStyle.width;

                // This is the main calculation for the location of the thumbs / dragging element
                float offsetForThumbFullWidth = -dragElement.resolvedStyle.marginLeft - dragElement.resolvedStyle.marginRight;
                float totalWidth = dragContainer.layout.width - dragElementWidth + offsetForThumbFullWidth;
                float newLeft = directionalNormalizedPosition * totalWidth;

                if (float.IsNaN(newLeft)) //This can happen when layout is not computed yet
                    return;

                float currentLeft = dragElement.transform.position.x;

                if (!SameValues(currentLeft, newLeft, halfPixel))
                {
                    var newPos = new Vector3(newLeft, 0, 0);
                    dragElement.transform.position = newPos;
                    dragBorderElement.transform.position = newPos;
                }
            }
            else
            {
                float dragElementHeight = dragElement.resolvedStyle.height;

                float totalHeight = dragContainer.resolvedStyle.height - dragElementHeight;

                // Vertical scrollbar default starts from the bottom, so we invert the normalized position.
                float newTop = (1 - directionalNormalizedPosition) * totalHeight;

                if (float.IsNaN(newTop)) //This can happen when layout is not computed yet
                    return;

                float currentTop = dragElement.transform.position.y;
                if (!SameValues(currentTop, newTop, halfPixel))
                {
                    var newPos = new Vector3(0, newTop, 0);
                    dragElement.transform.position = newPos;
                    dragBorderElement.transform.position = newPos;
                }
            }
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                UpdateDragElementPosition((GeometryChangedEvent)evt);
            }
        }

        private void UpdateTextFieldVisibility()
        {
            if (showInputField)
            {
                if (inputTextField == null)
                {
                    inputTextField = new TextField() { name = "unity-text-field" };
                    inputTextField.AddToClassList(textFieldClassName);
                    inputTextField.RegisterValueChangedCallback(OnTextFieldValueChange);
                    inputTextField.RegisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
                    visualInput.Add(inputTextField);
                    UpdateTextFieldValue();
                }
            }
            else if (inputTextField != null && inputTextField.panel != null)
            {
                if (inputTextField.panel != null)
                    inputTextField.RemoveFromHierarchy();

                inputTextField.UnregisterValueChangedCallback(OnTextFieldValueChange);
                inputTextField.UnregisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
                inputTextField = null;
            }
        }

        private void UpdateTextFieldValue()
        {
            if (inputTextField == null)
                return;

            inputTextField.SetValueWithoutNotify(String.Format(CultureInfo.InvariantCulture, "{0:g7}", value));
        }

        private void OnTextFieldFocusOut(FocusOutEvent evt)
        {
            UpdateTextFieldValue();
        }

        void OnTextFieldValueChange(ChangeEvent<string> evt)
        {
            var newValue = GetClampedValue(ParseStringToValue(evt.newValue));
            if (!EqualityComparer<TValueType>.Default.Equals(newValue, value))
            {
                value = newValue;
                evt.StopPropagation();

                if (elementPanel != null)
                    OnViewDataReady();
            }
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                dragElement?.RemoveFromHierarchy();
            }
            else
            {
                visualInput.Add(dragElement);
            }
        }
    }
}
