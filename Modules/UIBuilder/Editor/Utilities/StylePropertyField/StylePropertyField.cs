// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal abstract class StylePropertyField<TStyleValue, TValueField, TValue> : BaseField<TStyleValue>, IValueField<TStyleValue>
        where TStyleValue : IStyleValue<TValue>, new()
        where TValueField : BaseField<TValue>, new()
    {
        public static readonly BindingId persistentValidationProperty = nameof(persistentValidation);
        public static readonly BindingId validationProperty = nameof(validation);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : BaseField<TStyleValue>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeReference, UxmlObjectReference] List<StylePropertyValidation.UxmlSerializedData> validation;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags validation_UxmlAttributeFlags;
            #pragma warning restore 649

            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TStyleValue>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(validation), "validation"),
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

        /// <summary>
        /// The <see cref="BaseField{TValue}"/> that represents the inner field.
        /// </summary>
        public TValueField valueField => m_ValueField;

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
            : this(label, new TValueField()) { }

        /// <summary>
        /// Initializes and returns an instance of StylePropertyField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="visualInput">The visual input of the field.</param>
        protected StylePropertyField(string label, TValueField visualInput)
            : base(label, visualInput)
        {
            // Do not allow null visualInput
            visualInput ??= new TValueField { pickingMode = PickingMode.Ignore, focusable = true };

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
                    value = new TStyleValue { value = evt.newValue };
            });

            AddLabelDragger();
        }

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
