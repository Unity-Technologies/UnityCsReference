// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Internal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is the base class for the composite fields.
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public abstract class BaseCompositeField<TValueType, TField, TFieldValue> : BaseField<TValueType>, IDelayedField
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

        [ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : BaseField<TValueType>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] bool isDelayed;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags isDelayed_UxmlAttributeFlags;
            #pragma warning restore 649

            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TValueType>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(isDelayed), "is-delayed"),
                }, false);
            }

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (BaseCompositeField<TValueType, TField, TFieldValue>)obj;
                if (ShouldWriteAttributeValue(isDelayed_UxmlAttributeFlags))
                    e.isDelayed = isDelayed;
            }
        }

        private VisualElement GetSpacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList(spacerUssClassNameUnique);
            spacer.visible = false;
            spacer.focusable = false;
            return spacer;
        }

        List<TField> m_Fields;
        internal List<TField> fields => m_Fields;

        internal abstract FieldDescription[] DescribeFields();
        bool m_ShouldUpdateDisplay;
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

            m_ShouldUpdateDisplay = true;
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

                    field.onValidateValue += newValue =>
                    {
                        TValueType cur = value;
                        desc.write(ref cur, newValue);
                        var validatedValue = ValidatedValue(cur);
                        return desc.read(validatedValue);
                    };

                    field.RegisterValueChangedCallback(e =>
                    {
                        TValueType cur = value;
                        desc.write(ref cur, e.newValue);

                        // Here, just check and make sure the text is updated in the basic field and is the same as the value...
                        // For example, backspace done on a selected value will empty the field (text == "") but the value will be 0.
                        // Or : a text of "2+3" is valid until enter is pressed, so not equal to a value of "5".
                        var valueString = e.newValue.ToString();
                        var textString = ((TField)e.currentTarget).text;
                        // If text is different or value changed because of an explicit value set
                        if (valueString != textString || field.CanTryParse(textString))
                        {
                            m_ShouldUpdateDisplay = false;
                        }

                        value = cur;
                        m_ShouldUpdateDisplay = true;
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

                if (fieldsByLine < 3)
                {
                    int fieldsToAdd = 3 - fieldsByLine;
                    for (int countToAdd = 0; countToAdd < fieldsToAdd; countToAdd++)
                    {
                        if (isMultiLine)
                        {
                            newLineGroup.Add(GetSpacer());
                        }
                        else
                        {
                            visualInput.hierarchy.Add(GetSpacer());
                        }
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
                    m_Fields[i].SetValueWithoutNotify(fd.read(rawValue));
                    i++;
                }
            }
        }

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            var displayNeedsUpdate = m_ForceUpdateDisplay || (m_ShouldUpdateDisplay && !EqualityComparer<TValueType>.Default.Equals(rawValue, newValue));

            // Make sure to call the base class to set the value...
            base.SetValueWithoutNotify(newValue);

            // Before Updating the display, just check if the value changed...
            if (displayNeedsUpdate)
            {
                UpdateDisplay();
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
