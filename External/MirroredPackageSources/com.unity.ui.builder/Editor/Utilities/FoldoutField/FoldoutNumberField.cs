using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FoldoutNumberField : FoldoutField
    {
        public new class UxmlFactory : UxmlFactory<FoldoutNumberField, UxmlTraits> {}

        public new class UxmlTraits : FoldoutField.UxmlTraits {}

        TextField m_TextField;
        IntegerField m_DraggerIntegerField;
        public List<string> fieldValues = new List<string>(); // Keeps track of child field values inputted from the header field

        public static readonly string textUssClassName = BuilderConstants.FoldoutFieldPropertyName + "__textfield";
        static readonly string k_DraggerFieldUssClassName = BuilderConstants.FoldoutFieldPropertyName + "__dragger-field";
        static readonly char k_FieldStringSeparator = ' '; // Formatting the header field with multiple values

        public TextField headerInputField
        {
            get
            {
                return m_TextField;
            }
        }

        public FoldoutNumberField()
        {
            // Used for its dragger.
            var toggleInput = toggle.Q(className: "unity-toggle__input");
            m_DraggerIntegerField = new IntegerField(" ");
            m_DraggerIntegerField.name = "dragger-integer-field";
            m_DraggerIntegerField.AddToClassList(k_DraggerFieldUssClassName);
            m_DraggerIntegerField.RegisterValueChangedCallback(OnDraggerFieldUpdate);
            toggleInput.Add(m_DraggerIntegerField);

            m_TextField = new TextField();
            m_TextField.isDelayed = true; // only updates on Enter or lost focus
            m_TextField.AddToClassList(textUssClassName);
            header.hierarchy.Add(m_TextField);
        }

        public override void UpdateFromChildFields()
        {
            var styleFields = this.Query<StyleFieldBase>().ToList();

            bool allTheSame = true;
            string singleValue = string.Empty;
            string cumulativeValue = string.Empty;

            for (int i = 0; i < styleFields.Count; ++i)
            {
                if (i == 0)
                    singleValue = styleFields[i].value;
                else if (singleValue != styleFields[i].value)
                    allTheSame = false;

                if (i != 0)
                    cumulativeValue += k_FieldStringSeparator;

                cumulativeValue += styleFields[i].value;
            }

            if (allTheSame)
                m_TextField.SetValueWithoutNotify(singleValue);
            else
                m_TextField.SetValueWithoutNotify(cumulativeValue);

            if (styleFields.Count > 0 && (styleFields[0] is StyleField<int> || styleFields[0] is StyleField<float>))
            {
                var intField = styleFields[0] as StyleField<int>;
                var floatField = styleFields[0] as StyleField<float>;

                if (intField != null)
                    m_DraggerIntegerField.SetValueWithoutNotify(intField.innerValue);
                else
                    m_DraggerIntegerField.SetValueWithoutNotify((int)floatField.innerValue);
            }
        }

        void OnDraggerFieldUpdate(ChangeEvent<int> evt)
        {
            m_TextField.value = evt.newValue.ToString();
        }
    }
}
