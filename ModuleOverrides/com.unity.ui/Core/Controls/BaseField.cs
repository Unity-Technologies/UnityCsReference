// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal interface IPrefixLabel
    {
        string label { get; }
    }

    /// <summary>
    /// Abstract base class for controls.
    /// </summary>
    public abstract class BaseField<TValueType> : BindableElement, INotifyValueChanged<TValueType>, IMixedValueSupport, IPrefixLabel
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseField"/>.
        /// </summary>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label" };

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusIndex.defaultValue = 0;
                focusable.defaultValue = true;
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((BaseField<TValueType>)ve).label = m_Label.GetValueFromBag(bag, cc);
            }

            internal static List<string> ParseChoiceList(string choicesFromBag)
            {
                if (string.IsNullOrEmpty(choicesFromBag.Trim()))
                    return null;

                // Here the choices is comma separated in the string...
                var choices = choicesFromBag.Split(',');

                if (choices.Length != 0)
                {
                    var listOfChoices = new List<string>();
                    foreach (var choice in choices)
                    {
                        listOfChoices.Add(choice.Trim());
                    }
                    return listOfChoices;
                }
                return null;
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-base-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of elements of this type, when there is no label.
        /// </summary>
        public static readonly string noLabelVariantUssClassName = ussClassName + "--no-label";
        /// <summary>
        /// USS class name of labels in elements of this type, when there is a dragger attached on them.
        /// </summary>
        public static readonly string labelDraggerVariantUssClassName = labelUssClassName + "--with-dragger";
        /// <summary>
        /// USS class name of elements that show mixed values
        /// </summary>
        public static readonly string mixedValueLabelUssClassName = labelUssClassName + "--mixed-value";
        /// <summary>
        /// USS class name of elements that are aligned in a inspector element
        /// </summary>
        public static readonly string alignedFieldUssClassName = ussClassName + "__aligned";

        private static readonly string inspectorFieldUssClassName = ussClassName + "__inspector-field";

        /// <summary>
        /// Same as EditorGUI.kIndentPerLevel but in runtime world.
        /// </summary>
        private const int kIndentPerLevel = 15;

        protected internal static readonly string mixedValueString = "\u2014";
        protected internal static readonly PropertyName serializedPropertyCopyName = "SerializedPropertyCopyName";

        static CustomStyleProperty<float> s_LabelWidthRatioProperty = new CustomStyleProperty<float>("--unity-property-field-label-width-ratio");
        static CustomStyleProperty<float> s_LabelExtraPaddingProperty = new CustomStyleProperty<float>("--unity-property-field-label-extra-padding");
        static CustomStyleProperty<float> s_LabelBaseMinWidthProperty = new CustomStyleProperty<float>("--unity-property-field-label-base-min-width");

        private float m_LabelWidthRatio;
        private float m_LabelExtraPadding;
        private float m_LabelBaseMinWidth;

        private VisualElement m_VisualInput;

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
                m_VisualInput.AddToClassList(inputUssClassName);
                Add(m_VisualInput);
            }
        }

        [SerializeField]
        TValueType m_Value;

        /// <summary>
        /// The value of the element.
        /// </summary>
        protected TValueType rawValue
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        /// <summary>
        /// The value associated with the field.
        /// </summary>
        public virtual TValueType value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (!EqualityComparer<TValueType>.Default.Equals(m_Value, value))
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<TValueType> evt = ChangeEvent<TValueType>.GetPooled(m_Value, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
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
        /// </summary>
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
                        AddToClassList(noLabelVariantUssClassName);
                        labelElement.RemoveFromHierarchy();
                    }
                    else
                    {
                        if (!Contains(labelElement))
                        {
                            Insert(0, labelElement);
                            RemoveFromClassList(noLabelVariantUssClassName);
                        }
                    }
                }
            }
        }

        bool m_ShowMixedValue;

        /// <summary>
        /// When set to true, gives the field the appearance of editing multiple different values.
        /// </summary>
        public bool showMixedValue
        {
            get => m_ShowMixedValue;
            set
            {
                if (value == m_ShowMixedValue) return;

                m_ShowMixedValue = value;

                // Once value has been set, update the field's appearance
                UpdateMixedValueContent();
            }
        }

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
                    m_MixedValueLabel.AddToClassList(labelUssClassName);
                    m_MixedValueLabel.AddToClassList(mixedValueLabelUssClassName);
                }

                return m_MixedValueLabel;
            }
        }

        private VisualElement m_CachedInspectorElement;
        private int m_CachedListAndFoldoutDepth;

        internal BaseField(string label)
        {
            isCompositeRoot = true;
            focusable = true;
            tabIndex = 0;
            excludeFromFocusRing = true;
            delegatesFocus = true;

            AddToClassList(ussClassName);

            labelElement = new Label() { focusable = true, tabIndex = -1 };
            labelElement.AddToClassList(labelUssClassName);
            if (label != null)
            {
                this.label = label;
            }
            else
            {
                AddToClassList(noLabelVariantUssClassName);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            m_VisualInput = null;
        }

        protected BaseField(string label, VisualElement visualInput)
            : this(label)
        {
            this.visualInput = visualInput;
        }

        private void OnAttachToPanel(AttachToPanelEvent e)
        {
            var currentElement = parent;
            while (currentElement != null)
            {
                if (currentElement.ClassListContains("unity-inspector-element"))
                {
                    // These default values are based of IMGUI
                    m_LabelWidthRatio = 0.45f;
                    m_LabelExtraPadding = 2.0f;
                    m_LabelBaseMinWidth = 120.0f;

                    RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                    AddToClassList(inspectorFieldUssClassName);
                    m_CachedInspectorElement = currentElement;
                    m_CachedListAndFoldoutDepth = this.GetListAndFoldoutDepth();
                    RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
                    break;
                }

                currentElement = currentElement.parent;
            }
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
        }

        private void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
        {
            AlignLabel();
        }

        private void AlignLabel()
        {
            if (!ClassListContains(alignedFieldUssClassName))
            {
                return;
            }

            var listAndFoldoutMargin = kIndentPerLevel * m_CachedListAndFoldoutDepth;

            // Calculate all extra padding from the containing element's contents
            var totalPadding = resolvedStyle.paddingLeft + resolvedStyle.paddingRight +
                resolvedStyle.marginLeft + resolvedStyle.marginRight;

            // Get inspector element padding next
            totalPadding += m_CachedInspectorElement.resolvedStyle.paddingLeft +
                m_CachedInspectorElement.resolvedStyle.paddingRight +
                m_CachedInspectorElement.resolvedStyle.marginLeft +
                m_CachedInspectorElement.resolvedStyle.marginRight;

            // Then get label padding
            totalPadding += labelElement.resolvedStyle.paddingLeft + labelElement.resolvedStyle.paddingRight +
                labelElement.resolvedStyle.marginLeft + labelElement.resolvedStyle.marginRight;

            // Then get base field padding
            totalPadding += resolvedStyle.paddingLeft + resolvedStyle.paddingRight +
                resolvedStyle.marginLeft + resolvedStyle.marginRight;

            // Not all visual input controls have the same padding so we can't base our total padding on
            // that information.  Instead we add a flat value to totalPadding to best match the hard coded
            // calculation in IMGUI
            totalPadding += m_LabelExtraPadding;
            totalPadding += listAndFoldoutMargin;

            labelElement.style.minWidth = Mathf.Max(m_LabelBaseMinWidth - listAndFoldoutMargin, 0);

            // Formula to follow IMGUI label width settings
            var newWidth = m_CachedInspectorElement.resolvedStyle.width * m_LabelWidthRatio - totalPadding;
            if (Mathf.Abs(labelElement.resolvedStyle.width - newWidth) > UIRUtility.k_Epsilon)
            {
                labelElement.style.width = Mathf.Max(0f, newWidth);
            }
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
            m_Value = newValue;

            if (!string.IsNullOrEmpty(viewDataKey))
                SaveViewData();
            MarkDirtyRepaint();

            if (showMixedValue)
                UpdateMixedValueContent();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            if (m_VisualInput != null)
            {
                var key = GetFullHierarchicalViewDataKey();
                var oldValue = m_Value;
                OverwriteFromViewData(this, key);

                if (!EqualityComparer<TValueType>.Default.Equals(oldValue, m_Value))
                {
                    using (ChangeEvent<TValueType> evt = ChangeEvent<TValueType>.GetPooled(oldValue, m_Value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(m_Value);
                        SendEvent(evt);
                    }
                }
            }
        }

        internal override Rect GetTooltipRect()
        {
            return !string.IsNullOrEmpty(label) ? labelElement.worldBound : worldBound;
        }
    }

    /// <summary>
    /// Traits for the <see cref="BaseField"/>.
    /// </summary>
    public class BaseFieldTraits<TValueType, TValueUxmlAttributeType> : BaseField<TValueType>.UxmlTraits
        where TValueUxmlAttributeType : TypedUxmlAttributeDescription<TValueType>, new()
    {
        TValueUxmlAttributeType m_Value = new TValueUxmlAttributeType { name = "value" };

        /// <summary>
        /// Initializes the trait of <see cref="BaseField"/>.
        /// </summary>
        /// <param name="ve">The VisualElement to initialize.</param>
        /// <param name="bag">Bag of attributes.</param>
        /// <param name="cc">The creation context associated with these traits.</param>
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            ((INotifyValueChanged<TValueType>)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
        }
    }
}
