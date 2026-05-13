// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal interface IPrefixLabel
    {
        string label { get; }
        Label labelElement { get; }
    }

    internal interface IDelayedField
    {
        bool isDelayed { get; }
    }

    /// <summary>
    /// Interface for all editable elements.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal interface IEditableElement
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Action editingStarted { get; set; }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Action editingEnded { get; set; }
    }

    /// <summary>
    /// <para>Abstract base class for controls.</para>
    /// <para>A BaseField is a base class for field elements like <see cref="TextField"/> and <see cref="IntegerField"/>.
    /// To align a BaseField element automatically with other fields in an Inspector window,
    /// use the @@.unity-base-field__aligned@@ USS class. This style class is designed for use with
    /// Inspector elements like <see cref="PropertyField"/>, which has the style class by default.
    /// However, if you manually add a child BaseField element to a PropertyField, you must add
    /// the style class manually.</para>
    /// <para>When the style class is present, the field automatically calculates the label width
    /// to align with other fields in the Inspector window. If there are IMGUI fields present,
    /// UI Toolkit fields are aligned with them for consistency and compatibility.</para>
    /// </summary>
    [UxmlElement]
    public abstract partial class BaseField<TValueType> : BindableElement, INotifyValueChanged<TValueType>, IMixedValueSupport, IPrefixLabel, IEditableElement
    {
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static readonly BindingId valueProperty = nameof(value);
        internal static readonly BindingId labelProperty = nameof(label);
        internal static readonly BindingId showMixedValueProperty = nameof(showMixedValue);

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-base-field";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        internal static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string inputUssClassName = ussClassName + "__input";
        internal static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// USS class name of elements of this type, when there is no label.
        /// </summary>
        public static readonly string noLabelVariantUssClassName = ussClassName + "--no-label";
        internal static readonly UniqueStyleString noLabelVariantUssClassNameUnique = new(noLabelVariantUssClassName);

        /// <summary>
        /// USS class name of labels in elements of this type, when there is a dragger attached on them.
        /// </summary>
        public static readonly string labelDraggerVariantUssClassName = labelUssClassName + "--with-dragger";
        internal static readonly UniqueStyleString labelDraggerVariantUssClassNameUnique = new(labelDraggerVariantUssClassName);

        /// <summary>
        /// USS class name of elements that show mixed values
        /// </summary>
        public static readonly string mixedValueLabelUssClassName = labelUssClassName + "--mixed-value";
        internal static readonly UniqueStyleString mixedValueLabelUssClassNameUnique = new(mixedValueLabelUssClassName);

        /// <summary>
        /// USS class name of elements that are aligned in a inspector element
        /// </summary>
        public static readonly string alignedFieldUssClassName = ussClassName + "__aligned";
        internal static readonly UniqueStyleString alignedFieldUssClassNameUnique = new(alignedFieldUssClassName);

        private static readonly string inspectorFieldUssClassName = ussClassName + "__inspector-field";
        internal static readonly UniqueStyleString inspectorFieldUssClassNameUnique = new(inspectorFieldUssClassName);

        private static readonly UniqueStyleString inspectorElementUssClassNameUnique = new("unity-inspector-element");
        private static readonly UniqueStyleString inspectorMainContainerUssClassNameUnique = new("unity-inspector-main-container");

        protected internal static readonly string mixedValueString = "\u2014";

        protected internal static readonly PropertyName serializedPropertyCopyName = "SerializedPropertyCopyName";

        static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");
        static CustomStyleProperty<float> s_LabelBaseMinWidthProperty = new CustomStyleProperty<float>("--unity-property-field-label-base-min-width");

        private float m_LabelWidthRatio;
        private float m_LabelExtraPadding;
        private float m_LabelBaseMinWidth;

        private VisualElement m_VisualInput;

        // Event triggered when an expression is evaluated for a numeric field
        internal Action<ExpressionEvaluator.Expression> expressionEvaluated;

        // Sent whenever we overwrite the value from view data.
        internal event Action viewDataRestored;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElement visualInput
        {
            get { return m_VisualInput; }
            set
            {
                // Get rid of the older value...
                if (m_VisualInput != null)
                {
                    if (m_VisualInput.parent == this)
                    {
                        m_VisualInput.RemoveFromHierarchy();
                    }

                    m_VisualInput = null;
                }

                // Handle the new value...
                if (value != null)
                {
                    m_VisualInput = value;
                }
                else
                {
                    m_VisualInput = new VisualElement() { pickingMode = PickingMode.Ignore };
                }
                m_VisualInput.focusable = true;
                m_VisualInput.AddToClassList(inputUssClassNameUnique);
                Add(m_VisualInput);
            }
        }

        [SerializeField, DontCreateProperty]
        TValueType m_Value;

        /// <summary>
        /// The value of the element.
        /// </summary>
        protected TValueType rawValue
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        internal event Func<TValueType, TValueType> onValidateValue;

        // This has been introduced as part of the fix for UUM-62652.
        // Enable some fields to specify how ValueChanged events should be dispatched (queued or immediate). It is mainly used by delayed text fields
        // to immediately notify on value change as soon as the focus is lost, which was required to fix UUM-62652.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal DispatchMode dispatchMode { get; set; } = DispatchMode.Default;

        /// <summary>
        /// The value associated with the field.
        /// </summary>
        [CreateProperty]
        public virtual TValueType value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (!EqualsCurrentValue(value) || showMixedValue)
                {
                    var previousValue = m_Value;
                    SetValueWithoutNotify(value);

                    // We set showMixedValue after setting the value or it will revert the text back to the previous value. (UUUM-73855)
                    showMixedValue = false;

                    if (panel != null)
                    {
                        using (ChangeEvent<TValueType> evt = ChangeEvent<TValueType>.GetPooled(previousValue, m_Value))
                        {
                            evt.elementTarget = this;
                            SendEvent(evt, dispatchMode);
                        }
                        NotifyPropertyChanged(valueProperty);
                    }
                }
            }
        }

        /// <summary>
        /// This is the <see cref="Label"/> object that appears beside the input for the field.
        /// </summary>
        public Label labelElement { get; private set; }

        /// <summary>
        /// The string representing the label that will appear beside the field.
        /// If the string is empty, the label element is removed from the hierarchy.
        /// If the string is not empty, the label element is added to the hierarchy.
        /// </summary>
        [CreateProperty]
        [MultilineTextField]
        [UxmlAttribute]
        public string label
        {
            get
            {
                return labelElement.text;
            }
            set
            {
                if (labelElement.text != value)
                {
                    labelElement.text = value;
                    if (string.IsNullOrEmpty(labelElement.text))
                    {
                        AddToClassList(noLabelVariantUssClassNameUnique);
                        labelElement.RemoveFromHierarchy();
                    }
                    else
                    {
                        if (!Contains(labelElement))
                        {
                            hierarchy.Insert(0, labelElement);
                            RemoveFromClassList(noLabelVariantUssClassNameUnique);
                        }
                    }

                    NotifyPropertyChanged(labelProperty);
                }
            }
        }

        // Used for UXML as UxmlAttribute can not be used on virtual properties.
        [UxmlAttribute("value"), UxmlAttributeBindingPath(nameof(value)), UxmlInternalField]
        internal TValueType valueUXML
        {
            get => value;
            set => SetValueWithoutNotify(value);
        }

        bool m_ShowMixedValue;

        /// <summary>
        /// When set to true, gives the field the appearance of editing multiple different values.
        /// </summary>
        [CreateProperty]
        public bool showMixedValue
        {
            get => m_ShowMixedValue;
            set
            {
                if (value == m_ShowMixedValue) return;

                if (value && !canSwitchToMixedValue)
                {
                    return;
                }

                m_ShowMixedValue = value;

                // Once value has been set, update the field's appearance
                UpdateMixedValueContent();

                NotifyPropertyChanged(showMixedValueProperty);
            }
        }

        private protected virtual bool canSwitchToMixedValue => true;

        Label m_MixedValueLabel;
        /// <summary>
        /// Read-only label used to give the appearance of editing multiple different values.
        /// </summary>
        protected Label mixedValueLabel
        {
            get
            {
                if (m_MixedValueLabel == null)
                {
                    m_MixedValueLabel = new Label(mixedValueString) { focusable = true, tabIndex = -1 };
                    m_MixedValueLabel.AddToClassList(labelUssClassNameUnique);
                    m_MixedValueLabel.AddToClassList(mixedValueLabelUssClassNameUnique);
                }

                return m_MixedValueLabel;
            }
        }

        bool m_SkipValidation;
        private VisualElement m_CachedContextWidthElement;
        private VisualElement m_CachedInspectorElement;

        Action IEditableElement.editingStarted { get; set; }
        Action IEditableElement.editingEnded { get; set; }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal BaseField(string label)
        {
            isCompositeRoot = true;
            focusable = true;
            tabIndex = 0;
            excludeFromFocusRing = true;
            delegatesFocus = true;

            AddToClassList(ussClassNameUnique);

            labelElement = new Label() { focusable = true, tabIndex = -1 };
            labelElement.AddToClassList(labelUssClassNameUnique);
            if (label != null)
            {
                this.label = label;
            }
            else
            {
                AddToClassList(noLabelVariantUssClassNameUnique);
            }

            Callbacks.OnAttachToPanel.Register(this);
            Callbacks.OnDetachFromPanel.Register(this);

            m_VisualInput = null;
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="BaseField"/>.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="visualInput">The visual element to use as the input for the field.</param>
        protected BaseField(string label, VisualElement visualInput)
            : this(label)
        {
            this.visualInput = visualInput;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal virtual bool EqualsCurrentValue(TValueType v) => EqualityComparer<TValueType>.Default.Equals(m_Value, v);

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            // indicates the start and end of the field value being edited
            RegisterEditingCallbacks();

            if (e.destinationPanel == null)
            {
                return;
            }

            if (e.destinationPanel.contextType == ContextType.Player)
            {
                return;
            }

            m_CachedInspectorElement = null;
            m_CachedContextWidthElement = null;

            var currentElement = parent;
            while (currentElement != null)
            {
                if (currentElement.ClassListContains(inspectorElementUssClassNameUnique))
                {
                    m_CachedInspectorElement = currentElement;
                }

                if (currentElement.ClassListContains(inspectorMainContainerUssClassNameUnique))
                {
                    m_CachedContextWidthElement = currentElement;
                    break;
                }

                currentElement = currentElement.parent;
            }

            if (m_CachedInspectorElement == null)
            {
                RemoveFromClassList(inspectorFieldUssClassNameUnique);
                return;
            }

            // These default values are based of IMGUI
            m_LabelWidthRatio = 0.45f;

            // Those values are 40 and 120 in IMGUI, but they already take in account the fields margin. We readjust them
            // because the uitk margin is being taken in account later.
            m_LabelExtraPadding = 37.0f;
            m_LabelBaseMinWidth = 123.0f;

            Callbacks.OnCustomStyleResolved.Register(this);
            Callbacks.OnInspectorFieldGeometryChanged.Register(this);
            AddToClassList(inspectorFieldUssClassNameUnique);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            Callbacks.OnInspectorFieldGeometryChanged.Unregister(this);
            Callbacks.OnCustomStyleResolved.Unregister(this);

            UnregisterEditingCallbacks();

            onValidateValue = null;
        }

        internal virtual void RegisterEditingCallbacks()
        {
            Callbacks.OnFocusInStartEditing.Register(this);
            Callbacks.OnFocusOutEndEditing.Register(this);
        }

        internal virtual void UnregisterEditingCallbacks()
        {
            Callbacks.OnFocusOutEndEditing.Unregister(this);
            Callbacks.OnFocusInStartEditing.Unregister(this);
        }

        internal void StartEditing(EventBase e)
        {
            ((IEditableElement)this).editingStarted?.Invoke();
        }

        internal void EndEditing(EventBase e)
        {
            ((IEditableElement)this).editingEnded?.Invoke();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_LabelWidthRatioProperty, out var labelWidthRatio))
            {
                m_LabelWidthRatio = labelWidthRatio;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraPaddingProperty, out var labelExtraPadding))
            {
                m_LabelExtraPadding = labelExtraPadding;
            }

            if (evt.customStyle.TryGetValue(s_LabelBaseMinWidthProperty, out var labelBaseMinWidth))
            {
                m_LabelBaseMinWidth = labelBaseMinWidth;
            }

            AlignLabel();
        }

        private void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
        {
            AlignLabel();
        }

        private void AlignLabel()
        {
            if (!ClassListContains(alignedFieldUssClassNameUnique) || m_CachedInspectorElement == null)
            {
                return;
            }

            // Not all visual input controls have the same padding so we can't base our total padding on
            // that information.  Instead we add a flat value to totalPadding to best match the hard coded
            // calculation in IMGUI
            var totalPadding = m_LabelExtraPadding;
            var spacing = worldBound.x - m_CachedInspectorElement.worldBound.x - m_CachedInspectorElement.resolvedStyle.paddingLeft;

            totalPadding += spacing;
            totalPadding += resolvedStyle.paddingLeft;

            var minWidth = m_LabelBaseMinWidth - spacing - resolvedStyle.paddingLeft;
            var contextWidthElement = m_CachedContextWidthElement ?? m_CachedInspectorElement;

            labelElement.style.minWidth = Mathf.Max(minWidth, 0);

            // Formula to follow IMGUI label width settings
            var newWidth = Mathf.Ceil(contextWidthElement.resolvedStyle.width * m_LabelWidthRatio) - totalPadding;
            if (Mathf.Abs(labelElement.resolvedStyle.width - newWidth) > UIRUtility.k_Epsilon)
            {
                labelElement.style.width = Mathf.Max(0f, newWidth);
            }
        }

        private Rect ComputeTooltipRect()
        {
            if (!string.IsNullOrEmpty(label))
            {
                return string.IsNullOrEmpty(labelElement.tooltip) ? labelElement.worldBound : worldBound;
            }
            return worldBound;
        }

        internal TValueType ValidatedValue(TValueType value)
        {
            if (onValidateValue != null)
            {
                return onValidateValue.Invoke(value);
            }

            return value;
        }

        [EventInterest(typeof(TooltipEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is not TooltipEvent tooltipEvent)
            {
                base.HandleEventBubbleUp(evt);
                return;
            }

            // Hide the field's tooltip if the label does not have a tooltip and the mouse is not over the label.
            if ((tooltipEvent.elementTarget == labelElement)
                || (!string.IsNullOrEmpty(labelElement?.tooltip)) || (string.IsNullOrEmpty(label)))
                tooltipEvent.rect = ComputeTooltipRect();
            else
                tooltipEvent.StopImmediatePropagation();
        }

        /// <summary>
        /// Update the field's visual content depending on showMixedValue.
        /// </summary>
        protected virtual void UpdateMixedValueContent()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Allow to set a value without being notified of the change, if any.
        /// </summary>
        /// <param name="newValue">New value to be set.</param>
        public virtual void SetValueWithoutNotify(TValueType newValue)
        {
            if (m_SkipValidation)
            {
                m_Value = newValue;
            }
            else
            {
                m_Value = ValidatedValue(newValue);
            }


            if (!string.IsNullOrEmpty(viewDataKey))
                SaveViewData();
            MarkDirtyRepaint();

            if (showMixedValue)
                UpdateMixedValueContent();
        }

        internal void SetValueWithoutValidation(TValueType newValue)
        {
            m_SkipValidation = true;

            value = newValue;

            m_SkipValidation = false;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            if (m_VisualInput != null)
            {
                var key = GetFullHierarchicalViewDataKey();
                var oldValue = m_Value;
                OverwriteFromViewData(this, key);
                viewDataRestored?.Invoke();

                if (!EqualityComparer<TValueType>.Default.Equals(oldValue, m_Value))
                {
                    using (ChangeEvent<TValueType> evt = ChangeEvent<TValueType>.GetPooled(oldValue, m_Value))
                    {
                        evt.elementTarget = this;
                        SetValueWithoutNotify(m_Value);
                        SendEvent(evt);
                    }
                }
            }
        }

        private static class Callbacks
        {
            public static readonly EventCallbackDefinition<BaseField<TValueType>> OnAttachToPanel =
                EventCallback.Create<AttachToPanelEvent, BaseField<TValueType>>(static (e, self) => self.OnAttachToPanel(e));
            public static readonly EventCallbackDefinition<BaseField<TValueType>> OnDetachFromPanel =
                EventCallback.Create<DetachFromPanelEvent, BaseField<TValueType>>(static (e, self) => self.OnDetachFromPanel(e));
            public static readonly EventCallbackDefinition<BaseField<TValueType>> OnCustomStyleResolved =
                EventCallback.Create<CustomStyleResolvedEvent, BaseField<TValueType>>(static (e, self) => self.OnCustomStyleResolved(e));
            public static readonly EventCallbackDefinition<BaseField<TValueType>> OnInspectorFieldGeometryChanged =
                EventCallback.Create<GeometryChangedEvent, BaseField<TValueType>>(static (e, self) => self.OnInspectorFieldGeometryChanged(e));
            public static readonly EventCallbackDefinition<BaseField<TValueType>> OnFocusInStartEditing =
                EventCallback.Create<FocusInEvent, BaseField<TValueType>>(static (e, self) => self.StartEditing(e));
            public static readonly EventCallbackDefinition<BaseField<TValueType>> OnFocusOutEndEditing =
                EventCallback.Create<FocusOutEvent, BaseField<TValueType>>(static (e, self) => self.EndEditing(e));
        }
    }
}
