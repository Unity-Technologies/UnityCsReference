// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A min/max slider containing a representation of a range. For more information, refer to [[wiki:UIE-uxml-element-MinMaxSlider|UXML element MinMaxSlider]].
    /// </summary>
    /// <remarks>
    /// The MinMaxSlider manages navigation events in a customized manner.
    /// The MinMaxSlider has four navigation states:
    ///
    ///- Min thumb: adjusts the slider's minimum value if the <see cref="NavigationMoveEvent"/> aligns with the slider's direction.
    ///- Max thumb: adjusts the slider's maximum value if the <see cref="NavigationMoveEvent"/> aligns with the slider's direction.
    ///- Middle thumb: adjusts the slider's minimum and maximum values if the <see cref="NavigationMoveEvent"/> aligns with the slider's direction.
    ///- Default navigation: <see cref="NavigationMoveEvent"/> are ignored and handled by the default systems.
    ///
    /// The MinMaxSlider cycles through the navigation states when a <see cref="NavigationSubmitEvent"/> is received.
    /// </remarks>
    /// <remarks>
    /// SA: [[Slider]]
    /// </remarks>
    public class MinMaxSlider : BaseField<Vector2>
    {
        internal static readonly BindingId minValueProperty = nameof(minValue);
        internal static readonly BindingId maxValueProperty = nameof(maxValue);
        internal static readonly BindingId rangeProperty = nameof(range);
        internal static readonly BindingId lowLimitProperty = nameof(lowLimit);
        internal static readonly BindingId highLimitProperty = nameof(highLimit);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Vector2>.UxmlSerializedData, IUxmlSerializedDataCustomAttributeHandler
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Vector2>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(lowLimit), "low-limit"),
                    new (nameof(highLimit), "high-limit"),
                });
            }

            #pragma warning disable 649
            [SerializeField] float lowLimit;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags lowLimit_UxmlAttributeFlags;
            [SerializeField] float highLimit;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags highLimit_UxmlAttributeFlags;
            #pragma warning restore 649

            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes)
            {
                int foundAttributeCounter = 0;
                var minV = UxmlUtility.TryParseFloatAttribute("min-value", bag, ref foundAttributeCounter);
                var maxV = UxmlUtility.TryParseFloatAttribute("max-value", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    Value = new Vector2(minV, maxV);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("min-value");
                        uxmlAsset.RemoveAttribute("max-value");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(Value));
                    }
                }
            }

            public override object CreateInstance() => new MinMaxSlider();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (MinMaxSlider)obj;
                if (ShouldWriteAttributeValue(lowLimit_UxmlAttributeFlags))
                    e.lowLimit = lowLimit;
                if (ShouldWriteAttributeValue(highLimit_UxmlAttributeFlags))
                    e.highLimit = highLimit;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="MinMaxSlider"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<MinMaxSlider, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MinMaxSlider"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseField<Vector2>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_MinValue = new UxmlFloatAttributeDescription { name = "min-value", defaultValue = 0 };
            UxmlFloatAttributeDescription m_MaxValue = new UxmlFloatAttributeDescription { name = "max-value", defaultValue = kDefaultHighValue };
            UxmlFloatAttributeDescription m_LowLimit = new UxmlFloatAttributeDescription { name = "low-limit", defaultValue = float.MinValue };
            UxmlFloatAttributeDescription m_HighLimit = new UxmlFloatAttributeDescription { name = "high-limit", defaultValue = float.MaxValue };

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }

            /// <summary>
            /// Initialize <see cref="MinMaxSlider"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The element to initialize.</param>
            /// <param name="bag">The bag of attributes.</param>
            /// <param name="cc">Creation Context, unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var slider = ((MinMaxSlider)ve);
                slider.lowLimit = m_LowLimit.GetValueFromBag(bag, cc);
                slider.highLimit = m_HighLimit.GetValueFromBag(bag, cc);

                var value = new Vector2(m_MinValue.GetValueFromBag(bag, cc), m_MaxValue.GetValueFromBag(bag, cc));
                slider.value = value;
            }
        }

        // The order of this enum controls the Navigation mode switching
        private enum DragState
        {
            MinThumb,
            MaxThumb,
            MiddleThumb,
            NoThumb,
        }

        internal VisualElement dragElement { get; private set; }
        internal VisualElement dragMinThumb { get; private set; }
        internal VisualElement dragMaxThumb { get; private set; }
        internal ClampedDragger<float> clampedDragger { get; private set; }

        // For dragging purpose
        Vector2 m_DragElementStartPos;
        Vector2 m_ValueStartPos;
        DragState m_DragState;

        // Minimum value of the current position of the slider
        /// <summary>
        /// This is the low value of the range represented on the slider.
        /// </summary>
        [CreateProperty]
        public float minValue
        {
            get { return value.x; }
            set
            {
                var previous = minValue;
                base.value = ClampValues(new Vector2(value, rawValue.y));

                if (!Mathf.Approximately(previous, minValue))
                    NotifyPropertyChanged(minValueProperty);
            }
        }

        // Maximum value of the current position of the slider
        /// <summary>
        /// This is the high value of the range represented on the slider.
        /// </summary>
        [CreateProperty]
        public float maxValue
        {
            get { return value.y; }
            set
            {
                var previous = maxValue;
                base.value = ClampValues(new Vector2(rawValue.x, value));

                if (!Mathf.Approximately(previous, maxValue))
                    NotifyPropertyChanged(maxValueProperty);
            }
        }

        // Complete value of the slider position, where X is the minimum, and Y is the maximum in a Vector2
        /// <summary>
        /// This is the value of the slider. This is a <see cref="Vector2"/> where the x is the lower bound and the y is the higher bound.
        /// </summary>
        public override Vector2 value
        {
            get { return base.value; }
            set
            {
                base.value = ClampValues(value);
            }
        }

        public override void SetValueWithoutNotify(Vector2 newValue)
        {
            base.SetValueWithoutNotify(ClampValues(newValue));
            UpdateDragElementPosition();
        }

        // The complete range that the value could span on, from the minimum to the maximum limit.
        /// <summary>
        /// Returns the range of the low/high limits of the slider.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public float range
        {
            get { return Math.Abs(highLimit - lowLimit); }
        }

        float m_MinLimit;
        float m_MaxLimit;

        // This is the low limit that the slider can slide to.
        /// <summary>
        /// This is the low limit of the slider.
        /// </summary>
        [CreateProperty]
        public float lowLimit
        {
            get { return m_MinLimit; }
            set
            {
                if (!Mathf.Approximately(m_MinLimit, value))
                {
                    if (value > m_MaxLimit)
                    {
                        throw new ArgumentException("lowLimit is greater than highLimit");
                    }

                    m_MinLimit = value;
                    this.value = rawValue;
                    UpdateDragElementPosition();

                    if (!string.IsNullOrEmpty(viewDataKey))
                        SaveViewData();
                    NotifyPropertyChanged(lowLimitProperty);
                }
            }
        }

        // This is the high limit that the slider can slide to.
        /// <summary>
        /// This is the high limit of the slider.
        /// </summary>
        [CreateProperty]
        public float highLimit
        {
            get { return m_MaxLimit; }
            set
            {
                if (!Mathf.Approximately(m_MaxLimit, value))
                {
                    if (value < m_MinLimit)
                    {
                        throw new ArgumentException("highLimit is smaller than lowLimit");
                    }

                    m_MaxLimit = value;
                    this.value = rawValue;
                    UpdateDragElementPosition();

                    if (!string.IsNullOrEmpty(viewDataKey))
                        SaveViewData();
                    NotifyPropertyChanged(highLimitProperty);
                }
            }
        }

        internal const float kDefaultHighValue = 10;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-min-max-slider";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of tracker elements in elements of this type.
        /// </summary>
        public static readonly string trackerUssClassName = ussClassName + "__tracker";
        /// <summary>
        /// USS class name of dragger elements in elements of this type.
        /// </summary>
        public static readonly string draggerUssClassName = ussClassName + "__dragger";
        /// <summary>
        /// USS class name of the minimum thumb elements in elements of this type.
        /// </summary>
        public static readonly string minThumbUssClassName = ussClassName + "__min-thumb";
        /// <summary>
        /// USS class name of the maximum thumb elements in elements of this type.
        /// </summary>
        public static readonly string maxThumbUssClassName = ussClassName + "__max-thumb";
        /// <summary>
        /// USS class name of the element that is currently controlled by <see cref="NavigationMoveEvent"/>.
        /// When a <see cref="NavigationSubmitEvent"/> is received the slider cycles through the elements in the following order:
        ///1. Minimum thumb.
        ///2. Maximum thumb.
        ///3. Middle bar.
        ///4. None (Default Navigation behavior).
        /// </summary>
        public static readonly string movableUssClassName = ussClassName + "--movable";

        /// <summary>
        /// Constructor.
        /// </summary>
        public MinMaxSlider()
            : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minValue">The minimum value in the range to be represented.</param>
        /// <param name="maxValue">The maximum value in the range to be represented.</param>
        /// <param name="minLimit">The minimum value of the slider limit.</param>
        /// <param name="maxLimit">The maximum value of the slider limit.</param>
        public MinMaxSlider(float minValue, float maxValue, float minLimit, float maxLimit)
            : this(null, minValue, maxValue, minLimit, maxLimit) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minValue">The minimum value in the range to be represented.</param>
        /// <param name="maxValue">The maximum value in the range to be represented.</param>
        /// <param name="minLimit">The minimum value of the slider limit.</param>
        /// <param name="maxLimit">The maximum value of the slider limit.</param>
        public MinMaxSlider(string label, float minValue = 0, float maxValue = kDefaultHighValue, float minLimit = float.MinValue, float maxLimit = float.MaxValue)
            : base(label, null)
        {
            m_MinLimit = float.MinValue;
            m_MaxLimit = float.MaxValue;

            lowLimit = minLimit;
            highLimit = maxLimit;

            // Can't set to value here, because it could be overriden in a derived type.
            var clampedValue = ClampValues(new Vector2(minValue, maxValue));
            this.minValue = clampedValue.x;
            this.maxValue = clampedValue.y;
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            pickingMode = PickingMode.Ignore;

            m_DragState = DragState.NoThumb;

            visualInput.pickingMode = PickingMode.Position;
            var trackElement = new VisualElement() { name = "unity-tracker" };
            trackElement.AddToClassList(trackerUssClassName);
            visualInput.Add(trackElement);

            dragElement = new VisualElement() { name = "unity-dragger" };
            dragElement.AddToClassList(draggerUssClassName);
            dragElement.RegisterCallback<GeometryChangedEvent>(UpdateDragElementPosition);
            visualInput.Add(dragElement);

            // For a better handling of the cursor style, children elements are created so that the style is automatic with the uss.
            dragMinThumb = new VisualElement() { name = "unity-thumb-min" };
            dragMaxThumb = new VisualElement() { name = "unity-thumb-max" };
            dragMinThumb.AddToClassList(minThumbUssClassName);
            dragMaxThumb.AddToClassList(maxThumbUssClassName);
            dragElement.Add(dragMinThumb);
            dragElement.Add(dragMaxThumb);

            clampedDragger = new ClampedDragger<float>(null, SetSliderValueFromClick, SetSliderValueFromDrag);
            visualInput.AddManipulator(clampedDragger);

            m_MinLimit = minLimit;
            m_MaxLimit = maxLimit;
            rawValue = ClampValues(new Vector2(minValue, maxValue));
            UpdateDragElementPosition();

            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<BlurEvent>(OnBlur);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
        }

        // Clamp the actual parameter inside the low / high limit
        Vector2 ClampValues(Vector2 valueToClamp)
        {
            // Make sure the limits are ok...
            if (m_MinLimit > m_MaxLimit)
            {
                m_MinLimit = m_MaxLimit;
            }

            Vector2 clampedValue = new Vector2();

            // Make sure the value max is not bigger than the max limit...
            if (valueToClamp.y > m_MaxLimit)
            {
                valueToClamp.y = m_MaxLimit;
            }

            // Clamp both values
            clampedValue.x = Mathf.Clamp(valueToClamp.x, m_MinLimit, valueToClamp.y);
            clampedValue.y = Mathf.Clamp(valueToClamp.y, valueToClamp.x, m_MaxLimit);
            return clampedValue;
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

        void UpdateDragElementPosition()
        {
            // UpdateDragElementPosition() might be called at times where we have no panel
            // we must skip the position calculation and wait for a layout pass
            if (panel == null)
                return;

            // The slider border and marging are always shown
            var sliderLeftBorder = dragElement.resolvedStyle.borderLeftWidth + dragElement.resolvedStyle.marginLeft;
            var sliderRightBorder = dragElement.resolvedStyle.borderRightWidth + dragElement.resolvedStyle.marginRight;
            var sliderMinWidth = sliderRightBorder + sliderLeftBorder;

            var thumbFullWidth = dragMinThumb.resolvedStyle.width + dragMaxThumb.resolvedStyle.width + sliderMinWidth;
            var newPositionLeft = Mathf.Round(SliderLerpUnclamped(dragMinThumb.resolvedStyle.width, visualInput.layout.width - dragMaxThumb.resolvedStyle.width - sliderMinWidth, SliderNormalizeValue(minValue, lowLimit, highLimit)));
            var newPositionRight = Mathf.Round(SliderLerpUnclamped(dragMinThumb.resolvedStyle.width + sliderMinWidth, visualInput.layout.width - dragMaxThumb.resolvedStyle.width, SliderNormalizeValue(maxValue, lowLimit, highLimit)));

            dragElement.style.width = newPositionRight - newPositionLeft;
            dragElement.style.left = newPositionLeft;
            dragMinThumb.style.left = -dragMinThumb.resolvedStyle.width - sliderLeftBorder;
            dragMaxThumb.style.right = -dragMaxThumb.resolvedStyle.width - sliderRightBorder;
        }

        internal float SliderLerpUnclamped(float a, float b, float interpolant)
        {
            return Mathf.LerpUnclamped(a, b, interpolant);
        }

        internal float SliderNormalizeValue(float currentValue, float lowerValue, float higherValue)
        {
            return (currentValue - lowerValue) / (higherValue - lowerValue);
        }

        float ComputeValueFromPosition(float positionToConvert)
        {
            var interpolant = SliderNormalizeValue(positionToConvert, 0, visualInput.layout.width);
            return SliderLerpUnclamped(lowLimit, highLimit, interpolant);
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

        DragState GetNavigationState()
        {
            var minEnabled = dragMinThumb.ClassListContains(movableUssClassName);
            var maxEnabled = dragMaxThumb.ClassListContains(movableUssClassName);

            if (minEnabled)
                return maxEnabled ? DragState.MiddleThumb : DragState.MinThumb;
            else if (maxEnabled)
                return DragState.MaxThumb;
            return DragState.NoThumb;
        }

        void SetNavigationState(DragState newState)
        {
            dragMinThumb.EnableInClassList(movableUssClassName, newState == DragState.MinThumb || newState == DragState.MiddleThumb);
            dragMaxThumb.EnableInClassList(movableUssClassName, newState == DragState.MaxThumb || newState == DragState.MiddleThumb);
            dragElement.EnableInClassList(movableUssClassName, newState == DragState.MiddleThumb);
        }

        void OnFocusIn(FocusInEvent evt)
        {
            // Leave the navigation state unless it was no thumb.
            // This allows for setting the state in SetSliderValueFromClick when clicking on the slider.
            if (GetNavigationState() == DragState.NoThumb)
                SetNavigationState(DragState.MinThumb);
        }

        void OnBlur(BlurEvent evt) => SetNavigationState(DragState.NoThumb);

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            var newState = GetNavigationState() + 1;
            if (newState > DragState.NoThumb)
                newState = DragState.MinThumb;
            SetNavigationState(newState);
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            var moveState = GetNavigationState();
            if (moveState == DragState.NoThumb)
                return;

            if (evt.direction != NavigationMoveEvent.Direction.Left && evt.direction != NavigationMoveEvent.Direction.Right)
                return;

            ComputeValueFromKey(evt.direction == NavigationMoveEvent.Direction.Left, evt.shiftKey, moveState);
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }

        void ComputeValueFromKey(bool leftDirection, bool isShift, DragState moveState)
        {
            // Change by approximately 1/100 of entire range, or 1/10 if holding down shift
            // But round to nearest power of ten to get nice resulting numbers.
            var delta = Slider.GetClosestPowerOfTen(Mathf.Abs((highLimit - lowLimit) * 0.01f));
            if (isShift)
                delta *= 10;

            // Increment or decrement by just over half the delta.
            // This means that e.g. if delta is 1, incrementing from 1.0 will go to 2.0,
            // but incrementing from 0.9 is going to 1.0 rather than 2.0.
            // This feels more right since 1.0 is the "next" one.
            if (leftDirection)
                delta = -delta;

            // Clamp to prevent the thumb going into the other thumbs value.
            switch (moveState)
            {
                case DragState.MinThumb:
                {
                    var thumbValue = Slider.RoundToMultipleOf(value.x + (delta * 0.5001f), Mathf.Abs(delta));
                    thumbValue = Math.Clamp(thumbValue, lowLimit, value.y);
                    value = new Vector2(thumbValue, value.y);
                    break;
                }
                case DragState.MaxThumb:
                {
                    var thumbValue = Slider.RoundToMultipleOf(value.y + (delta * 0.5001f), Mathf.Abs(delta));
                    thumbValue = Math.Clamp(thumbValue, value.x, highLimit);
                    value = new Vector2(value.x, thumbValue);
                    break;
                }
                case DragState.MiddleThumb:
                {
                    var range = value.y - value.x;
                    if (delta > 0)
                    {
                        var thumbValue = Slider.RoundToMultipleOf(value.y + (delta * 0.5001f), Mathf.Abs(delta));
                        thumbValue = Math.Clamp(thumbValue, value.x, highLimit);
                        value = new Vector2(thumbValue - range, thumbValue);
                    }
                    else
                    {
                        var thumbValue = Slider.RoundToMultipleOf(value.x + (delta * 0.5001f), Mathf.Abs(delta));
                        thumbValue = Math.Clamp(thumbValue, lowLimit, value.y);
                        value = new Vector2(thumbValue, thumbValue + range);
                    }

                    break;
                }
            }
        }

        // Handles slider drags
        void SetSliderValueFromDrag()
        {
            if (clampedDragger.dragDirection != ClampedDragger<float>.DragDirection.Free)
                return;

            var originalPosition = m_DragElementStartPos.x;
            var newPosition = originalPosition + clampedDragger.delta.x;
            ComputeValueFromDraggingThumb(originalPosition, newPosition);
        }

        // Handles slider clicks and page scrolls
        void SetSliderValueFromClick()
        {
            if (clampedDragger.dragDirection == ClampedDragger<float>.DragDirection.Free)
                return;

            var worldStartMousePosition = visualInput.LocalToWorld(clampedDragger.startMousePosition);

            // Detection of the thumb click (min or max)
            if (dragMinThumb.worldBound.Contains(worldStartMousePosition))
            {
                m_DragState = DragState.MinThumb;
            }
            else if (dragMaxThumb.worldBound.Contains(worldStartMousePosition))
            {
                m_DragState = DragState.MaxThumb;
            }
            // If the click is within the slider x range, we drag it.
            else if (clampedDragger.startMousePosition.x > dragElement.layout.xMin && clampedDragger.startMousePosition.x < dragElement.layout.xMax)
            {
                m_DragState = DragState.MiddleThumb;
            }
            else
            {
                m_DragState = DragState.NoThumb;
            }

            // No thumb so we want to move the closest min/max slider.
            if (m_DragState == DragState.NoThumb)
            {
                var newValue = ComputeValueFromPosition(clampedDragger.startMousePosition.x);

                if (clampedDragger.startMousePosition.x < dragElement.layout.x)
                {
                    m_DragState = DragState.MinThumb;
                    value = new Vector2(newValue, value.y);
                }
                else
                {
                    m_DragState = DragState.MaxThumb;
                    value = new Vector2(value.x, newValue);
                }
            }

            SetNavigationState(m_DragState);

            // Start dragging
            m_ValueStartPos = value;
            clampedDragger.dragDirection = ClampedDragger<float>.DragDirection.Free;
            m_DragElementStartPos = clampedDragger.startMousePosition;
        }

        void ComputeValueFromDraggingThumb(float dragElementStartPos, float dragElementEndPos)
        {
            var startPosInValue = ComputeValueFromPosition(dragElementStartPos);
            var endPosInValue = ComputeValueFromPosition(dragElementEndPos);
            var deltaInValueWorld = endPosInValue - startPosInValue;
            SetNavigationState(m_DragState);

            switch (m_DragState)
            {
                case DragState.MiddleThumb:
                {
                    var newValue = value;
                    newValue.x = m_ValueStartPos.x + deltaInValueWorld;
                    newValue.y = m_ValueStartPos.y + deltaInValueWorld;
                    var actualDifference = (m_ValueStartPos.y - m_ValueStartPos.x);

                    if (newValue.x < lowLimit)
                    {
                        newValue.x = lowLimit;
                        newValue.y = lowLimit + actualDifference;
                    }
                    else if (newValue.y > highLimit)
                    {
                        newValue.y = highLimit;
                        newValue.x = highLimit - actualDifference;
                    }

                    value = newValue;
                    break;
                }
                case DragState.MinThumb:
                {
                    var newPosition = m_ValueStartPos.x + deltaInValueWorld;
                    if (newPosition > maxValue)
                    {
                        newPosition = maxValue;
                    }
                    else if (newPosition < lowLimit)
                    {
                        newPosition = lowLimit;
                    }

                    value = new Vector2(newPosition, maxValue);
                    break;
                }
                case DragState.MaxThumb:
                {
                    var newPosition = m_ValueStartPos.y + deltaInValueWorld;
                    if (newPosition < minValue)
                    {
                        newPosition = minValue;
                    }
                    else if (newPosition > highLimit)
                    {
                        newPosition = highLimit;
                    }

                    value = new Vector2(minValue, newPosition);
                    break;
                }
            }
        }

        protected override void UpdateMixedValueContent()
        {
        }

        internal override void RegisterEditingCallbacks()
        {
            visualInput.RegisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);
            visualInput.RegisterCallback<PointerUpEvent>(EndEditing);
        }

        internal override void UnregisterEditingCallbacks()
        {
            visualInput.UnregisterCallback<PointerDownEvent>(StartEditing, TrickleDown.TrickleDown);
            visualInput.UnregisterCallback<PointerUpEvent>(EndEditing);
        }
    }
}
