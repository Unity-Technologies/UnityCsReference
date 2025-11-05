// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal abstract class StylePropertyField<TStyleValue, TValueField, TValue> : BaseField<TStyleValue>, IValueField<TStyleValue>, IAffordanceField
        where TStyleValue : IStyleValue<TValue>
        where TValueField : BaseField<TValue>
    {
        public static readonly BindingId persistentValidationProperty = nameof(persistentValidation);
        public static readonly BindingId validationProperty = nameof(validation);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : BaseField<TStyleValue>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeReference, UxmlObjectReference] List<StylePropertyValidation.UxmlSerializedData> validation;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags validation_UxmlAttributeFlags;
            [SerializeField] bool containsAffordance = true;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags containsAffordance_UxmlAttributeFlags;
            #pragma warning restore 649

            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TStyleValue>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(validation), "validation"),
                    new (nameof(containsAffordance), "contains-affordance"),
                }, true);
            }

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (StylePropertyField<TStyleValue, TValueField, TValue>)obj;

                if (ShouldWriteAttributeValue(validation_UxmlAttributeFlags) && validation != null)
                {
                    e.validation.Clear();
                    foreach (var validationData in validation)
                    {
                        var v = (StylePropertyValidation)validationData.CreateInstance();
                        validationData.Deserialize(v);
                        e.AddValidation(v);
                    }
                }

                if (ShouldWriteAttributeValue(containsAffordance_UxmlAttributeFlags))
                {
                    e.containsAffordance = containsAffordance;
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-style-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        TValueField m_ValueField;
        List<StylePropertyValidation> m_PersistentValidation;
        readonly List<StylePropertyValidation> m_Validation = new();
        BaseFieldMouseDragger m_Dragger;
        bool m_ContainAffordance = true;

        /// <summary>
        /// The <see cref="BaseField{TValue}"/> that represents the inner field.
        /// </summary>
        public TValueField valueField => m_ValueField;

        /// <summary>
        /// The affordance element.
        /// </summary>
        public FieldAffordanceElement affordanceElement { get; set; }

        /// <summary>
        /// The <see cref="BaseFieldMouseDragger"/> that can be used to change the value of the field.
        /// </summary>
        BaseFieldMouseDragger dragger => m_Dragger;

        /// <summary>
        /// The collection of syntax to validate the value of the field.
        /// </summary>
        [CreateProperty]
        List<StylePropertyValidation> persistentValidation
        {
            get => m_PersistentValidation ??= new List<StylePropertyValidation>();
            set
            {
                m_PersistentValidation = value;

                UpdateValidation();
                NotifyPropertyChanged(persistentValidationProperty);
            }
        }

        /// <summary>
        /// The collection of syntax to validate the value of the field.
        /// </summary>
        [CreateProperty]
        internal List<StylePropertyValidation> validation => m_Validation;

        /// <summary>
        /// Whether the style property field is preceded by an affordance.
        /// </summary>
        [CreateProperty]
        internal bool containsAffordance
        {
            get => m_ContainAffordance;
            set
            {
                m_ContainAffordance = value;

                if (!m_ContainAffordance && affordanceElement != null)
                {
                    affordanceElement.RemoveFromHierarchy();
                    // If we had added a labelElement for the correct hierarchy, remove it
                    if (Contains(labelElement) && string.IsNullOrEmpty(labelElement.text))
                    {
                        AddToClassList(noLabelVariantUssClassName);
                        labelElement.RemoveFromHierarchy();
                    }
                }
                else if (m_ContainAffordance && (affordanceElement == null || !Contains(affordanceElement)))
                {
                    // Ensure labelElement is added so the affordance appears at index 0
                    if (!Contains(labelElement))
                    {
                        hierarchy.Insert(0, labelElement);
                        RemoveFromClassList(noLabelVariantUssClassName);
                    }

                    affordanceElement = new FieldAffordanceElement();
                    hierarchy.Insert(0, affordanceElement);
                }
            }
        }

        public StylePropertyValidationCollection GetValidation() => new (m_PersistentValidation, m_Validation);

        /// <summary>
        /// Refreshes the value field with the new validation.
        /// </summary>
        /// <remarks>
        /// Call this method whenever the validation changes.
        /// </remarks>
        public virtual void UpdateValidation()
        {
            // Nothing to do here
        }

        protected virtual bool Validate(TValue previousValue, TValue newValue)
        {
            // Nothing to do here
            return true;
        }

        /// <summary>
        /// Initializes and returns an instance of StylePropertyField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        protected StylePropertyField(string label)
            : this(label, null) { }

        /// <summary>
        /// Initializes and returns an instance of StylePropertyField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="visualInput">The visual input of the field.</param>
        /// <param name="containsAffordance">Whether the style property field is preceded by an affordance.</param>
        protected StylePropertyField(string label, TValueField visualInput, bool containsAffordance = true)
            : base(label, visualInput)
        {
            // Do not allow null visualInput
            visualInput ??= CreateValueField();
            visualInput.pickingMode = PickingMode.Ignore;
            visualInput.focusable = true;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            m_ValueField = visualInput;
            m_ValueField.AddToClassList(ussClassName);
            m_ValueField.visualInput?.AddToClassList(inputUssClassName);
            m_ValueField.AddToClassList(alignedFieldUssClassName);

            m_ValueField.RegisterValueChangedCallback(evt =>
            {
                if (Validate(evt.previousValue, evt.newValue))
                    value = CreateStyleValue(evt.newValue);
            });

            AddLabelDragger();

            this.containsAffordance = containsAffordance;
        }

        protected abstract TValueField CreateValueField();
        protected abstract TStyleValue CreateStyleValue(TValue v);

        public override void SetValueWithoutNotify(TStyleValue newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_ValueField.SetValueWithoutNotify(value.value);
        }

        protected void AddLabelDragger()
        {
            if (valueField is TextValueField<TValue> textValueField)
            {
                m_Dragger = new FieldMouseDragger<TStyleValue>(this);
                EnableLabelDragger(!textValueField.isReadOnly);
            }
        }

        public void EnableLabelDragger(bool enable)
        {
            if (m_Dragger != null)
            {
                m_Dragger.SetDragZone(enable ? labelElement : null);

                labelElement.EnableInClassList(labelDraggerVariantUssClassName, enable);
            }
        }

        public void AddValidation(StylePropertyValidation propertyValidation)
        {
            validation.Add(propertyValidation);
            UpdateValidation();
            NotifyPropertyChanged(validationProperty);
        }

        public void RemoveValidation(StylePropertyValidation propertyValidation)
        {
            if (validation.Remove(propertyValidation))
            {
                UpdateValidation();
                NotifyPropertyChanged(validationProperty);
            }
        }

        public void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, TStyleValue startValue)
        {
            if (valueField is TextValueField<TValue> textValueField)
            {
                textValueField.ApplyInputDeviceDelta(delta, speed, startValue.value);
            }
        }

        public void StartDragging()
        {
            if (valueField is TextValueField<TValue> textValueField)
            {
                textValueField.StartDragging();
            }

        }

        public void StopDragging()
        {
            if (valueField is TextValueField<TValue> textValueField)
            {
                textValueField.StopDragging();
            }
        }
    }
}
