// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public abstract class BaseCompoundField<T> : BaseValueField<T>
    {
        public class FieldDescription
        {
            public delegate void WriteDelegate(ref T val, double fieldValue);

            internal readonly string name;
            internal readonly Func<T, double> read;
            internal readonly WriteDelegate write;

            public FieldDescription(string name, Func<T, double> read, WriteDelegate write)
            {
                this.name = name;
                this.read = read;
                this.write = write;
            }
        }

        protected static FieldDescription[] s_FieldDescriptions;
        protected List<DoubleField> m_Fields;

        internal abstract FieldDescription[] DescribeFields();

        protected BaseCompoundField()
        {
            AddToClassList("compositeField");
            if (s_FieldDescriptions == null)
                s_FieldDescriptions = DescribeFields();

            if (s_FieldDescriptions == null)
            {
                Debug.LogError("Describe fields MUST return a non null array of field descriptions");
                return;
            }

            m_Fields = new List<DoubleField>(s_FieldDescriptions.Length);
            foreach (FieldDescription desc in s_FieldDescriptions)
            {
                var fieldContainer = new VisualElement();
                fieldContainer.AddToClassList("field");
                fieldContainer.Add(new Label(desc.name));
                var field = new DoubleField();
                fieldContainer.Add(field);
                field.OnValueChanged(e =>
                    {
                        T cur = value;
                        desc.write(ref cur, e.newValue);
                        SetValueAndNotify(cur);
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

        protected override void UpdateDisplay()
        {
            if (s_FieldDescriptions != null)
            {
                for (int i = 0; i < s_FieldDescriptions.Length; i++)
                {
                    m_Fields[i].value = (s_FieldDescriptions[i].read(m_Value));
                }
            }
        }

        private DoubleField AddDoubleField(EventCallback<ChangeEvent<double>> callback)
        {
            var field = new DoubleField();

            shadow.Add(field);
            field.OnValueChanged(callback);
            return field;
        }
    }
}
