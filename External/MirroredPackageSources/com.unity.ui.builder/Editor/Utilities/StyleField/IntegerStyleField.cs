using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class IntegerStyleField : StyleField<int>
    {
        static readonly string k_DraggerFieldUssClassName = "unity-style-field__dragger-field";

        public new class UxmlFactory : UxmlFactory<IntegerStyleField, UxmlTraits> {}

        public new class UxmlTraits : StyleField<int>.UxmlTraits {}

        public int number
        {
            get => innerValue;
            set
            {
                innerValue = value;
                option = s_NoOptionString;
                SetValueWithoutNotify(innerValue.ToString());
            }
        }

        IntegerField m_DraggerIntegerField;

        public IntegerStyleField() : this(string.Empty) { }

        public IntegerStyleField(string label) : base(label)
        {
            m_DraggerIntegerField = new IntegerField(" ");
            m_DraggerIntegerField.name = "dragger-integer-field";
            m_DraggerIntegerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerIntegerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            Insert(0, m_DraggerIntegerField);
            RefreshChildFields();
        }

        protected override List<string> GenerateAdditionalOptions(string binding)
        {
            return new List<string>() { s_NoOptionString };
        }

        protected override bool SetInnerValueFromValue(string val)
        {
            if (styleKeywords.Contains(val))
                return false;

            var num = new string(val.Where((c) => Char.IsDigit(c) || c == '-').ToArray());
            int number;
            var result = int.TryParse(num, out number);
            if (!result)
                return false;

            if (isKeyword)
                option = s_NoOptionString;

            innerValue = number;
            return true;
        }

        protected override bool SetOptionFromValue(string val)
        {
            if (base.SetOptionFromValue(val))
                return true;

            option = s_NoOptionString;
            return true;
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            if (isKeyword)
                option = s_NoOptionString;

            value = evt.newValue.ToString();

            evt.StopImmediatePropagation();
            evt.PreventDefault();
        }
    }
}
