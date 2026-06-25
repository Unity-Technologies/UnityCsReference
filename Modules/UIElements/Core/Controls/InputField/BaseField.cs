// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
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
    public abstract partial class BaseField<TValueType> : AbstractBaseField, INotifyValueChanged<TValueType>
    {
        // Static fields aren't inherited through generic type arguments, so these `new` forwarders
        // keep BaseField<int>.ussClassName working. Canonical values live on AbstractBaseField.

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = AbstractBaseField.ussClassName;

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = AbstractBaseField.labelUssClassName;

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = AbstractBaseField.inputUssClassName;

        /// <summary>
        /// USS class name of elements of this type, when there is no label.
        /// </summary>
        public new static readonly string noLabelVariantUssClassName = AbstractBaseField.noLabelVariantUssClassName;

        /// <summary>
        /// USS class name of labels in elements of this type, when there is a dragger attached on them.
        /// </summary>
        public new static readonly string labelDraggerVariantUssClassName = AbstractBaseField.labelDraggerVariantUssClassName;

        /// <summary>
        /// USS class name of elements that show mixed values
        /// </summary>
        public new static readonly string mixedValueLabelUssClassName = AbstractBaseField.mixedValueLabelUssClassName;

        /// <summary>
        /// USS class name of elements that are aligned in a inspector element
        /// </summary>
        public new static readonly string alignedFieldUssClassName = AbstractBaseField.alignedFieldUssClassName;

        // Binary-compat forwarders for the protected members that also moved to AbstractBaseField:
        // an assembly compiled against the old layout references e.g. BaseField<int>::mixedValueString,
        // which would MissingFieldException without these (same rationale as the USS forwarders above).
        protected internal new static readonly string mixedValueString = AbstractBaseField.mixedValueString;

        protected internal new static readonly PropertyName serializedPropertyCopyName = AbstractBaseField.serializedPropertyCopyName;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static readonly BindingId valueProperty = nameof(value);

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

        /// <summary>
        /// Represents a method that validates or adjusts a field's value before committing it.
        /// </summary>
        /// <remarks> Use this delegate to modify or validate a value before committing it.
        /// For example, you can clamp, sanitize, or replace the input value to match specific constraints.
        /// </remarks>
        /// <param name="value">The value this method modifies.</param>
        /// <returns>The final value after applying the modifications.</returns>
        public delegate TValueType ValidateValueHandler(TValueType value);

        /// <summary>
        /// An event raised to validate or adjust the field's value before committing it.
        /// </summary>
        /// <remarks>
        /// <para>Use this event to modify or validate a value before committing it.
        /// For example, you can clamp, sanitize, or replace the input value to match specific constraints.
        /// To reject an input value, return a fallback such as the current <see cref="value"/>.</para>
        /// <para>Registered callbacks run whenever the field's value changes, either through the
        /// <see cref="value"/> setter or <see cref="SetValueWithoutNotify"/>. The value returned by each
        /// callback replaces the incoming value and is then stored. If the change occurs from the
        /// <see cref="value"/> setter, it reports the stored value as <see cref="ChangeEvent{T}.newValue"/>.
        /// If the change occurs from <see cref="SetValueWithoutNotify"/>, it doesn't report a <see cref="ChangeEvent{T}"/>.</para>
        /// <para>If you register more than one callback, they compose in their registration order. Each callback
        /// receives the previous callback's result, and the final result is stored and reported.</para>
        /// <para>To assign a value while bypassing these callbacks, wrap the assignment in
        /// <see cref="IgnoreValidation"/>.</para>
        /// </remarks>
        /// <example>
        /// The following example clamps a field's value to a maximum before returning it:
        /// <code source="../../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/BaseField_onValidateValue.cs"/>
        /// </example>
        public event ValidateValueHandler onValidateValue
        {
            add
            {
                if (value != null)
                    (m_OnValidateValue ??= new List<ValidateValueHandler>()).Add(value);
            }
            remove => m_OnValidateValue?.Remove(value);
        }

        internal List<ValidateValueHandler> m_OnValidateValue;

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

        // Used for UXML as UxmlAttribute can not be used on virtual properties.
        [UxmlAttribute("value"), UxmlAttributeBindingPath(nameof(value)), UxmlInternalField]
        internal TValueType valueUXML
        {
            get => value;
            set => SetValueWithoutNotify(value);
        }

        bool m_SkipValidation;

        // Sent whenever we overwrite the value from view data.
        internal event Action viewDataRestored;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal BaseField(string label) : base(label)
        {
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="BaseField{TValueType}"/>.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="visualInput">The visual element to use as the input for the field.</param>
        protected BaseField(string label, VisualElement visualInput)
            : base(label, visualInput)
        {
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal virtual bool EqualsCurrentValue(TValueType v) => EqualityComparer<TValueType>.Default.Equals(m_Value, v);

        internal TValueType ValidatedValue(TValueType value)
        {
            var validators = m_OnValidateValue;
            if (validators == null || validators.Count == 0)
                return value;

            if (validators.Count == 1)
                return validators[0](value);

            // Snapshot so a validator that unsubscribes itself mid-iteration can't skip the next one.
            using (ListPool<ValidateValueHandler>.Get(out var snapshot))
            {
                snapshot.AddRange(validators);
                for (var i = 0; i < snapshot.Count; i++)
                    value = snapshot[i](value);
            }

            return value;
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

        /// <summary>
        /// Disables <see cref="onValidateValue"/> until the returned scope is disposed.
        /// </summary>
        /// <remarks>
        /// Scope this method with a <see langword="using"/> statement and use it with the
        /// <see cref="value"/> setter or <see cref="SetValueWithoutNotify"/> to assign a value that bypasses validation.
        /// Only <see cref="value"/> results in a <see cref="ChangeEvent{T}"/>.
        /// The scope can be nested safely.
        /// </remarks>
        /// <example>
        /// The following example sets a value that skips the field's validation:
        /// <code source="../../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/BaseField_IgnoreValidation.cs"/>
        /// </example>
        /// <returns>A scope that re-enables validation when disposed.</returns>
        public IgnoreValidationScope IgnoreValidation() => new IgnoreValidationScope(this);

        /// <summary>
        /// A scope, returned by <see cref="IgnoreValidation"/>, that disables <see cref="onValidateValue"/> until it is disposed.
        /// </summary>
        public readonly struct IgnoreValidationScope : IDisposable
        {
            readonly BaseField<TValueType> m_Field;
            readonly bool m_PreviousSkipValidation;

            internal IgnoreValidationScope(BaseField<TValueType> field)
            {
                m_Field = field;
                m_PreviousSkipValidation = field.m_SkipValidation;
                field.m_SkipValidation = true;
            }

            /// <summary>
            /// Re-enables validation.
            /// </summary>
            public void Dispose()
            {
                if (m_Field != null)
                    m_Field.m_SkipValidation = m_PreviousSkipValidation;
            }
        }

        internal void SetValueWithoutValidation(TValueType newValue)
        {
            using (IgnoreValidation())
                value = newValue;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            if (visualInput != null)
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
    }
}
