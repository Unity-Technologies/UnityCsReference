// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Properties;

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
    public abstract class BaseSlider<TValueType> : BaseField<TValueType>, IValueField<TValueType>
        where TValueType : System.IComparable<TValueType>
    {
        internal static readonly BindingId lowValueProperty = nameof(lowValue);
        internal static readonly BindingId highValueProperty = nameof(highValue);
        internal static readonly BindingId rangeProperty = nameof(range);
        internal static readonly BindingId pageSizeProperty = nameof(pageSize);
        internal static readonly BindingId showInputFieldProperty = nameof(showInputField);
        internal static readonly BindingId directionProperty = nameof(direction);
        internal static readonly BindingId invertedProperty = nameof(inverted);
        internal static readonly BindingId fillProperty = nameof(fill);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : BaseField<TValueType>.UxmlSerializedData
        {
            public new static void Register()
            {
                BaseField<TValueType>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(fill), "fill"),
                });
            }

            #pragma warning disable 649
            [SerializeField] bool fill;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags fill_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (BaseSlider<TValueType>)obj;
                if (ShouldWriteAttributeValue(fill_UxmlAttributeFlags))
                    e.fill = fill;
            }
        }

        internal VisualElement dragContainer { get; private set; }
        internal VisualElement dragElement { get; private set; }
        internal VisualElement trackElement { get; private set; }
        internal VisualElement dragBorderElement { get; private set; }
        internal TextField inputTextField { get; private set; }
        internal VisualElement fillElement { get; private set; }

        private protected override bool canSwitchToMixedValue
        {
            get
            {
                if (inputTextField == null)
                    return true;

                return !inputTextField.textInputBase.textElement.hasFocus;
            }
        }

        float m_AdjustedPageSizeFromClick = 0;
        bool m_IsEditingTextField;
        bool m_Fill;

        [SerializeField, DontCreateProperty]
        private TValueType m_LowValue;

        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseField<TValueType>.UxmlTraits {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseSlider"/>.
        ///
        /// This class must be used instead of the non-generic inherited UxmlTraits equivalent.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a BaseSlider element that you can
        /// use in a UXML asset.
        /// </remarks>
        [Obsolete("UxmlTraits<TValueUxmlAttributeType> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public class UxmlTraits<TValueUxmlAttributeType> : BaseFieldTraits<TValueType, TValueUxmlAttributeType>
            where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }
        }

        /// <summary>
        /// This is the minimum value that the slider encodes.
        /// </summary>
        [CreateProperty]
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
                    NotifyPropertyChanged(lowValueProperty);
                }
            }
        }

        [SerializeField, DontCreateProperty]
        private TValueType m_HighValue;

        /// <summary>
        /// This is the maximum value that the slider encodes.
        /// </summary>
        [CreateProperty]
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
                    NotifyPropertyChanged(highValueProperty);
                }
            }
        }

        internal void SetHighValueWithoutNotify(TValueType newHighValue)
        {
            m_HighValue = newHighValue;
            var newValue = clamped ? GetClampedValue(value) : value;
            SetValueWithoutNotify(newValue);
            UpdateDragElementPosition();
            SaveViewData();
        }

        /// <summary>
        /// This is the range from the minimum value to the maximum value of the slider.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public TValueType range
        {
            get { return SliderRange(); }
        }

        private float m_PageSize;

        /// <summary>
        /// Represents the value that should be applied to the calculated scroll offset while scrolling the slider, such as when clicking within the track or clicking the slider arrows.
        /// </summary>
        [CreateProperty]
        public virtual float pageSize
        {
            get { return m_PageSize; }
            set
            {
                if (m_PageSize == value)
                    return;
                m_PageSize = value;
                NotifyPropertyChanged(pageSizeProperty);
            }
        }

        private bool m_ShowInputField = false;

        /// <summary>
        /// The visibility of the optional field inside the slider control.
        /// </summary>
        /// <remarks>
        /// Set this property to true to display a numerical text field that provides another way to
        /// edit the slider value.
        /// </remarks>
        [CreateProperty]
        public virtual bool showInputField
        {
            get { return m_ShowInputField; }
            set
            {
                if (m_ShowInputField != value)
                {
                    m_ShowInputField = value;
                    UpdateTextFieldVisibility();
                    NotifyPropertyChanged(showInputFieldProperty);
                }
            }
        }

        /// <summary>
        /// Enables fill to set the color and shape of a slider.
        /// </summary>
        [CreateProperty]
        public bool fill
        {
            get => m_Fill;
            set
            {
                if (m_Fill == value)
                    return;
                m_Fill = value;

                if (value)
                {
                    UpdateDragElementPosition();
                }
                else if (fillElement != null)
                {
                    fillElement.RemoveFromHierarchy();
                    fillElement = null;
                }

                NotifyPropertyChanged(fillProperty);
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

        /// <summary>
        /// Called when the user is dragging the label to update the value contained in the field.
        /// </summary>
        /// <param name="delta">Delta on the move.</param>
        /// <param name="speed">Speed of the move.</param>
        /// <param name="startValue">Starting value.</param>
        public virtual void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TValueType startValue) {}

        /// <summary>
        /// Method called by the application when the label of the field is started to be dragged to change the value of it.
        /// </summary>
        void IValueField<TValueType>.StartDragging() {}

        /// <summary>
        /// Method called by the application when the label of the field is stopped to be dragged to change the value of it.
        /// </summary>
        void IValueField<TValueType>.StopDragging() {}

        // If available, this gets called in SetValueWithoutNotify. The idea of having this is that in certain cases, we
        // have some sort of value dependency, e.g, for the case of ScrollOffset needs to have the same value as the
        // Slider's values.
        internal event Action<TValueType> onSetValueWithoutNotify;

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            // Clamp the value around the real lowest and highest range values.
            var clampedValue = clamped ? GetClampedValue(newValue) : newValue;

            base.SetValueWithoutNotify(clampedValue);
            onSetValueWithoutNotify?.Invoke(clampedValue);
            UpdateDragElementPosition();
            UpdateTextFieldValue();
        }

        private SliderDirection m_Direction;

        /// <summary>
        /// This is the actual property to contain the direction of the slider.
        /// </summary>
        [CreateProperty]
        public SliderDirection direction
        {
            get { return m_Direction; }
            set
            {
                var previous = m_Direction;

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
                if (previous != m_Direction)
                    NotifyPropertyChanged(directionProperty);
            }
        }

        private bool m_Inverted = false;

        /// <summary>
        /// This indicates whether or not this slider is inverted.
        /// For an inverted horizontal slider, high value is located to the left, low value is located to the right
        /// For an inverted vertical slider, high value is located to the bottom, low value is located to the top.
        /// </summary>
        [CreateProperty]
        public bool inverted
        {
            get { return m_Inverted; }
            set
            {
                if (m_Inverted != value)
                {
                    m_Inverted = value;
                    UpdateDragElementPosition();
                    NotifyPropertyChanged(invertedProperty);
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
        /// <summary>
        /// USS class name of fill element in elements of this type.
        /// </summary>
        public static readonly string fillUssClassName = ussClassName + "__fill";
        /// <summary>
        /// USS class name on the dragger that indicates it is currently controlled by <see cref="NavigationMoveEvent"/>.
        /// When the slider detects move events aligned with the slider's direction, it adjusts the slider's value.
        /// If it detects a navigation submit event, it removes the style, causing all navigation events to revert to their default behavior.
        /// A second navigation submit event re-applies the style to the dragger and restores the previous customized behavior.
        /// </summary>
        public static readonly string movableUssClassName = ussClassName + "--movable";

        internal const string k_FillElementName = "unity-fill";

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

            dragContainer = new VisualElement { name = "unity-drag-container" };
            dragContainer.AddToClassList(dragContainerUssClassName);
            dragContainer.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);
            visualInput.Add(dragContainer);

            trackElement = new VisualElement { name = "unity-tracker", usageHints = UsageHints.DynamicColor };
            trackElement.AddToClassList(trackerUssClassName);
            dragContainer.Add(trackElement);

            dragBorderElement = new VisualElement { name = "unity-dragger-border" };
            dragBorderElement.AddToClassList(draggerBorderUssClassName);
            dragContainer.Add(dragBorderElement);

            dragElement = new VisualElement { name = "unity-dragger", usageHints = UsageHints.DynamicTransform };
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);
            dragElement.AddToClassList(draggerUssClassName);
            dragContainer.Add(dragElement);

            clampedDragger = new ClampedDragger<TValueType>(this, SetSliderValueFromClick, SetSliderValueFromDrag);
            dragContainer.pickingMode = PickingMode.Position;
            dragContainer.AddManipulator(clampedDragger);

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);

            UpdateTextFieldVisibility();

            var mouseDragger = new FieldMouseDragger<TValueType>(this);
            mouseDragger.SetDragZone(labelElement);
            labelElement.AddToClassList(labelDraggerVariantUssClassName);
        }

        /// <undoc/>
        /// TODO why not make this stuff internal?
        protected internal static float GetClosestPowerOfTen(float positiveNumber)
        {
            if (positiveNumber <= 0)
                return 1;
            return Mathf.Pow(10, Mathf.RoundToInt(Mathf.Log10(positiveNumber)));
        }

        /// <undoc/>
        /// TODO why not make this stuff internal?
        protected internal static float RoundToMultipleOf(float value, float roundingValue)
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
        internal abstract TValueType ParseStringToValue(string previousValue, string newValue);
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
            if (Mathf.Abs(totalRange) < UIRUtility.k_Epsilon)
                return;

            float normalizedDragElementPosition;

            if (clamped)
                normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos, totalRange)) / totalRange;
            else
                normalizedDragElementPosition = dragElementPos / totalRange;

            var oldValue = value;
            value = SliderLerpDirectionalUnclamped(lowValue, highValue, normalizedDragElementPosition);

            // Even if the value remains unchanged, we need to update the position of the drag element to ensure
            // it's at a valid position (UUM-21303)
            if (EqualityComparer<TValueType>.Default.Equals(value, oldValue))
            {
                UpdateDragElementPosition();
            }
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
                    float x, y, sliderLength, dragElementLength, dragElementStartPos;

                    if (direction == SliderDirection.Horizontal)
                    {
                        sliderLength = dragContainer.resolvedStyle.width;
                        dragElementLength = dragElement.resolvedStyle.width;

                        var totalRange = sliderLength - dragElementLength;
                        var targetXPos = clampedDragger.startMousePosition.x - (dragElementLength / 2f);

                        x = Mathf.Max(0f, Mathf.Min(targetXPos, totalRange));
                        y = dragElement.resolvedStyle.translate.y;
                        dragElementStartPos = x;
                    }
                    else
                    {
                        sliderLength = dragContainer.resolvedStyle.height;
                        dragElementLength = dragElement.resolvedStyle.height;

                        var totalRange = sliderLength - dragElementLength;
                        var targetYPos = clampedDragger.startMousePosition.y - (dragElementLength / 2f);

                        x = dragElement.resolvedStyle.translate.x;
                        y = Mathf.Max(0f, Mathf.Min(targetYPos, totalRange));
                        dragElementStartPos = y;
                    }

                    var pos = new Vector3(x, y, 0);

                    dragElement.style.translate = pos;
                    dragBorderElement.style.translate = pos;
                    m_DragElementStartPos = new Rect(x, y, dragElement.resolvedStyle.width, dragElement.resolvedStyle.height);

                    // Manipulation becomes a free form drag
                    clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.Free;
                    ComputeValueAndDirectionFromDrag(sliderLength, dragElementLength, dragElementStartPos);
                    return;
                }

                m_DragElementStartPos = new Rect(dragElement.resolvedStyle.translate.x, dragElement.resolvedStyle.translate.y, dragElement.resolvedStyle.width, dragElement.resolvedStyle.height);
            }

            if (direction == SliderDirection.Horizontal)
                ComputeValueAndDirectionFromClick(dragContainer.resolvedStyle.width, dragElement.resolvedStyle.width, dragElement.resolvedStyle.translate.x, clampedDragger.lastMousePosition.x);
            else
                ComputeValueAndDirectionFromClick(dragContainer.resolvedStyle.height, dragElement.resolvedStyle.height, dragElement.resolvedStyle.translate.y, clampedDragger.lastMousePosition.y);

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

            if (sliderKey == SliderKey.None)
                return;

            ComputeValueFromKey(sliderKey, evt.shiftKey);
            evt.StopPropagation();
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (!dragElement.ClassListContains(movableUssClassName))
                return;

            SliderKey sliderKey = SliderKey.None;
            bool isHorizontal = direction == SliderDirection.Horizontal;

            if (evt.direction == (isHorizontal ? NavigationMoveEvent.Direction.Left : NavigationMoveEvent.Direction.Down))
                sliderKey = inverted ? SliderKey.Higher : SliderKey.Lower;
            else if (evt.direction == (isHorizontal ? NavigationMoveEvent.Direction.Right : NavigationMoveEvent.Direction.Up))
                sliderKey = inverted ? SliderKey.Lower : SliderKey.Higher;

            if (sliderKey == SliderKey.None)
                return;

            ComputeValueFromKey(sliderKey, evt.shiftKey);
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (m_IsEditingTextField)
                return;
            dragElement.EnableInClassList(movableUssClassName, !dragElement.ClassListContains(movableUssClassName));
        }

        internal virtual void ComputeValueAndDirectionFromClick(float sliderLength, float dragElementLength, float dragElementPos, float dragElementLastPos)
        {
            var totalRange = sliderLength - dragElementLength;
            if (Mathf.Abs(totalRange) < UIRUtility.k_Epsilon)
                return;

            var isPositionDecreasing = dragElementLastPos < dragElementPos;
            var isPositionIncreasing = dragElementLastPos > (dragElementPos + dragElementLength);
            var isDraggingHighToLow = inverted ? isPositionIncreasing : isPositionDecreasing;
            var isDraggingLowToHigh = inverted ? isPositionDecreasing : isPositionIncreasing;

            // We maintain a cumulative page size value to handle scenarios where the page size is too small to produce noticeable movement with a single click (UUM-86425).
            m_AdjustedPageSizeFromClick = inverted ? m_AdjustedPageSizeFromClick - pageSize : m_AdjustedPageSizeFromClick + pageSize;

            if (isDraggingHighToLow && (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.LowToHigh))
            {
                clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.HighToLow;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos - m_AdjustedPageSizeFromClick, totalRange)) / totalRange;
                value = SliderLerpDirectionalUnclamped(lowValue, highValue, normalizedDragElementPosition);
            }
            else if (isDraggingLowToHigh && (clampedDragger.dragDirection != ClampedDragger<TValueType>.DragDirection.HighToLow))
            {
                clampedDragger.dragDirection = ClampedDragger<TValueType>.DragDirection.LowToHigh;
                float normalizedDragElementPosition = Mathf.Max(0f, Mathf.Min(dragElementPos + m_AdjustedPageSizeFromClick, totalRange)) / totalRange;
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
            if (needsElement)
            {
                // Only visible if parent is as well.
                dragElement.style.visibility = new StyleEnum<Visibility>(Visibility.Visible, StyleKeyword.Null);

                IStyle inlineStyles = dragElement.style;

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
            else
            {
                dragElement.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden, StyleKeyword.Undefined);
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

                float currentLeft = dragElement.resolvedStyle.translate.x;

                if (!SameValues(currentLeft, newLeft, halfPixel))
                {
                    var newPos = new Vector3(newLeft, 0, 0);
                    dragElement.style.translate = newPos;
                    dragBorderElement.style.translate = newPos;
                    
                    // When the dragElement moves we can reset our cumlative page size
                    m_AdjustedPageSizeFromClick = 0;
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

                float currentTop = dragElement.resolvedStyle.translate.y;
                if (!SameValues(currentTop, newTop, halfPixel))
                {
                    var newPos = new Vector3(0, newTop, 0);
                    dragElement.style.translate = newPos;
                    dragBorderElement.style.translate = newPos;
                    
                    // When the dragElement moves we can reset our cumlative page size
                    m_AdjustedPageSizeFromClick = 0;
                }
            }

            UpdateFill(normalizedPosition);
        }

        void UpdateFill(float normalizedValue)
        {
            if (!fill)
                return;

            if (fillElement == null)
            {
                fillElement = new VisualElement { name = k_FillElementName, usageHints = UsageHints.DynamicColor };
                fillElement.AddToClassList(fillUssClassName);
                trackElement.Add(fillElement);
            }

            float inverseNormalizedValue = 1.0f - normalizedValue;
            var valuePercent = Length.Percent(inverseNormalizedValue * 100.0f);
            if (direction == SliderDirection.Vertical)
            {
                fillElement.style.right = 0;
                fillElement.style.left = 0;
                fillElement.style.bottom = inverted ? valuePercent : 0;
                fillElement.style.top = inverted ? 0 : valuePercent;
            }
            else
            {
                fillElement.style.top = 0;
                fillElement.style.bottom = 0;
                fillElement.style.left = inverted ? valuePercent : 0;
                fillElement.style.right = inverted ? 0 : valuePercent;
            }
        }

        [EventInterest(typeof(GeometryChangedEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                UpdateDragElementPosition((GeometryChangedEvent)evt);
            }
        }

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("ExecuteDefaultAction override has been removed because default event handling was migrated to HandleEventBubbleUp. Please use HandleEventBubbleUp.", false)]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
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
                    inputTextField.RegisterCallback<FocusInEvent>(OnTextFieldFocusIn);
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
                inputTextField.UnregisterCallback<FocusInEvent>(OnTextFieldFocusIn);
                inputTextField.UnregisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
                inputTextField = null;
            }
        }

        private void UpdateTextFieldValue()
        {
            if (inputTextField == null || m_IsEditingTextField)
                return;

            inputTextField.SetValueWithoutNotify(String.Format(CultureInfo.InvariantCulture, "{0:g7}", value));
        }

        void OnFocusIn(FocusInEvent evt) => dragElement.AddToClassList(movableUssClassName);

        void OnFocusOut(FocusOutEvent evt) => dragElement.RemoveFromClassList(movableUssClassName);

        private void OnTextFieldFocusIn(FocusInEvent evt)
        {
            m_IsEditingTextField = true;
        }

        private void OnTextFieldFocusOut(FocusOutEvent evt)
        {
            m_IsEditingTextField = false;
            UpdateTextFieldValue();
        }

        void OnTextFieldValueChange(ChangeEvent<string> evt)
        {
            var newValue = GetClampedValue(ParseStringToValue(evt.previousValue, evt.newValue));
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
                if (inputTextField != null)
                {
                    inputTextField.showMixedValue = true;
                }
            }
            else
            {
                dragContainer.Add(dragElement);
                if (inputTextField != null)
                {
                    inputTextField.showMixedValue = false;
                }
            }
        }

        internal override void RegisterEditingCallbacks()
        {
            labelElement.RegisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);
            dragContainer.RegisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);

            dragContainer.RegisterCallback<PointerUpEvent>(EndEditing);
        }

        internal override void UnregisterEditingCallbacks()
        {
            labelElement.UnregisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);
            dragContainer.UnregisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);

            dragContainer.UnregisterCallback<PointerUpEvent>(EndEditing);
        }
    }
}
