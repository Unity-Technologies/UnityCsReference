// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is the base class for the composite fields.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement]
    public abstract partial class BaseCompositeField<TValueType, TField, TFieldValue> : BaseField<TValueType>, IDelayedField
        where TField : TextValueField<TFieldValue>, new()
    {
        internal static readonly BindingId isDelayedProperty = nameof(isDelayed);

        internal struct FieldDescription
        {
            public delegate void WriteDelegate(ref TValueType val, TFieldValue fieldValue);

            internal readonly string name;
            internal readonly string ussName;
            internal readonly Func<TValueType, TFieldValue> read;
            internal readonly WriteDelegate write;

            public FieldDescription(string name, string ussName, Func<TValueType, TFieldValue> read, WriteDelegate write)
            {
                this.name = name;
                this.ussName = ussName;
                this.read = read;
                this.write = write;
            }
        }

        List<TField> m_Fields;
        internal List<TField> fields => m_Fields;

        internal abstract FieldDescription[] DescribeFields();
        TField m_FieldSkippedByDisplayUpdate;
        bool m_IsSettingValidatedValue;
        bool m_ForceUpdateDisplay;
        bool m_IsDelayed;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-composite-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// USS class name of spacers in elements of this type.
        /// </summary>
        public static readonly string spacerUssClassName = ussClassName + "__field-spacer";
        internal static readonly UniqueStyleString spacerUssClassNameUnique = new(spacerUssClassName);

        /// <summary>
        /// USS class name of elements of this type when the fields are displayed on multiple lines.
        /// </summary>
        public static readonly string multilineVariantUssClassName = ussClassName + "--multi-line";
        internal static readonly UniqueStyleString multilineVariantUssClassNameUnique = new(multilineVariantUssClassName);

        /// <summary>
        /// USS class name of field groups in elements of this type.
        /// </summary>
        public static readonly string fieldGroupUssClassName = ussClassName + "__field-group";
        internal static readonly UniqueStyleString fieldGroupUssClassNameUnique = new(fieldGroupUssClassName);

        /// <summary>
        /// USS class name of fields in elements of this type.
        /// </summary>
        public static readonly string fieldUssClassName = ussClassName + "__field";
        internal static readonly UniqueStyleString fieldUssClassNameUnique = new(fieldUssClassName);

        /// <summary>
        /// USS class name of the first field in elements of this type.
        /// </summary>
        public static readonly string firstFieldVariantUssClassName = fieldUssClassName + "--first";
        internal static readonly UniqueStyleString firstFieldVariantUssClassNameUnique = new(firstFieldVariantUssClassName);

        /// <summary>
        /// USS class name of elements of this type when the fields are displayed on two lines.
        /// </summary>
        public static readonly string twoLinesVariantUssClassName = ussClassName + "--two-lines";
        internal static readonly UniqueStyleString twoLinesVariantUssClassNameUnique = new(twoLinesVariantUssClassName);


        /// <summary>
        /// If set to true, the value property only updates after either the user presses Enter or moves focus away from one of the value fields.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool isDelayed
        {
            get => m_IsDelayed;
            set
            {
                if (m_IsDelayed == value)
                    return;

                m_IsDelayed = value;
                foreach (var f in fields)
                {
                    f.isDelayed = m_IsDelayed;
                }

                NotifyPropertyChanged(isDelayedProperty);
            }
        }

        protected BaseCompositeField(string label, int fieldsByLine)
            : base(label, null)
        {
            delegatesFocus = false;
            visualInput.focusable = false;

            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);

            m_Fields = new List<TField>();
            FieldDescription[] fieldDescriptions = DescribeFields();

            int numberOfLines = 1;
            if (fieldsByLine > 1)
            {
                numberOfLines = fieldDescriptions.Length / fieldsByLine;
            }

            var isMultiLine = false;
            if (numberOfLines > 1)
            {
                isMultiLine = true;
                AddToClassList(multilineVariantUssClassNameUnique);
            }

            for (int i = 0; i < numberOfLines; i++)
            {
                VisualElement newLineGroup = null;
                if (isMultiLine)
                {
                    newLineGroup = new VisualElement();
                    newLineGroup.AddToClassList(fieldGroupUssClassNameUnique);
                }

                bool firstField = true;

                for (int j = i * fieldsByLine; j < ((i * fieldsByLine) + fieldsByLine); j++)
                {
                    var desc = fieldDescriptions[j];
                    var field = new TField()
                    {
                        name = desc.ussName
                    };
                    field.delegatesFocus = true;
                    field.AddToClassList(fieldUssClassNameUnique);
                    if (firstField)
                    {
                        field.AddToClassList(firstFieldVariantUssClassNameUnique);
                        firstField = false;
                    }

                    field.label = desc.name;

                    // The whole validated value, kept so components changed by onValidateValue aren't rebuilt from stale ones. (UUM-161306)
                    var hasValidatedValue = false;
                    TValueType validatedValue = default;
                    var hasEditStart = false;
                    TValueType editStartValue = default;
                    var editId = 0;

                    field.onValidateValue += newValue =>
                    {
                        if (isValidationIgnored)
                        {
                            hasValidatedValue = false;
                            return newValue;
                        }

                        // Every keystroke of an edit applies to the value the edit started from, so the other components don't drift.
                        if (hasEditStart && hasValidatedValue && !EqualityComparer<TValueType>.Default.Equals(value, validatedValue))
                            editStartValue = value;

                        TValueType cur = hasEditStart ? editStartValue : value;
                        desc.write(ref cur, newValue);
                        validatedValue = ValidatedValue(cur);
                        hasValidatedValue = true;
                        return desc.read(validatedValue);
                    };

                    field.RegisterCallback<FocusInEvent>(e =>
                    {
                        if (e.target == field)
                            return;
                        editId++;
                        hasEditStart = true;
                        editStartValue = value;
                        hasValidatedValue = false;
                    });

                    // The text is committed by the blur that follows the focus out, so the edit ends after it.
                    field.RegisterCallback<FocusOutEvent>(e =>
                    {
                        if (e.relatedTarget is VisualElement target && (target == field || field.Contains(target)))
                            return;
                        var endingEditId = editId;
                        field.schedule.Execute(() =>
                        {
                            if (endingEditId != editId)
                                return;
                            hasEditStart = false;
                            hasValidatedValue = false;
                        });
                    });

                    field.RegisterValueChangedCallback(e =>
                    {
                        var useValidatedValue = hasValidatedValue && EqualityComparer<TFieldValue>.Default.Equals(desc.read(validatedValue), e.newValue);
                        TValueType cur = value;
                        if (useValidatedValue)
                            cur = validatedValue;
                        else
                            desc.write(ref cur, e.newValue);

                        // Here, just check and make sure the text is updated in the basic field and is the same as the value...
                        // For example, backspace done on a selected value will empty the field (text == "") but the value will be 0.
                        // Or : a text of "2+3" is valid until enter is pressed, so not equal to a value of "5".
                        var valueString = e.newValue.ToString();
                        var textString = ((TField)e.currentTarget).text;
                        // If text is different or value changed because of an explicit value set
                        if (valueString != textString || field.CanTryParse(textString))
                        {
                            m_FieldSkippedByDisplayUpdate = field;
                        }

                        m_IsSettingValidatedValue = useValidatedValue;
                        try
                        {
                            value = cur;
                        }
                        finally
                        {
                            m_FieldSkippedByDisplayUpdate = null;
                            m_IsSettingValidatedValue = false;
                        }
                    });

                    m_Fields.Add(field);

                    if (isMultiLine)
                    {
                        newLineGroup.Add(field);
                    }
                    else
                    {
                        visualInput.hierarchy.Add(field);
                    }
                }

                if (isMultiLine)
                {
                    visualInput.hierarchy.Add(newLineGroup);
                }
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (m_Fields.Count != 0)
            {
                var i = 0;
                FieldDescription[] fieldDescriptions = DescribeFields();
                foreach (var fd in fieldDescriptions)
                {
                    // The field being edited keeps its text as typed.
                    if (m_Fields[i] != m_FieldSkippedByDisplayUpdate)
                        m_Fields[i].SetValueWithoutNotify(fd.read(rawValue));
                    i++;
                }
            }
        }

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            var displayNeedsUpdate = m_ForceUpdateDisplay || !EqualityComparer<TValueType>.Default.Equals(rawValue, newValue);

            // A value already validated by a sub-field edit is stored and displayed as is, so onValidateValue runs once per edit.
            var isValidatedValue = m_IsSettingValidatedValue;
            m_IsSettingValidatedValue = false;
            using (isValidatedValue ? IgnoreValidation() : default)
            {
                // Make sure to call the base class to set the value...
                base.SetValueWithoutNotify(newValue);

                // Before Updating the display, just check if the value changed...
                if (displayNeedsUpdate)
                {
                    UpdateDisplay();
                }
            }

            m_ForceUpdateDisplay = false;
        }

        internal override void OnViewDataReady()
        {
            // Should the composite field be reloaded, ensure that the value saved in memory is actually displayed when a data key is used.
            m_ForceUpdateDisplay = true;

            base.OnViewDataReady();
        }

        protected override void UpdateMixedValueContent()
        {
            foreach (var field in m_Fields)
            {
                field.showMixedValue = showMixedValue;
            }
        }
    }
}
