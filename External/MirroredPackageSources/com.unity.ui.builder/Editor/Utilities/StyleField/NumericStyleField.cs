using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class NumericStyleField : StyleField<float>
    {
        public new class UxmlFactory : UxmlFactory<NumericStyleField, UxmlTraits> {}

        public new class UxmlTraits : StyleField<float>.UxmlTraits {}

        public float number
        {
            get => innerValue;
            set
            {
                innerValue = value;
                option = s_NoOptionString;
                SetValueWithoutNotify(innerValue.ToString());
            }
        }

        public NumericStyleField() : this(string.Empty) { }

        public NumericStyleField(string label) : base(label)
        {
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

            var num = new string(val.Where((c) => Char.IsDigit(c) || c == '.' || c == '-').ToArray());
            float number;
            var result = float.TryParse(num, out number);
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
    }
}
