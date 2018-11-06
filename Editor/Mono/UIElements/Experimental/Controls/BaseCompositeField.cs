// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    /// <summary>
    ///  This is the base class for the compound fields of type TMain.
    /// </summary>
    /// <typeparam name="TValue">The type of the object to be represented by the fields (example: Vector3)</typeparam>
    /// <typeparam name="TField">The type of a single field in the compound field. (example: for a Vector3, this is FloatField)</typeparam>
    /// <typeparam name="TFieldValue">The basic type of an individual object contained in the TField. (example: for a FloatField, this is a float)</typeparam>
    public abstract class BaseCompositeField<TValue, TField, TFieldValue> : BaseField<TValue>
        where TField : TextValueField<TFieldValue>, new()
    {
        public new class UxmlTraits : BaseField<TValue>.UxmlTraits {}

        internal struct FieldDescription
        {
            public delegate void WriteDelegate(ref TValue val, TFieldValue fieldValue);

            internal readonly string name;
            internal readonly Func<TValue, TFieldValue> read;
            internal readonly WriteDelegate write;

            public FieldDescription(string name, Func<TValue, TFieldValue> read, WriteDelegate write)
            {
                this.name = name;
                this.read = read;
                this.write = write;
            }
        }

        public override int focusIndex
        {
            get { return base.focusIndex; }
            set
            {
                base.focusIndex = value;
                if ((m_Fields != null) && (m_Fields.Count > 0))
                {
                    foreach (var field in m_Fields)
                    {
                        field.focusIndex = value;
                    }
                }
            }
        }
        protected List<TField> m_Fields;
        internal abstract FieldDescription[] DescribeFields();

        bool m_ShouldUpdateDisplay;
        protected BaseCompositeField()
        {
            AddToClassList("compositeField");
            m_ShouldUpdateDisplay = true;
            m_Fields = new List<TField>();
            FieldDescription[] fieldDescriptions = DescribeFields();
            foreach (var desc in fieldDescriptions)
            {
                var fieldContainer = new VisualElement();
                fieldContainer.AddToClassList("field");
                fieldContainer.Add(new Label(desc.name));
                var field = new TField();
                fieldContainer.Add(field);
                field.OnValueChanged(e =>
                {
                    TValue cur = value;
                    desc.write(ref cur, e.newValue);

                    // Here, just check and make sure the text is updated in the basic field and is the same as the value...
                    // For example, backspace done on a selected value will empty the field (text == "") but the value will be 0.
                    // Or : a text of "2+3" is valid until enter is pressed, so not equal to a value of "5".
                    if (e.newValue.ToString() != ((TField)e.currentTarget).text)
                    {
                        m_ShouldUpdateDisplay = false;
                    }

                    value = cur;
                    m_ShouldUpdateDisplay = true;
                });
                m_Fields.Add(field);
                shadow.Add(fieldContainer);
            }

            UpdateDisplay();
        }

        public override VisualElement contentContainer
        {
            get { return null; }
        }

        private void UpdateDisplay()
        {
            if (m_Fields.Count != 0)
            {
                var i = 0;
                FieldDescription[] fieldDescriptions = DescribeFields();
                foreach (var fd in fieldDescriptions)
                {
                    m_Fields[i].value = (fd.read(m_Value));
                    i++;
                }
            }
        }

        public override void SetValueWithoutNotify(TValue newValue)
        {
            var displayNeedsUpdate = m_ShouldUpdateDisplay && !EqualityComparer<TValue>.Default.Equals(m_Value, newValue);

            // Make sure to call the base class to set the value...
            base.SetValueWithoutNotify(newValue);

            // Before Updating the display, just check if the value changed...
            if (displayNeedsUpdate)
            {
                UpdateDisplay();
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            // Focus first field if any
            if (evt.GetEventTypeId() == FocusEvent.TypeId() && m_Fields.Count > 0)
                m_Fields[0].Focus();
        }
    }
}
