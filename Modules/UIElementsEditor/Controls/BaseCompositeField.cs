// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public abstract class BaseCompositeField<TValueType, TField, TFieldValue> : BaseField<TValueType>
        where TField : TextValueField<TFieldValue>, new()
    {
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

        private VisualElement GetSpacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList(spacerUssClassName);
            spacer.visible = false;
            spacer.focusable = false;
            return spacer;
        }

        List<TField> m_Fields;
        internal List<TField> fields => m_Fields;

        internal abstract FieldDescription[] DescribeFields();
        bool m_ShouldUpdateDisplay;

        public new static readonly string ussClassName = "unity-composite-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string spacerUssClassName = ussClassName + "__field-spacer";
        public static readonly string multilineVariantUssClassName = ussClassName + "--multi-line";
        public static readonly string fieldGroupUssClassName = ussClassName + "__field-group";
        public static readonly string fieldUssClassName = ussClassName + "__field";
        public static readonly string firstFieldVariantUssClassName = fieldUssClassName + "--first";
        public static readonly string twoLinesVariantUssClassName = ussClassName + "--two-lines";

        protected BaseCompositeField(string label, int fieldsByLine)
            : base(label, null)
        {
            delegatesFocus = false;
            visualInput.focusable = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

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
                AddToClassList(multilineVariantUssClassName);
            }

            for (int i = 0; i < numberOfLines; i++)
            {
                VisualElement newLineGroup = null;
                if (isMultiLine)
                {
                    newLineGroup = new VisualElement();
                    newLineGroup.AddToClassList(fieldGroupUssClassName);
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
                    field.AddToClassList(fieldUssClassName);
                    if (firstField)
                    {
                        field.AddToClassList(firstFieldVariantUssClassName);
                        firstField = false;
                    }

                    field.label = desc.name;
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
                    m_Fields[i].value = (fd.read(rawValue));
                    i++;
                }
            }
        }

        public override void SetValueWithoutNotify(TValueType newValue)
        {
            var displayNeedsUpdate = m_ShouldUpdateDisplay && !EqualityComparer<TValueType>.Default.Equals(rawValue, newValue);

            // Make sure to call the base class to set the value...
            base.SetValueWithoutNotify(newValue);

            // Before Updating the display, just check if the value changed...
            if (displayNeedsUpdate)
            {
                UpdateDisplay();
            }
        }
    }
}
